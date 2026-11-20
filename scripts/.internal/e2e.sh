#!/usr/bin/env bash
set -euo pipefail
usage() {
    cat <<'USAGE'
End-to-end tests (Myapp.Web/src/**/*.e2e.ts): a real browser against the real server and database. Not part of
the gate (suwgate check). Arguments go to Playwright, e.g.
  e2e.sh --headed        watch the browser
  e2e.sh -g "note"       only the tests whose name matches

Every run starts from an empty database of its own (myapp_e2e; a linked worktree gets a suffixed name and its own
ports, suwgate dev checkout-env), next to the development one, and its own server and web app (playwright.config.ts):
what you run by hand is not touched. Locally it starts PostgreSQL (suwgate dev db up); CI provides it as a service.
USAGE
}
if [ "${1:-}" = "--help" ]; then
    usage
    exit 0
fi
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SUWGATE="$ROOT/Myapp.Web/node_modules/.bin/suwgate"
[ -x "$SUWGATE" ] || (cd "$ROOT/Myapp.Web" && pnpm install --frozen-lockfile --silent)
eval "$("$SUWGATE" dev checkout-env)"

if [ -z "${CI:-}" ]; then
    "$SUWGATE" dev db up >/dev/null
fi

recreate_outside_one_transaction=(-c "DROP DATABASE IF EXISTS $E2E_DATABASE WITH (FORCE)" -c "CREATE DATABASE $E2E_DATABASE")
if [ -n "${CI:-}" ]; then
    psql -q "postgresql://myapp:myapp@127.0.0.1:5533/postgres" "${recreate_outside_one_transaction[@]}"
else
    "$SUWGATE" dev db psql -q -d postgres "${recreate_outside_one_transaction[@]}"
fi

mkdir -p "$ROOT/.artifacts/logs"
dotnet build "$ROOT/Myapp.Server" --nologo -v q

cd "$ROOT/Myapp.Web"
exec pnpm exec playwright test -c e2e/playwright.config.ts "$@"
