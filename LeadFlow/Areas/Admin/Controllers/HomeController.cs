using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeadFlow.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin")]
public sealed class HomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
