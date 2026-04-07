# LoggedInLayout Refactor Plan

## Summary
Refactor `LoggedInLayout` into a thin shell component that owns render-facing state and Blazor lifecycle wiring, while moving orchestration into focused scoped services. Preserve current behavior exactly. Use this refactor to create clean seams for the later `ApiResult` work, but do not implement `ApiResult` handling in this phase.

After the refactor, `LoggedInLayout` should still own:
- Cascading shell state exposed to children: `MainData`, `Preferences`, `AppSettingsState`, filters, sort/search state, page title.
- Pure component UI state: `_showPwaInstallPrompt`, `_pwaInstallPromptDelayTask`, `_torrentsDirty`, `_torrentsVersion`, timer drawer state, and local render invalidation.
- Blazor lifecycle and dispatcher concerns: `OnInitialized*`, `OnAfterRenderAsync`, `StateHasChanged`, `InvokeAsync`, event subscription/unsubscription, disposal.
- The current qBittorrent `requestId` cursor, alongside the `MainData` snapshot it belongs to.
- Applying workflow outputs onto component state.

It should no longer directly orchestrate startup loading, startup recovery, steady-state refresh, pending download persistence/processing, welcome wizard flow, update checks, or status-bar command behavior.

## Key Changes
### 1. Extract shell session orchestration
Add `IShellSessionWorkflow`.

Public contract:
- `Task<ShellSessionLoadResult> LoadAsync(CancellationToken cancellationToken = default)`
- `Task<ShellSessionLoadResult> RecoverAsync(int requestId, CancellationToken cancellationToken = default)`
- `Task<ShellSessionRefreshResult> RefreshAsync(int requestId, Models.MainData? currentMainData, CancellationToken cancellationToken = default)`

Add typed outcomes/results:
- `ShellSessionLoadResult`
- `ShellSessionRefreshResult`
- `ShellSessionLoadOutcome`
- `ShellSessionRefreshOutcome`

Required result semantics:
- `ShellSessionLoadResult` distinguishes `Ready`, `AuthenticationRequired`, `LostConnection`, and `RetryableFailure`.
- On `Ready`, it returns `AppSettings`, `Preferences`, `Version`, `MainData`, and the next `RequestId`.
- `ShellSessionRefreshResult` distinguishes `Updated`, `AuthenticationRequired`, `LostConnection`, `RetryableFailure`, and `NoChange`.
- On refresh success, it returns the next `RequestId`, the current `MainData` snapshot to keep, and booleans for `ShouldRender` and `TorrentsDirty`.

This workflow owns:
- startup auth check
- initial settings/preferences/version/main-data load
- startup recovery load path
- main refresh tick API call and merge/recreate logic
- refresh interval update
- speed history initialization/push
- torrent completion notification processing
- locale preference synchronization
- failure classification

This workflow must not:
- navigate
- call `StateHasChanged`
- own render flags beyond returned result values
- expose internal transition batches back to the component
- own the `requestId` cursor

Implementation note:
- Replace the duplicated startup and startup-recovery pipeline with one shared internal load path used by both initialization and recovery ticks.
- Make the outcome model explicit so later `ApiResult` work can change internals without reopening `LoggedInLayout`.

### 2. Extract pending download handling
Add `IPendingDownloadWorkflow`.

Public contract:
- `Task RestoreAsync(CancellationToken cancellationToken = default)`
- `Task CaptureFromUriAsync(string? uri, CancellationToken cancellationToken = default)`
- `Task ProcessAsync(CancellationToken cancellationToken = default)`
- `Task ClearAsync(CancellationToken cancellationToken = default)`

This workflow owns:
- `_pendingDownloadStorageKey` and `_lastProcessedDownloadStorageKey`
- restoring pending/processed values from session storage
- extracting supported download links from URLs
- duplicate detection via last-processed token
- persisting and clearing pending downloads
- invoking `IDialogWorkflow.InvokeAddTorrentLinkDialog(...)`
- post-processing navigation home

`LoggedInLayout` should only:
- call `RestoreAsync` during startup
- call `CaptureFromUriAsync` for the initial URI and location changes
- call `ProcessAsync` once authentication is confirmed
- clear pending download state on auth failure

### 3. Extract startup experience orchestration
Add `IStartupExperienceWorkflow`.

Public contract:
- `Task<bool> RunWelcomeWizardAsync(string? initialLocale, bool useFullScreenDialog, CancellationToken cancellationToken = default)`
- `Task RunUpdateCheckAsync(bool updateChecksEnabled, string? dismissedReleaseTag, CancellationToken cancellationToken = default)`

