namespace InventoryTrackingSystem.Domain.Entities;

/// <summary>
/// The migrated shape of the legacy `tblDemirbasTurleri` table.
/// <see cref="Name"/> maps the legacy `DemirbasTuruAdi` column.
/// </summary>
public class AssetType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
