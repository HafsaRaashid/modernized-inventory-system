using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Lists personnel for the Room Assignment screen's responsible-person
/// selector. Authenticated-only (<see cref="AuthorizeAttribute"/>) — no
/// admin check — since the Room Assignment screen is reached from the Main
/// Menu, not the Admin Panel.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PersonnelController : ControllerBase
{
    private readonly AppDbContext _db;

    public PersonnelController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var personnel = await _db.Personnel
            .Select(p => new { id = p.Id, firstName = p.FirstName, lastName = p.LastName })
            .ToListAsync();

        return Ok(personnel);
    }
}
