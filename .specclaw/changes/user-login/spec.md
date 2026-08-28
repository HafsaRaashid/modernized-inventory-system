# Spec: BL-001 — User Login (Authentication)

**Change:** user-login
**Created:** 2026-08-28
**Status:** 🟡 Draft

## Overview

Replace the legacy `GİRİŞ_EKRANI` login (SQL-injectable string-concatenated query, plaintext password comparison, identity carried via public static fields) with a real Sign In capability: a parameterized, hashed-password credential check that issues a signed session token, backing a Sign In screen that reproduces SCR-001's layout and TK-001's theme. This is the application's entry point — MOD-001, dependency rank 0, no prerequisites.

## Requirements

### Functional Requirements

- **FR-1:** A Sign In screen at `/login` renders two stacked icon+field rows (Username, then masked Password) followed by a single wide primary action button, per SCR-001's recorded layout.
- **FR-2:** `POST /api/auth/login` accepts `{ username, password }` and checks it against a parameterized query comparing a PBKDF2 hash of the supplied password to the stored hash for that username (CQ-010, CQ-011).
- **FR-3:** On a match, the endpoint issues a signed JWT (replacing the legacy static-field identity carrier, SQ-004) and the client stores it for the session; the client then treats itself as authenticated and shows the app shell.
- **FR-4:** On no match (wrong password, unknown username, or an empty field naturally failing to match) the endpoint returns a rejection; the client shows "Hatalı giriş yaptınız. Lütfen tekrar giriniz!!!" and resets both fields to their placeholder text.
- **FR-5:** DR-004 on this screen only: a purely cosmetic, non-blocking indicator appears under a field when it loses focus while still empty. It never gates the submit action and never runs a separate pre-check before the credential request — this screen's own `button1_Click` has no such redundant gate (GM-013), unlike the five other DR-004 screens. An empty field simply flows into the normal credential check and fails the same way as a wrong password (FR-4).
- **FR-6:** Passwords are stored as a salted PBKDF2 hash, never plaintext. A reusable hashing service exists that can hash a new value and verify a supplied password against a stored hash.
- **FR-7:** An unauthenticated client requesting the app shell (`/`) is redirected to `/login`; a successful login navigates to `/`.

### Non-Functional Requirements

- **NFR-1:** No plaintext password is ever logged (the exception-handling middleware logs request method/path only, never the request body).
- **NFR-2:** The JWT signing key is read from configuration (`Jwt:SigningKey`), following the existing `ConnectionStrings:Default` convention of an empty checked-in placeholder filled via `dotnet user-secrets` in development.
- **NFR-3:** The credential-check query is parameterized by construction (EF Core LINQ), never string-concatenated (CQ-010).

## Acceptance Criteria

- **AC-1:** Correct username + password → `200 OK` with a JWT in the response body; matches GM-011.
- **AC-2:** Correct username + wrong password → rejection response; client shows the exact failure message and resets both fields to placeholder text; matches GM-012.
- **AC-3:** Empty username or password → the same rejection path as AC-2, not a distinct "required field" server error — no redundant non-empty gate exists in the submit handler; matches GM-013.
- **AC-4:** Blurring an empty field shows a cosmetic indicator; the indicator alone never prevents a submit attempt; matches GM-014's ErrorProvider-parity intent (display-only, decoupled from the actual gate).
- **AC-5:** `PasswordHasherService.Hash()` followed by `Verify()` round-trips correctly for arbitrary input, and two hashes of the same password produce different output (salted).
- **AC-6:** The issued token is a well-formed, correctly signed JWT (verifiable by decoding it with the same signing key) — not a static field, not an unsigned/opaque string.
- **AC-7:** An unauthenticated visit to `/` redirects to `/login`; a successful login at `/login` navigates to `/` and the app shell shows the signed-in username with a Sign Out action that clears the token and returns to `/login`.

## Edge Cases

- Username exists but the stored hash was produced with different PBKDF2 parameters (future-proofing, not exercised by any current data) — out of scope; the hash format embeds its own iteration count so this degrades gracefully rather than crashing, but no test constructs this case since no such data exists yet.
- Whitespace-only username/password: no legacy behavior trims input before the query (confirmed by GM-011/GM-012/GM-013's own capture); the rebuild does not add trimming either — reproduced as-is, consistent with SQ-012 (faithful-by-default).
- Multiple rapid failed attempts: no lockout/rate-limiting exists in the legacy app and CQ-011/SQ-004 do not ask for one; not built here (would be a new, unrequested capability).

## Dependencies

None (BL-001 has no backlog dependencies — application entry point).

## Notes

- **Open (per proposal, unchanged by this spec):** CQ-027 — whether to consolidate the cosmetic indicator and the actual gate into one validator. This spec preserves the current non-agreeing structure (FR-5) pending that decision.
- **Open (per proposal, unchanged by this spec):** the migration/reset procedure for the 9 existing legacy plaintext-password production accounts is not decided. This change builds the reusable hashing mechanism (FR-6/AC-5) that any such migration will use, but does not itself execute a migration against production data — no admin screen creates or edits `User` rows in this rebuild's scope at all (CQ-012), so account provisioning is entirely a data-migration concern outside this change.
- **UI grounding:** `.specclaw/ui/screens/` and `ui-manifest.json` don't exist yet (SQ-013 FAITHFUL, artifacts missing). TK-001's three colors are OS-theme-relative `SystemColors.*` symbols with no fixed RGB evidenced in source; `design.md` records the concrete CSS values chosen as a stated best-effort approximation, pending a human screenshot sign-off in `ui-review.md`.
- **error-map.md:** `INVALID_LOGIN_CREDENTIALS` exists with `Rebuild source: not yet mapped` — this change fills that in with the real `file:line` once the endpoint is built (see tasks.md T16).
