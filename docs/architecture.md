# Architecture

## The map

One line per top-level path (at most 300 characters each). `RepositoryLayoutTests` reads these
lines: a new top-level folder needs its line here, a deleted one loses it.

- `Myapp.slnx` — the .NET solution: the server and its tests.
- `Myapp.Server/` — the HTTP API (ASP.NET Core minimal APIs, Npgsql, Dapper with hand-written SQL). One folder and namespace per feature: `Api/` wires the routes, `Notes/` is the example feature, `Health/` checks the database, `Database/` is the pool and the grate migrations.
- `Myapp.Server/Database/Migrations/up/` — numbered SQL migrations `NNNN_what_it_does.sql`, applied at start-up by grate and never edited afterwards.
- `Myapp.Server.Tests/` — xUnit v3 tests against a real PostgreSQL. Mirrors the server's folders; the root holds the repository guards (layers, project graph, naming, comments, pragmas, AGENTS.md size, unused types).
- `Myapp.Web/` — the web app (React 19, Mantine, Vite, TypeScript strict, pnpm). One folder per feature under `src/` (`notes/` is the example: `api.ts`, `notes.ts` with its test, the screen, its e2e test); `src/api/` holds the HTTP client and the generated `openapi.d.ts`.
- `compose.yaml` — the whole stack on a laptop from the same Dockerfiles: db (profile `db`), server, web; everything configured from `.env` (`.env.example`) with ASP.NET names (`Section__Key`); only web publishes a port (8190).
- `Myapp.Server/Dockerfile`, `Myapp.Web/Dockerfile` — the two images (pinned bases, locked restore, build context is the repo root); the web image is nginx serving the build and proxying `/api` to the server.
- `.dockerignore` — keeps `node_modules`, `bin`, `obj` and `.env` out of the build context.
- `.github/` — `workflows/ci.yml`: the gate (`test`), the browser tests (`e2e`), and the images built and smoke-tested (`build`); nothing is published, the registry is project-specific.
- `.githooks/` — `pre-commit` runs gitleaks on what is being committed; `just sync` enables it.
<!-- template:start -->
- `.template.config/` — the `dotnet new` template definition (see the README); not copied into generated projects.
- `pack/` — packs the template as a NuGet package (`just pack-template`); not copied into generated projects.
- `scripts/.internal/init-project.proj` and `init-project.sh` — the template's post action; they run once in a new project and delete themselves.
<!-- template:end -->
- `docs/` — documentation for people working on the repo: this map, `development.md`.
- `scripts/` — `scripts/.internal/` holds what the gate, the hooks and `just` run: `check.sh`, `db.sh`, `checkout-env.sh`, `api-types.sh`, `e2e.sh`, `smoke.sh`, the Claude Code hook scripts.
- `.claude/` — the committed Claude Code settings: a Stop hook that runs the quick gate, a PreToolUse hook that protects migrations and generated files.
- `.config/` — `dotnet-tools.json`: the pinned CSharpier.
- `.just/` — modules of the `justfile` (`just db`).

Files at the root: `Directory.Build.props` (strict compiler and analyzers), `Directory.Build.rsp`
(no idle MSBuild workers), `Directory.Packages.props` (every package version, once),
`BannedSymbols.txt` (APIs the build refuses, each with what to use instead), `.editorconfig`
(every disabled rule has its reason), `global.json`, `nuget.config`, `.gitattributes`, `.gitignore`.

## What the gate forbids

Each line is a build rule or a test; its message says the fix. Rules live as low as they can: types and analyzers first.

