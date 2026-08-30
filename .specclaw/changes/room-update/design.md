# Design: BL-006 — Room Update (rename)

**Change:** room-update
**Created:** 2026-08-30

## Technical Approach

Three pieces, all extending BL-005's existing pieces rather than creating parallel ones:

1. **Backend:** two new actions on the existing `RoomsController` (BL-005) — `GET` (list all rooms) and `PUT` (rename by current name). Both call the existing `AdminAuthorizationExtensions.IsCallerAdminAsync` helper exactly as `Create` already does. No new controller, no new entity, no new migration.
2. **Frontend API client:** two new exports added to the existing `web/src/api/rooms.ts` (BL-005) — `listRooms()` and `updateRoom(oldName, newName)` — reusing the existing `Room` interface.
3. **Frontend screen:** a new `RoomUpdate.tsx` (SCR-012 layout) plus its CSS, structurally mirroring `RoomAdd.tsx`'s pattern (controlled form, `canSubmit` gate, success/error message state). `App.tsx` gets one new `<Route path="/room-update">` wrapped in the existing `RequireAdmin` — no change to `RequireAdmin` itself this time, since it was already generalized to accept children in BL-005.

## Architecture

```
AdminPanel ("Oda Güncelle" button, BL-004) --click--> /room-update
                                                          │
                                                          ▼
                                                 RequireAdmin (BL-005, unchanged)
                                                          │
                                                 <AppShell><RoomUpdate /></AppShell>
                                                          │
                                    on mount: GET /api/rooms (admin-gated, lists {id, name})
                                    on submit: PUT /api/rooms { oldName, newName } (admin-gated)
```

`RoomsController.Update` looks up the room by `Name == oldName` (CQ-004's decided keying, not by `Id`), sets `Name = newName`, and calls `SaveChangesAsync()` inside the same `try/catch (DbUpdateException)` pattern `Create` already uses for the uniqueness constraint — no separate pre-check query for the duplicate case, for the same reason `Create` doesn't have one (the DB constraint is the single source of truth; a pre-check plus the constraint would be two paths for one rule). The not-found case (`oldName` matches no room) *is* an explicit pre-check (`SingleOrDefaultAsync`, distinct rule from uniqueness), mirroring `Create`'s existing `INVALID_DEPARTMENT` pre-check pattern for the same reason — a different rule needs a distinguishable failure, not an exception-string guess.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Api/Controllers/RoomsController.cs` | Modify | Add `[HttpGet] List()` and `[HttpPut] Update()` actions |
| `web/src/api/rooms.ts` | Modify | Add `listRooms()` and `updateRoom(oldName, newName)` |
| `web/src/routes/RoomUpdate.tsx` | Create | SCR-012 layout: existing-room selector, new-name field, GÜNCELLE button, back control |
| `web/src/routes/RoomUpdate.css` | Create | Layout styling, matching `RoomAdd.css`'s conventions |
| `web/src/App.tsx` | Modify | Register `/room-update` through the existing (unchanged) `RequireAdmin` |
| `api/tests/InventoryTrackingSystem.Api.Tests/RoomsControllerTests.cs` | Modify | Add tests for `List`/`Update` (AC-3, AC-4, AC-5, AC-9, AC-10, AC-13) |
| `web/tests/RoomUpdate.test.tsx` | Create | AC-1, AC-2, AC-3, AC-4, AC-11 |
| `web/tests/App.test.tsx` | Modify | AC-6, AC-7, AC-8, AC-12 (regression for `/admin` and `/room-add`) |

## Data Model Changes

None — reuses `Room`/`Department` exactly as BL-005 defined them. No migration.

## API Changes

**`GET /api/rooms`** *(new action on the existing `RoomsController`)*
- Auth: `[Authorize]` + `IsCallerAdminAsync` (403 if not admin).
- Response `200`: `[{ "id": number, "name": string }, ...]`.

**`PUT /api/rooms`** *(new action on the existing `RoomsController`)*
- Auth: `[Authorize]` + `IsCallerAdminAsync` (403 if not admin).
- Request: `{ "oldName": string, "newName": string }`.
- `400 Bad Request` — `{ "error": "ROOM_NAME_REQUIRED", "message": "Oda adı gereklidir." }` when `newName` is null/empty/whitespace-only.
- `404 Not Found` — `{ "error": "ROOM_NOT_FOUND", "message": "Hatalı İşlem..." }` when no room's `Name` matches `oldName`.
- `409 Conflict` — `{ "error": "DUPLICATE_ROOM_NAME", "message": "Hatalı İşlem..." }` when the unique index on `Room.Name` rejects the update (caught as `DbUpdateException`, same mechanism `Create` already uses).
- `200 OK` — `{ "id": number, "name": string, "departmentId": number }` on success.

## Key Decisions

- **Rename keyed by current name, not ID (CQ-004).** `RoomsController.Update` looks the room up via `_db.Rooms.SingleOrDefaultAsync(r => r.Name == request.OldName)`, not `FindAsync(id)`. This is a deliberate legacy-parity choice, not an oversight — CQ-004 decided to preserve it, contingent on BL-005's uniqueness constraint making it safe (a `SingleOrDefaultAsync` on a unique column can only ever match zero or one row).
- **One generic error message for every server-side failure, matching the legacy screen.** Unlike Room Add (which had two distinct legacy messages — a validation state and "Kayıtlı Oda..."), Room Update's legacy screen has exactly one generic catch (`"Hatalı İşlem..."`). Both the not-found and duplicate-name failures surface that same string to the user, even though they carry distinct `error` codes internally (`ROOM_NOT_FOUND` vs `DUPLICATE_ROOM_NAME`) for a caller that wants to branch on them.
- **No pre-check for the duplicate-name case; an explicit pre-check for the not-found case.** Consistent with BL-005's own reasoning: uniqueness is a single-source-of-truth DB constraint (no pre-check, catch `DbUpdateException`), while "does this room exist" is a different rule entirely and gets its own direct query, exactly like `Create`'s `INVALID_DEPARTMENT` check.
- **FR-4/FR-6 are judgment calls, not CQ-decided rules.** functional-spec.md's DR-004 form list excludes `frmOdaGuncelle.cs` (no legacy required-field check on the new-name field at all), and no CQ decision addresses the "old name matches no row" no-op. Both are called out explicitly in spec.md rather than silently assumed.
- **`RequireAdmin` is not touched.** BL-005 already generalized it to accept `children`; this item is the first to actually prove that generalization pays off — a third admin-only route with zero changes to the guard itself.

## Risks & Mitigations

- **Risk:** Matching by name instead of ID could silently break if `Room.Name` weren't actually unique. **Mitigation:** it is — BL-005 added a real unique index, and CQ-004's decision explicitly conditions this design on that constraint existing, which it now does.
- **Risk:** Reusing one generic error message for two different failure modes (not-found vs duplicate) could make debugging harder for an admin user. **Mitigation:** this matches the legacy screen's own single-generic-catch UX exactly (per `ui-inventory.md`'s SCR-012 states) — not a regression introduced by this item, and the distinct `error` codes remain available in the response body for programmatic callers/logs even though the displayed text is shared.
- **Risk:** No golden-master fixture exists yet for this screen (`GM-023`/`GM-024` PENDING CAPTURE). **Mitigation:** acceptance rests on spec.md's criteria plus manual comparison until a human captures fresh fixtures against the constrained schema, same posture BL-005 already established.
