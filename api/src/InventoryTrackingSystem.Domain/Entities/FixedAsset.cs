namespace InventoryTrackingSystem.Domain.Entities;

/// <summary>
/// The migrated shape of the legacy `tblDemirbaslar` table.
/// <see cref="Name"/> maps the legacy `DemirbasAdi` column,
/// <see cref="Price"/> maps the legacy `Fiyat` column (CQ-013: legacy `money`
/// precision 19 scale 4), <see cref="PurchaseDate"/> maps the legacy
/// `AlimTarihi` column, <see cref="AssetTypeId"/> maps the legacy
/// `DemirbasTuruID` column, and <see cref="Quantity"/> maps the legacy
/// `Adet` column.
/// </summary>
public class FixedAsset
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime PurchaseDate { get; set; }

    public int AssetTypeId { get; set; }

    public int Quantity { get; set; }
}
