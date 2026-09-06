namespace LeadFlow.Areas.Admin.ViewModels;

public sealed record LeadStatusHistoryItemViewModel(
    string? PreviousStatus,
    string NewStatus,
    DateTimeOffset ChangedAt,
    string? ChangedByEmail);
