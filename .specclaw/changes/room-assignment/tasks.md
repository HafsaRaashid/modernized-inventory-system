# Tasks: BL-008 — Room to Personnel Assignment (Room Assignment)

**Change:** room-assignment
**Created:** 2026-08-30
**Total Tasks:** 7

## Summary

4 waves. Wave 1 builds the two independent tracks (backend entities/DbContext, frontend screen) in parallel. Wave 2 generates the migration and builds the two new controllers, both depending only on Wave 1's entities. Wave 3 generalizes `RequireAuth` and wires the frontend route, depending on the screen component existing. Wave 4 is tests.

## Tasks

### Wave 1 — Independent backend and frontend foundations

- [x] `T1` — Personnel and RoomAssetAssignment domain entities + AppDbContext registration
  - Files: `api/src/InventoryTrackingSystem.Domain/Entities/Personnel.cs`, `api/src/InventoryTrackingSystem.Domain/Entities/RoomAssetAssignment.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs`
  - Estimate: small
  - Kind: impl
  - Notes: `Personnel` has `Id`, `FirstName`, `LastName` (map legacy `PersonelAdi`/`PersonelSoyadi`). `RoomAssetAssignment` has `Id`, `RoomId` (`int?`), `PersonnelId` (`int?`), `AssetId` (`int?`), `Quantity` (`int?`) — all four nullable per CQ-003's already-decided shared-table shape; `AssetId`/`Quantity` are genuinely unused by this item (BL-011 will use them later), do not add any logic referencing them. Mirror `Department.cs`'s minimal style — plain properties, no navigation properties. In `AppDbContext.OnModelCreating`, add `modelBuilder.Entity<RoomAssetAssignment>().HasOne<Room>().WithMany().HasForeignKey(a => a.RoomId);` and `modelBuilder.Entity<RoomAssetAssignment>().HasOne<Personnel>().WithMany().HasForeignKey(a => a.PersonnelId);` — no FK for `AssetId` (no `FixedAsset` table exists yet). Register `DbSet<Personnel> Personnel` and `DbSet<RoomAssetAssignment> RoomAssetAssignments`.

- [x] `T2` — Room Assignment frontend screen + API client
  - Files: `web/src/api/personnel.ts`, `web/src/api/roomAssignments.ts`, `web/src/routes/RoomAssignment.tsx`, `web/src/routes/RoomAssignment.css`
  - Estimate: medium
  - Kind: impl
  - Notes: `personnel.ts` exports a `Personnel` interface (`{id, firstName, lastName}`) and `listPersonnel(): Promise<Personnel[]>` calling `apiFetch("/personnel")`, matching `departments.ts`'s pattern exactly. `roomAssignments.ts` exports `createRoomAssignment(roomId: number, personnelId: number): Promise<{id: number; roomId: number; personnelId: number}>` calling `apiFetch("/room-assignments", {method: "POST", body: {roomId, personnelId}})`. `RoomAssignment.tsx` (SCR-006 layout): two side-by-side `<select>`s — a room selector populated via `listRooms()` (import from `../api/rooms`, already exists from BL-006; options are room names, but you need the room's *id* for the POST body, so store the selected room's id, e.g. by keying the select's `value` on `room.id` and looking up the display name separately, or storing the whole selected room object) and a personnel selector populated via `listPersonnel()` (similarly, track the selected personnel's id, displaying `${firstName} ${lastName}` as each option's label). Beneath both selectors, two disabled text inputs echo the selected room's name and the selected personnel's full name (FR-3). A "KAYDET" button, disabled unless BOTH a room and a personnel are selected (FR-5/AC-5 — do not call `createRoomAssignment` otherwise). A back button navigating to `/` (mirror the `useNavigate()` pattern from `RoomAdd.tsx`, but target `/` not `/admin`). On success: call `createRoomAssignment(selectedRoomId, selectedPersonnelId)`, then reset both selections and echo fields, show "Atama başarıyla kaydedildi." On any thrown error: show a generic failure message (no legacy text exists for this screen — pick something reasonable, e.g. "Atama kaydedilirken bir hata oluştu."). Do not wire the `/room-assignment` route yet — that's T5.

### Wave 2 — Migration and controllers (depend on T1)

- [x] `T3` — EF Core migration for Personnel and RoomAssetAssignment
  - Files: `api/src/InventoryTrackingSystem.Infrastructure/Migrations/20260830104004_AddPersonnelAndRoomAssetAssignment.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Migrations/20260830104004_AddPersonnelAndRoomAssetAssignment.Designer.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
  - Estimate: small
  - Kind: migration
  - Depends: T1
  - Notes: Generate via `dotnet ef migrations add AddPersonnelAndRoomAssetAssignment` from within the `api/` directory (same tool/workflow the `AddRoomAndDepartment` migration used — do not hand-write the migration file). After generating, add a small `migrationBuilder.InsertData(...)` for 2-3 placeholder `Personnel` rows (e.g. "Ahmet Yılmaz", "Ayşe Kaya") for local dev/test only, mirroring exactly how the `AddRoomAndDepartment` migration seeded `Department` rows — edit the generated `Up()` method to append this after the `CreateTable` calls.

- [x] `T4` — PersonnelController and RoomAssignmentsController
  - Files: `api/src/InventoryTrackingSystem.Api/Controllers/PersonnelController.cs`, `api/src/InventoryTrackingSystem.Api/Controllers/RoomAssignmentsController.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T1
  - Notes: **Neither controller uses `AdminAuthorizationExtensions.IsCallerAdminAsync`** — this screen is not admin-gated, `[Authorize]` alone is correct. `PersonnelController`: `[Authorize]` + `[HttpGet]` `api/personnel` — return `Ok(await _db.Personnel.Select(p => new {id = p.Id, firstName = p.FirstName, lastName = p.LastName}).ToListAsync())`. `RoomAssignmentsController`: `[Authorize]` + `[HttpPost]` `api/room-assignments` — accept `CreateRoomAssignmentRequest` (`int? RoomId`, `int? PersonnelId`); if either is `null`, return `BadRequest(new {error = "SELECTION_REQUIRED", message = "Oda ve sorumlu personel seçilmelidir."})`; if no `Room` exists with `Id == request.RoomId`, return `BadRequest(new {error = "INVALID_ROOM", message = "Geçersiz oda."})`; if no `Personnel` exists with `Id == request.PersonnelId`, return `BadRequest(new {error = "INVALID_PERSONNEL", message = "Geçersiz personel."})`; otherwise create a `RoomAssetAssignment { RoomId = request.RoomId, PersonnelId = request.PersonnelId }` (leave `AssetId`/`Quantity` null), save, and return `Created(string.Empty, new {id = assignment.Id, roomId = assignment.RoomId, personnelId = assignment.PersonnelId})`. No `try/catch` needed (no uniqueness constraint on this table). Follow `RoomsController`'s existing style (constructor-injected `AppDbContext`, plain POCO request class declared after the controller).

