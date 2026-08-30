namespace InventoryTrackingSystem.Domain.Entities;

/// <summary>
/// The migrated shape of the legacy `tblOda` table. <see cref="Name"/> maps
/// the legacy `OdaAdi` column and <see cref="DepartmentId"/> maps the legacy
/// `DepartmanID` foreign key column. Unlike the legacy table, <see cref="Name"/>
/// is enforced unique at the database level — the legacy app surfaced a
/// "duplicate room" error message despite no such constraint ever existing.
/// </summary>
public class Room
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
}
