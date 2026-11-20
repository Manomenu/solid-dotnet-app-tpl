# TODO

<!-- template:start -->
Decided for the template, not built yet; each arrives as its own change when it is needed.

- A CI step that publishes the images: the registry is project-specific (`.github/workflows/ci.yml` builds and smoke-tests them, nothing more).
- TanStack Router with typed routes arrives with the second screen: a link to a missing route or a wrong param becomes a compile error.
- The shared kits, in this order (the plan agreed with the owner on 2026-11-20):
  1. suwgate (public, GitHub Manomenu/suwgate): `suwgate check` from npm `@maniumek/suwgate` replaces the check script and the repository guards, configured in `suwgate.yml`; the .NET guards come from NuGet `Suwgate.Architecture` and `Suwgate.Database`; CI calls suwgate's reusable workflows.
  2. suwfrontkit (npm `@maniumek/suwfrontkit`): the web's look and components, and later its typed API client, replace the copies in the web project.
  3. A backend inventory across the template and the apps decides what goes to suwbackkit and to `Suwgate.Testing` (a PostgreSQL Testcontainers fixture, characterization and e2e helpers).
  4. suwbackkit (NuGet): first the server defaults (logging, health checks, ProblemDetails, OpenAPI, Npgsql with Dapper and grate), then a CLI and SDK skeleton (suwagcms and suwageval have twin sets today), and the AI procedure engine only when a second app needs it next to alfusion.
- The dev scripts (db, logs, smoke, tmux-dev) stay here; they move into `suwgate dev` only if they start to drift between repos.
<!-- template:end -->

## When the first spec's criteria need a strength check

`criteria.sh` sees that a guardrail names a criterion, not that it would fail. The report that
measures it: run `just mutate` (Stryker.NET), read its JSON report (each mutant's `coveredBy` and
`killedBy` test ids), and count per `AC-<feature>-<n>` the mutants under the tests whose `Trait`
names it and how many survived. Stryker's `test-case-filter` does not work with the xUnit v3 runner
on Microsoft Testing Platform, so it reads a full run instead of running one criterion's tests.
First check that the `mtp` runner fills `coveredBy` per test.

## When the first fake of an outside service appears

The first fake in tests (WireMock for an external API, a mail catcher, e2e seed data) brings a
test environment of its own: a folder with its compose file and scripts, and a `just` module;
`db.sh` moves there. Until then the gate needs only the one PostgreSQL container from `db.sh`.
