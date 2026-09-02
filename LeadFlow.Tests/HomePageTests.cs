using System.Net;
using Xunit;

namespace LeadFlow.Tests;

public sealed class HomePageTests(HomePageWebApplicationFactory factory)
    : IClassFixture<HomePageWebApplicationFactory>
{
    [Fact]
    public async Task GetRoot_ReturnsLandingPage()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("LeadFlow", content);
    }
}
