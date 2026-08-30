# Design: BL-003 — Admin Authorization Gate (Admin button enable/disable)

**Change:** admin-authorization-gate
**Created:** 2026-08-28

## Technical Approach

Two halves, both small:

1. **Backend authentication boundary.** `Program.cs` currently registers `JwtTokenService` (issuance) but no validation — `[Authorize]` has never been usable. Add `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` reading the same `Jwt:SigningKey`/`Jwt:Issuer` configuration keys `JwtTokenService` already reads, plus `app.UseAuthentication()` / `app.UseAuthorization()` in the middleware pipeline (before `MapControllers()`).
2. **`GET /api/auth/me`.** A new `[Authorize]` action on `AuthController` reads the `sub` claim (the username `JwtTokenService.IssueToken` puts there), looks the user up the same way `Login` already does (`SingleOrDefaultAsync` by `Username`), and returns `{ username, isAdmin: user.YetkiID == true }`.
3. **Frontend.** `MainMenu` calls this endpoint once on mount (a `useEffect`), starts with the ADMİN button disabled, and enables it only on `isAdmin: true`. This matches the legacy re-check-per-load semantics without a persisted, cacheable role flag anywhere in the session.

## Architecture

```
MainMenu (mount)
  → GET /api/auth/me  (Authorization: Bearer <token>, attached automatically by apiFetch)
      → [Authorize] middleware validates the JWT signature/expiry
      → AuthController.Me() resolves User by sub claim, reads YetkiID
      ← { username, isAdmin }
  → setAdminEnabled(isAdmin)
```

No new persistence, no new migration — `Users.YetkiID` already exists (BL-001's `AddUserAuthentication` migration). No change to the login flow itself.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `api/src/InventoryTrackingSystem.Api/Program.cs` | Modify | Register JWT bearer authentication (`AddAuthentication`/`AddJwtBearer`) and `UseAuthentication()`/`UseAuthorization()` in the pipeline |
| `api/src/InventoryTrackingSystem.Api/InventoryTrackingSystem.Api.csproj` | Modify | Add explicit `PackageReference` for `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.8) — it is not part of the shared framework, corrected during build (see NFR-1) |
| `api/src/InventoryTrackingSystem.Api/Controllers/AuthController.cs` | Modify | Add `[Authorize] GET /api/auth/me` returning `{ username, isAdmin }` |
| `api/tests/InventoryTrackingSystem.Api.Tests/AuthControllerTests.cs` | Modify | Add integration tests for `/me`: admin user, non-admin user, null `YetkiID` user, missing/invalid token |
| `web/src/api/auth.ts` | Modify | Add a typed `getSession()` call to `GET /api/auth/me` |
| `web/src/routes/MainMenu.tsx` | Modify | Fetch session on mount; drive ADMİN button's `disabled` from `isAdmin` instead of a hardcoded `disabled` |
| `web/tests/MainMenu.test.tsx` | Modify | Replace the old "always disabled" assertion with admin/non-admin/loading-state cases, mocking `getSession()` |

## Data Model Changes

None. `Users.YetkiID` (`bit`, nullable) already exists.

## API Changes

**New:** `GET /api/auth/me`
- Auth: `[Authorize]` (JWT bearer)
- 200 response: `{ "username": string, "isAdmin": boolean }`
- 401: no/invalid/expired token (handled entirely by the authentication middleware, no custom body)

**Unchanged:** `POST /api/auth/login` stays anonymous.

## Key Decisions

- **Fail-closed on ambiguous `YetkiID`.** `null` maps to `isAdmin: false`, matching the "only the literal `true` enables" rule from DR-003 and the legacy code path. This is a deliberate divergence from GM-018's legacy fail-*open* behavior (a broken/absent-row query result left the button enabled) — SQ-004 already decided the rebuild adds *real* authorization, and a fail-open admin gate would be a straightforward privilege-escalation bug for a check that is being rebuilt from scratch, not reproduced from a query-string quirk.
- **Re-check every mount, no caching.** Matches the legacy `ANA_MENU_Load` semantics exactly (re-run on every load) and avoids inventing a persisted role claim that could go stale mid-session if `YetkiID` changes — there is no admin CRUD for `User` in scope (CQ-012) to change it anyway, but the re-check pattern is what the capability text specifies.
- **No new package for JWT validation.** `Microsoft.AspNetCore.Authentication.JwtBearer` ships as part of the ASP.NET Core shared framework for `Microsoft.NET.Sdk.Web` projects; only a `using` is needed, no `PackageReference`.
- **CQ-024's multi-row edge case:** no tie-break logic added (decided unreachable in practice — no evidence of duplicate username+password pairs in production data).

## Risks & Mitigations

- **Risk:** Registering authentication/authorization middleware for the first time could inadvertently start gating existing anonymous endpoints (`/api/auth/login`, `/health`). **Mitigation:** ASP.NET Core only enforces `[Authorize]` on decorated actions — `AddAuthentication`/`UseAuthentication` alone does not lock down unannotated endpoints. Verified by AC-7 (login stays anonymous) and a smoke check that `/health` remains reachable without a token.
- **Risk:** Test-suite JWT signing key/issuer must match between token issuance and the new validation parameters, or every `/me` test fails regardless of logic. **Mitigation:** reuse the exact same `AuthControllerTests` pattern already used for AC-6 (`TestSigningKey`/`TestIssuer` injected via `ConfigureAppConfiguration`), so both issuance and validation share one source of truth per test.
