namespace InventoryTrackingSystem.Domain.Entities;

/// <summary>
/// The migrated shape of the legacy `tblOdaDemirbasAtama` table.
/// This is a deliberately shared, mixed-purpose table: a row either
/// assigns a room (<see cref="RoomId"/>) to a person (<see cref="PersonnelId"/>),
/// or — for a later backlog item (BL-011), not yet built — assigns an
/// asset (<see cref="AssetId"/>) with a <see cref="Quantity"/> to a room.
/// All four columns are nullable to accommodate both row shapes in the
/// same table. This backlog item (BL-008) only ever populates
/// <see cref="RoomId"/> and <see cref="PersonnelId"/>; <see cref="AssetId"/>
/// and <see cref="Quantity"/> are unused by this item.
/// </summary>
public class RoomAssetAssignment
{
    public int Id { get; set; }

    public int? RoomId { get; set; }

    public int? PersonnelId { get; set; }

    public int? AssetId { get; set; }

    public int? Quantity { get; set; }
}
