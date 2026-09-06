namespace LeadFlow.Areas.Admin.ViewModels;

public sealed class LeadDetailsViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string Service { get; init; } = string.Empty;

    public string? CustomerComment { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public EditLeadViewModel Edit { get; init; } = new();

    public IReadOnlyList<LeadStatusHistoryItemViewModel> StatusHistory { get; init; } = [];
}
