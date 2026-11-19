#!/usr/bin/env bash
set -euo pipefail
usage() {
    cat <<'USAGE'
The local PostgreSQL for development and tests: the db service of compose.yaml, published on the loopback at
127.0.0.1:5533, its data in a named volume that survives a stop. check.sh, `just mutate` and e2e.sh call it before the
tests, and `just tmux-dev start` before the server; a linked worktree shares it under its own database names
(checkout-env.sh). This repo works with docker (on Fedora: podman behind the docker command; see docs/development.md).
  db.sh up          start it, or leave it running, and wait until it answers; prints its address
  db.sh psql ...    psql in it as its owner, e.g. db.sh psql -d postgres -c "select 1"
USAGE
}
if [ "${1:-}" = "--help" ]; then
    usage
    exit 0
fi
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
COMPOSE=(docker compose -f "$ROOT/compose.yaml" --profile db)

case "${1:-}" in
    up)
        "${COMPOSE[@]}" up -d --wait db >/dev/null 2>&1 || {
            echo "db.sh: postgres did not start — see: docker compose --profile db logs db" >&2
            exit 1
        }
        echo "postgresql://myapp:myapp@localhost:5533/myapp"
        ;;
    psql)
        shift
        "${COMPOSE[@]}" exec -T db psql -U myapp "$@"
        ;;
    *)
        echo "usage: db.sh up | psql <args>" >&2
        exit 2
        ;;
esac
