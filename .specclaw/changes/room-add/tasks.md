# Tasks: BL-005 — Room Add

**Change:** room-add
**Created:** 2026-08-30
**Total Tasks:** 7

## Summary

4 waves. Wave 1 builds the two independent tracks (backend entities/DbContext, frontend screen) in parallel. Wave 2 generates the migration and builds the two admin-gated controllers, both depending only on Wave 1's backend entities. Wave 3 wires the frontend route (generalizing `RequireAdmin`) once the screen component exists. Wave 4 is tests, split backend/frontend.

## Tasks

### Wave 1 — Independent backend and frontend foundations

- [x] `T1` — Room and Department domain entities + AppDbContext registration
  - Files: `api/src/InventoryTrackingSystem.Domain/Entities/Room.cs`, `api/src/InventoryTrackingSystem.Domain/Entities/Department.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs`
  - Estimate: small
  - Kind: impl
  - Notes: `Room` has `Id`, `Name`, `DepartmentId` (int FK, no navigation property); `Department` has `Id`, `Name`. Mirror `User.cs`'s minimal style exactly — plain properties, no navigation properties. In `AppDbContext.OnModelCreating`, add `modelBuilder.Entity<Room>().HasIndex(r => r.Name).IsUnique()` (CQ-018 — the real uniqueness constraint the legacy DB never had) and `modelBuilder.Entity<Room>().HasOne<Department>().WithMany().HasForeignKey(r => r.DepartmentId)` for the FK relationship. Register `DbSet<Room> Rooms` and `DbSet<Department> Departments`.

- [x] `T2` — Room Add frontend screen + API client
  - Files: `web/src/api/departments.ts`, `web/src/api/rooms.ts`, `web/src/routes/RoomAdd.tsx`, `web/src/routes/RoomAdd.css`
  - Estimate: medium
  - Kind: impl
  - Notes: `departments.ts` exports `listDepartments(): Promise<{id: number; name: string}[]>` calling `apiFetch("/departments")`, matching `auth.ts`'s pattern exactly. `rooms.ts` exports `createRoom(name: string, departmentId: number): Promise<{id: number; name: string; departmentId: number}>` calling `apiFetch("/rooms", {method: "POST", body: {name, departmentId}})`. `RoomAdd.tsx` (SCR-010 layout, per ui-inventory.md): a bordered "ODA EKLEME" section with a room-name text field, a department picker (a `<select>` populated from `listDepartments()` on mount — the paired ID/name list simplifies to a single select for the web, since selecting a name inherently selects its ID), a disabled text field showing the selected department's ID (FR-2), a centered "EKLE" button, and a back button navigating to `/admin` (FR-9, mirror `useNavigate()` pattern from `MainMenu.tsx`/`AdminPanel.tsx`). Client-side (FR-4, FR-6): "EKLE" is disabled (or shows a validation indicator, matching `Login.tsx`'s `*` hint pattern) unless the room name is non-empty (trimmed) AND a department is selected — do not call `createRoom` otherwise. On success: reset the room-name field and department selection, show "Oda başarıyla eklendi." (FR-3). On a `409` `ApiError` (check `error.status === 409` from `rooms.ts`'s `ApiError`, per `client.ts`): show "Kayıtlı Oda..." (FR-5). Do not wire the `/room-add` route yet — that's T5.

### Wave 2 — Migration and controllers (depend on T1)

- [x] `T3` — EF Core migration for Room and Department
  - Files: `api/src/InventoryTrackingSystem.Infrastructure/Migrations/20260830045603_AddRoomAndDepartment.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Migrations/20260830045603_AddRoomAndDepartment.Designer.cs`, `api/src/InventoryTrackingSystem.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
  - Estimate: small
  - Kind: migration
  - Depends: T1
  - Notes: Generate via `dotnet ef migrations add AddRoomAndDepartment --project api/src/InventoryTrackingSystem.Infrastructure --startup-project api/src/InventoryTrackingSystem.Api` (mirror however `AddUserAuthentication` was generated — do not hand-write the migration file). After generating, add a small `migrationBuilder.InsertData(...)` for 2-3 placeholder `Department` rows (e.g. "Genel", "Bilgi İşlem") for local dev/test only (see design.md's dev-seed note) — edit the generated `Up()` method to append this after the `CreateTable` calls.

- [x] `T4` — RoomsController, DepartmentsController, admin-authorization helper
  - Files: `api/src/InventoryTrackingSystem.Api/Authorization/AdminAuthorizationExtensions.cs`, `api/src/InventoryTrackingSystem.Api/Controllers/RoomsController.cs`, `api/src/InventoryTrackingSystem.Api/Controllers/DepartmentsController.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T1
  - Notes: `AdminAuthorizationExtensions.IsCallerAdminAsync(this ControllerBase controller, AppDbContext db)` — reads the `sub` claim (`JwtRegisteredClaimNames.Sub`, same as `AuthController.Me()`), looks up the `User` by username, returns `user?.YetkiID == true`. `DepartmentsController`: `[Authorize]` + `[HttpGet]` `api/departments` — call `IsCallerAdminAsync`, `Forbid()` if false, else return `Ok(departments)` as `{id, name}` projections. `RoomsController`: `[Authorize]` + `[HttpPost]` `api/rooms` — call `IsCallerAdminAsync` (`Forbid()` if false); if `Name` is null/empty/whitespace-only after `Trim()`, return `BadRequest(new {error = "ROOM_NAME_REQUIRED", message = "Oda adı gereklidir."})`; if no `Department` exists with the given `DepartmentId` (explicit `AnyAsync` check), return `BadRequest(new {error = "INVALID_DEPARTMENT", message = "Geçersiz departman."})`; otherwise add the `Room` and `SaveChangesAsync()` inside a `try/catch (DbUpdateException)` mapping to `Conflict(new {error = "DUPLICATE_ROOM_NAME", message = "Kayıtlı Oda..."})`; on success return `Created` (or `Ok`) with `{id, name, departmentId}`. Both controllers follow `AuthController`'s existing style (constructor-injected `AppDbContext`, plain POCO request/response shapes).

