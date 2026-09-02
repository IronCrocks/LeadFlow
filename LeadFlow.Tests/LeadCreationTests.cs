using System.Net;
using System.Text.RegularExpressions;
using LeadFlow.Domain.Enums;
using LeadFlow.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeadFlow.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class LeadCreationTests(PostgreSqlDatabaseFixture database) : IDisposable
{
    private readonly LeadCreationWebApplicationFactory _factory = new(database.ConnectionString);

    [Fact]
    public async Task Create_WithValidForm_PersistsNormalizedLeadAndInitialHistory()
    {
        using var client = CreateClient();
        var name = $"Клиент {Guid.NewGuid():N}";

        var response = await PostLeadAsync(client, new Dictionary<string, string>
        {
            ["Lead.Name"] = $"  {name}  ",
            ["Lead.Phone"] = "  +7 999 123-45-67  ",
            ["Lead.Email"] = "   ",
            ["Lead.Service"] = "Разработка сайта",
            ["Lead.CustomerComment"] = "  Нужен лендинг.  "
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/leads/success", response.Headers.Location!.OriginalString);

        await using var context = database.CreateContext();
        var lead = await context.Leads
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Name == name);

        Assert.Equal("+7 999 123-45-67", lead.Phone);
        Assert.Null(lead.Email);
        Assert.Equal("Разработка сайта", lead.Service);
        Assert.Equal("Нужен лендинг.", lead.CustomerComment);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Equal(LeadSource.Website, lead.Source);
        Assert.NotEqual(default, lead.CreatedAtUtc);

        var history = Assert.Single(lead.StatusHistory);
        Assert.Null(history.PreviousStatus);
        Assert.Equal(LeadStatus.New, history.NewStatus);
        Assert.Equal(lead.CreatedAtUtc, history.ChangedAtUtc);
    }

    [Fact]
    public async Task Create_WithBoundaryLengthValuesAndOuterWhitespace_PersistsNormalizedLead()
    {
        using var client = CreateClient();
        var name = new string('И', 92) + Guid.NewGuid().ToString("N")[..8];
        var phone = new string('1', 50);
        var comment = new string('К', 2000);

        var response = await PostLeadAsync(client, new Dictionary<string, string>
        {
            ["Lead.Name"] = $" {name} ",
            ["Lead.Phone"] = $" {phone} ",
            ["Lead.Email"] = " client@example.com ",
            ["Lead.Service"] = " Разработка сайта ",
            ["Lead.CustomerComment"] = $" {comment} "
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var context = database.CreateContext();
        var lead = await context.Leads.SingleAsync(item => item.Name == name);
        Assert.Equal(phone, lead.Phone);
        Assert.Equal("client@example.com", lead.Email);
        Assert.Equal("Разработка сайта", lead.Service);
        Assert.Equal(comment, lead.CustomerComment);
    }

    [Fact]
    public async Task Create_WithMissingRequiredFields_ReturnsFormAndDoesNotPersistLead()
    {
        using var client = CreateClient();
        var uniqueComment = $"invalid-{Guid.NewGuid():N}";

        var response = await PostLeadAsync(client, new Dictionary<string, string>
        {
            ["Lead.Name"] = "",
            ["Lead.Phone"] = "",
            ["Lead.Service"] = "",
            ["Lead.CustomerComment"] = uniqueComment
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Введите имя.", content);
        Assert.Contains("Введите телефон.", content);
        Assert.Contains("Выберите услугу.", content);

        await using var context = database.CreateContext();
        Assert.False(await context.Leads.AnyAsync(item => item.CustomerComment == uniqueComment));
    }

    [Fact]
    public async Task Create_WithServiceOutsideConfiguration_ReturnsFormAndDoesNotPersistLead()
    {
        using var client = CreateClient();
        var name = $"Клиент {Guid.NewGuid():N}";

        var response = await PostLeadAsync(client, new Dictionary<string, string>
        {
            ["Lead.Name"] = name,
            ["Lead.Phone"] = "+7 999 123-45-67",
            ["Lead.Service"] = "Несуществующая услуга"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Выберите услугу из списка.", content);

        await using var context = database.CreateContext();
        Assert.False(await context.Leads.AnyAsync(item => item.Name == name));
    }

    [Fact]
    public async Task Create_WithValidForm_UsesPostRedirectGet()
    {
        using var client = CreateClient();

        var response = await PostLeadAsync(client, new Dictionary<string, string>
        {
            ["Lead.Name"] = $"Клиент {Guid.NewGuid():N}",
            ["Lead.Phone"] = "+7 999 123-45-67",
            ["Lead.Service"] = "Разработка сайта"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/leads/success", response.Headers.Location!.OriginalString);

        var successResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);
        Assert.Contains("Заявка отправлена", await successResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_WithRepeatedIdenticalPost_PersistsOnlyOneLead()
    {
        using var client = CreateClient();
        var name = $"Клиент {Guid.NewGuid():N}";
        var formValues = await CreateFormValuesAsync(client, new Dictionary<string, string>
        {
            ["Lead.Name"] = name,
            ["Lead.Phone"] = "+7 999 123-45-67",
            ["Lead.Service"] = "Разработка сайта"
        });

        var firstResponse = await PostLeadFormAsync(client, formValues);
        var repeatedResponse = await PostLeadFormAsync(client, formValues);

        Assert.Equal(HttpStatusCode.Redirect, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, repeatedResponse.StatusCode);

        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync(item => item.Name == name));
    }

    [Fact]
    public async Task Create_WithoutAntiforgeryToken_ReturnsBadRequestAndDoesNotPersistLead()
    {
        using var client = CreateClient();
        var name = $"Клиент {Guid.NewGuid():N}";
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Lead.SubmissionId"] = Guid.NewGuid().ToString(),
            ["Lead.Name"] = name,
            ["Lead.Phone"] = "+7 999 123-45-67",
            ["Lead.Service"] = "Разработка сайта"
        });

        var response = await client.PostAsync("/leads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var context = database.CreateContext();
        Assert.False(await context.Leads.AnyAsync(item => item.Name == name));
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static async Task<HttpResponseMessage> PostLeadAsync(
        HttpClient client,
        IReadOnlyDictionary<string, string> values)
    {
        var formValues = await CreateFormValuesAsync(client, values);
        return await PostLeadFormAsync(client, formValues);
    }

    private static async Task<Dictionary<string, string>> CreateFormValuesAsync(
        HttpClient client,
        IReadOnlyDictionary<string, string> values)
    {
        var page = await client.GetStringAsync("/");
        var tokenMatch = Regex.Match(
            page,
            "<input[^>]+name=\"__RequestVerificationToken\"[^>]+value=\"(?<token>[^\"]+)\"");
        var token = WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var submissionIdMatch = Regex.Match(
            page,
            "<input[^>]+name=\"Lead.SubmissionId\"[^>]+value=\"(?<submissionId>[^\"]+)\"");
        var submissionId = WebUtility.HtmlDecode(submissionIdMatch.Groups["submissionId"].Value);
        Assert.False(string.IsNullOrWhiteSpace(submissionId));

        return new Dictionary<string, string>(values)
        {
            ["Lead.SubmissionId"] = submissionId,
            ["__RequestVerificationToken"] = token
        };
    }

    private static async Task<HttpResponseMessage> PostLeadFormAsync(
        HttpClient client,
        IReadOnlyDictionary<string, string> formValues)
    {
        using var form = new FormUrlEncodedContent(formValues);
        return await client.PostAsync("/leads", form);
    }
}
