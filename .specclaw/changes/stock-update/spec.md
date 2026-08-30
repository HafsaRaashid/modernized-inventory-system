# Spec: BL-010 — Stock / Asset Update

**Change:** stock-update
**Created:** 2026-08-30
**Status:** 🟡 Draft

## Overview

Extends the existing `FixedAssetsController` (built in BL-009) with `GET /api/fixed-assets` (list) and `PUT /api/fixed-assets` (update by id), and adds an admin-only "Stok Güncelle" screen at `/stock-update`. Unlike Room Update (name-keyed, PQ-004), this screen is genuinely ID-keyed — no ambiguity to resolve. Reproduces the legacy Stock Update form's row-select-then-edit pattern and its correctly-wired letter-only filter on the asset-name field (DR-006 — unlike Stock Add's declared-but-unwired defect).

## Requirements

### Functional Requirements

- **FR-1:** An admin-only user can select an existing fixed asset from a dropdown (populated via `GET /api/fixed-assets`), which populates editable fields with that asset's current name, price, purchase date, quantity, and asset type.
- **FR-2:** The admin can edit any of the populated fields and submit an update via `PUT /api/fixed-assets`.
- **FR-3:** The name, price, and quantity fields must be non-empty before submission is allowed (DR-004) — enforced client-side (submit disabled) and re-validated server-side.
- **FR-4:** The asset-name field applies a letter-only keypress restriction (DR-006, correctly wired on this screen per domain-model.md, unlike Stock Add's screen) — restricts keyboard entry to letters, backspace, and comma.
- **FR-5:** A duplicate asset name (colliding with a DIFFERENT existing record) is rejected; the record's own current name does not collide with itself.
- **FR-6:** An `assetTypeId` that doesn't match any existing `AssetType` row is rejected with a 400.
- **FR-7:** Updating an asset id that doesn't exist returns 404.
- **FR-8:** On any update failure (duplicate name, invalid asset type, or not-found), the same single generic message is shown — "Güncellenirken hata oluştu..." (mirrors the legacy screen's own single generic-error path; unlike Room Update, which also uses one shared message, but a different literal string).
- **FR-9:** On success, the form resets (selector and all fields cleared) and the selector list is refreshed, and a success message is shown.
- **FR-10:** The `/stock-update` route is reachable only by an authenticated admin (`RequireAdmin`, reused from BL-005/BL-009) — unauthenticated redirects to `/login`; authenticated non-admin redirects to `/`.
- **FR-11:** A back control returns to `/admin`.

### Non-Functional Requirements

- **NFR-1:** Price and quantity fields restrict keyboard entry to digits, backspace, and comma (DR-005/CQ-014), same filter as Stock Add.
- **NFR-2:** The update is genuinely ID-keyed (`WHERE Id = @id` equivalent), not name-keyed — no CQ-004-style ambiguity exists for this entity.
- **NFR-3:** `Price` continues to be stored as `decimal(19,4)` (CQ-013, already enforced by BL-009's schema — no new migration needed).

## Acceptance Criteria

- **AC-1:** Visiting `/stock-update` while authenticated as an admin renders a selector populated from `GET /api/fixed-assets`, plus name/price/purchase-date/asset-type/quantity fields and a submit button (SCR-009 layout per rebuild-backlog.md's written description).
- **AC-2:** Selecting an asset from the selector populates all fields with that asset's current values (name, price, purchase date, asset type, quantity).
- **AC-3:** Submitting a valid edit calls `PUT /api/fixed-assets` with `{id, name, price, purchaseDate, assetTypeId, quantity}`, and on success resets the form, refreshes the selector, and shows "Demirbaş başarıyla güncellendi."
- **AC-4:** The submit button is disabled (and no API call is made) unless an asset is selected and name/price/quantity are all non-empty.
- **AC-5:** The asset-name field blocks non-letter keystrokes (letters, backspace, and comma only) — a typed digit or symbol does not appear in the field.
- **AC-6:** `PUT /api/fixed-assets` with a blank/whitespace-only name returns 400 with `error: "ASSET_NAME_REQUIRED"`, and the record is not modified.
- **AC-7:** `PUT /api/fixed-assets` with an `assetTypeId` matching no `AssetType` row returns 400 with `error: "INVALID_ASSET_TYPE"`, and the record is not modified.
- **AC-8:** `PUT /api/fixed-assets` renaming a record to a name that collides with a DIFFERENT existing record returns 409 with `error: "DUPLICATE_ASSET_NAME"`; renaming a record to its OWN current name (no-op rename) succeeds.
- **AC-9:** `PUT /api/fixed-assets` with an `id` that matches no `FixedAsset` row returns 404 with `error: "ASSET_NOT_FOUND"`.
- **AC-10:** On any of AC-6/AC-7/AC-8/AC-9's failures, the frontend shows the single generic message "Güncellenirken hata oluştu..." — no per-status branching.
- **AC-11:** `GET /api/fixed-assets` returns all `FixedAsset` rows (id, name, price, purchaseDate, assetTypeId, quantity) to an authenticated admin; a non-admin gets 403.
- **AC-12:** Unauthenticated visit to `/stock-update` redirects to `/login`.
- **AC-13:** Authenticated non-admin visit to `/stock-update` redirects to `/`.
- **AC-14:** The back control on `/stock-update` navigates to `/admin`.
- **AC-15:** `/stock-add`, `/room-add`, `/room-update`, `/room-delete`, `/room-assignment`, `/admin`, and `/` are all unaffected by this change.

## Edge Cases

- Selecting a different asset in the selector before submitting discards any unsaved edits to the previously-selected asset's fields (no dirty-state warning — matches the legacy grid's row-switch behavior, which has no such warning either).
- A no-op update (submitting without changing any field) succeeds — the backend does not special-case "nothing changed."
- Whitespace-only name is trimmed before the non-empty check and before the uniqueness check.
- Two admins editing the same asset concurrently: last write wins (no optimistic-concurrency/`RowVersion` check) — matches the legacy app, which has none either.

## Dependencies

- BL-009 (Stock / Asset Add) — already built (`stock-add`, merged `a351837`). `FixedAssetsController`, `FixedAsset`/`AssetType` entities, and the `decimal(19,4)`/unique-index schema all already exist; this item extends rather than recreates them.
- No same-module or cross-module bypass needed.

## Notes

- No new migration is needed — the schema BL-009 created already supports this item's needs.
- `GET /api/fixed-assets` is a new addition to `FixedAssetsController`, needed both by this screen's selector and, in principle, reusable later by BL-012 (asset search) or BL-014 (reporting) — but this item adds it only for its own selector's needs; no speculative fields are added beyond what BL-009's `Create`/AC-8 already returns per-asset.
