using LeadFlow.Domain.Entities;
using LeadFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeadFlow.Application.Leads;

public sealed class LeadManagementService(ApplicationDbContext dbContext, TimeProvider timeProvider)
    : ILeadManagementService
{
    public async Task<LeadDetails?> GetDetailsAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Leads
            .AsNoTracking()
            .Where(lead => lead.Id == leadId)
            .Select(lead => new LeadDetails(
                lead.Id,
                lead.Name,
                lead.Phone,
                lead.Email,
                lead.Service,
                lead.CustomerComment,
                lead.ManagerComment,
                lead.CreatedAtUtc,
                lead.Status,
                lead.Source,
                lead.StatusHistory
                    .OrderBy(history => history.ChangedAtUtc)
                    .ThenBy(history => history.Id)
                    .Select(history => new LeadStatusHistoryItem(
                        history.PreviousStatus,
                        history.NewStatus,
                        history.ChangedAtUtc,
                        history.ChangedByUserId == null
                            ? null
                            : dbContext.Users
                                .Where(user => user.Id == history.ChangedByUserId)
                                .Select(user => user.Email)
                                .FirstOrDefault()))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Guid leadId,
        UpdateLeadCommand command,
        CancellationToken cancellationToken = default)
    {
        var lead = await dbContext.Leads.SingleOrDefaultAsync(lead => lead.Id == leadId, cancellationToken);
        if (lead is null)
        {
            return false;
        }

        var previousStatus = lead.Status;
        lead.Status = command.Status;
        lead.ManagerComment = NormalizeOptional(command.ManagerComment);

        if (previousStatus != command.Status)
        {
            dbContext.LeadStatusHistory.Add(new LeadStatusHistory
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                PreviousStatus = previousStatus,
                NewStatus = command.Status,
                ChangedAtUtc = timeProvider.GetUtcNow(),
                ChangedByUserId = command.ChangedByUserId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
