# Proposal: BL-002 — Main Menu Navigation Hub

**Created:** 2026-08-28
**Status:** 🟡 Draft

## Problem

Once a user signs in (BL-001), there's nowhere to go — `AppShell` is still the bootstrap placeholder ("Signed in as `{username}`" and nothing else). The legacy app's `frmAnaMenu` is the central navigation hub every other feature area is reached through: Search, Asset Assignment (Oda Demirbaş İşlemleri), Room Assignment (Oda Tanımlama), the Admin Panel, and Reporting. Nothing downstream (BL-003 Admin gate, BL-004 Admin routing, and everything reached from Main Menu) has anywhere to attach until this hub exists.

## Proposed Solution

Replace `AppShell`'s placeholder content with the real Main Menu screen, matching **SCR-002**'s layout (a 2×2 grid of large navigation buttons plus a full-width reporting button below), and wire its five buttons to client-side routes:

- ARAMALAR (Search) → `/search`
- ODA DEMİRBAŞ İŞLEMLERİ (Asset Assignment) → `/asset-assignment`
- ODA TANIMLAMA (Room Assignment) → `/room-assignment`
- ADMİN (Admin Panel) → `/admin`
- Rapor Çıktısı Al (Reporting) → `/reports`

None of these five destination screens exist in the rebuild yet — they're each their own later backlog item (BL-004, BL-008, BL-009/BL-011, BL-012/BL-013, BL-014/BL-015). Navigating to any of them today falls through to the app's existing `NotFound` catch-all route. That's not a stub or fabricated capability — it's the honest, structurally-true state of an app whose navigation hub is built before its destinations are. Nothing pretends those screens exist; the app's own real "page not found" behavior is what shows.

The Admin button renders in its legacy **default disabled state** (`btnAdmin.Enabled=false` is the pre-gate-check default per `frmAnaMenu.cs:50-53`) — the actual gate logic that conditionally enables it (DR-003, based on `User.YetkiID`) is BL-003's scope, which depends on this item and layers the real check on top.

**Open, not built:** the legacy `frmAnaMenu_FormClosing` behavior ("closing the Main Menu exits the entire application," `Application.Exit()`) has no meaningful web-platform equivalent — there is no SPA-controllable "window close" gesture analogous to closing a WinForms window, and the browser tab lifecycle is outside the application's control. No CQ/SQ decision addresses this specific shape change. This proposal does not invent a substitute "Exit" action; it's flagged as an open question rather than guessed at.

## Scope

### In Scope
- Main Menu screen matching SCR-002's layout (2×2 button grid + full-width reporting button)
- Five navigation buttons wired to real client-side routes (falling through to the existing `NotFound` route until each destination is built)
- Admin button rendered in its default-disabled state (no gating logic — that's BL-003)
- Replacing `AppShell`'s placeholder content with this screen, reachable at `/` post-login (the same route BL-001 already gates behind auth)

### Out of Scope
- Admin gate logic (DR-003, `YetkiID` check) — BL-003
- Admin Panel sub-routing — BL-004
- The five destination screens themselves — later backlog items
- Any substitute for "closing the window exits the app" — open question, no web equivalent decided

## Impact

- **Files affected:** ~3-4 (estimated) — `AppShell.tsx` becomes the Main Menu screen (or a new `MainMenu.tsx` route replaces its content), a small CSS file for the SCR-002 grid layout, possibly placeholder route stubs for the five destinations (or relying on the existing catch-all)
- **Complexity:** small
- **Risk:** low — pure client-side navigation, no backend changes, no numbered business rule (per the backlog's own acceptance basis: "No numbered DR-NNN rule governs plain navigation routing")

## Open Questions

- **Exit-on-window-close:** no web equivalent decided for `Application.Exit()` on Main Menu close — not built, flagged for a future decision if one is ever needed.
- **UI grounding missing:** SQ-013 (FAITHFUL) is decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent — the layout is built from `ui-inventory.md`'s recorded structure, pending a human screenshot sign-off.
- **Verification:** per the backlog's own note, no golden-master scenario covers pure navigation routing (NO BASELINE DATA) — a human must confirm the five navigation edges by direct comparison against the running legacy app; no fixture-based acceptance exists for this item.

---

**To proceed:** Review this proposal and approve to begin planning.
