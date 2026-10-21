# qbtmud 3.1.0

This release adds qBittorrent 5.3.0 compatibility and exposes its new settings and workflows, while continuing to support qBittorrent 5.1.x and 5.2.x.

## Compatibility

* Supports qBittorrent 5.1.x, 5.2.x and 5.3.x.
* Shows controls according to the connected server's Web API capabilities. Features that require 5.3 are hidden on older servers.
* Retains the existing torrent export, SMTP SSL and speed-limit workflows for older servers, while using the corresponding new APIs and settings where supported.
* Preserves category inheritance and share-limit settings when editing existing torrents and categories.

## New Features

* Added global, category and torrent share-limit modes, including **Match any limit** and **Match all the limits**.
* Added category options for inheriting or overriding the incomplete-download location and share limits.
* Added **Pause Session** and **Resume Session** actions, alongside a preference to start the BitTorrent session paused.
* Added downloads of completed individual files from the torrent content view.
* Added RSS rule cloning, JSON export and JSON import.
* Added an **Ignore dotfiles** option to torrent creation, enabled by default, and corrected the display of creation timestamps.
* Added free-space information when choosing supported download and category paths.
* Added preferences for saving search tabs and results, and used server-provided search metadata where available.
* Added torrent backup options, including backup location, moving backups after completion and removing backups with torrents.
* Added explicit SMTP encryption choices: **None**, **STARTTLS** and **SMTPS**.
* Added WebUI session limits, I2P Peer Exchange and tunnel length variance options, and advanced connection and session settings.
* Added peer contribution, queued tracker announce and request latency information.
* Added confirmation preferences for removing all tags and removing trackers from all torrents.

## Improvements and Fixes

* Updated speed-limit changes to use the batch API on supported servers, preserving limits that were not edited.
* Improved the RSS downloader dialog on mobile, with stacked content and wrapping action buttons at narrow widths.
* Refreshed the English WebUI translations for qBittorrent 5.3.

## Other Changes Since 3.0.0

* Added WebUI API-key management, including generation, reveal, copy, rotation and deletion, with confirmation dialogs for key changes.
* Added public Docker images with automated GHCR publishing, installation and update guidance, and release-content verification.
* Improved documentation styling and search.

## Framework and Dependencies

* Updated `QBittorrent.ApiClient` to `1.3.0` for qBittorrent 5.3.x support.
* Updated the required Microsoft.Extensions packages to `10.0.12`.
* Updated the qBittorrent source reference to the 5.3.0 release.

## Testing

* Added regression coverage for capability detection, version-specific options, preference and torrent data merging, category inheritance, share limits, RSS workflows, torrent creation, file downloads and session actions.
* Added coverage for batch speed-limit updates, preserving untouched limits and retrying failed saves.
* All **3,300 automated tests** pass, and the Release publish succeeds.
* Completed **283 live browser checks** against qBittorrent **5.3.0 / Web API 2.16.2**, covering API-key confirmation dialogs, settings persistence, categories, RSS clone/export/import, torrent creation, completed-file downloads, share limits and session pause/resume.
* Visually checked desktop and mobile layouts, including 320px and 390px widths.
* Previously validated against qBittorrent **5.2.4**, with 296 compatibility checks and 33 API-key checks passing. Support for 5.1 is covered by automated compatibility tests; a live 5.1 instance was unavailable for this release's checks.

## Upgrade Notes

Existing qBittorrent 5.1 and 5.2 installations can continue using qbtmud without upgrading the backend. New controls become available when the connected server supports them.

For deployments that host the UI and API separately, qBittorrent 5.3 adds request-origin checks when CSRF protection is enabled. CORS headers alone may not permit cookie-authenticated requests across different ports, subdomains or schemes. Review the [separate UI/API hosting guide](https://lantean-code.github.io/qbtmud/separate-api-ui-hosting/) and prefer a same-origin reverse proxy. qbtmud does not change the server's security settings automatically.

**Full changelog:** [3.0.0...release/3.1.0](https://github.com/lantean-code/qbtmud/compare/3.0.0...release/3.1.0)
