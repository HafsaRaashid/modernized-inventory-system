# Proposal: BL-010 — Stock / Asset Update

**Created:** 2026-08-30
**Status:** 🟢 Approved (approved via /specclaw:auto invocation, 2026-08-30)

## Problem

BL-009 (stock-add) created `FixedAsset` records but there is no way to edit one afterward — the Admin Panel's "Stok Güncelle" button currently falls through to `NotFound`. Unlike Room Update (which is name-keyed, PQ-004), the legacy Stock Update screen is correctly ID-keyed (`WHERE DemirbasID=@demirbasID`), so this item carries no CQ-004-style ambiguity.

## Proposed Solution

A `/stock-update` screen (SCR-009), admin-gated, reproducing the legacy Stock Update form's row-select-then-edit pattern:

- **Selector:** a dropdown (standing in for the legacy `DataGridView`'s row-selection, same simplification Room Update already used for its `ComboBox`→`<select>`) listing every `FixedAsset` by name. Selecting one populates all editable fields (name, price, purchase date, quantity, asset type) from the already-fetched list — no extra round-trip per selection.
- **Fields:** asset name (this time WITH a letter-only keypress filter — DR-006 is correctly wired on this legacy screen, unlike Stock Add's defect), price (digit/comma-only), purchase date, asset type (dropdown from `GET /api/asset-types`, reused from BL-009), quantity (digit/comma-only).
- **Backend:** extend the existing `FixedAssetsController` (BL-009) with `GET /api/fixed-assets` (list, for the selector) and `PUT /api/fixed-assets` (update by id — genuinely ID-keyed, not name-keyed) — validates non-empty name/price/quantity, validates the asset type exists, re-checks the name-uniqueness constraint (excluding the record's own id), and re-validates the referenced asset exists (404 if not).
- **Error handling:** unlike Room Update's shared generic message for both duplicate-name and not-found, Stock Update's own legacy error text is a single generic message covering all update failures — "Güncellenirken hata oluştu..." (per the backlog's own citation of `frmStokGuncelleme`'s error path) — so no per-status branching is needed here either, one message for every failure.

## Scope

### In Scope
- `GET /api/fixed-assets` (list) and `PUT /api/fixed-assets` (update by id) on the existing `FixedAssetsController`.
- `/stock-update` React screen + route, admin-gated (`RequireAdmin`, reused).
- Row-selection-populates-fields behavior, non-empty validation (DR-004), duplicate-name rejection (excluding the record's own row), not-found handling, letter-only filter on the name field (DR-006, correctly wired this time).
- Backend + frontend tests.

### Out of Scope
- Stock/Asset Add (BL-009, already built) and Stock/Asset Delete (not yet a backlog item — the legacy app has no stock-delete screen at all, per module-map.md's screen inventory).
- Asset Assignment & Stock Decrement (BL-011) — this item does not touch the assignment/decrement flow.
- Any change to `AssetType` (still permanently read-only, CQ-012).

## Impact

- **Files affected:** ~9 (1 controller extended, 1 API client extended, 1 frontend screen + CSS, `App.tsx`, 2 test files extended/created) — smaller than BL-009 since no new entities/migration are needed.
- **Complexity:** medium
- **Risk:** low — no dependency bypass needed (BL-009 already built, same module), extends existing, already-tested infrastructure (`FixedAsset`, `AssetType`, admin-gating pattern) rather than introducing anything new.

## Open Questions

- CQ-027 (unanswered, explicitly non-blocking per the backlog's Gate note, same as every prior item) — the legacy form's decoupled required-field validation paths; the rebuild continues to implement a single real validation path, consistent with every prior item.
- UI fidelity artifacts are still absent project-wide (deferred to end of backlog) — SCR-009's layout is reproduced from `ui-inventory.md`'s written structure only, same basis as every prior screen.
- The exact legacy error string for this screen's generic-failure path ("Guncellerken hata olustu...") is cited in rebuild-backlog.md's "Verification inputs needed" note as not yet captured in a golden-master fixture — this proposal uses that cited text as-is; if a future GM capture shows a different exact string, a follow-up correction is a one-line fix, not a re-scope.

---

**To proceed:** Review this proposal and approve to begin planning.
