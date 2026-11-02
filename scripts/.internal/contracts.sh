#!/usr/bin/env bash
# Which tests adapt this app to a contract of another repository, and to which version? A test that pins
# behaviour to a contract carries the contract's name and version instead of an acceptance criterion id:
#   server test (xunit):   [Trait("contract", "rabaty@v2")] on the test
#   web test (vitest):     the tag at the start of the title: it("contract rabaty@v2: the cart still shows the discount", …)
# Such a test needs no AC id and no spec here: its intent lives in the owner's spec, reached through the
# contract version. This script lists them so that, when a contract is bumped, every test hanging on the old
# version is one command away. It never fails; it is a lookup, not a gate.
#
#   contracts.sh              every contract tag: contract@version, file:line
#   contracts.sh rabaty@v2    the tests of one contract version
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

want="${1:-}"
found=0
# Why: a title may be quoted with ", ' or a backtick (prettier keeps template literals)
quotes="\"'\`"
while IFS=: read -r file line text; do
    # Why: a line matches one form, the other or neither; sed -n prints nothing instead of failing the pipeline
    # Why: only name@version is a tag; "contract terms" in a test's text is not
    tag="$(printf '%s' "$text" | sed -nE 's/.*"contract"[[:space:]]*,[[:space:]]*"([^"@]+@[^"]+)".*/\1/p' | head -n 1)"
    [ -n "$tag" ] || tag="$(printf '%s' "$text" | sed -nE "s/.*[$quotes]contract ([^ :@$quotes]+@[^ :$quotes]+).*/\1/p" | head -n 1)"
    [ -n "$tag" ] || continue
    if [ -n "$want" ] && [ "$tag" != "$want" ]; then continue; fi
    printf '%-24s %s:%s\n' "$tag" "$file" "$line"
    found=1
done < <(grep -rnE "Trait\(\"contract\"|[$quotes]contract [^ @]+@[^ ]+" --include='*.cs' --include='*.test.ts' --include='*.test.tsx' --include='*.e2e.ts' Myapp.Server.Tests Myapp.Web/src 2>/dev/null || true)
if [ "$found" -eq 0 ]; then
    echo "no contract tags${want:+ for $want} (a test adapting to another repository's contract carries [Trait(\"contract\", \"name@version\")] or 'contract name@version' in its title)"
fi
