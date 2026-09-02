using System.Diagnostics;
using LeadFlow.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeadFlow.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
