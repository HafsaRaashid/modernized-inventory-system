# Decisions: InventoryTrackingSystem

**Date generated:** 2026-08-28
**Source:** .specclaw/analysis/clarifications.md

<!--
  This is the clean, pinnable decision record /specclaw:bf-clarify --resolve
  produces from clarifications.md's answered questions, swept across all
  three question families (CQ-NNN/SQ-NNN/UQ-NNN). Every entry below is a
  mechanical transcription of an already-answered question — the
  Answer/Decided by/Date fields are transcribed, never reinterpreted. Each
  entry carries a **Family:** line (Extracted | Standard bank | Custom
  (per-repo)) derived mechanically from the question's ID prefix, so a
  reader can tell at a glance whether a decision came from this repo's own
  code, the plugin's standard bank, or a per-repo custom question.
  Re-running --resolve is idempotent: it always reflects the current state
  of clarifications.md's answered blocks, replacing this file's prior
  content wholesale (the prior version is archived, never lost).

  Pin this file — add `.specclaw/analysis/decisions.md` to config.yaml's
  `context.pin` (raise `max_lines` accordingly) and `git add` it, so every
  downstream /specclaw:propose, /specclaw:plan, and /specclaw:build cites
  these decisions as grounding instead of re-deriving them. Discovery
  enumerates via `git ls-files` — an untracked file is invisible to it.
-->

## Decisions

### CQ-001 — Does the checked-in DemirbasTakip.mdf/_log.ldf SQL Server data file pair represent a container the rebuild target must reproduce one-to-one, or a disposable local-dev artifact?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** (a) — settled by SQ-002 (keep SQL Server) and SQ-005 (migrate all existing data): the .mdf/.ldf pair is treated as real data to migrate. Direct schema inspection also confirmed real row counts (6 rooms, 5 assets, 9 users, etc.) — small but genuine, not obviously placeholder junk.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-001 (bf-architecture-analyst, Trigger T5) — L2 container "SQL Server (DemirbasTakip DB)" in architecture.md

### CQ-002 — Is the WinForms client and the SQL Server database intended to be co-located on the same host (per the literal "localhost" in the hardcoded connection string), or does this only reflect an un-updated dev default?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Superseded by SQ-001 (target platform: Web application) and SQ-003 (hosting: self-hosted/on-prem, running locally) — the rebuild's client/server topology is now a standard web-app shape (browser client + server-hosted API + DB), not the legacy desktop-app "localhost" pattern either option here describes.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-002 (bf-architecture-analyst, Trigger T6) — L2 container edge "WinForms Desktop App --> SQL Server (DemirbasTakip DB)" in architecture.md

### CQ-003 — Does tblOdaDemirbasAtama represent one uniform row-shape (room-responsibility record, optionally later carrying asset-issue columns) or two distinct kinds of rows sharing one table (a room-responsibility row with no DemirbasID/AlinanAdet, and a separate asset-issue row with no independent meaning of its own)?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Resolved by direct schema inspection (attached DemirbasTakip.mdf to a SQL Server container): option (a) confirmed. `tblOdaDemirbasAtama` has a single surrogate PK (`OdaDemirbasAtamaID`, int NOT NULL) with `OdaID`, `DemirbasID`, `AlinanAdet`, and `PersonelID` all nullable — one uniform, genuinely mixed-purpose row-shape. No discriminator column exists.
- **Decided by:** HafsaRaashid (via schema inspection)
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-003 (bf-domain-analyst, Trigger T3) — Entity RoomAssetAssignment (tblOdaDemirbasAtama) in domain-model.md — its field list, the optionality of DemirbasID/AlinanAdet, and the Room/Personnel/FixedAsset relationship cardinalities into it

