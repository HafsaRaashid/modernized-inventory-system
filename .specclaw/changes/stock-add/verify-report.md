# Verification Report: stock-add

**Verified:** 2026-08-30
**Model:** claude-sonnet-5
**Verdict:** PASS

## Acceptance Criteria

- ✅ **AC-1:** Visiting `/stock-add` as admin renders asset-name (no letter filter), price (digit/comma-only), purchase-date, asset-type `<select>`, disabled ID-echo, quantity (digit/comma-only), submit button. — `web/src/routes/StockAdd.tsx:132-224` renders all fields; `asset-name` input (134-141) has no `onKeyDown`; `price` (144-153) and `quantity` (197-207) both use `onKeyDown={handleDigitsAndCommaKeyDown}`. `StockAdd.test.tsx` AC-1 test (lines 1432-1446) passed.
- ✅ **AC-2:** Selecting an asset type updates the disabled ID-echo field. — `StockAdd.tsx` line ~172: `onChange={(event) => setAssetTypeId(event.target.value)}`, echoed via `value={assetTypeId}` on the disabled input (416-425 in original numbering). `StockAdd.test.tsx` AC-2 test passed (`fireEvent.change(select, {target:{value:"2"}})` → `expect(...).toHaveValue("2")`).
- ✅ **AC-3:** Submit with all fields calls `POST /api/fixed-assets` with `{name, price, purchaseDate, assetTypeId, quantity}`, resets fields, shows "Demirbaş başarıyla eklendi." — `StockAdd.tsx` `handleSubmit` (319-349) calls `createFixedAsset(assetName.trim(), parsedPrice, purchaseDate, Number(assetTypeId), parsedQuantity)`, then clears all state and sets `SUCCESS_MESSAGE`. `fixedAssets.ts` posts `{name, price, purchaseDate, assetTypeId, quantity}`. Test `AC-3` in `StockAdd.test.tsx` (1459-1499) passed, asserting the exact call args and field reset.
- ✅ **AC-4:** Submit disabled unless name/price/quantity non-empty and asset type selected. — `const canSubmit = assetName.trim() !== "" && price !== "" && quantity !== "" && assetTypeId !== "";` (StockAdd.tsx:316-317); button `disabled={!canSubmit}` (217). Two AC-4 tests in `StockAdd.test.tsx` (1501-1535) both passed, confirming `createFixedAsset` is never called when disabled.
- ✅ **AC-5:** Blank/whitespace name (or missing price/quantity) → 400 `ASSET_NAME_REQUIRED`, no row created. — `FixedAssetsController.cs:766-769`: `if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "ASSET_NAME_REQUIRED", ... })`. Test `Create_ReturnsAssetNameRequired_ForEmptyOrWhitespaceName` (`FixedAssetsControllerTests.cs:142-175`) covers `""` and `"   "`, asserts 400 + error code + `db.FixedAssets.CountAsync() == 0`. Passed.
- ✅ **AC-6:** Unknown `assetTypeId` → 400 `INVALID_ASSET_TYPE`, no row created. — `FixedAssetsController.cs:771-774`: `if (!await _db.AssetTypes.AnyAsync(t => t.Id == request.AssetTypeId)) return BadRequest(new { error = "INVALID_ASSET_TYPE", ... })`. Test `Create_ReturnsInvalidAssetType_ForUnknownAssetTypeId` (177-203) passed.
- ✅ **AC-7:** Duplicate name (exact match) → 409 `DUPLICATE_ASSET_NAME`, enforced via real DB unique index (not pre-check query), no second row. — `AppDbContext.cs:173`: `modelBuilder.Entity<FixedAsset>().HasIndex(a => a.Name).IsUnique();`; migration `20260830110443_AddFixedAssetAndAssetType.cs:55-59` creates `IX_FixedAssets_Name` unique. Controller has no pre-check query — it directly `_db.FixedAssets.Add(asset)` then `catch (DbUpdateException) → Conflict(new { error = "DUPLICATE_ASSET_NAME", message = "Kayıtlı Demirbaş..." })` (784-793). Test `Create_ReturnsDuplicateAssetName_ForSameNameTwice` (205-239) uses a `SaveChangesInterceptor` to simulate the unique-index violation since EF InMemory doesn't enforce it, asserts 409 + `DUPLICATE_ASSET_NAME` + `StartsWith("Kayıtlı Demirbaş")` + only 1 row exists. Passed.
- ✅ **AC-8:** Successful POST persists `Price` as `decimal(19,4)` and returns id/name/price/purchaseDate/assetTypeId/quantity. — `AppDbContext.cs:174`: `.Property(a => a.Price).HasColumnType("decimal(19,4)")`; migration `CreateTable` line 34: `Price = table.Column<decimal>(type: "decimal(19,4)", nullable: false)`. Controller returns all 6 fields (`FixedAssetsController.cs:795-803`). Test `Create_ReturnsCreated_ForAdminWithValidFields` (108-140) asserts 201 + all 6 returned fields including `Assert.Equal(199.99m, body.Price)`. Passed.
- ✅ **AC-9:** `GET /api/asset-types` returns all seeded rows to an admin. — `AssetTypesController.cs:855-868`: `List()` selects `{id, name}` for all `AssetTypes`, gated by `IsCallerAdminAsync`. Test `List_ReturnsAssetTypes_ForAdmin` (`AssetTypesControllerTests.cs:1324-1344`) seeds 2 types and asserts both are returned. Passed.
- ✅ **AC-10:** Unauthenticated visit to `/stock-add` redirects to `/login`. — `App.tsx` wraps `/stock-add` in `RequireAdmin`, which returns `<Navigate to="/login" replace />` when `!token` (App.tsx:942-944). Test `App.test.tsx:389-401` ("AC-10: an unauthenticated visit to /stock-add shows the Login screen") passed.
- ✅ **AC-11:** Authenticated non-admin visit to `/stock-add` redirects to `/`. — `RequireAdmin` returns `<Navigate to="/" replace />` when `status === "not-admin"` (App.tsx:948-949). Test `App.test.tsx:403-423` passed (asserts EKLE button/Stock Add screen absent, Main Menu shown instead).
- ✅ **AC-12:** Back control navigates to `/admin`. — `StockAdd.tsx` back button: `onClick={() => navigate("/admin")}` (line ~128-131). Test `StockAdd.test.tsx:1556-1562` ("AC-12: clicking the back button calls navigate with /admin") passed.
- ✅ **AC-13:** `/room-add`, `/room-update`, `/room-delete`, `/room-assignment`, `/admin`, `/` unaffected. — Only one new `<Route path="/stock-add">` added to `App.tsx` (1011-1018); no existing route/guard logic touched. `App.test.tsx` retains and passes all pre-existing tests for `/`, `/admin`, `/room-add`, `/room-update`, `/room-delete`, `/room-assignment` (21/21 App.test.tsx tests passed; full suite 79/79 passed, 10/10 files).

