using InventoryTrackingSystem.Infrastructure.Auth;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Issues a signed JWT for a matching username/password pair, replacing the
/// legacy `GİRİŞ_EKRANI` login's string-concatenated query and plaintext
/// comparison (SQ-004, CQ-010, CQ-011). There is no separate non-empty-field
/// pre-check: an empty username or password simply fails to match any row or
/// hash and falls through to the same rejection path as any other wrong
/// credential (FR-5/AC-3).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasherService _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AppDbContext db, PasswordHasherService passwordHasher, JwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new
            {
                error = "INVALID_LOGIN_CREDENTIALS",
                message = "Hatalı giriş yaptınız. Lütfen tekrar giriniz!!!",
            });
        }

        var token = _jwtTokenService.IssueToken(user.Username);

        return Ok(new
        {
            token,
            username = user.Username,
        });
    }
}

/// <summary>
/// Request body for <see cref="AuthController.Login"/>.
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
