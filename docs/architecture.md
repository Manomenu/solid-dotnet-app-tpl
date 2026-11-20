# Architecture

## The map

One line per top-level path (at most 300 characters each). suwgate's `repository-layout` guard reads these
lines: a new top-level folder needs its line here, a deleted one loses it.

- `Myapp.slnx` — the .NET solution: the server and its tests.
- `Myapp.Server/` — the HTTP API (ASP.NET Core minimal APIs, Npgsql, Dapper with hand-written SQL). One folder and namespace per feature: `Api/` wires the routes, `Notes/` is the example feature, `Health/` checks the database, `Database/` is the pool and the grate migrations.
- `Myapp.Server/Database/Migrations/up/` — numbered SQL migrations, applied at start-up by grate and never edited afterwards.
- `Myapp.Server.Tests/` — xUnit v3 tests against a real PostgreSQL. Mirrors the server's folders; `Characterization/` holds behaviour recorded before changing legacy code.
- `Myapp.Gate/` — the .NET architecture guards (xUnit): layers, project references, interfaces, names, escape hatches, unused types. NuGet `Suwgate.Architecture` replaces them once it is published (TODO.md).
- `Myapp.Web/` — the web app (React 19, Tailwind 4, Vite, TypeScript strict, pnpm), styled by `@maniumek/suwfrontkit`. A folder per feature under `src/` (`notes/` is the example); `src/api/` the HTTP client and the generated `openapi.d.ts`. Its packages include the gate, `@maniumek/suwgate`.
- `compose.yaml` — the whole stack on a laptop from the same Dockerfiles: db (profile `db`), server, web; everything configured from `.env` (`.env.example`) with ASP.NET names (`Section__Key`); only web publishes a port (8190).
- `Myapp.Server/Dockerfile`, `Myapp.Web/Dockerfile` — the two images (pinned bases, locked restore, build context is the repo root); the web image is nginx serving the build and proxying `/api` to the server.
- `.dockerignore` — keeps `node_modules`, `bin`, `obj` and `.env` out of the build context.
- `.github/` — `workflows/ci.yml`: the gate (suwgate's reusable gate workflow), the browser tests (`e2e`), and the images built and smoke-tested (`build`); nothing is published, the registry is project-specific.
- `.vscode/` — the editor's glue, shared: recommended extensions, formatter per language, the schema of `suwgate.yml`, file nesting, debug launch, tasks that run the gate. No style rules (they live in `.editorconfig` and the formatters' configs, so Rider agrees).
- `.githooks/` — `pre-commit` runs `suwgate hook pre-commit`: gitleaks on what is being committed, then the quick gate; `just sync` enables it.
<!-- template:start -->
- `.template.config/` — the `dotnet new` template definition (see the README); not copied into generated projects.
- `pack/` — packs the template as a NuGet package (`just pack-template`); not copied into generated projects.
- `scripts/.internal/init-project.proj` and `init-project.sh` — the template's post action; they run once in a new project and delete themselves.
<!-- template:end -->
- `docs/` — documentation for people working on the repo: this map, `development.md`, the specs in `visual/spec/` (their acceptance criteria live as guardrails, see AGENTS.md), decisions above one feature in `adr/`.
- `scripts/` — `scripts/.internal/` holds the tools of this app that `just` runs: `api-types.sh`, `e2e.sh`, `slow-tests.sh`; the local stack is `suwgate dev`. The gate refuses a script that nothing calls.
- `.claude/` — the committed Claude Code settings: `suwgate hook stop` runs the quick gate when Claude stops, `suwgate hook guard-edit` protects migrations, generated files, and the gate with the lint configs (`ALLOW_GATE_EDIT=1 claude` lifts the last).
- `.config/` — `dotnet-tools.json`: the pinned CSharpier.
- `contract/` — the API contract (`openapi.json`) and its version (`package.json`), generated and committed for consumers.
- `tmux-dev.just` — the `justfile` module of the local stack; a tool's own module is `<folder>/mod.just`.

