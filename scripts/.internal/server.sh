#!/usr/bin/env bash
# The server on :6300 under `dotnet watch`, rebuilt on every change, with the settings of .env; `just tmux-dev start` runs
# it in its pane after db.sh up. Its JSON log is also written to .artifacts/logs/server.jsonl, which `just logs` reads.
# Arguments go to `dotnet run`.
set -euo pipefail
if [ "${1:-}" = "--help" ]; then
    sed -n '2,/^[^#]/{/^#/s/^# \{0,1\}//p}' "$0"
    exit 0
fi
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
# Why: the local settings live in the gitignored .env, which a script a tmux pane runs does not get from just
if [ -f .env ]; then
    set -a
    # shellcheck source=/dev/null
    source .env
    set +a
fi
mkdir -p .artifacts/logs
dotnet watch --quiet --project Myapp.Server run "$@" 2>&1 | tee .artifacts/logs/server.jsonl
