using InventoryTrackingSystem.Api.Authorization;
using InventoryTrackingSystem.Domain.Entities;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Creates rooms for the admin-only room-add workflow (FR-1). Replaces the
/// legacy `ODA_EKLEME_EKRANI` screen's add-room flow: only admins
/// (<see cref="AdminAuthorizationExtensions.IsCallerAdminAsync"/>) may add a
/// room, the name must be non-blank, the department must exist, and the
/// room name must be unique — enforced here via the database's unique index
/// on <see cref="Room.Name"/> rather than a pre-check query, matching the
/// legacy app's user-facing "duplicate room" message despite the legacy
/// table never having actually enforced that constraint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomRequest request)
    {
        if (!await this.IsCallerAdminAsync(_db))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "ROOM_NAME_REQUIRED", message = "Oda adı gereklidir." });
        }

        if (!await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId))
        {
            return BadRequest(new { error = "INVALID_DEPARTMENT", message = "Geçersiz departman." });
        }

        var room = new Room { Name = request.Name.Trim(), DepartmentId = request.DepartmentId };
        _db.Rooms.Add(room);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "DUPLICATE_ROOM_NAME", message = "Kayıtlı Oda..." });
        }

        return Created(string.Empty, new { id = room.Id, name = room.Name, departmentId = room.DepartmentId });
    }

    /// <summary>
    /// Lists all rooms for the Room Update screen's existing-room selector
    /// (FR-2). Admin-gated the same way as <see cref="Create"/>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (!await this.IsCallerAdminAsync(_db))
        {
            return Forbid();
        }

        var rooms = await _db.Rooms
            .Select(r => new { id = r.Id, name = r.Name })
            .ToListAsync();

        return Ok(rooms);
    }

    /// <summary>
    /// Renames a room for the admin-only room-update workflow. Matches the
    /// room by its CURRENT name rather than its ID — a deliberate
    /// legacy-parity decision (CQ-004), not an oversight, and safe only
    /// because <see cref="Room.Name"/> is uniquely constrained. "Room not
    /// found" (404) is its own explicit pre-check, since it is a different
    /// rule from uniqueness; "duplicate name" (409) has no pre-check and is
    /// instead caught via <see cref="DbUpdateException"/>, the same
    /// single-source-of-truth pattern <see cref="Create"/> already uses for
    /// its own uniqueness constraint.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateRoomRequest request)
    {
        if (!await this.IsCallerAdminAsync(_db))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.NewName))
        {
            return BadRequest(new { error = "ROOM_NAME_REQUIRED", message = "Oda adı gereklidir." });
        }

        var room = await _db.Rooms.SingleOrDefaultAsync(r => r.Name == request.OldName);
        if (room is null)
        {
            return NotFound(new { error = "ROOM_NOT_FOUND", message = "Hatalı İşlem..." });
        }

        room.Name = request.NewName.Trim();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "DUPLICATE_ROOM_NAME", message = "Hatalı İşlem..." });
        }

        return Ok(new { id = room.Id, name = room.Name, departmentId = room.DepartmentId });
    }
}

/// <summary>
/// Request body for <see cref="RoomsController.Create"/>.
/// </summary>
public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
}

/// <summary>
/// Request body for <see cref="RoomsController.Update"/>.
/// </summary>
public class UpdateRoomRequest
{
    public string OldName { get; set; } = string.Empty;

    public string NewName { get; set; } = string.Empty;
}
