using LeadFlow.Application.Notifications;

namespace LeadFlow.Infrastructure.Telegram;

public sealed class DisabledLeadNotificationSender : ILeadNotificationSender
{
    public Task SendNewLeadAsync(NewLeadNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
