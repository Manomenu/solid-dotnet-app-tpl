# Architecture

## The map

One line per top-level path (at most 300 characters each). `RepositoryLayoutTests` reads these
lines: a new top-level folder needs its line here, a deleted one loses it.

- `Myapp.slnx` — the .NET solution: the server and its tests.
- `Myapp.Server/` — the HTTP API (ASP.NET Core minimal APIs, Npgsql, Dapper with hand-written SQL). One folder and namespace per feature: `Api/` wires the routes, `Notes/` is the example feature, `Health/` checks the database, `Database/` is the pool and the grate migrations.
- `Myapp.Server/Database/Migrations/up/` — numbered SQL migrations, applied at start-up by grate and never edited afterwards.
- `Myapp.Server.Tests/` — xUnit v3 tests against a real PostgreSQL. Mirrors the server's folders; `Characterization/` holds behaviour recorded before changing legacy code.
- `Myapp.Gate/` — the quality machinery: the gate script, the repository guards, the criteria check, the Claude Code hooks. See "The gate" below.
- `Myapp.Web/` — the web app (React 19, shadcn/ui, Tailwind 4, Vite, TypeScript strict, pnpm). A folder per feature under `src/` (`notes/` is the example); `src/design/` is the shared design system (shadcn components, `cn`); `src/api/` the HTTP client and the generated `openapi.d.ts`.
- `compose.yaml` — the whole stack on a laptop from the same Dockerfiles: db (profile `db`), server, web; everything configured from `.env` (`.env.example`) with ASP.NET names (`Section__Key`); only web publishes a port (8190).
- `Myapp.Server/Dockerfile`, `Myapp.Web/Dockerfile` — the two images (pinned bases, locked restore, build context is the repo root); the web image is nginx serving the build and proxying `/api` to the server.
- `.dockerignore` — keeps `node_modules`, `bin`, `obj` and `.env` out of the build context.
- `.github/` — `workflows/ci.yml`: the gate (`test`), the browser tests (`e2e`), and the images built and smoke-tested (`build`); nothing is published, the registry is project-specific.
- `.vscode/` — the editor's glue, shared: recommended extensions, formatter per language, file nesting, debug launch, tasks that call `scripts/.internal`. No style rules (they live in `.editorconfig` and the formatters' configs, so Rider agrees).
- `.githooks/` — `pre-commit` runs gitleaks on what is being committed; `just sync` enables it.
<!-- template:start -->
- `.template.config/` — the `dotnet new` template definition (see the README); not copied into generated projects.
- `pack/` — packs the template as a NuGet package (`just pack-template`); not copied into generated projects.
- `scripts/.internal/init-project.proj` and `init-project.sh` — the template's post action; they run once in a new project and delete themselves.
<!-- template:end -->
- `docs/` — documentation for people working on the repo: this map, `development.md`, the specs in `visual/spec/` (their acceptance criteria live as guardrails, see AGENTS.md), decisions above one feature in `adr/`.
- `scripts/` — `scripts/.internal/` holds the everyday tools the gate and `just` run: `db.sh`, `server.sh`, `tmux-dev.sh`, `checkout-env.sh`, `api-types.sh`, `api-diff.sh`, `contracts.sh`, `e2e.sh`, `smoke.sh`, `logs.sh`; the gate refuses one that nothing calls (`ScriptsCalledTests`).
- `.claude/` — the committed Claude Code settings: a Stop hook that runs the quick gate, a PreToolUse hook that protects migrations, generated files, and the gate with the lint configs (`ALLOW_GATE_EDIT=1 claude` lifts the last).
- `.config/` — `dotnet-tools.json`: the pinned CSharpier.
- `contract/` — the API contract (`openapi.json`) and its version (`package.json`), generated and committed for consumers.
- `tmux-dev.just` — the `justfile` module of the local stack; a tool's own module is `<folder>/mod.just` (`JustModulesTests`).

Files at the root: `Directory.Build.props` (strict compiler and analyzers), `Directory.Build.rsp`
(no idle MSBuild workers), `Directory.Packages.props` (every package version, once),
`BannedSymbols.txt` (APIs the build refuses, each with what to use instead), `.editorconfig`
(every disabled rule has its reason), `global.json`, `nuget.config`, `.gitattributes`, `.gitignore`.

## The gate

Quality machinery lives apart from what it checks, for two reasons: a guard tests the whole repository, not one
project, and an agent that cannot pass a guard must not be able to change it.

- Belongs in the gate: anything that decides whether a change passes. A tool for everyday work stays in `scripts/`,
  even when the gate calls it.
- A hook refuses an agent's edit of the whole folder, so a new guard is protected from its first line. The owner lifts
  it for one session with `ALLOW_GATE_EDIT=1 claude`.
- Data that changes with every feature is the exception: `layers.json` stays editable, because its diff is one
  readable line and a protected file would stop every feature at the owner's door.
- Guards are grouped by what they check: design, code, the repository, tools. Scripts sit apart from tests.
- Some configs stay at the repository root because the tools look for them there; the hook protects them all the same.

## What the gate forbids

Each line is a build rule or a test; its message says the fix. Rules live as low as they can: types and analyzers first.

