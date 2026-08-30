# Verification Report: stock-update

**Verified:** 2026-08-30
**Model:** Claude Sonnet 5
**Verdict:** PASS

## Acceptance Criteria

- ✅ **AC-1:** `/stock-update` renders selector + fields + submit button — `StockUpdate.tsx:170-277` renders `asset-select`, `asset-name`, `price`, `purchase-date`, `asset-type`, `quantity` fields and a "GÜNCELLE" button, populated via `listFixedAssets()` at `StockUpdate.tsx:87-93,95-102`. Confirmed by `StockUpdate.test.tsx` `AC-1` test (passing) and independent frontend re-run (11 files / 90 tests passed).
- ✅ **AC-2:** Selecting an asset populates all fields — `handleAssetSelect` (`StockUpdate.tsx:104-120`) sets name/price/purchaseDate/assetTypeId/quantity from the looked-up asset. Confirmed by `StockUpdate.test.tsx` `AC-2` test (passing).
- ✅ **AC-3:** Valid submit calls `PUT /api/fixed-assets` with `{id,name,price,purchaseDate,assetTypeId,quantity}`, resets form, refreshes selector, shows success message — `StockUpdate.tsx:138-153` calls `updateFixedAsset(...)`, resets state, calls `loadAssets()`, sets `SUCCESS_MESSAGE = "Demirbaş başarıyla güncellendi."` (`StockUpdate.tsx:7`). `updateFixedAsset` (`fixedAssets.ts:338-350`) issues `PUT` to `/fixed-assets` with the full body. Confirmed by `StockUpdate.test.tsx` `AC-3` test (passing, asserts `listFixedAssets` called twice).
- ✅ **AC-4:** Submit disabled unless asset selected + name/price/quantity non-empty — `canSubmit` (`StockUpdate.tsx:122-126`) checks all four; `handleSubmit` also early-returns if `!canSubmit` (`StockUpdate.tsx:131-133`). Confirmed by two `AC-4` tests (no-selection, name-cleared) — both passing, neither calls `updateFixedAsset`.
- ✅ **AC-5:** Asset-name field blocks non-letter keystrokes — `handleLettersAndCommaKeyDown` (`StockUpdate.tsx:46-53`) uses `/^[\p{L},]$/u` and is wired via `onKeyDown={handleLettersAndCommaKeyDown}` on the `asset-name` input (`StockUpdate.tsx:197`). Confirmed by `StockUpdate.test.tsx` `AC-5` test: digit key `"5"` is prevented (returns `false`), letter key `"a"` passes (returns `true`).
- ✅ **AC-6:** Blank/whitespace name → 400 `ASSET_NAME_REQUIRED`, record unmodified — `FixedAssetsController.cs:121-124` (`Update`). Backend test `Update_ReturnsAssetNameRequired_ForEmptyOrWhitespaceName` (theory `""`/`"   "`) asserts 400 + error code + asserts the seeded asset's name is unchanged (`FixedAssetsControllerTests.cs:379-412`). Independent `dotnet test` re-run: 61/61 passed.
- ✅ **AC-7:** Unknown `assetTypeId` → 400 `INVALID_ASSET_TYPE`, record unmodified — `FixedAssetsController.cs:126-129`. Backend test `Update_ReturnsInvalidAssetType_ForUnknownAssetTypeId` (`FixedAssetsControllerTests.cs:415-442`) passing.
- ✅ **AC-8:** Rename colliding with a different record → 409 `DUPLICATE_ASSET_NAME`; no-op rename to own current name → 200 OK — `FixedAssetsController.cs:143-150` relies on `DbUpdateException` from the DB unique index (no pre-check), exactly mirroring `Create`'s pattern. **Critical regression check**: `Update_ReturnsOk_ForNoOpRenameToOwnCurrentName` (`FixedAssetsControllerTests.cs:476-502`) submits the asset's own current name and asserts `HttpStatusCode.OK` (not 409), and the test-only `DuplicateFixedAssetNameSimulatingInterceptor` (`FixedAssetsControllerTests.cs:602-631`) explicitly excludes the candidate's own row via `a.Id != candidate.Id` (line 620), so a no-op rename cannot be mistaken for a collision. `Update_ReturnsDuplicateAssetName_ForRenameCollidingWithAnotherAsset` (lines 445-473) confirms the true-collision path still returns 409. Both pass.
- ✅ **AC-9:** Unknown `id` → 404 `ASSET_NOT_FOUND` — `FixedAssetsController.cs:131-135`. Backend test `Update_ReturnsAssetNotFound_ForUnknownId` (`FixedAssetsControllerTests.cs:505-531`) passing.
- ✅ **AC-10:** Any of AC-6/7/8/9 failures shows the single generic frontend message, no per-status branching — `StockUpdate.tsx`'s `catch` block (lines 154-156) is a bare `catch { setMessage({ text: FAILURE_MESSAGE, kind: "error" }) }` with no `instanceof ApiError`/status check, unlike `StockAdd.tsx:112-118`'s `if (error instanceof ApiError && error.status === 409) {...} else {...}` branching. Confirmed by `StockUpdate.test.tsx`'s "shows the generic failure message when updateFixedAsset rejects for any reason" test (lines 202-217), which rejects with a plain `Error("boom")` (not an `ApiError`) and still asserts the same `"Güncellenirken hata oluştu..."` text — proving there's no branch to bypass.
- ✅ **AC-11:** `GET /api/fixed-assets` returns all rows to admin, 403 for non-admin — `FixedAssetsController.cs:89-102` (`List`) is admin-gated via `IsCallerAdminAsync` (same as `Create`/`Update`, unlike BL-008's ungated pattern) and projects `id/name/price/purchaseDate/assetTypeId/quantity`. Backend tests `List_ReturnsFixedAssets_ForAdmin` and `List_ReturnsForbidden_ForNonAdminCaller` (`FixedAssetsControllerTests.cs:298-337`) both pass.
- ✅ **AC-12:** Unauthenticated visit redirects to `/login` — `App.tsx`'s `RequireAdmin` wrapper (lines 682-719) redirects to `/login` when `!token`; `/stock-update` route uses `RequireAdmin` (`App.tsx:786-793`). Confirmed by `App.test.tsx` "AC-12: an unauthenticated visit to /stock-update shows the Login screen" (lines 451-463), passing.
- ✅ **AC-13:** Authenticated non-admin redirects to `/` — `RequireAdmin` redirects to `/` when `status === "not-admin"` (`App.tsx:715-717`). Confirmed by `App.test.tsx` "AC-13: an authenticated non-admin visiting /stock-update ends up back at the Main Menu" (lines 465-485), passing.
- ✅ **AC-14:** Back control navigates to `/admin` — `StockUpdate.tsx:163-169`'s back button calls `navigate("/admin")`. Confirmed by `StockUpdate.test.tsx` "AC-14: clicking the back button navigates to /admin" (lines 1200-1206 of context payload / 194-199 of file), passing.
- ✅ **AC-15:** Other routes unaffected — `App.test.tsx` retains full passing coverage for `/stock-add` (lines 396-449), `/room-add` (198-251), `/room-update` (253-306), `/room-delete` (308-361), `/room-assignment` (363-394), `/admin` (122-196), and `/` (early tests). All 24 tests in `App.test.tsx` pass in the independent re-run, and `StockAdd.tsx`'s asset-name input (lines 134-141) has no `onKeyDown` handler — confirming it remains deliberately unfiltered and unaffected by this change.

No unhandled edge cases found — the spec's Edge Cases (dirty-state discard on selector switch, no-op update succeeds, whitespace trimming, no optimistic concurrency) are all consistent with the implementation read above (`handleAssetSelect` fully overwrites all fields with no diffing; `Update` has no `RowVersion`/concurrency check; `.Trim()` applied server-side at `FixedAssetsController.cs:137` before both the empty check and the save).

## Test Results

**Backend** (`dotnet test`, independently re-run):
```
Passed!  - Failed:     0, Passed:    61, Skipped:     0, Total:    61, Duration: 9 s - InventoryTrackingSystem.Api.Tests.dll (net8.0)
```

**Frontend** (`npx vitest run`, independently re-run):
```
 Test Files  11 passed (11)
      Tests  90 passed (90)
```
Includes `StockUpdate.test.tsx` (8 tests, all passing) and `App.test.tsx` (24 tests, all passing).

**Build** (from context payload, `dotnet build` + `tsc -b && vite build`): both succeeded, 0 warnings, 0 errors.

**Lint**: no lint output configured/reported for this project; nothing to evaluate.

## Issues Found

No issues found.

## Summary

**Passed:** 15/15 criteria
**Failed:** 0/15 criteria
**Verdict:** PASS
