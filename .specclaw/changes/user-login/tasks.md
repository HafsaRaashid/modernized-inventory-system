# Tasks: BL-001 — User Login (Authentication)

**Change:** user-login
**Created:** 2026-08-28
**Total Tasks:** 19

## Summary

4 waves. Wave 1 lays independent backend/frontend primitives in parallel. Wave 2 wires persistence and the endpoint, plus the frontend API/auth plumbing. Wave 3 integrates the frontend routes and closes the error-map citation. Wave 4 is tests.

## Tasks

### Wave 1 — Independent primitives

- [x] `T1` — Add `User` domain entity
  - Files: api/src/InventoryTrackingSystem.Domain/Entities/User.cs
  - Estimate: small
  - Kind: impl
  - Notes: `Id`, `Username`, `PasswordHash`, `YetkiID` (nullable bit, unread by this change). See design.md Data Model Changes.

- [x] `T2` — Add `PasswordHasherService` (PBKDF2)
  - Files: api/src/InventoryTrackingSystem.Infrastructure/Auth/PasswordHasherService.cs
  - Estimate: medium
  - Kind: impl
  - Notes: `Rfc2898DeriveBytes`, HMACSHA256, 600,000 iterations, 128-bit random salt. Format: `"{iterations}.{saltBase64}.{hashBase64}"`. `Hash(password)` and `Verify(password, stored)`. No new NuGet package (BCL only).

- [x] `T3` — Add `JwtTokenService`
  - Files: api/src/InventoryTrackingSystem.Infrastructure/Auth/JwtTokenService.cs, api/src/InventoryTrackingSystem.Infrastructure/InventoryTrackingSystem.Infrastructure.csproj
  - Estimate: medium
  - Kind: impl
  - Notes: Add `System.IdentityModel.Tokens.Jwt` package reference (explicit, not transitive). `IssueToken(username)` reads `Jwt:SigningKey`/`Jwt:Issuer` from `IConfiguration`, throws at construction if the key is missing or < 32 bytes (design.md Risks).

- [x] `T4` — Add Login screen (SCR-001 layout, unwired)
  - Files: web/src/routes/Login.tsx, web/src/routes/Login.css
  - Estimate: medium
  - Kind: impl
  - Notes: Two stacked icon+field rows (Username, masked Password) then one wide primary button, per ui-inventory.md SCR-001. `Login.css` declares TK-001's three colors as CSS custom properties scoped to this screen (see design.md Key Decisions for the stated approximate values). No submit handler yet — that's T15.

- [x] `T5` — Add `AuthContext`
  - Files: web/src/auth/AuthContext.tsx
  - Estimate: small
  - Kind: impl
  - Notes: React context holding `token`/`username`; `login(token, username)` and `logout()` persist to/clear `sessionStorage`; reads back from `sessionStorage` on mount.

### Wave 2 — Persistence, endpoint, frontend API plumbing

- [x] `T6` — Wire `User` into `AppDbContext`
  - Files: api/src/InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs
  - Estimate: small
  - Kind: impl
  - Depends: T1
  - Notes: `DbSet<User> Users`; `OnModelCreating` unique index on `Username`.

- [x] `T7` — Generate EF Core migration `AddUserAuthentication`
  - Files: api/src/InventoryTrackingSystem.Infrastructure/Migrations/
  - Estimate: small
  - Kind: migration
  - Depends: T6
  - Notes: `dotnet ef migrations add AddUserAuthentication --project src/InventoryTrackingSystem.Infrastructure --startup-project src/InventoryTrackingSystem.Api -o Migrations` from `api/`. Requires the .NET 8 SDK and the `dotnet-ef` local tool (`dotnet tool restore`) on PATH.

- [x] `T8` — Add `AuthController`
  - Files: api/src/InventoryTrackingSystem.Api/Controllers/AuthController.cs
  - Estimate: medium
  - Kind: impl
  - Depends: T1, T2, T3, T6
  - Notes: `POST /api/auth/login`. Parameterized EF Core lookup by `Username`, `PasswordHasherService.Verify`, on match `JwtTokenService.IssueToken`. On no match (including empty username/password — FR-5/AC-3): 401 `{ "error": "INVALID_LOGIN_CREDENTIALS", "message": "Hatalı giriş yaptınız. Lütfen tekrar giriniz!!!" }`. No separate non-empty pre-check before the lookup.

