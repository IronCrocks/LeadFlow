using LeadFlow.Application.Notifications;
using Microsoft.Extensions.Options;

namespace LeadFlow.Infrastructure.Telegram;

public static class TelegramServiceCollectionExtensions
{
    public static IServiceCollection AddLeadNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TelegramOptions.SectionName);
        var options = section.Get<TelegramOptions>() ?? new TelegramOptions();
        services.Configure<TelegramOptions>(section);

        if (!options.IsConfigured)
        {
            services.AddSingleton<ILeadNotificationSender, DisabledLeadNotificationSender>();
            return services;
        }

        services.AddHttpClient<ILeadNotificationSender, TelegramLeadNotificationSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromSeconds(5);
        })
            .RemoveAllLoggers();

        return services;
    }
}
