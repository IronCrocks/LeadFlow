namespace LeadFlow.Application.Leads;

public sealed record CreateLeadCommand(
    Guid SubmissionId,
    string Name,
    string Phone,
    string? Email,
    string Service,
    string? CustomerComment);
