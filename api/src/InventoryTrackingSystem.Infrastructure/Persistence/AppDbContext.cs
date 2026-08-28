using InventoryTrackingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Infrastructure.Persistence;

/// <summary>
/// The EF Core composition root for the target SQL Server database
/// (SQ-002, SQ-014).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
    }
}
