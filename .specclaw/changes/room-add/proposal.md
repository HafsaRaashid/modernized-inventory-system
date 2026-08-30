# Proposal: BL-005 — Room Add

**Created:** 2026-08-30
**Status:** 🟢 Approved (approved via /specclaw:auto invocation, 2026-08-30)

## Problem

The Admin Panel's "Oda Ekle" (Room Add) button (BL-004) currently falls through to `NotFound` at `/room-add` — there is no Room Add screen, and no backend support for rooms at all yet. This is the first backlog item that needs real persistence: no `Room` or `Department` entity, table, or API endpoint exists in the rebuild so far (only `User` does). The legacy screen (`frmOdaEkle.cs`) lets an admin type a room name, pick a department from a paired ID/name list (populated read-only from `tblDepartmanlar`), and insert the row — but its post-save field-clear is a structural no-op (per CQ-008, the clearing loop never recurses into the GroupBox holding the real field), and the legacy database enforces no uniqueness constraint on the room name at all despite a duplicate-catch error message implying one exists (per CQ-018).

## Proposed Solution

- **New `Room` entity** (`OdaID`, `OdaAdi`, `DepartmanID` FK) with a real EF Core migration, adding a genuine unique constraint on `OdaAdi` — fixing the legacy gap identified by CQ-018 rather than reproducing it.
- **New `Department` entity** (`DepartmanID`, `DepartmanAdi`) as a read-only reference table. Per CQ-012 (decided SCOPE), no admin CRUD screen is built for it in this rebuild — departments are provisioned outside the app, same as the legacy assumption. This item only needs to *read* department rows to populate the picker.
- **Backend:** a `POST /api/rooms` endpoint (create) and a `GET /api/departments` endpoint (list, for the picker) on top of the new entities/migration.
- **Frontend:** a new Room Add screen at `/room-add`, matching SCR-010's layout — a bordered "ODA EKLEME" section with a room-name field, a department picker (list, not free text — selecting one echoes its ID into a disabled field, matching the legacy paired-list interaction), and a centered Add button.
- **Validation (DR-004):** non-empty check on the room name only, applied consistently client- and server-side (the legacy app's `ErrorProvider` and its actual gate happened to agree on this screen, per GM-020 — this rebuild keeps them as one path, not two that coincidentally match).
- **Real field-clearing on success** (per CQ-008, decided DEFECT/fix): after a successful add, the room-name field and department selection genuinely reset, unlike the legacy no-op.
- **Duplicate-name handling:** with the new uniqueness constraint in place, a duplicate room name is now genuinely rejected (not silently accepted, as in the legacy app) — the UI shows the existing "Kayitli Oda..." (name already registered) message, but backed by a real constraint this time.

## Scope

### In Scope
- `Room` and `Department` entities + EF Core migration (including the new `OdaAdi` uniqueness constraint)
- `POST /api/rooms` (create) and `GET /api/departments` (list) endpoints
- Room Add screen (SCR-010 layout) at `/room-add`, reachable from the Admin Panel's "Oda Ekle" button (already wired by BL-004)
- Required-field validation on room name (DR-004), client- and server-side, as one agreeing path
- Genuine post-success field-clearing (fixing CQ-008's legacy no-op)
- Duplicate room-name rejection backed by a real DB constraint (CQ-018)

### Out of Scope
- Room Update / Room Delete (BL-006 / BL-007 — separate backlog items, same `Room` entity)
- Any admin CRUD for `Department` (CQ-012 — departments are read-only reference data in this rebuild)
- Seeding/migrating actual legacy department data into the new schema — a data-migration concern outside this item; local/dev environments will need at least one `Department` row to exercise the picker, which this item's migration will include as a minimal seed for testability only, not as a stand-in for real data migration
- Room-to-personnel assignment (BL-008) or asset-related room usage (BL-011) — this item only creates the room record itself

## Impact

- **Files affected:** ~8–10 (estimated) — new `Room.cs`/`Department.cs` domain entities, an EF Core migration, a `RoomsController`/`DepartmentsController` (or equivalent), an API client module for the frontend, a new `RoomAdd.tsx` + CSS, and backend/frontend tests
- **Complexity:** medium — first item requiring new persistence (entity + migration + endpoint), not just routing
- **Risk:** medium — schema/migration work is harder to walk back than a pure frontend change; the new uniqueness constraint is a deliberate behavior change from the legacy app (CQ-018) and should be called out clearly in the spec's acceptance criteria so it isn't mistaken for scope creep

## Open Questions

- **UI grounding missing:** SQ-013 (FAITHFUL) is decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent — per project decision, screenshots will be captured at the end of the whole backlog; this item's layout is built from `ui-inventory.md`'s SCR-010 description, consistent with how BL-001 through BL-004 already shipped.
- **CQ-027 (non-blocking, unanswered):** duplicate/decoupled validation paths for required-field checks (DR-004) generally — doesn't block this item since GM-020 already confirms this specific screen's two paths agree.
- **GM-021 cannot be reused as-is:** the legacy "duplicate name currently succeeds" fixture no longer describes the rebuild's intended behavior once CQ-018's constraint is in place; a human will need to capture a new expected-*rejection* fixture once the migration exists, rather than expecting this item's tests to reproduce GM-021's original outcome.
- **Minimal seed data for the department picker:** since Department is populated entirely outside the app (CQ-012) and no real data-migration path exists yet, local dev/test needs at least a placeholder department row to exercise the picker end-to-end — confirming this is acceptable as a dev-only seed (not real provisioning) before planning.

---

**To proceed:** Review this proposal and approve to begin planning.
