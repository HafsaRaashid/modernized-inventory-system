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

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<Room>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Room>().HasOne<Department>().WithMany().HasForeignKey(r => r.DepartmentId);
    }
}