### CQ-004 — Is keying the room UPDATE/DELETE statements by OdaAdi (room name) rather than OdaID (primary key) an intentional design choice, or a legacy defect that a rebuild should silently correct?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** (a) Intentional — preserve name-based matching (`OdaAdi`) in the rebuild. Given schema inspection confirmed no existing unique constraint on `OdaAdi` (see CQ-018), the rebuild must add a real uniqueness constraint/validation on the room-name field to make name-based matching safe.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-004 (bf-domain-analyst, Trigger T4) — DR rules for Room CRUD in domain-model.md (Room entity, MOD-002 in module-map.md)

### CQ-005 — Is the Room Assignment screen's lack of any empty-selection guard or try/catch around its INSERT an accepted "assumes valid state" design, or a defect that should not be reproduced in the rebuild?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** (b) Defect — add the same empty-selection guard/try-catch pattern used by every other mutating screen. Schema inspection confirmed `OdaID`/`PersonelID` in `tblOdaDemirbasAtama` are nullable, so the legacy app would silently insert an orphaned null-assignment row rather than crash — a real (if quiet) data-integrity gap worth closing in the rebuild.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-005 (bf-domain-analyst, Trigger T4) — frmOdaTanimlama.cs capability/workflow in functional-spec.md (Room Assignment), MOD-002 in module-map.md

### CQ-006 — Which module owns the Personnel entity (tblPersonel) for migration/acceptance purposes — Room Management (MOD-002) or Asset Assignment & Stock (MOD-003)?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Follow the legacy structure — don't force a single owner. Personnel is genuinely shared, externally-provisioned reference data (consistent with CQ-012: no admin CRUD for Personnel in this rebuild either), read by both MOD-002 and MOD-003 exactly as the legacy app does.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-006 (bf-domain-analyst, Trigger T3) — MOD-002, MOD-003 (both contest ownership of Personnel) in module-map.md

### CQ-007 — Which module owns the RoomAssetAssignment entity (tblOdaDemirbasAtama) — Room Management (MOD-002), which writes room-responsibility rows to it, or Asset Assignment & Stock (MOD-003), which writes asset-issue rows to it?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Follow the legacy structure — preserve the dual-write pattern exactly as today: MOD-002 (Room Management) writes room-responsibility rows, MOD-003 (Asset Assignment & Stock) writes asset-issue rows, both to the same table. No forced single owner.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-007 (bf-domain-analyst, Trigger T3) — MOD-002, MOD-003 (both write to RoomAssetAssignment/tblOdaDemirbasAtama) in module-map.md — see also PQ-003, which covers the same table's underlying row-shape ambiguity at the domain-model level

### CQ-008 — Is Room Add's post-save "clear the room-name field" behavior (as described in functional-spec.md) real, or is the clearing loop actually dead code that never touches the field?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** (a) Defect — fix it so the room-name field actually clears after a successful add, matching functional-spec.md's documented intent.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-008 (/specclaw:bf-ui (bf-ui-analyst), Trigger T2) — SCR-010 (Room Add) in ui-inventory.md; functional-spec.md's Room Add capability line (functional-spec.md:26, "clears every TextBox child control")

### CQ-009 — What font (family/size) actually renders across these 12 WinForms screens, given that none of them ever sets a Font property explicitly, and one screen (frmDemirbasIslem) declares a different design-time scaling metric than all its siblings?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** (a) All 12 screens target one consistent OS-default font (e.g. the target platform's equivalent of Segoe UI); frmDemirbasIslem's differing AutoScaleDimensions is treated as a harmless historical artifact, not a real design difference to preserve.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Promoted from PQ-009 (/specclaw:bf-ui (bf-ui-analyst), Trigger T6) — design-tokens.json omitted[] typography entry (all SCR-001 through SCR-012, global); ui-inventory.md Named Gaps 4–5

### CQ-010 — SQL Injection via String Concatenation in Auth, Authorization, Search, and Reporting Queries

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Settled by SQ-004 (add real authentication/authorization, sized to the platform): fix — parameterize every query path from the start.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** codebase-report.md § Risks/Tech-Debt ("SQL injection via string concatenation..."); architecture.md § Components (L3) ("Inconsistent query safety across components, confirmed directly"); frmGiris.cs, frmAnaMenu.cs, frmAramalar.cs, frmRapor.cs

