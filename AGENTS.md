# AGENTS.md

Rules for anyone — human or AI agent — changing this repository. `CLAUDE.md` only points here;
this file is the one to edit. Where things live: [docs/architecture.md](docs/architecture.md).
Setup and everyday commands: [docs/development.md](docs/development.md). Not built yet: [TODO.md](TODO.md).
Areas: not used until a second product area appears — how to add them:
[docs/architecture.md#areas-when-a-second-product-area-appears](docs/architecture.md#areas-when-a-second-product-area-appears).

## Commands

| Command | Use it to |
| --- | --- |
| `scripts/.internal/check.sh --quick` | during work: build with analyzers, csharpier, server tests against the real database, tsc, eslint, vitest (about 20 seconds) |
| `scripts/.internal/check.sh` | before calling a change done: quick, plus prettier, knip, API types, compose config, shellcheck, gitleaks |
| `scripts/.internal/e2e.sh` | after changing anything a user clicks: Playwright against a real server and its own database (not in the gate) |
| `scripts/.internal/logs.sh` (`just logs`) | why did it fail? the last warnings and errors of the newest server log, one line each; `--trace <id>` takes the `traceId` of an error response, `--all`, `--compose`, `-n N` |
| `scripts/.internal/api-types.sh` | after changing a record the API exposes: regenerates `openapi.d.ts` (never edit it) |
| `scripts/.internal/db.sh up\|down\|status\|psql` | the local PostgreSQL on `localhost:5533` (container `myapp-postgres`) |
| `just fmt` | fix formatting: CSharpier and Prettier |
| `just up` / `just down` | the container stack on port 8190, with its smoke test |

Humans use `just` (lists every recipe); agents call `scripts/.internal/` directly.
Ports of this template: server 6300, web 3300, PostgreSQL 5533, compose web 8190.

## Git

- **The main branch is `master`**, never `main` (`git init -b master`).
- Work on a feature branch, never on `master`. Agents do not rewrite or discard history: no
  amend, reset, rebase, stash, force-push or deleting branches. Merging and releasing are the owner's.
- A change with a red gate is not committed: it stays in the working tree and the summary says why.
- **One task, one branch.** The branch changes only what the task needs to work. Refactors,
  renames, extra logging, config or version bumps and unrelated fixes noticed on the way go to
  their own branch, or into `TODO.md`. Say so once when you reach for one; if the owner confirms
  it belongs here, put it in.
- Personal rules (how you commit, your tools) go in the gitignored `CLAUDE.local.md`, not here.

## KISS and YAGNI — the first rule

The repo grows **only when a real need shows up**; one developer maintains it after hours.

- No dependency, setting, env var, compose service or abstraction "for later": it arrives with
  the first code that uses it. No interface with one implementation; tests use the real thing
  (a real PostgreSQL) or a recorded answer.
- No new component (queue, cache, worker, sidecar) without the feature that needs it.
- When in doubt, leave it out — and say in the change what was left out and why.

You added logic with rules → `just mutate`; surviving mutants → stronger tests.

## Subtract before you add
Plan a change by listing what it makes obsolete, and delete that first, in the same change:
migrate every caller, then remove the old code. No shims, no [Obsolete], no "legacy" path kept just in case.

## Rules live as low as they can
When I correct you, or when you are about to add a rule to AGENTS.md / CLAUDE.md,
first try to make the mistake impossible or loud, in this order:
a type → an analyzer or lint (with the fix in its message) → a test → a hook.
Add a sentence only for what none of these can check, and say in one line why.

A guard's message always states the fix. Never weaken a gate step to make a change pass — fix
the change. A step that cannot run reports SKIP, never PASS, and the report ends with a line listing
the steps that were not verified; in CI a SKIP is a failure.

## Layout: by feature

The server grows by features: everything that makes one feature work sits in **one folder**
named in the users' terms (`Notes/`), not by technical kind (`Models/`, `Services/`). `Notes/`
is the pattern end to end: route (`NoteEndpoints`) → store with hand-written SQL (`NoteStore`)
→ records (`Note`). Wiring is explicit: `Api/ServerApp.cs` calls every `Map…`.

- A new capability starts as a new folder, or goes into the feature it belongs to — never into
  a shared bucket. Shared code is earned by the second feature that needs it, and is named
  after what it is (`Database`), not after who uses it.
- A new feature gets its layer in `ContractsTests.cs`, a new project its row in
  `ProjectReferencesTests.cs`, a new top-level folder its line in `docs/architecture.md`.
- Web: one folder per feature under `src/` (screen, logic with its test, `api.ts`), shared look in `src/design/` (shadcn, Tailwind), types from the generated `openapi.d.ts`; rules live in plain `.ts` modules, components only wire them.
- Schema changes go into a new numbered script in `Database/Migrations/up/`, never an edited one.
- Records an endpoint sends or receives are a contract with the outside; a row from our own SQL
  is not. Keep them apart once they differ.
- A problem the caller can fix is a ProblemDetails whose `detail` says what to do.

## What no test can check

- Names are chosen word by word: domain words in the users' language (English here), software
  words in technical English. One word per thing, everywhere.
- A comment says *why*, never *what*; if code needs a *what* comment, rename the code.
- Secrets never enter the repo: not in code, settings, tests or history. Local values live in
  user secrets or the gitignored `.env`; a real hit is removed and rotated, never allow-listed.
- Delegate larger, well-defined work (a clear goal, known files, a check that says when done)
  to a cheaper model; keep design decisions and the review of the result.
- When several changes share one branch, build them all, then run the full gate once.
