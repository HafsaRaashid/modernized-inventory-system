using InventoryTrackingSystem.Domain.Entities;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Assigns a fixed asset (with a quantity) to a room for the Asset Assignment
/// screen (BL-011). This is a separate controller from
/// <see cref="RoomAssignmentsController"/> — even though both write to the
/// same underlying <see cref="RoomAssetAssignment"/> table — because the two
/// screens are a different capability: <see cref="RoomAssignmentsController"/>
/// assigns a room to a responsible person, while this controller assigns an
/// asset (decrementing its stock) to a room that already has a responsible
/// person on file. CQ-007 decided this dual-write-to-one-table shape rather
/// than splitting the legacy `tblOdaDemirbasAtama` table into two. Like
/// <see cref="RoomAssignmentsController"/>, this is authenticated-only
/// (<see cref="AuthorizeAttribute"/>) with no admin check, since this screen
/// is reached from the Main Menu, not the Admin Panel. Uses an explicit
/// hyphenated route rather than the default <c>[controller]</c> token:
/// <c>AssetAssignments</c> is a compound word, and the default token would
/// not hyphenate it to match the frontend's <c>/api/asset-assignments</c>.
/// <see cref="Create"/> calls <see cref="AppDbContext.SaveChangesAsync"/>
/// exactly once, after both the new assignment row and the asset's
/// decremented <see cref="FixedAsset.Quantity"/> have been queued on the
/// same tracked <see cref="AppDbContext"/> instance, instead of wrapping the
/// two writes in an explicit transaction: a single <c>SaveChangesAsync</c>
/// call is already atomic (both writes commit or neither does), and the
/// EF Core InMemory provider used by this project's test suite does not
/// support <c>Database.BeginTransactionAsync</c> — see the "Architecture"
/// section of .specclaw/changes/asset-assignment/design.md.
/// </summary>
[ApiController]
[Route("api/asset-assignments")]
[Authorize]
public class AssetAssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AssetAssignmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetAssignmentRequest request)
    {
        if (request.RoomId is null || request.AssetId is null)
        {
            return BadRequest(new { error = "SELECTION_REQUIRED", message = "Oda ve demirbaş seçilmelidir." });
        }

        if (request.Quantity is null || request.Quantity <= 0)
        {
            return BadRequest(new { error = "QUANTITY_REQUIRED", message = "Miktar gereklidir." });
        }

        var room = await _db.Rooms.FindAsync(request.RoomId);
        if (room is null)
        {
            return BadRequest(new { error = "INVALID_ROOM", message = "Geçersiz oda." });
        }

        var asset = await _db.FixedAssets.FindAsync(request.AssetId);
        if (asset is null)
        {
            return BadRequest(new { error = "INVALID_ASSET", message = "Geçersiz demirbaş." });
        }

        if (request.Quantity > asset.Quantity)
        {
            return BadRequest(new { error = "INSUFFICIENT_STOCK", message = "Girilen değer stok miktarından fazla.Daha az bir değer giriniz..." });
        }

        var responsibility = await _db.RoomAssetAssignments
            .Where(a => a.RoomId == request.RoomId && a.PersonnelId != null && a.AssetId == null)
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();
        if (responsibility is null)
        {
            return BadRequest(new { error = "NO_RESPONSIBLE_PERSONNEL", message = "Bu odaya sorumlu personel atanmamış." });
        }

        var assignment = new RoomAssetAssignment
        {
            RoomId = request.RoomId,
            AssetId = request.AssetId,
            Quantity = request.Quantity,
            PersonnelId = responsibility.PersonnelId,
        };
        _db.RoomAssetAssignments.Add(assignment);
        asset.Quantity -= request.Quantity.Value;

        await _db.SaveChangesAsync();

        return Created(string.Empty, new
        {
            id = assignment.Id,
            roomId = assignment.RoomId,
            assetId = assignment.AssetId,
            personnelId = assignment.PersonnelId,
            quantity = assignment.Quantity,
            remainingStock = asset.Quantity,
        });
    }

    /// <summary>
    /// Lists the assets currently assigned to a room for the Asset
    /// Assignment screen's per-room list, joining each
    /// <see cref="RoomAssetAssignment"/> row to its <see cref="FixedAsset"/>
    /// for display. Only rows with a non-null <see cref="RoomAssetAssignment.AssetId"/>
    /// qualify — a null <c>AssetId</c> is a responsibility row written by
    /// <see cref="RoomAssignmentsController"/>, not an asset assignment.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int roomId)
    {
        var assignments = await _db.RoomAssetAssignments
            .Where(a => a.RoomId == roomId && a.AssetId != null)
            .Join(_db.FixedAssets, a => a.AssetId, f => f.Id, (a, f) => new { id = a.Id, assetId = a.AssetId, assetName = f.Name, quantity = a.Quantity })
            .ToListAsync();

        return Ok(assignments);
    }
}

/// <summary>
/// Request body for <see cref="AssetAssignmentsController.Create"/>.
/// </summary>
public class CreateAssetAssignmentRequest
{
    public int? RoomId { get; set; }

    public int? AssetId { get; set; }

    public int? Quantity { get; set; }
}
