using InventoryTrackingSystem.Domain.Entities;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Assigns a room to a responsible person for the Room Assignment screen
/// (BL-008). Fixes CQ-005: the legacy screen had no empty-selection guard
/// and no try/catch around its insert, so submitting with nothing selected
/// silently inserted an orphaned null row; this endpoint instead validates
/// both selections up front and returns a clear error. Authenticated-only
/// (<see cref="AuthorizeAttribute"/>) — no admin check — since this screen
/// is reached from the Main Menu, not the Admin Panel. No try/catch around
/// <see cref="AppDbContext.SaveChangesAsync"/> is needed here: unlike
/// <see cref="RoomsController"/>'s unique room name,
/// <see cref="RoomAssetAssignment"/> has no uniqueness constraint to
/// violate — this is a plain insert, not an upsert.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomAssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomAssignmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomAssignmentRequest request)
    {
        if (request.RoomId is null || request.PersonnelId is null)
        {
            return BadRequest(new { error = "SELECTION_REQUIRED", message = "Oda ve sorumlu personel seçilmelidir." });
        }

        if (!await _db.Rooms.AnyAsync(r => r.Id == request.RoomId))
        {
            return BadRequest(new { error = "INVALID_ROOM", message = "Geçersiz oda." });
        }

        if (!await _db.Personnel.AnyAsync(p => p.Id == request.PersonnelId))
        {
            return BadRequest(new { error = "INVALID_PERSONNEL", message = "Geçersiz personel." });
        }

        var assignment = new RoomAssetAssignment
        {
            RoomId = request.RoomId,
            PersonnelId = request.PersonnelId,
        };
        _db.RoomAssetAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return Created(string.Empty, new { id = assignment.Id, roomId = assignment.RoomId, personnelId = assignment.PersonnelId });
    }
}

/// <summary>
/// Request body for <see cref="RoomAssignmentsController.Create"/>.
/// </summary>
public class CreateRoomAssignmentRequest
{
    public int? RoomId { get; set; }

    public int? PersonnelId { get; set; }
}
