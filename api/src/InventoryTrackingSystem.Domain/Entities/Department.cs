namespace InventoryTrackingSystem.Domain.Entities;

/// <summary>
/// The migrated shape of the legacy `tblDepartmanlar` table.
/// <see cref="Name"/> maps the legacy `DepartmanAdi` column.
/// </summary>
public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
