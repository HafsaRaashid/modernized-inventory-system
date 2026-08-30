# Tasks: BL-011 — Asset Assignment and Stock Decrement (Composite Flow)

**Change:** asset-assignment
**Created:** 2026-08-30
**Total Tasks:** 5

## Summary

3 waves. Wave 1 builds the two independent tracks (backend composite-write controller, frontend screen + API client) in parallel — no new entities/migration needed, this item populates columns BL-008 already created but left null. Wave 2 wires the `/asset-assignment` route (trivial, applied directly, not spawned). Wave 3 is tests.

## Tasks

### Wave 1 — Independent backend and frontend work

- [x] `T1` — AssetAssignmentsController (composite create + per-room list)
  - Files: `api/src/InventoryTrackingSystem.Api/Controllers/AssetAssignmentsController.cs`
  - Estimate: large
  - Kind: impl
  - Notes: Read `design.md`'s "Controller Sketch" section FIRST and follow it closely — it is not illustrative, it is the specified shape. **`[ApiController]`, `[Route("api/asset-assignments")]` (explicit hyphenated route — `AssetAssignments` is a compound word, follow the same explicit-route discipline already established for `RoomAssignmentsController`/`FixedAssetsController`/`AssetTypesController`), `[Authorize]` only — do NOT admin-gate this controller (it's reached from Main Menu, not Admin Panel, same posture as `RoomAssignmentsController`/`PersonnelController` from BL-008).** `[HttpPost] Create([FromBody] CreateAssetAssignmentRequest request)` (`int? RoomId`, `int? AssetId`, `int? Quantity`): validate in this exact order — (1) `RoomId is null || AssetId is null` → 400 `{error: "SELECTION_REQUIRED", message: "Oda ve demirbaş seçilmelidir."}`; (2) `Quantity is null || Quantity <= 0` → 400 `{error: "QUANTITY_REQUIRED", message: "Miktar gereklidir."}`; (3) no `Room` with `Id == RoomId` → 400 `{error: "INVALID_ROOM", message: "Geçersiz oda."}`; (4) no `FixedAsset` with `Id == AssetId` → 400 `{error: "INVALID_ASSET", message: "Geçersiz demirbaş."}`; (5) `Quantity > asset.Quantity` → 400 `{error: "INSUFFICIENT_STOCK", message: "Girilen değer stok miktarından fazla.Daha az bir değer giriniz..."}`; (6) no `RoomAssetAssignment` row with `RoomId == request.RoomId && PersonnelId != null && AssetId == null` (order by `Id` descending, take the first — the MOST RECENT responsibility row wins if more than one exists) → 400 `{error: "NO_RESPONSIBLE_PERSONNEL", message: "Bu odaya sorumlu personel atanmamış."}`. Otherwise: create `new RoomAssetAssignment { RoomId = request.RoomId, AssetId = request.AssetId, Quantity = request.Quantity, PersonnelId = responsibility.PersonnelId }`, add it; set `asset.Quantity -= request.Quantity;` on the SAME already-tracked `asset` entity fetched in step (4); call `await _db.SaveChangesAsync();` **exactly once, tracking both the new row and the asset's mutated Quantity in that one call — this is what makes the write atomic. Do NOT call `_db.Database.BeginTransactionAsync()`/`CommitAsync()` anywhere in this controller — the EF Core InMemory provider used by this project's tests does not support explicit transactions and doing so will break every test that exercises this endpoint.** No `try/catch (DbUpdateException)` needed (no uniqueness constraint on this table). Return `Created(string.Empty, new {id = assignment.Id, roomId = assignment.RoomId, assetId = assignment.AssetId, personnelId = assignment.PersonnelId, quantity = assignment.Quantity, remainingStock = asset.Quantity})`. `[HttpGet] List([FromQuery] int roomId)`: return `Ok(await _db.RoomAssetAssignments.Where(a => a.RoomId == roomId && a.AssetId != null).Join(_db.FixedAssets, a => a.AssetId, f => f.Id, (a, f) => new {id = a.Id, assetId = a.AssetId, assetName = f.Name, quantity = a.Quantity}).ToListAsync())`. Declare `CreateAssetAssignmentRequest` as a plain POCO after the controller.