### Wave 3 — Frontend route wiring (depends on T2)

- [x] `T5` — Generalize RequireAuth and register the /room-assignment route
  - Files: `web/src/App.tsx`
  - Estimate: small
  - Kind: refactor
  - Depends: T2
  - Notes: Change `RequireAuth` from hardcoding `<MainMenu />` to accepting `children: ReactNode` and rendering `<AppShell>{children}</AppShell>` on the authenticated branch — the same refactor already applied to `RequireAdmin` in BL-005 (accept `children`, no other logic changes). Update the existing `/` route to `<Route path="/" element={<RequireAuth><MainMenu /></RequireAuth>} />` and add `<Route path="/room-assignment" element={<RequireAuth><RoomAssignment /></RequireAuth>} />`, importing `RoomAssignment` from `./routes/RoomAssignment`. Do NOT touch `RequireAdmin` or any of its routes. This is a pure refactor of `RequireAuth`'s signature — AC-11 requires `/`'s existing observable behavior to be unaffected, AC-12 requires all admin routes to be unaffected.

### Wave 4 — Tests

- [x] `T6` — Backend tests for Personnel and RoomAssignments endpoints
  - Files: `api/tests/InventoryTrackingSystem.Api.Tests/PersonnelControllerTests.cs`, `api/tests/InventoryTrackingSystem.Api.Tests/RoomAssignmentsControllerTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T1, T3, T4
  - Notes: Follow `RoomsControllerTests.cs`'s exact `CreateFactory`/`SeedKnownUserAsync`/`LoginAsync` pattern (a plain authenticated user is enough here — these endpoints don't check `YetkiID` at all, so you don't need to vary it, though seeding `yetkiId: false` for at least one test proves the endpoint doesn't accidentally require admin). `PersonnelControllerTests.cs`: a happy-path `GET /api/personnel` test — seed 1-2 `Personnel` rows directly via a new `AppDbContext` scope, authenticate, assert `200` + the seeded rows appear. `RoomAssignmentsControllerTests.cs`: AC-4 (seed a `Room` and a `Personnel`, `POST /api/room-assignments` with both real ids → `201` + body echoes ids), AC-6 (`POST` with a null `roomId` or `personnelId` → `400 SELECTION_REQUIRED`), AC-7 (`POST` with a `roomId`/`personnelId` that matches no seeded row → `400 INVALID_ROOM`/`INVALID_PERSONNEL`), AC-13 (POST the same valid pair twice → both succeed with `201`, then assert via a new `AppDbContext` scope that `db.RoomAssetAssignments.CountAsync()` is `2`, proving no upsert/dedup).

- [x] `T7` — Frontend tests for RoomAssignment screen and the /room-assignment route guard
  - Files: `web/tests/RoomAssignment.test.tsx`, `web/tests/App.test.tsx`
  - Estimate: medium
  - Kind: test
  - Depends: T2, T5
  - Notes: `RoomAssignment.test.tsx` (new, mirror `RoomAdd.test.tsx`'s `vi.mock` pattern for `useNavigate`, plus mock `../src/api/rooms` (`listRooms`), `../src/api/personnel` (`listPersonnel`), and `../src/api/roomAssignments` (`createRoomAssignment`)): AC-1 (renders both selectors, both echo fields, KAYDET button), AC-3 (selecting a room/personnel updates the corresponding echo field), AC-4 (selecting both and clicking KAYDET calls `createRoomAssignment` with the right ids, shows success, resets selections/echoes), AC-5 (KAYDET stays disabled/no-op with only one or neither selected — `createRoomAssignment` never called), AC-10 (back button navigates to `/`). In `App.test.tsx` (modify, same `getSession`/`window.history.pushState` pattern as existing tests — mock `../src/api/personnel`'s `listPersonnel` to resolve `[]` and `../src/api/roomAssignments`'s `createRoomAssignment`): AC-8 (no token → `/room-assignment` → Login), AC-9 (token, ANY `isAdmin` value → `/room-assignment` → renders RoomAssignment content, e.g. the "KAYDET" button — test with `isAdmin: false` specifically, to prove this route is NOT admin-gated), AC-11 (re-run the existing `/` tests unmodified — they must still pass after `RequireAuth`'s refactor), AC-12 (re-run the existing `/admin`/`/room-add`/`/room-update`/`/room-delete` tests unmodified). Also re-run the full `npx vitest run` suite from `web/` to confirm nothing regressed.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