- [x] `T9` — DI + config wiring
  - Files: api/src/InventoryTrackingSystem.Api/Program.cs, api/src/InventoryTrackingSystem.Api/appsettings.json
  - Estimate: small
  - Kind: config
  - Depends: T2, T3
  - Notes: Register `PasswordHasherService`/`JwtTokenService`; add empty `Jwt:SigningKey`/`Jwt:Issuer` keys to `appsettings.json` (filled via `dotnet user-secrets` locally, same convention as `ConnectionStrings:Default`).

- [x] `T10` — Add `auth.ts` API module
  - Files: web/src/api/auth.ts
  - Estimate: small
  - Kind: impl
  - Notes: `login(username, password)` calling `apiFetch<{ token: string; username: string }>("/auth/login", { method: "POST", body: { username, password } })`.

- [x] `T11` — Wire `client.ts`'s `getAuthHeader()`
  - Files: web/src/api/client.ts
  - Estimate: small
  - Kind: impl
  - Depends: T5
  - Notes: Reads the token from `AuthContext`'s storage (`sessionStorage`) instead of returning `{}`. Since `client.ts` has no React context access, read directly from `sessionStorage` by the same key `AuthContext` uses (documented constant, not duplicated logic).

### Wave 3 — Frontend integration

- [x] `T12` — Wire Login submit handler
  - Files: web/src/routes/Login.tsx
  - Estimate: medium
  - Kind: impl
  - Depends: T4, T5, T10
  - Notes: On submit, call `auth.ts`'s `login()`. Success → `AuthContext.login()` then navigate to `/`. Failure → show "Hatalı giriş yaptınız. Lütfen tekrar giriniz!!!" and reset both fields to placeholder text (AC-2). `onBlur` on each field: cosmetic indicator only if the field is empty, never disables the submit button (AC-4/FR-5).

- [x] `T13` — Add `/login` route and auth-gate `/`
  - Files: web/src/App.tsx
  - Estimate: small
  - Kind: impl
  - Depends: T5
  - Notes: Add `<Route path="/login" element={<Login />} />`. Wrap `/`'s `AppShell` element so an unauthenticated visit redirects to `/login` (AC-7).

- [x] `T14` — Show authenticated state in `AppShell`
  - Files: web/src/routes/AppShell.tsx
  - Estimate: small
  - Kind: impl
  - Depends: T5
  - Notes: Replace the "Foundation scaffold — no screen-bearing capability has been built yet" placeholder with "Signed in as `{username}`" and a Sign Out button calling `AuthContext.logout()` then navigating to `/login` (AC-7).

- [x] `T15` — Fill in `error-map.md`'s `INVALID_LOGIN_CREDENTIALS` rebuild source
  - Files: .specclaw/baseline/error-map.md
  - Estimate: small
  - Kind: docs
  - Depends: T8
  - Notes: Replace "Rebuild source: not yet mapped" with `AuthController`'s real `file:line` for the 401 branch.

### Wave 4 — Tests

- [x] `T16` — `PasswordHasherService` unit tests
  - Files: api/tests/InventoryTrackingSystem.Api.Tests/PasswordHasherServiceTests.cs
  - Estimate: small
  - Kind: test
  - Depends: T2
  - Notes: AC-5 — hash/verify round-trip; two hashes of the same input differ (salted).

- [x] `T17` — `AuthController` integration tests
  - Files: api/tests/InventoryTrackingSystem.Api.Tests/AuthControllerTests.cs
  - Estimate: medium
  - Kind: test
  - Depends: T8, T9
  - Notes: AC-1 through AC-3, AC-6, via `WebApplicationFactory<Program>` against an EF Core in-memory/SQLite provider seeded with one known hashed user. Covers: correct credentials → 200 + well-formed signed JWT; wrong password → 401 with the exact message; empty username/password → the same 401 path, not a distinct error.

- [x] `T18` — Login component tests
  - Files: web/tests/Login.test.tsx
  - Estimate: medium
  - Kind: test
  - Depends: T12, T13, T14
  - Notes: AC-2 (failure message + field reset), AC-4 (blur-empty shows the cosmetic indicator, submit still reachable), AC-7 (successful login navigates and shows the signed-in state).

- [x] `T19` — Auth redirect test
  - Files: web/tests/App.test.tsx
  - Estimate: small
  - Kind: test
  - Depends: T13
  - Notes: AC-7's other half — an unauthenticated visit to `/` renders `Login`, not `AppShell`. (Extends the existing App.test.tsx from the foundation scaffold, doesn't replace it.)

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed
