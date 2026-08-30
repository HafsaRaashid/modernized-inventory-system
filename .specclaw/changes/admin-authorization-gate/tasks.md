# Tasks: BL-003 — Admin Authorization Gate (Admin button enable/disable)

**Change:** admin-authorization-gate
**Created:** 2026-08-28
**Total Tasks:** 4

## Summary

2 waves. Wave 1 builds the backend auth boundary + `/me` endpoint and the frontend session-fetch wiring independently (they share only an HTTP contract, not code). Wave 2 is tests for both, run after their respective implementation tasks land.

## Tasks

### Wave 1 — Independent pieces

- [x] `T1` — JWT bearer authentication + `GET /api/auth/me`
  - Files: api/src/InventoryTrackingSystem.Api/Program.cs, api/src/InventoryTrackingSystem.Api/Controllers/AuthController.cs
  - Estimate: medium
  - Kind: impl
  - Notes: In Program.cs, add `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { ... })` reading `Jwt:SigningKey`/`Jwt:Issuer` from configuration (same keys `JwtTokenService` already reads) into `TokenValidationParameters` (`ValidateIssuerSigningKey: true`, `ValidateIssuer` true only if an issuer is configured, `ValidateAudience: false` — no audience is issued), then `app.UseAuthentication()` and `app.UseAuthorization()` before `app.MapControllers()`. In AuthController, add `[Authorize] [HttpGet("me")] public async Task<IActionResult> Me()` that reads `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` (or `ClaimTypes.NameIdentifier` — check which claim type ASP.NET Core's default JWT handler maps `sub` to; explicit `MapInboundClaims = false` on the JwtBearerOptions keeps the raw `sub` claim name accessible via `ClaimTypes.NameIdentifier` mapping avoided), looks up the `User` by that username the same way `Login` does, and returns `{ username, isAdmin = user.YetkiID == true }`. No new NuGet package needed (FR-1/FR-2, NFR-1).

- [x] `T2` — Wire the admin gate into MainMenu
  - Files: web/src/api/auth.ts, web/src/routes/MainMenu.tsx
  - Estimate: medium
  - Kind: impl
  - Notes: Add `getSession(): Promise<{ username: string; isAdmin: boolean }>` in auth.ts calling `apiFetch("/auth/me")` (GET). In MainMenu, add a `useEffect` on mount that calls `getSession()` and sets `isAdmin` state (initial `false`); render the ADMİN button's `disabled` as `!isAdmin` instead of the hardcoded `disabled` literal from BL-002. Swallow/ignore a rejected `getSession()` call by leaving `isAdmin` at its safe default `false` (no error UI needed — FR-3, AC-1/AC-2).

### Wave 2 — Tests

- [x] `T3` — Backend `/me` endpoint tests
  - Files: api/tests/InventoryTrackingSystem.Api.Tests/AuthControllerTests.cs
  - Estimate: medium
  - Kind: test
  - Depends: T1
  - Notes: Reuse the existing `CreateFactory`/`SeedKnownUserAsync`-style helpers (extend the seed helper to accept an optional `YetkiID`). Cover: AC-3 (`YetkiID = true` → 200, `isAdmin: true`), AC-4 (`YetkiID = false` → 200, `isAdmin: false`), AC-5 (`YetkiID = null` → 200, `isAdmin: false`), AC-6 (no `Authorization` header → 401; a garbage/expired token → 401), AC-7 (`/api/auth/login` still succeeds anonymously in the same test factory, unaffected by the new authentication registration). To get a valid bearer token for the positive cases, call `POST /api/auth/login` first in the test and reuse its returned token, exactly as a real client would.

- [x] `T4` — Frontend MainMenu admin-gate tests
  - Files: web/tests/MainMenu.test.tsx
  - Estimate: medium
  - Kind: test
  - Depends: T2
  - Notes: Mock `getSession()` (from `../src/api/auth`) with `vi.mock`. Replace the old "AC-4: the ADMİN button is disabled and clicking it does not navigate" test (which assumed a permanently-disabled button) with: (a) ADMİN starts disabled before the mocked promise resolves, (b) ADMİN becomes enabled and clickable after `getSession()` resolves with `isAdmin: true` (await the effect via `findByRole`/`waitFor`), (c) ADMİN stays disabled after `getSession()` resolves with `isAdmin: false`, (d) ADMİN stays disabled (not thrown/crashed) if `getSession()` rejects. Also re-run the full `npx vitest run` suite from `web/` to confirm AC-8 (the other four buttons + Sign Out) is unaffected.

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
