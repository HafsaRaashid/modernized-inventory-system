# Spec: BL-008 — Room to Personnel Assignment (Room Assignment)

**Change:** room-assignment
**Created:** 2026-08-30
**Status:** 🟡 Draft

## Overview

Give the Main Menu's "ODA TANIMLAMA" button (BL-002) a real destination: a Room Assignment screen at `/room-assignment`, matching SCR-006's layout, that pairs a room with its responsible personnel. Unlike BL-005/006/007, this screen is **not admin-only** — it's reached directly from the Main Menu, so `RequireAuth` (the guard for `/`) is generalized to accept `children`, the same refactor BL-005 already did for `RequireAdmin`. Introduces `Personnel` (read-only reference data, like `Department`) and `RoomAssetAssignment` — the real, physical table CQ-003 already decided the shape of (one surrogate PK, `RoomId`/`PersonnelId`/`AssetId`/`Quantity` all nullable, no discriminator, shared with BL-011's future asset-issue insert). Fixes CQ-005 (decided DEFECT): the legacy screen has no empty-selection guard and no try/catch, so an unguarded save silently inserts an orphaned null-assignment row — this item adds the guard.

## Requirements

### Functional Requirements

- **FR-1:** The Room Assignment screen renders at `/room-assignment`, matching SCR-006: two side-by-side selectors (rooms, personnel), two disabled echo fields beneath (room name, personnel full name), and a "KAYDET" (save) button, plus a back control.
- **FR-2:** The room selector is populated from `GET /api/rooms` (reused from BL-006); the personnel selector is populated from `GET /api/personnel` (new).
- **FR-3:** Selecting a room echoes its name into the disabled "Oda Adı" field; selecting a personnel echoes their full name into the disabled "Oda Sorumlusu" field.
- **FR-4:** Clicking "KAYDET" with both a room and a personnel selected calls `POST /api/room-assignments` with `{ roomId, personnelId }`. On success, both selections reset, the echo fields clear, and a success message ("Atama başarıyla kaydedildi.") is shown.
- **FR-5 (CQ-005 fix):** Clicking "KAYDET" with either selection missing is rejected before any API call — the legacy screen's Named Gap (no guard at all) is closed here, not reproduced.
- **FR-6 (CQ-005 fix, server-side):** `POST /api/room-assignments` independently validates both `roomId` and `personnelId` are present (`400` if not) and that both reference an existing `Room`/`Personnel` row (`400` if not) — closing the legacy's silent-orphan-insert gap at the API boundary too, not just the UI.
- **FR-7:** `/room-assignment` is gated the same way `/` already is: unauthenticated → `/login`; authenticated → renders the screen. No admin check — reuses the generalized `RequireAuth`, not `RequireAdmin`.
- **FR-8:** A back control on the Room Assignment screen navigates to `/` (Main Menu), matching the legacy screen's own back navigation (`btnTanimlamaBack_Click`).
- **FR-9:** Each successful save inserts a new row — there is no update-if-exists/upsert behavior, matching the legacy's plain `INSERT` (a room can accumulate multiple assignment rows over time; this is a journal, not a "current responsible person" pointer).

### Non-Functional Requirements

- **NFR-1:** Reuses the JWT bearer authentication (BL-003) — no new authentication mechanism, and no admin-check helper is used at all (this screen doesn't need one).
- **NFR-2:** `RoomAssetAssignment`'s `AssetId`/`Quantity` columns are created now (per CQ-003's decided shape) but are not populated or referenced by anything in this change — genuinely unused until BL-011.
- **NFR-3:** `Personnel` remains read-only reference data (CQ-006/CQ-012) — no admin CRUD screen is introduced for it.

## Acceptance Criteria

Each criterion must pass for the change to be considered complete.

- **AC-1:** At `/room-assignment` (as any authenticated user), the screen renders both selectors, both echo fields, and the "KAYDET" button (SCR-006 layout).
- **AC-2:** The room selector is populated from `GET /api/rooms`; the personnel selector is populated from `GET /api/personnel`.
- **AC-3:** Selecting a room sets the room-name echo field; selecting a personnel sets the personnel-name echo field.
- **AC-4:** Selecting both a room and a personnel and clicking "KAYDET" succeeds: `POST /api/room-assignments` is called with `{ roomId, personnelId }`, both selections and echo fields reset, and "Atama başarıyla kaydedildi." is shown.
- **AC-5:** Clicking "KAYDET" with no room selected, no personnel selected, or neither selected does not call `POST /api/room-assignments`.
- **AC-6:** Calling `POST /api/room-assignments` directly with a missing `roomId` or `personnelId` returns `400 Bad Request`.
- **AC-7:** Calling `POST /api/room-assignments` directly with a `roomId`/`personnelId` that doesn't reference an existing `Room`/`Personnel` returns `400 Bad Request`.
- **AC-8:** Visiting `/room-assignment` while unauthenticated redirects to `/login`.
- **AC-9:** Visiting `/room-assignment` while authenticated (regardless of admin status) renders the screen — this is not an admin-gated route.
- **AC-10:** The back control navigates to `/` (Main Menu).
- **AC-11:** `/`'s existing behavior (BL-001/BL-002's own acceptance criteria) is unaffected by generalizing `RequireAuth` to accept a child element — regression check.
- **AC-12:** `/admin`, `/room-add`, `/room-update`, and `/room-delete`'s existing behavior is unaffected by this change (regression check — `RequireAdmin` is untouched).
- **AC-13:** Saving the same room-personnel pair twice creates two separate `RoomAssetAssignment` rows (no upsert) — confirms FR-9's journal semantics, not a silent no-op or a replace.

## Edge Cases

- **No rooms or no personnel exist yet:** both selectors render empty; "KAYDET" stays unreachable (AC-5 covers this — nothing can be selected).
- **`RoomAssetAssignment`'s `AssetId`/`Quantity` columns:** created but genuinely unused by this change (NFR-2) — not a partial implementation of BL-011's scope, just the pre-decided shared-table shape existing ahead of its second writer.
- **GM-031/GM-032** (the legacy's silent-null-insert scenarios) are not reproduced — CQ-005 explicitly decided to add the guard instead, closing the gap rather than pinning it.

## Dependencies

BL-002 (Main Menu Navigation Hub) — **BUILT**, merged to `master`, verify PASS. BL-005 (Room Add) — **BUILT**, merged to `master`, verify PASS. This change reuses BL-002's Main Menu button/route slot, and BL-005's `Room` entity plus BL-006's `GET /api/rooms`.

## Notes

- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` are still absent (SQ-013 decided FAITHFUL) — layout built from `ui-inventory.md`'s SCR-006 description.
- **No success-message text is documented at all for this screen** (`ui-inventory.md`: "States evidenced in code: none beyond the default view"). "Atama başarıyla kaydedildi." is a newly-introduced message, a bigger assumption than BL-005/006/007's — those at least had a confirmed message *existing*, just not its exact text.
- **This item revisits BL-007's deferred CQ-023 gap earlier than expected:** `RoomAssetAssignment` (the table CQ-023's Room-Delete guard checks) is created by *this* item, not BL-011 as BL-007's proposal assumed. A follow-up to `room-delete` adding that guard is now possible and is tracked as a separate, small change immediately after this one — not bundled into this item's scope.
- **Verification:** GM-030 is a captured fixture (success case). GM-031/GM-032 (silent-null-insert) reportedly have a harness parameter-binding bug per rebuild-backlog.md, unconfirmed — this item's CQ-005 fix doesn't depend on resolving that.
