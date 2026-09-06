using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadFlow.Infrastructure.Identity;

public sealed class InitialManagerInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<InitialManagerOptions> options,
    ILogger<InitialManagerInitializer> logger)
{
    public async Task InitializeAsync()
    {
        var email = options.Value.Email?.Trim();
        var password = options.Value.Password;

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Initial manager is not configured; administrative login is unavailable.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "InitialManager:Email and InitialManager:Password must be set together.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var manager = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(manager, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Initial manager could not be created: {errors}");
        }

        logger.LogInformation("Initial manager account has been created.");
    }
}
