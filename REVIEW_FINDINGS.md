# Repository review findings

Static review dated 15 November 2026. This checklist records all 55 findings from the whole-repository review, grouped by area and then priority. Finding IDs retain the numbering of the original review so discussions can refer to a stable identifier.

## Progress and scope

- Total: **55 findings** — **2 P1**, **46 P2**, **7 P3**.
- Completed: **52 findings**. Deferred by user decision: **R11**, **R22** and **R50**.
- Mark a finding complete after addressing its cause and validating the affected scenario. Record any decision to defer or dismiss it alongside the item.
- P1: urgent defect to address next. P2: ordinary defect to fix. P3: lower-impact defect worth fixing.
- Deferred by user decision: **R11**, **R22** and **R50**, pending a decision about JavaScript regression testing.
- The review inspected current first-party code rather than a branch diff. The API client is an external NuGet package and was not audited as first-party source.
- No repository code, builds, tests, scripts or browser automation ran during the review. The existing passing tests were supplied context, and relevant test bodies were inspected selectively.
- Remote release state, live browser behaviour and an exhaustive audit of every test body remain unverified. There is currently no general Playwright page-testing runner; one is a possible follow-up, not an existing validation capability.

## Storage, synchronisation and history

### P1

- [x] **R01 — Preserve source data until storage migration commits.** [StorageRoutingService.cs:264](src/Lantean.QBTMud.Application/Services/StorageRoutingService.cs#L264)

    Migration deletes each successfully copied source entry before saving the new routing settings. A later failure leaves the old routing active while earlier settings are missing from its store. Defer source deletion until commit or implement recovery. Existing tests cover failure before any successful migration; add a failure after an earlier successful copy, including reverse migration.

### P2

- [x] **R03 — Retain failed settings writes when connectivity recovers.** [SettingsStorageService.cs:136](src/Lantean.QBTMud.Application/Services/SettingsStorageService.cs#L136)

    Failed ClientData writes fall back to local storage and return normally, but the next successful remote read ignores that fallback and restores the older value. Reconcile pending writes/removals or report an unsuccessful save. Existing fallback tests keep remote reads failing; cover successful reads after a failed write.

- [x] **R04 — Invalidate filtering when a save path changes.** [TorrentDataManager.cs:1299](src/Lantean.QBTMud.Application/Services/TorrentDataManager.cs#L1299)

    Save-path updates set `dataChanged` without `filterChanged`. An active save-path search therefore retains incorrect cached membership after a torrent moves. Mark path changes as affecting filters and cover a path-only delta with an active search.

- [x] **R05 — Rebuild category membership when subcategory mode changes.** [TorrentDataManager.cs:648](src/Lantean.QBTMud.Application/Services/TorrentDataManager.cs#L648)

    Updating `UseSubcategories` leaves `CategoriesState` built under the previous interpretation, producing incorrect parent counts and stale membership during later updates. Rebuild the relevant sets. Existing tests change the setting with empty collections; cover populated parent/child categories in both directions.

### P3

- [x] **R49 — Resume the persisted active speed bucket.** [SpeedHistoryService.cs:77](src/Lantean.QBTMud.Application/Services/SpeedHistoryService.cs#L77)

    Restoring history treats the active bucket as completed. Reloading within that window creates another bucket with the same timestamp, distorting averages and counting its duration twice during retention. Restore or merge the active bucket. Existing restore tests stop before adding another sample.

### Resolution and regression coverage

Completed on 15 November 2026. All five findings in this section have regression tests.

| Finding | Change and coverage |
| --- | --- |
| R01 | Source entries are retained until routing settings are committed. Tests cover later exact-key and prefix-copy failures, reverse migration, routing persistence failures in both directions, and remote cleanup failure after commit. |
| R03 | A local-only journal retains failed typed writes, raw writes and removals. Reads overlay and retry pending changes after reconnecting, including after reloading the service. Tests cover repeated failure, partial recovery, replacement writes, and migration of pending values and tombstones without losing unrelated pending changes. |
| R04 | A save-path delta invalidates filters. A regression test checks that an active path search loses membership after the torrent moves. |
| R05 | Changing subcategory mode rebuilds category membership and invalidates filters. Tests cover populated parent and child categories in both directions. |
| R49 | History resumes the persisted active bucket and preserves sample weights. Tests cover repeated reloads within one bucket, weighted averages, advancing to the next bucket, and legacy snapshots without a sample count. |

Validation: the full solution test suite passed with 3,342 tests and no failures or skips. Debug build and Release publishing succeeded. Changed C# files were formatted and all changed files were normalised to CRLF. Coverage was collected for the full suite; existing unreachable defensive branches in storage migration remain uncovered, without adding reflection or test-only hooks.

Build and test output used `/tmp/artifacts/qbtmud`. The installed formatter does not support `--artifacts-path`, so formatting used the equivalent `ArtifactsPath` and `UseArtifactsOutput` environment settings. Release publishing used the pinned repository-local .NET 10.0.100 SDK after installing its required WebAssembly workload locally; no SDK pin or project configuration changed.

## Core filtering and completion

### P1

- [x] **R02 — Separate synthetic filters from user-defined names.** [TorrentDataManager.cs:78](src/Lantean.QBTMud.Application/Services/TorrentDataManager.cs#L78)

    Tags named `All` or `Untagged`, and categories named `All` or `Uncategorized`, collide with synthetic dictionary entries and throw during full synchronisation, including startup. Incremental updates can also overwrite synthetic sets. Use distinct identities for synthetic filters and cover valid user-name collisions.

### P2

- [x] **R06 — Filter trackers using complete torrent membership.** [FilterHelper.cs:205](src/Lantean.QBTMud.Core/Helpers/FilterHelper.cs#L205)

    Sidebar counts use all associated trackers, while filtering compares only the torrent's current tracker. A torrent counted under a secondary tracker disappears when that tracker is selected, and bulk actions omit it. Use the complete tracker mapping. Existing tests cover single current trackers.

- [x] **R07 — Detect completion from verified progress.** [Extensions.cs:60](src/Lantean.QBTMud.Core/Extensions.cs#L60)

    `IsFinished` compares transferred bytes with total torrent size, including unselected files, so successful selective downloads miss completion notifications. Use authoritative completion progress/state. Existing tests encode byte equality and full-download examples; cover selective downloads and resumed content. See the [qBittorrent API field definitions](https://github.com/qbittorrent/qBittorrent/wiki/WebUI-API-%28qBittorrent-5.0%29).

- [x] **R08 — Bound regular-expression matching.** [FilterHelper.cs:160](src/Lantean.QBTMud.Core/Helpers/FilterHelper.cs#L160)

    Expensive valid expressions run synchronously during filtering without a timeout and can freeze the WebAssembly UI. Add bounded matching and handle timeout failures. Tests cover malformed patterns but not excessive backtracking, such as `(a+)+$` against a long near-match. See [Microsoft's regex timeout guidance](https://learn.microsoft.com/dotnet/standard/base-types/best-practices-regex#use-time-out-values).

### Resolution and regression coverage

Completed on 15 November 2026. All four findings in this section have regression tests.

| Finding | Change and coverage |
| --- | --- |
| R02 | Built-in category and tag filters use keys distinct from user-defined names. Tests cover full and incremental synchronisation, removal, filtering, UI selection and reload for `All`, `Untagged` and `Uncategorized`. Existing saved selections remain readable; new selections distinguish named filters from built-in filters. Test IDs encode filter keys so control characters do not enter rendered attributes. |
| R06 | Visible results and tracker bulk actions use the complete tracker-to-hash mapping. Matching hashes are collected once per query. Tests cover secondary URLs and hosts, duplicate membership, missing torrents, empty mappings, synthetic filters, the authenticated layout and sidebar start actions. The existing overloads remain available for callers without a tracker mapping. |
| R07 | Completion uses reported progress rather than transferred bytes. Tests cover selective downloads, resumed data, incomplete torrents with equal or excessive transferred bytes, metadata with zero sizes, completion transitions and exactly one completion notification. |
| R08 | Regex matching has a 50 ms timeout and handles timeout failures without escaping the filter. Tests exercise excessive backtracking in both inclusion and exclusion terms, alongside the existing valid and malformed expression cases. |

Validation: the full solution test suite passed with 3,379 tests and no failures or skips. Release publishing succeeded. Changed C# files were formatted, changed files use CRLF, and collected coverage covers the changed executable lines and branches. Build, test and publish output used `/tmp/artifacts/qbtmud`; formatting used the output environment settings described above. No browser automation was run.

## Themes and appearance

### P2

- [x] **R09 — Clear persisted selection before deleting the active theme.** [ThemeManagerService.cs:339](src/Lantean.QBTMud.Infrastructure/Services/ThemeManagerService.cs#L339)

    Deleting the active local theme reloads its saved ID and snapshot. With the default repository configuration, the deleted theme is reapplied as an apparent repository theme. Clear the saved selection before choosing a replacement. Existing deletion tests use an empty repository URL and miss the default path.

- [x] **R10 — Preserve theme selection after repository failures.** [ThemeManagerService.cs:684](src/Lantean.QBTMud.Infrastructure/Services/ThemeManagerService.cs#L684)

    A temporary repository fetch failure removes the selected theme from the rebuilt catalogue and deletes its persisted ID/snapshot. A later successful load cannot restore the selection. Preserve it until an authoritative successful load establishes removal. Cover a saved repository theme followed by failed preload/reload and recovery.

- [x] **R14 — Preserve theme edits across cascading updates.** [ThemeDetail.razor.cs:184](src/Lantean.QBTMud.Presentation/Pages/ThemeDetail.razor.cs#L184)

    Parameter updates unconditionally reload the saved definition and clear edits even when `ThemeId` is unchanged. Opening the drawer or changing dark mode can silently discard work. Initialise the editor when the selected theme changes, preserving edits for unrelated cascades. Add edit-then-cascade coverage.

- [x] **R18 — Assign the fallback theme before applying its font.** [Themes.razor.cs:363](src/Lantean.QBTMud.Presentation/Pages/Themes.razor.cs#L363)

    Importing JSON with `"theme": null` constructs a local fallback but passes the original null theme to the font helper, causing an uncaught exception. Assign the fallback or reject the content cleanly. The existing test explicitly expects the exception; replace that expectation with handled behaviour.

### P3

- [ ] **R50 — Accept whitespace in bootstrap font names.** [boot.resources.js:233](src/Lantean.QBTMud/wwwroot/js/boot.resources.js#L233)

    The regex literal uses `\\s` instead of `\s`, rejecting valid names such as Open Sans. Bootstrap skips their stylesheet and changes fonts after application startup. Correct the escape and cover persisted multi-word font names; layout tests currently mock this JavaScript.

    Deferred at the user's request on 15 November 2026, pending a decision on JavaScript regression testing. The implementation remains unchanged.

### Resolution and regression coverage

Addressed R09, R10, R14 and R18 on 15 November 2026. R50 is deferred at the user's request.

| Finding | Change and coverage |
| --- | --- |
| R09 | Deleting the active local theme clears its saved ID and snapshot before choosing a fallback. Tests use a configured repository, exercise bundled and default fallbacks, and reload the service to verify that the deleted theme stays deleted. |
| R10 | Failed or partial repository loads retain the selected ID and snapshot. Tests cover preload and explicit reload, failure followed by recovery, and a later successful load that confirms removal. |
| R14 | Unrelated parameter updates retain the editor state for the same theme. Tests change drawer and dark-mode cascading values through Blazor notifications, verify that name, description and font edits survive and can be saved, and check that selecting a different theme resets the editor. |
| R18 | Import normalisation assigns the fallback theme before applying its font. The null-theme regression now verifies a saved, non-null theme with the expected typography. |

Validation: the full solution test suite passed with 3,388 tests and no failures or skips. Release publishing succeeded. Changed C# files were formatted and changed files use CRLF. Coverage was collected for the full suite. Output used `/tmp/artifacts/qbtmud`, with the formatter environment settings described above. R50's JavaScript was not changed or exercised.

## Search, RSS and management pages

### P2

- [x] **R12 — Fetch outstanding results after a search stops.** [Search.razor.cs:597](src/Lantean.QBTMud.Presentation/Pages/Search.razor.cs#L597)

    Polling stops when no search is running, even if results remain unfetched. A completed search with 600 results retrieves only 250; refresh starts again with the same batch. Continue until the reported total is collected. Cover completed/hydrated searches and a search finishing with multiple outstanding batches.

- [x] **R13 — Identify RSS articles by feed and article ID.** [Rss.razor:114](src/Lantean.QBTMud.Presentation/Pages/Rss.razor#L114)

    Aggregated folder/Unread views key and select articles by ID alone. Two feeds containing the same syndicated GUID can produce duplicate rendering keys or select the wrong feed's article. Use a composite identity for rendering and selection. Tests currently use distinct IDs across feeds.

- [x] **R15 — Apply log text filters to displayed results.** [Log.razor:52](src/Lantean.QBTMud.Presentation/Pages/Log.razor#L52)

    Log and Blocks expose `Model.Criteria`, but neither implementation reads it. Submit only requests newer records, leaving displayed rows unfiltered. Apply criteria to retained history. Existing tests verify requests rather than the resulting filtered rows; cover message fragments and blocked IP criteria.

- [x] **R16 — Reload history when log severity changes.** [Log.razor.cs:89](src/Lantean.QBTMud.Presentation/Pages/Log.razor.cs#L89)

    Severity changes retain old rows and the previous request cursor. Switching Normal to Critical leaves Normal rows visible and skips older Critical messages. Reset the collection/cursor before fetching the new selection. Existing selection tests check persistence only.

- [x] **R17 — Invalidate category and tag snapshots after mutations.** [Categories.razor.cs:87](src/Lantean.QBTMud.Presentation/Pages/Categories.razor.cs#L87)

    Manual refresh creates snapshots that permanently override shared live data. Later additions, edits and deletions leave Categories and Tags displaying stale rows. Refresh or invalidate snapshots after successful mutations. Cover refresh followed by each mutation and shared-state updates.

- [x] **R19 — Keep torrent-task polling eligible after failures.** [TorrentCreator.razor.cs:191](src/Lantean.QBTMud.Presentation/Pages/TorrentCreator.razor.cs#L191)

    A failed polling request clears `_tasks`; subsequent ticks require a non-empty collection and never retry. Retain the previous state or track polling independently. Existing tests omit an unsuccessful API result followed by recovery while creation is running.

### Resolution and regression coverage

Addressed all six findings in this section on 15 November 2026.

| Finding | Change and coverage |
| --- | --- |
| R12 | Polling continues while a completed search has unfetched results. Tests collect 600 results across three batches for hydrated completed searches and searches that finish during polling, then verify that fetching stops. Failed jobs retain their existing manual-retry behaviour. |
| R13 | Article rendering, selection, read-state updates and refresh selection use feed plus article ID. Tests cover duplicate IDs in folder and Unread views on desktop and mobile, checking distinct list values and the correct feed's mark-read request. |
| R15 | Log and blocked-IP tables filter retained history using case-insensitive message/address fragments. Tests cover existing rows, empty history and clearing the criteria to restore retained results. |
| R16 | Severity changes clear previous rows, context selection and cursor before fetching the new selection. Tests verify fetching older Critical messages after Normal history, and discard late responses from both submitted requests and polling without corrupting the new cursor. |
| R17 | Successful mutations release manual-refresh snapshots; parent updates also return the tables to live shared data. Tests cover category add, edit and delete, tag add and delete, cancelled/failed mutations, and subsequent shared-data updates. |
| R19 | Failed task refreshes retain the previous task list so an active creation remains eligible for polling. A regression test covers failure, recovery to Finished, and stopping further requests once the task is terminal. |

Validation: all 3,415 solution tests passed with no failures or skips. Release publishing succeeded. Collected coverage includes all 65 changed executable source lines and their branches. Changed C# files were formatted and changed files use CRLF. Output used `/tmp/artifacts/qbtmud`, with the formatter environment settings described above. No browser automation was run.

## Options and settings

### P2

- [x] **R24 — Preserve pending options when changing tabs.** [Options.razor:26](src/Lantean.QBTMud.Presentation/Pages/Options.razor#L26)

    Default MudTabs behaviour disposes inactive content. Returning reconstructs displayed fields from saved preferences while hidden pending edits remain in the parent, so Save submits values different from those shown. Preserve panels or initialise from effective pending preferences. Cover ordinary edits across tab changes; behaviour was checked against tagged MudBlazor v9.13.0 source.

- [x] **R25 — Persist connection-limit switch changes.** [ConnectionOptions.razor.cs:220](src/Lantean.QBTMud.Presentation/Components/Options/ConnectionOptions.razor.cs#L220)

    Four connection/upload-limit switches change local flags without updating pending preferences or notifying the parent. Disabling a limit leaves it active; enabling without editing the numeric default also has no effect. Persist the value/unlimited sentinel and notify the page. Tests currently verify input disabling only.

- [x] **R26 — Restore schedule fields when edits return to saved values.** [SpeedOptions.razor.cs:163](src/Lantean.QBTMud.Presentation/Components/Options/SpeedOptions.razor.cs#L163)

    Starting at 01:00, editing to 02:30 then 01:15 retains pending hour `2` and saves 02:15. Both schedule handlers compare against original preferences without clearing earlier edits. Update or clear each pending component correctly. Cover partial and complete edit-then-revert sequences.

- [x] **R27 — Populate the correct inactive seeding-time field.** [BitTorrentOptions.razor.cs:186](src/Lantean.QBTMud.Presentation/Components/Options/BitTorrentOptions.razor.cs#L186)

    Initialisation assigns `Preferences.MaxInactiveSeedingTime` into `MaxSeedingTime`, overwriting the displayed total limit and leaving the inactive field at zero. Assign the intended property. Existing tests check enabled states without checking these numeric values.

- [x] **R28 — Clear legacy export paths when disabled.** [DownloadsOptions.razor.cs:267](src/Lantean.QBTMud.Presentation/Components/Options/DownloadsOptions.razor.cs#L267)

    Legacy `.torrent` export switches change local flags only. Disabling an existing export neither clears its pending path nor notifies the page, so exporting continues. Persist empty paths on disabling and selected paths on enabling. Cover switches without subsequent path edits on older supported servers.

- [x] **R29 — Reject duplicate monitored folders before mutation.** [DownloadsOptions.razor.cs:315](src/Lantean.QBTMud.Presentation/Components/Options/DownloadsOptions.razor.cs#L315)

    Adding an existing path throws from `Dictionary.Add`; renaming onto an existing path overwrites its save-location setting. Field validation does not prevent the callbacks. Guard before either mutation. Existing tests use unique destination paths; cover add and rename collisions without losing existing entries.

- [x] **R39 — Preserve independently inherited share limits.** [ShareRatioDialog.razor.cs:182](src/Lantean.QBTMud.Presentation/Components/Dialogs/ShareRatioDialog.razor.cs#L182)

    Mixed inherited/custom limits open in Custom mode, where saving converts untouched inherited `-2` limits to unlimited `-1`. Preserve each inherited sentinel unless explicitly changed. Tests cover uniform presets only. The [API contract](https://github.com/qbittorrent/qBittorrent/wiki/WebUI-API-%28qBittorrent-4.1%29) supports independent inherited/unlimited values.

### Resolution and regression coverage

Addressed all seven findings in this section on 15 November 2026.

| Finding | Change and coverage |
| --- | --- |
| R24 | Tabs retain their components after first use, with lazy loading for unopened panels. A page regression changes tabs and verifies that the displayed and submitted connection setting agree. |
| R25 | All four connection/upload limit switches persist a limit or the unlimited sentinel and notify the page. Tests disable and re-enable each without numeric edits. |
| R26 | Schedule handlers compare effective pending values and clear reverted components. Tests cover partial/full reversions, repeated values, unloaded preferences, and the options page's submitted values for both schedule times. |
| R27 | The inactive seeding-time preference populates its own field. Tests use distinct limits with the total limit enabled and disabled. |
| R28 | Legacy export switches persist an empty path on disabling and restore the retained path on enabling. Both destinations have regression tests without subsequent path edits. |
| R29 | Duplicate additions and rename collisions return before mutation. Tests verify preserved save locations, same-key edits and subsequent valid renames. |
| R39 | Each untouched inherited share limit retains its global sentinel. Tests cover all three inherited fields in mixed configurations, including explicit enabling followed by disabling. |

Validation: all 3,441 solution tests passed with no failures or skips. Release publishing succeeded. Coverage includes all 37 changed executable source lines and their branches. Changed C# files were formatted and changed files use CRLF. Output used `/tmp/artifacts/qbtmud`, with the formatter environment settings described above. No browser automation was run.

## Torrent content, categories and adding downloads

### P2

- [x] **R23 — Expand rename references without reprocessing captured values.** [FileNameMatcher.cs:144](src/Lantean.QBTMud.Presentation/Services/FileNameMatcher.cs#L144)

    Replacement handles `$1` before `$10`, corrupting multi-digit references, and later passes reinterpret dollar signs introduced by captured filenames. Parse replacement tokens once and append captures literally. Cover ten or more groups and capture values containing replacement syntax.

- [x] **R32 — Sort renamed folders by a comparable depth key.** [RenameFilesDialog.razor.cs:421](src/Lantean.QBTMud.Presentation/Components/Dialogs/RenameFilesDialog.razor.cs#L421)

    Replace All orders folders by `string[]` keys, which the default comparer cannot compare. Two renamed folders throw, potentially after file renames have succeeded. Use comparable depth keys and process children first. Existing coverage includes only one renamed folder.

- [x] **R33 — Exclude successful renames when retrying failures.** [RenameFilesDialog.razor.cs:410](src/Lantean.QBTMud.Presentation/Components/Dialogs/RenameFilesDialog.razor.cs#L410)

    After partial success, successful rows retain their old source paths and are submitted again on retry, failing against paths already renamed. Update their source state or exclude completed rows. Cover a successful row followed by failure, then retry.

- [x] **R34 — Include uncategorised torrents in category selection state.** [ManageCategoriesDialog.razor.cs:60](src/Lantean.QBTMud.Presentation/Components/Dialogs/ManageCategoriesDialog.razor.cs#L60)

    Empty categories are removed before All/Partial calculation. Movies plus an uncategorised torrent therefore appears entirely Movies; clicking Movies clears both instead of assigning Movies to both. Include every selected torrent. Existing partial-selection tests use two non-empty categories.

- [x] **R38 — Expose cookies for torrent URL downloads.** [AddTorrentLinkDialog.razor:22](src/Lantean.QBTMud.Presentation/Components/Dialogs/AddTorrentLinkDialog.razor#L22)

    The URL dialog hides the Cookie input needed for authenticated `.torrent` downloads, while file upload displays it. The workflow already maps cookies into API parameters. Enable the relevant field and cover authenticated URL additions. See the [API cookies parameter](https://github.com/qbittorrent/qBittorrent/wiki/WebUI-API-%28qBittorrent-v3.2.0-v4.0.4%29).

- [x] **R44 — Preserve parent paths during individual file renames.** [FilesTab.razor.cs:379](src/Lantean.QBTMud.Presentation/Components/FilesTab.razor.cs#L379)

    `contentItem.Path + value` omits the separator: renaming `root/file.txt` to `renamed.txt` sends `rootrenamed.txt`, moving it out of its parent. Join with the torrent's `/` separator. Existing tests explicitly assert the incorrect destination and need corrected expectations.

- [x] **R45 — Use the folder endpoint when renaming folders.** [FilesTab.razor.cs:379](src/Lantean.QBTMud.Presentation/Components/FilesTab.razor.cs#L379)

    Direct rename accepts folder rows but always calls `RenameFileAsync`, so folder rename fails. Select `RenameFolderAsync` for folders and construct the destination from the parent path. Direct-rename tests cover files only; the bulk dialog already distinguishes endpoints.

- [x] **R46 — Use forward slashes for subcategory names.** [FiltersNav.razor.cs:304](src/Lantean.QBTMud.Presentation/Components/FiltersNav.razor.cs#L304)

    Add Subcategory prefills `Movies\`, while hierarchy logic recognises `Movies/…`. The result behaves as an unrelated flat category. Use the category separator independently of filesystem conventions. Existing tests mirror the backslash; validate parent/child membership instead.

- [x] **R48 — Preserve file filters when their dialog is cancelled.** [FilesTab.razor.cs:129](src/Lantean.QBTMud.Presentation/Components/FilesTab.razor.cs#L129)

    Cancel returns null, which clears existing filter definitions/predicates and changes visible files and possibly bulk-priority targets. Leave applied filters unchanged and use the existing removal action explicitly. The cancellation test currently begins without an applied filter.

### P3

- [x] **R53 — Persist and restore remembered rename settings consistently.** [RenameFilesDialog.razor.cs:235](src/Lantean.QBTMud.Presentation/Components/Dialogs/RenameFilesDialog.razor.cs#L235)

    Search changes update previews without persisting `MultiRenamePreferences.Search`, despite loading it when reopening. The remembered-settings switch is also not restored. Persist the search and restore the switch. The persistence test omits the stored-search assertion.

### Resolution and regression coverage

Addressed all ten findings in this section on 15 November 2026.

| Finding | Change and coverage |
| --- | --- |
| R23 | Replacement tokens are expanded once and captures are appended literally. Tests cover multi-digit, named, empty, unmatched and sparse captures, escaped dollars, unknown/overflowing references, and enumeration widths. |
| R32 | Folder renames use descending depth. A strict sequence test verifies nested children are renamed before their parents. |
| R33 | Completed renames are excluded from retries and regenerated previews. Failed descendants defer ancestor folder renames. Tests cover partial failure, successful siblings, nested/unrelated folders, direct retry and preview regeneration. |
| R34 | Category selection includes empty and null categories. Tests verify Partial state and assigning Movies to both categorised and uncategorised selections. |
| R38 | URL additions expose the Cookie field. A dialog regression enters an authenticated URL and cookie and verifies the returned options; existing workflow tests verify the cookie reaches API parameters. |
| R44 | Direct renames join the parent path with `/`. Root and nested-file regressions verify the destination path. |
| R45 | Folder rows use the folder rename endpoint. Root and nested-folder regressions verify the destination and exclude file-endpoint calls. |
| R46 | Subcategory prefixes use `/`, including existing trailing separators. Menu tests verify the prefill and its compatibility with parent/child filtering. |
| R48 | Cancelling filter editing retains applied filters. Tests preserve visible rows, dialog definitions and filtered priority targets, then verify explicit removal restores all rows. |
| R53 | Remembered search changes persist, enabling Remember captures current settings, and the switch restores on reopening. Persistence and restore regressions cover these paths. |

Validation: all 3,471 solution tests passed with no failures or skips. Release publishing succeeded. Coverage includes all 73 changed executable source lines and their branches. Changed C# files were formatted and changed files use CRLF. Output used `/tmp/artifacts/qbtmud`, with the formatter environment settings described above. Sub-agents made scoped changes and read-only reviews; only the parent ran formatting, builds/tests and publishing. No browser automation was run.

## RSS rules and path browsing dialogs

### P2

- [x] **R35 — Reject stale RSS rule-selection responses.** [RssRulesDialog.razor.cs:358](src/Lantean.QBTMud.Presentation/Components/Dialogs/RssRulesDialog.razor.cs#L358)

    Selecting A then B can leave B's name paired with A's configuration when requests finish in reverse order. Save then overwrites B using A's settings. Bind before awaiting or guard continuation identity. Cover concurrent selections with reversed response completion.

- [x] **R36 — Display RSS tags with the editor's separator.** [RssRulesDialog.razor.cs:397](src/Lantean.QBTMud.Presentation/Components/Dialogs/RssRulesDialog.razor.cs#L397)

    Existing tags are joined with spaces while edits split on commas. Loading `alpha`, `beta` then appending `gamma` combines the original tags into `alpha beta`. Display comma-separated tags. Cover loading and editing an existing multi-tag rule.

- [x] **R37 — Ignore path-browser responses for obsolete paths.** [PathBrowserDialog.razor.cs:138](src/Lantean.QBTMud.Presentation/Components/Dialogs/PathBrowserDialog.razor.cs#L138)

    Earlier requests can overwrite newer entries; All mode can mix directories from the old path with files from the new path because `_currentPath` changes across awaits. Capture request identity and discard stale responses. Existing debounce tests do not cover requests already in flight.

### Resolution and regression coverage

Completed on 15 November 2026. All three findings in this section have regression tests.

| Finding | Change and coverage |
| --- | --- |
| R35 | Rule fields bind before matching articles are requested. Selection generations and captured rule identities prevent stale selections, saves, enable changes and removals from changing the current editor or a replacement rule. Tests cover reversed responses, reselection, current and obsolete failures, switching during save/removal, and removing and recreating a rule with the same name. |
| R36 | Existing tags use the comma separator expected by the editor. A regression loads two tags, appends a third and verifies that saving preserves three individual tags. |
| R37 | Directory requests capture their path and mode and discard obsolete results, failures and exceptions. Tests cover both stages of All mode, overlapping loading indicators and clearing the path while a request is pending. |

Validation: the full solution suite passed with 3,493 tests and no failures or skips. Release publishing succeeded. All 79 changed executable lines and their branches are covered. Changed C# files were formatted and all changed files were normalised to CRLF using the previously documented formatter settings.

## Shared UI, keyboard behaviour and startup workflows

### P2

- [ ] **R11 — Fetch the local resource in the CDN fallback.** [boot.resources.js:354](src/Lantean.QBTMud/wwwroot/js/boot.resources.js#L354)

    The asynchronous failure handler returns a URL, producing `Promise<string>` where Blazor requires a synchronous URL or `Promise<Response>`. Startup can fail despite available local assets. Return a local fetch response and cover CDN failure. See the [boot-resource contract](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-10.0#load-client-side-boot-resources).

- [x] **R20 — Preserve pending downloads when adding fails.** [PendingDownloadWorkflow.cs:100](src/Lantean.QBTMud.Presentation/Services/PendingDownloadWorkflow.cs#L100)

    Any normal dialog-workflow return marks the link processed and clears it, including structured API failures. The same link is then suppressed for the session. Return an explicit outcome and retain failed requests. Cover structured add failure through the combined workflow; existing tests cover thrown failures.

- [x] **R21 — Validate rate-limit text before parsing.** [DialogWorkflow.cs:1051](src/Lantean.QBTMud.Presentation/Services/DialogWorkflow.cs#L1051)

    Production rate-limit dialogs supply `long.Parse` to unrestricted text input. Clearing it or entering invalid/overflowing values throws through the event handler. Use non-throwing parsing with validation. Invalid-input tests omit the production custom converter.

- [ ] **R22 — Restore keyboard ownership after nested dialogs.** [interop.js:838](src/Lantean.QBTMud/wwwroot/js/interop.js#L838)

    Closing the wizard's theme preview clears global focus instead of restoring the wizard. Shortcuts then broadcast to background tables/torrent actions while the wizard remains open. Preserve an ownership-aware focus stack. Component tests mock individual calls and miss the shared JavaScript lifecycle.

- [x] **R30 — Route navigation shortcuts to the active table.** [DynamicTable.razor.cs:754](src/Lantean.QBTMud.Presentation/Components/UI/DynamicTable.razor.cs#L754)

    Simultaneous tables register the same global navigation keys without establishing ownership. In split view, Down in Files also advances the main torrent selection and replaces the details torrent. Restrict callbacks to the active table. Existing keyboard tests isolate one table.

- [x] **R47 — Allow Tab navigation out of pieces progress.** [PiecesProgressSvg.razor:9](src/Lantean.QBTMud.Presentation/Components/PiecesProgressSvg.razor#L9)

    The focusable toggle prevents every keydown's default action but handles only Enter/Space. Tab and Shift+Tab cannot move focus away. Use a native button or prevent defaults only for activation keys, and cover traversal. See [Blazor event default prevention](https://learn.microsoft.com/aspnet/core/blazor/components/event-handling?view=aspnetcore-10.0#prevent-default-actions).

### P3

- [x] **R51 — Cascade viewport orientation from the main layout.** [MainLayout.razor:4](src/Lantean.QBTMud.Presentation/Layout/MainLayout.razor#L4)

    The provider cascades only breakpoint by default. LoggedInLayout retains Portrait orientation and suppresses status-bar labels on landscape Md viewports. Cascade both values. Existing layout tests inject orientation directly, masking the missing connection.

- [x] **R54 — Invalidate pieces rendering when presentation inputs change.** [PiecesProgressSvg.razor.cs:126](src/Lantean.QBTMud.Presentation/Components/PiecesProgressSvg.razor.cs#L126)

    Colours and geometry rebuild only when piece state changes. Completed/stopped torrents retain old theme colours and breakpoint layouts; resizing to Xs can also omit the hidden-message text. Include presentation inputs in cache invalidation. Cover changes with stable piece data.

### Resolution and regression coverage

Completed on 15 November 2026 for R20, R21, R30, R47, R51 and R54. R11 and R22 remain deferred by user decision, alongside R50, pending a decision about JavaScript regression testing.

| Finding | Change and coverage |
| --- | --- |
| R20 | The URL dialog reports an explicit outcome. Pending downloads retain failed requests and consume added, accepted pending or cancelled requests. Integration tests combine the actual dialog and pending-download workflows, cover structured and per-torrent failures, retry successfully and verify session suppression only after handling. |
| R21 | Production rate dialogs use non-throwing parsing and show validation for invalid, empty, negative or overflowing values. Button and keyboard submission remain blocked until corrected. Tests exercise the actual converter and rendered dialog, including correction by text, infinity and slider. |
| R30 | Global navigation callbacks act only while focus is inside their table. Focus changes avoid unnecessary rendering. Two-table tests broadcast arrows, Shift+arrows and Enter through both registrations and verify initial inactivity, focus transfer and blur. Existing keyboard tests now establish table focus explicitly. |
| R47 | A native button replaces the toggle that intercepted every key. Tests cover activation, native button semantics and the absence of keyboard default prevention. Actual browser Tab traversal and browser-generated activation clicks were not exercised. |
| R51 | MainLayout cascades both breakpoint and orientation. A regression renders the real layout/provider/LoggedInLayout connection and verifies landscape and portrait viewport changes. |
| R54 | Render invalidation includes palette colours, dark mode, breakpoint, torrent identity and loading/failure state. Tests change those inputs while retaining piece data and component identity, including in-place palette changes, responsive geometry and the small-screen message. |

Validation: the full solution suite passed with 3,519 tests and no failures or skips. Release publishing succeeded. All 68 changed executable lines and their branches are covered. Changed C# files were formatted and all changed files were normalised to CRLF using the previously documented formatter settings.

Compatibility: the existing `Task InvokeAddTorrentLinkDialog(string? url = null)` entry point and `ValueGetFunc` parameter remain available. The pending-download flow uses the new outcome method, and rate dialogs use the new optional non-throwing converter.

## Translation tools

### P2

- [x] **R31 — Apply one translation validation retry budget.** [TranslationSyncService.cs:43](tools/QbtMudTranslations/TranslationSyncService.cs#L43)

    Nested loops each apply the retry setting: one retry yields four translation attempts, and three yield sixteen. Outer retries can retransmit previously successful keys. Share one budget and retain successful translations. Add positive-budget exhaustion coverage.

### P3

- [x] **R52 — Restore protected tokens as complete identifiers.** [TranslationTextProtector.cs:39](tools/QbtMudTranslations/TranslationTextProtector.cs#L39)

    Replacing `QBTMUDTOKEN1` also replaces the prefix of `QBTMUDTOKEN10`. Eleven protected fragments corrupt correctly preserved provider output and trigger validation failures. Use delimited/complete token matching. Tests contain fewer tokens; no current English asset was found reaching this threshold, so this is a supported-input tool defect.

### Resolution and regression coverage

Completed on 15 November 2026. Both findings in this section have regression tests.

| Finding | Change and coverage |
| --- | --- |
| R31 | One outer attempt loop owns the initial attempt and configured retries per locale. A per-run cache retains validated keys across validation, provider and write failures. Tests cover exhausted budgets of one and three retries, recovery with and without an intervening provider exception, write failure without retransmitting valid keys, and cancellation without retries. Existing tests retain coverage of locale ordering, existing files and derived locales. |
| R52 | A single regex evaluator restores complete numeric token identities using exact lookup. Tests exercise both provider wrappers with thirteen fragments, adjacent and overlapping identifiers, reversed map order, unknown tokens and literal inserted values containing token text, replacement metacharacters and entities. |

Validation: the full solution suite passed with 3,535 tests and no failures or skips. Release publishing of the translation tool succeeded. All 19 changed executable lines and their branches are covered. Changed C# files were formatted and all changed files were normalised to CRLF using the previously documented formatter settings.

## Build, delivery, screenshots and repository documentation

### P2

- [x] **R40 — Quote the converter project path.** [Lantean.QBTMud.csproj:154](src/Lantean.QBTMud/Lantean.QBTMud.csproj#L154)

    Translation generation expands the converter project path without quotation marks. Checkout paths containing spaces split the argument and prevent building/publishing when translation sources exist. Quote the full project argument. This scenario lacks coverage; no build was run during review.

- [x] **R41 — Retain historical bootstrap release provenance.** [release.py:91](docker/release.py#L91)

    The supported pre-manifest 3.0.0 fallback reads a mutable local-build pin now containing 3.1.0 and necessarily rejects the historical scenario. Publication still defaults to 3.0.0. Separate historical evidence from the advancing pin or update the contract consistently. Tests always provide a manifest. This finding uses checked-in evidence; live GitHub assets were not inspected.

- [x] **R42 — Preserve the wordmark when regenerating screenshots.** [run-readme-screenshots.sh:27](tools/ReadmeScreenshots/run-readme-screenshots.sh#L27)

    Cleanup deletes the entire output directory, including tracked `qbtmud-wordmark.svg`, referenced by the README. Capture regenerates five PNGs and never restores the SVG. Remove only generated screenshots or replace them individually. Cover preservation of non-generated assets.

- [x] **R43 — Use screenshot login selectors present in published builds.** [Program.cs:408](tools/ReadmeScreenshots/Program.cs#L408)

    Capture locates login controls through test IDs disabled in production; authentication-required deployments time out filling Username. The default orchestration masks this by bypassing localhost authentication, so its documentation also overstates login coverage. Use production selectors or supported capture attributes and validate the authenticated path.

### P3

- [x] **R55 — Update the repository project inventory.** [AGENTS.md:14](AGENTS.md#L14)

    The inventory lists nonexistent client/client-test projects and omits the current layered projects. The API client is the external `QBittorrent.ApiClient` NuGet package. Update the inventory to match the solution. This stale guidance already misdirected initial review scoping. The separate `ref/qBittorrent` reference-checkout guidance is not established as a defect merely because build resources use `external/qBittorrent`.

### Resolution and regression coverage

Completed on 15 November 2026. All five findings in this section have regression tests.

| Finding | Change and coverage |
| --- | --- |
| R40 | The converter project argument is quoted. A regression copies the actual translation properties and targets, real converter and SDK pin into an isolated temporary checkout whose path contains spaces, runs the real target, verifies generated translations and the locale manifest, checks original input hashes and removes the temporary copy. |
| R41 | Historical 3.0.0 provenance is retained in an immutable document recovered from commit 99092cd3, independently of the advancing local-build pin. Tests cover advancing pins, mismatched metadata and build evidence, checksum rejection, and later releases using their own manifests without bootstrap evidence. |
| R42 | Cleanup removes only the six known generated PNGs. An isolated Bash regression exercises the actual setup/cleanup prefix with the tracked wordmark, unrelated images, text and nested assets, including repeated cleanup and a missing output directory. |
| R43 | Screenshot login uses the production credential input IDs and submit-button semantics. Five tests cover the actual Login component with test IDs disabled, both login detection paths, an already authenticated page and unsuccessful login. Playwright locators drive real component callbacks through mocks. The disposable runner profile now requires localhost authentication, with a regression guarding that setting and documentation describing it. |
| R55 | The inventory lists the fifteen solution projects and identifies QBittorrent.ApiClient as an external NuGet package. A regression compares documented project paths with the solution and verifies that each path exists. |

Validation: the full solution suite passed with 3,542 tests and no failures or skips. All 32 release-policy tests and three screenshot-cleanup tests passed; the normal build workflow now runs both Python suites. Release publishing succeeded. All 33 changed executable C# lines and their branches are covered. Changed C# files were formatted; CRLF-governed files were normalised and checked, and Python/shell files retain LF.

The real translation target was exercised in a disposable copy, with original inputs remaining unchanged. Full Linux/qBittorrent screenshot capture and live historical GitHub asset availability were not checked. The login regression validates the real component and production selectors with test IDs disabled; browser interactions are mocked.
