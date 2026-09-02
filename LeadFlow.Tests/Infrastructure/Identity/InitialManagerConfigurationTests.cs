using LeadFlow.Infrastructure.Identity;
using Xunit;

namespace LeadFlow.Tests.Infrastructure.Identity;

public sealed class InitialManagerConfigurationTests
{
    [Fact]
    public void Validate_WhenManagerIsConfiguredWithoutConnectionString_ReportsConfigurationError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            InitialManagerConfiguration.Validate("manager@example.com", "Test-manager-password1!", null));

        Assert.Contains("ConnectionStrings:DefaultConnection", exception.Message);
    }
}
