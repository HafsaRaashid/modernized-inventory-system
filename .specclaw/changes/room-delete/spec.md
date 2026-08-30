# Spec: BL-007 — Room Delete

**Change:** room-delete
**Created:** 2026-08-30
**Status:** 🟡 Draft

## Overview

Give the Admin Panel's "Oda Sil" button (BL-004) a real destination: a Room Delete screen at `/room-delete`, matching SCR-011's layout, that deletes an existing room with **no confirmation dialog** (CQ-017, decided DEFECT: reproduce as-is, per the faithful-by-default policy SQ-012). The delete is matched by the room's current name, not its ID (CQ-004, same pattern as BL-006, safe under BL-005's uniqueness constraint). No new entity, migration, or authentication mechanism: this item reuses `Room` (BL-005), `GET /api/rooms` (BL-006), `AdminAuthorizationExtensions.IsCallerAdminAsync` (BL-005), and the unchanged `RequireAdmin` route guard (BL-005).

**Explicitly deferred scope (see "Item Split (informal)" in proposal.md):** this item's own acceptance basis (CQ-023, decided) requires the rebuild to reject deleting a room that still has assigned assets/personnel, as a real reportable error rather than a crash or a silent orphan. That guard depends on a `RoomAssetAssignment` entity that belongs to MOD-003 (Asset Assignment & Stock) and does not exist in this schema yet — its own backlog item, BL-011, hasn't been built. **This change does not implement that guard.** Deleting a room always succeeds (subject to the not-found check below) regardless of any hypothetical future assignments. No `IS-###` split record exists for this deferral — `specclaw-bf-rebuild-collect split-append` mechanically refuses items whose acceptance basis cites no `DR-###` rule, and this item cites none (only `CQ-###` decisions) — so a human must manually revisit this item once BL-011 lands; there is no automatic tracking.

## Requirements

### Functional Requirements

- **FR-1:** The Room Delete screen renders at `/room-delete`, matching SCR-011: a bordered "ODA SİLME" section containing a room selector and a "SİL" (delete) button in a single row, plus a back control.
- **FR-2:** The room selector is populated from `GET /api/rooms` (reused from BL-006) on screen load.
- **FR-3:** Clicking "SİL" with a room selected calls `DELETE /api/rooms` with `{ name }` immediately — **no confirmation dialog** (FR-4). On success, the selector clears and re-populates (matching the legacy "selector cleared and re-populated" success state) and a success message ("Oda başarıyla silindi.") is shown.
- **FR-4:** No confirmation step of any kind precedes the delete (CQ-017, decided DEFECT: reproduce as-is — the legacy screen has none, and the faithful-by-default policy preserves that here rather than "fixing" it into a safer UX).
- **FR-5:** If the selected room's current name no longer matches any room (a stale selection), the delete is rejected with a `404` and an honest error message — not a silent no-op — the same posture BL-006 already established for its own not-found case (FR-6/AC-13 there).
- **FR-6:** `/room-delete` is gated the same way `/admin`, `/room-add`, and `/room-update` already are: unauthenticated → `/login`; authenticated non-admin → `/`; pending check → renders nothing. Reuses BL-005's `RequireAdmin` guard completely unchanged — no modification to `RequireAdmin` itself.
- **FR-7:** `DELETE /api/rooms` independently enforces admin-only access server-side (`403` for an authenticated non-admin caller), reusing BL-005's `AdminAuthorizationExtensions.IsCallerAdminAsync`. (`GET /api/rooms`'s own admin gate already exists from BL-006 and is unchanged.)
- **FR-8:** A back control on the Room Delete screen navigates to `/admin`, matching the legacy screen's own back-to-Admin-Panel navigation (`btnOdaEkleSilBack_Click`).
- **FR-9:** The delete is matched by the room's current name (`name`), not by its database ID — CQ-004's decided keying, preserved intentionally, same pattern BL-006 already established for rename.
- **FR-10 (deferred-scope assertion):** `DELETE /api/rooms` contains no check against `RoomAssetAssignment` or any similar assignment concept — CQ-023's FK-guard is genuinely absent from this change, not partially wired. Deleting a room always succeeds once FR-5's not-found check passes.

### Non-Functional Requirements

