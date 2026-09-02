using LeadFlow.Application.Leads;
using LeadFlow.Areas.Admin.ViewModels;
using LeadFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LeadFlow.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/leads")]
public sealed class LeadsController(
    ILeadListQueryService leadListQueryService,
    IOptions<LeadFlowOptions> options) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] LeadStatus? status,
        [FromQuery] LeadListSortOrder sortOrder = LeadListSortOrder.NewestFirst,
        CancellationToken cancellationToken = default)
    {
        var leads = await leadListQueryService.GetAsync(status, sortOrder, cancellationToken);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

        return View(new LeadListViewModel
        {
            Status = status,
            SortOrder = sortOrder,
            Leads = leads.Select(lead => new LeadListItemViewModel(
                lead.Id,
                lead.Name,
                lead.Phone,
                lead.Service,
                TimeZoneInfo.ConvertTime(lead.CreatedAtUtc, timeZone),
                GetStatusName(lead.Status))).ToArray()
        });
    }

    private static string GetStatusName(LeadStatus status) => status switch
    {
        LeadStatus.New => "Новая",
        LeadStatus.InProgress => "В работе",
        LeadStatus.Completed => "Выполнена",
        LeadStatus.Rejected => "Отказ",
        _ => status.ToString()
    };
}
