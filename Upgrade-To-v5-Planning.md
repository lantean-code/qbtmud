# Upgrade to qBittorrent WebUI v5 – UI Alignment Plan

## Torrent List Filtering
- **Regex toggle & field selector**: Introduce the regex checkbox and the "Filter by" (Name/Save path) select found in v5. Update `FilterState`/`LoggedInLayout` to carry both values, wire them to `TorrentList`’s toolbar, and validate invalid patterns gracefully.
- **Filter helper parity**: Rework `FilterHelper.ContainsAllTerms/FilterTerms` to mirror `window.qBittorrent.Misc.containsAllTerms` (evaluate every term, respect `+`/`-` prefixes). Ensure filtering applies to the selected field, not just the torrent name.
- **New status buckets**: Add `Running` and `Moving` to `Status` enum, update `FilterHelper.FilterStatus`, `DisplayHelpers`, and `FiltersNav` so counts/icons match upstream.

## Tracker Filters
- **Special buckets**: Extend `FilterHelper`/`DataManager` to create sets for "Announce error", "Error", "Warning", and "Trackerless" in addition to "All". Store the required flags on the UI `Torrent` model (`HasTrackerError`, `HasTrackerWarning`, `HasOtherAnnounceError`, `TrackersCount`, etc.).
- **Tracker grouping & removal**: When grouping trackers by host in `FiltersNav`, retain original URL entries so removal can target the right string. Replace the placeholder "Remove tracker" action with a real implementation and disable it for synthetic buckets.

## ~~Torrent Data Model & Columns~~
- ~~**Model sync**: Bring `Lantean.QBTMud.Models.Torrent` into parity with v5 (`Popularity`, `DownloadPath`, `RootPath`, `InfoHashV1/2`, `IsPrivate`, share-limit action fields, tracker flags, etc.) and map them in `DataManager.CreateTorrent`.~~
- ~~**Column set alignment**: Match the v5 table defaults—add missing columns (Popularity, Reannounce in, Info hashes, Download path, Private, etc.), fix "Ratio Limit" to display `RatioLimit`, and ensure column ordering/enabled state mirrors `DynamicTable.TorrentsTable`.~~
- ~~**Helper updates**: Extend `DisplayHelpers` to format the new fields (popularity, private flag, info hashes, error state icons).~~

## Actions & Dialogs
- ~~**Copy submenu**: Add "Copy comment" and "Copy content path" to the copy submenu in `TorrentActions`, keeping clipboard behaviour identical to v5.~~
- ~~**Share ratio dialog**: Update `ShareRatioDialog`, `ShareRatio/ShareRatioMax`, and `DialogHelper.InvokeShareRatioDialog` to surface `ShareLimitAction`, fix the `MaxInactiveSeedingTime` mapping, and call `SetTorrentShareLimit` with the action.~~

## Add-Torrent Flow
- Mirror the v5 add-torrent pane: add controls for incomplete save path, tags, auto-start, queue position, share-limit action, etc., in `AddTorrentOptions.razor`, and wire the new fields into the submission object.

## ~~Preferences & Local Settings~~
- ~~Introduce new v5 toggles such as "Display full tracker URL" in `AdvancedOptions`, persist them via the preferences service, and respect the setting in the tracker column rendering.~~