- Web (`check.sh`): tsc strict, eslint (typescript-eslint strict + import boundaries: a feature uses `api` and `design`, `design` uses no feature), vitest, prettier (with Tailwind class order), knip (dead code), `openapi.d.ts` equal to the server's OpenAPI document.
- Template (`TemplateTests`, template repo only): the placeholder is spelled `Myapp` or `myapp` only, base ports are whole numbers, port symbols are consistent, nothing that must not be copied is missing from the exclusions.
- Logs (`HealthEndpointsTests`, `LogsScriptTests`): an error response carries the `traceId` that its log lines carry; `logs.sh` reads them back.
- Editor (`VsCodeSettingsTests`): `.vscode/settings.json` has no style keys; recommended extension ids are well formed.
- Images (`ImagePinsTests`): every `FROM` and `image:` carries a version, never `latest`.
- Containers (`check.sh`): `docker compose config` resolves with and without the `db` profile; `smoke.sh` (run by CI after the stack starts) checks `/healthz` and `/api/health` of the running stack; every compose port is on 127.0.0.1 (`ComposePortsTests`).
- Build: analyzers `latest-all`, warnings are errors, unused private members (IDE0051, IDE0052, CA1823), no `#pragma warning disable` (`NoPragmaTests`).
- Banned APIs (`BannedSymbols.txt`): `DateTime.Now`/`UtcNow`, blocking on tasks, `Thread.Sleep`, `Console`, `Activator`, reflection discovery, `Type.GetType(string)`, Newtonsoft.
- Layers (`ContractsTests`): a layer uses only the layers below it, every namespace has a layer, every `*Endpoints` class is mapped from `Api`, one per namespace.
- Unlisted folders and projects (`Every_server_namespace_has_a_layer`, `UnlistedProjectsTests`): a guard over a list covers nothing left off it, so a folder or project off the list fails until it is added; shell scripts are guarded by `git ls-files`, not by a list of folders.
- Project graph (`ProjectReferencesTests`): which project may reference which and see whose internals.
- Names (`NamingTests`): unique type names, one top-level type per file named like the file.
- Forbidden names (`ForbiddenNamesTests`): no folder (web `src/` included), project, namespace or type named Common, Helpers, Utils, Manager, Shared, Core, Domain, Services, Models, Components, Hooks, Ui and the like; no `*Repository`, `*Service`, `*UnitOfWork` types.
- Packages (`PackagesTests`): no MediatR, AutoMapper, Mapster, Scrutor, mocking libraries, FluentAssertions, Newtonsoft.Json or Entity Framework; every versioned package is referenced.
- Interfaces (`InterfacesTests`): an interface has at least two implementations.
- Deleting (`SubtractTests`): nothing `[Obsolete]`, no unused non-public type.
- Escape hatches (`EscapeHatchTests`): no `dynamic`; a `!` needs a `// Why:` on its line or the one above.
- Comments (`CommentsTests`): only `// Why: ...` and tool markers; no TODO, FIXME, workaround or commented-out code; XML docs only in a `*.Contracts` project.
- Settings (`AppSettingsTests`): every section of appsettings is bound by an options class.
- Acceptance criteria (`check.sh` without `--quick` runs `criteria.sh`: the suwagent plugin's `vdoc.py criteria`, pinned, with the kinds of `criteria.json`; `CriteriaKindsTests` keeps each kind in README and AGENTS.md): each spec in `docs/visual/spec/` has an acceptance section with one unique `AC-<feature>-<n>` per criterion, each is named by a guardrail of a known kind, every id a guardrail names is in a spec, no id stands where the gate checks nothing.
- Docs (`AgentsFileTests`, `RepositoryLayoutTests`): AGENTS.md at most 100 lines, no tree drawings, no capitals shouting; CLAUDE.md is `@AGENTS.md`; every top-level folder is in the map above.
- The gate itself (`check.sh`): a ceiling per step, the quick gate fails above its budget (20 s by default) and checks only what changed.

## How code is split

The server and the web grow by feature: everything that makes one feature work sits in one folder named in the users'
terms, not by technical kind. Wiring is explicit, so grep finds every route.

- A new capability starts as a new folder, or goes into the feature it belongs to, never into a shared bucket. Shared
  code is earned by the second feature that needs it, and is named after what it is, not after who uses it.
- A new folder arrives with its first code.
- Rules live in plain modules with a test beside them; screens only wire them.
- Records an endpoint sends or receives are a contract with the outside; a row from our own SQL is not. Keep them
  apart once they differ.
- A problem the caller can fix is a ProblemDetails whose `detail` says what to do.

## Areas (when a second product area appears)

Not used today: every feature sits directly under `Myapp.Server/` in one flat order. Do not
introduce an area for the first product area — only when a second one arrives.

An area groups the features of one product area (orders beside billing). It is one entry
in the top order and has an order of its own inside. Steps, each with the test that proves it:

1. Move the feature folders of the existing area under `Myapp.Server/<Area>/` (a move only, in a
   change of its own); namespaces follow the folders (IDE0130 fails the build otherwise).
2. In `Myapp.Gate/layers.json` put the area on one level of `serverLayers` (`"Orders | Billing"`) and add an
   `Areas` dictionary: area name to the order of its features, same `a | b` syntax. Extend
   `Every_server_namespace_has_a_layer` to look one segment deeper for names in `Areas`
   and `EdgesOf` to take a prefix.
   Proof: it fails for a feature folder missing from the area's order.
3. Areas never use each other: siblings on one level are forbidden edges, which `EdgesOf` yields.
   Proof: make `Billing` call `Orders` and see the layer test fail.
4. What both areas need moves down into a shared feature below the areas (notifications
   between the areas and `Database`), and shared features never use an area (the same edges).
5. `Api/` stays above all areas and maps each area's routes in a method of its own, so grep finds them.
6. Web (phase 2): `src/<area>/<feature>/`, an `areaFeature` pattern in the `boundaries` rules of
   `eslint.config.js`, and an explicit policy for any edge between features of one area.
7. Add the area's folder to the map above in the same change.
