# JavaScript browser regressions

These C# xUnit tests execute the actual `boot.resources.js` and `interop.js` files in isolated headless Chromium pages using Microsoft.Playwright. The small HTML page supplies the external Blazor startup entry point and keyboard targets. Tests supply external .NET callback references and controlled network responses; production JavaScript functions are not replaced.

Build the host test project and install its browser runtime:

```powershell
dotnet build test/Lantean.QBTMud.Test/Lantean.QBTMud.Test.csproj --artifacts-path=/tmp/artifacts/qbtmud
& /tmp/artifacts/qbtmud/bin/Lantean.QBTMud.Test/debug/playwright.ps1 install --only-shell chromium
```

On Linux, add `--with-deps` to the install command. Use `release` instead of `debug` when building with `--configuration Release`.

Choose a fresh coverage directory for each run, then run the browser tests:

```powershell
$env:QBTMUD_JS_COVERAGE_DIR='/tmp/artifacts/qbtmud/javascript-coverage-run'
dotnet test --project test/Lantean.QBTMud.Test/Lantean.QBTMud.Test.csproj --artifacts-path=/tmp/artifacts/qbtmud --filter-namespace Lantean.QBTMud.Test.JavaScript
python tools/JavaScriptCoverage/report.py --input /tmp/artifacts/qbtmud/javascript-coverage-run --output /tmp/artifacts/qbtmud/javascript-coverage-report --base HEAD --repo-root .
```

The default raw coverage directory is `/tmp/artifacts/qbtmud/javascript-coverage`. Setting `QBTMUD_JS_COVERAGE_DIR` keeps separate runs independent. Missing browsers fail the tests; they are not silently skipped.

Each page captures native V8 precise coverage through a Chromium DevTools session. The collector saves the source returned for each script ID together with its function and block ranges. Reporting rejects missing or stale source, merges coverage across tests and writes `coverage.lcov`, `summary.json` and `summary.txt`.

LCOV reports line coverage for both scripts. V8 range counts are reported separately; they include function roots and are not Istanbul branch coverage. The line mapper uses the innermost native range at each code position, supports UTF-16 offsets and CRLF, and excludes comments and punctuation-only lines. Its lexical filter is scoped to these scripts and is not a full JavaScript parser.

CI installs Chromium, runs these tests as part of the normal solution suite, tests the coverage reporter and uploads both raw coverage and the report. It requires every changed executable line in the two scripts to be covered, comparing with the PR merge base or the preceding commit for other runs. Overall file coverage remains visible, including existing functions outside these regression scenarios. JavaScript coverage is reported separately from .NET coverage.

The tests cover CDN success and fallback responses, local failures and integrity checks, persisted font names and stylesheet deduplication, nested keyboard ownership, disposal order, repeated focus, editable inputs and default prevention. They do not start the complete Blazor application or qBittorrent.
