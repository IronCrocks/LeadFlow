using LeadFlow.Domain.Entities;
using LeadFlow.Domain.Enums;
using LeadFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeadFlow.Application.Leads;

public sealed class LeadCreationService(ApplicationDbContext dbContext, TimeProvider timeProvider)
    : ILeadCreationService
{
    public async Task CreateAsync(CreateLeadCommand command, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Leads.AnyAsync(lead => lead.SubmissionId == command.SubmissionId, cancellationToken))
        {
            return;
        }

        var createdAtUtc = timeProvider.GetUtcNow();
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            SubmissionId = command.SubmissionId,
            Name = command.Name.Trim(),
            Phone = command.Phone.Trim(),
            Email = NormalizeOptional(command.Email),
            Service = command.Service.Trim(),
            CustomerComment = NormalizeOptional(command.CustomerComment),
            CreatedAtUtc = createdAtUtc,
            Status = LeadStatus.New,
            Source = LeadSource.Website
        };
        lead.StatusHistory.Add(new LeadStatusHistory
        {
            Id = Guid.NewGuid(),
            NewStatus = LeadStatus.New,
            ChangedAtUtc = createdAtUtc
        });

        try
        {
            dbContext.Leads.Add(lead);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            if (!await dbContext.Leads.AnyAsync(item => item.SubmissionId == command.SubmissionId, cancellationToken))
            {
                throw;
            }
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
