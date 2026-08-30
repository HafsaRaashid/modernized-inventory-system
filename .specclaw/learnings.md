# Learnings: user-login

Build learnings, spec gaps, and patterns discovered.

**Categories:** spec_gap | design_gap | pattern | best_practice | agent_issue

---

## [L1] design_gap — tasks.md never assigned a task to wire AuthProvider aroun...

**When:** 2026-08-28 10:21 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
tasks.md never assigned a task to wire AuthProvider around <App/> in main.tsx, even though multiple new components (App's route guard, Login.tsx, AppShell.tsx) depend on useAuth(). Both the T13 and T14 build agents independently discovered this gap at runtime and flagged it; it was fixed directly outside the numbered task list.

### Action
When a spec introduces a new React context whose hook is consumed by components in different tasks/waves, add an explicit task for wiring the Provider into the app root (main.tsx) rather than assuming it falls under one of the consuming tasks' file scope.

---

## [L2] design_gap — T7 and T8 were both placed in wave 2 with T7 declared as ...

**When:** 2026-08-28 10:21 UTC
**Category:** design_gap
**Priority:** low
**Status:** pending

### Detail
T7 and T8 were both placed in wave 2 with T7 declared as depending on T6, but T6 was ALSO in wave 2 — same-wave 'depends' entries aren't sequenced by the build engine's wave loop, since a wave is treated as parallel-safe by default. Handled manually this run by running T6 first, then T7/T8.

### Action
When a task depends on another task, put it in a LATER wave, never the same wave, even if file-overlap analysis says they're technically parallel-safe — 'depends' should always imply a wave boundary, not just a same-wave ordering hint.

---

## [L3] design_gap — T9's DI registration used AddSingleton for JwtTokenServic...

**When:** 2026-08-28 10:21 UTC
**Category:** design_gap
**Priority:** low
**Status:** pending

### Detail
T9's DI registration used AddSingleton for JwtTokenService, which resolves lazily by default in ASP.NET Core — the design.md-stated mitigation ('fails at startup if the signing key is missing/short') did not actually trigger until first login. Fixed directly with an eager app.Services.GetRequiredService<JwtTokenService>() call right after builder.Build().

### Action
When a spec/design states a service 'fails at startup' as a stated risk mitigation, the task implementing DI registration should explicitly call out that AddSingleton/AddScoped alone is lazy and doesn't achieve that on its own — add an explicit eager-resolution step to the task's notes, or make it its own acceptance criterion.

---

## [L4] design_gap — specclaw-build setup created the specclaw/main-menu featu...

**When:** 2026-08-28 10:43 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
specclaw-build setup created the specclaw/main-menu feature branch from origin/master (last pushed at BL-001's completion) rather than local master, silently dropping 3 local-only commits including the change's own spec/design/tasks.md. Caught immediately because build-context reported the files missing; fixed with git merge master --ff-only before the branch had any commits of its own.

### Action
Push to origin before running /specclaw:build if git.strategy is branch-per-change and local master is ahead of origin — or verify the new branch's log includes the change's own plan commit before starting wave 1.

---

## [L5] design_gap — Program.cs's original JWT bearer options captured Jwt:Sig...

**When:** 2026-08-28 16:27 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
Program.cs's original JWT bearer options captured Jwt:SigningKey/Jwt:Issuer into local variables read from builder.Configuration BEFORE builder.Build() ran, so any configuration source layered on after CreateBuilder (e.g. a WebApplicationFactory test override) was invisible to token validation even though JwtTokenService (DI-resolved post-Build) saw it fine. Caused spurious 401s in integration tests.

### Action
Read Jwt:SigningKey/Jwt:Issuer lazily inside the AddJwtBearer options delegate (off the captured builder.Configuration ConfigurationManager reference) instead of into eager local variables, so any config layered in before Build() completes is picked up consistently by both token issuance and validation.

---

## [L6] agent_issue — specclaw-build setup branched specclaw/admin-panel from s...

**When:** 2026-08-28 16:44 UTC
**Category:** agent_issue
**Priority:** high
**Status:** pending

### Detail
specclaw-build setup branched specclaw/admin-panel from stale origin/master (last pushed at BL-002's merge) instead of local master, which already had BL-003's merge — this silently reverted MainMenu.tsx's working-tree content to its pre-BL-003 state and left .specclaw/changes/admin-authorization-gate/ untracked on the new branch. Second occurrence of the exact issue logged as L4 during BL-002's build.

### Action
Caught before any task work began by diffing uncommitted changes against local master, then fixed with git stash -u; git merge --ff-only master; git stash pop. Recurs on every new branch-per-change change while local master stays ahead of origin/master with no commits pushed. The durable fix is to push master to origin right after each merge, or have specclaw-build setup branch from local master instead of a remote-tracking ref.

---

## [L7] best_practice — Spec/design for BL-004 needed no adjustment during build ...

**When:** 2026-08-30 04:00 UTC
**Category:** best_practice
**Priority:** low
**Status:** pending

### Detail
Spec/design for BL-004 needed no adjustment during build — RequireAdmin's three-state loading pattern and AdminPanel's button-grid structure were specified precisely enough to implement verbatim. Parallel wave-3 test agents that ran vitest themselves before reporting done caught zero issues, confirming self-verifying test agents are reliable for this repo.

### Action
None — reinforces current spec/design detail level and test-agent self-verification practice

---

## [L8] design_gap — design.md incorrectly assumed EF Core's InMemory provider...

**When:** 2026-08-30 05:15 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
design.md incorrectly assumed EF Core's InMemory provider enforces HasIndex().IsUnique() the same way a real unique index does. It does not — InMemory only enforces uniqueness for primary/alternate keys (HasAlternateKey), not for arbitrary indexes. T6's agent discovered this empirically and worked around it with a test-only SaveChangesInterceptor (RoomsControllerTests.cs) that simulates the DbUpdateException a real SQL Server unique-index violation produces, so RoomsController's actual catch(DbUpdateException) path is genuinely exercised.

### Action
Future backlog items relying on a DB-level unique constraint (e.g. any duplicate-name check) must plan for this InMemory limitation in their design.md up front — either the same simulate-via-interceptor technique, or noting that AC coverage for uniqueness needs a real relational provider (e.g. SQLite in-memory, which DOES enforce unique indexes) instead of EF Core InMemory

---

## [L9] best_practice — This was the first full-stack (entity+migration+endpoint+...

**When:** 2026-08-30 05:15 UTC
**Category:** best_practice
**Priority:** low
**Status:** pending

### Detail
This was the first full-stack (entity+migration+endpoint+screen) backlog item in the rebuild, following three pure-frontend-routing items. Spec/design's decision to generalize RequireAdmin (BL-004's hardcoded AdminPanel guard) into a reusable children-accepting wrapper worked cleanly — AC-12's regression check passed with zero changes needed to existing /admin tests, confirming the refactor was behavior-preserving.

### Action
Continue generalizing shared guards/patterns proactively when a second consumer appears, rather than duplicating; this pattern will keep paying off for BL-006/007/009/010's own admin-gated routes

---

## [L10] best_practice — Second consecutive backlog item to extend an existing con...

**When:** 2026-08-30 06:54 UTC
**Category:** best_practice
**Priority:** low
**Status:** pending

### Detail
Second consecutive backlog item to extend an existing controller/API-client file rather than create a parallel one (RoomsController gained List/Update alongside Create; rooms.ts gained listRooms/updateRoom alongside createRoom). Reusing BL-005's AdminAuthorizationExtensions, RequireAdmin guard, and DuplicateRoomNameSimulatingInterceptor test helper meant this item needed zero new auth/migration infrastructure — smallest item yet (5 tasks/3 waves vs BL-005's 7/4).

### Action
Continue favoring extension over parallel new files/controllers when a capability is a natural addition to an existing resource (same entity, same admin-gating story) — matches this project's own module-cohesion pattern

---

## [L11] design_gap — The DuplicateRoomNameSimulatingInterceptor built for BL-0...

**When:** 2026-08-30 06:54 UTC
**Category:** design_gap
**Priority:** low
**Status:** pending

### Detail
The DuplicateRoomNameSimulatingInterceptor built for BL-005's Create-only duplicate-name test only checked EntityState.Added, silently missing rename (Modified) collisions until BL-006's Update tests needed it — caught and fixed by the T4 test agent (now checks Added|Modified, excluding the candidate's own row by Id so a no-op same-name rename isn't flagged).

### Action
When a shared test double is built for one operation (Create), assume future sibling operations (Update/Delete) on the same entity will need it too, and design it to cover all EntityStates from the start rather than only the one in scope at the time

---

## [L12] best_practice — Third consecutive backlog item extending existing RoomsCo...

**When:** 2026-08-30 10:02 UTC
**Category:** best_practice
**Priority:** low
**Status:** pending

### Detail
Third consecutive backlog item extending existing RoomsController/rooms.ts/RequireAdmin rather than creating parallel infrastructure. Smallest item yet (5 tasks, same shape as BL-006) since it reused GET /api/rooms from BL-006 for the selector and needed zero new auth/migration work.

### Action
Continue favoring extension for same-entity capabilities; the Room CRUD family (Add/Update/Delete) now shares one controller and one screen pattern cleanly

---

## [L13] design_gap — BL-007's acceptance basis (CQ-023) required a cross-modul...

**When:** 2026-08-30 10:02 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
BL-007's acceptance basis (CQ-023) required a cross-module dependency (RoomAssetAssignment, owned by MOD-003/BL-011) that the backlog's own 'Depends on:' field never declared, so specclaw-bf-rebuild-collect bypass-check did not surface it automatically. Separately, split-append mechanically refuses to record an item-split for any item whose acceptance basis cites zero DR-### rules (BL-007 cites only CQ-### decisions) -- so the chosen item-split strategy for deferring CQ-023's FK-guard could only be recorded in proposal.md/spec.md prose, not as a formal IS-### with automatic blocked-until tracking.

### Action
When a bypass-check reports 'ok-built' or an empty dependency list for a screen-bearing item, still read the item's own acceptance-basis prose for cross-module entity references the Depends-on field might have missed -- and expect split-append to refuse a split for any item whose acceptance basis has no DR-### citation, since the tool can only partition against rule ids

---

## [L14] design_gap — RoomAssignmentsController used the default [Route("api/[c...

**When:** 2026-08-30 10:46 UTC
**Category:** design_gap
**Priority:** high
**Status:** pending

### Detail
RoomAssignmentsController used the default [Route("api/[controller]")] convention, which resolves to the literal PascalCase class name minus 'Controller' with NO kebab-casing -- /api/RoomAssignments, not /api/room-assignments. Every prior controller (Rooms, Departments, Personnel) has a single-word name, so ASP.NET Core's case-insensitive route matching silently papered over the mismatch; this is the first COMPOUND-word controller name, where the missing hyphen is a structural difference case-insensitivity cannot bridge. The frontend's roomAssignments.ts already called /room-assignments (matching spec/design/tasks), so the mismatch would have 404'd in real use. Caught by the T6 test-writing agent when its tests failed against the hyphenated path; fixed by adding an explicit [Route("api/room-assignments")] attribute.

### Action
For any FUTURE controller whose name is a compound/multi-word noun (not just Rooms/Departments/Personnel-style single words), explicitly verify or override the route with [Route("api/kebab-case-name")] rather than trusting the [controller] token -- don't rely on case-insensitive matching to save a compound name

---