- [x] `T2` — Asset Assignment frontend screen + API client
  - Files: `web/src/api/assetAssignments.ts`, `web/src/routes/AssetAssignment.tsx`, `web/src/routes/AssetAssignment.css`
  - Estimate: medium
  - Kind: impl
  - Notes: Read `web/src/routes/RoomAssignment.tsx` FIRST and mirror its structure closely (two-selector-plus-echo pattern, `message` state, `useNavigate()` back-to-`/` pattern — this screen is reached from Main Menu like Room Assignment, so back goes to `/`, not `/admin`). `assetAssignments.ts` exports `createAssetAssignment(roomId: number, assetId: number, quantity: number): Promise<{id, roomId, assetId, personnelId, quantity, remainingStock}>` calling `apiFetch("/asset-assignments", {method: "POST", body: {roomId, assetId, quantity}})`, and `listRoomAssetAssignments(roomId: number): Promise<{id: number; assetId: number; assetName: string; quantity: number}[]>` calling `apiFetch(\`/asset-assignments?roomId=${roomId}\`)`. `AssetAssignment.tsx`: a room `<select>` (populated via `listRooms()` from `../api/rooms`) with a disabled room-name echo field beneath it; an asset `<select>` (populated via `listFixedAssets()` from `../api/fixedAssets`) with a disabled asset-name echo field beneath it; a quantity input with the same digit/comma-only keydown filter used in `StockAdd.tsx`/`StockUpdate.tsx` (define it locally in this file, following the established per-file convention); a save button ("KAYDET", matching Room Assignment's button label) disabled unless a room, an asset, and a non-empty quantity are all present, AND ALSO disabled if the entered quantity (parsed, comma-to-period) exceeds the selected asset's currently-known `quantity` from the fetched asset list (client-side DR-001 pre-check — do not call `createAssetAssignment` in that case either). Below the form, a read-only panel listing the selected room's current assignments (a simple list or table of `assetName` × `quantity` pairs), populated via `listRoomAssetAssignments(roomId)` whenever the room selection changes (re-fetch on every room-select change, including re-selecting the same room). On save: call `createAssetAssignment(roomId, assetId, parsedQuantity)`; on success, clear ONLY the quantity field (keep the room and asset selections as-is, matching the legacy "grids refresh, selections persist" behavior — do NOT reset the whole form the way `RoomAdd.tsx`/`StockAdd.tsx` do), show "Odaya Demirbaş Atandı" as the success message, and re-fetch BOTH `listFixedAssets()` (to pick up the asset's decremented stock) and `listRoomAssetAssignments(roomId)` (to show the newly-issued row). On a 400 with `error === "INSUFFICIENT_STOCK"`: show the server's own message text verbatim (do not hardcode a duplicate string client-side — use the response body's `message` field, since the exact wording lives server-side per design.md). On any other error: show a generic failure message, e.g. "Demirbaş atanırken bir hata oluştu." Do not wire the `/asset-assignment` route yet — that's T3.

### Wave 2 — Frontend route wiring (depends on T2)

- [x] `T3` — Register the /asset-assignment route
  - Files: `web/src/App.tsx`
  - Estimate: small
  - Kind: impl
  - Depends: T2
  - Notes: Add `<Route path="/asset-assignment" element={<RequireAuth><AssetAssignment /></RequireAuth>} />` (note: `RequireAuth`, NOT `RequireAdmin` — this screen is not admin-gated, same as `/room-assignment`), importing `AssetAssignment` from `./routes/AssetAssignment`. Do NOT modify `RequireAdmin`, `RequireAuth`, or any existing route.

### Wave 3 — Tests

