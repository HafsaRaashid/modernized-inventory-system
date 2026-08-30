# Tasks: BL-007 — Room Delete

**Change:** room-delete
**Created:** 2026-08-30
**Total Tasks:** 5

## Summary

3 waves. Wave 1 builds the two independent tracks (backend controller addition, frontend screen + API client) in parallel — both extend existing files, no migration needed. Wave 2 wires the `/room-delete` route once the screen component exists. Wave 3 is tests, split backend/frontend. Note: this item explicitly does NOT implement CQ-023's FK-guard against `RoomAssetAssignment` — see spec.md's Overview and AC-13. No task below builds that check; if a task seems to require it, that's a signal the partition is wrong — stop and report, don't add it.

## Tasks

### Wave 1 — Independent backend and frontend additions

- [x] `T1` — RoomsController: delete action
  - Files: `api/src/InventoryTrackingSystem.Api/Controllers/RoomsController.cs`
  - Estimate: small
  - Kind: impl
  - Notes: Add one action to the existing `RoomsController` (do not create a new controller). `[HttpDelete] Delete([FromBody] DeleteRoomRequest request)`: call `IsCallerAdminAsync` (`Forbid()` if false); look up `var room = await _db.Rooms.SingleOrDefaultAsync(r => r.Name == request.Name)` — if `null`, return `NotFound(new {error = "ROOM_NOT_FOUND", message = "Hatalı İşlem..."})`; otherwise `_db.Rooms.Remove(room); await _db.SaveChangesAsync();` and return `Ok(new {id = room.Id, name = room.Name, departmentId = room.DepartmentId})`. No `try/catch` needed — a delete cannot violate the uniqueness constraint, and there is no FK constraint in this schema to violate (do NOT add any check referencing assignments/`RoomAssetAssignment` — that's explicitly out of scope, see spec.md AC-13). Add a `DeleteRoomRequest` POCO (`string Name`) after the controller class, alongside `CreateRoomRequest`/`UpdateRoomRequest`.

- [x] `T2` — Room Delete frontend screen + API client addition
  - Files: `web/src/api/rooms.ts`, `web/src/routes/RoomDelete.tsx`, `web/src/routes/RoomDelete.css`
  - Estimate: medium
  - Kind: impl
  - Notes: In the existing `web/src/api/rooms.ts` (do not create a new file), add `deleteRoom(name: string): Promise<Room>` calling `apiFetch<Room>("/rooms", {method: "DELETE", body: {name}})` — reuse the existing `Room` interface. `RoomDelete.tsx` (SCR-011 layout, "ODA SİLME"): a `<select>` room selector populated via `listRooms()` (import from `../api/rooms`, already exists from BL-006) on mount, and a "SİL" button in the same row (no confirmation dialog, no `window.confirm`, nothing — clicking SİL deletes immediately). A back button navigating to `/admin` (same `useNavigate()` pattern as `RoomAdd.tsx`/`RoomUpdate.tsx`). Client-side: "SİL" disabled unless a room is selected. On click: call `deleteRoom(selectedRoomName)`; on success, clear the selection and re-run `listRooms()` to re-populate the selector (matching the legacy "selector cleared and re-populated" success state), show "Oda başarıyla silindi.". On any thrown error: show "Hatalı İşlem..." (no status-code branching needed, same as `RoomUpdate.tsx`'s pattern). Do not wire the `/room-delete` route yet — that's T3.

### Wave 2 — Frontend route wiring (depends on T2)

- [x] `T3` — Register the /room-delete route
  - Files: `web/src/App.tsx`
  - Estimate: small
  - Kind: impl
  - Depends: T2
  - Notes: Add `<Route path="/room-delete" element={<RequireAdmin><RoomDelete /></RequireAdmin>} />` alongside the existing `/admin`, `/room-add`, and `/room-update` routes, importing `RoomDelete` from `./routes/RoomDelete`. Do NOT modify `RequireAdmin` itself — purely an additional route registration.

### Wave 3 — Tests

- [x] `T4` — Backend tests for the Rooms delete endpoint
  - Files: `api/tests/InventoryTrackingSystem.Api.Tests/RoomsControllerTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T1
  - Notes: Add tests to the existing `RoomsControllerTests.cs` (do not create a new file), following its exact `CreateFactory`/`SeedKnownUserAsync`/`SeedDepartmentAsync`/`SeedRoomAsync`/`LoginAsync` pattern. Cover: AC-3 (seed a room, admin token, `DELETE /api/rooms` with `{name}` matching it → 200 + body reflects the deleted room), AC-4 (non-admin token → `DELETE /api/rooms` with a valid name → 403), AC-11 (admin token, `name` matching no seeded room → 404 `ROOM_NOT_FOUND`), AC-12 (after a successful delete, assert via a new `AppDbContext` scope that `_db.Rooms.CountAsync()` no longer includes the deleted room — or more directly, that `_db.Rooms.AnyAsync(r => r.Id == deletedId)` is `false`). AC-5 (GET /api/rooms non-admin → 403) is already covered by the existing `List_ReturnsForbidden_ForNonAdminCaller` test — do not duplicate it.

- [x] `T5` — Frontend tests for RoomDelete screen and the /room-delete route guard
  - Files: `web/tests/RoomDelete.test.tsx`, `web/tests/App.test.tsx`
  - Estimate: medium
  - Kind: test
  - Depends: T2, T3
  - Notes: `RoomDelete.test.tsx` (new, mirror `RoomUpdate.test.tsx`'s `vi.mock` pattern for `useNavigate` and for `../src/api/rooms` — mock `listRooms` and `deleteRoom`): AC-1 (renders the room selector and "SİL" button), AC-2 (selector populated from mocked `listRooms`), AC-3 (selecting a room and clicking "SİL" calls `deleteRoom(selectedRoomName)` immediately — assert no `window.confirm`/dialog call happens, then assert the success message shows and `listRooms` is called again to re-populate), AC-9 (back button navigates to `/admin`). Also cover a failure case: mock `deleteRoom` to reject and assert "Hatalı İşlem..." renders. In `App.test.tsx` (modify, same `getSession`/`window.history.pushState` pattern the existing `/admin`/`/room-add`/`/room-update` tests use — mock `../src/api/rooms`'s `listRooms` to resolve `[]`, same as the existing `/room-update` tests already do, and additionally mock `deleteRoom`): AC-6 (no token → `/room-delete` → Login), AC-7 (token + `isAdmin: false` → `/room-delete` → ends at Main Menu), AC-8 (token + `isAdmin: true` → `/room-delete` → renders RoomDelete content, e.g. the "SİL" button), AC-10 (re-run the existing `/admin`, `/room-add`, and `/room-update` tests unmodified — they must still pass). Also re-run the full `npx vitest run` suite from `web/` to confirm nothing regressed.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
