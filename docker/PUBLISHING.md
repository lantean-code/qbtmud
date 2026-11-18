# Publish a container release

Image: `ghcr.io/lantean-code/qbtmud`.

## First publication

1. Merge the container implementation into the repository's default branch.
2. Review [bundled-software licences and source requirements](public/THIRD-PARTY-NOTICES.md), and arrange any required source distribution.
3. In organisation **Settings → Packages → Package Creation**, enable **Public**. Ensure repository Actions can create and write the organisation's GHCR package using `GITHUB_TOKEN`.
4. Run **Publish Public Container** from the default branch with release `3.0.0`.
5. Once the `qbtmud` package exists in GHCR, confirm its repository connection and set its visibility to **Public**. If the workflow failed its anonymous-pull check, rerun it after changing visibility.
6. Check that the workflow succeeded on both architectures and review the published digest and tags in its summary.

The initial tags are `3.0.0-qbt5.2.4-lt2.0.15-ls480-r1`, `3.0.0` and, if still the newest eligible stable release, `latest`.

## Later releases and rebuilds

- Publishing a stable GitHub release draft starts the container workflow. Releases need the international ZIP and provenance manifest produced by the UI build; 3.0.0 is already verified.
- If a release is published by another workflow using `GITHUB_TOKEN`, run **Publish Public Container** manually from the default branch with that release tag.
- LinuxServer's stable libtorrent 2 base is checked daily at 06:17 UTC. A changed base is pinned, built and tested on both architectures, then published with the newest stable qbtmud release. Unchanged inputs skip the build; failed tests leave the published aliases unchanged.
- To check upstream immediately, manually run **Publish Public Container** from the default branch with **check_upstream** enabled. The release input is ignored in this mode.
- Same-version upstream digest changes automatically receive a new packaging revision. Later qbtmud releases reuse the latest successfully published base, so `base-image.json` remains the local-build/bootstrap pin.
- To repackage an existing version combination manually, set `revision` in `publication.json` higher than that combination's published revision before merging. Existing combination tags cannot be overwritten.

## Failed or interrupted runs

### Supplemental verification for 3.2.0

The original 3.2.0 UI build passed its functional tests but failed the full-release JavaScript coverage check. Its release archives were built locally from the approved tag. This release has a separate, narrowly pinned verification path; other releases retain the existing build-provenance requirement.

Run **Verify unchanged 3.2.0 release** from the repository's default branch. It builds and tests the exact tagged source with only the supplementary split-panel test file from commit `a5cd97aad3d124b845d9bd6338b7020964dc15a6`, checks coverage against the previous master release and verifies both existing archive checksums. It can verify the draft and does not publish or modify the release, tag or assets.

The successful run retains a separate Actions evidence artifact for 90 days. Container publication checks the workflow identity, repository, default branch, successful run attempt, release and asset identities, pinned source, test commit and archive checksums. It records supplemental verification separately from UI build provenance. Missing or expired evidence blocks publication; rerun verification to renew it.

The release must still be published before **Publish Public Container** can build, test and publish its amd64 and arm64 images. Verification alone never publishes containers. Changes to the release archives, tag or supplementary tests require a fresh reviewed packaging policy.

### Retrying publication

Fix the reported cause and rerun. The workflow re-tests and reuses an existing matching immutable image, then completes eligible aliases. If it reports conflicting immutable inputs, increase the packaging revision and merge before starting a new run. If aliases were skipped because the default branch changed, start a new run from the current default branch.
