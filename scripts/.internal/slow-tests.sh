#!/usr/bin/env bash
# Which .NET tests take the longest? Builds, runs every test project once with an xunit TRX report and lists the slowest
# tests. A report, not a check: it never fails for time, and the gate never runs it. A test far slower than its neighbours
# usually waits for something (a timeout, a server started per test) and passes anyway; look at those first.
#   slow-tests.sh        the 10 slowest tests, with how long each took
#   slow-tests.sh -n 25  the 25 slowest
# Exit 1 when a test fails (the list still comes first). Needs python3; starts the test database like check.sh.
set -euo pipefail
if [ "${1:-}" = "--help" ]; then
    sed -n '2,/^[^#]/{/^#/s/^# \{0,1\}//p}' "$0"
    exit 0
fi
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
COUNT=10
if [ "${1:-}" = "-n" ]; then
    COUNT="${2:?slow-tests.sh: -n needs a number, e.g. -n 25}"
fi
# shellcheck source=scripts/.internal/checkout-env.sh
source "$ROOT/scripts/.internal/checkout-env.sh"
RESULTS="$ROOT/.artifacts/slow-tests"
rm -rf "$RESULTS"
mkdir -p "$RESULTS"

"$ROOT/scripts/.internal/db.sh" up >/dev/null
dotnet build "$ROOT/Myapp.slnx" --nologo -v q >/dev/null
status=0
dotnet test --solution "$ROOT/Myapp.slnx" --no-build --no-progress --report-xunit-trx --results-directory "$RESULTS" >"$RESULTS/run.log" 2>&1 || status=$?

python3 - "$RESULTS" "$COUNT" <<'PY'
import glob, sys, xml.etree.ElementTree as ET

def seconds(duration):
    hours, minutes, rest = duration.split(":")
    return int(hours) * 3600 + int(minutes) * 60 + float(rest)

results = [
    (seconds(result.get("duration")), result.get("testName"))
    for path in glob.glob(f"{sys.argv[1]}/*.trx")
    for result in ET.parse(path).getroot().iter()
    if result.tag.endswith("UnitTestResult") and result.get("duration")
]
results.sort(reverse=True)
print(f"{len(results)} tests, the {min(int(sys.argv[2]), len(results))} slowest:")
for took, name in results[: int(sys.argv[2])]:
    print(f"{took:7.2f} s  {name}")
PY
if [ "$status" -ne 0 ]; then
    echo "slow-tests.sh: a test failed; the run's output is in $RESULTS/run.log" >&2
    exit 1
fi
