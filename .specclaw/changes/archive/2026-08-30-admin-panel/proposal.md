# Proposal: BL-004 — Admin Panel Sub-Navigation

**Created:** 2026-08-28
**Status:** 🟢 Approved (auto-approved per user's "run specclaw from proposal to verify for all rebuild-backlog.md" instruction, 2026-08-28)

## Problem

The Main Menu's ADMİN button (BL-002) has never had a click handler at all — BL-003 wired up its enabled/disabled state but not its destination. There is also no `/admin` route yet, so even an enabled ADMİN button would have nowhere to go. The legacy `frmAdmin.cs` is a pure router: five buttons opening Stock Add, Stock Update, Room Delete, Room Add, and Room Update, with no database access of its own.

## Proposed Solution

- Add the ADMİN button's missing `onClick={() => navigate("/admin")}` (the four other Main Menu buttons already have theirs from BL-002).
- Add a new Admin Panel screen at `/admin`, matching SCR-007's layout: two large side-by-side buttons (Stock Add, Stock Update) in the upper portion, three smaller buttons in a row beneath (Room Delete, Room Add, Room Update).
- Each of the five buttons navigates to its own client-side route (`/stock-add`, `/stock-update`, `/room-delete`, `/room-add`, `/room-update`). None of these five destination screens exist yet — each falls through to the existing `NotFound` route, the same honest "not built yet" pattern BL-002 used for its own four destinations.
- **Route-level admin guard.** The legacy app's only admin gate is the Main Menu button's enabled/disabled state (per the backlog's own acceptance basis: "reachability itself is gated by DR-003 (BL-003), not by a rule of this screen's own"). That was sufficient in WinForms, where the only way to open `frmAdmin` is through that button. In a web app, `/admin` is directly URL-navigable regardless of the button's disabled state, so a client-side-only gate on the button would let any authenticated non-admin user reach the Admin Panel by typing the URL. This proposal extends DR-003's gate to the route itself: `/admin` re-runs the same `GET /api/auth/me` check BL-003 already built (no new backend work) and redirects a non-admin to `/` if the check fails or is still pending, mirroring how `RequireAuth` already gates `/` on being signed in at all. This is the same rule (DR-003), applied at the one additional entry point the web platform introduces — not a new business rule.

## Scope

### In Scope
- ADMİN button's `onClick` navigating to `/admin`
- Admin Panel screen matching SCR-007's layout (2 large buttons + 3 smaller buttons)
- Five navigation buttons wired to real client-side routes (falling through to `NotFound` until each destination is built)
- A route guard on `/admin` re-checking admin status (via the existing `GET /api/auth/me`) and redirecting non-admins to `/`

### Out of Scope
- The five destination screens themselves (Stock Add/Update — BL-009/BL-010; Room Add/Update/Delete — BL-005/BL-006/BL-007)
- Any backend authorization on those destination endpoints — each is scoped by its own backlog item
- Any change to DR-003's own check (`GET /api/auth/me`) — reused as-is from BL-003

## Impact

- **Files affected:** ~3 (estimated) — `MainMenu.tsx` (add onClick), a new `AdminPanel.tsx` + its CSS, `App.tsx` (register the `/admin` route with its guard)
- **Complexity:** small
- **Risk:** low — pure client-side routing, reuses BL-003's existing session check, no backend changes

## Open Questions

- **UI grounding missing:** SQ-013 (FAITHFUL) is decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent — per project decision, screenshots will be captured at the end of the whole backlog; this item's layout is built from `ui-inventory.md`'s SCR-007 description, consistent with how BL-001 through BL-003 already shipped.
- **Verification:** per the backlog's own note, no golden-master scenario covers this pure-routing screen (NO BASELINE DATA) — a human must confirm the five routing edges and the DR-003 reachability precondition by direct manual comparison against the running legacy app.

---

**To proceed:** Review this proposal and approve to begin planning.
