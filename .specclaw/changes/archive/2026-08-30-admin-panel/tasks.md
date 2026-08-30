# Tasks: BL-004 — Admin Panel Sub-Navigation

**Change:** admin-panel
**Created:** 2026-08-28
**Total Tasks:** 5

## Summary

3 waves. Wave 1 builds the ADMİN button's onClick and the new AdminPanel screen independently. Wave 2 wires the `/admin` route with its RequireAdmin guard (depends on both wave-1 pieces existing). Wave 3 is tests.

## Tasks

### Wave 1 — Independent pieces

- [x] `T1` — Wire the ADMİN button's onClick
  - Files: web/src/routes/MainMenu.tsx
  - Estimate: small
  - Kind: impl
  - Notes: Add `onClick={() => navigate("/admin")}` to the ADMİN button (FR-1). No other change to this file — its `disabled={!isAdmin}` from BL-003 stays as-is.

- [x] `T2` — Add AdminPanel screen (SCR-007 layout)
  - Files: web/src/routes/AdminPanel.tsx, web/src/routes/AdminPanel.css
  - Estimate: medium
  - Kind: impl
  - Notes: Two large buttons side-by-side (Stok Ekle / Stock Add, Stok Güncelle / Stock Update) in the upper portion, three smaller buttons in a row beneath (Oda Sil / Room Delete, Oda Ekle / Room Add, Oda Güncelle / Room Update), per ui-inventory.md SCR-007. Each button calls `useNavigate()` to `/stock-add`, `/stock-update`, `/room-delete`, `/room-add`, `/room-update` respectively (FR-3). Structurally mirror MainMenu.tsx's button-grid pattern (no state, no API calls in this component).

### Wave 2 — Integration

- [x] `T3` — Add RequireAdmin guard and register the /admin route
  - Files: web/src/App.tsx
  - Estimate: medium
  - Kind: impl
  - Depends: T2
  - Notes: Add a `RequireAdmin` component (sibling to the existing `RequireAuth`): if `useAuth().token` is falsy, `<Navigate to="/login" replace />` (same check RequireAuth already does — FR-4). Otherwise, track a three-state result via `useState<"loading" | "admin" | "not-admin">("loading")`, call `getSession()` (from `../api/auth`) in a `useEffect` on mount, set `"admin"` on `isAdmin: true`, `"not-admin"` on `isAdmin: false` or on rejection. While `"loading"`, render `null` (FR-5 — no premature redirect, no content flash). On `"not-admin"`, `<Navigate to="/" replace />`. On `"admin"`, render `<AppShell><AdminPanel /></AppShell>`. Register `<Route path="/admin" element={<RequireAdmin />} />` in the `<Routes>` block, importing `AdminPanel` from `./routes/AdminPanel`.

### Wave 3 — Tests

- [x] `T4` — AdminPanel component tests + MainMenu onClick test
  - Files: web/tests/AdminPanel.test.tsx, web/tests/MainMenu.test.tsx
  - Estimate: medium
  - Kind: test
  - Depends: T1, T2
  - Notes: New `AdminPanel.test.tsx` (AC-2, AC-3): renders all five button labels; clicking each navigates to its own route (mock `useNavigate` the same way `MainMenu.test.tsx` already does). In `MainMenu.test.tsx`, add one test for AC-1 (clicking the ADMİN button navigates to `/admin`) — mock `getSession` to resolve `isAdmin: true` first so the button is enabled and clickable, matching the pattern the existing "AC-1: the ADMİN button becomes enabled..." test already uses.

- [x] `T5` — RequireAdmin route-guard tests
  - Files: web/tests/App.test.tsx
  - Estimate: medium
  - Kind: test
  - Depends: T3
  - Notes: Mock `getSession` (from `../src/api/auth`) with `vi.mock`, same pattern as `MainMenu.test.tsx`. Before each `/admin` test, `window.history.pushState({}, "", "/admin")` (same technique the file already uses for `/`). Cover: AC-4 (no token → renders Login, i.e. `/login`'s content — reuse the existing unauthenticated assertion pattern); AC-5 (token present, `getSession()` resolves `isAdmin: false` → ends up back at `/` — the Main Menu shows, e.g. assert "Inventory Tracking System" plus absence of Admin Panel content); AC-6 (token present, `getSession()` resolves `isAdmin: true` → Admin Panel content renders); FR-5 (a still-pending `getSession()` promise renders neither Login, Main Menu, nor Admin Panel content — nothing has redirected or rendered yet). Also re-run the full `npx vitest run` suite from `web/` to confirm AC-7 (nothing else regressed).

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
