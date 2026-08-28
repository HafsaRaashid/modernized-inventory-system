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
