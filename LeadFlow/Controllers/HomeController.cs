using System.Diagnostics;
using LeadFlow.Application.Leads;
using LeadFlow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LeadFlow.Controllers;

public sealed class HomeController(IOptions<LeadFlowOptions> options) : Controller
{
    public IActionResult Index() => View(new LandingPageViewModel
    {
        Lead = new CreateLeadViewModel { SubmissionId = Guid.NewGuid() },
        Services = options.Value.Services
            .Select(service => service.Trim())
            .Where(service => !string.IsNullOrWhiteSpace(service))
            .Distinct(StringComparer.Ordinal)
            .ToArray()
    });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
