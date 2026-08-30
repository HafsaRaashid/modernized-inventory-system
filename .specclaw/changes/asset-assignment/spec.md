# Spec: BL-011 — Asset Assignment and Stock Decrement (Composite Flow)

**Change:** asset-assignment
**Created:** 2026-08-30
**Status:** 🟡 Draft

## Overview

Implements the last connector in MOD-003's core workflow: issuing a `FixedAsset` (BL-009/BL-010) to a `Room` that already has a responsible person (BL-008), decrementing the asset's on-hand quantity by the issued amount. The legacy app performs this as two sequential, independently-connected SQL writes after an in-memory guard (DR-001); the rebuild performs the equivalent as one atomic backend write (a single `SaveChangesAsync()` call tracking both the new assignment row and the asset's decremented quantity), closing CQ-026's guard-bypass gap structurally rather than by adding a second check.

## Requirements

### Functional Requirements

- **FR-1:** An authenticated user (not admin-gated — reached from Main Menu, matches BL-008's Room Assignment gating) can select a room and a fixed asset, enter a quantity, and issue that quantity of the asset to the room.
- **FR-2:** The room selector is populated from `GET /api/rooms`; the asset selector from `GET /api/fixed-assets`.
- **FR-3:** Selecting a room or asset echoes its name in a disabled field beneath the corresponding selector (mirrors BL-008's `RoomAssignment.tsx` echo convention).
- **FR-4:** Selecting a room populates a read-only panel listing everything currently assigned to that room, via `GET /api/asset-assignments?roomId=`.
- **FR-5:** The quantity field is non-empty before submission is allowed (DR-004) and restricted to digits, backspace, and comma while typing (DR-005, same filter as Stock Add/Update).
- **FR-6:** Client-side, the quantity may not exceed the selected asset's currently-known stock (DR-001 pre-check, fast feedback only).
- **FR-7:** Submitting calls `POST /api/asset-assignments` with `{roomId, assetId, quantity}`. The backend re-validates DR-001 authoritatively, inserts a new `RoomAssetAssignment` row (`RoomId`, `AssetId`, `Quantity`, `PersonnelId` inherited from the room's existing responsibility row), and decrements the asset's `Quantity` by the issued amount (DR-002) — both writes committed together via one `SaveChangesAsync()` call, so neither can land without the other.
- **FR-8:** On success: the quantity field clears (room/asset selections are retained, matching the legacy "both grids refresh" behavior rather than resetting the whole form), the fixed-asset list and the room's current-assignments panel both refresh, and a success message is shown — "Odaya Demirbaş Atandı" (the legacy workflow's own literal success text).
- **FR-9:** A missing room or asset selection returns 400; a missing/non-positive quantity returns 400; an unknown room or asset id returns 400; a quantity exceeding the asset's current stock returns 400 with the legacy DR-001 warning text; a room with no existing responsibility row returns 400 (this item's own documented assumption — see Notes).
- **FR-10:** `GET /api/asset-assignments?roomId=` returns only asset-issue rows for that room (`AssetId` not null) — the room-responsibility row(s) BL-008 wrote to the same table (`AssetId` null) are excluded from this listing.
- **FR-11:** The `/asset-assignment` route (already navigated to by `MainMenu.tsx`, currently falling through to `NotFound`) is reachable only by an authenticated user — unauthenticated redirects to `/login`; any authenticated user (admin or not) sees the screen.

### Non-Functional Requirements

- **NFR-1:** The insert-then-decrement sequence is atomic: implemented as tracked changes on a single `AppDbContext` instance flushed by one `SaveChangesAsync()` call — not two separate database round-trips, and not an explicit `Database.BeginTransactionAsync()` (the EF Core InMemory provider used by this project's tests does not support explicit transactions; a single `SaveChangesAsync()` call is already atomic on the real SQL Server provider and works identically against InMemory).
- **NFR-2:** CQ-026's guard-bypass gap is closed structurally: `FixedAsset.Quantity` can only be decremented through this one endpoint, and the guard check happens immediately before the same `SaveChangesAsync()` call that performs the decrement — there is no other code path (unlike legacy's independently-callable `GuncelleAdet()`) that could decrement stock without re-checking adequacy first.
- **NFR-3:** True concurrent-request race protection (two simultaneous requests against the same asset) is not added beyond re-checking the guard within the same request/method call — this matches the legacy app's own complete absence of any concurrency control, and is not part of CQ-026's decided scope (which addresses "no other path bypasses the guard," not general optimistic-concurrency).

## Acceptance Criteria

- **AC-1:** Visiting `/asset-assignment` while authenticated renders a room selector, an asset selector, two disabled echo fields (room name, asset name), a quantity input, a save button, and a read-only panel for the selected room's current assignments (SCR-004 layout per rebuild-backlog.md's written description).
- **AC-2:** Selecting a room updates the room-name echo field; selecting an asset updates the asset-name echo field.
- **AC-3:** Selecting a room populates the current-assignments panel via `GET /api/asset-assignments?roomId=`.
- **AC-4:** The save button is disabled (no API call made) unless a room, an asset, and a non-empty quantity are all present.
- **AC-5:** Entering a quantity greater than the selected asset's currently-known stock keeps the save button disabled (or otherwise prevents submission) client-side — no API call is made.
- **AC-6:** Submitting a valid assignment calls `POST /api/asset-assignments` with `{roomId, assetId, quantity}`; on success, the quantity field clears (selections retained), the success message "Odaya Demirbaş Atandı" is shown, and both the fixed-asset list and the current-assignments panel are re-fetched.
- **AC-7:** `POST /api/asset-assignments` with a missing `roomId` or `assetId` returns 400 `SELECTION_REQUIRED`, no row created.
- **AC-8:** `POST /api/asset-assignments` with a missing or non-positive `quantity` returns 400 `QUANTITY_REQUIRED`, no row created.
- **AC-9:** `POST /api/asset-assignments` with a `roomId` matching no `Room` returns 400 `INVALID_ROOM`.
- **AC-10:** `POST /api/asset-assignments` with an `assetId` matching no `FixedAsset` returns 400 `INVALID_ASSET`.
- **AC-11:** `POST /api/asset-assignments` with a `quantity` exceeding the asset's current `Quantity` returns 400 `INSUFFICIENT_STOCK`, message "Girilen değer stok miktarından fazla.Daha az bir değer giriniz...", and neither writes a `RoomAssetAssignment` row nor changes `FixedAsset.Quantity`.
- **AC-12:** `POST /api/asset-assignments` for a room with no existing room-responsibility row (no `RoomAssetAssignment` row with that `RoomId`, a non-null `PersonnelId`, and a null `AssetId`) returns 400 `NO_RESPONSIBLE_PERSONNEL`, no write.
- **AC-13:** A successful `POST /api/asset-assignments` both inserts a new `RoomAssetAssignment` row (`RoomId`, `AssetId`, `Quantity` = the issued amount, `PersonnelId` = the room's responsibility row's `PersonnelId`) AND decrements the referenced `FixedAsset.Quantity` by exactly that amount, and a query immediately after confirms both changes are present (proving the single-`SaveChangesAsync()` atomicity, not a two-step process that could partially fail).
- **AC-14:** When a room has more than one responsibility row (possible per BL-008's own no-dedup behavior), the MOST RECENTLY CREATED one (highest `Id`) is the one whose `PersonnelId` is used.
- **AC-15:** `GET /api/asset-assignments?roomId=` returns only rows with a non-null `AssetId` for that room (each with `assetId`, `assetName`, `quantity`) — a room-responsibility row for the same room (null `AssetId`) does not appear in the results.
- **AC-16:** Unauthenticated visit to `/asset-assignment` redirects to `/login`.
- **AC-17:** Authenticated visit (any `isAdmin` value) to `/asset-assignment` renders the screen — this route is not admin-gated.
- **AC-18:** `/room-assignment`, `/stock-add`, `/stock-update`, `/room-add`, `/room-update`, `/room-delete`, `/admin`, and `/` are all unaffected by this change.

## Edge Cases

- A quantity issued that exactly equals the asset's current stock succeeds and leaves `FixedAsset.Quantity` at exactly 0 (DR-001 only rejects when requested STRICTLY exceeds available stock — matches the legacy guard's `>` comparison, not `>=`).
- Two different assets issued to the same room create two separate `RoomAssetAssignment` rows (no upsert) — each asset-issue is its own row, matching the legacy insert-only pattern.
- Re-selecting the same room after a successful issue shows the newly-issued row in the current-assignments panel (via the AC-6 re-fetch).
- A room's responsibility row and its asset-issue rows coexist in the same table (`RoomAssetAssignment`) with different populated columns — this item never modifies or deletes the responsibility row, only reads its `PersonnelId`.

## Dependencies

- BL-002 (Main Menu Navigation Hub) — already built (`main-menu`). The "ODA DEMİRBAŞ İŞLEMLERİ" button already navigates to `/asset-assignment`; this item gives that route a real destination.
- BL-005 (Room Add) — already built (`room-add`). Provides `GET /api/rooms`.
- BL-008 (Room to Personnel Assignment) — already built (`room-assignment`). Provides the room-responsibility rows this item reads `PersonnelId` from.
- BL-009 (Stock / Asset Add) — already built (`stock-add`). Provides `GET /api/fixed-assets` and the `FixedAsset`/`RoomAssetAssignment` entities.
- No same-module or cross-module bypass needed — all four dependencies carry declared `BUILT:` notes.

## Notes

- **Assumption, not a formally-decided CQ** (no legacy SQL evidence pins this exact tie-break case): AC-14's "most recently created responsibility row wins" rule is this item's own engineering choice for a scenario the legacy evidence doesn't directly address (a room having more than one responsibility row is only possible because of BL-008's own deliberate no-dedup decision, which postdates the legacy app's original single-row assumption).
- CQ-028 (transaction-boundary question) remains formally open in decisions.md; this item implements its own stated proposed default (one atomic backend operation) as described in NFR-1, consistent with how CQ-027 has been carried as a non-blocking open question through every prior item in this backlog.
- This item does not touch Asset Search (BL-012), Personnel Search (BL-013), or Reporting (BL-014/BL-015) — those remain separate items.
