# Local container validation

Validated on 13 October 2026. This delivery implements public local packaging, config/startup tests, Compose documentation, a build-only manual workflow and a public publication workflow. No Git commits, pushes, public registry publications, package settings or private infrastructure were created. A disposable localhost registry was used to validate transfer and promotion.

## Inputs and output

- UI: published international qbtmud 3.0.0, `qbt-mud-v3.0.0-intl.zip`.
- Archive SHA-256: `b3228420b8f66c9d5aa59ab0acdb6a6ff14321f4167a9a7e5b0f969c1973be58`.
- UI/tag commit: `ec93ae97ed294d4730b414511a36ebc26aeb3037`; successful stable build [34381649201](https://github.com/lantean-code/qbtmud/actions/runs/34381649201) records that commit on `master`.
- LinuxServer tag: `5.2.4_v2.0.15-ls480` (qBittorrent 5.2.4, libtorrent 2.0.15).
- Pinned upstream index: `sha256:97333cc1ccf903e0bbcd8eb67e3332e4970e9c1ecabe145e499e9c0847ebcaed`.
- Local image: `qbtmud:local`, native `linux/amd64`.
- Image ID: `sha256:6d47dc24c2434ab3dcd1539408e85aa9db2dcaf7e42eda3ee2b04c4b46b93558` (local image configuration digest, not a published multi-architecture index).
- Export: ignored `docker/.build/qbtmud-local-amd64.tar.gz`, SHA-256 `33b59df006ebb757cbc401b403925790101d78a7ecb412c56db3d091b17c8acc`.
- Packaging is uncommitted and labelled dirty. Local build metadata sets `publicationEligible=false`.

Load the exact tested image into another Docker engine:

```sh
docker load --input docker/.build/qbtmud-local-amd64.tar.gz
docker compose -f docker/compose.yaml up -d
```

## Results

- Local BuildKit image build succeeded without warnings, using a temporary isolated WSL Docker engine.
- Configuration suite (29 scenarios plus read-only, symlink and missing-file checks) passed both on Ubuntu and against the image's actual tools: key/section insertion, wrong values, duplicates, exact matching, Qt values, comments, CRLF, mode retention, no-write idempotence including a missing final newline, and rejection of malformed, read-only, symlinked and missing files. Review regressions additionally cover padded/escaped duplicate Preferences sections, percent-escaped/Unicode-escaped managed keys, alternate separator/group paths, and preservation of unrelated encoded names; both verify and write reject ambiguous aliases without changing bytes. Further confirmation regressions reject multiline quoted values, backslash continuations and embedded control characters while preserving ordinary single-line quoting, escaped quotes and inline comments.
- Container integration tests passed: fresh seeding, international static asset serving, session-cookie authentication, permanent password persistence, managed-key repair across restart, unrelated download-path preservation, custom WebUI/torrent ports, and mapped `PUID=1234`/`PGID=1235` permissions.
- Failure-injection tests passed: malformed/read-only config, missing UI, skipped hook and stale previous-start success state. The remediated image also rejects quoted multiline bypasses, padded section and escaped key aliases, a selectively missing Blazor loader, an empty framework tree, an empty dotnet.js and corrupted native WebAssembly. Every bundled asset is verified against a build-generated SHA-256 manifest outside the public UI tree. These checks verified no qBittorrent process or WebUI listener started, the container exited unsuccessfully, and malformed config was retained.
- All disposable containers and volumes were removed; no torrents were added or downloaded.
- Solution build passed with the project-local pinned .NET SDK 10.0.100: zero warnings and errors.
- The required `dotnet test --artifacts-path=/tmp/artifacts/qbtmud` invocation failed before test execution because the option is forwarded to the native xUnit runner, which rejects it with exit code 5. The six built test executables were then run directly: all 3,199 tests passed, zero failed/skipped/not run. Existing .NET source and test configuration were not changed.
- Workflow/Compose YAML and PowerShell syntax were checked. Changed CRLF-governed files were normalised with `unix2dos`; shell, Python and awk sources remain LF. Git whitespace checks passed.

- Confirmation review: the same reviewer rechecked all four reported defects and returned **No findings**. The exact multiline fixture was independently rejected; ordinary single-line quoting and comments still passed. Native arm64 validation remains outstanding.

- Convenience helper validation: the pinned no-parameter download/checksum/staging path and repeat staging passed; invalid ZIP/checksum inputs retained existing staging. `build-local.ps1 -Test` built the default image and passed the full smoke suite, the standalone `test-local.ps1` also passed against the final image, and `-Run` started the separate local test project and served the 3.0.0 international boot settings at port 18080. Docker/Python adapters targeted the isolated WSL engine; only interactive Compose validation used a temporary host-network override because that engine has no bridge. The normal Compose configuration was validated by the official plugin. Playwright DOM validation was unavailable because its server has no Chrome installation.

## Remaining delivery stages

The public publication implementation is described in `PUBLISHING.md`. Native arm64 GitHub Actions execution, actual GHCR publication and public visibility/access verification remain pending; workflows cannot be dispatched from these uncommitted files. Bundled-software redistribution/source-delivery requirements still need maintainer review before distribution. Scheduled upstream refresh is implemented and validated below; a separate private derivative remains deferred. Do not infer those completion gates from local image validation.

## Publication implementation validation

- Verified real published 3.0.0 provenance through the GitHub API: release ID `385712229`, international asset ID `553224644`, matching asset SHA-256, resolved release-tag/UI commit and successful master push build `34381649201`.
- Actionlint passed for `container-publish.yml`, `container-build.yml` and the modified UI release workflow. The existing UI workflow's obsolete checkout v2 was updated to v4 so it passes the current validator.
- Thirteen policy/orchestration/OCI assembly tests passed, covering stable eligibility, numeric ordering, immutable-input/platform validation, registry errors, exact retry digests, late approval changes, per-combination revisions, stable build provenance and retained platform manifests/annotations.
- New publication candidates passed the complete Docker Desktop smoke suite on native amd64 and locally emulated arm64. The arm64 build initially hit an upstream HTTP 429 and succeeded on retry. Native arm64 CI remains outstanding.
- Actual archive conversion revealed that OCI media-type conversion reserialises configuration JSON; the exporter now preserves Docker schema2/config bytes within the OCI transport archive and verifies configuration plus filesystem-layer identities against the tested image.
- Both retained platform archives were transferred with digest preservation to a disposable local registry. The actual assembly code produced an annotated OCI index: `sha256:533e4d6867fc33bf38210a0ba29c8ae8985a3cf8ab552ef2aceee5687268aeb2`. This local fixture uses reduced test annotations, not publication-grade source inputs. The registry and convenience alias retained that exact digest and both original platform manifest/configuration digests.
- Buildx assembly from Docker schema2 sources omitted provenance annotations in the initial experiment. Publication instead assembles an OCI index locally and transfers the entire verified index with Skopeo; Buildx is used only for carbon-copy alias promotion.
- The same reviewer confirmed all three publication corrections and the final OCI transfer/assembly changes with **No findings**.
- The prescribed .NET test command was attempted; no .NET source changed. Current repository test-runner limitations are recorded above. No claim is made that GitHub Actions or public GHCR publication has run.

## Scheduled upstream refresh validation

- The public workflow checks daily at 06:17 UTC and supports manual `check_upstream`. It shares the existing publication concurrency guard and skips build/publish jobs when the accepted base and verified stable UI are unchanged.
- The live resolver verified LinuxServer's current `latest` as `5.2.4_v2.0.15-ls480@sha256:97333cc1ccf903e0bbcd8eb67e3332e4970e9c1ecabe145e499e9c0847ebcaed`, matching the bootstrap pin. Both architectures' version/build labels, named-tag/index agreement and supported platforms were checked read-only.
- Twenty-four policy, orchestration and assembly tests passed. Added coverage verifies unchanged checks, same-tag digest revision allocation, mixed architectures, tag races, libtorrent-family/downgrade rejection, accepted-base preservation and late UI/base/packaging changes. Live validation also caught and corrected Skopeo's rejection of `tag@digest` references without changing the Docker build's pinned reference.
- Actionlint passed for all three container/UI workflows. The same reviewer confirmed the upstream automation and freshness remediation with **No findings**.
- The required .NET invocation again reported zero tests with native-runner exit code 5; no .NET source changed. Existing image smoke validation remains applicable because the image runtime was unchanged by this scheduling work.
- No scheduled workflow was activated remotely, and no images or settings were published or changed on GitHub. Activation requires pushing these files to the default branch and completing the first-publication setup.
