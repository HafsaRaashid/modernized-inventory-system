# Verification Report: room-add

**Verified:** 2026-08-30
**Model:** claude-sonnet-5
**Verdict:** PASS

## Acceptance Criteria

- ✅ **AC-1:** At `/room-add` (as an admin), the screen renders the room-name field, department picker, disabled department-ID echo field, and "EKLE" button (SCR-010 layout). — `web/src/routes/RoomAdd.tsx` renders `<input id="room-name">`, `<select id="department">`, `<input id="department-id" disabled readOnly>`, and `<button type="submit" className="room-add__button">EKLE</button>`. Confirmed by running `npx vitest run`: `✓ tests/RoomAdd.test.tsx (8 tests)` including `"AC-1: renders the room-name input, department picker, disabled department-ID echo input, and EKLE button"`.

- ✅ **AC-2:** Selecting a department in the picker sets the disabled echo field to that department's ID. — `RoomAdd.tsx`: `<select ... onChange={(event) => setDepartmentId(event.target.value)}>` and `<input id="department-id" ... value={departmentId} disabled readOnly />` — the same state drives both. Test `"AC-2: selecting a department option updates the disabled echo input's value to that department's id"` passed in the actual vitest run.

- ✅ **AC-3:** Submitting a non-empty room name with a selected department succeeds: `POST /api/rooms` called, room created, fields reset, success message shown. — `handleSubmit`: `await createRoom(roomName.trim(), Number(departmentId)); setRoomName(""); setDepartmentId(""); setMessage({ text: SUCCESS_MESSAGE, kind: "success" });` where `SUCCESS_MESSAGE = "Oda başarıyla eklendi."`. Backend `RoomsController.Create` returns `Created(...)` with `{ id, name, departmentId }` on success. Test `"AC-3: submitting with a room name and department calls createRoom and shows success, resetting fields"` passed (asserts trimmed-name call, success text, and both fields empty afterward).

- ✅ **AC-4:** Submitting an empty or whitespace-only room name shows a validation indicator and does not call `POST /api/rooms`. — `canSubmit = roomName.trim() !== "" && departmentId !== ""`; submit button has `disabled={!canSubmit}`. Two passing tests: `"AC-4: leaving the room name empty keeps EKLE disabled..."` and `"AC-4: a whitespace-only room name keeps EKLE disabled..."`, both asserting `expect(createRoom).not.toHaveBeenCalled()`. Backend also defends this independently: `if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "ROOM_NAME_REQUIRED", ... })`.
  - ⚠️ Edge case: "validation indicator" is realized only as a disabled button, not a distinct error/help text next to the field — acceptable per FR-4's wording ("rejected... before any API call") but worth flagging if a more explicit inline validation message was expected.

- ✅ **AC-5:** Submitting a duplicate room name calls `POST /api/rooms`, which responds `409 Conflict`, and the UI shows "Kayıtlı Oda...". — `RoomsController.Create`: `catch (DbUpdateException) { return Conflict(new { error = "DUPLICATE_ROOM_NAME", message = "Kayıtlı Oda..." }); }`, backed by a real unique index (`AppDbContext`: `modelBuilder.Entity<Room>().HasIndex(r => r.Name).IsUnique();`) and a migration column `Name = nvarchar(450)` with `CreateIndex(..., unique: true)`. Frontend: `if (error instanceof ApiError && error.status === 409) setMessage({ text: DUPLICATE_MESSAGE, kind: "error" });` where `DUPLICATE_MESSAGE = "Kayıtlı Oda..."`. Backend test `Create_ReturnsDuplicateRoomName_ForSameNameTwice` (first call 201, second 409 DUPLICATE_ROOM_NAME) and frontend test `"shows the duplicate-room message when createRoom rejects with a 409 ApiError"` both present; frontend one confirmed passing in the actual run.

- ✅ **AC-6:** A department selection is required before submitting — the picker must have a selection before "EKLE" is enabled/submittable. — Same `canSubmit` gate requires `departmentId !== ""`. Passing test `"AC-13: leaving no department selected keeps EKLE disabled..."` covers this directly (labeled AC-13 in the test file, semantically satisfies AC-6 too).

- ✅ **AC-7:** `/room-add` is gated the same way `/admin` already is (unauth → `/login`; authenticated non-admin → `/`; nothing renders while pending). — `App.tsx`: `<Route path="/room-add" element={<RequireAdmin><RoomAdd /></RequireAdmin>} />`, and `RequireAdmin` implements `if (!token) return <Navigate to="/login" replace />; if (status === "loading") return null; if (status === "not-admin") return <Navigate to="/" replace />;`. Confirmed by actual passing tests in `App.test.tsx`: `"an unauthenticated visit to /room-add shows the Login screen"`, `"an authenticated non-admin visiting /room-add ends up back at the Main Menu"`, `"an authenticated admin visiting /room-add sees the Room Add screen"` — all part of the `10 tests` that passed in `tests/App.test.tsx`.

