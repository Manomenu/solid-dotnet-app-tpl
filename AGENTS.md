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
- A comment says *why*, never *what*; if code needs a *what* comment, rename the code.
- Secrets never enter the repo: not in code, settings, tests or history. Local values live in
  user secrets or the gitignored `.env`; a real hit is removed and rotated, never allow-listed.
