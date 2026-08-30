using InventoryTrackingSystem.Api.Authorization;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Lists asset types for the admin-only fixed-asset-add workflow. Only
/// admins (<see cref="AdminAuthorizationExtensions.IsCallerAdminAsync"/>)
/// may see the asset-type list used to populate the fixed-asset-add form.
/// Uses an explicit hyphenated route rather than the default
/// <c>[controller]</c> token: <c>AssetTypesController</c> is a compound word
/// ("AssetTypes"), and the default token does not hyphenate it to match the
/// frontend's <c>/api/asset-types</c> — the same bug class that previously
/// broke <c>RoomAssignmentsController</c>.
/// </summary>
[ApiController]
[Route("api/asset-types")]
[Authorize]
public class AssetTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AssetTypesController(AppDbContext db)
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

        var assetTypes = await _db.AssetTypes
            .Select(t => new { id = t.Id, name = t.Name })
            .ToListAsync();

        return Ok(assetTypes);
    }
}