No unhandled edge cases found: whitespace-trim (`request.Name.Trim()` at controller line 778, `assetName.trim()` at client line 330), concurrent-duplicate race (DB unique index + catch, not pre-check), and zero/negative price/quantity (no sign/magnitude validation anywhere, matching the spec's explicit non-requirement) are all consistent with the Edge Cases section.

## Special-Attention Items (from task brief)

1. **Hyphenated routes:** `FixedAssetsController.cs:747`: `[Route("api/fixed-assets")]`; `AssetTypesController.cs:844`: `[Route("api/asset-types")]`. Both explicit, not `[controller]` token. Test files call `/api/fixed-assets` and `/api/asset-types` respectively (confirmed in both test files). PASS.
2. **Admin-gating:** Both controllers call `IsCallerAdminAsync(_db)` and `return Forbid()` if false (`FixedAssetsController.cs:761-764`, `AssetTypesController.cs:858-861`). Confirmed via `Create_ReturnsForbidden_ForNonAdminCaller` and `List_ReturnsForbidden_ForNonAdminCaller` tests, both passing. PASS — correctly follows the BL-005/BL-009 admin-gated pattern, not the BL-008 non-gated pattern.
3. **`decimal(19,4)`:** Confirmed in both `AppDbContext.OnModelCreating` (line 174) and the migration's `CreateTable` (line 34, `20260830110443_AddFixedAssetAndAssetType.cs`). PASS.
4. **No letter filter on asset-name:** Confirmed absent — `StockAdd.tsx:134-141` has no `onKeyDown` attribute on the asset-name input, unlike price/quantity which do. PASS (deliberate defect reproduced faithfully).
5. **Migration seeds only `AssetType` rows:** `20260830110443_AddFixedAssetAndAssetType.cs:65-73` — `migrationBuilder.InsertData(table: "AssetTypes", ...)` inserting 3 rows (Elektronik/Mobilya/Ofis Malzemesi); no `InsertData` call targets `FixedAssets`. PASS.
6. **AC-13 route regression:** Verified above — all 79 frontend tests across 10 files pass, including every pre-existing route test in `App.test.tsx`.

Note: the migration file exists at `api/src/InventoryTrackingSystem.Infrastructure/Migrations/20260830110443_AddFixedAssetAndAssetType.cs` — a different timestamp than the `20260830120000_...` path referenced in the task brief/verify-context tool (which reported "File does not exist" for that exact name). This is a filename-guess mismatch in the tooling, not a real gap; the migration itself is present, complete, and correctly configured (confirmed by direct file read).

## Test Results

Both suites were re-run independently in this session (dotnet SDK 8.0.424 was available), not just taken from build-time evidence.

**Backend (`dotnet test`, re-run independently):**
```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51, Duration: 6 s - InventoryTrackingSystem.Api.Tests.dll (net8.0)
```
Matches the build-time capture exactly (51/51).

**Frontend (`npx vitest run`, re-run independently):**
```
 Test Files  10 passed (10)
      Tests  79 passed (79)
   Duration  3.77s
```
Matches the build-time capture (79/79, 10/10 files). Includes `StockAdd.test.tsx` (7/7 passed) and `App.test.tsx` (21/21 passed, covering all pre-existing routes plus the new stock-add route-guard tests AC-10/AC-11 and one unlabeled admin-success test).

**Build:** Backend `dotnet build` succeeded (0 warnings, 0 errors). Frontend `tsc -b && vite build` succeeded.

## Issues Found

No blocking issues found.

1. **Minor: mismatched AC comment in test file** — `FixedAssetsControllerTests.cs` line 21 labels the non-admin-403 test as "AC-8", but spec.md's AC-8 is actually about decimal persistence/response shape (the non-admin-403 case isn't separately numbered in spec.md's AC list — it's implied by FR-8's admin-only gating, tested at the route-guard level via AC-10/AC-11 in the frontend, and at the controller level here). This is a doc-comment labeling slip only; the test itself (`Create_ReturnsForbidden_ForNonAdminCaller`) is correct and necessary coverage for FR-1/NFR-implicit admin gating. Non-blocking, cosmetic. **Fix (optional):** relabel the comment to reference FR-1/admin-gating rather than "AC-8".

## Summary

**Passed:** 13/13 criteria
**Failed:** 0/13 criteria
**Verdict:** PASS
