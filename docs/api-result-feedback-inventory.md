# ApiResult Feedback Inventory

## Scope
This inventory covers the remaining `ApiResult` feedback work after the lost-connection phase.

It is intentionally limited to command-style `Task<ApiResult>` flows where the desired failure behavior is:
- inspect the already-returned result
- show feedback
- stop without continuing success-side effects

It does not include the lost-connection dialog refactor from phase 1.

## Remembered Policy Questions
- Whether `AuthenticationRequired` should be treated like any other generic non-connectivity failure in the shared workflow, or kept explicit like connectivity failures.
- Whether phase 3 should be a broad sweep across every obvious eligible call site, or a narrower representative pass first.

## Planned Shared Infrastructure Changes
These files are expected to be added or updated as the shared ApiResult workflow lands.

| File | Planned change |
| --- | --- |
| `src/Lantean.QBTMud/Services/IApiFeedbackWorkflow.cs` | New shared workflow interface for handling an already-produced `ApiResult`. |
| `src/Lantean.QBTMud/Services/ApiFeedbackWorkflow.cs` | New workflow implementation that applies the shared snackbar policy and skips generic handling for connectivity failures. |
| `src/Lantean.QBTMud/Program.cs` | Register `IApiFeedbackWorkflow` as a scoped service. |
| `test/Lantean.QBTMud.Test/Services/ApiFeedbackWorkflowTests.cs` | New unit tests covering success, failure, default message selection, custom builders, and connectivity exclusions. |

## Planned Production Call-Site Changes

### Replace local helper wrappers with the shared workflow

| File | Planned change |
| --- | --- |
| `src/Lantean.QBTMud/Services/DialogWorkflow.cs` | Replace `TryHandleApiFailure(ApiResult result)` with the shared workflow. |
| `src/Lantean.QBTMud/Pages/Rss.razor.cs` | Replace `TryHandleRssCommandFailure(ApiResult result, Func<string, string> buildMessage)` with the shared workflow plus operation-specific message builders. |
| `src/Lantean.QBTMud/Components/TorrentActions.razor.cs` | Replace `ShowApiFailure(ApiResult result)` in the simple command paths; keep rollback paths explicit. |

### Convert local `!IsSuccess` snackbar branches

| File | Methods / areas |
| --- | --- |
| `src/Lantean.QBTMud/Layout/LoggedInLayout.razor.cs` | `ToggleAlternativeSpeedLimits()` for the toggle command result only. |
| `src/Lantean.QBTMud/Pages/Options.razor.cs` | `Save()` for `SetApplicationPreferencesAsync(...)`. |
| `src/Lantean.QBTMud/Pages/Cookies.razor.cs` | `PersistCookiesAsync(...)` for `SetApplicationCookiesAsync(...)`. |
| `src/Lantean.QBTMud/Pages/TorrentCreator.razor.cs` | `OpenCreateDialog()` and `DeleteTask(...)`. |
| `src/Lantean.QBTMud/Components/Dialogs/SearchPluginsDialog.razor.cs` | `RunOperation(...)` for plugin enable/disable/install/uninstall/update commands. |

### Convert currently silent unchecked `Task<ApiResult>` command calls

