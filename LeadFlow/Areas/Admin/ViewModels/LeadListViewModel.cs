using LeadFlow.Application.Leads;
using LeadFlow.Domain.Enums;

namespace LeadFlow.Areas.Admin.ViewModels;

public sealed class LeadListViewModel
{
    public LeadStatus? Status { get; init; }

    public LeadListSortOrder SortOrder { get; init; } = LeadListSortOrder.NewestFirst;

    public IReadOnlyList<LeadListItemViewModel> Leads { get; init; } = [];
}
