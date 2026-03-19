# README Screenshots

The README screenshots are generated from a real qBittorrent WebUI session running qbtmud as the alternative WebUI.

## What This Uses

- A disposable WSL qBittorrent profile started only for the capture run.
- Synthetic local fixture payloads from [`readme-fixtures/payloads`](./readme-fixtures/payloads).
- Generated `.torrent` files created from those payloads.
- A small capture tool in [`tools/ReadmeScreenshots`](./).

## Generate The Screenshots

Run the WSL orchestration script:

```bash
./tools/ReadmeScreenshots/run-readme-screenshots.sh
```

The script will:

1. Publish qbtmud to a temporary output folder.
2. Stage the published site into a temporary alternative WebUI root that contains `public/`, matching qBittorrent's expected layout.
3. Generate deterministic `.torrent` fixtures.
4. Create an isolated Linux qBittorrent profile with a disposable WebUI username and password.
5. Launch qBittorrent in WSL, using qbtmud as the alternative WebUI.
6. Seed the sample torrents through the real qBittorrent Web API.
7. Log in through qbtmud with Playwright and capture the README screenshots into [`docs/readme-assets`](../../docs/readme-assets).
8. Stop the temporary qBittorrent process before exiting.

## Notes

- The runner does not use public torrents, public trackers, or mocked qBittorrent API responses.
- The seeded library is intentionally static: completed, stopped, and missing-file states are stable and reproducible.
- The default disposable credentials are `admin` / `adminadmin`. Override them with `QBITTORRENT_USERNAME` and `QBITTORRENT_PASSWORD` if needed.
- If Chromium is not installed in the local Playwright cache yet, the script installs it from the package-local Playwright CLI before capturing.
