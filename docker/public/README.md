# Local qbtmud container

This image combines the international qbtmud release with LinuxServer qBittorrent. It preserves the inherited entrypoint, qBittorrent service runner, ports and `/config` volume. LinuxServer does not maintain or support this derivative.

Local helpers build and test images. The public release workflow publishes verified stable releases to `ghcr.io/lantean-code/qbtmud` and checks daily for LinuxServer base updates; see [the publication guide](../PUBLISHING.md). The private derivative remains a separate delivery stage. No image is published by running the local helpers.

## Build

You need Docker with BuildKit and PowerShell 7. Start Docker Desktop or your Docker engine, then run from the repository root:

On Windows, the helpers also detect Docker Desktop's standard system and per-user installation paths when the current terminal's PATH predates installation.

```powershell
./docker/build-local.ps1 -Run
```

No release parameters or GitHub login are needed. The script reads the verified 3.0.0 international release from `docker/ui-release.json` and the pinned LinuxServer base from `docker/base-image.json`, downloads the ZIP if it is not already cached, verifies its checksum, builds `qbtmud:local`, and starts the local test UI at **http://localhost:18080**.

Read the temporary admin password and stop the test container with:

```sh
docker compose -p qbtmud-local-test -f docker/compose.yaml logs
docker compose -p qbtmud-local-test -f docker/compose.yaml down
```

The named test volumes preserve this test profile between runs and keep it separate from production. The test UI uses port 18080 and torrent connections use 16881, avoiding the usual production defaults. To discard only this project's test profile and downloads, use the same `down` command with `--volumes`; that deletes the test data.

For a build without starting the UI, use `./docker/build-local.ps1`. To build and run automated checks, use `./docker/build-local.ps1 -Test`. To test an already-built image, use `./docker/test-local.ps1`; automated checks additionally require Python 3. All commands work on Windows and Linux with PowerShell 7. The Docker build needs neither a .NET SDK nor WebAssembly workloads.

Every rerun extracts into a fresh temporary tree, checks its assets and international version, then replaces only the generated staging directory. A malformed or incorrectly checksummed archive leaves the existing staged assets intact. Never use live qBittorrent profile directories as build staging. `-StageOnly` prepares assets without Docker; `-Platform linux/arm64` selects an arm64 build on a compatible builder. `-Tag` changes the local image name.

Explicit `BaseImage`, `Archive`, `ArchiveSha256`, `UiVersion`, `UiCommit` and `UpstreamTag` overrides remain available for CI or maintainer builds. A different UI release must have verified archive/commit metadata supplied explicitly or recorded in `ui-release.json`; the helper does not silently choose branch-head or prerelease assets. A version string alone is insufficient publication provenance.

`docker/base-image.json` records the pinned upstream index. Base changes require a new local build and full startup tests. Image labels identify the UI checksum/commit, packaging commit and dirty working-tree status, and upstream tag/digest. The script records local build inputs with `publicationEligible=false`; it does not assert publication-grade provenance from a caller-supplied commit. Do not distribute an uncommitted local build as an immutable public release.

## Run

```sh
docker compose -p qbtmud-local-test -f docker/compose.yaml up -d
```

Adjust identities, timezone and host ports for your installation. Compose uses dedicated named test volumes for `/config` and `/downloads`. Use isolated paths for testing and never share a production profile with an experiment. The test WebUI is available at port 18080; obtain the initial temporary admin password from container logs and set a permanent password. The standard contract starts container initialisation as root and runs qBittorrent as `abc` using `PUID`/`PGID`. Docker `--user`/Compose `user:` is not supported by this derivative yet.

The image enforces exactly these preferences on each startup:

```ini
[Preferences]
WebUI\AlternativeUIEnabled=true
WebUI\RootFolder=/opt/qbtmud
```

Other preferences, including credentials, authentication protections, ports and download paths, remain under LinuxServer/qBittorrent/user control. Changing either managed preference in the running UI takes effect temporarily; the next start restores it. The compiled UI lives at `/opt/qbtmud/public/` independently of persisted configuration. Avoid mounting over `/opt/qbtmud` or `/custom-cont-init.d`.

The configuration helper validates the INI structure without reserialising Qt values, repairs only the two managed keys, collapses their canonical duplicates, preserves ownership/mode, and atomically replaces changed content. A compliant file remains byte-for-byte unchanged with the same mtime. Section matching recognises Qt whitespace and ASCII percent escapes. Ambiguous duplicate Preferences sections, escaped or alternate-path aliases of managed keys, malformed input and symlinked config files fail safely without rewriting the original. Multiline quoted values and backslash-continued records are rejected because physical lines inside them cannot safely be treated as preferences. Literal control characters other than tabs and normal LF/CRLF terminators are rejected too. Ordinary single-line quoted values, escaped quotes, inline semicolon comments, unrelated encoded names and Qt values remain intact. Idempotence concerns the helper; qBittorrent may write its own configuration while running or shutting down.

## Startup gate

LinuxServer custom hooks log failure and continue, so the image adds two s6 oneshots. `init-qbtmud-state` depends on `init-mods-end` and resets per-start state before `init-custom-files`. `init-qbtmud-verify` depends on `init-custom-files`; `svc-qbittorrent` additionally depends on successful verification. The inherited runner remains intact.

The gate checks the current generation/success marker, UI readability as `abc`, effective preferences and configuration writability. The build requires essential boot files and writes a SHA-256 manifest outside the public UI tree. Both hook and final gate verify every bundled asset against that manifest as `abc`; missing, empty or corrupted files fail validation even when their directories remain present. The installed UI must match the image's bundled payload; customise it through a new build rather than modifying files at runtime. `S6_BEHAVIOUR_IF_STAGE2_FAILS=2` stops the container if initialisation fails; do not override it. A skipped hook or a stale previous-start marker cannot authorise service startup. Additional custom hooks must leave the two preferences intact. A private hook must participate in a failure-result contract before that derivative can be supported; private configuration is not included here.

## Validate

```sh
bash docker/tests/test-config.sh
python3 docker/tests/smoke-test.py qbtmud:local
```

The config suite covers missing keys/section, duplicates, exact matching, Qt section/key aliases, unrelated encoded names, Qt values, comments, CRLF, malformed/read-only input, ownership/mode and no-write idempotence. The smoke suite reruns it against the base image's actual tools and uses disposable Docker volumes with external networking disabled. It checks real asset serving, authenticated preferences, permanent credentials, restart/repair, mapped ownership, and startup rejection for invalid/aliased configuration, missing/empty/corrupted assets, skipped hooks and stale state. No torrents are created or downloaded. All test containers and volumes are removed afterwards.

The manual build workflow tests native amd64 and arm64 images separately and exports the exact tested images as workflow artefacts. It has read-only repository permissions and no registry login or push step. Public GHCR visibility, moving/immutable tag policies, scheduled refresh, public licence-distribution requirements and private image access must be implemented and validated before publication.

See [LinuxServer configuration](https://docs.linuxserver.io/images/docker-qbittorrent/) for supported settings and [s6 failure behaviour](https://github.com/just-containers/s6-overlay#customizing-s6-overlay-behaviour) for the startup policy.
