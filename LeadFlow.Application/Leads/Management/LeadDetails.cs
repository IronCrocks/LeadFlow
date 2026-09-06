using LeadFlow.Domain.Enums;

namespace LeadFlow.Application.Leads;

public sealed record LeadDetails(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string Service,
    string? CustomerComment,
    string? ManagerComment,
    DateTimeOffset CreatedAtUtc,
    LeadStatus Status,
    LeadSource Source,
    IReadOnlyList<LeadStatusHistoryItem> StatusHistory);
