# Search Feature Parity Plan

## Objectives
- Bring the `qbt-mud` search experience to functional parity with the qBittorrent v5 WebUI, including multi-job handling, advanced result filtering, and plugin management.
- Reuse existing infrastructure (MudBlazor tables, dialogs, API client) wherever possible while filling the missing pieces.
- Deliver the work in incremental, testable slices that keep existing search behaviour stable until parity is achieved.

## Current Implementation Snapshot
- `Lantean.QBTMud/Pages/Search.razor` renders a simple form that runs a single search against one plugin and displays results in a static `DynamicTable` without row actions.
- `Lantean.QBTMud/Pages/Search.razor.cs` only stores the last `_searchId`, polls `search/results` until the job stops, and discards previous jobs.
- `SearchResult` models lack `EngineName`/`PublishedOn` data and the UI never exposes description/download links.
- There is no UI for plugin enable/disable/install/update, nor support for saved search filters (min seeds/size, search-in scope).
- The API client already exposes all `search/*` endpoints, but the UI consumes only a subset.

## Gap Analysis vs qBittorrent v5
- **Search job lifecycle**: v5 keeps a tab per job, shows statuses from `search/status`, supports restarting/completing jobs, and allows switching between them. `qbt-mud` supports only one ephemeral job.
- **Form inputs & filters**: v5 enables searching across *enabled* plugins, selecting multiple specific plugins, and provides client-side filters (search-in scope, seeds range, size range). Current form offers only single-plugin + category.
- **Results table**: v5 streams batched results (limit/offset), displays engine/site/pub date, exposes context menu actions (download, open description, copy data), and shows visible/total result counts. Current table is read-only with limited columns.
- **Search plugin management**: v5 surfaces enabled state, version, URL, manual install (local/URL), uninstall, enable/disable toggles, and update-all. There is no corresponding UI in `qbt-mud`.
- **State persistence**: v5 stores granular filter preferences locally. `qbt-mud` has no saved state for search filters or column selection specific to search.
- **Accessibility/UX**: parity requires toolbar buttons (stop/refresh/close job), empty-state messaging (no plugins/searches), and error surfaces when API calls fail.

## Implementation Plan

### 1. Search Job Lifecycle & State Management
- Introduce a dedicated view-model (e.g., `SearchJobViewModel`) to track pattern, selected plugins, category, status, totals, timestamps, and accumulated results. Locate in `Lantean.QBTMud/Models`.
- Expand `Search.razor.cs` to maintain a collection of jobs keyed by id, using `GetSearchesStatus()` for periodic synchronization and `GetSearchResults(id, limit, offset)` to stream additional rows.
- Replace the single `_searchId`/`_searchResults` fields with job-centric state, and schedule polling via `PeriodicTimer` per active job. Ensure timers dispose cleanly on navigation/dispose.
- Update the razor markup to display job tabs or a side list (matching MudBlazor’s `MudTabs` or `MudList`) allowing context menu actions (refresh, close, close all). Mirror qBittorrent behaviour where “Stop” cancels a running job and “Search” starts a new one without clearing previous jobs.
- Reflect job status icons/text (Running, Stopped, Aborted, Error) and total result counts in the UI; surface API failures through existing toast/dialog mechanisms.
- Files impacted: `Lantean.QBTMud/Pages/Search.razor`, `Lantean.QBTMud/Pages/Search.razor.cs`, `Lantean.QBTMud/Models/SearchForm.cs`, new `Lantean.QBTMud/Models/SearchJobViewModel.cs`.

### 2. Search Form & Filters
- Extend `SearchForm` with multi-select plugin selection (`ICollection<string> SelectedPlugins`), a special “Enabled plugins” option, search-in scope, and optional min/max seeds & size filters (with units). Persist defaults via `ILocalStorageService`.
- Update the form markup to use `MudSelect` with `MultiSelection="true"` and chips for selected plugins, add numeric inputs for seeds/size with validation, and wire a text filter box for client-side result filtering.
- Implement client-side filtering in `Search.razor.cs` by applying the configured filters to each job’s accumulated results before binding them to `DynamicTable` (similar to qBittorrent’s `search.js` behaviour). Consider extracting a helper (`SearchFilterHelper`) for readability.
- Include UI affordances for empty states (no plugins installed, no searches yet) and a “Manage plugins…” button that opens the plugin dialog.
- Files impacted: `Lantean.QBTMud/Pages/Search.razor`, `Lantean.QBTMud/Pages/Search.razor.cs`, `Lantean.QBTMud/Models/SearchForm.cs`, new helper under `Lantean.QBTMud/Helpers/SearchFilterHelper.cs`.

