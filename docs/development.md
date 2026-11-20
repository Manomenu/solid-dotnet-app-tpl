# Development

## Requirements

- .NET SDK 10 (`global.json` pins the band), `just`, Docker (it runs the local PostgreSQL).
- A POSIX shell: on Windows use **Git Bash** (the gate's steps and the scripts in `scripts/.internal/` are bash).
  Agents call `Myapp.Web/node_modules/.bin/suwgate` and the scripts directly, humans use `just`.
- Node 24 and pnpm 11 (`corepack enable`), for `Myapp.Web`, whose packages include the gate (`@maniumek/suwgate`); once per machine
  `pnpm exec playwright install chromium` in `Myapp.Web/` for the browser tests.
- For the full gate: `shellcheck`, `gitleaks`, `oasdiff`, `python3` and `curl` (the `criteria` step downloads its pinned `vdoc.py`).
  A missing tool is reported as SKIP (a failure in CI), and the report ends with `NOT VERIFIED: <steps>`.

## Everyday commands

```sh
just                      # every recipe, grouped
just tmux-dev start       # the one way to run the stack: PostgreSQL (compose.yaml, localhost:5533), the API on
                          # http://127.0.0.1:6300 (dotnet watch, .env loaded) and the web app on http://localhost:3300 (pnpm dev)
just e2e                  # browser tests (own server, own database, own ports; not part of the gate)
just logs                 # why did it fail? last warnings/errors of the newest server log (see below)
just db up                # the local PostgreSQL alone (also down, reset, psql, logs, status)
just api-types            # regenerate contract/openapi.json and Myapp.Web/src/api/openapi.d.ts after changing a record the API
                          # exposes; then raise the contract's version in contract/package.json
just slow-tests           # the 10 slowest .NET tests (-n 25 for more): a report, the gate never measures time
just mutate               # Stryker.NET (MTP runner, preview) on the server code changed since master; needs a master with commits
just pack-template        # the dotnet new template as a NuGet package in .artifacts/nupkg
just check-quick          # suwgate check --quick: the repository guards, and for what changed only build, csharpier, the .NET guards
                          # (Myapp.Gate), eslint, tsc, shellcheck
just check                # suwgate check: the whole gate, every test, the whole repository (the same CI runs)
just fmt                  # csharpier and prettier
```

The same without `just`: `Myapp.Web/node_modules/.bin/suwgate check --quick`, `… check`, `… dev db up`. What the gate runs and why
is in `suwgate.yml` and in suwgate's README; `suwgate check --verbose` lists every step with its time.

## Settings

Defaults live in `appsettings.json`, laptop values in `appsettings.Development.json`, the rest
come from the environment (`Section:Key` is `Section__Key`: `ConnectionStrings__Default`). A
setting is an options class of the feature that reads it, validated with `ValidateOnStart()`.

## Worktrees

A linked git worktree gets its own test and e2e databases (`myapp_test_<suffix>`, `myapp_e2e_<suffix>`) and its own e2e
port pair (20000-29999), so checkouts that run the browser tests at the same time do not collide. `suwgate dev checkout-env`
(evaluated by the scripts that need it) derives the suffix from the directory name (lower-case, at most 30 characters, plus a hash of
the whole name, because worktrees of one repository share a long prefix). The main checkout and CI keep the plain names and ports
(`myapp_test`, `myapp_e2e`, 6301/3301); a value already in the environment wins. A worktree's databases are left behind, not dropped.
`suwgate check` does not apply them to its .NET tests yet, so two checkouts that run the full gate at once share `myapp_test`.

## Claude Code hooks

`.claude/settings.json` runs `suwgate hook stop` when Claude stops: the quick gate, which blocks the stop while it fails (the loop is
cut after one retry), skipped when nothing changed since it last passed. The quick gate has a budget of 20 seconds and fails above it;
`SUWGATE_QUICK_LIMIT=<seconds>` raises it on a machine that is slow across the board. It costs what the change costs: the repository
guards, plus build, csharpier, the .NET guards of `Myapp.Gate`, eslint, tsc and shellcheck only for what differs from where the branch
left `master` (committed or not, and new files). No database and no other tests; they and the acceptance criteria run in the full gate.

`suwgate hook guard-edit` refuses an agent's edit of the gate and of generated files with a message saying what to run, and of a
migration that already exists. A characterization recording is the agent's to write the first time, fixes included; one git already
tracks is the owner's. `settings.hooks` in `suwgate.yml` adds this repository's own files to its lists.

`.githooks/pre-commit` runs `suwgate hook pre-commit`: gitleaks on what is being committed, then the quick gate. The full gate and CI
run gitleaks too, but a secret found after the push is already on GitHub and in every clone.

## Personal agent rules

Rules that are yours alone (how you commit, which tools you use) go in `CLAUDE.local.md`:
Claude Code loads it, and git ignores it.

## The container stack

`docker compose --profile db up -d --build --wait` builds both images and starts db, server and web on
http://127.0.0.1:8190, and `just smoke` checks it, as CI does. Settings come from `.env` (copy `.env.example`); every value
has a default. With an external PostgreSQL, set `ConnectionStrings__Default` in `.env` and run
`docker compose up -d --build --wait` (no `db` profile). Use `127.0.0.1`, not `localhost`, in connection strings:
on Windows `localhost` tries IPv6 first and every new connection waits seconds.

## Logs

The server writes JSON log lines (the console JSON formatter `Suwbackkit.Server` sets up; `Logging` in `appsettings.json` or the
environment overrides it, such as `Logging__Console__FormatterName=simple`).
The server of `just tmux-dev start` (`suwgate dev server`) also writes them to .artifacts/logs/server.jsonl, `e2e.sh` to .artifacts/logs/e2e-server.jsonl;
the container stack is read with `--compose`. `just logs` prints the last warnings and errors of the newest log, one
line each; `just logs --trace <traceId>` shows every line of one request, with the `traceId` any error response
carries. `--all` shows every level, `-n N` the last N lines.

## Editors

VS Code: accept the recommended extensions (`.vscode/extensions.json`); formatting, tasks and debug launch are set up.
Rider: install the CSharpier plugin; everything else comes from `.editorconfig` and the repo's configs.
Personal files (.idea/, `*.DotSettings.user`) are ignored.
