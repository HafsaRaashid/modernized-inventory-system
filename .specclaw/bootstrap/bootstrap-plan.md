# Target Foundation Plan: InventoryTrackingSystem

**Date:** 2026-08-28
**Repo:** the NEW (rebuild) repository
**Written by:** `bf-bootstrap-architect`, inside `/specclaw:bf-bootstrap`

## Resolved Target Stack

| Part | Resolution | Decision id | Source |
|---|---|---|---|
| Target platform | Web application (browser SPA + server-hosted API + DB) | SQ-001 | `.specclaw/analysis/decisions.md` |
| Backend | ASP.NET Core Web API (C#), .NET 8 | SQ-014 | `.specclaw/analysis/decisions.md` |
| ORM / data access | Entity Framework Core | SQ-014 | `.specclaw/analysis/decisions.md` |
| Database engine | SQL Server (kept as-is from legacy) | SQ-002 | `.specclaw/analysis/decisions.md` |
| Frontend framework | React + TypeScript SPA | SQ-014 / SQ-006 (SQ-006 explicitly deferred to SQ-014's answer) | `.specclaw/analysis/decisions.md` |
| Frontend build tool | Vite | Not itself decided by any SQ/CQ — an implementation detail of "how to scaffold a React+TS SPA", not an architectural fork; the standard, idiomatic tool for this exact stack combination | — |
| Hosting / deployment | Self-hosted / on-prem, single-tenant, runs locally | SQ-003 | `.specclaw/analysis/decisions.md` |
| Auth model (boundary only) | Real authentication/auth required, sized to the platform; NOT implemented here — see "Not In Scope" | SQ-004 | `.specclaw/analysis/decisions.md` |
| UI fidelity policy | FAITHFUL | SQ-013 | `.specclaw/analysis/decisions.md` |
| Scale | Small (6 rooms, 5 assets, 9 users, 9 personnel, 18 departments, 4 asset types) — no special performance/paging design needed | SQ-010 | `.specclaw/analysis/decisions.md` |
| Operational tooling (CI/CD, monitoring, backups) | Deferred to a later phase — none scaffolded here | SQ-011 | `.specclaw/analysis/decisions.md` |
| Browser matrix | Modern evergreen browsers only, WCAG AA | SQ-008 | `.specclaw/analysis/decisions.md` |

No ADRs exist anywhere in this repository (searched `docs/adr/`, `docs/decisions/`, `adr/`, and the whole tree — only `.specclaw/` and `.git/` exist at the repo root before this run), so every stack claim above is cited to `decisions.md` directly, not to an ADR.

## Structure

Two top-level directories, one per side of the stack, mirroring the module-map's shape (5 modules: MOD-001 Authentication & Navigation, MOD-002 Room Management, MOD-003 Asset Assignment & Stock, MOD-004 Search, MOD-005 Reporting & Print) without pre-committing to any of them — no module-specific folder exists yet.

```
api/
  InventoryTrackingSystem.sln
  .config/dotnet-tools.json          # local tool manifest (dotnet-ef)
  src/
    InventoryTrackingSystem.Api/            # ASP.NET Core Web API — entry point, controllers, DI composition, middleware
    InventoryTrackingSystem.Domain/         # domain layer — empty; no entity exists yet, each BL-### adds its own
    InventoryTrackingSystem.Infrastructure/ # EF Core DbContext, SQL Server wiring, future migrations
  tests/
    InventoryTrackingSystem.Api.Tests/      # xUnit

web/
  src/
    api/        # typed API client (fetch wrapper, ApiError, auth-header seam)
    components/ # cross-cutting UI conventions (ErrorBoundary)
    routes/     # routing shell only (AppShell, NotFound)
    styles/     # theme mechanism (CSS custom properties)
  tests/        # Vitest + Testing Library
```

**Layer boundaries (backend):** `Api` depends on `Infrastructure`, which depends on `Domain`. `Domain` depends on nothing. No `DbSet<T>` exists in `AppDbContext` yet — the persistence layer is wired and provably connects/migrates, but no domain entity has been modeled, because no backlog item has built one. This is the standard ASP.NET Core Web API layered-project convention (one class library per layer, referenced by the Web project), not a bespoke shape.

**Layer boundaries (frontend):** `routes/` never talks to `api/` directly except through the typed client in `api/client.ts`; components render, the client fetches. This is the conventional Vite + React + TypeScript SPA layout (no framework-specific state-management library is chosen here — none is architecturally decided, and none is needed by a routing shell with no data-bearing screen).

## Boundaries

- **Frontend → API boundary:** `web/src/api/client.ts`'s `apiFetch<T>()` is the single crossing point. It resolves the base URL from `VITE_API_BASE_URL`, JSON-encodes/decodes, and throws a typed `ApiError` on a non-2xx response. Every future capability call goes through it rather than a bare `fetch`.
- **API → domain boundary:** `InventoryTrackingSystem.Api`'s controllers depend on `InventoryTrackingSystem.Infrastructure`, which depends on `InventoryTrackingSystem.Domain`. No controller talks to `AppDbContext` directly today because `HealthController` needs no data — the reference chain exists so the first data-bearing controller does not have to invent it.
- **Domain → persistence boundary:** `AppDbContext` in `InventoryTrackingSystem.Infrastructure/Persistence/AppDbContext.cs`. Zero `DbSet<T>` properties today; the first BL-### item that introduces an entity adds its own DbSet and its own EF Core migration.
- **Auth boundary (not an implementation):** SQ-004 requires real authentication/authorization, but that is BL-001's job, always. Two seams are left for it, both explicitly commented in place:
  - Backend: `Program.cs` has no `AddAuthentication()`/`AddAuthorization()` call and no request is gated. A comment marks where BL-001 wires it in.
  - Frontend: `web/src/api/client.ts`'s `getAuthHeader()` returns an empty header set today; BL-001 is what starts populating it with a real token.
- **Error-handling conventions:** Backend — `ExceptionHandlingMiddleware` catches any unhandled exception, logs it with the request's trace id, and returns a stable `{ error, traceId }` JSON envelope. Frontend — `ErrorBoundary` catches any uncaught render error and shows a fallback instead of a blank screen; `ApiError` gives call sites a typed way to catch a failed request. No business rule is decided in either — only the shape of the failure.

## Testing Approach

- **Backend:** xUnit, in `api/tests/InventoryTrackingSystem.Api.Tests/`, run via `dotnet test`. One trivial test (`HealthControllerTests.Get_ReturnsOk`) proves the runner executes; it exercises only the health-check pillar's own endpoint.
- **Frontend:** Vitest + React Testing Library (`jsdom` environment), in `web/tests/`, run via `npm run test` (`vitest run`). One trivial test (`App.test.tsx`) renders the app shell and asserts its header text; it exercises only the frontend-shell/routing pillars.
- Both runners are wired into each side's own idiomatic project structure (`*.Tests.csproj` referenced from the solution; Vitest configured directly in `vite.config.ts`), which is the same structure `/specclaw:bf-replay`'s generated replay tests will need to live alongside once a capability exists to replay.

## Local Development Setup

Prerequisites: .NET 8 SDK, Node.js 18+, a reachable SQL Server instance (SQ-002 keeps SQL Server; SQ-003 is self-hosted/on-prem, so this is typically a local SQL Server / SQL Server Express / a local container, not a managed cloud service).

**Backend:**
```
cd api
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:Default" "<your local SQL Server connection string>" --project src/InventoryTrackingSystem.Api
dotnet build InventoryTrackingSystem.sln
dotnet run --project src/InventoryTrackingSystem.Api
```
The connection string is never committed: `appsettings.json` ships with `ConnectionStrings:Default` empty, and local development supplies the real value via `dotnet user-secrets` (stored outside the repo) or the `ConnectionStrings__Default` environment variable in any other environment.

**Frontend:**
```
cd web
cp .env.example .env.local   # optional — defaults already point at /api via the dev proxy
npm install
npm run dev
```
The Vite dev server proxies `/api/*` to `http://localhost:5080` (see `vite.config.ts`), so the SPA and the API are same-origin from the browser's point of view in both development and production (production: the API serves the SPA's own build output). No CORS policy exists anywhere in this foundation as a result.

**Database / migrations:** no migration exists yet, because no domain entity exists yet. The mechanism itself is proven by generating and applying an empty `InitialCreate` migration:
```
cd api
dotnet ef migrations add InitialCreate --project src/InventoryTrackingSystem.Infrastructure --startup-project src/InventoryTrackingSystem.Api -o Migrations
dotnet ef database update --project src/InventoryTrackingSystem.Infrastructure --startup-project src/InventoryTrackingSystem.Api
```
The first BL-### item that introduces an entity adds its own migration on top of this one.

## Smoke Verification

| Check | Command | Notes |
|---|---|---|
| `api-build` | `cd api && dotnet build InventoryTrackingSystem.sln` | Terminates on its own. |
| `api-start` | `cd api && dotnet run --project src/InventoryTrackingSystem.Api --urls http://localhost:5080 & PID=$!; sleep 5; curl -sf http://localhost:5080/api/health; kill $PID` | Starts, probes `/api/health` once, stops itself. |
| `test-backend` | `cd api && dotnet test tests/InventoryTrackingSystem.Api.Tests` | Terminates on its own. |
| `db-connect` | `cd api && dotnet ef dbcontext info --project src/InventoryTrackingSystem.Infrastructure --startup-project src/InventoryTrackingSystem.Api` | Requires a reachable SQL Server and a connection string supplied per "Local Development Setup" above — not runnable unattended without one. |
| `migrations-infra` | `cd api && dotnet ef migrations add InitialCreate --project src/InventoryTrackingSystem.Infrastructure --startup-project src/InventoryTrackingSystem.Api -o Migrations && dotnet ef database update --project src/InventoryTrackingSystem.Infrastructure --startup-project src/InventoryTrackingSystem.Api` | Proves the migrations mechanism only — the generated migration has an empty `Up`/`Down` body since no `DbSet<T>` exists yet. |
| `frontend-build` | `cd web && npm install && npm run build` | Terminates on its own (`tsc -b && vite build`). |
| `frontend-start` | `cd web && npm run build && (npm run preview -- --port 4173 & PID=$!; sleep 3; curl -sf http://localhost:4173/ > /dev/null && echo OK; kill $PID)` | Starts the built preview server, probes once, stops itself. |
| `test-frontend` | `cd web && npm run test` | `vitest run` — terminates on its own (not watch mode). |
| `frontend-to-api` | With both `api-start` and `npm run dev` running, `curl -sf http://localhost:5173/api/health` | Proves the Vite dev proxy actually reaches the backend's health endpoint end to end. |

This environment itself has no .NET SDK installed (confirmed: `dotnet --version` fails with "No .NET SDKs were found"), so none of the `dotnet`-based checks above were run by this agent. Node/npm are present (`node v24.15.0`, `npm 11.12.1`), but `npm install` was not run either, to avoid a long unattended network install inside this stage. `specclaw-bf-bootstrap smoke` is expected to execute all of the above in an environment with the prerequisites installed.

## UI Token Plumbing

SQ-013 is decided **FAITHFUL**, and `.specclaw/ui/design-tokens.json` is present. It defines exactly one token group, `TK-001` ("Login screen accent"), and that group's `scope` is `SCR-001` (the Login screen) — **not** `global`. `.specclaw/ui/design-tokens.json` contains no `global`-scoped token group at all.

Per the foundation-only token-plumbing line, only `global`-scoped groups are ever imported here, and a `SCR-###`-scoped group is never imported, even under FAITHFUL — that is exactly what a named human signs off in `ui-review.md`, per change, per screen (in this case, whoever builds BL-001/SCR-001).

**What was built:** the theme *mechanism* only — `web/src/styles/theme.css` (a `:root` block ready to hold CSS custom properties), imported once from `web/src/main.tsx`, plus the layout shell (`AppShell.tsx`) that mechanism will style. **No token value was imported.** `ui_tokens_imported` is empty in the declaration, and `ui_tokens_skipped_reason` states the reason above verbatim.

**Left to the screen-bearing items:** every actual layout structure and every token value — including all three of TK-001's colors (`login-button-background`, `login-button-foreground`, `login-form-background`) — is BL-001's (SCR-001, Login) to bring in and get signed off in `ui-review.md`, once `.specclaw/ui/screens/` and `.specclaw/ui/ui-manifest.json` exist (both are currently missing, per `rebuild-backlog.md`'s own warning).

## Not In Scope — and who owns it instead

- **Sign In / session handling, hashed-credential storage, real token/session issuance** → BL-001 (User Login). The auth boundary (empty DI seam in `Program.cs`, empty `getAuthHeader()` in `client.ts`) exists; no scheme, no login form, no hashing.
- **Main Menu navigation hub (five feature buttons) and Admin gate** → BL-002, BL-003. `AppShell` renders a static header and a placeholder message only — no navigation button, no admin-enable logic.
- **Admin Panel sub-navigation** → BL-004.
- **Room CRUD (Add/Update/Delete), Room-to-Personnel assignment** → BL-005, BL-006, BL-007, BL-008. No `Room` entity, no `Department` entity, no `tblOda`-equivalent table or DbSet exists.
- **Stock/Asset Add & Update, Asset Assignment + stock-decrement composite flow** → BL-009, BL-010, BL-011. No `FixedAsset`, `AssetType`, or `RoomAssetAssignment` entity or DbSet exists.
- **Search (by asset criteria, by personnel name)** → BL-012, BL-013.
- **Reporting & PDF/CSV export** → the Reporting module (MOD-005) item(s) in `rebuild-backlog.md`.
- **Any domain entity or EF Core `DbSet<T>`** — `AppDbContext` is intentionally empty; every entity listed above is a later item's responsibility.
- **Any domain migration** — `migrations-infra`'s smoke check only proves an empty `InitialCreate` migration applies; no schema for any real table is generated here.
- **CORS middleware** — not registered. See "Boundaries"; the same-origin dev-proxy/production-static-hosting shape (SQ-003) removes the need for one. If a future decision changes the deployment topology to a genuinely cross-origin shape, that is a new decision to record, not something this foundation silently assumed either way.

## Open Risks

- **`CORS` was scaffolded absent, not merely deferred.** If a future hosting decision splits the frontend and backend onto different origins in production (contradicting SQ-003's same-origin, self-hosted assumption), a CORS policy will need to be added at that point — this is not pre-wired.
- **UI fidelity artifacts are missing.** `rebuild-backlog.md` already flags this: `.specclaw/ui/screens/` and `.specclaw/ui/ui-manifest.json` do not exist, so every screen-bearing item is held at OPEN QUESTIONS until `/specclaw:bf-ui` runs. This foundation does not change that — the theme mechanism exists, but zero screens have been signed off against a screenshot.
- **`db-connect` and `migrations-infra` smoke checks could not be run in this environment** (no .NET SDK installed here) — they are declared with real, terminating commands for `specclaw-bf-bootstrap smoke` to execute in an environment that has the prerequisites, but this agent has not itself confirmed the EF Core/SQL Server wiring compiles or connects.
- **`npm install` was not run**, so the frontend's exact dependency resolution (lockfile) has not been generated or verified in this environment; `frontend-build`/`frontend-start`/`test-frontend` are declared but unexecuted here for the same reason.
- **Single-user concurrency (SQ-007)** is preserved by decision, not by any mechanism in this foundation — no optimistic-concurrency/locking scaffolding was added, consistent with SQ-007's explicit scope decision, but worth naming so a later reviewer does not assume it was considered and rejected rather than genuinely out of scope.
