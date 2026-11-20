set shell := ["bash", "-euo", "pipefail", "-c"]

suwgate := "Myapp.Web/node_modules/.bin/suwgate"

# The local stack in one tmux session, the one way to run it — `just tmux-dev start [--restart]|stop` (tmux-dev.just)
mod tmux-dev

[private]
default:
    @just --list --unsorted

# Why did it fail? The last warnings and errors of the newest server log, one line each (--all, --trace <id>, --compose, -n N)
[group('run')]
[positional-arguments]
logs *args:
    @{{ suwgate }} dev logs "$@"

# The local PostgreSQL of compose.yaml: up, down, reset, psql, logs, status
[group('run')]
db *args:
    @{{ suwgate }} dev db {{ args }}

# Smoke test of the running container stack (just smoke, or just smoke <base-url>)
[group('run')]
smoke *args:
    @{{ suwgate }} dev smoke {{ args }}

# Regenerate the API contract (contract/openapi.json) and the web app's types from it, after changing a record the API sends or receives; then raise the contract's version in contract/package.json
[group('dev')]
api-types:
    @./scripts/.internal/api-types.sh

# Browser tests (Playwright) against a real server and an empty database of their own; not part of `just check`
[group('maintenance')]
e2e *args:
    @./scripts/.internal/e2e.sh {{ args }}

# The whole gate: build with analyzers, csharpier, every test, the web checks, the API contract and its version, acceptance criteria, shellcheck, gitleaks (the same CI runs)
[group('maintenance')]
check:
    @{{ suwgate }} check

# The quick gate: the repository guards, and build, csharpier, eslint, tsc, shellcheck for what changed (what the Claude Code Stop hook runs)
[group('maintenance')]
check-quick:
    @{{ suwgate }} check --quick

# Which .NET tests take the longest: runs them all once and lists the slowest (-n 25 for more); a report, never a check
[group('maintenance')]
[positional-arguments]
slow-tests *args:
    @./scripts/.internal/slow-tests.sh "$@"

# Format the code: CSharpier for C#, Prettier for the web app
[group('maintenance')]
fmt:
    dotnet tool restore >/dev/null && dotnet csharpier format .
    cd Myapp.Web && pnpm exec prettier --write --log-level warn .

# Mutation testing of the server code changed since master (Stryker.NET): a surviving mutant is a test that is too weak
[group('maintenance')]
mutate:
    @{{ suwgate }} dev db up >/dev/null
    dotnet tool restore >/dev/null
    cd Myapp.Server.Tests && dotnet tool run dotnet-stryker

# Pack the dotnet new template as a NuGet package into .artifacts/nupkg
[group('dev')]
pack-template:
    dotnet pack pack/Solid.DotnetApp.Template.csproj -o .artifacts/nupkg

# Install tools and packages from the lockfiles, and enable the git hooks
[group('maintenance')]
sync:
    git config core.hooksPath .githooks
    dotnet tool restore
    dotnet restore Myapp.slnx
    cd Myapp.Web && pnpm install --frozen-lockfile