### CQ-011 — Plaintext Password Storage and Comparison

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Settled by SQ-004 (add real authentication/authorization, sized to the platform): fix — hash and salt passwords; migrate/reset the 9 existing accounts since their plaintext values can't feed a hash comparison directly.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** codebase-report.md § Risks/Tech-Debt ("Passwords stored/compared in plaintext"); domain-model.md § Entities → User; frmGiris.cs

### CQ-012 — No CRUD Anywhere for User, Department, Personnel, or AssetType

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** (2) Preserve the legacy assumption — no admin CRUD screens for User/Department/Personnel/AssetType in this rebuild; continue provisioning them via direct DB access/a separate tool.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** domain-model.md § Entities (User, Department, Personnel, AssetType Named Gaps); functional-spec.md § Named Gaps items 1–2

### CQ-013 — Fiyat (Price) Column's True Stored Data Type Unconfirmed

- **Type:** DATA
- **Family:** Extracted
- **Decision:** Resolved by direct schema inspection: `tblDemirbas.Fiyat` is `money` (precision 19, scale 4), nullable. Rebuild should use a matching monetary type (e.g. `decimal(19,4)`).
- **Decided by:** HafsaRaashid (via schema inspection)
- **Date:** 2026-08-19
- **Source:** domain-model.md § Entities → FixedAsset ("Field semantics: Fiyat is captured as free-typed text... no code path in scope ever parses it to a numeric type, so its true stored column type could not be confirmed from the app code alone")

### CQ-014 — Comma-Only Decimal Separator in Numeric Keypress Filters — Rationale Unconfirmed

- **Type:** MECHANICAL
- **Family:** Extracted
- **Decision:** Adopt as-is — keep comma-only decimal filtering, matching legacy behavior exactly.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** domain-model.md § Business Rules → DR-005

### CQ-015 — frmStokEkleme's HarfGirisiKontrol Declared But Never Wired

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Reproduce as-is — applying the faithful-by-default policy (SQ-012).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** domain-model.md § Business Rules → DR-006; functional-spec.md § Named Gaps item 6

### CQ-016 — Dead Price-Validation Code (FiyatDogruMu) With Unexplained Single-Space Edge Case

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** (3) Drop entirely — it was never live in production, so it sets no behavioral precedent.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** domain-model.md § Business Rules → DR-007; functional-spec.md § Named Gaps item 7; Test1.cs, UnitTestProject1/UnitTest1.cs

### CQ-017 — No Confirmation Dialog Before Room Deletion

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Reproduce as-is (no confirmation dialog) — applying the faithful-by-default policy (SQ-012).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** functional-spec.md § Capabilities → Room Delete; § Named Gaps item 8

### CQ-018 — Uniqueness Constraint(s) Implied by Generic Duplicate-Record Error Messages

