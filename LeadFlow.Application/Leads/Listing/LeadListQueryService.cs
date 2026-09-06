using LeadFlow.Domain.Entities;
using LeadFlow.Domain.Enums;
using LeadFlow.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeadFlow.Application.Leads;

public sealed class LeadListQueryService(IApplicationDbContext dbContext) : ILeadListQueryService
{
    public async Task<IReadOnlyList<LeadListItem>> GetAsync(
        LeadStatus? status,
        LeadListSortOrder sortOrder,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Lead> query = dbContext.Leads.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(lead => lead.Status == status);
        }

        query = sortOrder == LeadListSortOrder.OldestFirst
            ? query.OrderBy(lead => lead.CreatedAtUtc).ThenBy(lead => lead.Id)
            : query.OrderByDescending(lead => lead.CreatedAtUtc).ThenByDescending(lead => lead.Id);

        return await query
            .Select(lead => new LeadListItem(
                lead.Id,
                lead.Name,
                lead.Phone,
                lead.Service,
                lead.CreatedAtUtc,
                lead.Status))
            .ToListAsync(cancellationToken);
    }
}
