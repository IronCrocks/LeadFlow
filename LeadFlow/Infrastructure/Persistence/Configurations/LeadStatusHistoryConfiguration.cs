using LeadFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeadFlow.Infrastructure.Persistence.Configurations;

public sealed class LeadStatusHistoryConfiguration : IEntityTypeConfiguration<LeadStatusHistory>
{
    public void Configure(EntityTypeBuilder<LeadStatusHistory> builder)
    {
        builder.ToTable("LeadStatusHistory");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(history => history.ChangedAtUtc).IsRequired();

        builder.HasIndex(history => history.LeadId);

        builder.HasOne(history => history.ChangedByUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
