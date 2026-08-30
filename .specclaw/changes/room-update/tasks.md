# Tasks: BL-006 — Room Update (rename)

**Change:** room-update
**Created:** 2026-08-30
**Total Tasks:** 5

## Summary

3 waves. Wave 1 builds the two independent tracks (backend controller additions, frontend screen + API client) in parallel — both extend BL-005's existing files rather than creating new ones, and no migration is needed. Wave 2 wires the `/room-update` route once the screen component exists. Wave 3 is tests, split backend/frontend.

## Tasks

### Wave 1 — Independent backend and frontend additions

- [x] `T1` — RoomsController: list and update actions
  - Files: `api/src/InventoryTrackingSystem.Api/Controllers/RoomsController.cs`
  - Estimate: small
  - Kind: impl
  - Notes: Add two actions to the existing `RoomsController` (do not create a new controller). `[HttpGet] List()`: call `IsCallerAdminAsync` (`Forbid()` if false), return `Ok(await _db.Rooms.Select(r => new { id = r.Id, name = r.Name }).ToListAsync())`. `[HttpPut] Update([FromBody] UpdateRoomRequest request)`: call `IsCallerAdminAsync` (`Forbid()` if false); if `request.NewName` is null/empty/whitespace-only, return `BadRequest(new {error = "ROOM_NAME_REQUIRED", message = "Oda adı gereklidir."})`; look up `var room = await _db.Rooms.SingleOrDefaultAsync(r => r.Name == request.OldName)` — if `null`, return `NotFound(new {error = "ROOM_NOT_FOUND", message = "Hatalı İşlem..."})`; otherwise `room.Name = request.NewName.Trim();` and `try { await _db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(new {error = "DUPLICATE_ROOM_NAME", message = "Hatalı İşlem..."}); }`; on success return `Ok(new {id = room.Id, name = room.Name, departmentId = room.DepartmentId})`. Add an `UpdateRoomRequest` POCO (`string OldName`, `string NewName`) after the controller class, alongside the existing `CreateRoomRequest`.

- [x] `T2` — Room Update frontend screen + API client additions
  - Files: `web/src/api/rooms.ts`, `web/src/routes/RoomUpdate.tsx`, `web/src/routes/RoomUpdate.css`
  - Estimate: medium
  - Kind: impl
  - Notes: In the existing `web/src/api/rooms.ts` (do not create a new file), add `listRooms(): Promise<Room[]>` calling `apiFetch<Room[]>("/rooms")` and `updateRoom(oldName: string, newName: string): Promise<Room>` calling `apiFetch<Room>("/rooms", {method: "PUT", body: {oldName, newName}})` — reuse the existing `Room` interface, don't redeclare it. `RoomUpdate.tsx` (SCR-012 layout, "ODA GÜNCELLEME"): mirror `RoomAdd.tsx`'s structure closely — a `<select>` existing-room selector populated via `listRooms()` on mount (options are room names), a new-name text input, a centered "GÜNCELLE" button, and a back button navigating to `/admin` (same `useNavigate()` pattern). Client-side: "GÜNCELLE" disabled unless a room is selected AND the new name is non-empty after `.trim()` — do not call `updateRoom` otherwise. On success: reset both fields, re-run `listRooms()` to re-populate the selector (matching the legacy "combo re-populated" success state), show "Oda başarıyla güncellendi.". On any thrown error from `updateRoom` (both the 404 not-found and 409 duplicate cases map to the same displayed text per spec.md FR-5/FR-6): show "Hatalı İşlem..." — no need to branch on status code in the UI, unlike `RoomAdd.tsx`'s 409-specific branch. Do not wire the `/room-update` route yet — that's T3.

### Wave 2 — Frontend route wiring (depends on T2)

- [x] `T3` — Register the /room-update route
  - Files: `web/src/App.tsx`
  - Estimate: small
  - Kind: impl
  - Depends: T2
  - Notes: Add `<Route path="/room-update" element={<RequireAdmin><RoomUpdate /></RequireAdmin>} />` alongside the existing `/admin` and `/room-add` routes, importing `RoomUpdate` from `./routes/RoomUpdate`. Do NOT modify `RequireAdmin` itself — it already accepts `children` from BL-005; this is purely an additional route registration, not a refactor.

### Wave 3 — Tests

- [x] `T4` — Backend tests for Rooms list/update endpoints
  - Files: `api/tests/InventoryTrackingSystem.Api.Tests/RoomsControllerTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T1
  - Notes: Add tests to the existing `RoomsControllerTests.cs` (do not create a new file), following its exact `CreateFactory`/`SeedKnownUserAsync`/seed-department/`LoginAsync` pattern. Cover: AC-3 (seed a room, admin token, `PUT /api/rooms` with a valid `{oldName, newName}` → 200 + body reflects the new name), AC-4 (empty/whitespace `newName` → 400 `ROOM_NAME_REQUIRED`), AC-5 (seed two rooms, attempt to rename one to the other's name → 409 `DUPLICATE_ROOM_NAME`; note the existing `DuplicateRoomNameSimulatingInterceptor` in this file already simulates the InMemory provider's missing unique-index enforcement for `Room.Name` — reuse it, it applies to any `SaveChangesAsync` on `Room`, not just `Create`), AC-9 (non-admin token → `PUT /api/rooms` → 403), AC-10 (non-admin token → `GET /api/rooms` → 403), AC-13 (an `oldName` that matches no seeded room → 404 `ROOM_NOT_FOUND`). Also cover a happy-path `GET /api/rooms` returning seeded rooms for an admin token.

- [x] `T5` — Frontend tests for RoomUpdate screen and the /room-update route guard
  - Files: `web/tests/RoomUpdate.test.tsx`, `web/tests/App.test.tsx`
  - Estimate: medium
  - Kind: test
  - Depends: T2, T3
  - Notes: `RoomUpdate.test.tsx` (new, mirror `RoomAdd.test.tsx`'s `vi.mock` pattern for `useNavigate` and for `../src/api/rooms` — mock both `listRooms` and `updateRoom`): AC-1 (renders the room selector, new-name field, GÜNCELLE button), AC-2 (selector populated from mocked `listRooms`), AC-3 (valid submit calls `updateRoom(oldName, newName)`, then resets fields, shows the success message, and calls `listRooms` again to re-populate), AC-4 (empty/whitespace new name keeps GÜNCELLE disabled, `updateRoom` never called), AC-11 (back button navigates to `/admin`). Also cover a failure case: mock `updateRoom` to reject (any error) and assert "Hatalı İşlem..." renders. In `App.test.tsx` (modify, same `getSession`/`window.history.pushState` pattern the existing `/admin` and `/room-add` tests use — you'll need to additionally mock `../src/api/rooms`'s `listRooms` to resolve `[]` so `RoomUpdate` doesn't crash on mount): AC-6 (no token → `/room-update` → Login), AC-7 (token + `isAdmin: false` → `/room-update` → ends at Main Menu), AC-8 (token + `isAdmin: true` → `/room-update` → renders RoomUpdate content, e.g. the "GÜNCELLE" button), AC-12 (re-run the existing `/admin` and `/room-add` tests unmodified — they must still pass). Also re-run the full `npx vitest run` suite from `web/` to confirm nothing regressed.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
