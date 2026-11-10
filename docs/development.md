# Development

## Requirements

- .NET SDK 10 (`global.json` pins the band), `just`, Docker (it runs the local PostgreSQL).
- A POSIX shell: on Windows use **Git Bash** (the scripts in `scripts/.internal/` are bash; run
  them with `bash Myapp.Gate/scripts/check.sh`). Agents call the scripts directly, humans use `just`.
- Node 24 and pnpm 11 (`corepack enable`), for `Myapp.Web`; once per machine `pnpm exec playwright install chromium` in `Myapp.Web/` for the browser tests.
- For the full gate: `shellcheck` and `gitleaks`. A missing tool is reported as SKIP (a failure
  in CI), and the report ends with `NOT VERIFIED: <steps>`.

## Everyday commands

```sh
just                      # every recipe, grouped
just tmux-dev start       # the one way to run the stack: PostgreSQL (compose.yaml, localhost:5533), the API on
                          # http://127.0.0.1:6300 (dotnet watch, .env loaded) and the web app on http://localhost:3300 (pnpm dev)
just e2e                  # browser tests (own server, own database, own ports; not part of the gate)
just logs                 # why did it fail? last warnings/errors of the newest server log (see below)
just api-types            # regenerate contract/openapi.json and Myapp.Web/src/api/openapi.d.ts after changing a record the API
                          # exposes; then raise the contract's version in contract/package.json
just mutate               # Stryker.NET (MTP runner, preview) on the server code changed since master; needs a master with commits
just pack-template        # the dotnet new template as a NuGet package in .artifacts/nupkg
just check-quick          # the repository guards, and build, csharpier, eslint, tsc, shellcheck for what changed only: seconds
just check                # the whole gate: every test, the whole repository
just fmt                  # csharpier format
```

The same without `just`: `bash Myapp.Gate/scripts/check.sh --quick`, `bash Myapp.Gate/scripts/check.sh`,
`bash scripts/.internal/db.sh up`.

## Settings

Defaults live in `appsettings.json`, laptop values in `appsettings.Development.json`, the rest
come from the environment (`Section:Key` is `Section__Key`: `ConnectionStrings__Default`). A
setting is an options class of the feature that reads it, validated with `ValidateOnStart()`.

## Worktrees

A linked git worktree gets its own test database (`myapp_test_<dir>`), derived from its
directory name by `checkout-env.sh`; its databases are left behind, not dropped.

## Claude Code hooks

`.claude/settings.json` runs `Myapp.Gate/scripts/check.sh --quick` when Claude stops, and blocks
the stop while it fails (the loop is cut after one retry). The quick gate has a budget of 20
seconds and fails above it; `CHECK_QUICK_LIMIT=<seconds>` raises it on a machine that is slow
across the board. It costs what the change costs: the repository guards, plus build, csharpier, eslint, tsc
and shellcheck only for what differs from where the branch left `master` (committed or not, and new files).
No database and no vitest; the tests that prove acceptance criteria run only in the full gate. Edits to migrations that already
exist, to lockfiles and to other generated files are refused with a message saying what to run.

## Personal agent rules

Rules that are yours alone (how you commit, which tools you use) go in `CLAUDE.local.md`:
Claude Code loads it, and git ignores it.

## The container stack

`docker compose --profile db up -d --build --wait` builds both images and starts db, server and web on
http://127.0.0.1:8190, and `scripts/.internal/smoke.sh http://127.0.0.1:8190` checks it, as CI does. Settings come from `.env` (copy `.env.example`); every value
has a default. With an external PostgreSQL, set `ConnectionStrings__Default` in `.env` and run
`docker compose up -d --build --wait` (no `db` profile). Use `127.0.0.1`, not `localhost`, in connection strings:
on Windows `localhost` tries IPv6 first and every new connection waits seconds.

## Logs

The server writes JSON log lines (the built-in console JSON formatter, `Logging:Console` in `appsettings.json`).
The server of `just tmux-dev start` also writes them to .artifacts/logs/server.jsonl, `e2e.sh` to .artifacts/logs/e2e-server.jsonl;
the container stack is read with `--compose`. `just logs` prints the last warnings and errors of the newest log, one
line each; `just logs --trace <traceId>` shows every line of one request, with the `traceId` any error response
carries. `--all` shows every level, `-n N` the last N lines.

## Editors

VS Code: accept the recommended extensions (`.vscode/extensions.json`); formatting, tasks and debug launch are set up.
Rider: install the CSharpier plugin; everything else comes from `.editorconfig` and the repo's configs.
Personal files (.idea/, `*.DotSettings.user`) are ignored.
