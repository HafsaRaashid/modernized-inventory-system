# Rebuild Backlog: InventoryTrackingSystem

**Path analyzed:** .
**Date generated:** 2026-08-28
**Source documents:** codebase-report.md, architecture.md, domain-model.md, functional-spec.md, module-map.md

<!--
  NOTE ON THIS COMMENT: never write a literal double-brace placeholder
  token inside this comment's own prose (not even to describe it) — the
  render step's template substitution is a dumb global string replace, and
  a token mentioned here would get overwritten by that token's rendered
  value along with the real placeholder below, corrupting this comment.
  Refer to placeholders by section name instead (e.g. "the status block
  below", "the Backlog section").

  The status block right after this comment is bash-computed, never
  agent-drafted — date, which optional inputs (decisions.md,
  clarifications.md, baseline/manifest.json, baseline/scenarios.md) were
  consumed vs. missing (with the command that produces each),
  Gate/Verification counts, and the single recommended next item to
  propose. This block, and every item's Gate:/Verification: field below,
  is recomputed from scratch on every run — never hand-maintained.

  MODULE GROUPING. The Backlog section below is two levels deep: one
  "## MOD-### — <Module Name>" heading per module from module-map.md, with
  that module's "### BL-0##" items beneath it. Modules are ordered by their
  own dependency rank from the map (foundations first), computed in bash by
  the same fixed-point pass that ranks items; a declared cycle is reported
  and no rank number is printed, because the number would be an artifact of
  the iteration cap rather than a dependency depth.

  A module is a MIGRATION/ACCEPTANCE unit — the "one flow at a time" slice a
  large legacy system is rebuilt and signed off in. BL items remain the
  BUILD units: the hierarchy is MOD-### -> BL-0## -> DR-### -> GM-###, and a
  module is NEVER collapsed into one giant BL item. Item granularity rules
  are exactly as they were — a module only groups items that already exist
  at capability-bullet granularity.

  Item order WITHIN a module is unchanged from before modules existed
  (dependency rank first — a hard constraint — then within the same rank:
  CLEAR+VERIFIABLE, then CLEAR+PENDING CAPTURE/NO BASELINE DATA/
  UNVERIFIABLE, then OPEN QUESTIONS, then BLOCKED). Two further top-level
  groups may appear after the modules: "## Unassigned — no module declared"
  (items with no **Module:** field, or one naming a MOD-### the map does not
  define — never folded into a real module by guesswork) and "## Struck"
  (tombstones, which belong to no module).

  Expected per-item sub-structure inside each module group — one entry per
  backlog item:

  ### BL-NNN — <Feature Title>

  **Module:** <the MOD-### from module-map.md this item belongs to. Declared
    by the planner agent, read mechanically by bash, and NEVER derived from
    the item's DR-### rules — deriving one would be a silent assignment, and
    a disagreement between this field and the map's own rule ownership is
    exactly what /specclaw:bf-baseline record reports as a WARN at record
    time. An item with no such field is rendered under "## Unassigned",
    never guessed into a module.>
  **Maps to capability:** <functional-spec.md capability name/quote>
  **Depends on:** <earlier items' BL-NNN IDs, or "None">
  **Acceptance basis (domain-model.md):**
  - <entity/business-rule/enumeration reference, quoted — cite a business
    rule's DR-NNN ID (from domain-model.md) directly wherever the
    acceptance basis rests on a numbered rule, e.g. "DR-007: ..."; this is
    the join key /specclaw:bf-clarify and /specclaw:bf-baseline key their own
    CQ-NNN/GM-NNN citations against, so the ID itself must be textually
    present, not just implied by the quoted prose>

  **Verification inputs needed:**
  - <golden-master capture, external-format/DLL/COM semantics, or other
    human-supplied input this item's fidelity check will need — never
    leave this field blank; if genuinely nothing beyond the acceptance
    criteria above applies, say so explicitly rather than omitting it>

  **Gate:** <bash-computed: BLOCKED — blocked by <CQ-NNN + one-line title,
    ...> | OPEN QUESTIONS — risk from unanswered, non-blocking: <CQ-NNN,
    ...> | CLEAR>
  **Verification:** <bash-computed: VERIFIABLE — fixtures: <GM-NNN (legacy
    commit sha), ...> | PENDING CAPTURE — scenarios designed, no recorded
    fixture yet: <GM-NNN, ...> | UNVERIFIABLE — acceptance must come from a
    stakeholder decision, not fixture comparison (see CQ-NNN) | NO BASELINE
    DATA — baseline not run (or not designed) for these rules>
  **UI fidelity:** <bash-computed, and present ONLY when this item renders a
    screen AND the UI fidelity policy (SQ-013, read mechanically from
    decisions.md) is decided FAITHFUL/THEME-ONLY or is undecided. Renders as:
    FAITHFUL — reproduce the layout structure and token values of: <SCR-###,
    ...>; token groups: <TK-###, ...> | THEME-ONLY — reproduce the token
    values of: <TK-###, ...>; screens for reference only: <SCR-###, ...> |
    ⚠ UI GROUNDING MISSING — <the decided policy, plus which .specclaw/ui/
    artifacts are absent, or the fact that this item cites no SCR-### at all>
    | UNDECIDED — <SQ-013 has no recorded decision>. The last two also
    contribute an OPEN QUESTIONS state to the Gate line above, naming SQ-013.
    Under a decided REINTERPRET policy this field never appears on any item
    and no warning is emitted anywhere — the zero-extra-work path for a
    project that does not need visual fidelity. Which items render a screen
    is the planner agent's judgment, delivered as a SCREEN-BEARING: directive
    and applied mechanically here; SCR-###/TK-### content itself belongs to
    /specclaw:bf-ui, never to this document. A cited SCR-### never implies
    visual equivalence has been proven — that is established by a named human
    signing ui-review.md against recorded screenshots, never by this backlog
    and never by fixture replay.>
  **Settled constraints (from decisions):** <optional — only present when a
    mechanical-adopt decision applies to this item; omit the field entirely
    otherwise, never render it empty>

  **Status notes (human-added):** <optional — anything a human types under
    this exact heading (e.g. "built and merged, PR #12") survives every
    future /specclaw:bf-rebuild-plan --refresh verbatim, byte for byte. Nothing
    else in this document offers that guarantee — this is the one place a
    human note is safe to leave.>

  If two or more functional-spec capabilities are merged into a single
  backlog item, the item must state why in a "Merge rationale:" line —
  merging is a judgment call, never silent. A revised item (its acceptance
  basis rewritten because a decision changed its shape) states so inline,
  e.g. a line reading "⟲ revised per CQ-005, 2026-08-01" placed right after
  the heading.

  PROVISIONAL marker: an item touched by an open pending question — either
  a direct DR-NNN/BL-NNN join to a CQ-NNN promoted from a PQ-NNN (bash-
  computed), or a prose-level match the planner agent found and directed
  via a PROVISIONAL: line (agent-judged, mechanically re-verified by bash
  the same way an UNVERIFIABLE: directive is) — carries its own line right
  after the heading: "⚠ PROVISIONAL — pending PQ-NNN/CQ-NNN (proposed
  default: <x>)". This is soft-block: the item is still fully drafted,
  sequenced, and gated/verified exactly as any other; the marker rides
  alongside Gate/Verification, not instead of them, and both this line and
  Gate/Verification are recomputed from scratch on every run — it clears
  automatically once decisions.md answers the underlying question, no
  manual cleanup.

  STUB-BACKED marker: an item built against a dependency-bypass stub (see
  templates/CONTRACT.md (m) and .specclaw/analysis/module-stubs.md) carries
  its own line right after the heading, alongside any PROVISIONAL marker:
  "⚠ STUB-BACKED — built against ST-001 (stub-interface, faking BL-014
  (MOD-005)). Any replay verdict for this item says so until the stub is
  retired."

  It is deliberately NOT folded into the Verification: line. Verification
  answers "is there a fixture for this?"; taint answers "was the thing under
  test real?" — orthogonal axes, and collapsing them would let a
  VERIFIABLE item read as fully proven when part of what it was checked
  against was a placeholder. Like PROVISIONAL, it is recomputed from the
  registry on every run and never persisted, so retiring a stub clears every
  consuming item's marker automatically with no manual cleanup.

  A stub is only ever created by a human choosing one at /specclaw:propose
  time. Nothing in this document creates, edits, or retires one.

  BL-NNN IDs are permanent identifiers, not position — assigned once in
  dependency order on the first-ever run and never renumbered afterward.
  A later /specclaw:bf-rebuild-plan --refresh may append a genuinely new item
  (next free BL-NNN, dependency-placed correctly) or strike/defer an
  existing one, but an already-assigned ID is never reused, renumbered, or
  silently deleted — a struck item stays in the Backlog section as a
  one-line tombstone ("### BL-NNN — STRUCK — <reason>, <date>"); a deferred
  item moves in full to the Deferred section, out of the ready ordering.
  "Depends on:" always cites BL-NNN IDs, never bare position, for exactly
  this reason.
-->

**Date:** 2026-08-28
**Inputs consumed:**
- decisions.md: present
- clarifications.md: present
- baseline/manifest.json: present
- baseline/scenarios.md: present

