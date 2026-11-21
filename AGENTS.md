# AGENTS.md

Rules for anyone — human or AI agent — changing this repository. `CLAUDE.md` only points here;
this file is the one to edit. Setup: [docs/development.md](docs/development.md). Not built yet: [TODO.md](TODO.md).

Read @AGENTS.shared.md first: the rules every repository of the stack shares. This file adds this repository's own and wins where it says otherwise.

## Commands

- For people: recipes also live in a module of the whole repository beside `justfile` (`tmux-dev.just`) or in a tool's folder as `<folder>/mod.just`. `just tmux-dev start` is the one way to run the stack.
- For agents: a script in `scripts/.internal/`, which a recipe calls. The gate is
  `suwgate` (`Myapp.Web/node_modules/.bin/suwgate`), configured in `suwgate.yml`; `suwgate dev` runs the local stack.
- `suwgate step e2e -- --headed` after changing anything a user clicks; the full `suwgate check` also runs the browser tests, against a fresh database of their own.

## Rules

- This app holds only its wiring, its own config and its feature code. What every app needs (the gate and the .NET guards, the server
  defaults and the database, the look and the components) comes from the shared packages: npm `@maniumek/suwgate` and `@maniumek/suwfrontkit`,
  NuGet `Suwgate.*` and `Suwbackkit.*`. A fix or a missing piece goes to that package, never as a copy here.
- A closed set of string values in a record the API sends (`contract/openapi.json`) (a kind, a reason, the name of an event) is an enum in the contract,
  never a plain string, so a consumer generates it with the types and learns of a new value from the contract, not from a copy.
- Mutation tests run with `just mutate`.
- Names: the users' language here is English.

## Specs

A criterion gets a plain test before a server test, a server test before a browser test. The kinds of guardrail come with the profiles of `suwgate.yml`.

## Review

The always-careful list: login and permissions, payments, data migrations, code without characterization tests.