This workflow owns:
- building the welcome wizard plan
- marking the wizard as shown
- choosing and applying welcome wizard dialog options
- showing the welcome wizard dialog
- returning whether the PWA prompt may continue
- update-status lookup and dismissed-release handling
- update snackbar emission
- swallowing non-cancellation failures exactly as today

Keep `_showPwaInstallPrompt`, `_pwaInstallPromptDelayTask`, and the 2-second delay in `LoggedInLayout`, because that is render-timing behavior, not domain orchestration.

### 4. Extract status-bar command orchestration
Add `IStatusBarWorkflow`.

Public contract:
- `Task<bool?> ToggleAlternativeSpeedLimitsAsync(CancellationToken cancellationToken = default)`
- `Task<int?> ShowGlobalDownloadRateLimitAsync(int currentRateLimit, CancellationToken cancellationToken = default)`
- `Task<int?> ShowGlobalUploadRateLimitAsync(int currentRateLimit, CancellationToken cancellationToken = default)`

Return semantics:
- `ToggleAlternativeSpeedLimitsAsync` returns `true` or `false` when the new server state is known, and `null` when no state change should be applied because the workflow already handled the failure UX.

This workflow owns:
- alternative speed limit toggle API calls and follow-up state lookup
- localized success/error snackbar messages for alternative speed limits
- invoking global download/upload rate dialogs
- converting dialog/API failures into the current snackbar behavior

`LoggedInLayout` should only:
- keep the local re-entry guard for the toggle button
- apply returned values onto `MainData.ServerState`
- request re-render

## LoggedInLayout End State
`LoggedInLayout.razor.cs` should depend directly on:
- `ILostConnectionWorkflow`
- `IShellSessionWorkflow`
- `IPendingDownloadWorkflow`
- `IStartupExperienceWorkflow`
- `IStatusBarWorkflow`
- `NavigationManager`
- `IManagedTimerFactory`
- `IPreferencesUpdateService`
- `ILanguageLocalizer`

It should no longer inject:
- `IApiClient`
- `ITorrentDataManager`
- `IDialogWorkflow`
- `IDialogService`
- `ISettingsStorageService`
- `ISessionStorageService`
- `ISpeedHistoryService`
- `IAppSettingsService`
- `IMagnetLinkService`
- `IAppUpdateService`
- `IWelcomeWizardPlanBuilder`
- `IWelcomeWizardStateService`
- `ITorrentCompletionNotificationService`

## Implementation Order
1. Add `IShellSessionWorkflow` and move startup, recovery, refresh, locale-sync, and notification orchestration first.
2. Add `IPendingDownloadWorkflow` and remove pending-download fields and storage logic from the layout.
3. Add `IStartupExperienceWorkflow` and move welcome wizard and update-check sequencing.
4. Add `IStatusBarWorkflow` and remove direct API/dialog/snackbar logic from status-bar actions.
5. Reduce `LoggedInLayout` to lifecycle wiring, state application, and rendering concerns only.
6. Update `Program.cs` registrations.

## Test Plan
Add new unit test classes with 100% line and branch coverage:
- `ShellSessionWorkflowTests`
- `PendingDownloadWorkflowTests`
- `StartupExperienceWorkflowTests`
- `StatusBarWorkflowTests`

Refocus `LoggedInLayoutTests` so it covers only:
- cascaded values and render output
- filter/sort/search state updates
- timer loop start/stop wiring
- location-change dispatch
- PWA prompt display delay
- preferences update subscription/disposal
- applying workflow results onto component state
- login navigation and lost-connection workflow invocation from workflow outcomes

Required scenario coverage:
- startup success, auth failure, connectivity failure, retryable startup failure, startup recovery success/failure
- refresh full update, merge update, auth failure, connectivity failure, retryable refresh failure, no-change refresh
- locale sync cases currently covered in the layout tests
- pending magnet/torrent URL restoration, duplicate suppression, invalid values, dialog failure persistence, and post-process navigation
- welcome wizard shown/not shown/canceled and fullscreen option selection
- update available/dismissed/disabled/error/cancellation
- alternative speed toggle success/failure/re-entry and global rate dialog success/cancel/failure

## Assumptions
- This is a structural refactor only. User-facing behavior, copy, timing, dialog options, storage keys, and navigation behavior remain unchanged.
- The later `ApiResult` phase will plug into these workflow seams by changing workflow internals, not by re-expanding `LoggedInLayout`.
- No shared shell state container is introduced in this refactor. The component remains the owner of render state.
