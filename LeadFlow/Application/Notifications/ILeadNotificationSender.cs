namespace LeadFlow.Application.Notifications;

public interface ILeadNotificationSender
{
    Task SendNewLeadAsync(NewLeadNotification notification, CancellationToken cancellationToken = default);
}
