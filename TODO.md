# TODO

<!-- template:start -->
Decided for the template, not built yet; each arrives as its own change when it is needed.

- A CI step that publishes the images: the registry is project-specific (`.github/workflows/ci.yml` builds and smoke-tests them, nothing more).
- TanStack Router with typed routes arrives with the second screen: a link to a missing route or a wrong param becomes a compile error.
- The shared kits, in this order (the plan agreed with the owner on 2026-11-20):
  1. suwgate (public, GitHub Manomenu/suwgate), adopted: `suwgate check` from npm `@maniumek/suwgate`, configured in `suwgate.yml`, the hooks, the tool presets, `suwgate dev` and CI's reusable gate. Left for when NuGet `Suwgate.Architecture` and `Suwgate.Database` are published: they replace the .NET guards that stay in `Myapp.Gate` (layers, project references, interfaces, naming, forbidden type names, escape hatches, unused types, the fixture rule), `BannedSymbols.txt`, the build defaults of `Directory.Build.props`, the analyzer rules of `.editorconfig` and the migration name test, and `suwgate check --quick` runs them again (it runs only the test projects that use those packages).
  2. suwfrontkit (npm `@maniumek/suwfrontkit`), adopted for the look and the components; its typed API client replaces `src/api/client.ts` when it ships.
  3. A backend inventory across the template and the apps decides what goes to suwbackkit and to `Suwgate.Testing` (a PostgreSQL Testcontainers fixture, characterization and e2e helpers).
  4. suwbackkit (NuGet): first the server defaults (logging, health checks, ProblemDetails, OpenAPI, Npgsql with Dapper and grate, the migrator and pool with their tests, generating the API contract and types that `api-types.sh` does), then a CLI and SDK skeleton (suwagcms and suwageval have twin sets today). The AI procedure engine lives in suwchatbot, and alfusion has its own deterministic engine; whether they merge into suwbackkit is an open question in suwbackkit's TODO.md.
- Lookups that went with the deleted scripts come back when suwgate has them: the guardrails of one criterion (`just criteria AC-…`) and the tests that pin another repository's contract (`just contracts`).
- The README pictures still show the old gate script: edit `docs/visual/board/readme.html` and render them again.
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