- **NFR-1:** Reuses the JWT bearer authentication (BL-003) and the admin-check helper (BL-005) — no new authentication or authorization mechanism.
- **NFR-2:** No new database migration — `Room` already exists from BL-005; this item is API + UI only.
- **NFR-3:** The server-side admin check re-queries `YetkiID` fresh per request (the same pattern `GET /api/auth/me` and BL-005/BL-006's endpoints already use).

## Acceptance Criteria

Each criterion must pass for the change to be considered complete.

- **AC-1:** At `/room-delete` (as an admin), the screen renders the room selector and "SİL" button in a single row (SCR-011 layout).
- **AC-2:** The room selector is populated with all current room names from `GET /api/rooms`.
- **AC-3:** Selecting a room and clicking "SİL" deletes it immediately with no confirmation dialog: `DELETE /api/rooms` is called with `{ name }`, the room is deleted, the selector clears and re-populates, and "Oda başarıyla silindi." is shown.
- **AC-4:** Calling `DELETE /api/rooms` directly (bypassing the UI) as an authenticated non-admin returns `403 Forbidden`.
- **AC-5:** Calling `GET /api/rooms` directly as an authenticated non-admin still returns `403 Forbidden` (regression check — this endpoint's gating is unchanged from BL-006).
- **AC-6:** Visiting `/room-delete` while unauthenticated (no token) redirects to `/login`.
- **AC-7:** Visiting `/room-delete` while authenticated but not an admin redirects to `/`.
- **AC-8:** Visiting `/room-delete` while authenticated as an admin renders the screen.
- **AC-9:** The back control navigates to `/admin`.
- **AC-10:** `/admin`'s, `/room-add`'s, and `/room-update`'s existing behavior (all of BL-004's/BL-005's/BL-006's own acceptance criteria) is unaffected by this change — regression check. `RequireAdmin`'s signature is not modified.
- **AC-11:** Deleting a room whose name no longer matches any room (a stale selection) returns `404 Not Found` and the UI shows an honest error message, not a silent success.
- **AC-12:** After a successful delete, the deleted room no longer appears in a subsequent `GET /api/rooms` call — confirms actual persistence-level deletion, not just a `200` response.
- **AC-13 (deferred-scope absence, mandatory per split discipline):** No code path in this change references `RoomAssetAssignment` or checks for existing room assignments before deleting — confirmed by inspection of the changed files. A room is deletable regardless of any hypothetical future assignment data, since CQ-023's guard is explicitly out of scope (see Overview).

## Edge Cases

- **Deleting the last remaining room:** succeeds normally; the selector becomes empty afterward (nothing left to select).
- **CQ-023's FK-guard:** out of scope for this change entirely (FR-10/AC-13) — a room with hypothetical existing asset/personnel assignments (once BL-011 exists) can still be deleted by this endpoint with no warning, until a human manually revisits this item.
- **GM-029** (the legacy crash fixture for deleting a room with an assignment) is not a target for this change — it pins the legacy crash, and this item doesn't implement the guard that fixture concerns at all.

## Dependencies

BL-005 (Room Add) — **BUILT**, merged to `master` (commit `379cc66`), verify PASS. BL-006 (Room Update) — **BUILT**, merged to `master`, verify PASS (13/13 acceptance criteria). This change reuses BL-005's `Room` entity/`AdminAuthorizationExtensions`/`RequireAdmin`, and BL-006's `GET /api/rooms`, all as-is.

**Not a met dependency, explicitly deferred:** BL-011 (Asset Assignment and Stock Decrement, MOD-003) — introduces `RoomAssetAssignment`, which CQ-023's guard needs. Not built; this item does not wait for it (see Overview).

## Notes

- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` are still absent (SQ-013 decided FAITHFUL) — layout built from `ui-inventory.md`'s SCR-011 description, consistent with BL-001 through BL-006.
- **No exact success-message text is documented** — `ui-inventory.md` only says "message shown" (`frmOdaSil.cs:67-70`), the same gap BL-006 had. "Oda başarıyla silindi." is used as a natural analogue, an assumption not a legacy-parity-verified string.
- **No legacy error-message text is documented at all for this screen** — unlike Room Update (which has a documented generic "Hatalı İşlem..." catch), `ui-inventory.md`'s SCR-011 only documents a `default` and a `success` state, no error state. This change reuses BL-006's "Hatalı İşlem..." string for its own not-found case, for consistency across the admin screens — an assumption, not a citation specific to this screen.
- **Verification:** GM-026 (success, no children), GM-027 (no-op on nonexistent name — this change treats this as a 404, not a no-op, per FR-5), GM-028 (multi-row delete side-effect, pre-constraint, historical only) are captured fixtures per rebuild-backlog.md. GM-029 (deleting a room with an assignment) pins the legacy crash and is not this change's target (see Edge Cases).
