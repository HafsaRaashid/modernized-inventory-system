using Microsoft.AspNetCore.Mvc;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// The foundation's one health-check endpoint (health-check pillar). It
/// exists purely to prove the ASP.NET Core hosting, DI, and routing
/// wiring works end to end — it carries no capability of its own.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
