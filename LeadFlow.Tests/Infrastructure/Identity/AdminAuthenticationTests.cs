using System.Net;
using System.Text.RegularExpressions;
using LeadFlow.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeadFlow.Tests.Infrastructure.Identity;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdminAuthenticationTests(PostgreSqlDatabaseFixture database) : IDisposable
{
    private readonly AuthenticationWebApplicationFactory _factory = new(database.ConnectionString);

    [Fact]
    public async Task Startup_CreatesInitialManager()
    {
        using var client = CreateClient();
        await client.GetAsync("/admin/login");

        await using var context = database.CreateContext();
        var manager = await context.Users.SingleAsync(user => user.Email == AuthenticationWebApplicationFactory.ManagerEmail);

        Assert.Equal(AuthenticationWebApplicationFactory.ManagerEmail, manager.UserName);
        Assert.NotNull(manager.PasswordHash);
    }

    [Fact]
    public async Task GetAdmin_WhenAnonymous_RedirectsToLogin()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/admin/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Login_WithValidCredentials_GrantsAccessToAdmin()
    {
        using var client = CreateClient();

        var loginResponse = await PostLoginAsync(
            client,
            AuthenticationWebApplicationFactory.ManagerEmail,
            AuthenticationWebApplicationFactory.ManagerPassword);

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        var adminResponse = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        var content = await adminResponse.Content.ReadAsStringAsync();
        Assert.Contains("Административная часть", content);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_DoesNotGrantAccess()
    {
        using var client = CreateClient();

        var loginResponse = await PostLoginAsync(
            client,
            AuthenticationWebApplicationFactory.ManagerEmail,
            "incorrect-password");

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var content = await loginResponse.Content.ReadAsStringAsync();
        Assert.Contains("Неверный email или пароль.", WebUtility.HtmlDecode(content));

        var adminResponse = await client.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.Redirect, adminResponse.StatusCode);
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

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string email, string password)
    {
        var page = await client.GetStringAsync("/admin/login");
        var tokenMatch = Regex.Match(
            page,
            "<input[^>]+name=\"__RequestVerificationToken\"[^>]+value=\"(?<token>[^\"]+)\"");
        var token = WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value);
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        });
        return await client.PostAsync("/admin/login", form);
    }
}
