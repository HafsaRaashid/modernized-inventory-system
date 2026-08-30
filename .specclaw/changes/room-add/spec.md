# Spec: BL-005 — Room Add

**Change:** room-add
**Created:** 2026-08-30
**Status:** 🟡 Draft

## Overview

Give the Admin Panel's "Oda Ekle" button (BL-004) a real destination: a Room Add screen at `/room-add`, matching SCR-010's layout, that creates a room under a department. This is the first backlog item requiring real persistence in the rebuild — new `Room` and `Department` entities, an EF Core migration, and two new API endpoints. It also fixes two documented legacy defects: the post-save field-clear that never actually runs (CQ-008), and the complete absence of a database uniqueness constraint on the room name despite a duplicate-catch error message implying one exists (CQ-018). Because `/room-add` is directly URL-navigable in the same way `/admin` is, this item also extends the admin gate — both at the route level (reusing and generalizing BL-004's `RequireAdmin`) and, for the first time in this rebuild, at the backend endpoint level, which BL-004's own proposal explicitly deferred to "each [destination]'s own backlog item."

## Requirements

### Functional Requirements

- **FR-1:** The Room Add screen renders at `/room-add`, matching SCR-010: a bordered "ODA EKLEME" section containing a room-name field, a department picker (paired ID/name list, not free text), a disabled department-ID echo field, a centered "EKLE" (add) button, and a back control.
- **FR-2:** The department picker is populated from `GET /api/departments` on screen load. Selecting a department sets the disabled echo field to that department's ID — the same paired-list interaction as the legacy screen, not a free-typed value.
- **FR-3:** Clicking "EKLE" with a non-empty room name and a selected department calls `POST /api/rooms`. On success, the room-name field and department selection are genuinely reset (fixing CQ-008's legacy no-op, which never actually cleared the field) and a success message ("Oda başarıyla eklendi.") is shown.
- **FR-4:** An empty or whitespace-only room name is rejected before any API call (DR-004, non-empty check on room name only) — the same client-side gate GM-020 confirms genuinely matches this screen's actual save condition, not a second path that merely happens to agree.
- **FR-5:** A duplicate room name is rejected by a real database uniqueness constraint on `Room.Name` (CQ-018 — the legacy database enforces none despite its misleading duplicate-catch message) and surfaced to the user as "Kayıtlı Oda...". Unlike the legacy app, this is now a genuine rejection, not a silent success.
- **FR-6:** A department selection is required before submitting — the picker must have a selection before "EKLE" is enabled/submittable. *(Not itself a numbered DR-### rule; see Open Questions — this closes an otherwise-unhandled gap rather than reproducing GM-022's legacy "no department selected" uncaught-exception behavior, which has no web equivalent worth preserving.)*
- **FR-7:** `/room-add` is gated the same way `/admin` already is: an unauthenticated visitor is redirected to `/login`; an authenticated non-admin visitor (`GET /api/auth/me` resolving `isAdmin: false`, or rejecting) is redirected to `/`; while the check is pending, nothing renders yet. This reuses BL-004's `RequireAdmin` guard, generalized to wrap any admin-only screen rather than hardcoding `AdminPanel`.
- **FR-8:** `POST /api/rooms` and `GET /api/departments` independently enforce admin-only access server-side (403 for an authenticated non-admin caller) — the first backend endpoints in this rebuild to do so, closing the gap BL-004 explicitly left open ("any backend authorization on those destination endpoints — each is scoped by its own backlog item").
- **FR-9:** A back control on the Room Add screen navigates to `/admin`, matching the legacy screen's own back-to-Admin-Panel navigation (`btnOdaEkleSilBack_Click`).

### Non-Functional Requirements

- **NFR-1:** The new endpoints reuse the JWT bearer authentication already registered by BL-003 — no new authentication mechanism.
- **NFR-2:** The server-side admin check re-queries `YetkiID` fresh per request (the same "fail-closed, always re-check" pattern `GET /api/auth/me` already uses), not a cached/trusted JWT claim.
- **NFR-3:** `Department` remains read-only reference data in this rebuild (CQ-012, decided SCOPE) — no admin CRUD screen is introduced for it; this item only reads department rows.

## Acceptance Criteria

Each criterion must pass for the change to be considered complete.

- **AC-1:** At `/room-add` (as an admin), the screen renders the room-name field, department picker, disabled department-ID echo field, and "EKLE" button (SCR-010 layout).
- **AC-2:** Selecting a department in the picker sets the disabled echo field to that department's ID.
- **AC-3:** Submitting a non-empty room name with a selected department succeeds: `POST /api/rooms` is called, the room is created, the room-name field and department selection reset, and "Oda başarıyla eklendi." is shown.
- **AC-4:** Submitting an empty or whitespace-only room name shows a validation indicator and does not call `POST /api/rooms`.
- **AC-5:** Submitting a duplicate room name calls `POST /api/rooms`, which responds `409 Conflict`, and the UI shows "Kayıtlı Oda...".
- **AC-6:** Visiting `/room-add` while unauthenticated (no token) redirects to `/login`.
- **AC-7:** Visiting `/room-add` while authenticated but not an admin redirects to `/`.
- **AC-8:** Visiting `/room-add` while authenticated as an admin renders the screen.
- **AC-9:** Calling `POST /api/rooms` directly (bypassing the UI) as an authenticated non-admin returns `403 Forbidden`.
- **AC-10:** Calling `GET /api/departments` directly as an authenticated non-admin returns `403 Forbidden`.
- **AC-11:** The back control navigates to `/admin`.
- **AC-12:** `/admin`'s existing behavior (BL-004, all of its own acceptance criteria) is unaffected by generalizing `RequireAdmin` to accept a child element — regression check.
- **AC-13:** Submitting with no department selected is prevented client-side (does not call `POST /api/rooms`); if reached server-side anyway (a direct API call with an invalid/omitted `departmentId`), the server rejects it with `400 Bad Request` rather than a raw FK-constraint error.

## Edge Cases

- **No departments exist yet:** the picker renders empty and submission remains blocked by FR-6/AC-13 (no selection possible) — this item does not seed real department data (see Notes); a human-provisioned or minimal dev-seed row is required to exercise the picker at all, consistent with CQ-012 (departments are provisioned outside this application).
- **Invalid `departmentId` sent directly to the API** (not reachable via the UI, but reachable via a direct call): rejected with `400 Bad Request`, not a raw database FK-violation error (AC-13).
- **`GM-021` cannot be reused as a rebuild acceptance fixture:** the legacy "duplicate name currently succeeds" capture describes pre-constraint behavior; once CQ-018's constraint exists, a duplicate must be rejected (AC-5), so `GM-021`'s original outcome is historical evidence only, not this item's target.
- **`GM-022` (legacy: no department selected throws an uncaught exception):** not reproduced — FR-6/AC-13 replace it with an ordinary validation rejection, since an uncaught exception has no honest web equivalent worth preserving.

## Dependencies

BL-004 (Admin Panel Sub-Navigation) — **BUILT**, merged to `master` (commit `90b51c1`), verify PASS (see `.specclaw/analysis/rebuild-backlog.md`'s BL-004 status notes). This change is the real destination for the Admin Panel's "Oda Ekle" button and reuses/generalizes BL-004's `RequireAdmin` guard.

## Notes

- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` are still absent (SQ-013 decided FAITHFUL) — per project decision, screenshots will be captured at the end of the whole backlog; layout built from `ui-inventory.md`'s SCR-010 description.
- **Verification:** per the backlog's own note, `GM-019`/`GM-020`/`GM-022` are PENDING CAPTURE and `GM-021` cannot be reused unmodified (see Edge Cases) — this item's acceptance rests on the criteria above plus manual comparison against the legacy app, not fixture replay, until a human captures fresh golden-master data against the constrained schema.
- **Minimal dev seed:** since `Department` is populated entirely outside this application (CQ-012) and no real data-migration path exists yet, the migration will include a small number of placeholder department rows for local dev/test only — not a stand-in for real department provisioning.
- **Open question carried from the proposal:** FR-6 (require a department selection before submit) fills a gap the backlog's acceptance basis does not explicitly decide (only DR-004's room-name check is numbered) — flagged here as a judgment call, not a legacy-parity-verified rule.