- Web (`check.sh`): tsc strict, eslint (typescript-eslint strict + import boundaries between feature folders), vitest, prettier, knip (dead code), `openapi.d.ts` equal to the server's OpenAPI document.
- Template (`TemplateTests`, template repo only): the placeholder is spelled `Myapp` or `myapp` only, base ports are whole numbers, port symbols are consistent, nothing that must not be copied is missing from the exclusions.
- Images (`ImagePinsTests`): every `FROM` and `image:` carries a version, never `latest`.
- Containers (`check.sh`): `docker compose config` resolves with and without the `db` profile; `smoke.sh` (run by `just up`) checks `/healthz` and `/api/health` of the running stack.
- Build: analyzers `latest-all`, warnings are errors, unused private members (IDE0051, IDE0052, CA1823), no `#pragma warning disable` (`NoPragmaTests`).
- Banned APIs (`BannedSymbols.txt`): `DateTime.Now`/`UtcNow`, blocking on tasks, `Thread.Sleep`, `Console`, `Activator`, reflection discovery, `Type.GetType(string)`, Newtonsoft.
- Layers (`ContractsTests`): a layer uses only the layers below it, every namespace has a layer, every `*Endpoints` class is mapped from `Api`, one per namespace.
- Project graph (`ProjectReferencesTests`): which project may reference which and see whose internals.
- Names (`NamingTests`): unique type names, one top-level type per file named like the file.
- Forbidden names (`ForbiddenNamesTests`): no folder (web `src/` included), project, namespace or type named Common, Helpers, Utils, Manager, Shared, Core, Domain, Services, Models, Components, Hooks, Ui and the like; no `*Repository`, `*Service`, `*UnitOfWork` types.
- Packages (`PackagesTests`): no MediatR, AutoMapper, Mapster, Scrutor, mocking libraries, FluentAssertions, Newtonsoft.Json or Entity Framework; every versioned package is referenced.
- Interfaces (`InterfacesTests`): an interface has at least two implementations.
- Deleting (`SubtractTests`): nothing `[Obsolete]`, no unused non-public type.
- Escape hatches (`EscapeHatchTests`): no `dynamic`; a `!` needs a `// Why:` on its line or the one above.
- Comments (`CommentsTests`): only `// Why: ...` and tool markers; no TODO, FIXME, workaround or commented-out code; XML docs only in a `*.Contracts` project.
- Settings (`AppSettingsTests`): every section of appsettings is bound by an options class.
- Docs (`AgentsFileTests`, `RepositoryLayoutTests`): AGENTS.md at most 150 lines, no tree drawings, no capitals shouting; CLAUDE.md is `@AGENTS.md`; every top-level folder is in the map above.
- The gate itself (`check.sh`): a ceiling per step, a WARN when the quick gate passes 60 s.

## Areas (when a second product area appears)

Not used today: every feature sits directly under `Myapp.Server/` in one flat order. Do not
introduce an area for the first product area — only when a second one arrives.

An area groups the features of one product area (`Orders/` beside `Billing/`). It is one entry
in the top order and has an order of its own inside. Steps, each with the test that proves it:

1. Move the feature folders of the existing area under `Myapp.Server/<Area>/` (a move only, in a
   change of its own); namespaces follow the folders (IDE0130 fails the build otherwise).
2. In `ContractsTests.cs` put the area on one level of `Layers` (`"Orders | Billing"`) and add an
   `Areas` dictionary: area name to the order of its features, same `a | b` syntax. Extend
   `Every_server_namespace_has_a_layer` to look one segment deeper for names in `Areas`
   and `EdgesOf` to take a prefix.
   Proof: it fails for a feature folder missing from the area's order.
3. Areas never use each other: siblings on one level are forbidden edges, which `EdgesOf` yields.
   Proof: make `Billing` call `Orders` and see the layer test fail.
4. What both areas need moves down into a shared feature below the areas (`Notifications/`
   between the areas and `Database`), and shared features never use an area (the same edges).
5. `Api/` stays above all areas and maps each area's routes in a method of its own, so grep finds them.
6. Web (phase 2): `src/<area>/<feature>/`, an `areaFeature` pattern in the `boundaries` rules of
   `eslint.config.js`, and an explicit policy for any edge between features of one area.
7. Add the area's folder to the map above in the same change.
