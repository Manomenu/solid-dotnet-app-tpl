#!/usr/bin/env bash
# Run once by `dotnet new solid-dotnet` (its post action, which needs your consent: --allow-scripts yes, through init-project.proj) in the
# new project, never in the template itself. It regenerates what the template leaves out because it depends on the
# project's name (lockfiles), removes the template's notes, creates the git repository on `master` with the hooks
# enabled when the folder is not in one yet, says what it did, and deletes itself.
set -euo pipefail
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
else
    done_steps+=("SKIPPED pnpm install in ${web#./}: pnpm is not installed (corepack enable), run it before the gate")
fi

if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    done_steps+=("left git alone: this folder is already inside a repository")
else
    git init -b master -q
    git config core.hooksPath .githooks
    done_steps+=("created a git repository on master and enabled the hooks (.githooks needs gitleaks to commit)")
fi

rm -- "$SELF" "${SELF%.sh}.proj"
done_steps+=("deleted scripts/.internal/init-project.sh and init-project.proj")

# Why: dotnet new writes every file without its executable bit; git skips a hook that is not executable, and the
# index carries the mode on Windows too, where chmod alone does not reach it
executables=(scripts/.internal/*.sh .githooks/pre-commit)
chmod +x "${executables[@]}"
git add --chmod=+x -- "${executables[@]}"
done_steps+=("made the scripts and the pre-commit hook executable and staged them so (git add --chmod=+x)")

echo "Project ready. What init-project.sh did:"
printf '  - %s\n' "${done_steps[@]}"
echo "Next: bash scripts/.internal/check.sh, then commit (the first commit is yours)."
