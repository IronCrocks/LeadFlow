using System.Net;
using System.Text.RegularExpressions;
using LeadFlow.Domain.Enums;
using LeadFlow.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeadFlow.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class MvpWorkflowTests(PostgreSqlDatabaseFixture database) : IDisposable
{
    private readonly MvpWorkflowWebApplicationFactory _factory = new(database.ConnectionString);

    [Fact]
    public async Task Manager_CanProcessLeadCreatedFromPublicForm()
    {
        using var client = CreateClient();
        var name = $"Клиент {Guid.NewGuid():N}";

        var createResponse = await PostPublicLeadAsync(client, new Dictionary<string, string>
        {
            ["Lead.Name"] = name,
            ["Lead.Phone"] = "+7 999 123-45-67",
            ["Lead.Email"] = "client@example.com",
            ["Lead.Service"] = "Разработка сайта",
            ["Lead.CustomerComment"] = "Нужен лендинг."
        });
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.Equal("/leads/success", createResponse.Headers.Location!.OriginalString);

        await LoginAsync(client);

        var list = WebUtility.HtmlDecode(await client.GetStringAsync("/admin/leads"));
        Assert.Contains(name, list);

        await using var readContext = database.CreateContext();
        var leadId = await readContext.Leads
            .Where(item => item.Name == name)
            .Select(item => item.Id)
            .SingleAsync();

        var details = WebUtility.HtmlDecode(await client.GetStringAsync($"/admin/leads/{leadId}"));
        Assert.Contains("client@example.com", details);
        Assert.Contains("Нужен лендинг.", details);
        Assert.Contains("Новая", details);

        var updateResponse = await PostStatusUpdateAsync(client, leadId, LeadStatus.InProgress, "Позвонить клиенту.");
        Assert.Equal(HttpStatusCode.Redirect, updateResponse.StatusCode);
        Assert.Equal($"/admin/leads/{leadId}", updateResponse.Headers.Location!.OriginalString);

        await using var writeContext = database.CreateContext();
        var lead = await writeContext.Leads
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == leadId);
        Assert.Equal(LeadStatus.InProgress, lead.Status);
        Assert.Equal("Позвонить клиенту.", lead.ManagerComment);
        Assert.Collection(
            lead.StatusHistory.OrderBy(item => item.ChangedAtUtc).ThenBy(item => item.Id),
            initial =>
            {
                Assert.Null(initial.PreviousStatus);
                Assert.Equal(LeadStatus.New, initial.NewStatus);
            },
            update =>
            {
                Assert.Equal(LeadStatus.New, update.PreviousStatus);
                Assert.Equal(LeadStatus.InProgress, update.NewStatus);
            });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    private static async Task<HttpResponseMessage> PostPublicLeadAsync(
        HttpClient client,
        IReadOnlyDictionary<string, string> values)
    {
        var page = await client.GetStringAsync("/");
        var formValues = new Dictionary<string, string>(values)
        {
            ["Lead.SubmissionId"] = GetInputValue(page, "Lead.SubmissionId"),
            ["__RequestVerificationToken"] = GetInputValue(page, "__RequestVerificationToken")
        };
        using var form = new FormUrlEncodedContent(formValues);
        return await client.PostAsync("/leads", form);
    }

    private static async Task LoginAsync(HttpClient client)
    {
        var page = await client.GetStringAsync("/admin/login");
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = MvpWorkflowWebApplicationFactory.ManagerEmail,
            ["Password"] = MvpWorkflowWebApplicationFactory.ManagerPassword,
            ["__RequestVerificationToken"] = GetInputValue(page, "__RequestVerificationToken")
        });
        var response = await client.PostAsync("/admin/login", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostStatusUpdateAsync(
        HttpClient client,
        Guid leadId,
        LeadStatus status,
        string managerComment)
    {
        var page = await client.GetStringAsync($"/admin/leads/{leadId}");
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Edit.Status"] = status.ToString(),
            ["Edit.ManagerComment"] = managerComment,
            ["__RequestVerificationToken"] = GetInputValue(page, "__RequestVerificationToken")
        });
        return await client.PostAsync($"/admin/leads/{leadId}", form);
    }

    private static string GetInputValue(string page, string name)
    {
        var match = Regex.Match(
            page,
            $"<input[^>]+name=\"{Regex.Escape(name)}\"[^>]+value=\"(?<value>[^\"]+)\"");
        var value = WebUtility.HtmlDecode(match.Groups["value"].Value);
        Assert.False(string.IsNullOrWhiteSpace(value));
        return value;
    }
}
