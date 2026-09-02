using LeadFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeadFlow.Infrastructure.Persistence.Configurations;

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");

        builder.HasKey(lead => lead.Id);

        builder.Property(lead => lead.SubmissionId).IsRequired();
        builder.Property(lead => lead.Name).HasMaxLength(100).IsRequired();
        builder.Property(lead => lead.Phone).HasMaxLength(50).IsRequired();
        builder.Property(lead => lead.Email).HasMaxLength(254);
        builder.Property(lead => lead.Service).HasMaxLength(150).IsRequired();
        builder.Property(lead => lead.CustomerComment).HasMaxLength(2000);
        builder.Property(lead => lead.ManagerComment).HasMaxLength(4000);
        builder.Property(lead => lead.CreatedAtUtc).IsRequired();
        builder.Property(lead => lead.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(lead => lead.Source).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(lead => new { lead.Status, lead.CreatedAtUtc });
        builder.HasIndex(lead => lead.SubmissionId).IsUnique();

        builder.HasMany(lead => lead.StatusHistory)
            .WithOne(history => history.Lead)
            .HasForeignKey(history => history.LeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