- [x] `T4` — Backend tests for AssetAssignmentsController
  - Files: `api/tests/InventoryTrackingSystem.Api.Tests/AssetAssignmentsControllerTests.cs`
  - Estimate: large
  - Kind: test
  - Depends: T1
  - Notes: Follow `RoomAssignmentsControllerTests.cs`/`RoomsControllerTests.cs`'s exact `CreateFactory`/`SeedKnownUserAsync`/`LoginAsync` pattern (a plain authenticated user is enough — this endpoint doesn't check admin status at all). Add helpers as needed: `SeedRoomAsync`, `SeedAssetTypeAsync`+`SeedFixedAssetAsync` (or import the pattern — these are duplicated per test class in this codebase's established convention, do not try to share across files), and a `SeedRoomResponsibilityAsync(factory, roomId, personnelId)` that inserts a `RoomAssetAssignment { RoomId = roomId, PersonnelId = personnelId }` directly (mirrors what `RoomAssignmentsController.Create` would produce, without needing an HTTP round-trip). Cover: AC-7 (missing roomId or assetId → 400 `SELECTION_REQUIRED`), AC-8 (missing/zero/negative quantity → 400 `QUANTITY_REQUIRED`), AC-9 (unknown roomId → 400 `INVALID_ROOM`), AC-10 (unknown assetId → 400 `INVALID_ASSET`), AC-11 (quantity exceeding the seeded asset's stock → 400 `INSUFFICIENT_STOCK`, and assert via a fresh scope that NO `RoomAssetAssignment` row was created and the asset's `Quantity` is unchanged), AC-12 (a room with no responsibility row seeded → 400 `NO_RESPONSIBLE_PERSONNEL`), AC-13 (valid request → 201, AND a fresh-scope query confirms BOTH the new `RoomAssetAssignment` row exists with the right fields AND the `FixedAsset.Quantity` is decremented by exactly the issued amount — this is the core atomicity proof), AC-14 (seed TWO responsibility rows for the same room with two different personnel — a `SeedPersonnelAsync` helper may be needed too, or seed `Personnel` directly — then assert the created assignment's `personnelId` matches the SECOND (higher-Id) seeded responsibility row, not the first), a boundary test for the edge case "quantity exactly equals stock succeeds, leaving Quantity at 0" (not a rejection — the guard is strictly `>`, not `>=`). For `GET`: AC-15 (seed a responsibility row AND an asset-issue row for a room, plus a DIFFERENT room's asset-issue row — assert the response for the first room's `roomId` contains only that room's asset-issue row, with the right `assetName`/`quantity`, and excludes the responsibility row).

- [x] `T5` — Frontend tests for AssetAssignment screen and the /asset-assignment route guard
  - Files: `web/tests/AssetAssignment.test.tsx`, `web/tests/App.test.tsx`
  - Estimate: medium
  - Kind: test
  - Depends: T2, T3
  - Notes: `AssetAssignment.test.tsx` (new, mirror `RoomAssignment.test.tsx`'s `vi.mock` pattern for `useNavigate`, plus mock `../src/api/rooms` (`listRooms`), `../src/api/fixedAssets` (`listFixedAssets`), and `../src/api/assetAssignments` (`createAssetAssignment`, `listRoomAssetAssignments`)): AC-1 (renders both selects, both echo fields, quantity input, KAYDET button, and the current-assignments panel), AC-2 (selecting room/asset updates the corresponding echo field), AC-3 (selecting a room calls `listRoomAssetAssignments` with that room's id and renders the returned rows), AC-4 (KAYDET disabled/no-op with any of room/asset/quantity missing), AC-5 (entering a quantity greater than the selected asset's known stock keeps KAYDET disabled / `createAssetAssignment` never called — mock `listFixedAssets` to return an asset with a known `quantity` value and test both just-over and at-the-boundary), AC-6 (a valid save calls `createAssetAssignment` with the right args, shows "Odaya Demirbaş Atandı", clears ONLY the quantity field — assert the room/asset selections are UNCHANGED after success, unlike every other Add/Update screen in this codebase — and re-fetches both `listFixedAssets` and `listRoomAssetAssignments`). In `App.test.tsx` (modify, same pattern as existing tests; mock `../src/api/assetAssignments`'s exports to resolve `[]`/reject nothing by default): AC-16 (no token → `/asset-assignment` → Login), AC-17 (token, ANY `isAdmin` value → `/asset-assignment` → renders AssetAssignment content — test with `isAdmin: false` specifically, proving this route is NOT admin-gated, same style as the existing `/room-assignment` test). AC-18: re-run the full `npx vitest run` suite to confirm every pre-existing test still passes.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
