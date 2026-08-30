# Design: BL-007 — Room Delete

**Change:** room-delete
**Created:** 2026-08-30

## Technical Approach

Three pieces, all extending existing files rather than creating parallel infrastructure:

1. **Backend:** one new action on the existing `RoomsController` (BL-005/BL-006) — `[HttpDelete] Delete([FromBody] DeleteRoomRequest request)`, gated by the existing `AdminAuthorizationExtensions.IsCallerAdminAsync`. No new controller, no new entity, no new migration.
2. **Frontend API client:** one new export added to the existing `web/src/api/rooms.ts` — `deleteRoom(name)` — reusing the existing `Room` interface for its resolved type.
3. **Frontend screen:** a new `RoomDelete.tsx` (SCR-011 layout) plus its CSS, reusing `listRooms()` (BL-006) to populate the room selector. `App.tsx` gets one new `<Route path="/room-delete">` wrapped in the existing, **unchanged** `RequireAdmin`.

## Architecture

```
AdminPanel ("Oda Sil" button, BL-004) --click--> /room-delete
                                                       │
                                                       ▼
                                              RequireAdmin (BL-005, unchanged)
                                                       │
                                              <AppShell><RoomDelete /></AppShell>
                                                       │
                                on mount: GET /api/rooms (BL-006, admin-gated, reused as-is)
                                on click "SİL" (no confirmation step): DELETE /api/rooms { name } (admin-gated)
```

`RoomsController.Delete` looks up the room by `Name == request.Name` (CQ-004's decided keying, same lookup shape `Update` already uses), returns `404` if not found, otherwise removes it and saves — no `try/catch` for a uniqueness or FK violation, because deleting a row never conflicts with a unique index, and no FK constraint exists in this schema to violate (CQ-023's guard, which *would* need one, is explicitly deferred — see spec.md Overview).

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Api/Controllers/RoomsController.cs` | Modify | Add `[HttpDelete] Delete()` action + `DeleteRoomRequest` POCO |
| `web/src/api/rooms.ts` | Modify | Add `deleteRoom(name)` |
| `web/src/routes/RoomDelete.tsx` | Create | SCR-011 layout: room selector + "SİL" button in one row, no confirmation, back control |
| `web/src/routes/RoomDelete.css` | Create | Layout styling, matching `RoomUpdate.css`'s conventions |
| `web/src/App.tsx` | Modify | Register `/room-delete` through the existing (unchanged) `RequireAdmin` |
| `api/tests/InventoryTrackingSystem.Api.Tests/RoomsControllerTests.cs` | Modify | Add tests for `Delete` (AC-3, AC-4, AC-11, AC-12) |
| `web/tests/RoomDelete.test.tsx` | Create | AC-1, AC-2, AC-3, AC-9 |
| `web/tests/App.test.tsx` | Modify | AC-6, AC-7, AC-8, AC-10 (regression for `/admin`, `/room-add`, `/room-update`) |

## Data Model Changes

None — reuses `Room` exactly as BL-005 defined it. No migration.

## API Changes

**`DELETE /api/rooms`** *(new action on the existing `RoomsController`)*
- Auth: `[Authorize]` + `IsCallerAdminAsync` (403 if not admin).
- Request: `{ "name": string }` (DELETE with a body — matching `Update`'s PUT-with-body pattern for the same name-keyed reason, CQ-004, and avoiding URL-encoding concerns for names with special characters).
- `404 Not Found` — `{ "error": "ROOM_NOT_FOUND", "message": "Hatalı İşlem..." }` when no room's `Name` matches the given `name` (reusing BL-006's not-found message/code for consistency across the admin screens — this screen itself has no documented legacy error text at all, see spec.md Notes).
- `200 OK` — `{ "id": number, "name": string, "departmentId": number }` echoing the deleted room, for consistency with `Create`/`Update`'s response shape.

No `409`/duplicate path exists for delete — there is nothing to collide with.

## Key Decisions

- **Delete keyed by current name, not ID (CQ-004).** Same reasoning and same lookup shape as `Update`: `_db.Rooms.SingleOrDefaultAsync(r => r.Name == request.Name)`, safe because `Room.Name` is uniquely constrained (BL-005).
- **No confirmation step, anywhere — client or server (CQ-017).** The legacy screen has none; the faithful-by-default policy (SQ-012) preserves that rather than "improving" it. This is a deliberate reproduction, not an oversight — flagged explicitly so a future reviewer doesn't add one thinking it was missed.
- **CQ-023's FK-guard is not implemented, and is asserted absent (AC-13).** The guard depends on `RoomAssetAssignment` (MOD-003, not built — BL-011). Implementing even a placeholder check against a table that doesn't exist would mean inventing schema that belongs to a different backlog item's design, not this one's. The absence is deliberate and disclosed (proposal.md's "Item Split (informal)"), not silently dropped.
- **No formal `IS-###` split record.** `specclaw-bf-rebuild-collect split-append` refuses items whose acceptance basis cites no `DR-###` rule; BL-007 cites only `CQ-###` decisions. The deferral is recorded in prose in proposal.md and spec.md instead — a known tooling gap, not a workaround chosen to avoid the mechanism.
- **Reuses `GET /api/rooms` (BL-006) as-is** for the room selector — no new list endpoint, no duplicated query.
- **`RequireAdmin` is not touched**, same as BL-006 — a third (now fourth, counting `/admin` itself) admin-only route through the same unmodified guard.

## Risks & Mitigations

- **Risk:** Once BL-011 lands and introduces `RoomAssetAssignment`, this endpoint will still delete a room with live assignments unless a human manually revisits it — there is no mechanical `blocked_until` tracking for this deferral (no `IS-###` exists to carry it). **Mitigation:** flagged prominently in proposal.md, spec.md, and this design doc; the deferred-scope assertion (AC-13) makes the gap visible to anyone reading the spec, not just this design doc.
- **Risk:** No confirmation dialog is a real UX risk (a misclick deletes a room immediately, permanently). **Mitigation:** this is an intentional, CQ-017-decided reproduction of legacy behavior, not a decision made unilaterally by this change — the risk is accepted at the project level, not introduced here.
- **Risk:** Reusing BL-006's "Hatalı İşlem..." message for a screen that has no documented legacy error text of its own could be wrong if a real legacy citation later surfaces. **Mitigation:** flagged explicitly as an assumption in spec.md Notes, not presented as a verified citation.
