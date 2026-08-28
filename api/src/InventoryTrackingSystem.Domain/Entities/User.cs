namespace InventoryTrackingSystem.Domain.Entities;

/// <summary>
/// The migrated shape of the legacy `tblKullanicilar` table. Login logic
/// (BL-001) reads only <see cref="Username"/> and <see cref="PasswordHash"/>;
/// <see cref="YetkiID"/> exists on the row now but is not read or returned by
/// this change — consuming it for the admin gate is BL-003's scope.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool? YetkiID { get; set; }
}