| File | Methods / areas |
| --- | --- |
| `src/Lantean.QBTMud/Components/ApplicationActions.razor.cs` | `Exit()` callback around `ShutdownAsync()`. |
| `src/Lantean.QBTMud/Pages/Tags.razor.cs` | `DeleteTag(...)`, `AddTag()`. |
| `src/Lantean.QBTMud/Pages/Categories.razor.cs` | `DeleteCategory(...)`. |
| `src/Lantean.QBTMud/Pages/Rss.razor.cs` | `UpdateContextRename()`, `UpdateContextEditUrl()`, `UpdateContextDelete()`, `UpdateContextAddFolder()`, `AddSubscriptionAtNode(...)`, `MarkNodeAsRead(...)`, `RefreshFeedsForNode(...)`. |
| `src/Lantean.QBTMud/Components/FiltersNav.razor.cs` | `RemoveCategory()`, `RemoveTracker()`, `AddTag()`, `RemoveTag()`, `StartTorrents(...)`, `StopTorrents(...)`. |
| `src/Lantean.QBTMud/Components/FilesTab.razor.cs` | `PriorityValueChanged(...)`, single-file rename callback in `RenameFiles(...)`, bulk priority changes. |
| `src/Lantean.QBTMud/Components/TrackersTab.razor.cs` | `AddTracker()`, tracker edit callback, `RemoveTracker(...)`. |
| `src/Lantean.QBTMud/Components/PeersTab.razor.cs` | `AddPeer()`, `BanPeer(...)`. |
| `src/Lantean.QBTMud/Components/TorrentActions.razor.cs` | `Stop()`, `Start()`, `ForceStart()`, `SetCategory(...)`, `ForceReannounce()`, `MoveToTop()`, `MoveUp()`, `MoveDown()`, `MoveToBottom()`, `ToggleTag(...)`, `ToggleCategory(...)`. |
| `src/Lantean.QBTMud/Components/Dialogs/ManageTagsDialog.razor.cs` | `SetTag(...)`, `AddTag()`, `RemoveAllTags()`. |
| `src/Lantean.QBTMud/Components/Dialogs/ManageCategoriesDialog.razor.cs` | `SetCategory(...)`, `AddCategory()`, `RemoveCategory()`. |
| `src/Lantean.QBTMud/Components/Dialogs/RssRulesDialog.razor.cs` | `RemoveRule()`, `Submit()`. |
| `src/Lantean.QBTMud/Services/DialogWorkflow.cs` | `InvokeAddCategoryDialog(...)`, `InvokeDeleteTorrentDialog(...)`, `ForceRecheckAsync(...)`, `InvokeDownloadRateDialog(...)`, `InvokeGlobalDownloadRateDialog(...)`, `InvokeEditCategoryDialog(...)`, `InvokeSetLocationDialog(...)`, `InvokeUploadRateDialog(...)`, `InvokeGlobalUploadRateDialog(...)`. |

### Convert throw-based or catch-based handling that no longer matches `ApiResult`

| File | Planned change |
| --- | --- |
| `src/Lantean.QBTMud/Components/Dialogs/SearchPluginsDialog.razor.cs` | Remove the `InvalidOperationException`-only error path from `RunOperation(...)`; branch on `ApiResult` instead. |

## Planned Explicit Exclusions
These locations are expected to stay hand-written because failure changes control flow or local state beyond “show error and stop”.

| File | Reason to leave explicit |
| --- | --- |
| `src/Lantean.QBTMud/Components/ApplicationActions.razor.cs` | `ResetWebUI()`, `Logout()`, `StartAllTorrents()`, and `StopAllTorrents()` have auth/connectivity-specific branching. |
| `src/Lantean.QBTMud/Layout/LoggedInLayout.razor.cs` | Startup, refresh, and recovery flows already branch on authentication and connectivity state. |
| `src/Lantean.QBTMud/Pages/Search.razor.cs` | Search start/stop/polling logic has auth/connectivity handling, background cleanup, and result-loading behavior. |
| `src/Lantean.QBTMud/Components/TorrentActions.razor.cs` | `ToggleAutoTMM()`, `ToggleSuperSeeding()`, `DownloadSequential()`, and `DownloadFirstLast()` perform optimistic local-state rollback. |
| `src/Lantean.QBTMud/Pages/Rss.razor.cs` | `HandleArticleSelection(...)` marks articles read optimistically before the API call and would need rollback logic if the command fails. |
| `src/Lantean.QBTMud/Components/Dialogs/RenameFilesDialog.razor.cs` | Uses per-item success and error state instead of a single snackbar outcome. |
| `src/Lantean.QBTMud/Components/Dialogs/SearchPluginsDialog.razor.cs` | `LoadPlugins()` remains explicit because it is a query/result-loading flow, not a command-style `Task<ApiResult>`. |
| `src/Lantean.QBTMud/Components/FiltersNav.razor.cs` | `RemoveUnusedCategories()` and `RemoveUnusedTags()` return payloads via `ApiResult<T>` and are not direct `Task<ApiResult>` candidates. |
| `src/Lantean.QBTMud/Services/DialogWorkflow.cs` | `AddTorrentAsync(...)` flows return payloads via `ApiResult<T>` and are outside this exact shared-workflow shape. |
| `src/Lantean.QBTMud/Pages/Options.razor.cs` | The reload step after save remains explicit because it is a query/result-loading path. |
| `src/Lantean.QBTMud/Pages/Cookies.razor.cs` | The reload step after save remains explicit because it is a query/result-loading path. |
| `src/Lantean.QBTMud/Pages/TorrentCreator.razor.cs` | Task polling and task-list refresh remain explicit because they are query/result-loading paths. |

