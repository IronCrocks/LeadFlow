using LeadFlow.Application.Leads;
using LeadFlow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LeadFlow.Controllers;

public sealed class LeadsController(
    ILeadCreationService leadCreationService,
    IOptions<LeadFlowOptions> options) : Controller
{
    [HttpPost("leads")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Lead")] CreateLeadViewModel model,
        CancellationToken cancellationToken)
    {
        model = model.Normalize();
        ModelState.Clear();
        TryValidateModel(model, "Lead");

        var services = GetServices();
        var selectedService = model.Service;

        if (model.SubmissionId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Не удалось обработать форму. Обновите страницу и повторите попытку.");
        }

        if (!services.Contains(selectedService, StringComparer.Ordinal))
        {
            ModelState.AddModelError("Lead.Service", "Выберите услугу из списка.");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Home/Index.cshtml", new LandingPageViewModel
            {
                Lead = model,
                Services = services
            });
        }

        await leadCreationService.CreateAsync(
            new CreateLeadCommand(
                model.SubmissionId,
                model.Name,
                model.Phone,
                model.Email,
                selectedService,
                model.CustomerComment),
            cancellationToken);

        return RedirectToAction(nameof(Success));
    }

    [HttpGet("leads/success")]
    public IActionResult Success() => View();

    private IReadOnlyList<string> GetServices()
    {
        return options.Value.Services
            .Select(service => service.Trim())
            .Where(service => !string.IsNullOrWhiteSpace(service))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
