using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Infrastructure.Persistence;

/// <summary>
/// The EF Core composition root for the target SQL Server database
/// (SQ-002, SQ-014). Deliberately holds no <c>DbSet&lt;T&gt;</c> properties
/// yet — no domain entity has been built by a backlog item. Each future
/// item that introduces an entity adds its own DbSet and its own
/// migration; this class only proves the connection/migration mechanism
/// itself is wired and working (see the "migrations" pillar and the
/// db-connect / migrations-infra smoke checks).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
