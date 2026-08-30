# Design: BL-008 — Room to Personnel Assignment (Room Assignment)

**Change:** room-assignment
**Created:** 2026-08-30

## Technical Approach

Five pieces:

1. **Domain entities.** `Personnel` (`Id`, `FirstName`, `LastName` — mapping legacy `PersonelAdi`/`PersonelSoyadi`), mirroring `Department`'s minimal style. `RoomAssetAssignment` (`Id`, `RoomId?`, `PersonnelId?`, `AssetId?`, `Quantity?`) — CQ-003's already-decided shared-table shape; only `RoomId`/`PersonnelId` are used/FK-constrained by this item, `AssetId`/`Quantity` exist as plain nullable columns for BL-011 to use later.
2. **`AppDbContext`:** register both new `DbSet`s; add FK relationships `RoomAssetAssignment.RoomId → Room` and `RoomAssetAssignment.PersonnelId → Personnel` (no navigation properties, matching the existing `Room.DepartmentId` FK style).
3. **EF Core migration**, including a minimal `Personnel` dev-seed (mirroring BL-005's `Department` seed precedent).
4. **Backend:** a new `PersonnelController` (`GET /api/personnel`, authenticated only) and a new `RoomAssignmentsController` (`POST /api/room-assignments`, authenticated only) — neither uses `AdminAuthorizationExtensions`, since this screen isn't admin-gated.
5. **Frontend:** a new `RoomAssignment.tsx` (SCR-006 layout) plus `web/src/api/personnel.ts` and `web/src/api/roomAssignments.ts` API client modules. `RequireAuth` in `App.tsx` is generalized from hardcoding `<MainMenu />` to accepting `children` — the same refactor BL-005 already did for `RequireAdmin` — so `/` and the new `/room-assignment` share one guard.

## Architecture

```
MainMenu ("ODA TANIMLAMA" button, BL-002) --click--> /room-assignment
                                                          │
                                                          ▼
                                                 RequireAuth (generalized)
                                                  ┌────────┴────────┐
                                            no token              has token
                                                  │                     │
                                          Navigate to /login   <AppShell><RoomAssignment /></AppShell>
                                                                          │
                                          on mount: GET /api/rooms (BL-006) + GET /api/personnel (new)
                                          on save: POST /api/room-assignments { roomId, personnelId }
```

`RoomAssignmentsController.Create` validates both IDs are present (400 if not — CQ-005's guard), validates both reference existing rows (400 if not — same pre-check pattern `RoomsController.Create`'s `INVALID_DEPARTMENT` check already established), then inserts a `RoomAssetAssignment` row with only `RoomId`/`PersonnelId` set (`AssetId`/`Quantity` left null) and saves — no `try/catch` needed, since there's no uniqueness constraint on this table to violate (FR-9: plain inserts, no upsert).

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Domain/Entities/Personnel.cs` | Create | `Id`, `FirstName`, `LastName` |
| `api/src/InventoryTrackingSystem.Domain/Entities/RoomAssetAssignment.cs` | Create | `Id`, `RoomId?`, `PersonnelId?`, `AssetId?`, `Quantity?` |
| `api/src/InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs` | Modify | Register both `DbSet`s; FK `RoomId`→`Room`, `PersonnelId`→`Personnel` |
| `api/src/InventoryTrackingSystem.Infrastructure/Migrations/*_AddPersonnelAndRoomAssetAssignment.cs` (+ `.Designer.cs`, updated snapshot) | Create (generated) | `dotnet ef migrations add`; includes a minimal dev-seed of `Personnel` rows |
| `api/src/InventoryTrackingSystem.Api/Controllers/PersonnelController.cs` | Create | `GET /api/personnel` — authenticated, not admin-gated |
| `api/src/InventoryTrackingSystem.Api/Controllers/RoomAssignmentsController.cs` | Create | `POST /api/room-assignments` — authenticated, CQ-005 validation |
| `web/src/api/personnel.ts` | Create | `listPersonnel(): Promise<Personnel[]>` |
| `web/src/api/roomAssignments.ts` | Create | `createRoomAssignment(roomId, personnelId): Promise<RoomAssetAssignment>` |
| `web/src/routes/RoomAssignment.tsx` | Create | SCR-006 layout: room/personnel selectors, two echo fields, KAYDET button, back control |
| `web/src/routes/RoomAssignment.css` | Create | Layout styling |
| `web/src/App.tsx` | Modify | Generalize `RequireAuth` to accept `children`; register `/room-assignment` through it |
| `api/tests/InventoryTrackingSystem.Api.Tests/PersonnelControllerTests.cs` | Create | AC-2 happy path |
| `api/tests/InventoryTrackingSystem.Api.Tests/RoomAssignmentsControllerTests.cs` | Create | AC-4, AC-6, AC-7, AC-13 |
| `web/tests/RoomAssignment.test.tsx` | Create | AC-1, AC-3, AC-4, AC-5, AC-10 |
| `web/tests/App.test.tsx` | Modify | AC-8, AC-9, AC-11, AC-12 (regression for `/` and all admin routes) |

## Data Model Changes

New tables:

- **`Personnel`** — `Id` (PK, identity), `FirstName` (`nvarchar`), `LastName` (`nvarchar`). Read-only from the application's point of view (CQ-006/CQ-012); the migration's dev-seed rows are the only writer, local/test only.
- **`RoomAssetAssignments`** — `Id` (PK, identity), `RoomId` (`int`, nullable, FK → `Rooms.Id`), `PersonnelId` (`int`, nullable, FK → `Personnel.Id`), `AssetId` (`int`, nullable, no FK yet), `Quantity` (`int`, nullable). This item populates only `RoomId`/`PersonnelId`.

## API Changes

**`GET /api/personnel`** *(new)*
- Auth: `[Authorize]` only — no admin check.
- Response `200`: `[{ "id": number, "firstName": string, "lastName": string }, ...]`.

**`POST /api/room-assignments`** *(new)*
- Auth: `[Authorize]` only — no admin check.
- Request: `{ "roomId": number | null, "personnelId": number | null }`.
- `400 Bad Request` — `{ "error": "SELECTION_REQUIRED", "message": "Oda ve sorumlu personel seçilmelidir." }` when either `roomId` or `personnelId` is null (CQ-005's guard).
- `400 Bad Request` — `{ "error": "INVALID_ROOM", "message": "Geçersiz oda." }` when `roomId` doesn't match an existing `Room`.
- `400 Bad Request` — `{ "error": "INVALID_PERSONNEL", "message": "Geçersiz personel." }` when `personnelId` doesn't match an existing `Personnel`.
- `201 Created` — `{ "id": number, "roomId": number, "personnelId": number }` on success.

## Key Decisions

- **`RequireAuth` becomes reusable, not `/`-specific.** Same reasoning as BL-005's `RequireAdmin` generalization: a second consumer (`/room-assignment`) needs the identical "any authenticated user" gate. AC-11 exists specifically to catch a regression in this refactor, mirroring BL-005's AC-12.
- **`PersonnelController`/`RoomAssignmentsController` do NOT use `AdminAuthorizationExtensions`.** This screen is reached from the Main Menu, not the Admin Panel — admin-gating it would be a scope error, not a safety improvement. `[Authorize]` alone (JWT bearer, BL-003) is the correct and complete gate.
- **`RoomAssetAssignment` is created now with its full CQ-003-decided shape**, not a minimal `RoomId`/`PersonnelId`-only table that would need an `ALTER TABLE` migration when BL-011 arrives. `AssetId`/`Quantity` are real nullable columns from day one — inert, not stubbed (nothing reads or writes them yet, so there's no fake behavior to taint anything).
- **No `try/catch` on the assignment insert.** Unlike `Room`'s unique-name constraint, `RoomAssetAssignment` has no uniqueness rule to violate (FR-9 — plain inserts, matching the legacy). Only the two explicit pre-checks (selection presence, reference validity) guard this endpoint.
- **This item, not BL-011, physically creates the shared table.** BL-007's proposal assumed BL-011 would introduce `RoomAssetAssignment`; in fact the legacy workflow inserts room-responsibility rows here first. This is noted so a human revisiting BL-007's deferred CQ-023 gap knows the real unblocking point moved earlier.

## Risks & Mitigations

- **Risk:** Generalizing `RequireAuth` changes a file every prior item's route depends on. **Mitigation:** AC-11/AC-12 require the full existing test suite (all `/`, `/admin`, `/room-add`, `/room-update`, `/room-delete` tests) to keep passing unmodified in behavior — only `RequireAuth`'s implementation changes (accepting `children` instead of hardcoding `<MainMenu />`), not its externally observable gating.
- **Risk:** No golden-master fixture exists yet exercising this item's own success/validation paths with confidence (GM-031/GM-032's harness bug, unconfirmed). **Mitigation:** acceptance rests on spec.md's criteria plus manual comparison, same posture every item since BL-005 has taken.
- **Risk:** `RoomAssetAssignment`'s unused `AssetId`/`Quantity` columns could be mistaken for half-built BL-011 scope. **Mitigation:** NFR-2 and this design doc state plainly that they are genuinely unused, not partially wired — nothing in this change's code paths ever reads or writes them.
