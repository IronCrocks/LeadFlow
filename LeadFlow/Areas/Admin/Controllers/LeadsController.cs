using LeadFlow.Application.Leads;
using LeadFlow.Areas.Admin.ViewModels;
using LeadFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace LeadFlow.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/leads")]
public sealed class LeadsController(
    ILeadListQueryService leadListQueryService,
    ILeadManagementService leadManagementService,
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var details = await leadManagementService.GetDetailsAsync(id, cancellationToken);
        return details is null ? NotFound() : View(ToViewModel(details));
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        Guid id,
        [Bind(Prefix = "Edit")] EditLeadViewModel model,
        CancellationToken cancellationToken)
    {
        model = model.Normalize();
        ModelState.Clear();
        TryValidateModel(model, "Edit");

        if (model.Status is { } status && !Enum.IsDefined(status))
        {
            ModelState.AddModelError("Edit.Status", "Выберите допустимый статус.");
        }

        if (!ModelState.IsValid)
        {
            var details = await leadManagementService.GetDetailsAsync(id, cancellationToken);
            return details is null ? NotFound() : View("Details", ToViewModel(details, model));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var changedByUserId))
        {
            return Forbid();
        }

        var updated = await leadManagementService.UpdateAsync(
            id,
            new UpdateLeadCommand(model.Status!.Value, model.ManagerComment, changedByUserId),
            cancellationToken);

        return updated ? RedirectToAction(nameof(Details), new { id }) : NotFound();
    }

    private LeadDetailsViewModel ToViewModel(LeadDetails details, EditLeadViewModel? edit = null)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

        return new LeadDetailsViewModel
        {
            Id = details.Id,
            Name = details.Name,
            Phone = details.Phone,
            Email = details.Email,
            Service = details.Service,
            CustomerComment = details.CustomerComment,
            CreatedAt = TimeZoneInfo.ConvertTime(details.CreatedAtUtc, timeZone),
            Status = GetStatusName(details.Status),
            Source = details.Source.ToString(),
            Edit = edit ?? new EditLeadViewModel
            {
                Status = details.Status,
                ManagerComment = details.ManagerComment
            },
            StatusHistory = details.StatusHistory.Select(history => new LeadStatusHistoryItemViewModel(
                history.PreviousStatus is null ? null : GetStatusName(history.PreviousStatus.Value),
                GetStatusName(history.NewStatus),
                TimeZoneInfo.ConvertTime(history.ChangedAtUtc, timeZone),
                history.ChangedByEmail)).ToArray()
        };
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
