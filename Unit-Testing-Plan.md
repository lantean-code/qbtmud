# bUnit Coverage Expansion Plan

## Objectives
- Establish a modern component testing stack for `Lantean.QBTMud` using bUnit + xUnit so critical UI flows can be validated without manual regression.
- Provide structured guidance for converting the existing placeholder tests into meaningful component coverage, prioritising high-value pages and shared UI primitives.
- Ensure the plan dovetails with the broader qBittorrent v5 alignment work (e.g., new search experience, torrent actions, dialogs).

## Current Test Landscape
- `Lantean.QBTMud.Test` is already configured as an xUnit project but contains only experimental/unit scaffolding (`UnitTest1.cs`). No component tests run today.
- No bUnit, MudBlazor test services, or HTTP abstractions are wired into the test project; dependency injection for components (e.g., `IApiClient`, `ILocalStorageService`, `IDialogService`) is unmocked.
- CI expectations for UI regression coverage are unclear; codifying a baseline will help future contributors.

## High‑Level Strategy
1. **Lay the foundation**: add bUnit/MudBlazor testing dependencies, create reusable test context helpers, and introduce typed doubles for frequently injected services.
2. **Cover critical views first**: prioritise pages and components with complex state or upcoming rewrites (Search page, Torrent list, dialogs).
3. **Expand outward**: incrementally add tests for navigation/layout wrappers, filter components, and shared dialogs as new features land.
4. **Integrate with CI**: ensure `dotnet test` executes component tests locally and on pipelines, with fixtures structured for parallel execution.
5. **Adopt guardrails**: document patterns and required assertions so new components ship with tests by default.

## Implementation Steps

### 1. Test Project Setup
- Update `Lantean.QBTMud.Test.csproj`:
  - Add packages: `bunit`, `bunit.xunit`, `Bunit.Moq`, `MudBlazor.Services`, `Microsoft.Extensions.DependencyInjection`, `Moq`, and `AwesomeAssertions`.
  - Enable nullable warnings consistency by mirroring app project settings.
- Create a `TestImports.cs` file with global usings for bUnit, MudBlazor, Moq/NSubstitute, and the app namespaces to reduce boilerplate.
- Replace existing placeholder tests with a `SmokeTests` folder reserved for minimal sanity checks.

### 2. Shared Test Infrastructure
- Introduce `ComponentTestContext : TestContext` (or extension methods) under `Lantean.QBTMud.Test/Infrastructure` to centralise DI setup:
  - Register MudBlazor services (`Services.AddMudServices()`), NavigationManager (FakeNav), configuration, and logging stubs.
  - Provide helper `AddApiClientMock`, `AddLocalStorageMock`, etc., returning strongly typed mocks or substitutes.
  - Ensure deterministic `ILocalStorageService` by using `Blazored.LocalStorage`’s in-memory implementation or a bespoke stub.
- Expose utility methods (`RenderComponentWithServices<TComponent>(Action<IServiceCollection>?)`) so tests can override specific dependencies.
- Add snapshot helpers for table row extraction, dialog inspection, and event dispatch (e.g., clicking buttons, submitting forms).

### 3. Search Page Coverage (High Priority)
- Create `Search` test suite (aligning with Search-Implementation plan):
  - **Form rendering**: assert initial state (default plugin/category selection, button text) and dynamic behaviour (Stop label once a job starts).
  - **Search lifecycle**: mock `IApiClient` to return synthetic plugin lists, search IDs, and results. Validate that `StartSearch` is called with expected payloads and that subsequent renders display fetched rows.
  - **Job management UI**: when multi-job support ships, verify tab/list rendering, status icons, and ability to stop/delete jobs on user interaction.
  - **Client-side filters**: stub job results and assert that seeds/size/search-scope filters adjust the rendered rows and visible totals.
  - **Context menu actions**: simulate row right-click and ensure download/copy handlers invoke the right helper methods (`DialogHelper`, clipboard service).

### 4. Torrent List & Filters
- Cover `Pages/TorrentList.razor` with focus on:
  - Toolbar state (search box debounce, filter chips, action menus).
  - Interaction with cascaded `MainData` and `SearchTermChanged` callbacks.
  - Row selection + bulk action context menus (mock API calls via injected services).