**Module map:** CONFIRMED by Hafsa, 28-8-2026 — 5 active module(s)

**Recommended next module to build:** MOD-001 — Authentication & Navigation
- **Why:** dependency rank 0; depends on none; 4 active item(s) — 0 CLEAR, 4 OPEN QUESTIONS, 0 BLOCKED, 0 PROVISIONAL; every module it depends on is likewise free of BLOCKED and PROVISIONAL items.
- **Readiness, not completion:** specclaw records no "built" state for a backlog item, so this is the next module whose work can *start*, not a claim that anything it depends on is finished.

**UI fidelity policy:** FAITHFUL (SQ-013)
- .specclaw/ui/ui-inventory.md: present
- .specclaw/ui/design-tokens.json: present
- .specclaw/ui/screens/: missing — a human must capture screenshots per screenshot-checklist.md
- .specclaw/ui/ui-manifest.json: missing — run /specclaw:bf-ui --record
- Screen-bearing items: 15, of which 15 lack UI grounding

> ⚠ **WARNING — UI fidelity policy FAITHFUL is decided, but the artifacts it requires do not exist:** .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json. Every screen-bearing item above is held at OPEN QUESTIONS as a result. Run `/specclaw:bf-ui` (and `--record`, after a human captures the screenshots) — this backlog cannot state a UI acceptance basis without them, and it will not pretend to.

**Gate counts:** CLEAR: 0, OPEN QUESTIONS: 15, BLOCKED: 0 (of 15 active items; 0 struck, 0 deferred)
**Verification counts:** VERIFIABLE: 9, PENDING CAPTURE: 0, UNVERIFIABLE: 0, NO BASELINE DATA: 6
**Provisional (pending a decision):** 0 item(s) — independent of Gate/Verification; see each item's own marker

**Recommended next item to propose:** None — every item is BLOCKED or no active items remain.

## Backlog

## MOD-001 — Authentication & Navigation

_Depends on: none. Module dependency rank 0. 4 active item(s)._

### BL-001 — User Login (Authentication)



**Module:** MOD-001
**Maps to capability:** "Log in with username and password — fields: Username (`txtUsername`, single-line TextBox, pre-filled placeholder text "KULLANICI ADI" cleared on click), Password (`txtPassword`, single-line TextBox with `PasswordChar='*'` — masked/password input). On success navigates to Main Menu; on failure shows "Hatalı giriş yaptınız..." and resets both fields to their placeholder text. Two decorative `PictureBox` icons (`pbGirisEkraniUser`, `pbGirisEkraniPass`) accompany the fields but capture no data. Non-empty validation is DR-004 (soft/cosmetic — see domain-model.md)." (functional-spec.md, Authentication) — plus the "Login (branches on credential validity)" workflow: "`GİRİŞ_EKRANI.button1_Click` reads `txtUsername`/`txtPassword`, runs `SELECT COUNT(*) FROM tblKullanicilar WHERE KullaniciAdi=... AND Sifre=...`, and branches on the result... the authenticated identity is carried between these two forms entirely through `GİRİŞ_EKRANI`'s `public static string kAdi, sifre;` fields, not a typed session object."
**Depends on:** None
**Acceptance basis (domain-model.md):**
- Entity User (`tblKullanicilar`): "Fields observed: `KullaniciAdi` (username), `Sifre` (password, stored/compared as plain text), `YetkiID` (authorization flag)."
- DR-004 — Required-field soft validation (cross-cutting): "each sets an `ErrorProvider` icon/message on a designated field ... when that field is empty. Mechanical: this display is cosmetic only — every one of these handlers separately re-checks `Text.Trim() != ""` in its own `if` before proceeding, so the `ErrorProvider` and the actual gating condition are two independent code paths that happen to agree; a rebuild reproducing only one of the two would not reproduce this rule." Login is one of DR-004's six cited screens, and its own scenario (GM-013) shows this screen's own `button1_Click` has **no** such redundant gate at all — the two paths do not even agree here.
- Per CQ-010 (decided DEFECT/fix): "parameterize every query path from the start" — replaces the SQL-injection-vulnerable concatenated login query (`"SELECT COUNT(*) FROM tblKullanicilar WHERE KullaniciAdi='" + kAdi+ "' AND Sifre='" + sifre + "'"`).
- Per CQ-011 (decided DEFECT/fix): "hash and salt passwords; migrate/reset the 9 existing accounts since their plaintext values can't feed a hash comparison directly."
- Per SQ-004 (decided TARGET-GAP): "Add real authentication/authorization, sized to the target platform (hashed/salted credentials, proper session/token handling — replacing the legacy plaintext/SQL-injectable login and `YetkiID` flag model)." This retires the static-field identity-carrying mechanism quoted above in favor of a real session/token.
- SCR-001 (Login) layout structure, per ui-inventory.md (FAITHFUL policy): "A top input block containing two stacked icon+field rows... Below the input block, a single wide accent-colored primary action button spans roughly the same horizontal extent as the fields above it." Token group TK-001 (login-button-background/foreground, login-form-background — all `SystemColors.*` symbolic values, per design-tokens.json).
- Open question: CQ-027 (unanswered DEFECT, DR-004) — whether to consolidate the `ErrorProvider` display and the actual gating check into one validator, or preserve the duplicated (here: non-agreeing) structure.

**Verification inputs needed:**
- GM-011 (success), GM-012 (failure), GM-013 (empty fields not gated before the query runs), GM-014 (`ErrorProvider` fires independently of the login attempt) are the recorded design-mode scenarios in scenarios.md, but `manifest.json` is absent (`fixtures: []`) — these are PENDING CAPTURE, not yet recorded fixtures; a human must run the harness against the legacy app before replay comparison is possible.
- Because the rebuild replaces plaintext comparison with hashing and static-field identity with a real session (CQ-011/SQ-004), no legacy fixture can validate the *new* hash-comparison/session mechanism itself — a human must define new acceptance fixtures for that mechanism, and must separately decide the migration/reset procedure for the 9 existing plaintext-password production accounts (golden-master data) before any hashed-login fixture can be captured for them.
**Gate:** OPEN QUESTIONS — risk from unanswered, non-blocking: CQ-027; UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-013 (66e5eb5), GM-014 (66e5eb5), GM-020 (66e5eb5), GM-022 (66e5eb5), GM-038 (66e5eb5), GM-041 (66e5eb5), GM-043 (66e5eb5), GM-044 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

