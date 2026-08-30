# Proposal: BL-011 — Asset Assignment and Stock Decrement (Composite Flow)

**Created:** 2026-08-30
**Status:** 🟢 Approved (approved via /specclaw:auto invocation, 2026-08-30)

## Problem

Main Menu's "ODA DEMİRBAŞ İŞLEMLERİ" button already navigates to `/asset-assignment` (built in BL-002) but currently 404s — there is no way to issue a fixed asset to a room and decrement its stock. This is the last piece of MOD-003's core workflow: BL-009/BL-010 create/edit `FixedAsset` rows, BL-008 pairs a room with a responsible person, but nothing yet connects them.

## Proposed Solution

A `/asset-assignment` screen (SCR-004), authenticated (not admin-gated — reached from Main Menu, same as BL-008's Room Assignment), reproducing the legacy composite flow as **one atomic backend operation**:

- **Selectors:** a room `<select>` (from `GET /api/rooms`) and an asset `<select>` (from `GET /api/fixed-assets`), each with a disabled echo field beneath (mirrors BL-008's `RoomAssignment.tsx` echo-name convention — the legacy screen used DataGridViews rather than named echo fields, so no literal control names are evidenced to reuse).
- **Quantity field:** digit/comma-only keypress filter (DR-005, same filter already used by Stock Add/Update — `frmDemirbasIslem.cs`'s `txtDIAdet_KeyPress` is cited by the same DR-005 rule).
- **Client-side DR-001 pre-check:** quantity may not exceed the selected asset's currently-known stock (from the already-fetched asset list) — this is a fast-feedback UX check only; the authoritative check happens server-side.
- **A single `POST /api/asset-assignments`** performs, inside one database transaction: (1) re-validates the stock-adequacy guard (DR-001) against the asset's current row — this is also where CQ-026's decided fix lives: since the rebuild has no separate "decrement-only" entry point (unlike legacy's `GuncelleAdet()`, which could be reached independently of the guard), the guard and the decrement are structurally inseparable here; (2) inserts a new `RoomAssetAssignment` row (`RoomId`, `AssetId`, `Quantity`, and `PersonnelId` inherited from the room's existing room-responsibility row — the same dual-write table BL-008 already writes to, per CQ-007's decided no-forced-single-owner); (3) decrements `FixedAsset.Quantity` by the issued amount (DR-002); (4) commits both writes together, addressing CQ-028's still-open transaction-boundary question with its own proposed default (single atomic transaction, not two separate client-triggered calls).
- **A second read-only panel** listing everything currently assigned to the selected room (`GET /api/asset-assignments?roomId=`), matching SCR-004's documented second grid.

## Scope

### In Scope
- `POST /api/asset-assignments` (the composite issue-and-decrement operation) and `GET /api/asset-assignments?roomId=` (per-room assignment listing) on a new `AssetAssignmentsController`.
- `/asset-assignment` React screen + route (already navigated to by `MainMenu.tsx`, currently 404ing).
- DR-001 stock-adequacy guard (both client pre-check and authoritative server re-check inside the transaction), DR-002 stock decrement, DR-004 non-empty quantity check.
- Looking up the room's existing responsible-personnel pairing (from BL-008's rows) to populate the new assignment row's `PersonnelId`.
- Backend + frontend tests, including a same-request race scenario proving the guard holds under a already-decremented row.

### Out of Scope
- Room-to-Personnel Assignment itself (BL-008, already built) — this item only reads its existing rows, never writes room-responsibility rows.
- Asset Search / Personnel Search (BL-012/BL-013) and Reporting (BL-014/BL-015) — separate items.
- Resolving PQ-003/PQ-007 (RoomAssetAssignment's row-shape and module-ownership questions) — both remain provisional with their proposed defaults (one nullable mixed-purpose row shape; MOD-003 ownership), same posture used throughout this backlog so far.
- Resolving CQ-028 formally — this item adopts its stated proposed default (single atomic transaction) as the implementation, consistent with how CQ-027 has been handled on every prior item (proceed with the sensible default; the question stays open in the record).

## Impact

- **Files affected:** ~10 (1 new controller, 1 new API client module, 1 frontend screen + CSS, `App.tsx` unaffected since the route already exists from BL-002 — no, wait: `/asset-assignment` route needs to actually render a component, so `App.tsx` IS touched to wrap it — 2 test files).
- **Complexity:** medium-high — the only item so far with a genuine multi-write, transaction-guarded backend operation and a cross-module data lookup (reading BL-008's rows).
- **Risk:** medium — the race-condition guard (CQ-026) and the "no responsible personnel assigned to this room yet" edge case (an assumption this proposal makes explicit, since no legacy SQL evidence pins the exact tie-break when a room might have more than one responsibility row — BL-008's AC-13 deliberately allows duplicate room+personnel pairs with no dedup) are both new engineering surface, not just reproduction of an already-fully-specified legacy behavior.

## Open Questions

- **Assumption (not a formal CQ, since no legacy SQL evidence pins this exact case):** when a room has more than one room-responsibility row (possible per BL-008's AC-13, which deliberately allows duplicate room+personnel pairs), this item uses the MOST RECENTLY CREATED responsibility row (highest `Id`) as the personnel to carry into the new asset-issue row. If a room has NO responsibility row at all, the POST is rejected with a clear error — asset issuance requires an existing room-personnel pairing, matching the legacy app's implicit assumption that `frmOdaTanimlama.cs` runs before `frmDemirbasIslem.cs` for a given room.
- CQ-026 (decided): implemented as described above — the guard is structurally inseparable from the decrement in this rebuild, which is a stronger fix than "a non-negative guard at the point of decrement" alone, since there is no other code path that can decrement `FixedAsset.Quantity` at all except through this one guarded operation. (Stock Update's direct quantity edit, BL-010, is a distinct, already-accepted legacy-parity behavior — an admin setting a value directly, not a "decrement" operation — and is unaffected by this item.)
- CQ-027 (unanswered, non-blocking, same posture as every prior item) — single real validation path implemented, not two decoupled ones.
- CQ-028 (unanswered, non-blocking) — proposed default (single atomic transaction) adopted as this item's implementation; the underlying question remains open in decisions.md/pending-questions for a human to resolve on its own timeline.
- UI fidelity artifacts remain absent project-wide (deferred to end of backlog).

---

**To proceed:** Review this proposal and approve to begin planning.
