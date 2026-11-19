# AGENTS.md

Rules for anyone — human or AI agent — changing this repository. `CLAUDE.md` only points here;
this file is the one to edit. Setup: [docs/development.md](docs/development.md). Not built yet: [TODO.md](TODO.md).

## Commands

- For people: a recipe in `justfile`, in a module of the whole repository beside it (`tmux-dev.just`), or in a tool's folder as `<folder>/mod.just`, with a one-line description; `just` lists them. `just tmux-dev start` is the one way to run the stack.
- For agents: a script in `scripts/.internal/`, or in `Myapp.Gate/scripts/` when it checks quality; each answers `--help`. A recipe only calls the script.
- `check.sh --quick` while working, `check.sh` before calling a change done. `e2e.sh` is not in the
  gate: run it after changing anything a user clicks.

## Git

- **The main branch is `master`**, never `main`.
- Work on a feature branch, never on `master`. Agents do not rewrite or discard history: no
  amend, reset, rebase, stash, force-push or deleting an unmerged branch. Merging and releasing are the owner's.
- **A branch merged into `master` is deleted** right after the merge: local, remote and its worktree, with
  `git branch -d`, which refuses unmerged work. Needed again? Start a new one from `master`.
- A change with a red gate is not committed: it stays in the working tree and the summary says why.
- **One task, one branch.** Unrelated refactors, renames, version bumps and fixes go to their own
  branch or into `TODO.md`; say so once when you reach for one, and put it in if the owner confirms.
- Personal rules (how you commit, your tools) go in the gitignored `CLAUDE.local.md`, not here.

## Rules

- KISS and YAGNI first: the repo grows only when a real need shows up; one developer maintains it after hours. When in doubt, leave it out, and say in the change what was left out and why.
- Plan a change by listing what it makes obsolete, and delete that first, in the same change.
- Mutation tests (`just mutate`) run only when a person asks for them: an agent neither runs nor proposes them on its own.
- Correction or a new rule? Make the mistake impossible or loud first: a type → a lint → a test → a hook. Add a sentence here only for what none of these can check.
- A guard's message states the fix. Never weaken a guard to make a change pass: fix the change.

## Specs

A spec's acceptance criteria live as guardrails: `Myapp.Gate/scripts/criteria.sh` and `Myapp.Gate/criteria.json` say what fits, its messages the fix.
What it cannot see:

- Set ids with `vdoc.py ids`, never by hand: a dropped id is never reused, an id is never renumbered.
- A criterion no automated check can prove is a wrong criterion: ask the owner, never keep it unguarded.
- A criterion gets the lightest check that can fail for it: a plain test before a server test, a server test before a browser test; browser tests cover what only a browser can.
- A spec written before its feature is built is a draft in `docs/draft/spec/`, which the gate does not read; it moves to `docs/visual/spec/` when its feature is built, together with the tests that guard its criteria.
- Before merging a spec'd feature, run `/suwagent:visual-guardrails`: `criteria.sh` sees that an id is named, not that the test would fail.

## Review

A person also reads the diff only when the change touches the always-careful list: login and permissions,
payments, data migrations, code without characterization tests. The list is revised after incidents.

## Where code goes

[docs/architecture.md](docs/architecture.md). Read it before adding a folder, a project or a feature.
Docs say what and why, not where: a line that must change when the code moves does not belong in the docs.

## What no test can check

- Names are chosen word by word: domain words in the users' language (English here), software
  words in technical English. One word per thing, everywhere.
- Secrets never enter the repo: not in code, settings, tests or history. Local values live in
  user secrets or the gitignored `.env`; a real hit is removed and rotated, never allow-listed.

## No comments

No comments in project files: agents mostly use comments to excuse bad code and comments rot, so names, small functions, types and tests say what a comment would; if code is unclear without a comment, fix the code.

- Covered: every code and config file (C#, TypeScript, JavaScript, shell, YAML, Dockerfiles, ignore files, `.editorconfig`, MSBuild and XML files, `index.html`, JSON, SQL, CSS, justfiles), `///` docs, JSDoc, commented-out code and task markers included. Not covered: Markdown, `docs/` and data strings (UI texts, messages).
- Not scanned: lockfiles, generated files (`openapi.d.ts`, `contract/openapi.json`, generator headers with them), `node_modules`, `bin`, `obj`, `.artifacts`, `docs/`.
- Allowed, because a tool reads them (the guards hold the same list, nothing is judged by eye):
  - every comment in `.env.example`; in a justfile, the one line directly above a recipe or `mod` that `just --list` shows;
  - a shebang `#!` on the first line; `# shellcheck <directive>=…` in shell scripts; `# syntax=…` on the first line of a Dockerfile;
  - `// @ts-expect-error <reason>`, `// @vitest-environment <env>`, and `eslint-disable` directives only where nothing else works;
  - in C#, `/*lang=…*/` and `// <auto-generated>`;
  - a comment that is exactly a criterion id where `criteria.sh` reads it: `// AC-x-n` in `eslint.config.js` and the tsconfigs, `# AC-x-n` in `.editorconfig`, `<!-- AC-x-n -->` in `Directory.Build.props`.
- `#pragma` and the null-forgiving `!` are banned outright; no comment excuses them.
- A reason that cannot live in code goes into a test, the spec, this file or `docs/` as a sentence. A new format that needs a comment to work goes on this list and into its guard in the same change.
