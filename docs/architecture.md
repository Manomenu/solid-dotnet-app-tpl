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
(every disabled rule has its reason in the table below), `global.json`, `nuget.config`, `.gitattributes`, `.gitignore`.

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

- Web (`check.sh`): tsc strict, eslint (typescript-eslint strict + import boundaries: a feature uses `api` and `design`, `design` uses no feature; no `localStorage`, `sessionStorage` or `indexedDB`: state lives on the server or in the address), vitest, prettier (with Tailwind class order), knip (dead code), `openapi.d.ts` equal to the server's OpenAPI document.
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
- Escape hatches (`EscapeHatchTests`): no `dynamic`, no null-forgiving `!`.
- Comments: none in any code or config file, only the tool directives AGENTS.md lists ("No comments"). C# `CommentsTests` (Roslyn); TypeScript and JavaScript the local eslint rule `no-comments` (examples in `Myapp.Web/eslint.config.test.js`, `WebCommentsTests` keeps it on); shell, YAML, Dockerfiles, ignore files, `.editorconfig`, nginx and justfiles `HashCommentsTests`; MSBuild, XML and `index.html` `XmlCommentsTests`; JSON `JsonCommentsTests`; SQL `SqlCommentsTests`; CSS `CssCommentsTests` (the last five in `Myapp.Gate/Comments/`).
- Settings (`AppSettingsTests`): every section of appsettings is bound by an options class.
- Acceptance criteria (`check.sh` without `--quick` runs `criteria.sh`: the suwagent plugin's `vdoc.py criteria`, pinned, with the kinds of `criteria.json`; `CriteriaKindsTests` keeps each kind in README and AGENTS.md): each spec in `docs/visual/spec/` has an acceptance section with one unique `AC-<feature>-<n>` per criterion, each is named by a guardrail of a known kind, every id a guardrail names is in a spec, no id stands where the gate checks nothing.
- Docs (`AgentsFileTests`, `RepositoryLayoutTests`): AGENTS.md at most 100 lines, no tree drawings, no capitals shouting; CLAUDE.md is `@AGENTS.md`; every top-level folder is in the map above.
- The gate itself (`check.sh`): a ceiling per step, the quick gate fails above its budget (20 s by default) and checks only what changed.

## Disabled analyzer and lint rules, and why

Directory.Build.props turns on every analyzer (`AnalysisLevel` latest-all) and makes each warning an error; formatting is
CSharpier's (140 columns), so everything in `.editorconfig` past the basics is lint. A rule that does not fit is switched off
there, one line each, never with `#pragma`, and gets its row here.

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
| `restrict-template-expressions` allows numbers | web | template literals with numbers are how the UI builds labels and paths |
| `no-confusing-void-expression` ignores arrow shorthand | web | `onClick={() => setOpen(false)}` is the idiomatic React handler; braces add nothing |
| `no-unused-vars` ignores rest siblings | web | `const { a: _, ...rest } = obj` drops a key on purpose |

The web's import contracts (`boundaries` in `eslint.config.js`): every folder of `src/` is a feature, independent by default;
a feature imports only itself, `api` and `design`, and `design` imports no feature. The element patterns are matched in order and
the first wins, which is why `api` and `design` come before the catch-all feature pattern. The entry files at the root of `src/`
(`main.tsx`, `App.tsx`) put the page together and may import any feature. Browser tests drive the page from outside and are left out.
When one feature really composes another, that edge is its own policy, and its reason a row in this section.

Browser storage (`no-restricted-globals`, `no-restricted-properties` in `eslint.config.js`): state lives on the server, behind an
action, or in the address, so a reload, a link and a second tab show the same thing. When the owner asks for one remembered
preference, its one file is added there as the only exception.

## Why some settings are as they are

- NuGet: `nuget.config` clears every source but nuget.org: with a second source (a company feed) central package management raises
  NU1507, and warnings as errors turn it into a failed build.
- Build workers: `Directory.Build.rsp` (`-nodeReuse:false`) and the MSBuild variables `checkout-env.sh` exports leave no MSBuild workers or build
  server running after a build: parallel builds of several checkouts (agents in worktrees) otherwise pile up dozens of idle nodes, gigabytes of memory.
- Line endings: `.gitattributes` keeps LF everywhere whatever the OS (as `.editorconfig`), never normalises binaries, and marks the lockfiles and the
  generated API files as generated, so GitHub collapses them in diffs.
- TypeScript: `tsconfig.json` only points at the two projects so the editor knows which settings a file gets: `tsconfig.app.json` is the browser
  app, `tsconfig.node.json` the code that runs in Node (the Vite config, the Playwright tests). The app leaves out exactly the `*.e2e.ts`
  files the Node project checks, and the Node project sets `"exclude": []` because it would otherwise inherit that exclusion.
  The app's `types` already loads Vite's client types, so no file needs a `/// <reference>` line.
- pnpm: `pnpm-workspace.yaml` holds pnpm 11's settings (it reads them there, not from `package.json`): esbuild (via Vite) needs its
  postinstall for its platform binary, and approving it keeps installs non-interactive; `unrs-resolver` (from eslint-plugin-boundaries)
  gets its prebuilt binary as an optional package, so its fallback build is not needed.
- The web image's nginx serves `/healthz` for the container's own liveness, independent of the API. `/api` is proxied through a
  variable so nginx looks the server's name up again every 10 s (a recreated server container gets a new address); with a variable and
  no URI in `proxy_pass` the request goes on unchanged, `/api` prefix included. The browser sees one origin, as behind the Vite proxy.
  Both images are built with the repository root as context: `docker build -f Myapp.Server/Dockerfile .`.
- In `compose.yaml` every value comes from the environment (`.env`, see `.env.example`) with a default, so a bare
  `docker compose --profile db up` works; leave the `db` profile out and point `ConnectionStrings__Default` at an external PostgreSQL.
  Every port is on the loopback (the database password is a development one), which `ComposePortsTests` keeps. The database's
  healthcheck asks over TCP, so the socket-only server a fresh volume first starts never counts as ready, and `db.sh up` waits for it with `--wait`.
- CI pins every action by its commit; the step's name carries the version the commit is. The gate's tools are installed in the
  runner's temp folder, because the repository guards count every untracked file as part of the repository.
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
- The guard that every shell script is executable (`Every_shell_script_is_executable_in_git`) reads the git index, not the file system: a checkout on Windows or with
  `core.fileMode=false` has no exec bit to read.
- A script nobody calls is code nobody runs, so it rots unseen (`ScriptsCalledTests`); a recipe a person runs is the call that keeps
  it alive, while a doc naming a script or a test running it calls nothing for real.
- The quick gate costs what the change costs, not what the repository has grown to. In a new repository `master` or even `HEAD`
  may not exist yet: then everything tracked is new, and a contract with nothing to compare against is right by definition. Only a
  `vdoc.py` that cannot be downloaded is a SKIP; a checksum mismatch runs the criteria step, which fails and says why. The budget is
  decided before the summary, so a green run over budget never prints PASS.
- The contract's version check (`api-diff.sh`) runs after `api-types.sh --check` has proven the document current. It compares versions with `sort -V`, which
  orders 0.10.0 after 0.9.0, where a plain comparison of strings gets it wrong.
- The browser-test run (`e2e.sh`) drops and creates its database in two commands, because `CREATE DATABASE` cannot run inside the one transaction a single
  `-c` would be.
- The local stack (`tmux-dev.sh`) does not trust `TMUX` alone: an editor started from a tmux pane hands it down to its embedded terminals, and
  `switch-client` there would move another terminal's client. The session name is the lower-case project name.

Web:

- A feature's HTTP calls live with the feature (`<feature>/api.ts`) and take their types from the generated `openapi.d.ts`, never a
  hand-written copy of a server model; `src/api/client.ts` holds only what they share: the base path, `request`, and the error a
  response turns into (the server's ProblemDetails `detail` when it sent one).
- The API base (`VITE_API_BASE`) is unset in every real setup: the Vite dev proxy and nginx both serve the API at `/api`, one origin for the browser.
  `API_PROXY_TARGET` points the e2e run's Vite at its own server.
- Under React's StrictMode an effect runs twice in development, so a screen aborts the first request and ignores its result.
- The Notes screen is the template's example feature: delete it when it is in the way.
- Browser tests start their own server and web app on their own ports and an empty database (`e2e.sh`), so what runs by hand and its
  data stay untouched. They run on one worker, because tests that create data in one database would see each other's in parallel.
  On CI a failure is retried once, only to tell a steady failure from a flaky one in the report: a test that passes on the retry
  still fails the run.

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
