using LeadFlow.Domain.Enums;

namespace LeadFlow.Application.Leads;

public sealed record UpdateLeadCommand(LeadStatus Status, string? ManagerComment, Guid ChangedByUserId);
