# Architecture

## The map

One line per top-level path (at most 300 characters each). suwgate's `repository-layout` guard reads these
lines: a new top-level folder needs its line here, a deleted one loses it.

- `Myapp.slnx` — the .NET solution: the server and its tests.
- `Myapp.Server/` — the HTTP API (ASP.NET Core minimal APIs, Dapper with hand-written SQL) on NuGet `Suwbackkit.Server` and `Suwbackkit.Postgres`. One folder and namespace per feature: `Api/` wires the routes, `Notes/` is the example feature.
- `Myapp.Server/Database/Migrations/up/` — numbered SQL migrations, applied at start-up by `Suwbackkit.Postgres` (grate) and never edited afterwards.
- `Myapp.Server.Tests/` — xUnit v3 tests against a real PostgreSQL, whose database NuGet `Suwbackkit.Testing` recreates (only one named with the word `test`). Mirrors the server's folders; `Characterization/` holds behaviour recorded before changing legacy code.
- `Myapp.Gate/` — the xUnit project that calls the .NET guards of NuGet `Suwgate.Architecture` and `Suwgate.Database` (`GuardTests`), and the template's own `TemplateTests`; `suwgate check --quick` runs it.
- `Myapp.Web/` — the web app (React 19, Tailwind 4, Vite, TypeScript strict, pnpm), styled by `@maniumek/suwfrontkit`. A folder per feature under `src/` (`notes/` is the example); `src/api/` the HTTP client and the generated `openapi.d.ts`. Its packages include the gate, `@maniumek/suwgate`.
- `compose.yaml` — the whole stack on a laptop from the same Dockerfiles: db (profile `db`), server, web; everything configured from `.env` (`.env.example`) with ASP.NET names (`Section__Key`); only web publishes a port (8190).
- `Myapp.Server/Dockerfile`, `Myapp.Web/Dockerfile` — the two images (pinned bases, locked restore, build context is the repo root); the web image is nginx serving the build and proxying `/api` to the server.
- `.dockerignore` — keeps `node_modules`, `bin`, `obj` and `.env` out of the build context.
- `.github/` — `workflows/ci.yml`: one call of suwgate's reusable gate workflow: the gate (the browser tests and the database included) and the images built and smoke-tested (`smoke: true`); nothing is published, the registry is project-specific.
- `.vscode/` — the editor's glue, shared: recommended extensions, formatter per language, the schema of `suwgate.yml`, file nesting, debug launch, tasks that run the gate. No style rules (they live in `.editorconfig` and the formatters' configs, so Rider agrees).
- `.githooks/` — `pre-commit` runs `suwgate hook pre-commit`: gitleaks on what is being committed, then the quick gate; `just sync` enables it.
<!-- template:start -->
- `.template.config/` — the `dotnet new` template definition (see the README); not copied into generated projects.
- `pack/` — packs the template as a NuGet package (`just pack-template`); not copied into generated projects.
- `scripts/.internal/init-project.proj` and `init-project.sh` — the template's post action (`just init` without `--allow-scripts yes`); they run once in a new project and delete themselves, their recipe and `just pack-template`.
<!-- template:end -->
- `docs/` — documentation for people working on the repo: this map, `development.md`, the specs in `visual/spec/` (their acceptance criteria live as guardrails, see AGENTS.md), decisions above one feature in `adr/`.
- `scripts/` — `scripts/.internal/` holds the tools of this app that `just` runs; the local stack, the contract types (`api-types`) and the slow-test report (`slow-tests`) are `suwgate dev`. The gate refuses a script that nothing calls.
- `.claude/` — the committed Claude Code settings: `suwgate hook stop` runs the quick gate when Claude stops, `suwgate hook guard-edit` protects migrations, generated files, and the gate with the lint configs (`ALLOW_GATE_EDIT=1 claude` lifts the last).
- `.config/` — `dotnet-tools.json`: the pinned CSharpier.
- `contract/` — the API contract (`openapi.json`) and its version (`package.json`), generated and committed for consumers.
- `tmux-dev.just` — the `justfile` module of the local stack; a tool's own module is `<folder>/mod.just`.

