using LeadFlow.Application.Authentication;
using LeadFlow.Application.Managers;
using LeadFlow.Application.Persistence;
using LeadFlow.Infrastructure.Identity;
using LeadFlow.Infrastructure.Persistence;
using LeadFlow.Infrastructure.Telegram;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeadFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var initialManagerEmail = configuration["InitialManager:Email"];
        var initialManagerPassword = configuration["InitialManager:Password"];
        InitialManagerConfiguration.Validate(initialManagerEmail, initialManagerPassword, connectionString);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.Configure<InitialManagerOptions>(configuration.GetSection(InitialManagerOptions.SectionName));
        services.AddScoped<InitialManagerInitializer>();
        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IManagerAuthenticationService, IdentityManagerAuthenticationService>();
        services.AddScoped<IManagerDirectory, IdentityManagerDirectory>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager();

        services.AddLeadNotifications(configuration);

        return services;
    }
}
