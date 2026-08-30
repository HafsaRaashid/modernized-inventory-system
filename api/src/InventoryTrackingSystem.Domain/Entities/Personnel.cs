namespace InventoryTrackingSystem.Domain.Entities;

/// <summary>
/// The migrated shape of the legacy `tblPersonel` table.
/// <see cref="FirstName"/> maps the legacy `PersonelAdi` column and
/// <see cref="LastName"/> maps the legacy `PersonelSoyadi` column.
/// </summary>
public class Personnel
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}
