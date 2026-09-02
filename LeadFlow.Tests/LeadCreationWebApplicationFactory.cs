using LeadFlow.Application.Notifications;
using LeadFlow.Infrastructure.Persistence;
using LeadFlow.Infrastructure.Telegram;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeadFlow.Tests;

public sealed class LeadCreationWebApplicationFactory(string connectionString)
    : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
            services.AddSingleton<ILeadNotificationSender, DisabledLeadNotificationSender>();
        });
    }
}
