# Spec: BL-006 — Room Update (rename)

**Change:** room-update
**Created:** 2026-08-30
**Status:** 🟡 Draft

## Overview

Give the Admin Panel's "Oda Güncelle" button (BL-004) a real destination: a Room Update screen at `/room-update`, matching SCR-012's layout, that renames an existing room. The rename is matched by the room's **current name**, not its ID (CQ-004, decided DEFECT/option a — Intentional) — the sole exception to this codebase's otherwise ID-keyed CRUD pattern (Named Gap 4). CQ-004's decision explicitly notes this name-keyed matching "only becomes safe once BL-005's CQ-018 uniqueness constraint exists" — which it now does (BL-005 is built, merged, and verified). No new entity, migration, or authentication mechanism is introduced: this item reuses `Room`/`Department` (BL-005), the `Room.Name` uniqueness constraint (BL-005), `AdminAuthorizationExtensions.IsCallerAdminAsync` (BL-005), and the generalized `RequireAdmin` route guard (BL-005).

## Requirements

### Functional Requirements

- **FR-1:** The Room Update screen renders at `/room-update`, matching SCR-012: a bordered "ODA GÜNCELLEME" section containing an existing-room selector, a new-name field, a "GÜNCELLE" (update) button, and a back control.
- **FR-2:** The existing-room selector is populated from `GET /api/rooms` on screen load — replacing the legacy's live `SELECT * FROM tblOda`-sourced ComboBox with an equivalent API call.
- **FR-3:** Submitting "GÜNCELLE" with a room selected and a non-empty new name calls `PUT /api/rooms` with `{ oldName, newName }`. On success, both fields reset, the selector re-populates (refetching the current room list, matching the legacy's "combo re-populated" success state), and a success message ("Oda başarıyla güncellendi.") is shown.
- **FR-4:** An empty or whitespace-only new name is rejected before any API call. *(Judgment call, not a legacy-documented rule — see Notes: functional-spec.md's DR-004 form list does not include `frmOdaGuncelle.cs`, so the legacy screen has no required-field check on this field at all. This spec adds one anyway for consistency with Room Add's validated new-name field, using the same `ROOM_NAME_REQUIRED` contract.)*
- **FR-5:** Renaming to a name already used by a *different* existing room is rejected by the real uniqueness constraint on `Room.Name` (BL-005) and surfaced with the legacy screen's own generic error message ("Hatalı İşlem...") — the legacy screen has only one generic catch for any failure (`frmOdaGuncelle.cs:73-76`), so this item reuses that single string rather than inventing a distinct duplicate-specific message the legacy screen never had.
- **FR-6:** If the selected room's current name no longer matches any room (a stale selection), the rename is rejected with the same generic error message ("Hatalı İşlem...") and a `404` status, rather than the legacy's silent no-op success (GM-024). *(Judgment call — no CQ decision addresses this; treated as an honest rejection rather than reproducing the legacy no-op, consistent with how BL-005 already fixed CQ-008's silent no-op field-clear.)*
- **FR-7:** `/room-update` is gated the same way `/admin` and `/room-add` already are: unauthenticated → `/login`; authenticated non-admin → `/`; pending check → renders nothing. Reuses BL-005's generalized `RequireAdmin` guard as-is — no further changes to `RequireAdmin` itself.
- **FR-8:** `GET /api/rooms` and `PUT /api/rooms` independently enforce admin-only access server-side (`403` for an authenticated non-admin caller), reusing BL-005's `AdminAuthorizationExtensions.IsCallerAdminAsync`.
- **FR-9:** A back control on the Room Update screen navigates to `/admin`, matching the legacy screen's own back-to-Admin-Panel navigation (`btnOdaEkleSilBack_Click`).
- **FR-10:** The rename is matched by the room's current name (`oldName`), not by its database ID — CQ-004's decided keying, preserved intentionally.

### Non-Functional Requirements

- **NFR-1:** Reuses the JWT bearer authentication (BL-003) and the admin-check helper (BL-005) — no new authentication or authorization mechanism.
- **NFR-2:** No new database migration — `Room`/`Department` entities and the `Room.Name` uniqueness constraint already exist from BL-005; this item is API + UI only.
- **NFR-3:** The server-side admin check re-queries `YetkiID` fresh per request (the same pattern `GET /api/auth/me` and BL-005's endpoints already use).

## Acceptance Criteria

Each criterion must pass for the change to be considered complete.

- **AC-1:** At `/room-update` (as an admin), the screen renders the existing-room selector, new-name field, and "GÜNCELLE" button (SCR-012 layout).
- **AC-2:** The existing-room selector is populated with all current room names from `GET /api/rooms`.
- **AC-3:** Selecting a room, entering a non-empty new name, and submitting succeeds: `PUT /api/rooms` is called with `{ oldName, newName }`, the room is renamed, both fields reset, the selector re-populates, and "Oda başarıyla güncellendi." is shown.
- **AC-4:** Submitting with an empty or whitespace-only new name shows a validation indicator and does not call `PUT /api/rooms`.
- **AC-5:** Submitting a new name that collides with a *different* existing room calls `PUT /api/rooms`, which responds `409 Conflict`, and the UI shows "Hatalı İşlem...".
- **AC-6:** Visiting `/room-update` while unauthenticated (no token) redirects to `/login`.
- **AC-7:** Visiting `/room-update` while authenticated but not an admin redirects to `/`.
- **AC-8:** Visiting `/room-update` while authenticated as an admin renders the screen.
- **AC-9:** Calling `PUT /api/rooms` directly (bypassing the UI) as an authenticated non-admin returns `403 Forbidden`.
- **AC-10:** Calling `GET /api/rooms` directly as an authenticated non-admin returns `403 Forbidden`.
- **AC-11:** The back control navigates to `/admin`.
- **AC-12:** `/admin`'s and `/room-add`'s existing behavior (all of BL-004's and BL-005's own acceptance criteria) is unaffected by this change — regression check. Unlike BL-005, this item does not modify `RequireAdmin`'s signature — it only adds a third route through the existing guard.
- **AC-13:** Submitting a rename where `oldName` no longer matches any room returns `404 Not Found` and the UI shows "Hatalı İşlem...".

## Edge Cases

- **Renaming a room to its own unchanged current name:** succeeds as a no-op (the row's `Name` column is set to the same value it already holds, so no uniqueness violation occurs) — not a special case to code for separately, a natural consequence of relying on the DB constraint alone rather than a pre-check query.
- **A stale existing-room selection** (the selector was populated at screen load, but the room was renamed/deleted by another session before submit): covered by FR-6/AC-13 — rejected with `404`, not a silent no-op.
- **GM-025's "multi-row rename" legacy scenario** (renaming when duplicate names already existed under the pre-constraint schema) is unreachable once BL-005's uniqueness constraint is enforced — historical/legacy-parity reference only, not a target behavior.

## Dependencies

BL-005 (Room Add) — **BUILT**, merged to `master` (commit `379cc66`), verify PASS (see `.specclaw/analysis/rebuild-backlog.md`'s BL-005 status notes). This change reuses BL-005's `Room`/`Department` entities, the `Room.Name` uniqueness constraint, `AdminAuthorizationExtensions`, and the generalized `RequireAdmin` guard as-is.

## Notes

- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` are still absent (SQ-013 decided FAITHFUL) — layout built from `ui-inventory.md`'s SCR-012 description, consistent with BL-001 through BL-005.
- **No exact success-message text is documented for this screen** — `ui-inventory.md` only says "message shown" (`frmOdaGuncelle.cs:67-71`), unlike Room Add's message which functional-spec.md quotes verbatim. "Oda başarıyla güncellendi." is used as a natural analogue to Room Add's confirmed message — an assumption, not a legacy-parity-verified string.
- **The legacy generic error message IS documented** ("Hatalı İşlem...", `frmOdaGuncelle.cs:73-76`) and is reused here for every server-side failure path (not-found old name, duplicate new name) rather than inventing distinct per-case strings, matching the legacy screen's own single-generic-catch behavior.
- **Verification:** `GM-023`/`GM-024`/`GM-025` are PENDING CAPTURE and `GM-025` describes pre-constraint legacy behavior no longer reachable — this item's acceptance rests on the criteria above plus manual comparison against the legacy app, not fixture replay, until fresh golden-master data exists against the constrained schema.
