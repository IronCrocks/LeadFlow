using System.Net.Http.Json;
using LeadFlow.Application.Notifications;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace LeadFlow.Infrastructure.Telegram;

public sealed class TelegramLeadNotificationSender(
    HttpClient httpClient,
    IOptions<TelegramOptions> options,
    ILogger<TelegramLeadNotificationSender> logger) : ILeadNotificationSender
{
    public async Task SendNewLeadAsync(NewLeadNotification notification, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return;
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"./bot{settings.BotToken}/sendMessage",
                new
                {
                    chat_id = settings.ChatId,
                    text = FormatMessage(notification)
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Telegram notification request failed with HTTP status {StatusCode}.",
                    (int)response.StatusCode);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Telegram notification could not be sent: {ExceptionType}.",
                exception.GetType().Name);
        }
    }

    public static string FormatMessage(NewLeadNotification notification)
    {
        var message = $"Новая заявка\n\nИмя: {notification.Name}\nТелефон: {notification.Phone}\nУслуга: {notification.Service}";

        return string.IsNullOrWhiteSpace(notification.CustomerComment)
            ? message
            : $"{message}\n\nКомментарий:\n{notification.CustomerComment}";
    }
}
