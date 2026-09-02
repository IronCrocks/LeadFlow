using LeadFlow.Application.Notifications;
using LeadFlow.Infrastructure.Identity;
using LeadFlow.Infrastructure.Persistence;
using LeadFlow.Infrastructure.Telegram;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeadFlow.Tests;

public sealed class MvpWorkflowWebApplicationFactory(string connectionString)
    : WebApplicationFactory<global::Program>
{
    public const string ManagerEmail = "manager@example.com";
    public const string ManagerPassword = "Test-manager-password1!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
            services.Configure<InitialManagerOptions>(options =>
            {
                options.Email = ManagerEmail;
                options.Password = ManagerPassword;
            });
            services.AddSingleton<ILeadNotificationSender, DisabledLeadNotificationSender>();
        });
    }
}
