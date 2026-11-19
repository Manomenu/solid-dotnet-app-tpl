#!/usr/bin/env bash
set -euo pipefail

input="$(cat)"
path="$(printf '%s' "$input" | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -n 1 | sed 's/^[^:]*:[[:space:]]*"//; s/"$//')"
path="$(printf '%s' "$path" | tr '\134' '/')"
[ -n "$path" ] || exit 0

if [ "${ALLOW_GATE_EDIT:-}" != "1" ]; then
    case "$path" in
        */Myapp.Gate/layers.json) ;;
        */Myapp.Gate/* | */.claude/settings.json | */.github/workflows/* | */.editorconfig | */eslint.config.js | */BannedSymbols.txt | \
            */Directory.Build.props | */tsconfig*.json)
            echo "Blocked: $path is part of the gate (a check, a hook or a lint config), which an agent does not change on its own." >&2
            echo "Do not work around it: ask the user to start a session with ALLOW_GATE_EDIT=1 claude and give them the exact change to make there (the file, what to change, why)." >&2
            exit 2
            ;;
        */Myapp.Server.Tests/Characterization/*)
            git -C "$(dirname "$path")" ls-files --error-unmatch -- "$path" >/dev/null 2>&1 || exit 0
            echo "Blocked: $path records behaviour as it is today (a characterization test); an agent does not change a recording on its own, it changes the code until the recording passes again." >&2
            echo "Do not work around it: ask the user to start a session with ALLOW_GATE_EDIT=1 claude and give them the exact change to make there (the file, what to change, why)." >&2
            exit 2
            ;;
    esac
fi

case "$path" in
    */Database/Migrations/up/*)
        if [ -e "$path" ]; then
            echo "Blocked: $path is a migration that already exists; grate refuses to start when a script changes after it ran." >&2
            echo "Add a new numbered script instead: Myapp.Server/Database/Migrations/up/NNNN_what_it_does.sql (next free number)." >&2
            exit 2
        fi
        ;;
    */packages.lock.json)
        echo "Blocked: $path is a generated lockfile. Change Directory.Packages.props and run: dotnet restore Myapp.slnx" >&2
        exit 2
        ;;
    */pnpm-lock.yaml)
        echo "Blocked: $path is a generated lockfile. Change package.json and run: pnpm install" >&2
        exit 2
        ;;
    */openapi.d.ts | */contract/openapi.json)
        echo "Blocked: $path is generated from the server's endpoints. Regenerate it with scripts/.internal/api-types.sh (just api-types) instead of editing it." >&2
        exit 2
        ;;
esac
exit 0
