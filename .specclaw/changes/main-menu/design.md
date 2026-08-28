# Design: BL-002 — Main Menu Navigation Hub

**Change:** main-menu
**Created:** 2026-08-28

## Technical Approach

Refactor `AppShell` from a self-contained screen into a generic wrapper (`{ children: ReactNode }`) that keeps the header chrome (title, signed-in state, Sign Out) persistent and renders whatever screen is passed to it. Add a new `MainMenu` component (SCR-002's layout) and render it as `AppShell`'s child at `/`, replacing the bare `<AppShell />` App.tsx currently renders.

## Architecture

```
App.tsx "/" route (still gated by RequireAuth, unchanged from BL-001)
  -> <AppShell><MainMenu /></AppShell>
       AppShell: header (title, "Signed in as {username}", Sign Out) — unchanged behavior, new children prop
       MainMenu: 2x2 button grid + 1 full-width button
         ARAMALAR                -> navigate("/search")           -> NotFound (no screen yet)
         ODA DEMİRBAŞ İŞLEMLERİ  -> navigate("/asset-assignment")  -> NotFound (no screen yet)
         ODA TANIMLAMA           -> navigate("/room-assignment")   -> NotFound (no screen yet)
         ADMİN                   -> disabled, no navigation
         Rapor Çıktısı Al        -> navigate("/reports")           -> NotFound (no screen yet)
```

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `web/src/routes/AppShell.tsx` | Modify | Accept `{ children }: { children: ReactNode }`; move "Signed in as/Sign Out" into the header; render `{children}` in `.app-shell__content` instead of the old static text. |
| `web/src/routes/MainMenu.tsx` | Create | SCR-002 layout: 2×2 grid of 4 buttons + 1 full-width button. Each wires `useNavigate()` to its target route; ADMİN renders `disabled`. |
| `web/src/routes/MainMenu.css` | Create | Grid layout for the 2×2 button arrangement + full-width button below. |
| `web/src/App.tsx` | Modify | `/` route's authenticated branch renders `<AppShell><MainMenu /></AppShell>` instead of bare `<AppShell />`. |
| `web/tests/MainMenu.test.tsx` | Create | AC-2, AC-3, AC-4: labels present, four buttons navigate to their routes, ADMİN is disabled and non-navigating. |
| `web/tests/AppShell.test.tsx` | Create | AC-1, AC-5: renders header chrome + arbitrary children; Sign Out still works. |

## Data Model Changes

None — pure frontend routing/UI.

## API Changes

None.

## Key Decisions

- **`AppShell` becomes a wrapper, not a screen.** Its own doc comment already said "the root layout every screen-bearing backlog item will eventually render inside" — this item is the first to actually use it that way. Keeping "Signed in as/Sign Out" in the header (not per-screen content) makes it genuinely persistent chrome across every future screen, not something each new screen has to re-implement.
- **No stub screens for the five destinations.** Falling through to the existing `NotFound` route is the accurate current state of the app, not a capability this item is faking — nothing pretends `/search` etc. have real content. Building placeholder "coming soon" screens would be inventing UI nobody asked for (YAGNI) and would misrepresent what's actually built.
- **ADMİN button ships disabled, no gate logic.** Matches the legacy default (`Enabled=false` until the Main Menu's `Load` event re-evaluates it) exactly, and keeps DR-003's real gating logic entirely in BL-003 where it belongs — this item does not reach into BL-003's scope to make the button "look more finished."
- **Exit-on-close: not built.** No web equivalent decided; inventing one (e.g. a fake "Exit" button that does something arbitrary) would be scope nobody asked for. Flagged in spec.md Notes instead.

## Risks & Mitigations

- **Risk:** refactoring `AppShell` to accept `children` could silently break the Sign Out flow BL-001 already verified. **Mitigation:** AC-5 is an explicit regression criterion, and `AppShell.test.tsx` covers it directly rather than relying on `MainMenu.test.tsx` to catch it incidentally.
- **Risk:** none of the five navigation targets have real screens, so "clicking navigates correctly" can only be tested against `NotFound`, not against the eventual real destination. **Mitigation:** explicitly scoped as testing the *route*, not the destination's future content — re-verified naturally once each destination's own backlog item builds a real screen there.
