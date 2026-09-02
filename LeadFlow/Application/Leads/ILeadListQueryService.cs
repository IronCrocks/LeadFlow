using LeadFlow.Domain.Enums;

namespace LeadFlow.Application.Leads;

public interface ILeadListQueryService
{
    Task<IReadOnlyList<LeadListItem>> GetAsync(
        LeadStatus? status,
        LeadListSortOrder sortOrder,
        CancellationToken cancellationToken = default);
}
