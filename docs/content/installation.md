# Installation

This page covers the normal way to run qbtmud: as qBittorrent's alternative WebUI.

This is the standard deployment model.

## Before you start

You need:

- qBittorrent installed with the WebUI enabled
- a qbtmud release archive from the [qbtmud Releases](https://github.com/lantean-code/qbtmud/releases) page

## Install qbtmud

1. Download the latest qbtmud release archive.
2. Extract the archive.
3. Find the extracted folder that contains `public`.
4. In qBittorrent, open `Tools` > `Options` > `Web UI`.
5. Enable **Use alternative WebUI**.
6. Set **Root Folder** to the extracted folder that contains `public`.
7. Save the settings.
8. Open your usual qBittorrent WebUI address, for example `http://localhost:8080`.

qBittorrent expects the selected alternative WebUI root folder to contain `public/` directly.

## What happens in the standard setup

In the standard installation:

- qBittorrent serves the qbtmud files directly
- qbtmud uses its default `Hash` routing mode
- qbtmud talks to the qBittorrent API on the same host
- you do not need to edit `appsettings.json`

The default configuration in the release archive is:

```json
{
  "Api": {
    "BaseUrl": ""
  },
  "Routing": {
    "Mode": "Hash"
  }
}
```

Leave that as-is unless you are following one of the advanced hosting guides.

## Updating to a newer release

1. Download the new release archive.
2. Stop qBittorrent or disable the alternative WebUI setting temporarily.
3. Remove the old extracted qbtmud files completely.
4. Extract the new release.
5. Make sure qBittorrent points at the folder that contains `public`.
6. Re-enable the alternative WebUI if needed.
7. Reload the browser.

## Advanced scenarios

- [Reverse proxy hosting](reverse-proxy.md) if qbtmud should be published through another public URL
- [Separate API and UI hosting](separate-api-ui-hosting.md) if the UI and API are on different origins
- [Advanced setup](advanced-setup.md) if you need the deployment settings reference

## Troubleshooting

- qBittorrent only validates that the alternative WebUI location is not blank. If the folder exists but does not contain a valid qbtmud layout, qBittorrent will still try to serve from it.
- qBittorrent serves files from the configured root and then from its `public/` subdirectory. If that structure is wrong, browser requests for qbtmud assets will return `404 Not Found` and the UI will not load correctly.
- If you copied a new release over an old one, remove the old files completely and extract the release again. Mixed asset versions can leave the UI in a broken state.
- If qBittorrent is still serving the old UI, confirm the configured root folder is the qbtmud folder you intended and refresh the browser without cached files.

For qBittorrent background on alternative WebUI hosting, see the [Alternate WebUI Usage Guide](https://github.com/qbittorrent/qBittorrent/wiki/Alternate-WebUI-usage).
