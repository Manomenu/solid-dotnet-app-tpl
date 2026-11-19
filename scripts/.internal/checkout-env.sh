#!/usr/bin/env bash
export MSBUILDDISABLENODEREUSE=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0
_checkout_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
_git_dir="$(git -C "$_checkout_root" rev-parse --absolute-git-dir)"
_common_dir="$(git -C "$_checkout_root" rev-parse --path-format=absolute --git-common-dir)"
if [ -z "${CI:-}" ] && [ "$_git_dir" != "$_common_dir" ]; then
    _name="$(basename "$_checkout_root")"
    _hash="$(printf '%s' "$_name" | cksum | cut -d' ' -f1)"
    _suffix="$(printf '%s' "$_name" | tr '[:upper:]' '[:lower:]' | tr -c 'a-z0-9_' '_' | cut -c1-30)_$_hash"
    _base=$((20000 + _hash % 5000 * 2))
    export E2E_DATABASE="${E2E_DATABASE:-myapp_e2e_$_suffix}"
    export TEST_CONNECTION_STRING="${TEST_CONNECTION_STRING:-Host=127.0.0.1;Port=5533;Database=myapp_test_$_suffix;Username=myapp;Password=myapp}"
    export E2E_SERVER_PORT="${E2E_SERVER_PORT:-$_base}"
    export E2E_WEB_PORT="${E2E_WEB_PORT:-$((_base + 1))}"
    echo "checkout $_name: test database myapp_test_$_suffix, e2e database $E2E_DATABASE, e2e ports $E2E_SERVER_PORT/$E2E_WEB_PORT" >&2
fi
export E2E_DATABASE="${E2E_DATABASE:-myapp_e2e}"
export E2E_SERVER_PORT="${E2E_SERVER_PORT:-6301}"
export E2E_WEB_PORT="${E2E_WEB_PORT:-3301}"
unset _checkout_root _git_dir _common_dir _name _suffix _base _hash
