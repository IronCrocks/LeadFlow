using LeadFlow.Domain.Entities;
using LeadFlow.Application.Managers;
using LeadFlow.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeadFlow.Application.Leads;

public sealed class LeadManagementService(
    IApplicationDbContext dbContext,
    IManagerDirectory managerDirectory,
    TimeProvider timeProvider)
    : ILeadManagementService
{
    public async Task<LeadDetails?> GetDetailsAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        var details = await dbContext.Leads
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
                        history.ChangedByUserId,
                        null))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        if (details is null)
        {
            return null;
        }

        var managerIds = details.StatusHistory
            .Where(history => history.ChangedByUserId is not null)
            .Select(history => history.ChangedByUserId!.Value)
            .Distinct()
            .ToArray();
        var managerEmails = await managerDirectory.GetEmailsAsync(managerIds, cancellationToken);

        return details with
        {
            StatusHistory = details.StatusHistory
                .Select(history => history with
                {
                    ChangedByEmail = history.ChangedByUserId is { } managerId &&
                                     managerEmails.TryGetValue(managerId, out var email)
                        ? email
                        : null
                })
                .ToArray()
        };
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
