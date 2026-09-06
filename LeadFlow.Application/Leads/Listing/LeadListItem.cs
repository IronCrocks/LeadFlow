using LeadFlow.Domain.Enums;

namespace LeadFlow.Application.Leads;

public sealed record LeadListItem(
    Guid Id,
    string Name,
    string Phone,
    string Service,
    DateTimeOffset CreatedAtUtc,
    LeadStatus Status);
