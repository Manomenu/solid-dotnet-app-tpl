<!-- template:start -->
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/visual/board/img/readme/hero-dark.png">
  <img alt="solid-dotnet-app-tpl: a dotnet new template. A .NET 10 app where the wrong change fails the build, with a message that says the fix: made for coding agents and the people reviewing them. ASP.NET Core and PostgreSQL, React and shadcn/ui · Tailwind, one gate in about 20 seconds, Claude Code hooks. One command decides done; 18 kinds of guard, each with its fix; a quick gate of about 15 seconds after every agent turn." src="docs/visual/board/img/readme/hero-light.png" width="100%">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/visual/board/img/readme/why-dark.png">
  <img alt="A usual template versus this template. Rules in a long README the agent skims and forgets, versus a short AGENTS.md where every checkable rule is a test, analyzer or hook. Controllers, Services and Repositories where one change touches five folders, versus one folder per feature with route, SQL and records together. MediatR and assembly scanning where grep can't find who calls what, versus explicit wiring with reflection and mediator packages banned. A Helpers folder that grows forever, versus vague names (Helpers, Utils, Common, Manager) failing the gate. Mocks pass while the real database fails, versus tests against a real PostgreSQL with one database per worktree. Tests pass while half the checks were skipped, versus a step that can't run saying SKIP and NOT VERIFIED, and failing in CI." src="docs/visual/board/img/readme/why-light.png" width="100%">
</picture>

## Create a project

```sh
git clone https://github.com/Manomenu/solid-dotnet-app-tpl.git
dotnet new install ./solid-dotnet-app-tpl
dotnet new solid-dotnet -n Invoices --port-offset 2 --allow-scripts yes
cd Invoices && bash scripts/.internal/check.sh
```

- `-n Invoices` renames `Myapp`/`myapp` everywhere: projects, namespaces, folders, docker names, databases.
  Use a simple name (letters and digits): it becomes a database, a container and a package name.
- `--port-offset N` (0-9) shifts every port by `10 * N`, so projects run side by side. Base ports: server 6300,
  web 3300, PostgreSQL 5533, compose web 8190, e2e 6301/3301.
- `--allow-scripts yes` lets the post action run `scripts/.internal/init-project.sh`: it regenerates the
  lockfiles, creates the git repository on `master` with the hooks enabled, and removes the template notes
  (this block included). Without it, run `dotnet msbuild scripts/.internal/init-project.proj` yourself. The
  post action goes through MSBuild so that on Windows `bash` is Git's, not WSL's; install Git for Windows.
- `dotnet new uninstall <path>` removes the template. To share it as a file instead of a folder:
  `just pack-template`, then `dotnet new install .artifacts/nupkg/Solid.DotnetApp.Template.1.0.0.nupkg`.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/visual/board/img/readme/flow-dark.png">
  <img alt="How work flows. Create once with dotnet new solid-dotnet: it renames everything, shifts ports, regenerates lockfiles, initializes git and hooks. The agent edits; a PreToolUse hook blocks generated files and applied migrations. Quick gate, about 15 seconds, run by the Stop hook every turn: build with analyzers, server tests on PostgreSQL, tsc, eslint, vitest. Full gate, about 20 seconds, before a commit: adds prettier, knip, API types, compose, shellcheck, gitleaks. CI runs the same script plus e2e, images and a smoke test; SKIP counts as FAIL. Rule for rules: when the agent repeats a mistake, add not a sentence to AGENTS.md but a type, an analyzer, a test or a hook." src="docs/visual/board/img/readme/flow-light.png" width="100%">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/visual/board/img/readme/features-dark.png">
  <img alt="What the gate forbids. Layers against their order (ArchUnitNET): every namespace must have a layer, the message says where to move the code. Vague names (repo-wide test): Common, Utils, Helpers, Shared, Services, Repositories in folders, namespaces and types. Banned packages: MediatR, AutoMapper, EF Core, Moq, Newtonsoft, each with its replacement. Hidden wiring (BannedApiAnalyzers): Activator, assembly scanning, DateTime.Now, .Result. Dead and for-later code (tests and knip): unused types, packages and settings, Obsolete shims, lone interfaces. Stray comments (Roslyn): TODO, workaround, commented-out code, the null-forgiving operator and dynamic without a reason." src="docs/visual/board/img/readme/features-light.png" width="100%">
</picture>

The full list, each guard with its fix: [docs/architecture.md, "What the gate forbids"](docs/architecture.md#what-the-gate-forbids).

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/visual/board/img/readme/choose-dark.png">
  <img alt="Which command when. While working: check.sh --quick, which the Stop hook runs for you. Before calling a change done: check.sh, the one definition of done. You changed something a user clicks: e2e.sh, Playwright with its own database. Something failed and you need to know why: just logs, the last errors one line each, or --trace with a request id. You added logic with rules: just mutate, where surviving mutants mean weak tests. Prove the containers work: just up and smoke.sh, the stack as it ships." src="docs/visual/board/img/readme/choose-light.png" width="100%">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/visual/board/img/readme/architecture-dark.png">
  <img alt="How it is built. Web: React 19, shadcn/ui, Tailwind, Vite; one folder per feature, types generated from the server's OpenAPI (src/notes/). Server: ASP.NET Core minimal APIs; route, then a store with hand-written SQL (Dapper), then records, with explicit Map calls (Notes/). PostgreSQL: grate migrations, numbered scripts never edited once applied, a database per git worktree (Database/). It runs on the host while developing, in compose with all settings from .env and only web exposed, and from the same images anywhere." src="docs/visual/board/img/readme/architecture-light.png" width="100%">
</picture>

The pictures are drawn on `docs/visual/board/readme.html` and rendered with the suwagent plugin's
`vdoc.py render docs/visual/board/readme.html --picture`; edit the board, render again, commit both.
They are not copied into generated projects. Everything below this block is the README a generated project starts with.
<!-- template:end -->

# Myapp

An ASP.NET Core minimal-API server (Npgsql, Dapper, hand-written SQL, grate migrations), a React 19 + shadcn/ui + Tailwind
web app with types generated from the server's OpenAPI document, and a Notes feature as the pattern end to end.

## What the gate guarantees

`bash scripts/.internal/check.sh` (`--quick` while working) fails on: any compiler or analyzer warning, banned
APIs, a layer used against its order, dead types and packages, vague names, stray comments, `dynamic`, unpinned
images, stale generated types, an unresolvable compose file. The full list, each with its fix:
[docs/architecture.md, "What the gate forbids"](docs/architecture.md#what-the-gate-forbids).
Browser tests run separately (`scripts/.internal/e2e.sh`), and `just up` starts the container stack and
smoke-tests it.

## Requirements and commands

See [docs/development.md](docs/development.md). Rules for contributors and agents: [AGENTS.md](AGENTS.md).
