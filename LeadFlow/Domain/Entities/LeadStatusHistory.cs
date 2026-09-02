using LeadFlow.Domain.Enums;

namespace LeadFlow.Domain.Entities;

public sealed class LeadStatusHistory
{
    public Guid Id { get; set; }

    public Guid LeadId { get; set; }

    public Lead Lead { get; set; } = null!;

    public LeadStatus? PreviousStatus { get; set; }

    public LeadStatus NewStatus { get; set; }

    public DateTimeOffset ChangedAtUtc { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public ApplicationUser? ChangedByUser { get; set; }
}
