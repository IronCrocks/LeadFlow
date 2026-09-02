using System.Net;
using System.Text.RegularExpressions;
using LeadFlow.Domain.Entities;
using LeadFlow.Domain.Enums;
using LeadFlow.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LeadFlow.Tests.Infrastructure.Identity;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdminLeadListTests(PostgreSqlDatabaseFixture database) : IDisposable
{
    private readonly AuthenticationWebApplicationFactory _factory = new(database.ConnectionString);

    [Fact]
    public async Task GetLeads_WhenAnonymous_RedirectsToLogin()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/admin/leads");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/admin/login", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData(LeadStatus.New)]
    [InlineData(LeadStatus.InProgress)]
    [InlineData(LeadStatus.Completed)]
    [InlineData(LeadStatus.Rejected)]
    public async Task GetLeads_WithStatusFilter_ShowsOnlyRequestedStatus(LeadStatus status)
    {
        var names = await SeedLeadsAsync();
        using var client = CreateClient();
        await LoginAsync(client);

        var response = await client.GetAsync($"/admin/leads?status={status}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(names[status], content);
        foreach (var (otherStatus, name) in names.Where(item => item.Key != status))
        {
            Assert.DoesNotContain(name, content);
        }
    }

    [Fact]
    public async Task GetLeads_SortsByCreationDateOnDatabaseQuery()
    {
        var prefix = Guid.NewGuid().ToString("N");
        var olderName = $"Старая заявка {prefix}";
        var newerName = $"Новая заявка {prefix}";
        await SeedLeadAsync(olderName, LeadStatus.New, DateTimeOffset.UtcNow.AddHours(-2));
        await SeedLeadAsync(newerName, LeadStatus.New, DateTimeOffset.UtcNow.AddHours(-1));
        using var client = CreateClient();
        await LoginAsync(client);

        var newestFirst = WebUtility.HtmlDecode(await client.GetStringAsync("/admin/leads"));
        var oldestFirst = WebUtility.HtmlDecode(await client.GetStringAsync("/admin/leads?sortOrder=OldestFirst"));

        Assert.True(newestFirst.IndexOf(newerName, StringComparison.Ordinal) < newestFirst.IndexOf(olderName, StringComparison.Ordinal));
        Assert.True(oldestFirst.IndexOf(olderName, StringComparison.Ordinal) < oldestFirst.IndexOf(newerName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetLeads_DisplaysCreationTimeInConfiguredTimeZone()
    {
        var name = $"Time zone {Guid.NewGuid():N}";
        await SeedLeadAsync(
            name,
            LeadStatus.New,
            new DateTimeOffset(2026, 9, 2, 12, 40, 0, TimeSpan.Zero));
        using var client = CreateClient();
        await LoginAsync(client);

        var content = await client.GetStringAsync("/admin/leads");

        Assert.Contains("02.09.2026 15:40", content);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<Dictionary<LeadStatus, string>> SeedLeadsAsync()
    {
        var prefix = Guid.NewGuid().ToString("N");
        var names = new Dictionary<LeadStatus, string>();
        var timestamp = DateTimeOffset.UtcNow;

        foreach (var status in Enum.GetValues<LeadStatus>())
        {
            var name = $"{status} {prefix}";
            names[status] = name;
            await SeedLeadAsync(name, status, timestamp);
            timestamp = timestamp.AddMinutes(1);
        }

        return names;
    }

    private async Task SeedLeadAsync(string name, LeadStatus status, DateTimeOffset createdAtUtc)
    {
        await using var context = database.CreateContext();
        context.Leads.Add(new Lead
        {
            Id = Guid.NewGuid(),
            SubmissionId = Guid.NewGuid(),
            Name = name,
            Phone = "+7 999 123-45-67",
            Service = "Разработка сайта",
            CreatedAtUtc = createdAtUtc,
            Status = status,
            Source = LeadSource.Website
        });
        await context.SaveChangesAsync();
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
        var tokenMatch = Regex.Match(
            page,
            "<input[^>]+name=\"__RequestVerificationToken\"[^>]+value=\"(?<token>[^\"]+)\"");
        var token = WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value);
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = AuthenticationWebApplicationFactory.ManagerEmail,
            ["Password"] = AuthenticationWebApplicationFactory.ManagerPassword,
            ["__RequestVerificationToken"] = token
        });
        var response = await client.PostAsync("/admin/login", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
