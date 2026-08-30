# Spec: BL-002 — Main Menu Navigation Hub

**Change:** main-menu
**Created:** 2026-08-28
**Status:** 🟡 Draft

## Overview

Give the authenticated app somewhere to go. `AppShell` currently renders only a static "Signed in as `{username}`" placeholder; this change turns it into a generic wrapper (persistent header chrome) and adds the real Main Menu screen — SCR-002's 2×2 grid of navigation buttons plus a full-width reporting button — as its content at `/`.

## Requirements

### Functional Requirements

- **FR-1:** `AppShell` becomes a generic layout wrapper accepting `children`: the header (title, "Signed in as `{username}`", Sign Out) stays persistent chrome; `children` render in the content region.
- **FR-2:** A new Main Menu screen renders at `/`, matching SCR-002: a 2×2 grid of four large buttons (ARAMALAR, ODA DEMİRBAŞ İŞLEMLERİ, ODA TANIMLAMA, ADMİN) plus one full-width button below (Rapor Çıktısı Al).
- **FR-3:** Each of the five buttons navigates to its own client-side route on click: `/search`, `/asset-assignment`, `/room-assignment`, `/admin`, `/reports` respectively. None of these five destination screens exist yet — each falls through to the app's existing `NotFound` catch-all route, which is the honest current state, not a fabricated capability.
- **FR-4:** The ADMİN button renders in its legacy default-disabled state (`disabled`), matching `frmAnaMenu.cs`'s `btnAdmin.Enabled=false` default. The gate logic that conditionally enables it (DR-003, `User.YetkiID`) is BL-003's scope, not this item's.
- **FR-5:** "Closing the Main Menu exits the entire application" (`Application.Exit()`) has no built equivalent — there is no SPA-controllable "window close" gesture analogous to a WinForms window close, and no CQ/SQ decision addresses this shape change. Not built.

### Non-Functional Requirements

- **NFR-1:** No numbered `DR-###` business rule governs this item (per the backlog's own acceptance basis — pure navigation routing and default-state rendering).

## Acceptance Criteria

- **AC-1:** `AppShell` renders its header chrome (title, signed-in username, Sign Out) regardless of what `children` it's given, and renders `children` in its content region.
- **AC-2:** At `/` (authenticated), all five Main Menu buttons render with their exact legacy labels (ARAMALAR, ODA DEMİRBAŞ İŞLEMLERİ, ODA TANIMLAMA, ADMİN, Rapor Çıktısı Al), laid out as a 2×2 grid plus one full-width button below.
- **AC-3:** Clicking ARAMALAR navigates to `/search`; ODA DEMİRBAŞ İŞLEMLERİ to `/asset-assignment`; ODA TANIMLAMA to `/room-assignment`; Rapor Çıktısı Al to `/reports`. All four currently render the app's `NotFound` page (no screen exists there yet).
- **AC-4:** The ADMİN button is rendered `disabled` and does not navigate when clicked.
- **AC-5:** Sign Out still works exactly as BL-001 built it (regression check — `AppShell`'s refactor must not break it).

## Edge Cases

- None of the five destination routes have a real screen yet — asserting they hit `NotFound` (not asserting what those future screens will contain) is the correct, current-truth test.
- The Admin button's disabled state has no interaction to test beyond "disabled and non-navigating" — its real enable/disable logic is BL-003's, not testable here.

## Dependencies

BL-001 (User Login) — **BUILT**, merged `master@3ddd3a9`. Main Menu is only reached after a successful login, and `/` is already auth-gated by BL-001's `RequireAuth`.

## Notes

- **Open (not built, per proposal):** no web equivalent exists for "closing the Main Menu exits the application" — flagged, not guessed at.
- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` are still absent (SQ-013 FAITHFUL) — layout built from `ui-inventory.md`'s SCR-002 description, pending human screenshot sign-off.
- **Verification:** per the backlog's own note, no golden-master scenario covers pure navigation — NO BASELINE DATA. This item's acceptance rests on the criteria above plus manual comparison against the legacy app, not fixture replay.