### 3. Search Results Table & Row Actions
- Expand the column definitions to match v5 (`Name`, `Size`, `Seeders`, `Leechers`, `Engine`, `Site`, `Published`, optional `Actions`). Update `ColumnsDefinitions` in `Search.razor.cs` and ensure `DynamicTable` can render links/buttons inside rows.
- Add a row-action menu leveraging `DynamicTable`’s `OnTableDataContextMenu` to provide “Download”, “Open description”, “Copy → Name/Download link/Description URL” options. Implement the handlers in the code-behind, reusing `DialogHelper.InvokeAddTorrentLinkDialog` and clipboard utilities.
- Track and display the visible vs total result counts per job (using `SearchResults.Total` + post-filter counts) and surface in the UI header.
- Support incremental result loading by requesting in batches (e.g., 200-500 items) with offset; append to the job’s result list and trigger table refresh without re-fetching the full dataset.
- Files impacted: `Lantean.QBTMud/Pages/Search.razor`, `Lantean.QBTMud/Pages/Search.razor.cs`, possibly `Lantean.QBTMud/Components/UI/DynamicTable.razor.cs` (if new hooks required), clipboard utilities in `Lantean.QBTMud/Helpers`.

### 4. Search Plugin Management Experience
- Create a dialog (e.g., `SearchPluginsDialog.razor` + `.razor.cs`) presenting the plugin list with columns for enabled, name, version, URL, and last update. Include actions to enable/disable (batch), uninstall, install from URL/path, and update all.
- Wire the dialog into the search page “Manage plugins…” button and optionally from settings. Ensure optimistic UI updates after each command with error fallback.
- Provide basic validation for install sources (URL/local path) and progress feedback (loading spinner, success/fail toasts).
- Files impacted: new component under `Lantean.QBTMud/Components/Dialogs/SearchPluginsDialog.*`, updates to `Lantean.QBTMud/Helpers/DialogHelper.cs` (shortcut methods), and `Lantean.QBTMud/Pages/Search.razor` for invocation.

### 5. Client & Model Updates
- Update `Lantean.QBitTorrentClient/Models/SearchResult.cs` to include `EngineName`, `SiteUrl` (already), and `PublishedOn` (`pubDate`) properties with appropriate JSON bindings. Adjust constructors and equality semantics accordingly.
- Audit `Lantean.QBTMud` consumers for the new properties and update them to display `EngineName` instead of reusing `SiteUrl` for plugin name.
- Validate whether `SearchStatus` needs extra fields (e.g., `Plugin`) in v5 API responses; extend the model if necessary and adapt `ApiClientSearchTests` fixtures.
- Ensure `StartSearch` can accept “enabled” and multi-plugin input. Update `DoSearch` to send either `["enabled"]` or the selected plugin names without wrapping them in an array when empty. Handle cases where no plugin is selected gracefully.
- Files impacted: `Lantean.QBitTorrentClient/Models/SearchResult.cs`, `Lantean.QBitTorrentClient/Models/SearchStatus.cs` (if needed), `Lantean.QBitTorrentClient/ApiClient.cs`, `Lantean.QBitTorrentClient.Test/ApiClientSearchTests.cs`, downstream mapping code in `Lantean.QBTMud`.

### 6. Testing & Validation
- Unit tests: extend `ApiClientSearchTests` to cover the new `SearchResult` fields and multi-plugin payload logic. Add tests for the filter helper to ensure parity with v5 behaviour (min/max seeds/size, pattern matching, search-in scope).
- Component/integration tests: create bUnit tests for the search page covering (a) job creation and stop flow, (b) filtering behaviour, and (c) context menu actions invoking expected API calls or helper methods.
- Manual QA checklist: verify multi-job tabs, plugin install/uninstall flows, incremental result loading, download actions, and resilience to API failures (404, timeouts). Include mobile viewport sanity checks for responsive layout.

## Assumptions & Open Questions
- qBittorrent v5 continues to expose `engineName` and `pubDate` fields; confirm with a sample response before implementing.
- Determine whether search results should persist across sessions (v5 clears on reload); initial plan assumes in-memory only.
- Confirm availability of clipboard services within existing helper infrastructure or add a consistent abstraction.
- Check whether MudBlazor can render high-density tab headers akin to v5; if not, consider a vertical `MudNavMenu` for job selection.

## Suggested Sequencing
- Stage 1: Model/client updates + unit tests (ensures data shapes are correct).
- Stage 2: Search page refactor to multi-job architecture (retain basic table).
- Stage 3: Layer in advanced filters and result actions.
- Stage 4: Add plugin management dialog and wiring.
- Stage 5: Polish UX (counts, empty states, toasts) and execute full QA pass.

