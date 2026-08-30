using InventoryTrackingSystem.Api.Authorization;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Lists departments for the admin-only room-add workflow (FR-1). Only
/// admins (<see cref="AdminAuthorizationExtensions.IsCallerAdminAsync"/>)
/// may see the department list used to populate the room-add form.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (!await this.IsCallerAdminAsync(_db))
        {
            return Forbid();
        }

        var departments = await _db.Departments
            .Select(d => new { id = d.Id, name = d.Name })
            .ToListAsync();

        return Ok(departments);
    }
}
