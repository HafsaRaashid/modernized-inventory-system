# Design: BL-004 — Admin Panel Sub-Navigation

**Change:** admin-panel
**Created:** 2026-08-28

## Technical Approach

Three pieces:

1. **`MainMenu.tsx`:** add the missing `onClick={() => navigate("/admin")}` to the ADMİN button (the other four buttons already have theirs).
2. **`AdminPanel.tsx`:** a new pure-router screen, structurally identical in spirit to `MainMenu.tsx` — a `useNavigate()`-driven button grid, no state, no API calls of its own.
3. **`App.tsx`:** a new `RequireAdmin` wrapper component (sibling to the existing `RequireAuth`) that gates `/admin`: redirect to `/login` if unauthenticated (reusing `useAuth().token`, same check `RequireAuth` already does), otherwise call `getSession()` on mount and track a three-state result (`"loading" | "admin" | "not-admin"`) — render nothing while loading, `<Navigate to="/" />` if not admin, `<AppShell><AdminPanel /></AppShell>` if admin.

## Architecture

```
MainMenu (ADMİN button, enabled) --click--> /admin
                                                │
                                                ▼
                                          RequireAdmin
                                    ┌───────────┴───────────┐
                              no token                  has token
                                    │                         │
                              Navigate to /login        GET /api/auth/me (reused from BL-003)
                                                          ┌────┴────┐
                                                    isAdmin:false  isAdmin:true
                                                          │             │
                                                   Navigate to /   <AppShell><AdminPanel /></AppShell>
```

`AdminPanel`'s five buttons behave exactly like `MainMenu`'s four built-but-unimplemented destinations: `useNavigate()` to a route with no matching `<Route>`, falling through to the existing `*` → `NotFound` catch-all.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `web/src/routes/MainMenu.tsx` | Modify | Add `onClick={() => navigate("/admin")}` to the ADMİN button |
| `web/src/routes/AdminPanel.tsx` | Create | SCR-007 layout: 2 large buttons + 3 smaller buttons, each navigating to its own route |
| `web/src/routes/AdminPanel.css` | Create | Layout styling for the two-row button grid |
| `web/src/App.tsx` | Modify | Add `RequireAdmin` wrapper component and register the `/admin` route through it |
| `web/tests/MainMenu.test.tsx` | Modify | Add a test for the ADMİN button's new `onClick` (only exercisable once enabled — reuse the existing `isAdmin: true` mock setup) |
| `web/tests/AdminPanel.test.tsx` | Create | Button labels/layout + all five navigation clicks |
| `web/tests/App.test.tsx` | Modify | Add `RequireAdmin` gating tests: no token → `/login`; token + non-admin → `/`; token + admin → renders Admin Panel; pending check → no premature redirect |

## Data Model Changes

None.

## API Changes

None — reuses `GET /api/auth/me` from BL-003 exactly as-is.

## Key Decisions

- **Route-level re-check, not just a disabled button.** The legacy app's only gate is the Main Menu button's enabled state, sufficient because WinForms offers no other way to open `frmAdmin`. The web platform's URL bar is a second entry point the legacy app never had, so this change extends DR-003's existing gate to `/admin` itself rather than trusting the button alone — same rule, applied at the one additional entry point the platform introduces.
- **Three-state loading, not a boolean default.** `MainMenu`'s admin check defaults `isAdmin` to `false` because "disabled until proven otherwise" is the correct default for a button. A route guard defaulting to "not admin" would incorrectly flash a redirect to `/` for a genuine admin before their check resolves. `RequireAdmin` therefore tracks `"loading" | "admin" | "not-admin"` explicitly and renders nothing during `"loading"`.
- **No new backend endpoint.** `GET /api/auth/me` already returns exactly what the route guard needs; adding a second endpoint for the same fact would duplicate BL-003's work for no reason.

## Risks & Mitigations

- **Risk:** `RequireAdmin`'s `getSession()` call could race with `MainMenu`'s own call to the same endpoint if a user opens `/admin` in a way that also renders `MainMenu` — in practice this can't happen, since `/admin` and `/` are mutually exclusive routes rendering different trees, so at most one of the two mount-time calls is ever active. No mitigation needed beyond the routing structure itself.
- **Risk:** A stale test asserting the ADMİN button has no `onClick` (none exists — BL-002/BL-003's tests only asserted `disabled`/`enabled` state, never absence of a handler) — no regression risk here.
