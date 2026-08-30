# Proposal: BL-008 — Room to Personnel Assignment (Room Assignment)

**Created:** 2026-08-30
**Status:** 🟢 Approved (approved via user's continue-the-backlog instruction, 2026-08-30)

## Problem

The Main Menu's "ODA TANIMLAMA" button (BL-002) currently falls through to `NotFound` at `/room-assignment` — there is no Room Assignment screen. Unlike Room Add/Update/Delete, this screen is **not admin-only**: it's reached directly from the Main Menu, so any authenticated user can pair a room with its responsible staff member (`frmOdaTanimlama.cs`). The legacy screen has a documented defect (CQ-005): no empty-selection guard and no try/catch around the insert — since `OdaID`/`PersonelID` are nullable columns, an unguarded save would silently insert an orphaned null-assignment row. This item both builds the screen and fixes that defect.

**Two new entities, first introduced here:** `Personnel` (read-only reference data, like `Department`) and `RoomAssetAssignment` (the shared, mixed-purpose table this item's insert shares with BL-011's future asset-issue insert, per CQ-003's already-decided schema shape — one surrogate PK, `RoomId`/`PersonnelId`/`AssetId`/`Quantity` all nullable, no discriminator).

## Proposed Solution

- **`Personnel` entity** (`Id`, `FirstName`, `LastName`) — read-only reference data (CQ-006/CQ-012), same treatment as `Department`.
- **`RoomAssetAssignment` entity** (`Id`, `RoomId?`, `PersonnelId?`, `AssetId?`, `Quantity?`) — the real physical table CQ-003 already decided the shape of. This item wires FK constraints for `RoomId`→`Room` and `PersonnelId`→`Personnel` (both exist); `AssetId` is a plain nullable column for now (no FK yet — `FixedAsset` doesn't exist until BL-009, and no discriminator column exists per CQ-003, so the column is simply unused by this item, not stubbed).
- **`GET /api/personnel`** *(new)* — lists personnel, authenticated (any logged-in user — this screen isn't admin-gated).
- **`POST /api/room-assignments`** *(new)* — creates a room↔personnel assignment. Fixes CQ-005: rejects (400) if either selection is missing, and validates both the room and personnel actually exist (400 if not) before inserting — closing the legacy's silent-orphan-insert gap.
- **Frontend:** a new Room Assignment screen (SCR-006 layout) at `/room-assignment` — two side-by-side selectors (rooms, personnel; reusing BL-006's `listRooms()` for the room list), two disabled echo fields (room name, personnel full name) beneath, and a "KAYDET" (save) button. Gated the same way `/` already is — **`RequireAuth` generalized to accept `children`**, the same refactor BL-005 already did for `RequireAdmin`, since this is the second screen needing "any authenticated user" gating.

## Scope

### In Scope
- `Personnel` and `RoomAssetAssignment` entities + migration (with FK constraints for the two references that exist today)
- `GET /api/personnel` and `POST /api/room-assignments` endpoints, authenticated (not admin-gated)
- Room Assignment screen (SCR-006 layout) at `/room-assignment`, reachable from the Main Menu's "ODA TANIMLAMA" button (already wired by BL-002)
- CQ-005's fix: empty-selection guard, invalid-reference validation
- `RequireAuth` generalized to accept `children` (mirrors BL-005's `RequireAdmin` generalization)
- A minimal dev-seed of `Personnel` rows in the migration (mirroring BL-005's `Department` dev-seed precedent — CQ-006 leaves real provisioning outside this application)

### Out of Scope
- Any `AssetId`/`Quantity` usage on `RoomAssetAssignment` — that's BL-011's insert, once `FixedAsset` (BL-009) exists
- Any admin CRUD for `Personnel` (CQ-012 — read-only reference data)
- Row-order requirements for the two selector lists — CQ-025 already decided this isn't a real requirement; any stable explicit order (by `Id`) is acceptable

## Impact

- **Files affected:** ~10 (estimated) — two new domain entities, a migration, two new controllers, two new frontend API client modules, a new `RoomAssignment.tsx` + CSS, `App.tsx`'s `RequireAuth` generalization + route registration, and backend/frontend tests
- **Complexity:** medium — second full-stack item (after BL-005), introduces two new entities and a genuinely shared, forward-shaped table
- **Risk:** low-medium — the `RequireAuth` refactor touches the existing `/` route (needs its own regression check, mirroring BL-005's `RequireAdmin` AC-12 pattern); the `RoomAssetAssignment` schema shape is pre-decided (CQ-003), reducing design risk

## Open Questions

- **UI grounding missing:** SQ-013 (FAITHFUL) decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent — layout built from `ui-inventory.md`'s SCR-006 description.
- **No success-message text is documented at all for this screen** — `ui-inventory.md`'s own "States evidenced in code" line for SCR-006 says "none beyond the default view — no validation/error branch exists in code." Unlike Room Add/Update/Delete (where at least *some* message text was cited), this screen has zero documented feedback of any kind. A minimal, newly-introduced success message ("Atama başarıyla kaydedildi.") will be used, flagged as a bigger assumption than the Room screens' — there the *existence* of a message was at least confirmed, just not its exact text.
- **GM-030 is captured; GM-031/GM-032 (the silent-null-insert scenarios CQ-005 is fixing) reportedly have a harness parameter-binding bug** (`AddWithValue` null vs `DBNull.Value`) per rebuild-backlog.md's own note — not independently confirmed. This item's CQ-005 fix doesn't depend on resolving that harness bug; it implements the decided guard regardless.
- **Revisits BL-007's deferred CQ-023 gap:** once this item's migration lands, `RoomAssetAssignment` (the table CQ-023's guard checks) will exist for the first time — earlier than BL-007's proposal anticipated (it expected BL-011 to introduce the table). A follow-up to `room-delete` adding CQ-023's FK-guard becomes possible once this item is built; that follow-up is tracked separately, not bundled into this proposal's scope.

---

**To proceed:** Review this proposal and approve to begin planning.
