using InventoryTrackingSystem.Api.Authorization;
using InventoryTrackingSystem.Domain.Entities;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryTrackingSystem.Api.Controllers;

/// <summary>
/// Creates fixed assets for the admin-only fixed-asset-add workflow.
/// Replaces the legacy `DEMIRBAS_EKLEME_EKRANI` screen's add-asset flow:
/// only admins (<see cref="AdminAuthorizationExtensions.IsCallerAdminAsync"/>)
/// may add a fixed asset, the name must be non-blank, the asset type must
/// exist, and the asset name must be unique — enforced here via the
/// database's unique index on <see cref="FixedAsset.Name"/> rather than a
/// pre-check query, mirroring <see cref="RoomsController.Create"/>'s
/// duplicate-name pattern. Uses an explicit hyphenated route rather than the
/// default <c>[controller]</c> token: <c>FixedAssetsController</c> is a
/// compound word ("FixedAssets"), and the default token does not hyphenate
/// it to match the frontend's <c>/api/fixed-assets</c> — the same bug class
/// that previously broke <c>RoomAssignmentsController</c>.
/// </summary>
[ApiController]
[Route("api/fixed-assets")]
[Authorize]
public class FixedAssetsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FixedAssetsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetRequest request)
    {
        if (!await this.IsCallerAdminAsync(_db))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "ASSET_NAME_REQUIRED", message = "Demirbaş adı gereklidir." });
        }

        if (!await _db.AssetTypes.AnyAsync(t => t.Id == request.AssetTypeId))
        {
            return BadRequest(new { error = "INVALID_ASSET_TYPE", message = "Geçersiz demirbaş türü." });
        }

        var asset = new FixedAsset
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            PurchaseDate = request.PurchaseDate,
            AssetTypeId = request.AssetTypeId,
            Quantity = request.Quantity,
        };
        _db.FixedAssets.Add(asset);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "DUPLICATE_ASSET_NAME", message = "Kayıtlı Demirbaş..." });
        }

        return Created(string.Empty, new
        {
            id = asset.Id,
            name = asset.Name,
            price = asset.Price,
            purchaseDate = asset.PurchaseDate,
            assetTypeId = asset.AssetTypeId,
            quantity = asset.Quantity,
        });
    }
}

/// <summary>
/// Request body for <see cref="FixedAssetsController.Create"/>.
/// </summary>
public class CreateFixedAssetRequest
{
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime PurchaseDate { get; set; }

    public int AssetTypeId { get; set; }

    public int Quantity { get; set; }
}
