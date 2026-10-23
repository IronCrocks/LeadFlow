using LeadFlow.Domain.Enums;

namespace LeadFlow.Domain.Entities;

public sealed class Lead
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public string Name { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string Service { get; set; } = null!;

    public string? CustomerComment { get; set; }

    public string? ManagerComment { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadSource Source { get; set; } = LeadSource.Website;

    public ICollection<LeadStatusHistory> StatusHistory { get; } = new List<LeadStatusHistory>();
}
