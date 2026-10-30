using System.Net;
using LeadFlow.Tests.Infrastructure.Persistence;
using Xunit;

namespace LeadFlow.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class HomePageTests(PostgreSqlDatabaseFixture database) : IDisposable
{
    private readonly HomePageWebApplicationFactory _factory = new(database.ConnectionString);

    [Fact]
    public async Task GetRoot_ReturnsLandingPage()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("LeadFlow", content);
        Assert.Contains("Разрабатываем ПО", content);
        Assert.Contains("id=\"services\"", content);
        Assert.Contains("id=\"process\"", content);
        Assert.Contains("id=\"contact\"", content);
        Assert.Contains("name=\"Lead.SubmissionId\"", content);
        Assert.Contains("name=\"Lead.Service\"", content);
        Assert.DoesNotContain("50+ компаний", content);
        Assert.DoesNotContain("hello@leadflow.example", content);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
