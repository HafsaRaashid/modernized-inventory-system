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
}

/// <summary>
/// Request body for <see cref="RoomsController.Create"/>.
/// </summary>
public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
}
