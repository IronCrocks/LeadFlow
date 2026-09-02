using LeadFlow.Domain.Enums;

namespace LeadFlow.Application.Leads;

public sealed record LeadStatusHistoryItem(
    LeadStatus? PreviousStatus,
    LeadStatus NewStatus,
    DateTimeOffset ChangedAtUtc,
    string? ChangedByEmail);
