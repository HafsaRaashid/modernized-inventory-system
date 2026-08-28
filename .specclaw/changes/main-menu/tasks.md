# Tasks: BL-002 — Main Menu Navigation Hub

**Change:** main-menu
**Created:** 2026-08-28
**Total Tasks:** 5

## Summary

3 waves. Wave 1 builds AppShell's wrapper refactor and the new MainMenu screen independently. Wave 2 wires them together in App.tsx. Wave 3 is tests.

## Tasks

### Wave 1 — Independent pieces

- [ ] `T1` — Refactor AppShell into a generic wrapper
  - Files: web/src/routes/AppShell.tsx
  - Estimate: small
  - Kind: impl
  - Notes: Accept `{ children }: { children: React.ReactNode }`. Keep the header's title, "Signed in as {username}", and Sign Out button exactly as they behave today (AC-1, AC-5) — move nothing about their logic, only add the children prop and render `{children}` in `.app-shell__content` instead of the old static "Signed in as/Sign Out" markup (which moves to the header).

- [ ] `T2` — Add MainMenu screen (SCR-002 layout)
  - Files: web/src/routes/MainMenu.tsx, web/src/routes/MainMenu.css
  - Estimate: medium
  - Kind: impl
  - Notes: 2x2 grid of 4 buttons (ARAMALAR, ODA DEMİRBAŞ İŞLEMLERİ, ODA TANIMLAMA, ADMİN) + one full-width button below (Rapor Çıktısı Al), per ui-inventory.md SCR-002. First three grid buttons + the full-width button call `useNavigate()` to `/search`, `/asset-assignment`, `/room-assignment`, `/reports` respectively (FR-3). ADMİN button renders `disabled` (FR-4), no onClick needed since it can't be activated. No project-specific token group applies (SCR-002 has none per design-tokens.json).

### Wave 2 — Integration

- [ ] `T3` — Wire MainMenu into AppShell at "/"
  - Files: web/src/App.tsx
  - Estimate: small
  - Kind: impl
  - Depends: T1, T2
  - Notes: The `/` route's authenticated branch (`RequireAuth`, unchanged from BL-001) now renders `<AppShell><MainMenu /></AppShell>` instead of bare `<AppShell />`. Import both from their existing files.

### Wave 3 — Tests

- [ ] `T4` — MainMenu component tests
  - Files: web/tests/MainMenu.test.tsx
  - Estimate: medium
  - Kind: test
  - Depends: T2, T3
  - Notes: AC-2 (all 5 labels present, correct grid/full-width structure), AC-3 (clicking ARAMALAR/ODA DEMİRBAŞ İŞLEMLERİ/ODA TANIMLAMA/Rapor Çıktısı Al navigates to the right route — assert via MemoryRouter + a NotFound sentinel, or by spying on navigate), AC-4 (ADMİN is disabled, clicking it does nothing).

- [ ] `T5` — AppShell wrapper tests
  - Files: web/tests/AppShell.test.tsx
  - Estimate: small
  - Kind: test
  - Depends: T1, T3
  - Notes: AC-1 (renders header chrome plus arbitrary children passed to it), AC-5 (Sign Out still calls logout() and navigates to /login — regression check against BL-001's behavior). Also run the FULL test suite (`npx vitest run` from web/) to confirm the existing App.test.tsx's authenticated-render assertions ("Inventory Tracking System", "Signed in as testuser") still pass unmodified — they should, since AppShell's header keeps both, but confirm rather than assume.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
