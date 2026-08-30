# Design: BL-011 — Asset Assignment and Stock Decrement (Composite Flow)

**Change:** asset-assignment
**Created:** 2026-08-30

## Technical Approach

A new `AssetAssignmentsController` (distinct from BL-008's `RoomAssignmentsController`, though both write to the same `RoomAssetAssignment` table — CQ-007's decided dual-write, no forced single owner) with two actions: `POST` (the composite issue-and-decrement operation) and `GET ?roomId=` (per-room assignment listing). A new `/asset-assignment` React screen mirrors `RoomAssignment.tsx`'s two-selector-plus-echo pattern, extended with a third read-only panel. No new entities, no new migration — `RoomAssetAssignment.AssetId`/`Quantity` (created nullable in BL-008, unused until now) are finally populated by this item.

## Architecture

No new architectural pattern for the frontend (reuses `RequireAuth`, same as BL-008). The one new backend pattern: **a single `SaveChangesAsync()` call tracking two separate entity mutations** (a new `RoomAssetAssignment` add, an existing `FixedAsset` update) to achieve atomicity without an explicit transaction — this is necessary because the EF Core **InMemory** provider (used throughout this project's `WebApplicationFactory`-based tests) does not support `Database.BeginTransactionAsync()`, and it is also unnecessary: EF Core's `SaveChangesAsync()` already wraps every tracked change from one call in a single implicit transaction on any real relational provider (SQL Server included). **Do not call `Database.BeginTransactionAsync()`/`CommitAsync()` in this controller — it would throw against the InMemory provider and break every test.**

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Api/Controllers/AssetAssignmentsController.cs` | create | `[Authorize]` (not admin-gated); `POST api/asset-assignments`, `GET api/asset-assignments?roomId=` |
| `web/src/api/assetAssignments.ts` | create | `createAssetAssignment(...)`, `listRoomAssetAssignments(roomId)` |
| `web/src/routes/AssetAssignment.tsx` | create | The screen |
| `web/src/routes/AssetAssignment.css` | create | Styling |
| `web/src/App.tsx` | modify | Add `/asset-assignment` route wrapped in `RequireAuth`, import `AssetAssignment` |
| `api/tests/InventoryTrackingSystem.Api.Tests/AssetAssignmentsControllerTests.cs` | create | All backend ACs |
| `web/tests/AssetAssignment.test.tsx` | create | AC-1/2/3/4/5/6 |
| `web/tests/App.test.tsx` | modify | AC-16/17/18 |

## Data Model Changes

None. Reuses `RoomAssetAssignment` exactly as BL-008 created it (`Id`, `RoomId`, `PersonnelId`, `AssetId`, `Quantity`, all nullable) and `FixedAsset`/`Room` exactly as BL-009/BL-005 created them. This item is the first to populate `AssetId`/`Quantity` on `RoomAssetAssignment`.

## API Changes

- **`POST /api/asset-assignments`** — body `{roomId, assetId, quantity}`. `[Authorize]` only.
  - 400 `SELECTION_REQUIRED` if `roomId` or `assetId` is null.
  - 400 `QUANTITY_REQUIRED` if `quantity` is null or `<= 0`.
  - 400 `INVALID_ROOM` if no `Room` matches `roomId`.
  - 400 `INVALID_ASSET` if no `FixedAsset` matches `assetId`.
  - 400 `INSUFFICIENT_STOCK`, message `"Girilen değer stok miktarından fazla.Daha az bir değer giriniz..."`, if `quantity > asset.Quantity`.
  - 400 `NO_RESPONSIBLE_PERSONNEL` if no `RoomAssetAssignment` row exists for `roomId` with a non-null `PersonnelId` and a null `AssetId`.
  - 201, body `{id, roomId, assetId, personnelId, quantity, remainingStock}` (the asset's new `Quantity` after decrement, so the frontend can update its local copy without a second fetch if it wants — though this design still has the frontend re-fetch the full list per FR-8/AC-6, matching the legacy "grids refresh" behavior rather than a partial client-side patch).
- **`GET /api/asset-assignments?roomId=`** — `[Authorize]` only. Returns `[{id, assetId, assetName, quantity}, ...]` for rows where `RoomId == roomId` and `AssetId != null`, joining `FixedAsset` for the name.

## Controller Sketch (for the build task's reference — not literal final code)

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateAssetAssignmentRequest request)
{
    if (request.RoomId is null || request.AssetId is null)
        return BadRequest(new { error = "SELECTION_REQUIRED", ... });
    if (request.Quantity is null || request.Quantity <= 0)
        return BadRequest(new { error = "QUANTITY_REQUIRED", ... });

    var room = await _db.Rooms.FindAsync(request.RoomId);
    if (room is null) return BadRequest(new { error = "INVALID_ROOM", ... });

    var asset = await _db.FixedAssets.FindAsync(request.AssetId);
    if (asset is null) return BadRequest(new { error = "INVALID_ASSET", ... });

    if (request.Quantity > asset.Quantity)
        return BadRequest(new { error = "INSUFFICIENT_STOCK", message = "Girilen değer stok miktarından fazla.Daha az bir değer giriniz..." });

    var responsibility = await _db.RoomAssetAssignments
        .Where(a => a.RoomId == request.RoomId && a.PersonnelId != null && a.AssetId == null)
        .OrderByDescending(a => a.Id)
        .FirstOrDefaultAsync();
    if (responsibility is null)
        return BadRequest(new { error = "NO_RESPONSIBLE_PERSONNEL", ... });

    var assignment = new RoomAssetAssignment
    {
        RoomId = request.RoomId,
        AssetId = request.AssetId,
        Quantity = request.Quantity,
        PersonnelId = responsibility.PersonnelId,
    };
    _db.RoomAssetAssignments.Add(assignment);
    asset.Quantity -= request.Quantity;

    await _db.SaveChangesAsync(); // single call — both writes commit together

    return Created(string.Empty, new { id = assignment.Id, roomId = assignment.RoomId, assetId = assignment.AssetId, personnelId = assignment.PersonnelId, quantity = assignment.Quantity, remainingStock = asset.Quantity });
}
```

Note: no `try/catch (DbUpdateException)` is needed here — there is no uniqueness constraint on `RoomAssetAssignment` to violate (matches BL-008's `RoomAssignmentsController.Create`, which also has none).

## Key Decisions

- **One `SaveChangesAsync()` call, not an explicit transaction** — see Architecture above. This is both simpler and the only option compatible with the InMemory test provider.
- **The guard is structurally inseparable from the decrement** (NFR-2) — there is no other controller action, no other code path, that can modify `FixedAsset.Quantity` via a decrement operation. (Stock Update's `PUT /api/fixed-assets`, BL-010, can set `Quantity` directly, but that is a distinct, already-accepted admin action — a direct field edit, not this item's "decrement by an issued amount" operation — and is unaffected.)
- **`NO_RESPONSIBLE_PERSONNEL` is this item's own invented error code and message** — no legacy string is evidenced for this case (see spec.md Notes). Pick a clear, consistent-style Turkish message, e.g. `"Bu odaya sorumlu personel atanmamış."`
- **The frontend echoes names, not raw ids** — mirrors `RoomAssignment.tsx`'s exact convention (BL-008), the most recent precedent for a two-selector-plus-echo screen.
- **`AssetAssignmentsController` is a NEW, separate controller from `RoomAssignmentsController`** — not an added action on the existing one. They serve different capabilities (personnel-pairing vs. asset-issuance) reached from different Main Menu buttons, and CQ-007 already decided this table gets more than one writer; a second controller keeps that separation explicit rather than overloading one controller with two unrelated request shapes.

## Risks & Mitigations

- **Risk:** Reaching for `Database.BeginTransactionAsync()` out of habit (a natural instinct for "atomic multi-write") and breaking every InMemory-backed test — **Mitigation:** design and task notes explicitly forbid it and explain why a single `SaveChangesAsync()` call is already sufficient and correct.
- **Risk:** Forgetting AC-14's tie-break rule (most recent responsibility row) and picking an arbitrary/unordered `FirstOrDefaultAsync()` — **Mitigation:** the controller sketch above explicitly orders by `Id` descending; a dedicated test seeds two responsibility rows for one room and asserts the newer one's `PersonnelId` is used.
- **Risk:** EF Core InMemory provider's lack of true row-locking could let a test race-condition scenario pass even if the real guard logic were subtly wrong (since InMemory has no concurrent-transaction contention) — **Mitigation:** the test for AC-11 (insufficient stock) uses a single sequential request, which is sufficient to prove the guard logic itself is correct; true concurrent-race testing is out of scope per NFR-3.
- **Risk:** No UI-fidelity artifacts exist for SCR-004 (flagged since BL-004) — **Mitigation:** same posture as every prior screen, reproduced from the written layout description only.
