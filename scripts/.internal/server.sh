#!/usr/bin/env bash
set -euo pipefail
usage() {
    cat <<'USAGE'
The server on :6300 under `dotnet watch`, rebuilt on every change, with the settings of .env; `just tmux-dev start` runs
it in its pane after db.sh up. Its JSON log is also written to .artifacts/logs/server.jsonl, which `just logs` reads.
Arguments go to `dotnet run`.
USAGE
}
if [ "${1:-}" = "--help" ]; then
    usage
    exit 0
fi
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
load_the_gitignored_env_file() {
    set -a
    # shellcheck source=/dev/null
    source .env
    set +a
}
if [ -f .env ]; then load_the_gitignored_env_file; fi
mkdir -p .artifacts/logs
dotnet watch --quiet --project Myapp.Server run "$@" 2>&1 | tee .artifacts/logs/server.jsonl