### Wave 3 — Frontend route wiring (depends on T2)

- [x] `T5` — Generalize RequireAdmin and register the /room-add route
  - Files: `web/src/App.tsx`
  - Estimate: small
  - Kind: refactor
  - Depends: T2
  - Notes: Change `RequireAdmin` from hardcoding `<AdminPanel />` to accepting `children: ReactNode` and rendering `<AppShell>{children}</AppShell>` on the admin branch — its loading/redirect logic (token check, `getSession()` call, three-state status) is otherwise unchanged. Update the existing `/admin` route to `<Route path="/admin" element={<RequireAdmin><AdminPanel /></RequireAdmin>} />` and add `<Route path="/room-add" element={<RequireAdmin><RoomAdd /></RequireAdmin>} />`, importing `RoomAdd` from `./routes/RoomAdd`. This is a pure refactor of `RequireAdmin`'s signature — AC-12 requires `/admin`'s existing observable behavior to be unaffected.

### Wave 4 — Tests

- [x] `T6` — Backend tests for Rooms and Departments endpoints
  - Files: `api/tests/InventoryTrackingSystem.Api.Tests/RoomsControllerTests.cs`, `api/tests/InventoryTrackingSystem.Api.Tests/DepartmentsControllerTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T1, T3, T4
  - Notes: Follow `AuthControllerTests.cs`'s exact pattern — `WebApplicationFactory<Program>` with the `AppDbContext` SQL Server registration swapped for a uniquely-named EF Core InMemory database per test, seed a known admin user (`YetkiID: true`) and a known non-admin user (`YetkiID: false`), log in via `POST /api/auth/login` to get a real token (reuse `AuthControllerTests`' `LoginAsync`-style helper), then call the endpoint under test with the `Authorization: Bearer <token>` header. Cover: AC-3 (valid create → 201 + body), AC-4 (empty/whitespace name → 400 `ROOM_NAME_REQUIRED`, no row created), AC-5 (creating the same name twice → second call 409 `DUPLICATE_ROOM_NAME`), AC-9 (non-admin token → `POST /api/rooms` → 403), AC-10 (non-admin token → `GET /api/departments` → 403), AC-13 (invalid `departmentId` → 400 `INVALID_DEPARTMENT`). Also cover a happy-path `GET /api/departments` returning seeded rows for an admin token.

- [x] `T7` — Frontend tests for RoomAdd screen and the /room-add route guard
  - Files: `web/tests/RoomAdd.test.tsx`, `web/tests/App.test.tsx`
  - Estimate: medium
  - Kind: test
  - Depends: T2, T5
  - Notes: `RoomAdd.test.tsx` (new, mirror `AdminPanel.test.tsx`'s `vi.mock` pattern for `useNavigate`, plus `vi.mock("../src/api/departments")` and `vi.mock("../src/api/rooms")`): AC-1 (renders room-name field, department picker, disabled ID echo, EKLE button), AC-2 (selecting a department sets the echo field), AC-3 (valid submit calls `createRoom`, then resets fields and shows the success message — mock `createRoom` to resolve), AC-4 (empty name blocks submit — `createRoom` never called), AC-11 (back button navigates to `/admin`), AC-13 (no department selected blocks submit). For AC-5, mock `createRoom` to reject with an `ApiError`-shaped object (`status: 409`) and assert the "Kayıtlı Oda..." message renders. In `App.test.tsx` (modify, same `getSession`/`window.history.pushState` pattern as the existing `/admin` tests): AC-6 (no token → `/room-add` → Login), AC-7 (token + `isAdmin: false` → `/room-add` → ends at Main Menu), AC-8 (token + `isAdmin: true` → `/room-add` → renders RoomAdd content, e.g. the "EKLE" button), AC-12 (re-run the existing `/admin` tests unmodified — they must still pass after `RequireAdmin`'s refactor). Also re-run the full `npx vitest run` suite from `web/` to confirm nothing regressed.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
