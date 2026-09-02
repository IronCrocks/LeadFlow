namespace LeadFlow.Areas.Admin.ViewModels;

public sealed record LeadListItemViewModel(
    Guid Id,
    string Name,
    string Phone,
    string Service,
    DateTimeOffset CreatedAt,
    string Status);
