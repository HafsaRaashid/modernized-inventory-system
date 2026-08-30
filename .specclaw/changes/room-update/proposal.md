# Proposal: BL-006 — Room Update (rename)

**Created:** 2026-08-30
**Status:** 🟢 Approved (approved via user's continue-the-backlog instruction, 2026-08-30)

## Problem

The Admin Panel's "Oda Güncelle" button (BL-004) currently falls through to `NotFound` at `/room-update` — there is no Room Update screen. The legacy screen (`frmOdaGuncelle.cs`) lets an admin pick an existing room by name from a live-loaded list and rename it to a new name. Renaming is matched by the room's *current name*, not its ID (`WHERE OdaAdi=@old`) — the sole exception to this codebase's otherwise ID-keyed CRUD pattern (Named Gap 4). Per CQ-004 (decided DEFECT, option a), this name-keyed matching is preserved intentionally in the rebuild, but the decision explicitly notes it "only becomes safe once BL-005's CQ-018 uniqueness constraint exists" — which it now does (BL-005 is built and merged).

## Proposed Solution

- **`GET /api/rooms`** *(new)* — lists all rooms (`id`, `name`), admin-gated, to populate the existing-room selector (replacing the legacy `SELECT * FROM tblOda` ComboBox source with a live API call).
- **`PUT /api/rooms`** *(new)* — renames a room, matched by its **current name** (not ID), per CQ-004's decided keying. Body: `{ oldName, newName }`. Admin-gated via the same `AdminAuthorizationExtensions.IsCallerAdminAsync` helper BL-005 already built (no new auth mechanism). Validates the new name is non-empty (DR-004-style, mirroring BL-005's pattern), rejects a rename to a name already used by *another* room via the same real uniqueness constraint BL-005 added on `Room.Name` (now safe to rely on, per CQ-004's own note), and reports a distinct outcome if `oldName` no longer matches any room (a live-loaded selector reduces staleness risk versus legacy, but doesn't eliminate it across concurrent sessions).
- **Frontend:** a new Room Update screen (SCR-012 layout, "ODA GÜNCELLEME") at `/room-update` — an existing-room selector (populated from `GET /api/rooms`), a new-name field, and a "GÜNCELLE" button, gated the same way `/room-add` is (reusing `RequireAdmin`).

## Scope

### In Scope
- `GET /api/rooms` (list) and `PUT /api/rooms` (rename-by-name) endpoints, both admin-gated
- Room Update screen (SCR-012 layout) at `/room-update`, reachable from the Admin Panel's "Oda Güncelle" button (already wired by BL-004)
- Rename matched by current name per CQ-004 (not by ID)
- Duplicate-new-name rejection via the existing `Room.Name` uniqueness constraint (BL-005)
- Route-level and endpoint-level admin gating, matching BL-005's pattern exactly

### Out of Scope
- Room Delete (BL-007 — separate backlog item, same `Room` entity)
- Any change to Room Add's own behavior or schema (BL-005 is complete and unaffected)
- An ID-keyed update variant — CQ-004 decided to preserve name-keyed matching intentionally; this item does not introduce a parallel ID-based path
- Any new database migration — `Room`/`Department` entities and the uniqueness constraint already exist from BL-005; this item is API + UI only

## Impact

- **Files affected:** ~6–7 (estimated) — a `RoomsController` addition (two new actions on the existing controller, or new endpoints alongside it), a frontend API client addition (`web/src/api/rooms.ts`), a new `RoomUpdate.tsx` + CSS, `App.tsx` route registration, and backend/frontend tests
- **Complexity:** small-medium — reuses BL-005's admin-gating helper, uniqueness constraint, and route-guard pattern directly; no new entity or migration
- **Risk:** low-medium — the name-keyed match (not ID) is an intentional legacy-parity quirk (CQ-004), not a mistake, but is worth calling out clearly in the spec so a future reader doesn't "fix" it into an ID-keyed lookup

## Open Questions

- **UI grounding missing:** SQ-013 (FAITHFUL) is decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent — per project decision, screenshots will be captured at the end of the whole backlog; this item's layout is built from `ui-inventory.md`'s SCR-012 description, consistent with BL-001 through BL-005.
- **No exact success-message text is documented for this screen** — `ui-inventory.md` only says "message shown" (`frmOdaGuncelle.cs:67-71`), unlike Room Add's message which functional-spec.md quotes verbatim. This proposal will use "Oda başarıyla güncellendi." as a natural analogue to Room Add's confirmed message, flagged explicitly as an assumption, not a legacy-parity-verified string.
- **The legacy generic error message IS documented** ("Hatalı İşlem...", `frmOdaGuncelle.cs:73-76`, a single generic catch for any failure) — this proposal reuses it for every failure path (not-found old name, duplicate new name) rather than inventing distinct per-case strings, matching the legacy screen's own single-generic-catch behavior.
- **"Old name matches no row" (GM-024):** no CQ decision addresses whether this should stay a legacy-style silent no-op-success or become an honest error. This proposal treats it as an honest rejection (distinct from success) rather than reproducing the legacy no-op, consistent with how BL-005 already fixed CQ-008's silent no-op field-clear — flagged here as a judgment call, not a decided rule, since GM-025's related scenario is noted as historical-only once BL-005's constraint exists.
- **GM-023/GM-024/GM-025 are PENDING CAPTURE** and GM-025 specifically describes pre-constraint legacy behavior no longer reachable — this item's acceptance rests on the criteria in spec.md plus manual comparison, not fixture replay, until fresh golden-master data exists against the constrained schema.

---

**To proceed:** Review this proposal and approve to begin planning.
