using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InventoryTrackingSystem.Infrastructure.Auth;

/// <summary>
/// Issues signed JWTs (<c>System.IdentityModel.Tokens.Jwt</c>) carrying the
/// authenticated username as the subject claim, replacing the legacy
/// static-field identity carrier (SQ-004). The signing key is read from
/// configuration (<c>Jwt:SigningKey</c>) following the existing
/// <c>ConnectionStrings:Default</c> convention of an empty checked-in
/// placeholder filled via <c>dotnet user-secrets</c> in development.
/// </summary>
public class JwtTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    private readonly string _signingKey;
    private readonly string? _issuer;

    /// <summary>
    /// Reads and validates <c>Jwt:SigningKey</c>/<c>Jwt:Issuer</c> from
    /// <paramref name="configuration"/>. Throws at construction time (not at
    /// first call) if the signing key is missing/empty or shorter than 32
    /// bytes when UTF8-encoded — matching design.md's "fail loud and early"
    /// mitigation for an insecure or empty signing key.
    /// </summary>
    public JwtTokenService(IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrEmpty(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing or shorter than 32 bytes. Set it via dotnet user-secrets in development.");
        }

        _signingKey = signingKey;
        _issuer = configuration["Jwt:Issuer"];
    }

    /// <summary>
    /// Creates a JWT with <paramref name="username"/> as the <c>sub</c>
    /// claim, signed with HMAC-SHA256 using the configured signing key, with
    /// an 8-hour expiry and the configured issuer.
    /// </summary>
    public string IssueToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
        };

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _issuer,
            claims: claims,
            notBefore: now,
            expires: now.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
