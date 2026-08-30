# Proposal: BL-009 — Stock / Asset Add

**Created:** 2026-08-30
**Status:** 🟢 Approved (approved via /specclaw:auto invocation, 2026-08-30)

## Problem

The Admin Panel (BL-004) has a "Stok Ekle" (Stock Add) button that currently falls through to `NotFound` — there is no way to create a `FixedAsset` (tblDemirbas) record at all. Nothing downstream (Stock Update, Asset Assignment & Stock Decrement, Asset Search) can be built or tested against real data until fixed assets can be created.

## Proposed Solution

A `/stock-add` screen (SCR-008), admin-gated like Room Add/Update/Delete, reproducing the legacy Stock Add form's fields and rules:

- **Fields:** asset name (plain text input, no letter filter — CQ-015 decided defect, reproduced as-is), price (digit/comma-only filtered), purchase date (native date input), asset type (dropdown sourced from the read-only `AssetType` lookup, CQ-012 — no CRUD for it), quantity (digit/comma-only filtered).
- **Client-side non-empty validation** on name/price/quantity only (DR-004), matching Room Add's pattern.
- **Backend:** `POST /api/fixed-assets` — validates required fields, validates the referenced `AssetTypeId` exists, enforces a real uniqueness constraint on `DemirbasAdi`/asset name (CQ-018, decided — no such constraint existed in the legacy DB, but the rebuild adds one, same reasoning as Room Add's `Room.Name`), stores `Fiyat` as `decimal(19,4)` (CQ-013, decided).
- **`GET /api/asset-types`** — read-only lookup endpoint (no admin gate needed beyond auth, mirrors `DepartmentsController`'s existing read-only pattern) to populate the asset-type dropdown.
- An `AssetType` seed migration (2-3 dev/test rows), same approach as BL-005's `Department` seed and BL-008's `Personnel` seed — there is no other item in the backlog that creates `AssetType` rows (CQ-012: no CRUD for it, ever).

## Scope

### In Scope
- `FixedAsset` and `AssetType` domain entities + EF Core migration.
- `POST /api/fixed-assets` (create), `GET /api/asset-types` (list, for the dropdown).
- `/stock-add` React screen + route, admin-gated (`RequireAdmin`, reused from BL-005).
- Non-empty validation (name/price/quantity), duplicate-name rejection, asset-type existence validation.
- Backend + frontend tests.

### Out of Scope
- Stock/Asset Update and Delete (BL-010, separate item).
- Asset Assignment & Stock Decrement — the composite flow that actually issues assets into rooms and decrements `Adet` (BL-011; this item only creates the initial on-hand quantity).
- Any CRUD for `AssetType` itself (CQ-012, decided: read-only lookup, provisioned outside this application — permanently, not just deferred).
- Reproducing DR-006's letter-only keypress filter on the asset-name field — CQ-015 decided this is a defect (declared but never wired in the legacy form) that should be reproduced as-is, i.e. the asset-name field gets NO letter filter, matching the legacy bug faithfully.

## Impact

- **Files affected:** ~14 (2 domain entities, 1 migration + designer + snapshot, 2 controllers, 2 request POCOs, 1 frontend screen + CSS, 2 API client modules, `App.tsx`, 2 test files) — similar footprint to BL-005/BL-008.
- **Complexity:** medium
- **Risk:** low — no dependency bypass needed (BL-004 already built), no cross-module coupling; `AssetType` is a brand-new read-only lookup table with no prior art to conflict with.

## Open Questions

- CQ-027 (unanswered, explicitly non-blocking per the backlog's Gate note): the legacy form has two decoupled required-field validation paths (a cosmetic `ErrorProvider` and a separate `Text.Trim() != ""` check) for DR-004. Like every prior item in this backlog, the rebuild implements a single, real validation path (client-side disable + server-side 400), not two decoupled ones — this is consistent with how Room Add/Update handled the same DR-004 pattern.
- UI fidelity artifacts (`.specclaw/ui/screens/`, `.specclaw/ui/ui-manifest.json`) are still absent project-wide (flagged since BL-004, deferred to end of backlog per project decision) — SCR-008's layout is reproduced from `ui-inventory.md`'s written structure only, same basis used for every prior screen in this backlog.

---

**To proceed:** Review this proposal and approve to begin planning.
