# Verification Report: asset-assignment

**Verified:** 2026-08-30
**Model:** claude-sonnet-5
**Verdict:** PASS

## Acceptance Criteria

- ✅ **AC-1:** Room selector, asset selector, two disabled echo inputs, quantity input, KAYDET button, and assignments panel/table all render — `web/src/routes/AssetAssignment.tsx:177-289`; asserted in `web/tests/AssetAssignment.test.tsx:64-85`.
- ✅ **AC-2:** Selecting room/asset updates the respective echo field — `AssetAssignment.tsx:107-108,225,237`; test `AssetAssignment.test.tsx:87-100`.
- ✅ **AC-3:** Selecting a room calls `listRoomAssetAssignments` and renders rows — `AssetAssignment.tsx:124-131`; test `AssetAssignment.test.tsx:102-116`.
- ✅ **AC-4:** `canSubmit` requires roomId, assetId, and quantity all non-empty — `AssetAssignment.tsx:112-113`; tests `AssetAssignment.test.tsx:118-164` (3 variants: room-only, room+asset-no-qty, nothing-selected), all assert `createAssetAssignment` not called.
- ✅ **AC-5:** `quantityExceedsStock` disables submit strictly `>` (not `>=`) — `AssetAssignment.tsx:110-111`; tests `AssetAssignment.test.tsx:166-198` cover both the over-stock (disabled) and exactly-equal-to-stock (enabled) boundary.
- ✅ **AC-6:** `handleSave` posts `{roomId, assetId, quantity}`, clears only `quantity` on success (retains roomId/assetId), shows "Odaya Demirbaş Atandı", and re-fetches both `listFixedAssets` and `listRoomAssetAssignments` — `AssetAssignment.tsx:133-153`; test `AssetAssignment.test.tsx:200-235` explicitly asserts `Miktar` value is `""` AND `Oda`/`Demirbaş` selects retain `"1"`/`"5"` after save (lines 229-231), plus both re-fetches (233-234).
- ✅ **AC-7:** Missing `roomId`/`assetId` → 400 `SELECTION_REQUIRED` — `AssetAssignmentsController.cs:50-53`; test `Create_ReturnsSelectionRequired_ForMissingRoomOrAsset` (theory, both variants) in `AssetAssignmentsControllerTests.cs:202-228`.
- ✅ **AC-8:** Missing/non-positive quantity → 400 `QUANTITY_REQUIRED` — `Controller.cs:55-58`; test `Create_ReturnsQuantityRequired_ForMissingOrNonPositiveQuantity` (null/0/-1) at `...Tests.cs:234-260`.
- ✅ **AC-9:** Unknown `roomId` → 400 `INVALID_ROOM` — `Controller.cs:60-64`; test `...Tests.cs:263-286`.
- ✅ **AC-10:** Unknown `assetId` → 400 `INVALID_ASSET` — `Controller.cs:66-70`; test `...Tests.cs:289-312`.
- ✅ **AC-11:** Quantity exceeding stock → 400 `INSUFFICIENT_STOCK` with legacy message text, no row written and asset unchanged — `Controller.cs:72-75`; test `Create_ReturnsInsufficientStock_ForQuantityExceedingStock` at `...Tests.cs:315-349` asserts both `RoomAssetAssignments.AnyAsync(...)` is false AND `asset.Quantity == 5` unchanged.
- ✅ **AC-12:** Room with no responsibility row → 400 `NO_RESPONSIBLE_PERSONNEL` — `Controller.cs:77-84`; test `...Tests.cs:383-408`.
- ✅ **AC-13 (core atomicity):** Single `SaveChangesAsync()` call (`Controller.cs:96`) flushes both the new `RoomAssetAssignment` (queued `Controller.cs:93`) and the decremented `asset.Quantity` (`Controller.cs:94`). Test `Create_ReturnsCreated_AndAtomicallyInsertsAssignmentAndDecrementsStock` (`...Tests.cs:411-447`) re-opens a **fresh** `AppDbContext` scope after the HTTP call and asserts BOTH the new row's `Quantity`/`PersonnelId` (lines 441-443) AND the asset's decremented `Quantity == 13` (lines 445-446, from 20-7).
- ✅ **AC-14:** Responsibility rows queried `OrderByDescending(a => a.Id).FirstOrDefaultAsync()` — `Controller.cs:79-80`. Test `Create_UsesMostRecentResponsibilityRow_WhenRoomHasMultiple` (`...Tests.cs:450-479`) seeds two DIFFERENT personnel (Ayşe then Mehmet) as two separate responsibility rows for one room, and asserts the response's `PersonnelId` equals `secondPersonnelId` (the higher-`Id`/later-seeded row), not the first.
- ✅ **AC-15:** `List` filters `a.AssetId != null` — `Controller.cs:120-123`. Test `List_ReturnsOnlyAssetIssueRows_ForRoom` (`...Tests.cs:482-511`) seeds a responsibility row + one asset-issue row for Room A and a different asset-issue row for Room B, then GETs Room A and asserts exactly one qualifying row comes back (Room B's and the responsibility row are both excluded).
- ✅ **AC-16:** Unauthenticated `/asset-assignment` → Login screen — `App.tsx` `RequireAuth` wrapper (`App.tsx:637-643,720-727`); test `App.test.tsx:514-526`.
- ✅ **AC-17:** Authenticated (non-admin) visit renders the screen (not admin-gated) — same `RequireAuth` wrapper does not check `isAdmin`; test `App.test.tsx:528-545` (`isAdmin: false` still renders KAYDET button).
- ✅ **AC-18:** All pre-existing routes (`/`, `/admin`, `/room-add`, `/room-update`, `/room-delete`, `/room-assignment`, `/stock-add`, `/stock-update`) still have passing tests in `App.test.tsx` — confirmed by the full 104/104 passing frontend run (26 tests in `App.test.tsx`), including their own AC-labeled tests (AC-4/5/6, AC-8/9, AC-10/11, AC-12/13, etc.) untouched by this change.

## Special-Attention Verification

1. **Atomicity (NFR-1/AC-13):** `AssetAssignmentsController.Create` calls `_db.SaveChangesAsync()` exactly once (`Controller.cs:96`), after queuing both the `Add` (line 93) and the in-place `asset.Quantity -=` mutation (line 94) on the same tracked context. Grepped the whole `Controllers` directory (`AssetAssignmentsController.cs`, `FixedAssetsController.cs`, `AssetTypesController.cs`, `RoomsController.cs`, `DepartmentsController.cs`) for `BeginTransactionAsync` — zero matches anywhere in the project, confirming no controller (including this one) uses explicit transactions, consistent with the InMemory-provider constraint documented in the class's XML doc comment (`Controller.cs:25-33`). The atomicity test genuinely asserts both halves via a fresh `AppDbContext` scope (`...Tests.cs:439-446`), not just one.
2. **Strict `>` guard:** `Controller.cs:72` reads `if (request.Quantity > asset.Quantity)`. Boundary test `Create_ReturnsCreated_ForQuantityExactlyEqualToStock` (quantity 5 == stock 5) returns 201 and decrements to exactly 0 (`...Tests.cs:352-380`), while `Create_ReturnsInsufficientStock_ForQuantityExceedingStock` (quantity 6 > stock 5) returns 400. Both sides of the boundary are covered.
3. **AC-14 tie-break:** Confirmed order-by-`Id`-descending in code and a genuine two-distinct-personnel seed + assertion against the higher-`Id` row's `PersonnelId` in the test, as detailed above.
4. **No admin gating:** `[Authorize]` is the only attribute on the class (`Controller.cs:37`); no `IsCallerAdminAsync` call anywhere in the file or controller directory. `App.tsx` wraps `/asset-assignment` in `RequireAuth`, never `RequireAdmin`.
5. **Frontend reset-quantity-only:** `handleSave` (`AssetAssignment.tsx:133-153`) calls only `setQuantity("")` on success — `roomId`/`assetId` state is never touched. Test asserts this precisely (`AssetAssignment.test.tsx:229-231`).
6. **AC-15 filter + test rigor:** Confirmed above — the test seeds a same-room responsibility row plus a different room's asset-issue row alongside the target row, so the single-result assertion is a genuine discriminating test, not a vacuous one.
7. **AC-18:** All named routes have passing pre-existing tests in the current `App.test.tsx`, verified both by static grep of test names and the full green test run.

## Test Results

**Frontend** (`cd web && npx vitest run`): **104/104 passed**, 12 test files, 14.09s. Includes `AssetAssignment.test.tsx` (12/12) and `App.test.tsx` (26/26). No failures, no skips.

**Backend** (`cd api && dotnet test`, .NET SDK 8.0.424 confirmed present and used directly — no fallback to static inspection needed): **74/74 passed**, 0 failed, 0 skipped, 23s, `InventoryTrackingSystem.Api.Tests.dll (net8.0)`. Includes all `AssetAssignmentsControllerTests` (14 test cases across theories and facts covering AC-7 through AC-15 plus the boundary test).

## Issues Found

None blocking. No non-blocking issues identified either — the implementation matches the spec's stated design (single atomic `SaveChangesAsync`, strict `>` guard, most-recent-responsibility-row tie-break, no admin gating, quantity-only reset) and every acceptance criterion has direct, non-vacuous test coverage that was independently re-run and passed.

## Summary

**Passed:** 18/18
**Failed:** 0/18
**Verdict:** PASS
