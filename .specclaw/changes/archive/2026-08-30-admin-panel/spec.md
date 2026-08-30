# Spec: BL-004 — Admin Panel Sub-Navigation

**Change:** admin-panel
**Created:** 2026-08-28
**Status:** 🟡 Draft

## Overview

Give the ADMİN button (enabled/disabled by BL-003, but never wired to navigate) somewhere to go: a new Admin Panel screen at `/admin`, matching SCR-007's layout, routing to five admin-only sub-screens. Because `/admin` is directly URL-navigable in a way the legacy WinForms button never was, this change also extends DR-003's admin gate to the route itself — reusing BL-003's existing `GET /api/auth/me` check, not inventing a new rule.

## Requirements

### Functional Requirements

- **FR-1:** The Main Menu's ADMİN button navigates to `/admin` on click (it currently has no `onClick` at all).
- **FR-2:** A new Admin Panel screen renders at `/admin`, matching SCR-007: two large buttons side-by-side (Stock Add, Stock Update) in the upper portion, three smaller buttons in a row beneath (Room Delete, Room Add, Room Update).
- **FR-3:** Each of the five buttons navigates to its own client-side route on click: `/stock-add`, `/stock-update`, `/room-delete`, `/room-add`, `/room-update` respectively. None of these five destination screens exist yet — each falls through to the app's existing `NotFound` catch-all route, the same honest current-state pattern BL-002 used for its own four destinations.
- **FR-4:** `/admin` is gated the same way `/` already is (authenticated) plus an admin check: an unauthenticated visitor is redirected to `/login` (reusing the existing token check); an authenticated non-admin visitor (a fresh `GET /api/auth/me` returning `isAdmin: false`, or the call rejecting) is redirected to `/`. Both checks apply regardless of how `/admin` is reached — button click or direct URL entry.
- **FR-5:** While the admin check is pending, `/admin` renders nothing yet (no flash of the Admin Panel's content, and no premature redirect) — the same "safe default until resolved" pattern BL-003 used for the ADMİN button itself.

### Non-Functional Requirements

- **NFR-1:** No backend changes — `/admin`'s gate reuses BL-003's existing `GET /api/auth/me` endpoint as-is.
- **NFR-2:** No numbered `DR-###` rule governs pure routing (per the backlog's own acceptance basis) — DR-003 (admin reachability) is the only rule in play, and it is reused, not re-implemented.

## Acceptance Criteria

- **AC-1:** Clicking the enabled ADMİN button on the Main Menu navigates to `/admin`.
- **AC-2:** At `/admin` (as an admin), all five Admin Panel buttons render with their exact labels, laid out as two large buttons above three smaller ones.
- **AC-3:** Clicking Stock Add navigates to `/stock-add`; Stock Update to `/stock-update`; Room Delete to `/room-delete`; Room Add to `/room-add`; Room Update to `/room-update`. All five currently render the app's `NotFound` page.
- **AC-4:** Visiting `/admin` while unauthenticated (no token) redirects to `/login`.
- **AC-5:** Visiting `/admin` while authenticated but not an admin (`GET /api/auth/me` resolves `isAdmin: false`, or rejects) redirects to `/`.
- **AC-6:** Visiting `/admin` while authenticated as an admin renders the Admin Panel.
- **AC-7:** The rest of the app (Main Menu's other buttons, Sign Out, the login flow) is unaffected by this change.

## Edge Cases

- **Direct URL navigation to `/admin` by a non-admin:** covered by AC-5 — the route itself re-checks, not just the button's disabled state.
- **The admin check is still pending when `/admin` is visited:** no content flashes and no premature redirect (FR-5) — same race-safety pattern as BL-003's button.
- **None of the five destination routes have a real screen yet:** asserting they hit `NotFound` (not asserting what those future screens will contain) is the correct, current-truth test, exactly as BL-002 established for its own four destinations.

## Dependencies

BL-003 (Admin Authorization Gate) — **BUILT**, merged to `master`. This change wires the ADMİN button BL-003 already gates, and reuses BL-003's `GET /api/auth/me` endpoint for the route guard.

## Notes

- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` are still absent (SQ-013 FAITHFUL) — per project decision, screenshots will be captured at the end of the whole backlog; layout built from `ui-inventory.md`'s SCR-007 description.
- **Verification:** per the backlog's own note, no golden-master scenario covers this pure-routing screen (NO BASELINE DATA) — this item's acceptance rests on the criteria above plus manual comparison against the legacy app, not fixture replay.