## Companion Test Files Likely To Change
These are the existing tests most likely to need updates once phase 3 starts.

| Test file | Expected reason |
| --- | --- |
| `test/Lantean.QBTMud.Test/Layout/LoggedInLayoutTests.cs` | Toggle alternative speed limits failure behavior. |
| `test/Lantean.QBTMud.Test/Services/DialogWorkflowTests.cs` | Shared dialog workflow command failures and helper removal. |
| `test/Lantean.QBTMud.Test/Pages/OptionsTests.cs` | Save failure feedback and stop-on-failure behavior. |
| `test/Lantean.QBTMud.Test/Pages/CookiesTests.cs` | Cookie save failure feedback and no reload on failure. |
| `test/Lantean.QBTMud.Test/Pages/TorrentCreatorTests.cs` | Create/delete task failures and stop-on-failure behavior. |
| `test/Lantean.QBTMud.Test/Pages/RssTests.cs` | RSS command failures and helper removal. |
| `test/Lantean.QBTMud.Test/Pages/TagsTests.cs` | Tag command failures and no local update after failure. |
| `test/Lantean.QBTMud.Test/Pages/CategoriesTests.cs` | Category deletion failure handling. |
| `test/Lantean.QBTMud.Test/Components/ApplicationActionsTests.cs` | `Exit()` command failure if converted. |
| `test/Lantean.QBTMud.Test/Components/FiltersNavTests.cs` | Previously silent mutations now surfacing snackbar errors. |
| `test/Lantean.QBTMud.Test/Components/FilesTabTests.cs` | File-priority and rename command failures stop local follow-up behavior. |
| `test/Lantean.QBTMud.Test/Components/TrackersTabTests.cs` | Tracker mutation failures now show feedback. |
| `test/Lantean.QBTMud.Test/Components/PeersTabTests.cs` | Peer add/ban failures now show feedback. |
| `test/Lantean.QBTMud.Test/Components/TorrentActionsTests.cs` | Shared helper replacement and newly checked command mutations. |
| `test/Lantean.QBTMud.Test/Components/Dialogs/ManageTagsDialogTests.cs` | Tag-management failures stop local refresh/state changes. |
| `test/Lantean.QBTMud.Test/Components/Dialogs/ManageCategoriesDialogTests.cs` | Category-management failures stop local refresh/state changes. |
| `test/Lantean.QBTMud.Test/Components/Dialogs/RssRulesDialogTests.cs` | RSS rule mutation failures stop success-side effects. |
| `test/Lantean.QBTMud.Test/Components/Dialogs/SearchPluginsDialogTests.cs` | Workflow-backed command failure behavior and unreachable catch removal. |

## Notes
- This is the current planned edit inventory, not an implementation commitment for every `ApiResult<T>` path in the repo.
- If the shared workflow is later expanded to generic `ApiResult<T>` command returns, the excluded `ApiResult<T>` command sites should be revisited in a separate pass.
