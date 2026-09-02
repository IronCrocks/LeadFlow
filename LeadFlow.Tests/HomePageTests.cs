using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LeadFlow.Tests;

public sealed class HomePageTests(WebApplicationFactory<global::Program> factory)
    : IClassFixture<WebApplicationFactory<global::Program>>
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
