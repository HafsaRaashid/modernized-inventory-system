# Proposal: BL-003 — Admin Authorization Gate (Admin button enable/disable)

**Created:** 2026-08-28
**Status:** 🟢 Approved (auto-approved per user's "run specclaw from proposal to verify for all rebuild-backlog.md" instruction, 2026-08-28)

## Problem

The Main Menu (BL-002) ships the ADMİN button permanently disabled — the real gate logic was explicitly deferred to this item. In the legacy app, `frmAnaMenu.cs`'s `ANA_MENU_Load` re-evaluates authorization on every Main Menu load with a fresh query (`SELECT YetkiID FROM tblKullanicilar WHERE KullaniciAdi=... AND Sifre=...`, using the static username/password captured at login) and enables the button only when the result is the literal value `True`. Without this, the Admin Panel (BL-004) and everything beneath it stays permanently unreachable.

## Proposed Solution

Implement DR-003's admin authorization gate sized to the target platform (per SQ-004), not as a literal SQL string comparison:

- On Main Menu load, the frontend calls a real backend authorization check (e.g. a role/claim already embedded in the JWT issued at login, or a dedicated `GET /api/auth/is-admin` endpoint) instead of re-running credential-bound SQL.
- `User.YetkiID` (a `bit` column — confirmed during baseline harness work, not a string) drives the check: enabled only when true.
- The ADMİN button toggles enabled/disabled based on this result, replacing BL-002's hardcoded `disabled` default.
- Per CQ-024 (decided): the multi-row-match edge case (more than one `tblKullanicilar` row matching the same username+password with different `YetkiID` values) is treated as unreachable in practice — no tie-break rule is implemented.

## Scope

### In Scope
- Backend authorization check exposing the current user's `YetkiID`/admin status (via JWT claim or endpoint), evaluated fresh on every Main Menu load — not cached client-side across the session
- Frontend: Main Menu's ADMİN button reflects the real enabled/disabled state instead of BL-002's static disabled default
- Migration of the existing `YetkiID` bit column semantics (true → enabled, false → disabled, no third value)

### Out of Scope
- Admin Panel's own sub-navigation (BL-004)
- A tie-break rule for the multi-row-match edge case — CQ-024 decided this is unreachable in practice
- The GM-017 (case-sensitive non-match) fixture — its capture is broken due to a pre-existing harness assertion bug, unrelated to DR-003 itself

## Impact

- **Files affected:** ~4-5 (estimated) — a backend claim/endpoint for admin status, a frontend hook/check in the Main Menu component, tests on both sides
- **Complexity:** small
- **Risk:** low — single boolean gate, dependency (BL-002) already built and merged, fixtures exist for the core behavior

## Open Questions

- **CQ-027** (unanswered, non-blocking, DR-004 validation-path duplication) does not apply to this item — DR-003 carries no DR-004 required-field gate.
- **UI grounding missing:** SQ-013 (FAITHFUL) is decided but `.specclaw/ui/screens/` and `ui-manifest.json` are absent — per project decision, screenshots will be captured at the end of the backlog; this item's ADMİN button styling is built from `ui-inventory.md`'s recorded structure (SCR-002) pending that later sign-off, consistent with how BL-001/BL-002 already shipped.
- **GM-017 capture is broken** (harness assertion bug, not a DR-003 defect) — the case-sensitive non-match boundary is not independently fixture-verified this round.

---

**To proceed:** Review this proposal and approve to begin planning.
