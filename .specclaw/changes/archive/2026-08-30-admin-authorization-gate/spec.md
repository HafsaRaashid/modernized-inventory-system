# Spec: BL-003 — Admin Authorization Gate (Admin button enable/disable)

**Change:** admin-authorization-gate
**Created:** 2026-08-28
**Status:** 🟡 Draft

## Overview

BL-002 shipped the ADMİN button hardcoded `disabled` — the real gate logic was explicitly deferred here. This change implements DR-003: a fresh, server-side authorization check evaluated every time the Main Menu loads, driving whether the ADMİN button is enabled. Because no request is currently authenticated server-side (the JWT is issued at login but nothing validates it on later calls — `Program.cs`'s own comment: "no request is gated on the issued token yet; that enforcement is a future backlog item"), this change also wires up JWT bearer authentication so the backend can identify who is asking.

## Requirements

### Functional Requirements

- **FR-1:** The API validates the `Authorization: Bearer <token>` header on protected endpoints using JWT bearer authentication, configured with the same `Jwt:SigningKey`/`Jwt:Issuer` that `JwtTokenService` already signs tokens with.
- **FR-2:** A new endpoint, `GET /api/auth/me`, protected by `[Authorize]`, resolves the calling user from the JWT's `sub` claim and returns `{ username, isAdmin }`, where `isAdmin` is `true` only when `User.YetkiID == true` (a two-value `bit` column — no third value is ever checked, per DR-003 and the Enumerations section of domain-model.md).
- **FR-3:** On every Main Menu mount (matching legacy `ANA_MENU_Load`'s re-evaluation on every load, not a cached client-side flag), the frontend calls `GET /api/auth/me` and sets the ADMİN button's enabled state from `isAdmin`. The button starts disabled and only becomes enabled once a `true` response arrives.
- **FR-4:** Per CQ-024 (decided): the multi-row-match edge case (more than one `tblKullanicilar` row matching the same username+password with different `YetkiID` values) is unreachable in practice and needs no tie-break rule. The existing `Users.Username` lookup already assumes uniqueness (BL-001's `SingleOrDefaultAsync` by username); this change adds no new handling for it.
- **FR-5:** A request to `GET /api/auth/me` with a missing, malformed, or expired token is rejected with `401 Unauthorized` by the authentication middleware itself (no custom handling needed beyond `[Authorize]`).

### Non-Functional Requirements

- **NFR-1:** ~~No new NuGet package is required~~ — corrected during build: `Microsoft.AspNetCore.Authentication.JwtBearer` does NOT ship in the ASP.NET Core shared framework (it depends on `Microsoft.IdentityModel`/`System.IdentityModel.Tokens.Jwt`, NuGet-only libraries) — confirmed absent from the installed shared runtime and reference pack. `Microsoft.AspNetCore.Api.csproj` adds an explicit `PackageReference` for it, version-pinned to `8.0.8` to match the project's other EF Core package versions.
- **NFR-2:** No new EF Core migration is required — the `Users.YetkiID` (`bit`, nullable) column already exists from BL-001's `AddUserAuthentication` migration.

## Acceptance Criteria

- **AC-1:** A signed-in user whose `YetkiID` is `true` sees the ADMİN button become enabled after Main Menu loads.
- **AC-2:** A signed-in user whose `YetkiID` is `false` or `null` sees the ADMİN button stay disabled after Main Menu loads.
- **AC-3:** `GET /api/auth/me` with a valid token for a `YetkiID = true` user returns `200` with `isAdmin: true`.
- **AC-4:** `GET /api/auth/me` with a valid token for a `YetkiID = false` user returns `200` with `isAdmin: false`.
- **AC-5:** `GET /api/auth/me` with a valid token for a `YetkiID = null` user returns `200` with `isAdmin: false` (fail-closed, matching GM-018's fail-open-on-zero-matching-rows legacy outcome being replaced by a real, closed default per SQ-004's "add real authentication/authorization" — the legacy fail-open default is a defect this rebuild does not reproduce for the *new* token-based check, only the *query* shape is preserved).
- **AC-6:** `GET /api/auth/me` with no `Authorization` header, or an invalid/expired token, returns `401 Unauthorized`.
- **AC-7:** `POST /api/auth/login` (BL-001) is unaffected — it remains anonymous and continues to issue tokens exactly as before.
- **AC-8:** The Main Menu's other four buttons and Sign Out (BL-002/BL-001) keep working exactly as before — this change touches only the ADMİN button's enabled state and its own mount-time check.

## Edge Cases

- **YetkiID is null** (never explicitly set on a migrated row): treated as not-admin (AC-5), the same as `false` — DR-003 only ever checks for the literal `true`.
- **GM-017 (case-sensitive non-match) is not independently fixture-verified** this round — its capture is broken by a pre-existing harness assertion bug unrelated to DR-003 itself; not blocking.
- **Race between Main Menu mount and the `/me` call resolving:** the button stays in its safe default (disabled) until the response arrives — no flash of an incorrectly-enabled button.
- **CQ-024's multi-row scenario:** out of scope per FR-4 — decided unreachable in practice.

## Dependencies

BL-002 (Main Menu Navigation Hub) — **BUILT**, merged to `master`. This change replaces BL-002's hardcoded `disabled` ADMİN button with the real gate; BL-001 (Login/JWT issuance) — **BUILT**, merged `master@3ddd3a9` — this change adds the missing validation side of the token BL-001 already issues.

## Notes

- **CQ-027** (unanswered, non-blocking — DR-004 validation-path duplication) does not apply: DR-003 carries no DR-004 required-field gate.
- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` are still absent (SQ-013 FAITHFUL) — per project decision, screenshots will be captured at the end of the whole backlog; no new layout is introduced here (the ADMİN button already exists from BL-002), only its enabled-state logic.
- **This change also lays the JWT-validation foundation** (FR-1) that later admin-only screens (BL-004 onward, gated behind the Admin Panel) will reuse via `[Authorize]` — a natural, minimally-scoped side effect of being the first item that needs to know "who is calling," not scope creep: without it, DR-003 cannot be implemented as a real server-side check at all.