Files at the root: `suwgate.yml` (the gate), `layers.json` (the server's layers and which project may reference which),
`Directory.Build.props` (the target framework, the settings NuGet reads before any package, and `Suwgate.Architecture`, which brings
the strict compiler, the analyzers and the banned APIs), `Directory.Build.rsp` (no idle MSBuild workers), `Directory.Packages.props`
(every package version, once), `.editorconfig` (whitespace and this app's own rule overrides), `global.json`, `nuget.config`,
`.gitattributes`, `.gitignore`.

## The gate

The gate is suwgate (npm `@maniumek/suwgate`, [its README](https://github.com/Manomenu/suwgate)): `suwgate check` runs the steps
of the profiles `suwgate.yml` names (web, dotnet, database) and its repository guards. This repository keeps only the wiring and what
is its own:

- `suwgate.yml`: the profiles, where the web app lives, the steps it skips and the exceptions, each with its reason, the edit hook's
  extra files, and the names and ports of the local stack (`suwgate dev`). The ports are written out because the template shifts them.
- `layers.json`: the order of the server's layers and which project may reference which; the app's own design.
- `Myapp.Gate/`: one test that runs every .NET guard of `Suwgate.Architecture` and `Suwgate.Database` (`SuwgateGuards.VerifyAll`), given
  only what suwgate cannot find itself: the server's assembly, the gate's own and the type that maps the endpoints; `dotnet-test` fails when
  a guard did not run. It stays a project of its own, apart from
  `Myapp.Server.Tests`, because `suwgate check --quick` runs every test of a project that uses those packages: here that is a few
  seconds without a database, while the server tests need PostgreSQL and start the server.
- `Directory.Build.props` and `.editorconfig`: the build defaults, the banned APIs and the analyzer rules come from `Suwgate.Architecture`
  (buildTransitive props and a global analyzer config); the app keeps only what NuGet must read before any package, and its own overrides.
- The tool configs of the web app (`eslint.config.js`, `tsconfig*.json`, `prettier.config.js`, `knip.config.ts`,
  `e2e/playwright.config.ts`) extend suwgate's presets and keep only the app's paths, features and entry points.

An agent that cannot pass a guard must not be able to change it: the edit hook refuses `suwgate.yml`, `layers.json`, `Myapp.Gate/`,
the hooks, the workflows and the lint, type and build configs (suwgate's defaults; `Myapp.Gate/` is this app's line in `settings.hooks.protected`). The owner lifts it for one session with `ALLOW_GATE_EDIT=1 claude`.

## What the gate forbids

Each line is a build rule, a test or a guard; its message says the fix. Rules live as low as they can: types and analyzers first.
suwgate's guards and steps are listed with their reasons in its README; the ones this app relies on:

- Web: tsc strict, eslint (typescript-eslint strict, feature boundaries, no browser storage, no comments), vitest, prettier (with Tailwind class order), knip, `openapi.d.ts` and `contract/openapi.json` equal to what the server generates, the contract's version right for what changed (oasdiff).
- Template (`TemplateTests` in `Myapp.Gate`, template repo only): the placeholder is spelled `Myapp` or `myapp` only, base ports are whole numbers, port symbols are consistent, nothing that must not be copied is missing from the exclusions.
- Logs: an error response carries the `traceId` that its log lines carry (`Suwbackkit.Server`, tested there); `just logs --trace` reads them back.
- Repository (suwgate guards): required files, AGENTS.md short and `CLAUDE.md` exactly `@AGENTS.md`, every path the docs name exists, every top-level folder in the map above, no vague folder or project names (Common, Utils, Services, Components…), every script called and answering `--help`, just modules, pinned images, compose ports on 127.0.0.1, no style keys in `.vscode/settings.json`, the hooks wired.
- Build (`Suwgate.Architecture`, and architecture/build-settings checks no project turns it off): analyzers `latest-all`, warnings are errors, unused private members (IDE0051, IDE0052, CA1823); no `#pragma warning disable`, `@ts-ignore` or `eslint-disable` (`no-in-place-switches`).
- Server tests (architecture/no-fixture-in-field): a test class takes the server from its class fixture, never starts one in a field.
- Banned APIs (the list `Suwgate.Architecture` ships; a BannedSymbols.txt at the root would add the app's own lines): `DateTime.Now`/`UtcNow`, blocking on tasks, `Thread.Sleep`, `Console`, `Activator`, reflection discovery, `Type.GetType(string)`, Newtonsoft.
- Packages (`banned-nuget-packages`, `unused-nuget-versions`, `banned-npm-packages`): no MediatR, AutoMapper, Mapster, Scrutor, mocking libraries, FluentAssertions, Newtonsoft.Json or Entity Framework; every versioned package is referenced.
- Settings (`appsettings-bound`, `connection-host`): every section of appsettings is read by an options class; connection strings name `127.0.0.1`.
- Migrations (`migrations-unchanged`, database/migration-script-names): a script master has is never changed; names are `NNNN_snake_case.sql`.
- Layers (architecture/layers, architecture/endpoints): a layer uses only the layers below it, every namespace has a layer, every `*Endpoints` class is mapped from `Api`, one per namespace.
- Unlisted projects (architecture/layers, architecture/every-project-guarded): a guard over a list covers nothing left off it, so a namespace or project off the list fails until it is added.
- Project graph (architecture/project-references): which project may reference which and see whose internals.
- Names (architecture/unique-type-names, architecture/one-type-per-file, architecture/forbidden-type-names): unique type names, one top-level type per file named like the file; no namespace or type named with a vague word, no `*Repository`, `*Service`, `*UnitOfWork` types.
- Interfaces (architecture/interfaces-have-two-implementations): an interface has at least two implementations.
- Deleting (architecture/nothing-obsolete, architecture/no-unused-types): nothing `[Obsolete]`, no unused non-public type.
- Escape hatches (architecture/no-escape-hatches): no `dynamic`, no null-forgiving `!`.
- Comments (`comments-*` guards and suwgate's eslint rule `no-comments`): none in any code or config file, only the tool directives AGENTS.md lists ("No comments").
- Acceptance criteria (the `criteria` step, full gate only): each spec in `docs/visual/spec/` has an acceptance section with one unique `AC-<feature>-<n>` per criterion, each is named by a guardrail of a known kind, every id a guardrail names is in a spec, no id stands where the gate checks nothing.

## Disabled analyzer and lint rules, and why

`Suwgate.Architecture` turns on every analyzer (`AnalysisLevel` latest-all), makes each warning an error and ships the rules it
switches off, each with its reason, in its README; formatting is CSharpier's (140 columns). A rule that does not fit this app is
switched off in `.editorconfig`, one line each, never with `#pragma`, and gets its row here. The web's lint rules come from
suwgate's preset, with its reasons.

| Rule | Where | Why |
| --- | --- | --- |
| CA2100 | server tests | the test database's name in `CREATE DATABASE` is the test's own, never input |

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
- The deletion guard (architecture/no-unused-types) gets `ServerApp` as an entry point, because the top-level `Program` that starts it has a
  generated `Main` the architecture reader does not see.

Web:

- A feature's HTTP calls live with the feature (`<feature>/api.ts`) as `dataOf(api.GET(…))` on suwfrontkit's client, typed by the
  `paths` of the generated `openapi.d.ts`, never a hand-written copy of a server model; `src/api/client.ts` makes that one client.
  A failure is suwfrontkit's `ApiError`, a refusal read by its `code` (typed by the contract's enum through `refusalReader`).
- The client calls the page's own origin: the Vite dev proxy and nginx both serve the API at `/api`, one origin for the browser.
  `API_PROXY_TARGET` points the e2e run's Vite at its own server.
- The look (Catppuccin Mocha, dark only, Inter) and the components (Button, Input, Label, Alert, Page…) come from `@maniumek/suwfrontkit`,
  pinned to an exact version: a change of the look is a pull request here, through this app's own tests. Desktop only, 16:9 and 21:9.
- Routes are wouter's (`<Switch>` of `<Route>`s, an unknown path redirected): small, hooks only, no loaders, which a client-only
  desktop tool does not need; the derived apps use it too.
- An error while rendering a screen shows suwfrontkit's `ErrorBoundary` (the error and Try again) in place of that screen, not a blank
  page. It is keyed by the path, so going to another screen clears it. `createRoot`'s `onCaughtError` and `onUncaughtError` log to the
  console with the component stack: the one place to plug in an error reporter later.
- A form sends through a `<form action>` with `useActionState`: Enter submits, the submit button is disabled while the action runs (no
  double sends), and a refusal (the screen's own check or the server's `detail`) comes back as the action's state, never thrown, so it
  shows in the form. React empties a form after its action, so the fields are uncontrolled with the state's text as `defaultValue`: a
  refused text stays, a saved one clears. What an action throws reaches the error boundary.
- A screen loads its data as a promise kept in state (`useState(fetch…)`, created once when the screen mounts) that a child reads with
  `use` under `Suspense`; after a change it sets a new promise inside a transition, so the old list stays until the new one arrives.
  A failed load is the error boundary's, and Try again mounts the screen anew, which loads again. No effect, so no race to abort; under
  StrictMode development sends the first request twice and uses one. A data library (TanStack Query, through the kit's
  `createQueryClient`) comes when a screen needs caching or refetching.
- React Compiler memoizes the components at build time (`babel-plugin-react-compiler`, pinned exactly as its docs advise, in the Vite
  React plugin's Babel options; with `@vitejs/plugin-react` 6 it moves to `@rolldown/plugin-babel`). New code needs no `useMemo` or
  `useCallback`; existing ones stay, as React advises.
- The Notes screen is the template's example feature: delete it when it is in the way.
- Browser tests start their own server and web app on their own ports and an empty database (the gate's `e2e` step), so what runs by hand and its
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
2. In `layers.json` put the areas on one level of the layers of `Myapp.Server` (`"Orders | Billing"`).
   Proof: architecture/layers fails for an area missing from the layers.
3. Areas never use each other: siblings on one level are forbidden edges.
   Proof: make `Billing` call `Orders` and see architecture/layers fail.
4. What both areas need moves down into a shared feature below the areas, and shared features never use an area (the same edges).
   The order of the features inside an area goes into `areas` of the `Myapp.Server` entry in `layers.json`
   (`"areas": { "Orders": ["Checkout", "Cart | Payment"] }`, optionally `"subFeatures": true` to keep a feature's sub-namespaces
   out of the feature and its siblings). Proof: make a feature of the area use one above it and see architecture/layers fail.
5. `Api/` stays above all areas and maps each area's routes in a method of its own, so grep finds them.
6. Web (phase 2): `src/<area>/<feature>/`, an `areaFeature` pattern in the `boundaries` rules of
   `eslint.config.js`, and an explicit policy for any edge between features of one area.
7. Add the area's folder to the map above in the same change.
