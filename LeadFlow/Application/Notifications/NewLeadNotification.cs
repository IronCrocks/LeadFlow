namespace LeadFlow.Application.Notifications;

public sealed record NewLeadNotification(
    string Name,
    string Phone,
    string Service,
    string? CustomerComment);
