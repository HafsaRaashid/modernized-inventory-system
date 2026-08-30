# Proposal: BL-007 — Room Delete

**Created:** 2026-08-30
**Status:** 🟢 Approved (approved via user's continue-the-backlog instruction, 2026-08-30)

## Problem

The Admin Panel's "Oda Sil" button (BL-004) currently falls through to `NotFound` at `/room-delete` — there is no Room Delete screen. The legacy screen (`frmOdaSil.cs`) lets an admin pick an existing room by name and delete it, with **no confirmation dialog** (CQ-017, decided DEFECT: reproduce as-is, per the faithful-by-default policy). The delete is keyed by name, not ID (CQ-004, same pattern as BL-006, now safe under BL-005's uniqueness constraint).

**A genuine cross-module gap:** this item's own acceptance basis (CQ-023, decided) requires the rebuild to treat deleting a room that still has assigned assets/personnel as "a real, reportable error condition" — not the legacy's unhandled crash, and not a silent orphan. That guard depends on a `RoomAssetAssignment` entity, which `module-map.md` places in **MOD-003 (Asset Assignment & Stock)** — a different module, not yet built (its own backlog item is BL-011, several items away). The backlog's own `Depends on:` field for BL-007 only lists BL-005, so this cross-module dependency was never mechanically declared — this proposal makes it explicit.

## Proposed Solution

Ship a real, complete Room Delete now; defer CQ-023's FK-guard until BL-011 exists (see "Item Split (informal)" below):

- **`DELETE /api/rooms`** *(new)* — deletes a room matched by its current name (`{ name }` in the request body — DELETE with a body, matching the existing name-keyed pattern from BL-006's `PUT`), admin-gated via the existing `AdminAuthorizationExtensions.IsCallerAdminAsync`. No confirmation step (CQ-017). Reports `404 Not Found` if no room matches the given name (an honest error, not a silent no-op — same posture BL-006 already established for its own not-found case).
- **Frontend:** a new Room Delete screen (SCR-011 layout, "ODA SİLME") at `/room-delete` — a room selector (populated from `GET /api/rooms`, reused from BL-006) and a "SİL" (delete) button in a single row, no confirmation dialog. On success: selector clears and re-populates (matching the legacy "selector cleared and re-populated" success state), success message shown. Gated the same way `/room-add`/`/room-update` are (reusing `RequireAdmin`, unchanged).

## Scope

### In Scope
- `DELETE /api/rooms` (delete-by-name), admin-gated, real database delete
- Room Delete screen (SCR-011 layout) at `/room-delete`, reachable from the Admin Panel's "Oda Sil" button (already wired by BL-004)
- No confirmation dialog (CQ-017 — faithful legacy reproduction)
- Delete matched by current name, not ID (CQ-004, same pattern as BL-006)
- Route-level and endpoint-level admin gating, matching BL-005/BL-006's pattern exactly

### Out of Scope
- **CQ-023's FK-guard** ("cannot delete a room with existing asset/personnel assignments") — genuinely undeliverable right now: the `RoomAssetAssignment` entity it depends on belongs to MOD-003 and does not exist in this schema, so there is no code path today that could ever create the condition the guard exists to catch. Deferred until BL-011 (Asset Assignment) introduces `RoomAssetAssignment` — see "Item Split (informal)" below.
- Room Add/Update (BL-005/BL-006 — separate, already-built items, same `Room` entity)
- Any new database migration — `Room` already exists from BL-005; this item is API + UI only

## Item Split (informal — no `IS-###` recorded)

**Chosen strategy:** item-split (ship the real, usable delete capability now; defer one specific business rule).

**Why no formal `IS-###` registry entry exists:** `specclaw-bf-rebuild-collect split-append` mechanically refuses this item — its acceptance basis cites zero `DR-###` business rules (only `CQ-023`/`CQ-004`/`CQ-017` decision citations), and the tool's rule-partition mechanism requires at least one `DR-###` to split the now/deferred halves against ("an item with no rule citations cannot be split mechanically"). This is a real tooling gap, not a decision to route around by inventing a fake `DR-###` id. The deferral below is recorded here in prose instead, and does **not** get the mechanical protections a registered split gets (no automatic `bf-replay --item BL-007` PARTIAL marking, no `blocked_until` tracking that flips automatically when BL-011 lands) — a human should revisit this item manually once BL-011 exists.

- **Implemented now:** `DELETE /api/rooms` (delete-by-name, admin-gated, 404 on not-found), the Room Delete screen (SCR-011), no confirmation dialog (CQ-017), name-keyed matching (CQ-004).
- **Deferred:** CQ-023's FK-guard — rejecting a delete when the room has existing `RoomAssetAssignment` rows, surfaced as a reportable error rather than a crash.
- **What unblocks the remainder:** BL-011 (Asset Assignment and Stock Decrement) landing, which introduces the `RoomAssetAssignment` entity this guard checks against.
- **Where the deferred scope attaches:** inside `RoomsController`'s new `Delete` action, as an additional pre-check (`if (await _db.RoomAssetAssignments.AnyAsync(a => a.RoomId == room.Id)) return Conflict(...)`) — the same shape as this item's own admin-check/not-found pre-checks, just against a table that doesn't exist yet.
- **No layer is removed.** This is not a horizontal cut — the full UI/API/persistence stack for Room Delete ships now; only one specific validation rule (the FK-guard) is deferred, not the whole screen or capability.

## Impact

- **Files affected:** ~6 (estimated) — a `RoomsController` addition (`DELETE`, alongside the existing `Create`/`List`/`Update`), a frontend API client addition (`deleteRoom` in `web/src/api/rooms.ts`), a new `RoomDelete.tsx` + CSS, `App.tsx` route registration, and backend/frontend tests
- **Complexity:** small — reuses BL-005/BL-006's admin-gating helper, `GET /api/rooms` (BL-006), and route-guard pattern directly; no new entity or migration
- **Risk:** low, plus one explicitly-accepted known gap (CQ-023's guard not yet enforceable) — flagged prominently rather than silently dropped

## Open Questions

- **UI grounding missing:** SQ-013 (FAITHFUL) is decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent — layout built from `ui-inventory.md`'s SCR-011 description, consistent with BL-001 through BL-006.
- **No exact success-message text is documented** — `ui-inventory.md` only says "message shown" (`frmOdaSil.cs:67-70`), same gap BL-006 already had for its own screen. A natural analogue ("Oda başarıyla silindi.") will be used, flagged as an assumption, not a legacy-parity-verified string.
- **GM-026 through GM-029 are captured fixtures** (per rebuild-backlog.md), but GM-029 (deleting a room with an assignment) pins the **legacy crash**, not this item's own target behavior — and since this item defers the FK-guard entirely, GM-029 isn't a target for this change at all right now. A human should revisit GM-029 alongside BL-011.
- **When BL-011 lands, a human must manually revisit this item** to add the FK-guard check — there is no mechanical `blocked_until` tracking for this deferral (see "Item Split (informal)" above).

---

**To proceed:** Review this proposal and approve to begin planning.
