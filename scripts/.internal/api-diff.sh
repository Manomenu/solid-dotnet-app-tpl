#!/usr/bin/env bash
# Does this change break the API contract? Compares Myapp.Server/openapi.json of the working tree with the one on
# a base ref (default: master, or origin/master when master is not a local branch) and fails on a breaking change:
# a removed endpoint or field, a narrowed type, a new required parameter. Consumers in other repositories learn of
# such a change here, in the owner's CI, before any of them does. Compatible additions pass.
#
#   api-diff.sh [base-ref]   exit 1 on a breaking change, 3 when oasdiff is not installed, 0 otherwise
#
# Uses oasdiff (https://github.com/oasdiff/oasdiff): `oasdiff breaking base revision --fail-on ERR`. Run
# api-types.sh first when the server changed, so that the working tree's document is current.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
CONTRACT="Myapp.Server/openapi.json"

base="${1:-}"
if [ -z "$base" ]; then
    if git -C "$ROOT" show-ref --verify --quiet refs/heads/master; then base=master; else base=origin/master; fi
fi

if ! command -v oasdiff >/dev/null 2>&1; then
    echo "api-diff.sh: oasdiff is not installed (https://github.com/oasdiff/oasdiff/releases, or: brew install oasdiff)." >&2
    exit 3
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
if ! git -C "$ROOT" show "$base:$CONTRACT" >"$TMP/base.json" 2>/dev/null; then
    echo "api-diff.sh: no $CONTRACT on $base yet; nothing to compare against."
    exit 0
fi
if git -C "$ROOT" diff --quiet --no-index "$TMP/base.json" "$ROOT/$CONTRACT" 2>/dev/null; then
    echo "API contract unchanged since $base"
    exit 0
fi

if oasdiff breaking "$TMP/base.json" "$ROOT/$CONTRACT" --fail-on ERR; then
    echo "API contract changed since $base, compatibly (nothing breaks a consumer)"
else
    echo "api-diff.sh: the API contract breaks consumers (see above). Keep the old shape and add the new one beside it, or agree the break with every consumer first." >&2
    exit 1
fi