- Add tests for `Components/FiltersNav.razor` verifying bucket counts, selection, and tracker/category pipes once filter logic is upgraded.
- Validate `FilterHelper` behaviours via dedicated unit tests if not already covered (regex toggle, field selection, status buckets).

### 5. Dialog & Action Components
- For each Mud dialog (e.g., `AddTorrentFileDialog`, `ColumnOptionsDialog`, upcoming `SearchPluginsDialog`):
  - Render inside a `DialogService` test host, populate parameters, trigger submission, and assert returned `DialogResult` data.
  - Mock `IApiClient` interactions (upload torrent, enable plugin). Ensure failures surface error UI (snackbar/toasts) when applicable.
- Test `DialogHelper` extension methods by invoking them within the test context and verifying underlying service calls.

### 6. Layout & Navigation
- Test `Layout/LoggedInLayout.razor` and `Layout/ListLayout.razor` for:
  - Drawer toggling logic, search cascades, and navigation events (`NavigationManager.NavigateTo`).
  - Correct propagation of `CascadingValue`s to child components using a stub child that records received values.
- Ensure top-level routes (e.g., `/`, `/search`, `/settings`) render expected components via `Router` tests or minimal `App.razor` integration tests.

### 7. Regression Harness & Tooling
- Configure `dotnet test` to run with `--filter "FullyQualifiedName~Lantean.QBTMud"` to focus on component tests during local workflows; optionally add a separate github action job for UI tests.
- Implement deterministic snapshot helpers (HTML normalisation) only if comparisons are stable; otherwise rely on semantic assertions (CSS class presence, text, event invocation).
- Document new testing conventions in `CONTRIBUTING.md` or a dedicated `docs/testing.md` entry (how to add bUnit tests, service registration patterns, use of mocks).

## Component Prioritisation Checklist
1. **Critical flows**: Search page, Torrent list, Add torrent dialogs, Share ratio dialog.
2. **High churn components**: Filters, status navigation, upcoming tracker changes.
3. **Shared UI primitives**: `DynamicTable`, `FieldSwitch`, `SortLabel`—ensure core behaviours (sorting, column selection, local storage state) are verified.
4. **Error states**: offline mode (`MainData.LostConnection`), failed API calls, and empty lists (no torrents, no plugins).

## Testing Utilities to Build
- `ApiClientMockBuilder`: fluent helper returning mocks with queued responses for search/torrent operations.
- `LocalStorageInMemory`: simple implementation capturing set/get, supporting assertions on persisted keys (column selections, search filters).
- `EventDispatcher`: wraps `IRenderedComponent<T>` to simplify firing click/submit/change events on MudBlazor controls (abstracts CSS selectors).
- `DialogHostDriver`: orchestrates rendering a dialog and extracting returned data without duplicating boilerplate.

## Deliverables & Milestones
1. **Sprint 1**: project setup, base infrastructure, smoke test rendering of home/search pages.
2. **Sprint 2**: full coverage for Search page (form, lifecycle, filters) with mocked API flows.
3. **Sprint 3**: torrent list + filters + column options dialog tests; measure coverage delta.
4. **Sprint 4**: dialogs/actions (add torrent, share ratio, plugin management), plus regression fixtures for navigation layouts.
5. **Ongoing**: integrate with CI, enforce new component tests as part of definition of done.

## Open Questions / Assumptions
- Determine preferred mocking framework (current packages include AwesomeAssertions; decide whether to standardise on Moq or NSubstitute).
- Confirm availability of clipboard/browser APIs within test environment; may need to wrap them for deterministic testing.
- Decide on snapshot vs semantic assertions for DynamicTable output—HTML may be verbose; consider helper methods to parse table rows into POCOs before asserting.
- Validate whether UI tests must run under multiple cultures/themes; if so, extend test context to toggle `MudTheme` or culture info.

## Next Steps
- Review and align on tooling choices (Moq vs NSubstitute, FluentAssertions adoption).
- Implement Step 1–2 in a feature branch, replacing placeholder tests with the shared infrastructure and a first Search page smoke test.
- Iterate on the checklist as new UI work (Search parity, tracker filters) lands to keep tests in lockstep with features.

