using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Authorization;

/// <summary>
/// Shared admin-check helper for controllers gated to admin users only.
/// Mirrors <see cref="InventoryTrackingSystem.Api.Controllers.AuthController.Me"/>:
/// it resolves the caller from the JWT's `sub` claim and re-queries the
/// user's current `YetkiID` from the database on every call, rather than
/// trusting a cached/stale claim baked into the JWT at login time.
/// </summary>
public static class AdminAuthorizationExtensions
{
    public static async Task<bool> IsCallerAdminAsync(this ControllerBase controller, AppDbContext db)
    {
        var username = controller.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);
        return user?.YetkiID == true;
    }
}