Files at the root: `suwgate.yml` (the gate), `layers.json` (the server's layers and which project may reference which),
`Directory.Build.props` (strict compiler and analyzers), `Directory.Build.rsp` (no idle MSBuild workers), `Directory.Packages.props`
(every package version, once), `BannedSymbols.txt` (APIs the build refuses, each with what to use instead), `.editorconfig`
(every disabled rule has its reason in the table below), `global.json`, `nuget.config`, `.gitattributes`, `.gitignore`.

## The gate

The gate is suwgate (npm `@maniumek/suwgate`, [its README](https://github.com/Manomenu/suwgate)): `suwgate check` runs the steps
of the profiles `suwgate.yml` names (web, dotnet, database) and its repository guards. This repository keeps only the wiring and what
is its own:

- `suwgate.yml`: the profiles, where the web app lives, the steps it skips and the exceptions, each with its reason, the edit hook's
  extra files, and the names and ports of the local stack (`suwgate dev`). The ports are written out because the template shifts them.
- `layers.json`: the order of the server's layers and which project may reference which; the app's own design.
- `Myapp.Gate/`: the .NET guards that suwgate does not ship as a package yet (TODO.md).
- The tool configs of the web app (`eslint.config.js`, `tsconfig*.json`, `prettier.config.js`, `knip.config.ts`,
  `e2e/playwright.config.ts`) extend suwgate's presets and keep only the app's paths, features and entry points.

An agent that cannot pass a guard must not be able to change it: the edit hook refuses `suwgate.yml`, `layers.json`, `Myapp.Gate/`,
the hooks, the workflows and the lint, type and build configs. The owner lifts it for one session with `ALLOW_GATE_EDIT=1 claude`.

## What the gate forbids

Each line is a build rule, a test or a guard; its message says the fix. Rules live as low as they can: types and analyzers first.
suwgate's guards and steps are listed with their reasons in its README; the ones this app relies on:

- Web: tsc strict, eslint (typescript-eslint strict, feature boundaries, no browser storage, no comments), vitest, prettier (with Tailwind class order), knip, `openapi.d.ts` and `contract/openapi.json` equal to what the server generates, the contract's version right for what changed (oasdiff).
- Template (`TemplateTests`, template repo only): the placeholder is spelled `Myapp` or `myapp` only, base ports are whole numbers, port symbols are consistent, nothing that must not be copied is missing from the exclusions.
- Logs (`HealthEndpointsTests`): an error response carries the `traceId` that its log lines carry; `just logs --trace` reads them back.
- Repository (suwgate guards): required files, AGENTS.md short and `CLAUDE.md` exactly `@AGENTS.md`, every path the docs name exists, every top-level folder in the map above, no vague folder or project names (Common, Utils, Services, Components…), every script called and answering `--help`, just modules, pinned images, compose ports on 127.0.0.1, no style keys in `.vscode/settings.json`, the hooks wired.
- Build: analyzers `latest-all`, warnings are errors, unused private members (IDE0051, IDE0052, CA1823); no `#pragma warning disable`, `@ts-ignore` or `eslint-disable` (`no-in-place-switches`).
- Banned APIs (`BannedSymbols.txt`): `DateTime.Now`/`UtcNow`, blocking on tasks, `Thread.Sleep`, `Console`, `Activator`, reflection discovery, `Type.GetType(string)`, Newtonsoft.
- Packages (`banned-nuget-packages`, `unused-nuget-versions`, `banned-npm-packages`): no MediatR, AutoMapper, Mapster, Scrutor, mocking libraries, FluentAssertions, Newtonsoft.Json or Entity Framework; every versioned package is referenced.
- Settings (`appsettings-bound`, `connection-host`): every section of appsettings is read by an options class; connection strings name `127.0.0.1`.
- Migrations (`migrations-unchanged`, `MigrationFilesTests`): a script master has is never changed; names are `NNNN_snake_case.sql`.
- Layers (`ContractsTests`): a layer uses only the layers below it, every namespace has a layer, every `*Endpoints` class is mapped from `Api`, one per namespace.
- Unlisted projects (`Every_server_namespace_has_a_layer`, `UnlistedProjectsTests`): a guard over a list covers nothing left off it, so a namespace or project off the list fails until it is added.
- Project graph (`ProjectReferencesTests`): which project may reference which and see whose internals.
- Names (`NamingTests`, `ForbiddenNamesTests`): unique type names, one top-level type per file named like the file; no namespace or type named with a vague word, no `*Repository`, `*Service`, `*UnitOfWork` types.
- Interfaces (`InterfacesTests`): an interface has at least two implementations.
- Deleting (`SubtractTests`): nothing `[Obsolete]`, no unused non-public type.
- Escape hatches (`EscapeHatchTests`): no `dynamic`, no null-forgiving `!`.
- Comments (`comments-*` guards and suwgate's eslint rule `no-comments`): none in any code or config file, only the tool directives AGENTS.md lists ("No comments").
- Acceptance criteria (the `criteria` step, full gate only): each spec in `docs/visual/spec/` has an acceptance section with one unique `AC-<feature>-<n>` per criterion, each is named by a guardrail of a known kind, every id a guardrail names is in a spec, no id stands where the gate checks nothing.

## Disabled analyzer and lint rules, and why

Directory.Build.props turns on every analyzer (`AnalysisLevel` latest-all) and makes each warning an error; formatting is
CSharpier's (140 columns), so everything in `.editorconfig` past the basics is lint. A rule that does not fit is switched off
there, one line each, never with `#pragma`, and gets its row here. The web's lint rules come from suwgate's preset, with its reasons.

| Rule | Where | Why |
| --- | --- | --- |
| IDE0055 | all C# | formatting is CSharpier's job; two formatters would fight |
| IDE0130 (on, as an error) | all C# | a namespace is its folder path; set on its own so tuning the Style category can never switch it off |
| CA2007 | all C# | `ConfigureAwait(false)` matters under a synchronization context; ASP.NET Core and the test runner have none |
| IDE0058 | all C# | builder APIs (`services.AddX()`, `app.MapY()`) return themselves for chaining; discarding each with `_ =` hides the code |
| IDE0045, IDE0046 | all C# | they rewrite if/else into conditional expressions even when that nests ternaries or buries a throw |
| CA1032 | all C# | the standard exception constructor set is for exceptions others rethrow; ours are thrown once, with a message |
| CA1707, CA1515, CA2100, CA1054 | tests and the gate | test names are sentences (`Health_answers_ok`); xUnit needs public classes; SQL built in a test is its own, never input |
| CA2234 | tests and the gate | a relative path string reads better than `new Uri(...)` in a request |
| RS0030 | generated code under obj | xUnit's generated entry point blocks on the task once; it is not ours to change |

The web's import boundaries (`app()` of `@maniumek/suwgate/eslint` in `eslint.config.js`): every folder of `src/` is a feature or a
shared folder, named there; a feature imports only itself and the shared folders (`api`), a shared folder only itself. The entry files at
the root of `src/` (`main.tsx`, `App.tsx`) put the page together and may import any feature. When one feature really composes another,
that edge is its own policy, and its reason a row in this section.

Browser storage: state lives on the server, behind an action, or in the address, so a reload, a link and a second tab show the
same thing. When the owner asks for one remembered preference, its one file goes into `storage` of `app()` as the only exception.

## Why some settings are as they are

- NuGet: `nuget.config` clears every source but nuget.org: with a second source (a company feed) central package management raises
  NU1507, and warnings as errors turn it into a failed build.
- Build workers: `Directory.Build.rsp` (`-nodeReuse:false`) and the MSBuild variables `suwgate dev checkout-env` exports leave no MSBuild workers or build
  server running after a build: parallel builds of several checkouts (agents in worktrees) otherwise pile up dozens of idle nodes, gigabytes of memory.
- Line endings: `.gitattributes` keeps LF everywhere whatever the OS (as `.editorconfig`), never normalises binaries, and marks the lockfiles and the
  generated API files as generated, so GitHub collapses them in diffs.
- TypeScript: `tsconfig.json` is the browser app (suwgate's browser preset), `tsconfig.node.json` the code that runs in Node (the Vite
  and knip configs, the Playwright tests; its node preset). The app leaves out exactly the `*.e2e.ts` files the Node project checks.
  The browser preset's `types` already loads Vite's client types, so no file needs a `/// <reference>` line.
- pnpm: `pnpm-workspace.yaml` holds pnpm 11's settings (it reads them there, not from `package.json`): esbuild (via Vite) needs its
  postinstall for its platform binary, and approving it keeps installs non-interactive; `unrs-resolver` (from eslint-plugin-boundaries)
  gets its prebuilt binary as an optional package, so its fallback build is not needed. `minimumReleaseAgeExclude` lets a new release
  of the two shared kits, `@maniumek/suwgate` and `@maniumek/suwfrontkit`, in on its first day; every other package waits pnpm's day.
- The web image's nginx serves `/healthz` for the container's own liveness, independent of the API. `/api` is proxied through a
  variable so nginx looks the server's name up again every 10 s (a recreated server container gets a new address); with a variable and
  no URI in `proxy_pass` the request goes on unchanged, `/api` prefix included. The browser sees one origin, as behind the Vite proxy.
  Both images are built with the repository root as context: `docker build -f Myapp.Server/Dockerfile .`.
- In `compose.yaml` every value comes from the environment (`.env`, see `.env.example`) with a default, so a bare
  `docker compose --profile db up` works; leave the `db` profile out and point `ConnectionStrings__Default` at an external PostgreSQL.
  Every port is on the loopback (the database password is a development one), which suwgate's `compose-ports` guard keeps. The database's
  healthcheck asks over TCP, so the socket-only server a fresh volume first starts never counts as ready, and `suwgate dev db up` waits for it with `--wait`.
- CI pins every action, and suwgate's reusable gate workflow, by its commit; a step's name carries the version the commit is. The
  reusable workflow has no database service, so the gate job starts the database of `compose.yaml` in its command before the gate.
- VS Code recommends only the extensions of the tools this repository uses: C# Dev Kit, CSharpier, Prettier, ESLint, Tailwind CSS
  IntelliSense, Playwright, ShellCheck, EditorConfig, YAML, Containers and Just.

## Decisions the code relies on

Reasons that used to sit in comments and that no name or test can carry.

Server and gate:

- The OpenAPI build step starts the app only to read its routes, so start-up up to that point must not need a database.
- A server-test class starts one server, not one per test: xUnit creates the class anew for every test, and a server per test made
  the tests about five times slower. Each test still works on data of its own.
- The deletion guard (`SubtractTests`) treats `ServerApp` as an entry point, because the top-level `Program` that starts it has a generated `Main` the
  architecture reader does not see; it ignores the obsolete constructors the compiler makes for a type with required members.
- The browser-test run (`e2e.sh`) drops and creates its database in two commands, because `CREATE DATABASE` cannot run inside the one transaction a single
  `-c` would be.

Web:

- A feature's HTTP calls live with the feature (`<feature>/api.ts`) and take their types from the generated `openapi.d.ts`, never a
  hand-written copy of a server model; `src/api/client.ts` holds only what they share: the base path, `request`, and the error a
  response turns into (the server's ProblemDetails `detail` when it sent one).
- The API base (`VITE_API_BASE`) is unset in every real setup: the Vite dev proxy and nginx both serve the API at `/api`, one origin for the browser.
  `API_PROXY_TARGET` points the e2e run's Vite at its own server.
- The look (Catppuccin Mocha, dark only, Inter) and the components (Button, Input, Label, Alert, Page…) come from `@maniumek/suwfrontkit`,
  pinned to an exact version: a change of the look is a pull request here, through this app's own tests. Desktop only, 16:9 and 21:9.
- Under React's StrictMode an effect runs twice in development, so a screen aborts the first request and ignores its result.
- The Notes screen is the template's example feature: delete it when it is in the way.
- Browser tests start their own server and web app on their own ports and an empty database (`e2e.sh`), so what runs by hand and its
  data stay untouched.

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
2. In `layers.json` put the area on one level of `serverLayers` (`"Orders | Billing"`) and add an
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