**Status notes (human-added):**
- BUILT: change `user-login`, merged to `master` at commit `3ddd3a9` (branch `specclaw/user-login`), 2026-08-28. All 19 tasks complete, verify PASS on all 7 acceptance criteria (`.specclaw/changes/user-login/verify-report.md`), 10 backend + 8 frontend tests passing. Login screen, AuthController, PasswordHasherService (PBKDF2) and JwtTokenService are real and in place; still open: CQ-027 (unanswered), UI screenshot sign-off (`.specclaw/ui/ui-manifest.json` absent), and the production password-migration procedure for the 9 legacy accounts (not built — no admin User CRUD exists in this rebuild's scope per CQ-012).

---

### BL-002 — Main Menu Navigation Hub



**Merge rationale:** "Navigate to a feature area" and "Closing the Main Menu exits the entire application" are merged into one item — both are behaviors of the single frmAnaMenu component with no numbered business rule of their own (pure navigation/lifecycle), trivially small, and tightly coupled to the same screen and the same click/close handlers.
**Module:** MOD-001
**Maps to capability:** "Navigate to a feature area - five buttons: Search (btnArama), Asset Assignment (btnOdaDemirbasIslemleri), Room Assignment (btnOdaTanimlama), Admin Panel (btnAdmin), Reporting (button1, labeled Rapor Cikitisi Al). Each opens its target form and hides the Main Menu (this.Hide())." and "Closing the Main Menu exits the entire application (frmAnaMenu_FormClosing: Application.Exit())." (functional-spec.md, Main Menu / Navigation)
**Depends on:** BL-001 (frmAnaMenu is only reached from frmGiris on a successful login)
**Acceptance basis (domain-model.md):**
- architecture.md L3: "Main Menu / Navigation (frmAnaMenu.cs) is the apps navigation hub: its constructor takes the previous form and closes it, its five button handlers route to Search, Asset Assignment, Room Assignment, Admin Panel, and Reporting."
- No numbered DR-NNN rule governs plain navigation routing or the FormClosing/Application.Exit() lifecycle - this is a client-orchestrated navigation shell with no backend rule behind it, stated explicitly per the Fidelity Discipline note that not every items acceptance basis rests on a numbered rule.
- SCR-002 (Main Menu) layout structure, per ui-inventory.md (FAITHFUL policy): a 2x2 grid of large navigation buttons filling most of the form, plus a full-width button holding the reporting entry point. No project-specific token group applies beyond the OS-default styling already covered by CQ-009s resolution (one consistent OS-default font across all 12 screens); Main Menu carries no entry in design-tokens.json token_groups.

**Verification inputs needed:**
- No GM scenario in scenarios.md exercises pure navigation routing or the FormClosing/Application.Exit() lifecycle (every GM scenario targets a data-bearing seam) - this item has NO BASELINE DATA for its own routing/exit behavior; a human must confirm the five navigation edges and the exit-on-close behavior by direct manual comparison against the running legacy app, since no scenario covers it and none is proposed here (pure UI routing is not a golden-master-worthy seam on its own).
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-003 — Admin Authorization Gate (Admin button enable/disable)


⟲ revised per CQ-024, 2026-08-28

**Module:** MOD-001
**Maps to capability:** "Admin Panel button is conditionally enabled - per DR-003, re-evaluated on every Main Menu load." (functional-spec.md, Main Menu / Navigation)
**Depends on:** BL-002
**Acceptance basis (domain-model.md):**
- DR-003 - Admin authorization gate: frmAnaMenu.cs, ANA_MENU_Load: the Main Menu's ADMIN button is enabled only when a fresh re-query (SELECT YetkiID FROM tblKullanicilar WHERE KullaniciAdi=... AND Sifre=..., using the static username/password captured at login) returns the literal string True.
- Entity User.YetkiID - Enumerations section: a two-value authorization level, compared in code only against the literal string True. No third value or any other literal is ever checked against this field anywhere in scope.
- ui-inventory.md, SCR-002 widget row: ADMIN button (conditionally enabled - DR-003), bound to User.YetkiID (gates this button only).
- Per CQ-024 (decided DEFECT): Treat the multi-row-match edge case (more than one tblKullanicilar row matching the same username+password with different YetkiID values) as unreachable in practice - no evidence of duplicate username+password pairs in migrated production data, and no scenario arranges this state; a tie-break rule is not required in the rebuild. Note: harness work this session confirmed YetkiID is a bit column, not a string as the original finding assumed - this narrows the column's real value space to true/false but does not change this answer.

**Verification inputs needed:**
- GM-015 (exact "True" match enables), GM-016 ("False" disables), and GM-018 (fail-open/stays-enabled on zero matching rows) are now captured fixtures (manifest.json, legacy commit 66e5eb5) - this item's core DR-003 boundary behavior is directly evidenced.
- GM-017 (case-sensitive non-match treated as False) is listed in manifest.json's missing_scenarios - its capture failed due to a pre-existing test-assertion bug in the harness itself, unrelated to DR-003's own contract or to CQ-024's now-resolved question; a human must fix that assertion and re-run the harness before this specific boundary fixture exists.
- CQ-024's multi-row-match edge case is now resolved as unreachable in practice - no fixture is needed for it, consistent with scenarios.md's own Rule Coverage Check never having scenario'd it.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-015 (66e5eb5), GM-016 (66e5eb5), GM-018 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-004 — Admin Panel Sub-Navigation




**Module:** MOD-001
**Maps to capability:** "Navigate to an admin-only sub-screen - five buttons routing to Stock Add (btnStokEkle), Stock Update (btnStokGuncelle), Room Delete (btnOdaSil), Room Add (btnOdaEkle), Room Update (btnOdaGuncelle). Pure router with no data access of its own (confirmed: no SqlConnection field in this file)." (functional-spec.md, Admin Panel)
**Depends on:** BL-003
**Acceptance basis (domain-model.md):**
- architecture.md L3: "Admin Panel (frmAdmin.cs) has no direct database access of its own (no SqlConnection field) - it is a pure routing screen gating five admin-only child screens."
- No numbered DR-NNN rule governs pure routing - reachability itself is gated by DR-003 (BL-003), not by a rule of this screen's own.
- SCR-007 (Admin Panel) layout structure, per ui-inventory.md (FAITHFUL policy): two large side-by-side buttons in the upper portion for stock operations, three smaller buttons in a row beneath them for room operations. No project-specific token group beyond the global OS-default styling (CQ-009).

**Verification inputs needed:**
- No GM scenario exercises this pure-routing screen directly (its five destinations are exercised as their own screens' entry seams) - NO BASELINE DATA for the routing edges themselves; a human must confirm the five routing edges and the DR-003 reachability precondition by direct manual comparison against the running legacy app.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-015 (66e5eb5), GM-016 (66e5eb5), GM-018 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

## MOD-002 — Room Management

_Depends on: MOD-001. Module dependency rank 1. 4 active item(s)._

### BL-005 — Room Add



**Module:** MOD-002
**Maps to capability:** "Add a new room - fields: Room name (txtOdaESGodaAdi, plain TextBox, no keypress filter), Department (captured via a paired ListBox selector - lboxDepartmanID/lboxDepartmanAdi populated from tblDepartmanlar; selecting an ID row echoes into a disabled TextBox txtDepartmanID, not free-typed). On success, clears every TextBox child control and shows Oda basariyla eklendi; on SqlException (e.g. a duplicate) shows Kayitli Oda... (DR-004 non-empty check on room name only)." (functional-spec.md, Room Add)
**Depends on:** BL-004
**Acceptance basis (domain-model.md):**
- Entity Room (tblOda): Fields observed: OdaID (primary key), OdaAdi (room name), DepartmanID (foreign key to Department).
- Entity Department (tblDepartmanlar): read-only lookup, no screen in scope creates, edits, or deletes tblDepartmanlar rows. Per CQ-012 (decided SCOPE, option 2): Preserve the legacy assumption - no admin CRUD screens for User/Department/Personnel/AssetType in this rebuild.
- DR-004 - Required-field soft validation (cross-cutting): non-empty check on room name only, per functional-spec.md's own text quoted above; scenario GM-020 confirms the ErrorProvider and the Trim() != "" gate genuinely agree on this screen (unlike Login's GM-013).
- Per CQ-008 (decided DEFECT/fix): fix it so the room-name field actually clears after a successful add, matching functional-spec.md's documented intent - the legacy field-clear loop is a structural no-op (never recurses into the GroupBox holding the real field, per ui-inventory.md Named Gap 3), so the rebuild must implement genuine field-clearing, not reproduce the legacy no-op.
- Per CQ-018 (decided DATA): no unique constraint exists at all on OdaAdi or DemirbasAdi; Rebuild should add real uniqueness constraints on both name fields rather than assuming they already exist - this item's schema must add a uniqueness constraint on Room.OdaAdi that the legacy app's misleading Kayitli Oda... duplicate-catch message implied but never actually enforced.
- SCR-010 (Room Add) layout structure, per ui-inventory.md (FAITHFUL policy): a single bordered section labeled ODA EKLEME, containing a labeled room-name field near the top-left, a paired department-ID/department-name list to the right, a disabled department-ID echo field beneath the room-name field, and a centered add button. No project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-019 (success), GM-020 (empty name rejected via agreeing paths), GM-021 (duplicate name currently succeeds, no constraint), GM-022 (no department selected throws an uncaught exception) - PENDING CAPTURE.
- Because CQ-018's new uniqueness constraint changes GM-021's outcome (a duplicate name will now be rejected instead of silently succeeding), GM-021 cannot be reused unmodified as the rebuild's target fixture - a human must capture a new expected-rejection fixture for the constrained schema once the migration/constraint is in place, keeping the legacy-parity GM-021 capture only as historical evidence, not as the rebuild's own acceptance target.
**Gate:** OPEN QUESTIONS — risk from unanswered, non-blocking: CQ-027; UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-013 (66e5eb5), GM-014 (66e5eb5), GM-020 (66e5eb5), GM-022 (66e5eb5), GM-038 (66e5eb5), GM-041 (66e5eb5), GM-043 (66e5eb5), GM-044 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-006 — Room Update (rename)



**Module:** MOD-002
**Maps to capability:** "Rename an existing room - fields: existing-room selector (cboOdaESGodaAdiGuncel, a ComboBox whose options are sourced from a live SELECT * FROM tblOda query, i.e. every current room name), new room name (txtOdaESGyeniOdaAdi, plain TextBox). See PQ-004 - keyed by name, not ID." (functional-spec.md, Room Update)
**Depends on:** BL-005
**Acceptance basis (domain-model.md):**
- Entity Room (tblOda), OdaAdi field, matched by name rather than OdaID - the sole exception to this codebase's otherwise ID-keyed CRUD pattern (Named Gap 4 in functional-spec.md).
- Per CQ-004 (decided DEFECT, option a): Intentional - preserve name-based matching (OdaAdi) in the rebuild. Given schema inspection confirmed no existing unique constraint on OdaAdi (see CQ-018), the rebuild must add a real uniqueness constraint/validation on the room-name field to make name-based matching safe. This item's UPDATE remains keyed by name, but only becomes safe once BL-005's CQ-018 uniqueness constraint exists.
- No numbered DR-NNN rule governs the update-by-name keying mechanism itself (a structural/keying choice CQ-004 resolves directly, not a numbered business rule).

**Verification inputs needed:**
- GM-023 (success, keyed by current name), GM-024 (no-op success when the old name matches no row), GM-025 (multi-row rename side-effect when duplicate names exist under the pre-constraint schema) - PENDING CAPTURE.
- GM-025's multi-row-rename scenario describes legacy behavior that becomes unreachable once BL-005's CQ-018 uniqueness constraint is enforced (duplicate OdaAdi values can no longer exist) - capture it for historical/legacy-parity reference only, not as a target behavior the rebuild must reproduce going forward.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-007 — Room Delete


⟲ revised per CQ-023, 2026-08-28

**Module:** MOD-002
**Maps to capability:** "Delete a room - field: room selector (cboOdaESGodaAdiSil, ComboBox sourced from tblOda). No confirmation dialog is shown before the delete executes. See PQ-004 - keyed by name, not ID." (functional-spec.md, Room Delete)
**Depends on:** BL-005
**Acceptance basis (domain-model.md):**
- Entity Room (tblOda) and RoomAssetAssignment (tblOdaDemirbasAtama) - relationship: Room to RoomAssetAssignment via OdaID (FK).
- Per CQ-017 (decided DEFECT): Reproduce as-is (no confirmation dialog) - applying the faithful-by-default policy (SQ-012).
- Per CQ-004 (decided): preserve name-based keying (as in BL-006), now safe under BL-005's uniqueness constraint.
- Per CQ-023 (decided DEFECT, no DR-NNN citation, confirmed empirically): a real FK constraint (FK_tblOdaDemirbasAtama_tblOda) exists and blocks the delete of a Room still referenced by a tblOdaDemirbasAtama row - the legacy app crashes with an unhandled SqlException ("The DELETE statement conflicted with the REFERENCE constraint...") since frmOdaSil.cs has no try/catch at all, the only mutating handler among all eleven forms with none. This reverses this item's own prior "Proposed default" of a silent orphan - the confirmed legacy behavior is a crash, not a silent success. The rebuild must treat Room Delete against a room with existing assignments as a real, reportable error condition (e.g. "this room has assigned assets/personnel and cannot be deleted"), not a silent orphan and not an unhandled crash.
- SCR-011 (Room Delete) layout structure, per ui-inventory.md (FAITHFUL policy): a single bordered section labeled ODA SILME, containing a labeled room selector and a delete button in a single row. No project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-026 (success, no children), GM-027 (no-op on a nonexistent name), GM-028 (multi-row delete side-effect, pre-constraint) are now captured fixtures (manifest.json, legacy commit 66e5eb5).
- GM-029 (deleting a room with an associated tblOdaDemirbasAtama row) is now a captured fixture confirming the crash directly against a live database - outcome REJECTED, threw true, the FK constraint named above rejecting the DELETE. Its manifest entry still carries a stale provisional_ref ("PQ-010,CQ-003") marker predating CQ-023's resolution; a human should re-run the baseline capture step to clear that stale marker from manifest.json itself, but the underlying observed behavior it captured is exactly what CQ-023's answer now states as decided.
- Per the Fidelity Discipline: this fixture pins the legacy crash exactly, but the rebuild's own target behavior (a real, reportable error rather than an unhandled crash) is a deliberate improvement per CQ-023's decision text - a human must additionally define and capture a new target fixture for the rebuild's own graceful error-handling path, since GM-029 only proves what the legacy app does today, not what the rebuild should do about it.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-008 — Room to Personnel Assignment (Room Assignment)


⟲ revised per CQ-025, 2026-08-28

**Module:** MOD-002
**Maps to capability:** "Assign a responsible staff member to a room - cross-references the Room-to-Personnel Assignment workflow below. User selects a row in the Room grid (dGWOda, read-only DataGridView, single-select via RowEnter) and a row in the Personnel grid (dGWPersonel, read-only DataGridView), then clicks Kaydet (btnOTodaKaydet). Selected values echo into two disabled/read-only TextBoxes (txtOTodaAdi, txtOTOdaSorumlusu) - these are display-only, not user-entered fields." and "Named Gap: no non-empty/selection guard and no try/catch around the insert - see PQ-005." (functional-spec.md, Room Assignment)
Workflow: "Room-to-Personnel Assignment (linear) - frmOdaTanimlama.cs: user selects a Room grid row (populates Odaid) and a Personnel grid row (populates Personelid), then clicks Kaydet, which runs a single INSERT INTO tblOdaDemirbasAtama(OdaID,PersonelID). Linear - no validation branch exists in code (see PQ-005 Named Gap on the missing guard/try-catch)."
**Depends on:** BL-002, BL-005
**Acceptance basis (domain-model.md):**
- Entity RoomAssetAssignment (tblOdaDemirbasAtama): "1. Room-responsibility assignment (frmOdaTanimlama.cs): insert into tblOdaDemirbasAtama(OdaID,PersonelID)values(@odaID,@personelID) - pairs a room with its responsible staff member; no DemirbasID/AlinanAdet."
- Per CQ-003 (decided DECISION): tblOdaDemirbasAtama has a single surrogate PK (OdaDemirbasAtamaID, int NOT NULL) with OdaID, DemirbasID, AlinanAdet, and PersonelID all nullable - one uniform, genuinely mixed-purpose row-shape. No discriminator column exists. This item's insert shares this table with BL-011's asset-issue insert.
- Per CQ-005 (decided DEFECT): Defect - add the same empty-selection guard/try-catch pattern used by every other mutating screen. Schema inspection confirmed OdaID/PersonelID in tblOdaDemirbasAtama are nullable, so the legacy app would silently insert an orphaned null-assignment row rather than crash - a real (if quiet) data-integrity gap worth closing in the rebuild.
- Per CQ-006 (decided DECISION): Follow the legacy structure - don't force a single owner. Personnel is genuinely shared, externally-provisioned reference data, read by both MOD-002 and MOD-003 exactly as the legacy app does.
- Per CQ-007 (decided DECISION): Follow the legacy structure - preserve the dual-write pattern exactly as today: MOD-002 (Room Management) writes room-responsibility rows, MOD-003 (Asset Assignment and Stock) writes asset-issue rows, both to the same table. No forced single owner.
- Per CQ-025 (decided DECISION, confirmed): row order in this screen's Room/Personnel selection grids is not a real requirement - the rebuild is free to choose any explicit, stable order (e.g. by primary key) without it being a behavioral regression. The absence of any ORDER BY anywhere in this codebase is itself the evidence this was never a considered requirement.
- SCR-006 (Room Assignment) layout structure, per ui-inventory.md (FAITHFUL policy): two side-by-side selection grids (rooms left, personnel right), a row of two labeled disabled echo fields beneath the grids, and a save button beneath the echo-field row. No project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-030 (success) is now a captured fixture (manifest.json, legacy commit 66e5eb5).
- GM-031 (NULL OdaID inserted silently) and GM-032 (both fields NULL inserted silently) are listed in manifest.json's missing_scenarios - the harness run reports a real SqlException from an AddWithValue null-string parameter bug rather than the silent-insert result these scenarios were designed to observe. This reads as a harness-authoring issue (passing a raw C# null to SqlCommand.Parameters.AddWithValue, which ADO.NET itself rejects, rather than DBNull.Value) and not a finding about frmOdaTanimlama.cs's own runtime behavior - but this has not been independently confirmed either way, since no document read this run resolves it. A human must fix the harness's own parameter-binding for these two scenarios and re-run them before this item's CQ-005 guard-replacement fixtures (a rejection, not a silent orphaned insert) can be captured with confidence.
- Row-order display for the two selection grids is no longer an open question - CQ-025 is answered and no fixture is needed for it.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

## MOD-003 — Asset Assignment & Stock

_Depends on: MOD-001. Module dependency rank 1. 3 active item(s)._

### BL-009 — Stock / Asset Add



**Module:** MOD-003
**Maps to capability:** "Add a new fixed-asset stock record - fields: Asset name (txtSEdemirbasAdi, plain TextBox, no keypress filter - see DR-006 Named Gap), Price (txtSEfiyat, TextBox with digit/comma-only keypress filter - DR-005), Purchase date (dtpAlimTarihi, a real DateTimePicker), Asset Type (paired ListBox selector lboxDemirbasTuruID/lboxDemirbasTuruAdi, sourced from tblDemirbasTurleri), Quantity (txtSEadet, TextBox with digit/comma-only keypress filter - DR-005). Non-empty checks (DR-004) on name/price/quantity only." (functional-spec.md, Stock / Asset Add)
**Depends on:** BL-004
**Acceptance basis (domain-model.md):**
- Entity FixedAsset (tblDemirbas): Fields DemirbasID, DemirbasAdi, Fiyat, AlimTarihi, DemirbasTuruID, Adet.
- Entity AssetType (tblDemirbasTurleri): read-only lookup. Per CQ-012 (decided): preserve, no CRUD.
- DR-005 - Numeric-only keypress filters (cross-cutting): restricts keyboard entry to digits, backspace, and comma; no code path in scope ever parses the field's text as a decimal number.
- DR-006 - Letter-only keypress filters (cross-cutting), Named Gap: frmStokEkleme.cs declares an identical HarfGirisiKontrol method but never wires it to any control - its own asset-name field (txtSEdemirbasAdi) has no keypress filter at all, unlike its Update-screen counterpart.
- Per CQ-015 (decided DEFECT): Reproduce as-is - applying the faithful-by-default policy (SQ-012). The asset-name field's missing letter-filter is preserved as-is, not fixed.
- Per CQ-013 (decided DATA): tblDemirbas.Fiyat is money (precision 19, scale 4), nullable. Rebuild should use a matching monetary type (e.g. decimal(19,4)).
- Per CQ-014 (decided MECHANICAL): Adopt as-is - keep comma-only decimal filtering, matching legacy behavior exactly.
- Per CQ-018 (decided DATA): add a real uniqueness constraint on FixedAsset.DemirbasAdi (same reasoning as BL-005's Room.OdaAdi constraint).
- DR-004 - non-empty checks on name/price/quantity.
- SCR-008 (Stock Add) layout structure, per ui-inventory.md (FAITHFUL policy): a vertically-stacked column of four labeled fields (name, price, date, quantity) on the left, a paired asset-type id/name list to the right of the name/price rows, a disabled asset-type-ID echo field, and a wide add button beneath the column. No project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-006 (numeric keypress filter, this screen's own duplicated implementation), GM-010 (this screen's unwired letter-filter proving parity of logic, but confirmed never actually applied to the name field) - PENDING CAPTURE.
- No GM scenario in scenarios.md directly exercises this screen's own successful-add/duplicate-name/empty-field seams (unlike Room Add's GM-019 through GM-022) - a human must capture new golden-master fixtures for Stock Add's own insert/validation/duplicate paths against the running legacy app before this item can be marked verifiable beyond its keypress-filter sub-behaviors.
- Per CQ-013's money-type resolution, migrated Fiyat values must be validated as parseable decimal(19,4) during data migration - a human must confirm no legacy Fiyat value in the production data fails this conversion.
**Gate:** OPEN QUESTIONS — risk from unanswered, non-blocking: CQ-027; UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-005 (66e5eb5), GM-006 (66e5eb5), GM-007 (66e5eb5), GM-008 (66e5eb5), GM-009 (66e5eb5), GM-010 (66e5eb5), GM-013 (66e5eb5), GM-014 (66e5eb5), GM-020 (66e5eb5), GM-022 (66e5eb5), GM-038 (66e5eb5), GM-039 (66e5eb5), GM-041 (66e5eb5), GM-043 (66e5eb5), GM-044 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-010 — Stock / Asset Update



**Module:** MOD-003
**Maps to capability:** "Update an existing fixed-asset stock record - user selects a row from DGWStokGuncelleme (read-only DataGridView) to populate: Asset name (txtSGdemirbasAdi, TextBox with letter-only keypress filter - DR-006), Price (txtSGfiyat, TextBox, digit-only - DR-005), Purchase date (DtmSGAlimTarihi, DateTimePicker), Quantity (txtSGadet, TextBox, digit-only - DR-005), Asset Type (paired ListBox selector, same pattern as Stock Add). Non-empty checks (DR-004) on name/price/quantity." (functional-spec.md, Stock / Asset Update)
**Depends on:** BL-009
**Acceptance basis (domain-model.md):**
- Entity FixedAsset (tblDemirbas), keyed by DemirbasID (unlike Room Update/Delete, this screen is correctly ID-keyed - no CQ-004-style ambiguity).
- DR-005, DR-006 (both wired correctly on this screen, unlike Stock Add's DR-006 gap).
- DR-004 - non-empty checks on name/price/quantity.
- Per CQ-013/CQ-014/CQ-018 - same monetary type / comma-decimal / uniqueness-constraint decisions as BL-009 apply identically here.
- SCR-009 (Stock Update) layout structure, per ui-inventory.md (FAITHFUL policy): a wide selection grid spanning most of the form's width near the top, a labeled purchase-date field below it, a vertically-stacked name/price pair with a paired asset-type list beside it, a labeled quantity field, and a single wide update button spanning almost the full width at the bottom. No project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-007 (numeric keypress filter), GM-008 (letter keypress filter) - PENDING CAPTURE.
- No GM scenario exercises this screen's own row-selection-populates-fields, successful-update, or generic-error (Guncellerken hata olustu...) paths - a human must capture new golden-master fixtures for these before this item can be marked verifiable beyond its keypress-filter sub-behaviors.
**Gate:** OPEN QUESTIONS — risk from unanswered, non-blocking: CQ-027; UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-005 (66e5eb5), GM-006 (66e5eb5), GM-007 (66e5eb5), GM-008 (66e5eb5), GM-009 (66e5eb5), GM-010 (66e5eb5), GM-013 (66e5eb5), GM-014 (66e5eb5), GM-020 (66e5eb5), GM-022 (66e5eb5), GM-038 (66e5eb5), GM-039 (66e5eb5), GM-041 (66e5eb5), GM-043 (66e5eb5), GM-044 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-011 — Asset Assignment and Stock Decrement (Composite Flow)


⟲ revised per CQ-026, 2026-08-28

**Module:** MOD-003
**Maps to capability:** "Issue a fixed asset to a room - cross-references the Asset Assignment and Stock Decrement workflow below (a Composite Flow - see Workflows). User selects a row in the Room grid (dgwOdalar, read-only DataGridView) and a row in the Asset grid (dgwDemirbas, read-only DataGridView), enters a Quantity (txtDIAdet, TextBox with digit-only keypress filter - DR-005), then clicks Kaydet (btnDemirbasIslemKaydet). DR-001 blocks the save if quantity exceeds available stock." (functional-spec.md, Asset Assignment)
Workflow: "Asset Assignment and Stock Decrement (Composite Flow) - A single click of btnDemirbasIslemKaydet in frmDemirbasIslem.cs triggers a sequence of two distinct backend writes, after an in-memory guard: 1. Guard (DR-001)... 2. Call 1, INSERT INTO tblOdaDemirbasAtama... 3. Call 2, GuncelleAdet() -> UPDATE tblDemirbas SET Adet=@adet... (DR-002). 4. Both grids refresh. What is lost if step 3 is omitted: the asset's tblDemirbas.Adet would remain at its pre-assignment count even though units were just issued - every subsequent DR-001 stock-adequacy check would then compare the requested quantity against a stale (too-high) stock figure, allowing the same units to be issued repeatedly without ever running out."
**Depends on:** BL-002, BL-005, BL-008, BL-009
**Acceptance basis (domain-model.md):**
- DR-001 - Stock adequacy check before assignment: rejects an asset-to-room assignment when the requested quantity exceeds the in-memory stock count captured from the currently selected asset grid row, performing no database write in that case.
- DR-002 - Stock decrement on assignment: immediately after a successful assignment insert, sets tblDemirbas.Adet = (stok - Alinanadet) for the issued asset. This is one half of a Composite Flow.
- DR-004 - Required-field soft validation (cross-cutting): non-empty check on the quantity field, one of DR-004's six cited screens.
- Entity RoomAssetAssignment: "2. Asset-issue record (frmDemirbasIslem.cs): insert into tblOdaDemirbasAtama(OdaID,DemirbasID,AlinanAdet,PersonelID)values(...) - records a quantity of a fixed asset issued into a room, carried by whichever personnel-room pairing already exists" (PersonelID inherited from BL-008's room-responsibility row).
- Per CQ-007 (decided): MOD-003 writes asset-issue rows to this same table (dual-write with MOD-002/BL-008), no forced single owner.
- The two-write sequence (insert assignment, then decrement stock) is a backend-orchestrated composite flow that, in the target ASP.NET Core Web API plus React SPA rebuild, must remain a single atomic backend operation, not two separate client-triggered API calls - no single DR-NNN states must be atomic/one transaction on its own, but functional-spec.md's own "What is lost if step 3 is omitted" paragraph (quoted above) makes the consequence of splitting it explicit.
- Open question: CQ-028 (unanswered DEFECT, no DR-NNN) - Non-atomic multi-connection write in Asset Assignment and Stock Decrement - whether the rebuild must wrap the insert and decrement in a single transaction (proposed default) or reproduce the legacy non-atomic two-connection sequence as-is.
- Per CQ-026 (decided DEFECT, confirmed empirically): tblDemirbas.Adet has no CHECK constraint preventing a negative value - GuncelleAdet() called directly (bypassing DR-001's guard) succeeds silently, leaving Adet at a negative value with no exception thrown (GM-040's captured fixture: adet_after -3, threw false). DR-001's in-memory guard in the click handler is therefore the only enforcement of non-negative stock anywhere in this flow - GuncelleAdet() itself, and the database schema behind it, enforce nothing. The rebuild's own stock-decrement logic must supply its own non-negative guard at the point of decrement (not merely at the click-handler's pre-check) if going negative is undesired, since the legacy DB schema never enforced it and nothing else in this composite flow will catch a decrement invoked through any other path.
- SCR-004 (Asset Assignment) layout structure, per ui-inventory.md (FAITHFUL policy): a bordered section with two side-by-side selection grids (rooms left, assets right) in its top half, a row of two disabled echo fields beneath the grids, a labeled quantity field, and a save button; a second bordered section to the right holding one read-only grid listing everything currently assigned to the selected room. No project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-034 (guard rejects over-stock), GM-035 (guard passes at exact equality), GM-036 (partial issue decrements correctly), GM-037 (decrement to exactly zero), GM-038 (empty quantity rejected, DR-004), GM-039 (non-numeric quantity bypassing keypress filter throws FormatException), and GM-040 (GuncelleAdet() called directly bypasses DR-001's guard entirely, confirmed: stock goes negative, no exception) are now captured fixtures (manifest.json, legacy commit 66e5eb5) - this item's core DR-001/DR-002 behavior, including the negative-stock edge case CQ-026 resolves, is directly evidenced.
- Per the Fidelity Discipline: none of GM-034 through GM-040 exercise the client-orchestration question of whether a rebuilt SPA correctly triggers both backend writes as a single request - each pins one backend seam's own logic. A human must additionally capture (or the harness must additionally assert) that the rebuilt frontend issues exactly one API call for this composite action, not two, since a backend-step fixture alone cannot detect a client that only calls the assignment-insert endpoint and forgets the decrement.
- CQ-028's transaction-boundary decision changes what GM-036/GM-037's success fixture should assert about partial-failure recovery (e.g. a dropped connection between the two writes) - cannot be captured until CQ-028 is answered.
- Now that CQ-026 confirms no DB-level guard exists, the rebuild's own new non-negative guard (whatever shape it takes) will need its own fresh fixture once built - GM-040 only pins the legacy app's current (unguarded) behavior, not the rebuild's target behavior, and per the Fidelity Discipline a legacy fixture alone cannot prove the new guard behaves correctly.
**Gate:** OPEN QUESTIONS — risk from unanswered, non-blocking: CQ-027, CQ-028; UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-005 (66e5eb5), GM-006 (66e5eb5), GM-007 (66e5eb5), GM-013 (66e5eb5), GM-014 (66e5eb5), GM-020 (66e5eb5), GM-022 (66e5eb5), GM-034 (66e5eb5), GM-035 (66e5eb5), GM-036 (66e5eb5), GM-037 (66e5eb5), GM-038 (66e5eb5), GM-039 (66e5eb5), GM-040 (66e5eb5), GM-041 (66e5eb5), GM-043 (66e5eb5), GM-044 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

## MOD-004 — Search

_Depends on: MOD-001. Module dependency rank 1. 2 active item(s)._

### BL-012 — Search fixed assets by one of five criteria


⟲ revised per CQ-025, 2026-08-28

**Module:** MOD-004
**Maps to capability:** "Search fixed assets by one of five criteria - a mutually-exclusive RadioButton group (rdbDemirbasAdi default-checked, rdbDemirbasTuru, rdbFiyat, rdbAlimTarihi, rdbAdet) selects which WHERE clause runs against a shared free-text box (txtAramalarBilgiGiriniz, TextBox) - except for the Alim Tarihi (purchase date) criterion, which swaps in a DateTimePicker (dtmBilgi) in place of the text box. Results render into dgwAramalarDemirbas (read-only DataGridView). Non-empty check (DR-004) on the free-text box only (not applicable when the date picker is active)." (functional-spec.md, Search)
Workflow: "Search (branches on selected criterion) - frmAramalar.cs's asset-search side branches into one of five WHERE clauses depending on which RadioButton is checked, with the input control itself changing (free-text box vs. date picker) depending on the branch."
**Depends on:** BL-002, BL-009
**Acceptance basis (domain-model.md):**
- Entity FixedAsset, AssetType (joined for the type-name criterion).
- DR-004 - non-empty check on the free-text box only, not applicable when the date picker is active (functional-spec.md explicitly notes this exception).
- Per CQ-010 (decided): this screen's ad-hoc filter query is one of the four query paths CQ-010 requires parameterized/fixed for the rebuild (Search's SQL-concatenation vulnerability).
- Per CQ-025 (decided DECISION, confirmed): search-results row order is not a real requirement - the rebuild is free to choose any explicit, stable order (e.g. by primary key).
- SCR-003 (Search) layout structure, per ui-inventory.md (FAITHFUL policy): a back button pinned top-left, two side-by-side bordered panels (personnel search left, asset search right); the asset panel has a label plus a row of five mutually-exclusive criterion radio buttons above a shared input slot that swaps between a free-text field and a date picker, a search button, and a results grid below. No project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-041 (empty search term rejected) and GM-043 (date-criterion search) are now captured fixtures (manifest.json, legacy commit 66e5eb5). GM-042 (name-search match) is also captured, though it pins no numbered rule and so is not automatically joined to this item by the rule-citation mechanism - cited here directly as supporting evidence for the name-search branch.
- GM-043's captured result records whichever conversion format SQL Server's implicit datetime-to-varchar conversion actually produced against the live database - a human should inspect the recorded fixture directly to confirm the exact format observed, since this was not independently derivable from source code alone.
- Once CQ-010's parameterized-query fix is applied, this screen's underlying query mechanism changes; a human must confirm the date-criterion's implicit-conversion-dependent match behavior (GM-043) is preserved or intentionally redefined under the new parameterized query, since a parameterized date comparison may not reproduce the same LIKE-pattern string-matching semantics as the legacy code - GM-043 as captured remains a legacy-parity reference, not necessarily the rebuild's own target fixture.
- Row order for search results is no longer an open question - CQ-025 is answered and no fixture is needed for it.
**Gate:** OPEN QUESTIONS — risk from unanswered, non-blocking: CQ-027; UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-013 (66e5eb5), GM-014 (66e5eb5), GM-020 (66e5eb5), GM-022 (66e5eb5), GM-038 (66e5eb5), GM-041 (66e5eb5), GM-043 (66e5eb5), GM-044 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-013 — Search personnel by first/last name


⟲ revised per CQ-025, 2026-08-28

**Module:** MOD-004
**Maps to capability:** "Search personnel by first/last name - fields: txtAramalarAd (letter-only keypress filter - DR-006), txtAramalarSoyad (letter-only keypress filter - DR-006), both plain TextBoxes. Results render into dgwAramalarPersonel (read-only DataGridView)." (functional-spec.md, Search)
**Depends on:** BL-002, BL-008, BL-011
**Acceptance basis (domain-model.md):**
- Entity Personnel (tblPersonel), RoomAssetAssignment, FixedAsset (joined) - per architecture.md L3: a personnel-name search over tblPersonel/tblOdaDemirbasAtama/tblDemirbas.
- DR-006 - Letter-only keypress filters (cross-cutting): both fields wired correctly on this screen.
- No DR-004 gate on this branch at all (confirmed directly, functional-spec.md's Search capability text) - an explicit absence, not an omission in this analysis: this branch has no DR-004 gate, confirmed by reading the whole method, unlike the asset-filter branch (per GM-044).
- Per CQ-006 (decided): Personnel entity is shared reference data, read by both MOD-002 and MOD-003 exactly as the legacy app does - this search screen (MOD-004) is a third consumer, referencing but not owning it.
- Per CQ-025 (decided DECISION, confirmed): row order is not a real requirement, same as BL-012 - the rebuild is free to choose any explicit, stable order.
- SCR-003 (Search) layout structure, per ui-inventory.md (FAITHFUL policy): the personnel panel has a labeled first-name field and a labeled last-name field stacked vertically near the top, a search button beside them, and a results grid filling the panel below. Shared screen with BL-012; no project-specific token group beyond CQ-009's global OS-default.

**Verification inputs needed:**
- GM-044 (both name fields empty, no gate at all, returns zero rows) is now a captured fixture (manifest.json, legacy commit 66e5eb5). GM-045 (apostrophe in input triggers a SQL syntax error under the current concatenated query) is also captured, though it pins no numbered rule and so is not automatically joined to this item by the rule-citation mechanism - cited here directly as supporting evidence.
- GM-045 is explicitly flagged in scenarios.md as capturing legacy AS-IS behavior for baseline parity only, with the expectation it will legitimately fail to replay once CQ-010's fix ships - a human must treat a divergence on this specific fixture as expected, not as a regression, once the parameterized query is in place.
- Row order is no longer an open question - CQ-025 is answered and no fixture is needed for it.
**Gate:** OPEN QUESTIONS — risk from unanswered, non-blocking: CQ-027; UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** VERIFIABLE — fixtures: GM-008 (66e5eb5), GM-009 (66e5eb5), GM-010 (66e5eb5), GM-013 (66e5eb5), GM-014 (66e5eb5), GM-020 (66e5eb5), GM-022 (66e5eb5), GM-038 (66e5eb5), GM-041 (66e5eb5), GM-043 (66e5eb5), GM-044 (66e5eb5)
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

## MOD-005 — Reporting & Print

_Depends on: MOD-001. Module dependency rank 1. 2 active item(s)._

### BL-014 — Room Occupancy Report (view)


⟲ revised per CQ-025, 2026-08-28

**Module:** MOD-005
**Maps to capability:** "View a per-room asset-assignment report - cross-references the Room Occupancy Report workflow below. Room selector (cmbRapor, ComboBox sourced from tblOda), a Listele button (btnRaporArama) to refresh, results in dgwRapor (read-only DataGridView). Loads with the first room pre-selected (cmbRapor.SelectedIndex = 0)." (functional-spec.md, Reporting and Print)
Workflow: "Room Occupancy Report and Print (linear, with a print-side error branch) - frmRapor.cs: on load, populates the room selector, pre-selects the first room, and immediately runs RaporDoldur() to populate the grid. Clicking Listele re-runs RaporDoldur() for the currently selected room."
**Depends on:** BL-002, BL-005, BL-008, BL-011
**Acceptance basis (domain-model.md):**
- Entities Room, Personnel, FixedAsset, RoomAssetAssignment (all joined) - per architecture.md L3: joins tblOda/tblOdaDemirbasAtama/tblPersonel/tblDemirbas filtered by a selected room.
- No numbered DR-NNN rule governs this read/render sequence - functional-spec.md itself: this is a read/render sequence, not a Composite Flow under the domain rule, so it is documented here as a plain multi-step workflow rather than under Business Rules.
- Per CQ-010 (decided): Reporting's per-room filter query is one of the four query paths CQ-010 requires fixed/parameterized.
- Per CQ-025 (decided DECISION, confirmed): report-row order is not a real requirement, same reasoning as BL-012/BL-013 - the rebuild is free to choose any explicit, stable order.
- SCR-005 (Reporting and Print) layout structure, per ui-inventory.md (FAITHFUL policy): a room-selector row containing a label, a combo box, and a refresh button left-to-right, below that a wide results grid. No project-specific token group beyond CQ-009's global OS-default; print portion shared with BL-015.

**Verification inputs needed:**
- GM-046 (a room name containing an apostrophe crashes this screen, unhandled, no try/catch at all), GM-047 (joined report rows for a room with assignments), and GM-048 (empty result set for a room with zero assignments) are now captured fixtures (manifest.json, legacy commit 66e5eb5), though they pin no numbered rule and so are not automatically joined to this item by the rule-citation mechanism - cited here directly as supporting evidence.
- GM-046 is exactly the kind of crash CQ-010's fix must eliminate - a human must confirm the rebuilt query no longer throws on an apostrophe-bearing room name, replacing this crash fixture with a success fixture once the parameterized query is in place.
- Row order for report rows is no longer an open question - CQ-025 is answered and no fixture is needed for it.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui

---

### BL-015 — Report Export (Print / PDF / CSV)



**Module:** MOD-005
**Maps to capability:** "Print the current report - btnYazdir_Click renders the grid to a Bitmap and opens a Windows PrintDialog (PpdDialog), handing off to the OS print subsystem (see architecture.md L1). No file/image field is involved - the bitmap is a transient render target, not a stored/captured field." (functional-spec.md, Reporting and Print)
Workflow: "Room Occupancy Report and Print" print-side branch: "Clicking Yazdir renders the grid to a Bitmap and opens the Windows print dialog; a try/catch around this shows Hata Olustu... on any rendering/print failure, with no further detail."
**Depends on:** BL-014
**Acceptance basis (domain-model.md):**
- No numbered DR-NNN rule governs this capability - a target-platform-specific rendering mechanism, not a numbered business rule.
- Per CQ-021 (decided TARGET-GAP, settled by SQ-009): build genuine PDF/CSV export for the Reporting screen, superseding both the legacy bitmap-print path and the broken itextsharp reference. This is a full shape change: the legacy Windows PrintDialog/OS-print-subsystem mechanism has no direct equivalent in a web target and is replaced entirely, not reproduced.
- Per SQ-009 (decided SCOPE): Replace with a modern equivalent: real PDF/CSV export for the Reporting screen.
- Per scenarios.md's No Legacy Behaviour Exists item 5: PDF or any other file export from the Reporting screen. The csproj references itextsharp, but the referenced dll does not exist in this checkout and no cs file anywhere in scope uses an iTextSharp namespace - there is no method to invoke and nothing to capture. There is no legacy behavior at all to baseline for the actual export mechanism this item builds; only the bitmap/print path (which this item retires) has legacy behavior recorded.

**Verification inputs needed:**
- No golden-master scenario in scenarios.md exercises the print/export path at all (the closest recorded behavior, the bitmap-render-to-PrintDialog mechanism, is being retired per CQ-021/SQ-009, not reproduced) - this item's acceptance must come from a fresh, human-defined specification of the new PDF/CSV export's exact content and format (columns, ordering, file-naming), since no legacy fixture exists to compare against and none ever will; per the Fidelity Discipline, a fixture cannot establish same-app equivalence for behavior the legacy app never had.
- The print-side error branch's generic Hata Olustu error message (functional-spec.md) has no equivalent error-handling spec defined yet for the new export mechanism - a human must define what failure modes the new PDF/CSV export needs to handle and how they should be surfaced.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided FAITHFUL, required artifacts missing
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** ⚠ UI GROUNDING MISSING — FAITHFUL decided (SQ-013) but these artifacts are absent: .specclaw/ui/screens/, .specclaw/ui/ui-manifest.json — run /specclaw:bf-ui



## Deferred

None.

## Sequencing Rationale

MOD-001 (Authentication and Navigation) is drafted first because every other module's screens are reached only through the Main Menu (or, for admin-only screens, through the Admin Panel, itself only reachable once the Admin gate is enabled) - architecture.md's L3 flowchart shows every other component's navigation edge originating at mainMenu or adminPanel. Within MOD-001: BL-001 (Login) has no dependency, since it is the application's entry point. BL-002 (Main Menu) depends on BL-001 because frmAnaMenu is only ever constructed after a successful login. BL-003 (Admin gate) depends on BL-002 because the gate check runs on the Main Menu's own Load event. BL-004 (Admin Panel routing) depends on BL-003 because the Admin button is only reachable/enabled once the gate passes.

MOD-002 (Room Management) comes next because both Asset Assignment (MOD-003) and Reporting (MOD-005) join against Room data that only this module's screens create. Within MOD-002: BL-005 (Room Add) depends on BL-004 (it is admin-only) and is drafted before BL-006/BL-007 because a room must exist before it can be renamed or deleted - the update/delete screens' own ComboBox selectors are populated from whatever BL-005 has created. BL-008 (Room Assignment) depends on BL-002 directly (it is reachable straight from the Main Menu, not admin-gated) and on BL-005 (it needs an existing room to pair with a person).

MOD-003 (Asset Assignment and Stock) depends on MOD-001 and MOD-002 per module-map.md's own dependency graph. Within MOD-003: BL-009 (Stock Add) depends on BL-004 (admin-only) and is drafted before BL-010 (Stock Update), since a stock record must exist before it can be updated. BL-011 (Asset Assignment) depends on BL-002 (reachable from Main Menu), BL-005 (needs a Room), BL-008 (the workflow's own text states PersonelID is inherited from the room's existing room-responsibility row created by BL-008), and BL-009 (needs FixedAsset stock to issue).

MOD-004 (Search) depends on MOD-001 through MOD-003 per module-map.md. BL-012 (asset search) depends on BL-002 and BL-009 (it searches FixedAsset/AssetType data). BL-013 (personnel search) depends on BL-002, BL-008, and BL-011, since architecture.md's own L3 description of this seam is a three-way join across tblPersonel/tblOdaDemirbasAtama/tblDemirbas, and BL-011 is the item that populates the asset-issue half of that join with meaningful DemirbasID values.

MOD-005 (Reporting and Print) is drafted last, depending on MOD-001 through MOD-003, because its report query is the widest join in the whole application (Room, Personnel, FixedAsset, RoomAssetAssignment all at once - architecture.md L3). BL-014 (report view) depends on BL-002, BL-005, BL-008, and BL-011 for the same reason BL-013 does. BL-015 (report export) depends only on BL-014, since there is nothing to export until a report can be rendered.

Decisions already recorded in decisions.md (CQ-001 through CQ-022, SQ-001 through SQ-014, UQ-001, UQ-002) are folded directly into the affected items' acceptance basis above, in their already-decided shape, since this is a first-ever draft with no prior version to preserve separately. Six clarify questions remain unanswered (CQ-023 through CQ-028); each is named against the specific item(s) its prose touches in that item's own acceptance basis, and again, exhaustively, in the Coverage Check section's Open Questions Blocking Readiness subsection below. Two of those six (CQ-023, CQ-025) cite no DR-NNN or BL-NNN and are therefore invisible to bash's own mechanical Gate/PROVISIONAL join on a first-ever draft - both receive an explicit PROVISIONAL directive at the top of this file. The other four (CQ-024/DR-003, CQ-026/DR-001, CQ-027/DR-004) cite a DR-NNN their affected items also cite, so bash's own rule-intersection join will mark those items PROVISIONAL automatically once this backlog exists (CQ-028 cites no DR-NNN and is not promoted from a PQ, so it receives no PROVISIONAL directive, but it is still surfaced below so it is never silently passed over).

## Coverage Check

<!--
  Capability-bullet coverage, authored by the planner agent (bash never
  writes prose it cannot verify against the source documents) and carried
  by bash: this run's draft wins, otherwise the prior file's section is
  preserved verbatim, otherwise a line saying plainly that it is absent.

  Each bullet is accounted for on its own line, in this countable form so
  that the per-module rollup below can be computed mechanically rather than
  asserted:

    - **MOD-002** — "<capability bullet, quoted>" -> BL-014
    - **MOD-002** — "<capability bullet, quoted>" -> EXCLUDED: <reason>
    - **MOD-002** — "<capability bullet, quoted>" -> ORPHAN

  Granularity is unchanged — this is still one line per individual
  capability bullet (and per distinct clause of a compound bullet), never
  one line per module. The "### Module Coverage Rollup" subsection is
  bash-computed by counting these lines per module and is re-derived from
  scratch every run; a prior run's copy is dropped before the new one is
  appended, exactly as the UI Screen Coverage subsection is. When no line
  matches the countable form, the rollup says it is not computable rather
  than reporting 0/0 — which would read as "nothing to cover."

  Under a --module scoped run, only the scoped module's lines are replaced;
  every other module's accounting is preserved by line-level surgery.
-->

Capability bullets (functional-spec.md, Capabilities section), one line per individual bullet:

- **MOD-001** — "Log in with username and password" -> BL-001
- **MOD-001** — "Navigate to a feature area" -> BL-002
- **MOD-001** — "Admin Panel button is conditionally enabled" -> BL-003
- **MOD-001** — "Closing the Main Menu exits the entire application (frmAnaMenu_FormClosing: Application.Exit())." -> BL-002
- **MOD-001** — "Navigate to an admin-only sub-screen" -> BL-004
- **MOD-002** — "Assign a responsible staff member to a room" -> BL-008
- **MOD-002** — "Named Gap: no non-empty/selection guard and no try/catch around the insert - see PQ-005." -> BL-008
- **MOD-002** — "Add a new room" -> BL-005
- **MOD-002** — "Rename an existing room" -> BL-006
- **MOD-002** — "Delete a room" -> BL-007
- **MOD-003** — "Add a new fixed-asset stock record" -> BL-009
- **MOD-003** — "Update an existing fixed-asset stock record" -> BL-010
- **MOD-003** — "Issue a fixed asset to a room" -> BL-011
- **MOD-004** — "Search fixed assets by one of five criteria" -> BL-012
- **MOD-004** — "Search personnel by first/last name" -> BL-013
- **MOD-005** — "View a per-room asset-assignment report" -> BL-014
- **MOD-005** — "Print the current report" -> BL-015

Named workflows (functional-spec.md, Workflows section), one line per workflow:

- **MOD-001** — "Login (branches on credential validity)" -> BL-001
- **MOD-003** — "Asset Assignment and Stock Decrement (Composite Flow)" -> BL-011
- **MOD-002** — "Room-to-Personnel Assignment (linear)" -> BL-008
- **MOD-004** — "Search (branches on selected criterion)" -> BL-012
- **MOD-005** — "Room Occupancy Report and Print (linear, with a print-side error branch)" -> BL-014, BL-015

**Orphaned:** none

### Open Questions Blocking Readiness

- CQ-023 (unanswered DEFECT, no DR-NNN citation) — blocks BL-007 (Room Delete's referenced-room delete case: unhandled crash vs. silent orphan).
- CQ-024 (unanswered DEFECT, DR-003) — blocks BL-003 (Admin gate result when more than one tblKullanicilar row matches the same credentials with different YetkiID values).
- CQ-025 (unanswered DECISION, no DR-NNN citation) — blocks BL-008, BL-012, BL-013, BL-014 (row order of every multi-row selection/results/report grid these items render).
- CQ-026 (unanswered DEFECT, DR-001) — blocks BL-011 (whether tblDemirbas.Adet has a CHECK constraint, and thus GuncelleAdet()'s own outcome when called with a quantity exceeding stock).
- CQ-027 (unanswered DEFECT, DR-004) — blocks BL-001, BL-005, BL-009, BL-010, BL-011, BL-012 (whether the ErrorProvider display and the actual gating check for every DR-004 screen should be consolidated into one validator or left as two independent paths).
- CQ-028 (unanswered DEFECT, no DR-NNN citation, not promoted from a PQ) — blocks BL-011 (whether the assignment-insert and stock-decrement writes must be wrapped in a single transaction).

### Module Coverage Rollup

_Bash-computed by counting the per-module capability-coverage lines above. Bullet granularity is unchanged — every bullet is still accounted for individually; this only rolls those counts up per module._

- **MOD-001 — Authentication & Navigation:** 0/6 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-002 — Room Management:** 0/6 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-003 — Asset Assignment & Stock:** 0/4 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-004 — Search:** 0/3 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-005 — Reporting & Print:** 0/3 capability bullets covered; 0 excluded; 0 orphaned


### UI Screen Coverage (SCR)

_Bash-computed from .specclaw/ui/ui-inventory.md against every active item's own SCR-### citations (plus this run's SCR-OUT-OF-SCOPE directives). 12 screen(s) under UI fidelity policy FAITHFUL._

- **SCR-001** — Login (`GİRİŞ_EKRANI`) → BL-001
- **SCR-002** — Main Menu (`frmAnaMenu`) → BL-002, BL-003
- **SCR-003** — Search (`frmAramalar`) → BL-012, BL-013
- **SCR-004** — Asset Assignment (`frmDemirbasIslem`) → BL-011
- **SCR-005** — Reporting & Print (`frmRapor`) → BL-014, BL-015
- **SCR-006** — Room Assignment (`frmOdaTanimlama`) → BL-008
- **SCR-007** — Admin Panel (`frmAdmin`) → BL-004
- **SCR-008** — Stock / Asset Add (`frmStokEkleme`) → BL-009
- **SCR-009** — Stock / Asset Update (`frmStokGuncelleme`) → BL-010
- **SCR-010** — Room Add (`frmOdaEkle`) → BL-005
- **SCR-011** — Room Delete (`frmOdaSil`) → BL-007
- **SCR-012** — Room Update (`frmOdaGuncelle`) → BL-006

**Unmapped:** none

## Stub Retirement

<!--
  Bash-computed every run from .specclaw/analysis/module-stubs.md, never
  agent-narrated. For every ACTIVE or RETIRING dependency-bypass stub
  (templates/CONTRACT.md (m)): is the thing it substitutes built yet, and if
  so, exactly what does it take to retire it?

  THE TRIGGER IS A DECLARED SIGNAL, NOT PROSE. A stub becomes "ready to
  retire" only when the item it substitutes carries a line beginning "BUILT:"
  inside its own "**Status notes (human-added):**" block — e.g.
  "BUILT: PR #42, merged 2026-08-10". Free text is not parsed: specclaw
  records no built state of its own, and reading "done last week" as a
  completion signal would be exactly the guess the bypass mechanism exists
  to prevent. When a stub substitutes a whole MOD-###, EVERY active item of
  that module must carry the signal — a module is not built because one of
  its items is.

  WHO DOES WHAT. Retirement is a human/Claude handoff, and each step below
  names its actor. In short: a human decides the stub is gone and removes
  the code; Claude re-runs the replays and, only on a clean run, flips the
  registry entry to RETIRED citing that run id; a human decides what to do
  with a FAIL. Claude never removes stub code on its own initiative and
  never retires an entry on an unclean run.

  The three-state flow (ACTIVE -> RETIRING -> RETIRED) exists because with
  only two states the run that PROVES a stub is gone is itself stamped
  tainted, and flipping to RETIRED first leaves a failing re-replay falsely
  marked retired. See CONTRACT.md (m.4).

  This section changes no Gate, no Verification, and no ordering. It is a
  work list.
-->

_No active dependency bypass stubs. Every item is being built on the real modules it depends on._

## Item Splits

<!--
  Bash-computed every run from .specclaw/analysis/item-splits.md, never
  agent-narrated (templates/CONTRACT.md (o)). Which backlog items are
  PARTIALLY BUILT, what each is still missing, and — once every blocked-until
  item carries a declared "BUILT:" note — the exact steps to resume.

  A SPLIT IS NOT A STUB. Nothing was faked, so nothing is tainted and there is
  nothing to retire. What a split puts in question is whether the ITEM IS
  FINISHED, which is why it gets its own section rather than a row in Stub
  Retirement.

  THE SAME DECLARED TRIGGER as stub retirement: an entry becomes
  READY-TO-RESUME only when every id in its "Blocked until" list carries a
  literal "BUILT:" line in that item's own Status-notes block. Prose is never
  parsed. Unlike stub retirement, that transition is WRITTEN by bash here (a
  single Status-line rewrite, one direction only) — it is a pure function of
  declared data, so there is no human judgement to defer to, and a stale
  ACTIVE would be indistinguishable from "nobody got round to it".

  COMPLETE is a handoff, not a computation: it needs a clean
  /specclaw:bf-replay --item BL-### run to cite, and split-update refuses it
  straight from ACTIVE.

  This section changes no Gate, no Verification, and no ordering. It is a
  work list.
-->

_No item splits. Every backlog item is being built whole._

## Change Report

<!--
  Populated only by /specclaw:bf-rebuild-plan --refresh — bash-computed by
  diffing this run's fresh Gate/Verification against the prior file's own
  stored Gate:/Verification: lines, never agent-narrated. On a first-ever
  run this section reads "Not applicable."
-->

**Newly unblocked:** BL-003, BL-011, BL-004
**Newly verifiable:** BL-003, BL-011, BL-012, BL-013, BL-001, BL-004, BL-005, BL-009, BL-010
**Struck this run:** none
**Deferred this run:** none
**Revised this run:** BL-003, BL-007, BL-008, BL-011, BL-012, BL-013, BL-014
**Added this run:** none
**Recommended next item to propose:** None — every item is BLOCKED or no active items remain.
