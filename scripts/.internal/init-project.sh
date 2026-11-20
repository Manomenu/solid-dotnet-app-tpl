#!/usr/bin/env bash
set -euo pipefail
if [ "${1:-}" = "--help" ]; then
    cat <<'USAGE'
The template's post action (dotnet new, through init-project.proj): run once in a project created from the template, never in the
template itself. It removes the template notes, regenerates the lockfiles, the API contract and the web types, formats the code,
creates docs/visual/spec/, the git repository on master with the hooks enabled, and deletes itself.
USAGE
    exit 0
fi
SELF="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
cd "$(dirname "$SELF")/../.."

if [ -d .template.config ]; then
    echo "init-project.sh runs in a project created from the template, not in the template itself." >&2
    exit 1
fi

done_steps=()

for file in README.md TODO.md docs/architecture.md; do
    if [ -f "$file" ] && grep -q '<!-- template:start -->' "$file"; then
        sed -i '/<!-- template:start -->/,/<!-- template:end -->/d' "$file"
        done_steps+=("removed the template notes from $file")
    fi
done

solution="$(ls ./*.slnx | head -n 1)"
dotnet restore "$solution" --force-evaluate --nologo -v quiet
dotnet tool restore >/dev/null
done_steps+=("regenerated the NuGet lockfiles (dotnet restore --force-evaluate)")

web="$(ls -d ./*.Web | head -n 1)"
dotnet csharpier format . >/dev/null
done_steps+=("formatted the C# files (the new name changes using order and line widths)")
if command -v pnpm >/dev/null 2>&1; then
    (cd "$web" && pnpm install --silent && pnpm exec prettier --write --log-level warn .)
    done_steps+=("regenerated the pnpm lockfile in ${web#./} and formatted its files")
    bash scripts/.internal/api-types.sh >/dev/null
    done_steps+=("regenerated the API contract (openapi.json), which carries the project's name, and the web types from it (api-types.sh)")
else
    done_steps+=("SKIPPED pnpm install and api-types.sh in ${web#./}: pnpm is not installed (corepack enable); run both before the gate")
fi

mkdir -p docs/visual/spec
touch docs/visual/spec/.gitkeep
done_steps+=("created docs/visual/spec/ for the living specs (the template leaves its own docs/visual/ out)")

if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    done_steps+=("left git alone: this folder is already inside a repository")
else
    git init -b master -q
    git config core.hooksPath .githooks
    done_steps+=("created a git repository on master and enabled the hooks (.githooks needs gitleaks to commit)")
fi

rm -- "$SELF" "${SELF%.sh}.proj"
done_steps+=("deleted scripts/.internal/init-project.sh and init-project.proj")

executables_dotnet_new_wrote_without_their_bit=(scripts/.internal/*.sh .githooks/pre-commit)
chmod +x "${executables_dotnet_new_wrote_without_their_bit[@]}"
git add --chmod=+x -- "${executables_dotnet_new_wrote_without_their_bit[@]}"
done_steps+=("made the scripts and the pre-commit hook executable and staged them so (git add --chmod=+x)")

echo "Project ready. What init-project.sh did:"
printf '  - %s\n' "${done_steps[@]}"
echo "Next: just check, then commit (the first commit is yours)."
