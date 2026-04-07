# Centralized ApiResult Failure Snackbar Pattern

## Summary
Replace throw-based command failure handling and ad hoc snackbar checks for `ApiResult` command calls with one reusable workflow that operates on an already-produced `ApiResult`.

The new workflow should become the default for command-style `Task<ApiResult>` calls in qbtmud after the API call returns:
- convert call sites that currently ignore `ApiResult`
- convert call sites that currently show a snackbar via local `if (!result.IsSuccess)` helpers
- convert throw-based handling where the API now returns `ApiResult` instead of throwing
- do not convert flows where `ApiResult.Failure` drives extra behavior beyond "show error and stop", such as optimistic rollback or branch-specific recovery

## Key Changes
- Add a new scoped service, `IApiFeedbackWorkflow`, implemented by `ApiFeedbackWorkflow`.
- Add a dedicated lost-connection dialog workflow service, separate from `LoggedInLayout`, so any connectivity-aware flow can trigger the lost-connection UI without depending on the layout owning the dialog state.
- Keep `ISnackbarWorkflow` focused on presentation only; do not put API-result orchestration into it.
- `ApiFeedbackWorkflow` owns the shared failure policy for command-style `ApiResult` values:
  - success returns `false` from `TryHandleFailure(...)` because no failure was handled
  - failure shows an error snackbar and returns `true`
  - default snackbar copy is localized `HttpServer / "qBittorrent returned an error. Please try again."`
  - `Failure.UserMessage` can be appended, substituted, or otherwise incorporated when appropriate for the call site
  - connectivity failures are excluded from the generic snackbar default and should continue through the existing lost-connection behavior instead of using this generic handler
  - support an optional localized failure-message builder so call sites can supply operation-specific copy without reimplementing the result check
- The lost-connection workflow service should own lost-connection dialog presentation:
  - expose a small trigger API that can be called when connectivity failure is detected
  - make dialog display idempotent so rapid repeated triggers do not stack multiple dialogs
  - replace the current layout-local `_lostConnectionDialogShown` ownership with service-owned coordination
  - remain compatible with the existing `IConnectivityStateService` as the source of connectivity state rather than replacing it

- Expose a small, reusable API that operates on an existing result:
  - `bool TryHandleFailure(ApiResult result, Func<string, string>? buildFailureMessage = null)`
  - an equivalent `HandleFailure(...)` or `ShowFailure(...)` shape is acceptable as long as it accepts `ApiResult` directly and returns only whether failure was handled
  - do not add `RunAsync(...)` or any helper that executes the API operation on behalf of the caller

- Apply the workflow to three categories of call sites:
  - unchecked command calls where `await ApiClient.*Async(...)` currently ignores the returned `ApiResult`
  - local snackbar wrappers that only exist to check `IsSuccess` and show an error
  - throw-based handlers around `Task<ApiResult>` calls where the success path currently assumes "no exception means success"
  - in those flows, the caller should inspect the result and delegate failure handling to the workflow instead of wrapping the operation itself

- Do not convert call sites where `ApiResult.Failure` drives additional logic:
  - optimistic UI rollback
  - alternate navigation or connectivity/authentication handling
  - partial-success or failure-count logic
  - query/result-loading flows using `TryGetValue`
  - inline/per-item validation surfaces if they intentionally need custom per-item error state

## Implementation Notes
- Replace local one-off helpers that are now redundant with the centralized workflow:
  - remove/replace helpers like RSS command failure handlers and similar component-local `IsSuccess` snackbar code where they do not perform extra logic
  - retain specialized helpers where they do more than show an error and stop
- Convert throw-based `ApiResult` usages to the workflow:
  - remove unreachable `catch`-only error handling for result-returning API calls
  - inspect the returned `ApiResult`, delegate failure handling to the workflow, and run success-side effects only when no failure was handled
- Use the workflow in representative silent command areas:
  - toolbar/context-menu mutations in filters, peers, trackers, tags, categories, files, and similar UI command surfaces
  - dialog submit/remove actions that currently proceed after unchecked commands
  - dialog/workflow service methods whose mutating API calls are currently unchecked unless they need extra branch logic
- Move lost-connection dialog orchestration out of the layout and into the dedicated service:
  - the layout should consume the service rather than own dialog deduplication itself
  - connectivity-aware flows outside the layout should be able to request the lost-connection dialog through the same service
  - repeated connectivity failures in quick succession should reuse the same in-flight/latched dialog state instead of layering multiple modal instances
- Preserve current behavior for advanced flows:
  - `TorrentActions` optimistic rollback logic stays explicit
  - `ApplicationActions.ResetWebUI`-style auth/connectivity branching stays explicit
  - any flow that needs failure inspection for behavior, not just feedback, remains hand-written

## Tests
- Add unit tests for `ApiFeedbackWorkflow`:
  - success returns `false` from `TryHandleFailure(...)` and shows no snackbar
  - failure returns `true` and shows the expected snackbar
  - generic failures use the localized API-error default instead of the reachability message
  - `Failure.UserMessage` is incorporated correctly when a call site supplies a custom builder
  - connectivity failures are not treated as the generic snackbar case
  - custom failure-message builder is applied

- Add unit/component tests for the lost-connection workflow service:
  - first lost-connection trigger shows the dialog
  - repeated triggers while the dialog is already active do not open additional dialogs
  - reconnect/reset behavior allows the dialog to be shown again after connectivity is restored
  - layout-level tests assert that dialog ownership no longer depends on layout-local deduplication state

- Update representative component/service tests to assert the new behavior:
  - previously silent commands now show an error snackbar on `ApiResult.Failure`
  - local state updates or refreshes do not happen after a failed command
  - converted throw-based handlers now branch on `ApiResult` instead of unreachable `catch` blocks
  - already-specialized flows with rollback or auth/connectivity branching remain unchanged
  - remove tests that only cover deleted local `IsSuccess`/snackbar helper methods and replace them with workflow-backed assertions

## Assumptions and Defaults
- The new workflow is the standard path for command-style `Task<ApiResult>` calls whose only failure behavior should be "inspect result, show snackbar, and stop".
- Existing explicit `ApiResult` handling remains in place only when failure affects control flow or state beyond snackbar presentation.
- The workflow does not execute API operations and does not absorb exceptions.
- Default snackbar severity is `Error`.
- The generic snackbar default is the API-error message, not the lost-connection message.
- Lost-connection behavior remains a separate explicit path from the generic `ApiResult` feedback workflow, but dialog presentation should move into a dedicated shared service rather than staying in `LoggedInLayout`.
- Operation-specific localized copy should be supplied at the call site only when the generic failure detail is not enough for a good UX.
