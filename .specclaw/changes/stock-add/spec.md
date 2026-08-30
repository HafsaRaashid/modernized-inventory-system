# Spec: BL-009 — Stock / Asset Add

**Change:** stock-add
**Created:** 2026-08-30
**Status:** 🟡 Draft

## Overview

Adds the ability to create a `FixedAsset` (legacy `tblDemirbas`) record: an admin-only "Stok Ekle" screen at `/stock-add`, wired from the Admin Panel button that currently falls through to `NotFound`. Reproduces the legacy Stock Add form's fields and rules (name, price, purchase date, asset type, quantity), including its known defect (no letter-filter on the asset-name field — CQ-015, decided: reproduce as-is) and its decided data-modeling fixes (real `decimal(19,4)` price type — CQ-013; a real DB uniqueness constraint on the asset name — CQ-018, since none existed in the legacy schema despite the legacy app's duplicate-record error message implying one). `AssetType` (`tblDemirbasTurleri`) is a read-only lookup populated by a dev/test seed migration only — CQ-012 (decided): no CRUD screen for it, ever.

## Requirements

### Functional Requirements

- **FR-1:** An admin-only user can create a fixed asset by providing a name, price, purchase date, asset type, and quantity.
- **FR-2:** The asset-type field is populated from `GET /api/asset-types`, a read-only lookup of all `AssetType` rows.
- **FR-3:** Selecting an asset type echoes its numeric ID in a disabled field (mirrors Room Add's department-ID echo, FR-2 of BL-005).
- **FR-4:** The name, price, and quantity fields must be non-empty before submission is allowed (DR-004) — enforced client-side (submit disabled) and re-validated server-side (400 if any is blank).
- **FR-5:** A duplicate asset name (case-sensitive exact match, matching `Room.Name`'s existing uniqueness pattern) is rejected with a 409, surfaced to the user with the legacy-parity message "Kayıtlı Demirbaş..." (CQ-018, DR-009/Named Gap item 9).
- **FR-6:** An `AssetTypeId` that doesn't match any existing `AssetType` row is rejected with a 400.
- **FR-7:** On success, the form resets (name/price/date/type/quantity all cleared) and shows a success message.
- **FR-8:** The `/stock-add` route is reachable only by an authenticated admin — unauthenticated users are redirected to `/login`; authenticated non-admins are redirected to `/` (same `RequireAdmin` guard used by `/room-add`/`/room-update`/`/room-delete`).
- **FR-9:** A back control returns to `/admin`.

### Non-Functional Requirements

- **NFR-1:** `Fiyat`/price is stored as `decimal(19,4)` (CQ-013, decided from direct schema inspection of the legacy `money` column).
- **NFR-2:** The asset-name field applies NO letter-only keypress restriction — CQ-015 (decided DEFECT): the legacy form declared but never wired its `HarfGirisiKontrol` filter to this field, and the faithful-by-default policy (SQ-012) requires reproducing that gap, not silently fixing it.
- **NFR-3:** Price and quantity fields restrict keyboard entry to digits, backspace, and comma (DR-005/CQ-014, decided: adopt the comma-only filter as-is). No code path parses either field's text as a numeric value client-side beyond what's needed to submit it as the request's numeric fields — matching the legacy app's own "keypress-level restriction only" behavior described in DR-005.

## Acceptance Criteria

- **AC-1:** Visiting `/stock-add` while authenticated as an admin renders a form with: asset-name input (no letter filter), price input (digit/comma-only), purchase-date input, asset-type `<select>` populated from `GET /api/asset-types`, a disabled asset-type-ID echo field, quantity input (digit/comma-only), and a submit button (SCR-008 layout per rebuild-backlog.md's written description).
- **AC-2:** Selecting an asset type updates the disabled ID-echo field to that type's numeric ID.
- **AC-3:** Submitting with name, price, date, asset type, and quantity all filled calls `POST /api/fixed-assets` with `{name, price, purchaseDate, assetTypeId, quantity}`, and on success resets all fields and shows "Demirbaş başarıyla eklendi."
- **AC-4:** The submit button is disabled (and no API call is made) unless name, price, and quantity are all non-empty and an asset type is selected.
- **AC-5:** `POST /api/fixed-assets` with a blank/whitespace-only name (or missing price/quantity) returns 400 with `error: "ASSET_NAME_REQUIRED"` (or the corresponding field-required code) and does not create a row.
- **AC-6:** `POST /api/fixed-assets` with an `assetTypeId` that matches no `AssetType` row returns 400 with `error: "INVALID_ASSET_TYPE"` and does not create a row.
- **AC-7:** `POST /api/fixed-assets` with a name that already exists (exact match) returns 409 with `error: "DUPLICATE_ASSET_NAME"`, and no second row is created — enforced via a real DB unique index on `FixedAsset.Name`, not a pre-check query (mirrors `Room.Name`'s pattern).
- **AC-8:** A successful `POST /api/fixed-assets` persists `Price` as `decimal(19,4)` and returns the created asset's id, name, price, purchaseDate, assetTypeId, and quantity.
- **AC-9:** `GET /api/asset-types` returns all seeded `AssetType` rows (id, name) to an authenticated admin.
- **AC-10:** Unauthenticated visit to `/stock-add` redirects to `/login`.
- **AC-11:** Authenticated non-admin visit to `/stock-add` redirects to `/`.
- **AC-12:** The back control on `/stock-add` navigates to `/admin`.
- **AC-13:** `/room-add`, `/room-update`, `/room-delete`, `/room-assignment`, `/admin`, and `/` are all unaffected by this change (no shared guard/route logic touched beyond adding one new route).

## Edge Cases

- Whitespace-only asset name is trimmed before the non-empty check and before the uniqueness check (mirrors `Room.Name.Trim()`).
- Two different admins submitting the same asset name concurrently: the second `SaveChangesAsync` throws `DbUpdateException` from the unique index, caught and mapped to 409 — no race window for a duplicate to slip through (same pattern as `RoomsController.Create`).
- Price/quantity are transmitted from the client as numbers (parsed from the comma-restricted text at submit time, matching how Room Add already sends `departmentId` as a number) — this endpoint does not need to parse a raw comma-decimal string server-side.
- Zero or negative price/quantity: DR-004 requires only non-empty, not a positive-value check — no code path in the legacy app validates the sign or magnitude of these fields, so none is added here (faithful reproduction).

## Dependencies

- BL-004 (Admin Panel Sub-Navigation) — already built (`admin-panel`, merged `90b51c1`). The "Stok Ekle" button already exists and currently 404s; this change gives it a real destination.
- No same-module or cross-module bypass needed.

## Notes

- `AssetType` CRUD is permanently out of scope (CQ-012) — the seed migration's 2-3 rows are the only rows this table will ever have unless someone edits the DB directly, by design.
- This item does not touch `RoomAssetAssignment.AssetId`/`Quantity` (still null-only, reserved for BL-011's Asset Assignment & Stock Decrement composite flow) — it only creates the `FixedAsset` row that BL-011 will later reference.