- **Type:** DATA
- **Family:** Extracted
- **Decision:** Resolved by direct schema inspection: **no unique constraint exists at all** on `OdaAdi` or `DemirbasAdi` — only the primary key on each table's ID column. The app's generic "duplicate record" catch-block messages are not backed by any real DB constraint (they're either misleading or catching an unrelated error). `OdaAdi` is also nullable. Rebuild should add real uniqueness constraints on both name fields rather than assuming they already exist.
- **Decided by:** HafsaRaashid (via schema inspection)
- **Date:** 2026-08-19
- **Source:** functional-spec.md § Named Gaps item 9; § Capabilities → Room Add, Stock/Asset Add

### CQ-019 — Orphaned frmAdminsilinecek.resx With No Matching Form

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** (1) Treat as vestigial dev clutter — no rebuild action.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** functional-spec.md § UI Inventory (closing note); § Named Gaps item 10

### CQ-020 — Unanalyzed Deneme.smproj/Deneme.smp.old Files May Hold the Authoritative DB Schema

- **Type:** DATA
- **Family:** Extracted
- **Decision:** Investigated (option 1). These are **SourceMonitor** static code-metrics tool project files (a C/C++/C#/Java complexity/LOC analysis tool) — unrelated to the database schema. "Deneme" was just the metrics-run's project name. They do not resolve CQ-013/CQ-018; the real schema was instead confirmed directly by attaching `DemirbasTakip.mdf` to a SQL Server container (see CQ-013, CQ-003, CQ-018).
- **Decided by:** HafsaRaashid (via investigation)
- **Date:** 2026-08-19
- **Source:** functional-spec.md § Named Gaps item 11

### CQ-021 — Broken itextsharp Reference and Bitmap-Based Printing — PDF/Export Target-Gap

- **Type:** TARGET-GAP
- **Family:** Extracted
- **Decision:** Settled by SQ-009 (modern equivalent — PDF/CSV export): (1) build genuine PDF/CSV export for the Reporting screen, superseding both the legacy bitmap-print path and the broken itextsharp reference.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** codebase-report.md § Dependencies, § Risks/Tech-Debt ("Broken/missing third-party dependency"); architecture.md § System Context (L1); frmRapor.cs

### CQ-022 — Low-Confidence Inference on Intended Deployment Scale/Context

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Confirmed by SQ-003 (self-hosted/on-prem, single-tenant) and SQ-007 (single-user, matching legacy exactly): single-institution, single-tenant tool. Design accordingly — no multi-tenant/organization-scoping needed.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** codebase-report.md § Domain ("Inference (low confidence): ... suggests this was built as a small-scale institutional (e.g., school, office building) asset tracker rather than a multi-tenant or commercial retail inventory system")

### CQ-023 — Does deleting a Room that still has associated tblOdaDemirbasAtama rows throw an unhandled exception, or silently orphan the child row(s)?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** (a) An FK constraint exists and blocks the delete — confirmed empirically by running the golden-master harness against a live database: GM-029's captured fixture shows `FK_tblOdaDemirbasAtama_tblOda` rejects the DELETE with `SqlException` ("The DELETE statement conflicted with the REFERENCE constraint..."), which propagates unhandled since `frmOdaSil.cs` has no try/catch. The rebuild must treat Room Delete with existing assignments as a real, reportable error condition, not a silent orphan.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-28
- **Source:** Promoted from PQ-010 (bf-baseline-designer, Trigger T3) — frmOdaSil.cs capability/workflow in functional-spec.md (Room Delete); GM-029 in scenarios.md; MOD-002 in module-map.md

### CQ-024 — When more than one tblKullanicilar row matches the same username+password with different YetkiID values, which one determines the Admin-gate result?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** (a) Treat as unreachable in practice — no evidence of duplicate username+password pairs in migrated production data, and no scenario arranges this state; a tie-break rule is not required in the rebuild. (Note: harness work this session confirmed `YetkiID` is a `bit` column, not a string as the original finding assumed — doesn't change this answer, but narrows the column's real value space to true/false.)
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-28
- **Source:** Promoted from PQ-011 (bf-baseline-designer, Trigger T6) — DR-003 in domain-model.md; frmAnaMenu.cs ANA_MENÜ_Load; MOD-001 in module-map.md

### CQ-025 — Is row order in any multi-row list screen (Search results, Room/Personnel grids, Reporting rows) an observable requirement, or is it legitimately unspecified?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** (a) Row order is not a real requirement — the rebuild is free to choose any explicit, stable order (e.g. by primary key). The absence of any ORDER BY across the entire legacy codebase indicates this was never a considered requirement.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-28
- **Source:** Promoted from PQ-012 (bf-baseline-designer, Trigger T6) — Search (frmAramalar.cs) and Reporting (frmRapor.cs) capabilities in functional-spec.md; every multi-row dgw*Doldur() read method across frmDemirbasIslem.cs/frmOdaTanimlama.cs; MOD-003, MOD-004, MOD-005 in module-map.md

### CQ-026 — Does tblDemirbas.Adet have a CHECK constraint preventing a negative value, and if so, does GuncelleAdet() (called directly, bypassing DR-001's guard) throw an unhandled exception, or does the column silently accept negative stock?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** (a) No constraint exists — confirmed empirically by running the golden-master harness against a live database: GM-040's captured fixture shows `GuncelleAdet()` called directly (bypassing the DR-001 guard) succeeds silently, leaving `tblDemirbas.Adet` at -3 with no exception thrown. The rebuild's own stock-decrement logic must supply its own non-negative guard if this is undesired — the legacy DB schema does not enforce it.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-28
- **Source:** Promoted from PQ-013 (bf-baseline-designer (harness mode), Trigger T3) — GM-040 in scenarios.md; frmDemirbasIslem.GuncelleAdet() in domain-model.md/functional-spec.md; MOD-003 in module-map.md; error-map.md's "Unmapped Conditions" entry for GM-040

### SQ-001 — Target platform

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Web application
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-002 — Database engine and hosting

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Keep the legacy database engine as-is (SQL Server).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-003 — Hosting/deployment model

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Self-hosted / on-prem, single-tenant — runs locally, no cloud hosting needed.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-004 — Authentication/authorization approach

- **Type:** TARGET-GAP
- **Family:** Standard bank
- **Decision:** Add real authentication/authorization, sized to the target platform (hashed/salted credentials, proper session/token handling — replacing the legacy plaintext/SQL-injectable login and YetkiID flag model).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-005 — Existing production data

- **Type:** SCOPE
- **Family:** Standard bank
- **Decision:** Migrate all existing production data.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-006 — UI framework / component library

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Settled by SQ-014: React (with TypeScript) as the SPA frontend framework.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-007 — Concurrent multi-user support

- **Type:** SCOPE
- **Family:** Standard bank
- **Decision:** Single-user, matching legacy behaviour exactly.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-008 — Browser/device/OS support matrix

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Modern evergreen browsers only, no legacy support, standard accessibility (WCAG AA).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-009 — Reporting/printing/export behaviours

- **Type:** SCOPE
- **Family:** Standard bank
- **Decision:** Replace with a modern equivalent: real PDF/CSV export for the Reporting screen.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-010 — Non-functional targets

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Small scale, no special performance work needed. Confirmed by direct schema inspection: 6 rooms, 5 assets, 9 users, 9 personnel, 18 departments, 4 asset types.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-011 — Operational requirements

- **Type:** SCOPE
- **Family:** Standard bank
- **Decision:** Defer operational tooling (backups/logging/monitoring/CI-CD) to a later phase. Risk: no automated backup/recovery or observability during initial rollout — acceptable given the small, self-hosted, single-institution scope (SQ-003, SQ-010).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-012 — Fidelity default

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Faithful by default — reproduce legacy behaviour unless a specific CQ says otherwise. Consistent with the FAITHFUL UI fidelity answer (SQ-013).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-013 — UI fidelity policy

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** FAITHFUL — reproduce the legacy layout structure and colour theme exactly, within the target web platform's own rendering norms.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### SQ-014 — Target backend stack

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** ASP.NET Core Web API (C#) + Entity Framework Core against SQL Server, with a React + TypeScript SPA frontend.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** Standard bank v2 (references/clarify-standard-questions.md)

### UQ-001 — Should offline mode be supported?

- **Type:** SCOPE
- **Family:** Custom (per-repo)
- **Decision:** No — always-online is fine for this rebuild.
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** .specclaw/analysis/custom-questions.md — "Should offline mode be supported?"

### UQ-002 — Do we need a mobile app eventually?

- **Type:** DECISION
- **Family:** Custom (per-repo)
- **Decision:** No — web-responsive is enough. Consistent with SQ-001 (target platform: Web application).
- **Decided by:** HafsaRaashid
- **Date:** 2026-08-19
- **Source:** .specclaw/analysis/custom-questions.md — "Do we need a mobile app eventually?"

## ADR Promotion Candidates

- **CQ-004** — Room CRUD Keyed by Name with Enforced Uniqueness — Preserves an unusual natural-key design choice and commits to adding a constraint the legacy DB never had; worth recording for future maintainers
- **CQ-006** — No Forced Single-Module Ownership for Shared Reference Data (Personnel) — Establishes a domain-modeling principle departing from the module-map's default single-owner invariant, applicable beyond this one entity
- **CQ-007** — Preserve Dual-Write Pattern for RoomAssetAssignment Across Modules — Departs from the module-map single-owner invariant deliberately; future contributors need the rationale recorded
- **CQ-010** — Parameterize All SQL Queries to Eliminate Injection Vectors — Security-critical fix spanning Auth, Main Menu, Search, and Reporting; worth a standalone record even though motivated by SQ-004
- **CQ-011** — Hash and Salt Credentials; Migrate Existing Accounts — Security-critical decision with a concrete data-migration/reset consequence for all 9 existing accounts
- **CQ-012** — No Administrative CRUD for Reference Data (External Provisioning Preserved) — Scope decision with lasting architectural consequence: admins must manage Users/Departments/Personnel/AssetTypes outside the rebuilt app
- **CQ-018** — Add Real Uniqueness Constraints on Room and Asset Name Fields — Legacy DB enforces neither constraint despite misleading duplicate-record error messages; rebuild schema must deliberately add both, affecting two entities
- **CQ-021** — Build Genuine PDF/CSV Export, Replacing Legacy Bitmap-Print Path — New target-platform capability replacing a broken/vestigial legacy dependency; affects Reporting screen design
- **CQ-023** — Room Delete Must Handle FK-Constraint Violation as a Reportable Error — Empirically confirmed unhandled-crash defect on a destructive operation; the rebuild must deliberately design error handling here rather than reproduce a silent crash
- **CQ-025** — List/Grid/Report Row Order Is Not a Requirement, Use Explicit Stable Ordering — Cross-cutting design principle applied across Search, Reporting, and every multi-row grid; prevents future ambiguity about incidental legacy ordering
- **CQ-026** — Stock Decrement Must Enforce Its Own Non-Negative Guard — Empirically confirmed the legacy DB has no CHECK constraint and silently allows negative stock; the rebuild must deliberately add integrity enforcement absent in legacy
- **SQ-001** — Target Platform: Web Application — Foundational platform decision every other shaping decision depends on
- **SQ-002** — Keep SQL Server as the Database Engine — Foundational persistence decision affecting schema design and data migration tooling from day one
- **SQ-003** — Self-Hosted, On-Prem, Single-Tenant Deployment — Foundational hosting/deployment model decision
- **SQ-004** — Add Real Authentication and Authorization — Foundational security decision replacing a SQL-injectable, plaintext-password, flag-based legacy auth model
- **SQ-005** — Migrate All Existing Production Data — Foundational data-migration scope decision
- **SQ-007** — Preserve Single-User Concurrency Model Despite Web Target — Non-obvious decision given the web-platform choice; leaves known race-condition risk in stock-adequacy checks (DR-001/DR-002) deliberately undesigned-for
- **SQ-008** — Modern Evergreen Browsers Only, WCAG AA Accessibility Baseline — Sets a real, testable technical compliance bar for the rebuild's frontend
- **SQ-012** — Faithful-by-Default Fidelity Policy for Undecided Legacy Behaviors — Governs every future DEFECT-type decision not yet individually resolved; a project-wide policy worth recording
- **SQ-013** — FAITHFUL UI Fidelity Policy — Foundational UI decision activating the screenshot-capture workstream and constraining the rebuild's actual appearance
- **SQ-014** — Target Backend Stack: ASP.NET Core Web API + EF Core + SQL Server, React+TS Frontend — Foundational technology-stack decision required before any scaffolding can begin


## Outstanding Questions

- **CQ-027** — Duplicate, decoupled validation paths for required-field checks (DR-004) (Type: DEFECT, Family: Extracted, Blocking: no)
- **CQ-028** — Non-atomic multi-connection write in Asset Assignment & Stock Decrement (Type: DEFECT, Family: Extracted, Blocking: no)
