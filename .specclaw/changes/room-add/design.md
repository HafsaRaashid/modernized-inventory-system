# Design: BL-005 — Room Add

**Change:** room-add
**Created:** 2026-08-30

## Technical Approach

Five pieces:

1. **Domain entities.** `Room` (`Id`, `Name`, `DepartmentId`) and `Department` (`Id`, `Name`), mirroring `User.cs`'s minimal style (plain properties, no navigation properties, English names for fields with an unambiguous translation — `OdaAdi` → `Name`, `DepartmanID` → `DepartmentId` — matching how `User.cs` renamed `KullaniciAdi`/`Sifre` but kept `YetkiID` verbatim where no clean rename existed).
2. **`AppDbContext`:** register both `DbSet`s; add a unique index on `Room.Name` (the CQ-018 fix) and a FK relationship from `Room.DepartmentId` to `Department.Id` via Fluent API (no navigation property needed, same as `User`'s standalone style).
3. **EF Core migration** (`dotnet ef migrations add AddRoomAndDepartment`), including a small placeholder `Department` seed row or two for local dev/test (see spec.md Notes).
4. **Backend:** `DepartmentsController` (`GET /api/departments`) and `RoomsController` (`POST /api/rooms`), both gated by a new `AdminAuthorizationExtensions.IsCallerAdminAsync` helper — a small, single-purpose method rather than a full custom `AuthorizationHandler`/policy, since only these two call sites need it right now and `GET /api/auth/me` already established the "re-query `YetkiID` fresh" pattern this helper repeats.
5. **Frontend:** a new `RoomAdd.tsx` screen (SCR-010 layout) plus `web/src/api/rooms.ts` and `web/src/api/departments.ts` API client modules (matching `auth.ts`'s `apiFetch<T>` pattern). `RequireAdmin` in `App.tsx` is generalized from hardcoding `<AdminPanel />` to accepting `children`, so `/admin` and the new `/room-add` share one guard instead of duplicating the loading/redirect logic.

## Architecture

```
AdminPanel ("Oda Ekle" button, BL-004) --click--> /room-add
                                                       │
                                                       ▼
                                              RequireAdmin (generalized)
                                       ┌───────────────┴───────────────┐
                                 no token                        has token
                                       │                               │
                                 Navigate to /login          GET /api/auth/me (BL-003, reused)
                                                               ┌────────┴────────┐
                                                         isAdmin:false      isAdmin:true
                                                               │                  │
                                                        Navigate to /    <AppShell><RoomAdd /></AppShell>
                                                                                    │
                                                          on mount: GET /api/departments (admin-gated)
                                                          on submit: POST /api/rooms (admin-gated)
```

`RoomsController.Create` and `DepartmentsController.List` both call `AdminAuthorizationExtensions.IsCallerAdminAsync(this, _db)` before doing anything else — a 403 short-circuits an authenticated-but-non-admin caller who reaches the endpoint directly (AC-9/AC-10), independent of whether the frontend route guard was ever evaluated.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Domain/Entities/Room.cs` | Create | `Id`, `Name`, `DepartmentId` |
| `api/src/InventoryTrackingSystem.Domain/Entities/Department.cs` | Create | `Id`, `Name` |
| `api/src/InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs` | Modify | Register both `DbSet`s; unique index on `Room.Name`; FK `Room.DepartmentId` → `Department.Id` |
| `api/src/InventoryTrackingSystem.Infrastructure/Migrations/*_AddRoomAndDepartment.cs` (+ `.Designer.cs`, updated `AppDbContextModelSnapshot.cs`) | Create (generated) | `dotnet ef migrations add AddRoomAndDepartment`; includes a minimal dev-seed of `Department` rows |
| `api/src/InventoryTrackingSystem.Api/Authorization/AdminAuthorizationExtensions.cs` | Create | `IsCallerAdminAsync(ControllerBase, AppDbContext)` — re-queries the caller's `YetkiID` from the `sub` claim, same pattern as `AuthController.Me()` |
| `api/src/InventoryTrackingSystem.Api/Controllers/DepartmentsController.cs` | Create | `GET /api/departments` — admin-gated list |
| `api/src/InventoryTrackingSystem.Api/Controllers/RoomsController.cs` | Create | `POST /api/rooms` — admin-gated create, non-empty-name check, department-existence check, unique-name constraint mapped to 409 |
| `web/src/api/departments.ts` | Create | `listDepartments(): Promise<Department[]>` |
| `web/src/api/rooms.ts` | Create | `createRoom(name, departmentId): Promise<Room>`, typed `ApiError` cases for the caller to branch on |
| `web/src/routes/RoomAdd.tsx` | Create | SCR-010 layout: room-name field, department picker, disabled ID echo, EKLE button, back control |
| `web/src/routes/RoomAdd.css` | Create | Layout styling |
| `web/src/App.tsx` | Modify | Generalize `RequireAdmin` to accept `children`; register `/room-add` through it |
| `api/tests/InventoryTrackingSystem.Api.Tests/RoomsControllerTests.cs` | Create | AC-3, AC-4, AC-5, AC-9, AC-13 |
| `api/tests/InventoryTrackingSystem.Api.Tests/DepartmentsControllerTests.cs` | Create | AC-10, and a happy-path list |
| `web/tests/RoomAdd.test.tsx` | Create | AC-1, AC-2, AC-3, AC-4, AC-11, AC-13 |
| `web/tests/App.test.tsx` | Modify | AC-6, AC-7, AC-8, AC-12 (regression for `/admin`) |

## Data Model Changes

New tables:

- **`Rooms`** — `Id` (PK, identity), `Name` (`nvarchar`, unique index — CQ-018), `DepartmentId` (`int`, FK → `Departments.Id`).
- **`Departments`** — `Id` (PK, identity), `Name` (`nvarchar`). Read-only from the application's point of view (CQ-012) — no endpoint ever writes to it; the migration's dev-seed rows are the only writer, and only for local/test environments.

## API Changes

**`GET /api/departments`** *(new)*
- Auth: `[Authorize]` + `IsCallerAdminAsync` (403 if not admin).
- Response `200`: `[{ "id": number, "name": string }, ...]`.

**`POST /api/rooms`** *(new)*
- Auth: `[Authorize]` + `IsCallerAdminAsync` (403 if not admin).
- Request: `{ "name": string, "departmentId": number }`.
- `400 Bad Request` — `{ "error": "ROOM_NAME_REQUIRED", "message": "Oda adı gereklidir." }` when `name` is null/empty/whitespace-only (server-side re-check of FR-4, same rule as the client's, not a second one).
- `400 Bad Request` — `{ "error": "INVALID_DEPARTMENT", "message": "Geçersiz departman." }` when `departmentId` does not match an existing `Department` (checked via an explicit `AnyAsync` query before insert, not by parsing the FK-violation exception — keeps the "invalid department" and "duplicate name" failure paths distinguishable instead of both landing in one generic catch).
- `409 Conflict` — `{ "error": "DUPLICATE_ROOM_NAME", "message": "Kayıtlı Oda..." }` when the unique index on `Room.Name` rejects the insert (caught as `DbUpdateException` — the single source of truth for uniqueness, not duplicated by an application-level pre-check, since two coincidentally-agreeing paths for the same rule is exactly the DR-004 anti-pattern this project's own analysis already called out).
- `201 Created` — `{ "id": number, "name": string, "departmentId": number }` on success.

## Key Decisions

- **`RequireAdmin` becomes reusable, not `/room-add`-specific.** BL-004 hardcoded `<AdminPanel />` inside `RequireAdmin` because it was the only admin-gated route. This item generalizes it to accept `children`, since BL-006/007/009/010 will need the same guard for their own routes — better to make the existing guard reusable than to hand-copy its loading/redirect logic a second time. AC-12 exists specifically to catch a regression in this refactor.
- **Backend admin-gating via a small extension method, not a policy/claims-based `[Authorize(Roles = ...)]`.** The JWT carries only a `sub` (username) claim today; adding a role claim at issuance would mean re-signing the token-issuance contract for two call sites. `AdminAuthorizationExtensions.IsCallerAdminAsync` reuses the exact "re-query `YetkiID` per request" pattern `GET /api/auth/me` already established, at the cost of one extra DB read per admin-gated request — acceptable for the current traffic profile, and consistent rather than introducing a second authorization mechanism for two endpoints.
- **Duplicate-name detection via the real DB constraint alone, not a pre-check + constraint combo.** A `SELECT`-then-`INSERT` pre-check plus the constraint would be two independently-maintained paths that happen to agree — precisely the shape DR-004's analysis flagged as fragile. The constraint is the single source of truth; `DbUpdateException` is the one place duplicate detection lives.
- **Department existence, by contrast, IS pre-checked explicitly** (not via catching an FK-violation exception) — this is a different rule (referential validity, not uniqueness) from a different cause, and disambiguating "which DB exception fired" via string-matching would be more fragile than one direct query.
- **No navigation properties on `Room`/`Department`.** Matches `User.cs`'s existing minimalism (no EF navigation properties anywhere yet); the FK is modeled via Fluent API (`HasOne<Department>().WithMany().HasForeignKey(...)`) without adding a `Department Department { get; set; }` property neither controller currently needs.
- **FR-6 (require a department selection) is new baseline validation, not a numbered legacy rule.** GM-022 documents the legacy screen crashing (uncaught exception) when no department is selected; rather than reproducing a crash-equivalent, this item just requires a selection before submit — flagged in spec.md as a judgment call since no CQ/DR decision explicitly covers it.

## Risks & Mitigations

- **Risk:** This is the first migration to run against a real SQL Server database in this rebuild (previous items only added `Users`). **Mitigation:** the migration is generated via `dotnet ef migrations add` (not hand-written), matching the existing `AddUserAuthentication` migration's provenance exactly; `dotnet ef database update` is a manual step outside this item's automated build (consistent with how `AddUserAuthentication` was handled).
- **Risk:** EF Core's InMemory provider (used by `AuthControllerTests`'s `WebApplicationFactory` pattern) enforces unique indexes since EF Core 3.0+, so `RoomsControllerTests` can exercise AC-5 (duplicate rejection) without a real SQL Server — confirmed consistent with the existing test convention, not a new one.
- **Risk:** Generalizing `RequireAdmin` changes a file BL-004 already shipped and verified. **Mitigation:** AC-12 requires the full existing `/admin` test suite (`App.test.tsx`'s BL-004 tests) to keep passing unmodified in behavior — only the *implementation* of `RequireAdmin` changes (accepting `children` instead of hardcoding `<AdminPanel />`), not its externally observable gating behavior.
- **Risk:** No real department data exists to migrate yet (CQ-012 leaves provisioning outside the app). **Mitigation:** the migration includes a minimal placeholder seed for local dev/test only, explicitly called out in spec.md as not a stand-in for real data migration.
