using System.IdentityModel.Tokens.Jwt;
using System.Text;
using InventoryTrackingSystem.Api.Middleware;
using InventoryTrackingSystem.Infrastructure.Auth;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- DI / composition root --------------------------------------------
// (di pillar) Everything the app needs at runtime is registered here, in
// this stack's own idiom (the built-in Microsoft.Extensions.DependencyInjection
// container) — nothing framework-specific is invented on top of it.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// (persistence pillar) SQ-002 (SQL Server) + SQ-014 (EF Core). The
// connection string itself is never committed — see appsettings.json and
// bootstrap-plan.md's "Local Development Setup".
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// --- Auth boundary -------------------------------------------------------
// SQ-004's credential hashing and token issuance (BL-001). Both services
// are stateless (JwtTokenService reads IConfiguration once at construction,
// PasswordHasherService holds no state at all) so they're safe as
// singletons.

builder.Services.AddSingleton<PasswordHasherService>();
builder.Services.AddSingleton<JwtTokenService>();

// BL-003: JWT bearer authentication, validated against the same
// Jwt:SigningKey/Jwt:Issuer configuration keys JwtTokenService already signs
// tokens with. MapInboundClaims is disabled so [Authorize] actions can read
// the raw `sub` claim (JwtRegisteredClaimNames.Sub) instead of having the
// default handler rewrite it to ClaimTypes.NameIdentifier. No audience is
// issued, so ValidateAudience stays off; ValidateIssuer only turns on when
// an issuer is actually configured.
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Force JwtTokenService's construction now rather than on first login: its
// constructor validates Jwt:SigningKey and throws if it's missing/too short.
// AddSingleton alone resolves lazily, which would defer that failure to the
// first request — this line is what actually makes it fail at startup
// (design.md's "fail loud and early" mitigation for an unset signing key).
app.Services.GetRequiredService<JwtTokenService>();

// (error-handling pillar) Every unhandled exception is caught, logged, and
// reshaped into a stable envelope here — no business rule is decided in
// this middleware.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// (cors pillar — absent-by-decision) SQ-003 decided a self-hosted, on-prem,
// single-tenant deployment. In development the Vite dev server proxies
// /api/* to this process (see web/vite.config.ts); in production this API
// serves the SPA's own build output. Both paths are same-origin from the
// browser's point of view, so no CORS policy is registered here.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Enables WebApplicationFactory<Program>-style integration testing later,
// without this foundation writing that test itself.
public partial class Program
{
}
