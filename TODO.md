# TODO

<!-- template:start -->
Decided for the template, not built yet; each arrives as its own change when it is needed.

- A CI step that publishes the images: the registry is project-specific (`.github/workflows/ci.yml` builds and smoke-tests them, nothing more).
- What is left of the shared kits (adopted: suwgate with `Suwgate.Architecture` and `Suwgate.Database`, suwfrontkit, `Suwbackkit.Server` and `Suwbackkit.Postgres`):
  1. TanStack Query (the kit's `createQueryClient`) in place of the screens' promise kept in state, when a screen needs caching or refetching.
  2. `Suwgate.Testing` (a PostgreSQL fixture, characterization and e2e helpers) replaces `TestDatabase`, `TestApp` and `ClassServer` of `Myapp.Server.Tests`.
  3. suwbackkit generates the API contract and types that `api-types.sh` does; then a CLI and SDK skeleton (suwagcms and suwageval have twin sets today).
- Lookups that went with the deleted scripts come back when suwgate has them: the guardrails of one criterion (`just criteria AC-…`) and the tests that pin another repository's contract (`just contracts`).
- The README pictures still show the old gate scripts (a check script, and the browser tests as a run of their own): edit `docs/visual/board/readme.html` and render them again.
<!-- template:end -->

## When the first spec's criteria need a strength check

The `criteria` step of `suwgate check` sees that a guardrail names a criterion, not that it would fail. The report that
measures it: run `just mutate` (Stryker.NET), read its JSON report (each mutant's `coveredBy` and
`killedBy` test ids), and count per `AC-<feature>-<n>` the mutants under the tests whose `Trait`
names it and how many survived. Stryker's `test-case-filter` does not work with the xUnit v3 runner
on Microsoft Testing Platform, so it reads a full run instead of running one criterion's tests.
First check that the `mtp` runner fills `coveredBy` per test.

## When the first fake of an outside service appears

The first fake in tests (WireMock for an external API, a mail catcher, e2e seed data) brings a
test environment of its own: a folder with its compose file, and a `just` module; `dev.compose` in `suwgate.yml`
points `suwgate dev db` there. Until then the gate needs only the one PostgreSQL container of `compose.yaml`.
