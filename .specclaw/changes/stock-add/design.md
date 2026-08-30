# Design: BL-009 — Stock / Asset Add

**Change:** stock-add
**Created:** 2026-08-30

## Technical Approach

Follow BL-005 (Room Add)'s exact shape one level up: a new `FixedAsset` entity + a new read-only `AssetType` lookup entity, one EF Core migration (with a dev/test seed for `AssetType`, mirroring BL-005's `Department` seed and BL-008's `Personnel` seed), two controllers (`FixedAssetsController` for create, `AssetTypesController` for the read-only lookup — both admin-gated via the existing `AdminAuthorizationExtensions.IsCallerAdminAsync`, same as `RoomsController`/`DepartmentsController`), a new `/stock-add` React screen wrapped in the existing `RequireAdmin` guard, and two new API client modules.

## Architecture

No new architectural pattern — this is the fourth admin-gated CRUD-lite screen in MOD-003/MOD-002's family (after Room Add/Update/Delete). Reuses:
- `AdminAuthorizationExtensions.IsCallerAdminAsync` (existing, `api/src/InventoryTrackingSystem.Api/Authorization/AdminAuthorizationExtensions.cs`) — no changes needed.
- `RequireAdmin` (existing, `web/src/App.tsx`) — no changes needed, just one more route wrapped in it.
- The unique-index + `DbUpdateException`-catch pattern `RoomsController.Create` already uses for `Room.Name` — applied identically to `FixedAsset.Name`.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Domain/Entities/FixedAsset.cs` | create | `Id`, `Name`, `Price` (decimal), `PurchaseDate` (DateTime), `AssetTypeId` (int), `Quantity` (int) |
| `api/src/InventoryTrackingSystem.Domain/Entities/AssetType.cs` | create | `Id`, `Name` — read-only lookup, no other fields |
| `api/src/InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs` | modify | Register `DbSet<FixedAsset> FixedAssets`, `DbSet<AssetType> AssetTypes`; add unique index on `FixedAsset.Name`; add FK `FixedAsset.AssetTypeId → AssetType` |
| `api/src/InventoryTrackingSystem.Infrastructure/Migrations/<timestamp>_AddFixedAssetAndAssetType.cs` (+ Designer + snapshot) | create | Generated via `dotnet ef migrations add`; hand-edit `Up()` to add `AssetType` seed rows |
| `api/src/InventoryTrackingSystem.Api/Controllers/FixedAssetsController.cs` | create | `[Authorize]` + admin check; `POST /api/fixed-assets` |
| `api/src/InventoryTrackingSystem.Api/Controllers/AssetTypesController.cs` | create | `[Authorize]` + admin check; `GET /api/asset-types` |
| `web/src/api/assetTypes.ts` | create | `listAssetTypes(): Promise<AssetType[]>` |
| `web/src/api/fixedAssets.ts` | create | `createFixedAsset(...)` |
| `web/src/routes/StockAdd.tsx` | create | The screen, mirrors `RoomAdd.tsx` structure |
| `web/src/routes/StockAdd.css` | create | Mirrors `RoomAdd.css` |
| `web/src/App.tsx` | modify | Add `/stock-add` route wrapped in `RequireAdmin`, import `StockAdd` |
| `api/tests/InventoryTrackingSystem.Api.Tests/FixedAssetsControllerTests.cs` | create | AC-3/5/6/7/8 |
| `api/tests/InventoryTrackingSystem.Api.Tests/AssetTypesControllerTests.cs` | create | AC-9 |
| `web/tests/StockAdd.test.tsx` | create | AC-1/2/3/4/12 |
| `web/tests/App.test.tsx` | modify | AC-10/11/13 |

## Data Model Changes

```
FixedAsset
  Id           int (PK, identity)
  Name         nvarchar(max), NOT NULL, UNIQUE INDEX
  Price        decimal(19,4), NOT NULL
  PurchaseDate datetime2, NOT NULL
  AssetTypeId  int, NOT NULL, FK -> AssetType.Id
  Quantity     int, NOT NULL

AssetType
  Id    int (PK, identity)
  Name  nvarchar(max), NOT NULL
```

`decimal(19,4)` matches CQ-013's resolved schema fact (`tblDemirbas.Fiyat` is `money`, precision 19 scale 4). Unlike `RoomAssetAssignment`'s deliberately-nullable columns, every `FixedAsset` field here is required — there is no analogous "populated by a different insert path" ambiguity for this entity.

## API Changes

- **`POST /api/fixed-assets`** — body `{name, price, purchaseDate, assetTypeId, quantity}`. Admin-gated.
  - 400 `ASSET_NAME_REQUIRED` if `name` blank.
  - 400 `INVALID_ASSET_TYPE` if `assetTypeId` matches no `AssetType`.
  - 409 `DUPLICATE_ASSET_NAME` (message "Kayıtlı Demirbaş...") on unique-index violation, caught via `DbUpdateException` exactly like `RoomsController.Create`'s `DUPLICATE_ROOM_NAME`.
  - 201, body `{id, name, price, purchaseDate, assetTypeId, quantity}`.
- **`GET /api/asset-types`** — admin-gated, returns `[{id, name}, ...]`. Same shape as `DepartmentsController.List`.

## Key Decisions

- **Both new controllers are admin-gated**, not merely `[Authorize]` — unlike BL-008's Personnel/RoomAssignments endpoints, this screen is reached exclusively from the Admin Panel (like Room Add/Update/Delete), so it follows that family's pattern, not BL-008's.
- **No letter-filter on the asset-name field** (NFR-2) — a deliberate faithful-defect reproduction (CQ-015), not an omission. The task instructions for the frontend must say this explicitly so a future reviewer doesn't "fix" it.
- **Price/quantity sent as numbers, not raw strings**, from the client — the comma-only keypress filter constrains what characters can be *typed*, matching DR-005's "keypress-level restriction only, not a value-level one"; parsing to a number at submit time (replacing a comma with a decimal point if present) is necessary to fit the `decimal`/`int` request fields and does not reintroduce value-level validation the legacy app never had.
- **Uniqueness enforced by DB constraint + catch, not a pre-check query** — identical reasoning to `Room.Name` (CQ-018): avoids a race window, and matches the legacy app's own (unbacked) duplicate-message behavior now backed by a real constraint.

## Risks & Mitigations

- **Risk:** Confusing `FixedAsset.Name` (asset name) with `AssetType.Name` (type name) during implementation, given both are just "Name" — **Mitigation:** distinct entity/table names (`FixedAsset` vs `AssetType`) and distinct property docs in the task notes.
- **Risk:** EF Core InMemory provider doesn't enforce `HasIndex().IsUnique()` (known issue from BL-005/BL-006) — **Mitigation:** reuse the existing `DuplicateRoomNameSimulatingInterceptor` pattern, generalized or duplicated for `FixedAsset.Name`, in the test project (same fix already applied twice for Room).
- **Risk:** No UI-fidelity artifacts exist for SCR-008 (flagged in rebuild-backlog.md's Gate) — **Mitigation:** same situation as every prior screen in this backlog; reproduce from the written layout description only, consistent with established precedent.
