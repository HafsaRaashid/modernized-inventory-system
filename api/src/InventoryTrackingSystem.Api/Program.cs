using InventoryTrackingSystem.Api.Middleware;
using InventoryTrackingSystem.Infrastructure.Auth;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
// singletons. No authentication scheme/middleware is registered here — no
// request is gated on the issued token yet; that enforcement is a future
// backlog item.

builder.Services.AddSingleton<PasswordHasherService>();
builder.Services.AddSingleton<JwtTokenService>();

var app = builder.Build();

// (error-handling pillar) Every unhandled exception is caught, logged, and
// reshaped into a stable envelope here — no business rule is decided in
// this middleware.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// (cors pillar — absent-by-decision) SQ-003 decided a self-hosted, on-prem,
// single-tenant deployment. In development the Vite dev server proxies
// /api/* to this process (see web/vite.config.ts); in production this API
// serves the SPA's own build output. Both paths are same-origin from the
// browser's point of view, so no CORS policy is registered here.

app.MapControllers();

app.Run();

// Enables WebApplicationFactory<Program>-style integration testing later,
// without this foundation writing that test itself.
public partial class Program
{
}
