# Proposal: BL-001 — User Login (Authentication)

**Created:** 2026-08-28
**Status:** 🟡 Draft

## Problem

The legacy application's login screen (`GİRİŞ_EKRANI`) is the entry point to the entire system — nothing else is reachable without it — and it carries three confirmed security defects: the credential check is a string-concatenated, SQL-injectable query (`SELECT COUNT(*) FROM tblKullanicilar WHERE KullaniciAdi='" + kAdi + "' AND Sifre='" + sifre + "'"`), passwords are stored and compared as plain text, and the authenticated identity is carried between forms through public static fields (`GİRİŞ_EKRANI.kAdi, sifre`) rather than any session mechanism. The rebuild's target application has no application at all yet without this screen — it is the first item of MOD-001 (Authentication & Navigation), the module every other module depends on.

## Proposed Solution

Build a real Sign In capability against the ASP.NET Core Web API + React/TypeScript foundation already scaffolded (`/specclaw:bf-bootstrap`):

- **Backend:** a login endpoint that runs a parameterized credential check (per **CQ-010**, decided DEFECT/fix — replacing the concatenated query), compares a hashed/salted password (per **CQ-011**, decided DEFECT/fix), and issues a real session/token on success (per **SQ-004**, decided TARGET-GAP — replacing the static-field identity carrier). The 9 existing production accounts' plaintext passwords are migrated/reset as part of this work, since a plaintext value can't feed a hash comparison directly.
- **Frontend:** a Sign In screen reproducing **SCR-001**'s layout structure and theme tokens (**TK-001**) under the FAITHFUL UI-fidelity policy (**SQ-013**) — a top input block with two stacked icon+field rows (Username, Password/masked), and a single wide accent-coloured primary action button below it. On failure it shows "Hatalı giriş yaptınız..." and resets both fields to their placeholder text, matching legacy behaviour.
- **DR-004 (required-field soft validation):** reproduced exactly as this screen's own scenarios show it — **not** the redundant two-path pattern seen on other DR-004 screens. GM-013 confirms this screen's `button1_Click` has no independent `Trim() != ""` gate alongside the `ErrorProvider` display; the two paths don't even agree here, and the rebuild preserves that as-observed behaviour rather than "fixing" it to match the other five DR-004 screens.

## Scope

### In Scope
- Login form: Username + masked Password fields, matching SCR-001's layout/theme (TK-001)
- Backend endpoint: parameterized credential check against a hashed/salted password (CQ-010, CQ-011)
- Password hashing/salting + one-time migration of the 9 existing plaintext production accounts
- Real session/token issuance on success (SQ-004), retiring the static-field identity carrier
- Failure path: "Hatalı giriş yaptınız..." message, both fields reset to placeholder text
- DR-004 required-field behaviour reproduced exactly as GM-013/GM-014 show it for this screen (no added cross-check)

### Out of Scope
- Main Menu navigation and the Admin authorization gate (BL-002, BL-003) — separate backlog items
- Route guards / authorization enforcement beyond issuing the session/token itself
- Resolving CQ-027 (whether to consolidate the ErrorProvider display and the gating check into one validator) — left open per the backlog's own OPEN QUESTIONS marker
- Full UI sign-off against captured screenshots — `.specclaw/ui/screens/` and `ui-manifest.json` don't exist yet; the layout is built from `ui-inventory.md`'s recorded structure and confirmed visually once a human runs `/specclaw:bf-ui --record`

## Impact

- **Files affected:** ~10-14 (estimated) — new `AuthController`/login endpoint, password hashing service, migration for the 9 existing accounts, session/token issuance in `api/`; a Sign In route/component and API client call in `web/`
- **Complexity:** medium
- **Risk:** medium — security-sensitive (credential handling, one-time production password migration), and the exact hashed-login/session fixtures are new (no legacy fixture can validate the new mechanism itself, per the backlog's own verification-inputs note)

## Open Questions

- **CQ-027** (unanswered, non-blocking): consolidate the ErrorProvider display and the actual gating check into one validator, or preserve the observed non-agreeing structure? This proposal preserves the observed structure (see Scope, In Scope) pending that decision.
- **UI grounding missing:** SQ-013 (FAITHFUL) is decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent. The layout is built from `ui-inventory.md`'s prose description; a named human must sign off `ui-review.md` against a real screenshot once captured.
- **Migration/reset procedure** for the 9 existing plaintext-password production accounts is not yet defined — needed before any hashed-login fixture can be captured for them.

---

**To proceed:** Review this proposal and approve to begin planning.
