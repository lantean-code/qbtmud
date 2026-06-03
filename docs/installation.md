# Installation

This guide covers the standard way to run qbtmud as an alternative WebUI for qBittorrent.

## Prerequisites

- a qBittorrent installation with WebUI enabled
- a qbtmud release archive downloaded from the [qbtmud Releases](https://github.com/lantean-code/qbtmud/releases) page

## Standard installation

1. Download the latest qbtmud release archive for your platform.
2. Extract the archive.
3. Locate the extracted directory that contains the `public` subdirectory.
4. Open qBittorrent and go to `Tools` > `Options` > `Web UI`.
5. Enable **Use alternative WebUI**.
6. Set **Root Folder** to the extracted directory that contains `public`.
7. Save the settings.
8. Open your qBittorrent WebUI address, such as `http://localhost:8080`.

qBittorrent expects the selected alternative WebUI root to contain a `public/` directory.

## Updating an existing installation

1. Download the new release archive.
2. Extract it to a new location or replace the existing extracted files.
3. Ensure the configured alternative WebUI root still points to the directory that contains `public`.
4. Reload the qBittorrent WebUI in the browser.

If you stage each release in a separate directory, you can roll back by pointing qBittorrent back at the previous version.

## Troubleshooting

- If qBittorrent shows a blank or broken UI, confirm the configured root folder contains `public` directly.
- If the old WebUI still appears, clear browser cache and confirm qBittorrent is pointing at the correct extracted directory.
- If qbtmud loads but some navigation fails on refresh, review the deployment notes in [Advanced setup](advanced-setup.md).

For qBittorrent-specific background on alternative WebUI hosting, refer to the [Alternate WebUI Usage Guide](https://github.com/qbittorrent/qBittorrent/wiki/Alternate-WebUI-usage).
