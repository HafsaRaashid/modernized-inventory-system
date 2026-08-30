# Tasks: BL-010 — Stock / Asset Update

**Change:** stock-update
**Created:** 2026-08-30
**Total Tasks:** 5

## Summary

3 waves. Wave 1 builds the two independent tracks (backend controller extension, frontend screen + API client) in parallel — no new entities/migration needed since this item extends BL-009's existing schema. Wave 2 wires the `/stock-update` route (trivial, applied directly, not spawned). Wave 3 is tests.

## Tasks

### Wave 1 — Independent backend and frontend work

- [x] `T1` — Extend FixedAssetsController with List and Update actions
  - Files: `api/src/InventoryTrackingSystem.Api/Controllers/FixedAssetsController.cs`
  - Estimate: medium
  - Kind: impl
  - Notes: Add two new actions to the EXISTING `FixedAssetsController` (do not create a new controller or touch its existing `Create` action/route/admin-gating). `[HttpGet] List()`: admin check via `IsCallerAdminAsync` (same as `Create`), then `return Ok(await _db.FixedAssets.Select(a => new {id = a.Id, name = a.Name, price = a.Price, purchaseDate = a.PurchaseDate, assetTypeId = a.AssetTypeId, quantity = a.Quantity}).ToListAsync());`. `[HttpPut] Update([FromBody] UpdateFixedAssetRequest request)`: admin check first; if `string.IsNullOrWhiteSpace(request.Name)` return `BadRequest(new {error = "ASSET_NAME_REQUIRED", message = "Demirbaş adı gereklidir."})`; if no `AssetType` exists with `Id == request.AssetTypeId` return `BadRequest(new {error = "INVALID_ASSET_TYPE", message = "Geçersiz demirbaş türü."})`; find `var asset = await _db.FixedAssets.SingleOrDefaultAsync(a => a.Id == request.Id);` — if `null` return `NotFound(new {error = "ASSET_NOT_FOUND", message = "Demirbaş bulunamadı."})`; otherwise set `asset.Name = request.Name.Trim(); asset.Price = request.Price; asset.PurchaseDate = request.PurchaseDate; asset.AssetTypeId = request.AssetTypeId; asset.Quantity = request.Quantity;`, wrap `await _db.SaveChangesAsync();` in `try/catch (DbUpdateException) { return Conflict(new {error = "DUPLICATE_ASSET_NAME", message = "Kayıtlı Demirbaş..."}); }` (mirror `RoomsController.Update`'s exact try/catch shape — note this endpoint's own duplicate-check must exclude the record's own id, which is a property of the interceptor used in tests, not this controller code, since the controller itself has no pre-check query), then `return Ok(new {id = asset.Id, name = asset.Name, price = asset.Price, purchaseDate = asset.PurchaseDate, assetTypeId = asset.AssetTypeId, quantity = asset.Quantity});`. Declare `UpdateFixedAssetRequest` (`int Id`, `string Name`, `decimal Price`, `DateTime PurchaseDate`, `int AssetTypeId`, `int Quantity`) as a plain POCO after the controller, alongside the existing `CreateFixedAssetRequest`.

- [x] `T2` — Stock Update frontend screen + API client additions
  - Files: `web/src/api/fixedAssets.ts`, `web/src/routes/StockUpdate.tsx`, `web/src/routes/StockUpdate.css`
  - Estimate: medium
  - Kind: impl
  - Notes: In `fixedAssets.ts` (modify — ADD to the existing file, do not touch `createFixedAsset`/the `FixedAsset` interface): add `listFixedAssets(): Promise<FixedAsset[]>` calling `apiFetch("/fixed-assets")`, and `updateFixedAsset(id: number, name: string, price: number, purchaseDate: string, assetTypeId: number, quantity: number): Promise<FixedAsset>` calling `apiFetch("/fixed-assets", {method: "PUT", body: {id, name, price, purchaseDate, assetTypeId, quantity}})`. `StockUpdate.tsx` (SCR-009 layout, mirror `RoomUpdate.tsx`'s overall structure — message-state pattern, `useNavigate()` back-to-`/admin` pattern, `loadAssets()`/re-fetch-after-success pattern — but generalize from Room Update's single-field edit to five fields): a `<select>` (id `asset-select`) populated via `listFixedAssets()`/`listAssetTypes()` (both on mount) listing assets by name; selecting one populates local state for name/price/purchaseDate/assetTypeId/quantity from the already-fetched list (no extra API call per selection). Below the selector: asset-name text input — **WITH a letter-only keypress filter this time (DR-006/FR-4/AC-5) — restrict to letters, backspace, and comma; this is the OPPOSITE of Stock Add's screen, which deliberately has NO letter filter — do not copy Stock Add's unfiltered name input here.** Price input (digit/comma-only filter, same as Stock Add). Purchase-date `<input type="date">`. Asset-type `<select>` (from `listAssetTypes()`, same options pattern as Stock Add) with its disabled ID-echo field. Quantity input (digit/comma-only filter). Submit button ("GÜNCELLE") disabled unless an asset is selected AND name (trimmed)/price/quantity are all non-empty. Back button navigates to `/admin`. On submit: call `updateFixedAsset(selectedId, name.trim(), parsedPrice, purchaseDate, Number(assetTypeId), parsedQuantity)` (parse price/quantity the same comma-to-period way Stock Add does); on success, reset the selector and all fields, re-fetch the asset list (mirror `RoomUpdate.tsx`'s `loadRooms()` re-fetch-after-success call), show "Demirbaş başarıyla güncellendi." On ANY error (do not branch on status code — mirror `RoomUpdate.tsx`'s single-catch-block pattern, not `RoomAdd.tsx`'s status-branching pattern): show "Güncellenirken hata oluştu..." Do not wire the `/stock-update` route yet — that's T3.

### Wave 2 — Frontend route wiring (depends on T2)

- [x] `T3` — Register the /stock-update route
  - Files: `web/src/App.tsx`
  - Estimate: small
  - Kind: impl
  - Depends: T2
  - Notes: Add `<Route path="/stock-update" element={<RequireAdmin><StockUpdate /></RequireAdmin>} />` alongside the existing `/stock-add`/`/room-add`/`/room-update`/`/room-delete` routes, importing `StockUpdate` from `./routes/StockUpdate`. Do NOT modify `RequireAdmin`, `RequireAuth`, or any existing route.

### Wave 3 — Tests

- [x] `T4` — Backend tests for FixedAssets List and Update actions
  - Files: `api/tests/InventoryTrackingSystem.Api.Tests/FixedAssetsControllerTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T1
  - Notes: Modify the EXISTING test file (do not touch its existing `Create_*` tests). Add: AC-11 happy-path `GET /api/fixed-assets` as admin (seed 1-2 assets, assert 200 + rows appear) and non-admin → 403. AC-3 happy-path `PUT /api/fixed-assets` as admin with a valid edit → 200, body echoes the new values. AC-6 blank name → 400 `ASSET_NAME_REQUIRED`, record unchanged (assert via a fresh scope that the DB row's `Name` is still the original). AC-7 unknown `assetTypeId` → 400 `INVALID_ASSET_TYPE`. AC-8: seed TWO assets ("A", "B"); renaming "A" to "B" → 409 `DUPLICATE_ASSET_NAME`; renaming "A" to its OWN current name ("A") → 200 (no-op rename succeeds, proving the duplicate check excludes the record's own id). AC-9 unknown `id` → 404 `ASSET_NOT_FOUND`. **You must extend the existing `DuplicateFixedAssetNameSimulatingInterceptor` (currently checks only `EntityState.Added`) to also check `EntityState.Modified`, excluding the candidate's own `Id` from the duplicate query** — mirror `RoomsControllerTests`'s `DuplicateRoomNameSimulatingInterceptor`, which already does exactly this (`.Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)` and `r.Name == candidate.Name && r.Id != candidate.Id`) for the analogous BL-006 fix. Non-admin `PUT` → 403.

- [x] `T5` — Frontend tests for StockUpdate screen and the /stock-update route guard
  - Files: `web/tests/StockUpdate.test.tsx`, `web/tests/App.test.tsx`
  - Estimate: medium
  - Kind: test
  - Depends: T2, T3
  - Notes: `StockUpdate.test.tsx` (new, mirror `RoomUpdate.test.tsx`'s `vi.mock` pattern, plus mock `../src/api/assetTypes` and the `listFixedAssets`/`updateFixedAsset` exports of `../src/api/fixedAssets`): AC-1 (renders selector + all fields + GÜNCELLE button), AC-2 (selecting an asset populates all fields with its current values), AC-3 (editing and submitting calls `updateFixedAsset` with the right args, shows success, resets), AC-4 (GÜNCELLE stays disabled/no-op when no asset selected or a required field is empty), AC-5 (typing a digit into the asset-name field's keypress handler does not add it to the field's value — simulate a keydown with a non-letter key and assert the value is unchanged, mirroring how any existing keypress-filter test in this codebase asserts blocked characters), AC-10 (a rejected `updateFixedAsset` call shows "Güncellenirken hata oluştu..." regardless of the rejection's shape — test with at least a generic Error, no status-branching to verify since there is none), AC-14 (back button navigates to `/admin`). In `App.test.tsx` (modify, same pattern as existing `/stock-add` tests; mock `listFixedAssets` to resolve `[]`): AC-12 (no token → `/stock-update` → Login), AC-13 (token, `isAdmin: false` → `/stock-update` → redirected to `/`), and a happy-path admin test (token, `isAdmin: true` → `/stock-update` → renders StockUpdate content, e.g. the "GÜNCELLE" button). AC-15: re-run the full `npx vitest run` suite to confirm every pre-existing test still passes.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
