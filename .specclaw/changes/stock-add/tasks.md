# Tasks: BL-009 — Stock / Asset Add

**Change:** stock-add
**Created:** 2026-08-30
**Total Tasks:** 7

## Summary

4 waves, mirroring room-add/room-assignment's proven shape. Wave 1 builds the two independent tracks (backend entities/DbContext, frontend screen) in parallel. Wave 2 generates the migration and builds the two new controllers, both depending only on Wave 1's entities. Wave 3 wires the `/stock-add` route, depending on the screen component existing. Wave 4 is tests.

## Tasks

### Wave 1 — Independent backend and frontend foundations

- [x] `T1` — FixedAsset and AssetType domain entities + AppDbContext registration
  - Files: `api/src/InventoryTrackingSystem.Domain/Entities/FixedAsset.cs`, `api/src/InventoryTrackingSystem.Domain/Entities/AssetType.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs`
  - Estimate: small
  - Kind: impl
  - Notes: `FixedAsset` has `Id`, `Name` (string, required — maps legacy `DemirbasAdi`), `Price` (`decimal`, required — maps `Fiyat`; CQ-013 decided the legacy column is `money` precision 19 scale 4, so configure `HasColumnType("decimal(19,4)")` in `OnModelCreating`), `PurchaseDate` (`DateTime`, required — maps `AlimTarihi`), `AssetTypeId` (`int`, required — maps `DemirbasTuruID`), `Quantity` (`int`, required — maps `Adet`). `AssetType` has `Id`, `Name` (string, required — maps `DemirbasTuruAdi`); no other fields, no navigation properties (mirror `Department.cs`'s minimal style). In `AppDbContext.OnModelCreating`: `modelBuilder.Entity<FixedAsset>().HasIndex(a => a.Name).IsUnique();` (same pattern as `Room.Name`), `modelBuilder.Entity<FixedAsset>().Property(a => a.Price).HasColumnType("decimal(19,4)");`, `modelBuilder.Entity<FixedAsset>().HasOne<AssetType>().WithMany().HasForeignKey(a => a.AssetTypeId);`. Register `DbSet<FixedAsset> FixedAssets` and `DbSet<AssetType> AssetTypes`.

- [x] `T2` — Stock Add frontend screen + API client
  - Files: `web/src/api/assetTypes.ts`, `web/src/api/fixedAssets.ts`, `web/src/routes/StockAdd.tsx`, `web/src/routes/StockAdd.css`
  - Estimate: medium
  - Kind: impl
  - Notes: `assetTypes.ts` exports an `AssetType` interface (`{id, name}`) and `listAssetTypes(): Promise<AssetType[]>` calling `apiFetch("/asset-types")`, matching `departments.ts`'s exact pattern. `fixedAssets.ts` exports `createFixedAsset(name: string, price: number, purchaseDate: string, assetTypeId: number, quantity: number): Promise<{id, name, price, purchaseDate, assetTypeId, quantity}>` calling `apiFetch("/fixed-assets", {method: "POST", body: {name, price, purchaseDate, assetTypeId, quantity}})`. `StockAdd.tsx` (SCR-008 layout, mirror `RoomAdd.tsx`'s structure exactly — component shape, message-state pattern, back-button pattern): a text input for asset name — **do NOT add any letter-only filter to this field, this is a deliberate faithful reproduction of a legacy defect (CQ-015/NFR-2), not an oversight**; a text input for price restricted via `onKeyPress`/`onChange` filtering to digits, backspace, and comma only (mirror DR-005's keypress-level restriction — do not parse the string as a number until submit, and when submitting replace a comma with a decimal point before converting to a JS number); a native `<input type="date">` for purchase date; a `<select>` populated via `listAssetTypes()` for asset type (option value = type id, label = type name); a disabled text input echoing the selected asset type's id (mirror `RoomAdd.tsx`'s department-ID echo field exactly); a quantity input with the same digit/comma-only keypress filter as price (parse as integer at submit time). Submit button ("EKLE") disabled unless name (trimmed) is non-empty, price non-empty, quantity non-empty, AND an asset type is selected (FR-4/AC-4) — mirror `RoomAdd.tsx`'s `canSubmit` boolean pattern. Back button navigates to `/admin` (mirror `RoomAdd.tsx` exactly, not `RoomAssignment.tsx`'s `/`). On success: call `createFixedAsset(...)`, reset all fields, show "Demirbaş başarıyla eklendi." On a 409 (`ApiError` with `status === 409`): show "Kayıtlı Demirbaş..." (mirror `RoomAdd.tsx`'s `DUPLICATE_MESSAGE` constant pattern exactly). On any other error: show a generic failure message, e.g. "Demirbaş eklenirken bir hata oluştu." Do not wire the `/stock-add` route yet — that's T5.

### Wave 2 — Migration and controllers (depend on T1)

- [x] `T3` — EF Core migration for FixedAsset and AssetType
  - Files: `api/src/InventoryTrackingSystem.Infrastructure/Migrations/20260830120000_AddFixedAssetAndAssetType.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Migrations/20260830120000_AddFixedAssetAndAssetType.Designer.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
  - Estimate: small
  - Kind: migration
  - Depends: T1
  - Notes: Generate via `dotnet ef migrations add AddFixedAssetAndAssetType` from within the `api/` directory (same tool/workflow used for every prior migration in this project — do not hand-write the migration file; the exact generated filename timestamp may differ from what's listed above, that's fine, name the files whatever `dotnet ef` actually generates). After generating, edit the generated `Up()` method to append a small `migrationBuilder.InsertData(...)` for 2-3 placeholder `AssetType` rows (e.g. "Elektronik", "Mobilya", "Ofis Malzemesi") for local dev/test only — this is the ONLY place `AssetType` rows will ever be created (CQ-012: no CRUD screen for this table, ever), mirroring exactly how the `AddRoomAndDepartment` migration seeded `Department` and the `AddPersonnelAndRoomAssetAssignment` migration seeded `Personnel`. Do NOT seed any `FixedAsset` rows — those are created only through the screen this change builds.

- [x] `T4` — FixedAssetsController and AssetTypesController
  - Files: `api/src/InventoryTrackingSystem.Api/Controllers/FixedAssetsController.cs`, `api/src/InventoryTrackingSystem.Api/Controllers/AssetTypesController.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T1
  - Notes: **Both controllers ARE admin-gated** via `AdminAuthorizationExtensions.IsCallerAdminAsync` (`if (!await this.IsCallerAdminAsync(_db)) return Forbid();`) — unlike BL-008's Personnel/RoomAssignments endpoints, this screen is reached exclusively from the Admin Panel, following `RoomsController`/`DepartmentsController`'s pattern, not BL-008's. `AssetTypesController`: `[Authorize]` + `[HttpGet]` `api/asset-types` — admin check, then return `Ok(await _db.AssetTypes.Select(t => new {id = t.Id, name = t.Name}).ToListAsync())` (mirror `DepartmentsController.List` exactly). `FixedAssetsController`: `[Authorize]` + `[HttpPost]` `api/fixed-assets` — admin check first; accept `CreateFixedAssetRequest` (`string Name`, `decimal Price`, `DateTime PurchaseDate`, `int AssetTypeId`, `int Quantity`); if `string.IsNullOrWhiteSpace(request.Name)` return `BadRequest(new {error = "ASSET_NAME_REQUIRED", message = "Demirbaş adı gereklidir."})`; if no `AssetType` exists with `Id == request.AssetTypeId` return `BadRequest(new {error = "INVALID_ASSET_TYPE", message = "Geçersiz demirbaş türü."})`; otherwise create `new FixedAsset {Name = request.Name.Trim(), Price = request.Price, PurchaseDate = request.PurchaseDate, AssetTypeId = request.AssetTypeId, Quantity = request.Quantity}`, add, and wrap `SaveChangesAsync()` in a `try/catch (DbUpdateException)` returning `Conflict(new {error = "DUPLICATE_ASSET_NAME", message = "Kayıtlı Demirbaş..."})` on conflict (mirror `RoomsController.Create`'s exact try/catch shape) — otherwise `Created(string.Empty, new {id = asset.Id, name = asset.Name, price = asset.Price, purchaseDate = asset.PurchaseDate, assetTypeId = asset.AssetTypeId, quantity = asset.Quantity})`. Follow `RoomsController`'s existing style (constructor-injected `AppDbContext`, plain POCO request class declared after the controller).

### Wave 3 — Frontend route wiring (depends on T2)

- [x] `T5` — Register the /stock-add route
  - Files: `web/src/App.tsx`
  - Estimate: small
  - Kind: impl
  - Depends: T2
  - Notes: Add `<Route path="/stock-add" element={<RequireAdmin><StockAdd /></RequireAdmin>} />` alongside the existing `/room-add`/`/room-update`/`/room-delete` routes, importing `StockAdd` from `./routes/StockAdd`. Do NOT modify `RequireAdmin`, `RequireAuth`, or any existing route — this is a pure addition, unlike BL-008's T5 which had to generalize `RequireAuth`.

### Wave 4 — Tests

- [x] `T6` — Backend tests for FixedAssets and AssetTypes endpoints
  - Files: `api/tests/InventoryTrackingSystem.Api.Tests/FixedAssetsControllerTests.cs`, `api/tests/InventoryTrackingSystem.Api.Tests/AssetTypesControllerTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T1, T3, T4
  - Notes: Follow `RoomsControllerTests.cs`'s exact `CreateFactory`/`SeedKnownUserAsync`/`SeedDepartmentAsync`/`LoginAsync` pattern, seeding a known admin user (`yetkiId: true`) via the same helper style — add a `SeedAssetTypeAsync` helper analogous to `SeedDepartmentAsync`. **Both endpoints require an admin session** — include at least one test per controller proving a non-admin (`yetkiId: false`) authenticated user gets `403 Forbidden`, mirroring how `RoomsControllerTests.cs` already proves this for `RoomsController`. `AssetTypesControllerTests.cs`: happy-path `GET /api/asset-types` as admin — seed 1-2 rows, assert `200` + rows appear; non-admin → `403`. `FixedAssetsControllerTests.cs`: AC-3 (seed an `AssetType`, `POST /api/fixed-assets` with all valid fields as admin → `201` + body echoes all fields, `price` round-trips as the same decimal value), AC-5 (blank name → `400 ASSET_NAME_REQUIRED`), AC-6 (`assetTypeId` matching no seeded row → `400 INVALID_ASSET_TYPE`), AC-7 (POST the same name twice → second returns `409 DUPLICATE_ASSET_NAME`, and a follow-up query proves only one row exists — you will need a `DbUpdateException`-simulating `SaveChangesInterceptor` for `FixedAsset.Name` in the test factory, since the EF Core InMemory provider does not enforce `HasIndex().IsUnique()`; mirror `RoomsControllerTests.cs`'s existing `DuplicateRoomNameSimulatingInterceptor` — either generalize it or add a sibling interceptor for `FixedAsset`), non-admin → `403`.

- [x] `T7` — Frontend tests for StockAdd screen and the /stock-add route guard
  - Files: `web/tests/StockAdd.test.tsx`, `web/tests/App.test.tsx`
  - Estimate: medium
  - Kind: test
  - Depends: T2, T5
  - Notes: `StockAdd.test.tsx` (new, mirror `RoomAdd.test.tsx`'s `vi.mock` pattern for `useNavigate`, plus mock `../src/api/assetTypes` (`listAssetTypes`) and `../src/api/fixedAssets` (`createFixedAsset`)): AC-1 (renders name/price/date/type-select/type-echo/quantity/submit), AC-2 (selecting a type updates the echo field), AC-3 (filling all fields and submitting calls `createFixedAsset` with the right args, shows success, resets fields), AC-4 (submit stays disabled/no-op when any of name/price/quantity/type is missing — `createFixedAsset` never called), AC-12 (back button navigates to `/admin`). In `App.test.tsx` (modify, same `getSession`/`window.history.pushState` pattern as existing admin-route tests; mock `../src/api/assetTypes`'s `listAssetTypes` to resolve `[]`): AC-10 (no token → `/stock-add` → Login), AC-11 (token, `isAdmin: false` → `/stock-add` → redirected to `/`, mirror the existing `/room-add` non-admin-redirect test), and a happy-path admin test (token, `isAdmin: true` → `/stock-add` → renders StockAdd content, e.g. the "EKLE" button). AC-13: re-run the full `npx vitest run` suite from `web/` to confirm every pre-existing `/`, `/admin`, `/room-add`, `/room-update`, `/room-delete`, `/room-assignment` test still passes unmodified.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