- ✅ **AC-8:** `POST /api/rooms` and `GET /api/departments` independently enforce admin-only access server-side (403 for authenticated non-admin). — Both controllers open with `if (!await this.IsCallerAdminAsync(_db)) { return Forbid(); }` before any other logic, each backed by its own `[Authorize]` attribute and independent call to `IsCallerAdminAsync`. Backend tests `Create_ReturnsForbidden_ForNonAdminCaller` (Rooms) and `List_ReturnsForbidden_ForNonAdminCaller` (Departments) assert 403 in each controller separately.

- ✅ **AC-9:** Calling `POST /api/rooms` directly as an authenticated non-admin returns `403 Forbidden`. — Same `Forbid()` path in `RoomsController.Create`. Test `Create_ReturnsForbidden_ForNonAdminCaller` seeds `yetkiId: false`, asserts `Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);`.

- ✅ **AC-10:** Calling `GET /api/departments` directly as an authenticated non-admin returns `403 Forbidden`. — `DepartmentsController.List`'s `Forbid()` path. Test `List_ReturnsForbidden_ForNonAdminCaller` asserts `HttpStatusCode.Forbidden`.

- ✅ **AC-11:** The back control navigates to `/admin`. — `<button type="button" className="room-add__back" onClick={() => navigate("/admin")}>Geri</button>`. Passing test `"AC-11: clicking the back button calls navigate with /admin"` asserts `expect(mockNavigate).toHaveBeenCalledWith("/admin")`.

- ✅ **AC-12:** `/admin`'s existing behavior (BL-004, all its own ACs) is unaffected by generalizing `RequireAdmin` to accept a child element — regression check. — `RequireAdmin` now takes `{ children }: { children: ReactNode }` and wraps whatever is passed (`AdminPanel` or `RoomAdd`) with identical loading/redirect logic unchanged from BL-004. Actual test run confirms: `tests/AdminPanel.test.tsx (6 tests)` all passed, and `App.test.tsx` still contains and passes the pre-existing `/admin` tests — `"AC-4: an unauthenticated visit to /admin shows the Login screen"`, `"AC-5: an authenticated non-admin visiting /admin ends up back at the Main Menu"`, `"AC-6: an authenticated admin visiting /admin sees the Admin Panel"`, `"FR-5: renders nothing while the admin check is pending"` — all present and part of the passing `10 tests` in `tests/App.test.tsx`.

- ✅ **AC-13:** Submitting with no department selected is prevented client-side; if reached server-side anyway, the server rejects with `400 Bad Request` rather than a raw FK-constraint error. — Client: `canSubmit` requires `departmentId !== ""`, test `"AC-13: leaving no department selected keeps EKLE disabled and never calls createRoom"` passed. Server: `RoomsController.Create`: `if (!await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId)) return BadRequest(new { error = "INVALID_DEPARTMENT", message = "Geçersiz departman." });` — this check runs *before* `_db.Rooms.Add(room)`/`SaveChangesAsync()`, so an invalid/missing department never reaches the FK constraint. Backend test `Create_ReturnsInvalidDepartment_ForUnknownDepartmentId` (departmentId=999999) asserts `HttpStatusCode.BadRequest` + `INVALID_DEPARTMENT`.

## Test Results

Frontend — re-run by the verify agent (`npx vitest run` in `web/`):
```
✓ tests/AppShell.test.tsx (2 tests)
✓ tests/Login.test.tsx (5 tests)
✓ tests/MainMenu.test.tsx (10 tests)
✓ tests/RoomAdd.test.tsx (8 tests)
✓ tests/App.test.tsx (10 tests)
✓ tests/AdminPanel.test.tsx (6 tests)

Test Files  6 passed (6)
     Tests  41 passed (41)
```

Backend — the verify agent's own sandbox had no .NET SDK installed, so it could not independently re-run `dotnet test`, and judged AC-3/5/8/9/10/13's server-side half from code inspection alone. This is closed by evidence already captured earlier in this build/verify pass: `dotnet test` was actually executed during `/specclaw:build finalize`, producing:
```
Passed!  - Failed:     0, Passed:    24, Skipped:     0, Total:    24, Duration: 3 s - InventoryTrackingSystem.Api.Tests.dll (net8.0)
```
and `dotnet build` reported `Build succeeded. 0 Warning(s) 0 Error(s)`. Both are real, already-executed results (not re-derived here), consistent with the code inspection above.

## Issues Found

No issues found. (The verify agent's own environment lacked a .NET SDK to re-run backend tests itself; that gap is closed by the real `dotnet test`/`dotnet build` output already captured during this change's build finalize step, quoted above.)

## Summary

**Passed:** 13/13 criteria
**Failed:** 0/13 criteria
**Verdict:** PASS
