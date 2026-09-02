using System.Net;
using System.Text.RegularExpressions;
using LeadFlow.Domain.Entities;
using LeadFlow.Domain.Enums;
using LeadFlow.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeadFlow.Tests.Infrastructure.Identity;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdminLeadDetailsTests(PostgreSqlDatabaseFixture database) : IDisposable
{
    private readonly AuthenticationWebApplicationFactory _factory = new(database.ConnectionString);

    [Fact]
    public async Task GetDetails_WhenAnonymous_RedirectsToLogin()
    {
        var leadId = await SeedLeadAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/admin/leads/{leadId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/admin/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task GetDetails_ShowsLeadDataAndStatusHistory()
    {
        var leadId = await SeedLeadAsync();
        using var client = CreateClient();
        await LoginAsync(client);

        var content = WebUtility.HtmlDecode(await client.GetStringAsync($"/admin/leads/{leadId}"));

        Assert.Contains("Иван Петров", content);
        Assert.Contains("ivan@example.com", content);
        Assert.Contains("Нужен лендинг.", content);
        Assert.Contains("Новая", content);
    }

    [Fact]
    public async Task GetDetails_WhenLeadDoesNotExist_ReturnsNotFound()
    {
        using var client = CreateClient();
        await LoginAsync(client);

        var response = await client.GetAsync($"/admin/leads/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithChangedStatus_PersistsCommentAndHistoryAuthor()
    {
        var leadId = await SeedLeadAsync();
        using var client = CreateClient();
        await LoginAsync(client);

        var response = await PostUpdateAsync(client, leadId, new Dictionary<string, string>
        {
            ["Edit.Status"] = LeadStatus.InProgress.ToString(),
            ["Edit.ManagerComment"] = "  Позвонить клиенту.  "
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/admin/leads/{leadId}", response.Headers.Location!.OriginalString);

        await using var context = database.CreateContext();
        var lead = await context.Leads
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == leadId);
        var manager = await context.Users.SingleAsync(user => user.Email == AuthenticationWebApplicationFactory.ManagerEmail);

        Assert.Equal(LeadStatus.InProgress, lead.Status);
        Assert.Equal("Позвонить клиенту.", lead.ManagerComment);
        var history = Assert.Single(lead.StatusHistory, item => item.NewStatus == LeadStatus.InProgress);
        Assert.Equal(LeadStatus.New, history.PreviousStatus);
        Assert.Equal(manager.Id, history.ChangedByUserId);
        Assert.NotEqual(default, history.ChangedAtUtc);
    }

    [Fact]
    public async Task Update_WithSameStatus_DoesNotAddHistory()
    {
        var leadId = await SeedLeadAsync();
        using var client = CreateClient();
        await LoginAsync(client);

        var response = await PostUpdateAsync(client, leadId, new Dictionary<string, string>
        {
            ["Edit.Status"] = LeadStatus.New.ToString(),
            ["Edit.ManagerComment"] = "Комментарий менеджера"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var context = database.CreateContext();
        var lead = await context.Leads
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == leadId);
        Assert.Equal("Комментарий менеджера", lead.ManagerComment);
        Assert.Single(lead.StatusHistory);
    }

    [Fact]
    public async Task Update_WithBoundaryLengthCommentAndOuterWhitespace_PersistsNormalizedValue()
    {
        var leadId = await SeedLeadAsync();
        using var client = CreateClient();
        await LoginAsync(client);
        var comment = new string('К', 4000);

        var response = await PostUpdateAsync(client, leadId, new Dictionary<string, string>
        {
            ["Edit.Status"] = LeadStatus.New.ToString(),
            ["Edit.ManagerComment"] = $" {comment} "
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var context = database.CreateContext();
        var lead = await context.Leads.SingleAsync(item => item.Id == leadId);
        Assert.Equal(comment, lead.ManagerComment);
    }

    [Fact]
    public async Task Update_WithInvalidModel_DoesNotChangeLead()
    {
        var leadId = await SeedLeadAsync();
        using var client = CreateClient();
        await LoginAsync(client);

        var response = await PostUpdateAsync(client, leadId, new Dictionary<string, string>
        {
            ["Edit.ManagerComment"] = new string('К', 4001)
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Выберите статус.", content);
        Assert.Contains("Внутренний комментарий не должен быть длиннее 4000 символов.", content);

        await using var context = database.CreateContext();
        var lead = await context.Leads
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == leadId);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Null(lead.ManagerComment);
        Assert.Single(lead.StatusHistory);
    }

    [Fact]
    public async Task Update_WithoutAntiforgeryToken_ReturnsBadRequestAndDoesNotChangeLead()
    {
        var leadId = await SeedLeadAsync();
        using var client = CreateClient();
        await LoginAsync(client);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Edit.Status"] = LeadStatus.InProgress.ToString(),
            ["Edit.ManagerComment"] = "Позвонить клиенту."
        });

        var response = await client.PostAsync($"/admin/leads/{leadId}", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var context = database.CreateContext();
        var lead = await context.Leads
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == leadId);
        Assert.Equal(LeadStatus.New, lead.Status);
        Assert.Null(lead.ManagerComment);
        Assert.Single(lead.StatusHistory);
    }

    [Fact]
    public async Task GetDetails_ShowsStatusHistoryInChronologicalOrder()
    {
        var leadId = await SeedLeadWithStatusHistoryAsync();
        using var client = CreateClient();
        await LoginAsync(client);

        var content = WebUtility.HtmlDecode(await client.GetStringAsync($"/admin/leads/{leadId}"));
        var history = content[content.IndexOf("История статусов", StringComparison.Ordinal)..];

        var created = history.IndexOf("Новая", StringComparison.Ordinal);
        var started = history.IndexOf("Новая → В работе", StringComparison.Ordinal);
        var completed = history.IndexOf("В работе → Выполнена", StringComparison.Ordinal);
        Assert.True(created >= 0);
        Assert.True(started > created);
        Assert.True(completed > started);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<Guid> SeedLeadAsync()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            SubmissionId = Guid.NewGuid(),
            Name = "Иван Петров",
            Phone = "+7 999 123-45-67",
            Email = "ivan@example.com",
            Service = "Разработка сайта",
            CustomerComment = "Нужен лендинг.",
            CreatedAtUtc = createdAt,
            Status = LeadStatus.New,
            Source = LeadSource.Website
        };

        await using var context = database.CreateContext();
        context.Leads.Add(lead);
        context.LeadStatusHistory.Add(new LeadStatusHistory
        {
            Id = Guid.NewGuid(),
            Lead = lead,
            NewStatus = LeadStatus.New,
            ChangedAtUtc = createdAt
        });
        await context.SaveChangesAsync();

        return lead.Id;
    }

    private async Task<Guid> SeedLeadWithStatusHistoryAsync()
    {
        var createdAt = new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            SubmissionId = Guid.NewGuid(),
            Name = "Иван Петров",
            Phone = "+7 999 123-45-67",
            Service = "Разработка сайта",
            CreatedAtUtc = createdAt,
            Status = LeadStatus.Completed,
            Source = LeadSource.Website
        };

        await using var context = database.CreateContext();
        context.Leads.Add(lead);
        context.LeadStatusHistory.AddRange(
            new LeadStatusHistory
            {
                Id = Guid.NewGuid(),
                Lead = lead,
                NewStatus = LeadStatus.New,
                ChangedAtUtc = createdAt
            },
            new LeadStatusHistory
            {
                Id = Guid.NewGuid(),
                Lead = lead,
                PreviousStatus = LeadStatus.New,
                NewStatus = LeadStatus.InProgress,
                ChangedAtUtc = createdAt.AddMinutes(1)
            },
            new LeadStatusHistory
            {
                Id = Guid.NewGuid(),
                Lead = lead,
                PreviousStatus = LeadStatus.InProgress,
                NewStatus = LeadStatus.Completed,
                ChangedAtUtc = createdAt.AddMinutes(2)
            });
        await context.SaveChangesAsync();

        return lead.Id;
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

    private static async Task LoginAsync(HttpClient client)
    {
        var page = await client.GetStringAsync("/admin/login");
        var token = GetAntiforgeryToken(page);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = AuthenticationWebApplicationFactory.ManagerEmail,
            ["Password"] = AuthenticationWebApplicationFactory.ManagerPassword,
            ["__RequestVerificationToken"] = token
        });
        var response = await client.PostAsync("/admin/login", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostUpdateAsync(
        HttpClient client,
        Guid leadId,
        IReadOnlyDictionary<string, string> values)
    {
        var page = await client.GetStringAsync($"/admin/leads/{leadId}");
        var formValues = new Dictionary<string, string>(values)
        {
            ["__RequestVerificationToken"] = GetAntiforgeryToken(page)
        };
        using var form = new FormUrlEncodedContent(formValues);
        return await client.PostAsync($"/admin/leads/{leadId}", form);
    }

    private static string GetAntiforgeryToken(string page)
    {
        var tokenMatch = Regex.Match(
            page,
            "<input[^>]+name=\"__RequestVerificationToken\"[^>]+value=\"(?<token>[^\"]+)\"");
        var token = WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value);
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }
}
