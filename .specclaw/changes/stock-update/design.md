# Design: BL-010 — Stock / Asset Update

**Change:** stock-update
**Created:** 2026-08-30

## Technical Approach

Extend rather than recreate: add `List` and `Update` actions to the existing `FixedAssetsController.cs` (BL-009), add corresponding client functions to the existing `web/src/api/fixedAssets.ts`, and build a new `StockUpdate.tsx` screen mirroring `RoomUpdate.tsx`'s row-selection-populates-fields pattern, generalized to five fields instead of one. No new entities, no new migration.

## Architecture

No new architectural pattern. Reuses:
- `FixedAsset`/`AssetType` entities and `AppDbContext` registration (BL-009, unchanged).
- `AdminAuthorizationExtensions.IsCallerAdminAsync` (existing) — both new actions are admin-gated, same as `Create`/`AssetTypesController.List`.
- `RequireAdmin` (existing) — one more route wrapped in it.
- The unique-index + `DbUpdateException`-catch pattern, extended to exclude the record's own id from the duplicate check — same approach `RoomsController.Update` already uses for `Room.Name` renames.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Api/Controllers/FixedAssetsController.cs` | modify | Add `List` (`GET`) and `Update` (`PUT`) actions + `UpdateFixedAssetRequest` POCO |
| `web/src/api/fixedAssets.ts` | modify | Add `listFixedAssets()` and `updateFixedAsset(...)` |
| `web/src/routes/StockUpdate.tsx` | create | The screen, mirrors `RoomUpdate.tsx`'s pattern generalized to 5 fields |
| `web/src/routes/StockUpdate.css` | create | Mirrors `RoomUpdate.css` |
| `web/src/App.tsx` | modify | Add `/stock-update` route wrapped in `RequireAdmin`, import `StockUpdate` |
| `api/tests/InventoryTrackingSystem.Api.Tests/FixedAssetsControllerTests.cs` | modify | Add tests for `List`/`Update` (AC-6/7/8/9/11) |
| `web/tests/StockUpdate.test.tsx` | create | AC-1/2/3/4/5/10/14 |
| `web/tests/App.test.tsx` | modify | AC-12/13/15 |

## Data Model Changes

None — reuses `FixedAsset`/`AssetType` exactly as BL-009 created them. No migration.

## API Changes

- **`GET /api/fixed-assets`** — admin-gated. Returns `[{id, name, price, purchaseDate, assetTypeId, quantity}, ...]` for all rows. Mirrors `RoomsController.List`'s shape and gating.
- **`PUT /api/fixed-assets`** — admin-gated. Body `{id, name, price, purchaseDate, assetTypeId, quantity}`.
  - 400 `ASSET_NAME_REQUIRED` if `name` blank.
  - 400 `INVALID_ASSET_TYPE` if `assetTypeId` matches no `AssetType`.
  - 404 `ASSET_NOT_FOUND` if no `FixedAsset` matches `id`.
  - 409 `DUPLICATE_ASSET_NAME` on unique-index violation (via `DbUpdateException`), when the new name collides with a DIFFERENT row's name.
  - 200, body `{id, name, price, purchaseDate, assetTypeId, quantity}`.

## Key Decisions

- **ID-keyed, not name-keyed** — unlike `RoomsController.Update`/`Delete` (which match by `Name` per CQ-004's legacy-parity decision), this item's legacy screen is already correctly ID-keyed (`WHERE DemirbasID=@demirbasID`), so `Update`'s request carries `id`, matches `RoomsController`'s general REST shape more directly, and needs no parity workaround.
- **Duplicate-check must exclude the record's own id** — the existing `Room.Name` interceptor pattern (from BL-006) already established this exact shape (`AnyAsync(r => r.Name == candidate.Name && r.Id != candidate.Id)`) for the InMemory-provider test double; `FixedAssetsControllerTests`'s existing `DuplicateFixedAssetNameSimulatingInterceptor` (added in BL-009, currently `Added`-only) must be extended to also check `EntityState.Modified`, mirroring BL-006's exact fix to `DuplicateRoomNameSimulatingInterceptor`.
- **Single generic error message on the frontend, no per-status branching** — FR-8 mirrors `RoomUpdate.tsx`'s existing pattern (one message for both 409 and 404), but with Stock Update's own distinct legacy string ("Güncellenirken hata oluştu...") rather than reusing Room Update's "Hatalı İşlem...".
- **Letter-only filter IS added on this screen's name field** (DR-006, FR-4/AC-5) — the opposite of Stock Add's deliberate omission. This is not an inconsistency: DR-006 documents that the legacy Update screen's filter genuinely IS wired, unlike Add's.

## Risks & Mitigations

- **Risk:** Copying Stock Add's "no letter filter" note by habit and omitting DR-006's filter here too — **Mitigation:** task notes explicitly state this screen's letter-filter IS required, calling out the contrast with Stock Add by name.
- **Risk:** Forgetting to exclude the record's own id from the duplicate-name check, causing every no-op rename to spuriously 409 — **Mitigation:** design and task notes cite `RoomsController.Update`'s exact existing pattern (`&& r.Id != candidate.Id`) as the template to copy, and the extended `DuplicateFixedAssetNameSimulatingInterceptor` mirrors BL-006's exact fix to its Room counterpart.
- **Risk:** EF Core InMemory provider not enforcing uniqueness (known issue, BL-005/006/009) — **Mitigation:** extend the existing `DuplicateFixedAssetNameSimulatingInterceptor` (BL-009) to also check `Modified` state, exactly as `DuplicateRoomNameSimulatingInterceptor` was extended in BL-006.
