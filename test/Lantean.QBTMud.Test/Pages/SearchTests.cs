using System.Net;
using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;
using UiCategory = Lantean.QBTMud.Models.Category;
using UiMainData = Lantean.QBTMud.Models.MainData;
using UiServerState = Lantean.QBTMud.Models.ServerState;
using UiTorrent = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class SearchTests : RazorComponentTestBase<Search>
    {
        private const string _preferencesStorageKey = "Search.Preferences";
        private const string _jobsStorageKey = "Search.Jobs";
        private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

        [Fact]
        public void GIVEN_NoStoredPreferences_WHEN_Render_THEN_DefaultsApplied()
        {
            var apiMock = TestContext.UseApiClientMock();
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            _popoverProvider = TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Instance.GetState(x => x.Value).Should().BeNull();

            var categorySelect = FindComponentByTestId<MudSelect<string>>(target, "CategorySelect");
            categorySelect.Instance.GetState(x => x.Value).Should().Be(SearchForm.AllCategoryId);

            var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");
            pluginSelect.Instance.GetState(x => x.SelectedValues).Should().Contain("movies");

            var toggleAdvancedButton = FindComponentByTestId<MudButton>(target, "ToggleAdvancedFilters");
            GetChildContentText(toggleAdvancedButton.Instance.ChildContent).Should().Be("Show filters");

            var startButton = FindComponentByTestId<MudButton>(target, "StartSearchButton");
            startButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_PersistedPreferences_WHEN_Render_THEN_AdvancedFiltersExpanded()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedCategory = "movies",
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase),
                FilterText = "1080p",
                SearchIn = SearchInScope.Names,
                MinimumSeeds = 5
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            _popoverProvider = TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForState(() =>
            {
                return FindComponentByTestId<MudCollapse>(target, "AdvancedFiltersCollapse").Instance.Expanded;
            });

            var advancedFiltersCollapse = FindComponentByTestId<MudCollapse>(target, "AdvancedFiltersCollapse");
            advancedFiltersCollapse.Instance.Expanded.Should().BeTrue();

            var searchInSelect = FindComponentByTestId<MudSelect<SearchInScope>>(target, "SearchInScopeSelect");
            searchInSelect.Instance.GetState(x => x.Value).Should().Be(SearchInScope.Names);

            var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");
            pluginSelect.Instance.GetState(x => x.SelectedValues).Should().Contain("movies");
        }

        [Fact]
        public async Task GIVEN_PersistedJobs_WHEN_Render_THEN_JobSummaryDisplayed()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 11,
                    Pattern = "Ubuntu",
                    Category = "movies",
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(11, SearchJobStatus.Stopped, 2)
            });
            apiMock.Setup(client => client.GetSearchResultsAsync(11, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc", "Ubuntu 24.04", 1_500_000_000, "http://files/ubuntu", 10, 200, "http://site", "movies", 1_700_000_000)
            }, SearchJobStatus.Stopped, 2));

            _popoverProvider = TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForState(() =>
            {
                return string.Equals(GetChildContentText(FindComponentByTestId<MudText>(target, "JobSummary").Instance.ChildContent), "1/2", StringComparison.Ordinal);
            });

            var jobTabSummary = FindComponentByTestId<MudText>(target, "JobSummary");
            GetChildContentText(jobTabSummary.Instance.ChildContent).Should().Be("1/2");

            var resultsTable = target.FindComponent<DynamicTable<SearchResult>>();
            var items = resultsTable.Instance.Items.Should().NotBeNull().And.Subject;
            items.Should().ContainSingle(result => result.FileName == "Ubuntu 24.04");
        }

        [Fact]
        public async Task GIVEN_CorruptStoredState_WHEN_Render_THEN_DefaultPreferencesApplied()
        {
            await TestContext.LocalStorage.SetItemAsStringAsync(_preferencesStorageKey, "{", Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsStringAsync(_jobsStorageKey, "{", Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            _popoverProvider = TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            TestContext.Render<MudSnackbarProvider>();
            TestContext.Render<MudSnackbarProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var searchFormCollapse = FindComponentByTestId<MudCollapse>(target, "SearchFormCollapse");
                searchFormCollapse.Instance.Expanded.Should().BeTrue();

                var toggleAdvancedButton = FindComponentByTestId<MudButton>(target, "ToggleAdvancedFilters");
                GetChildContentText(toggleAdvancedButton.Instance.ChildContent).Should().Be("Show filters");

                var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");
                pluginSelect.Instance.GetState(x => x.SelectedValues).Should().Contain("movies");
            });

            var emptyStateTitle = FindComponentByTestId<MudText>(target, "SearchEmptyStateTitle");
            GetChildContentText(emptyStateTitle.Instance.ChildContent).Should().Be("Start a search above.");
        }

        [Fact]
        public async Task GIVEN_DisabledPersistedPlugin_WHEN_Render_THEN_SelectsEnabledPlugins()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedCategory = "legacy",
                SelectedPlugins = new HashSet<string>(new[] { "legacy" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);

            var enabledPlugin = new SearchPlugin(true, "Primary", "primary", new[] { new SearchCategory("primary", "Primary") }, "http://plugins/primary", "2.0");
            var disabledPlugin = new SearchPlugin(false, "Legacy", "legacy", new[] { new SearchCategory("legacy", "Legacy") }, "http://plugins/legacy", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { disabledPlugin, enabledPlugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");
                pluginSelect.Instance.GetState(x => x.SelectedValues).Should().BeEquivalentTo(new[] { "primary" });

                var categorySelect = FindComponentByTestId<MudSelect<string>>(target, "CategorySelect");
                categorySelect.Instance.GetState(x => x.Value).Should().Be(SearchForm.AllCategoryId);
            });
        }

        [Fact]
        public void GIVEN_PluginLoadReturnsForbidden_WHEN_Render_THEN_NavigatesToLogin()
        {
            var navigationManager = UseTestNavigationManager();
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Search disabled", HttpStatusCode.Forbidden);
            apiMock.Setup(client => client.GetSearchesStatusAsync())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Search disabled", HttpStatusCode.Forbidden);

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                navigationManager.Uri.Should().Be("http://localhost/login");
                navigationManager.LastForceLoad.Should().BeTrue();
            });

            target.FindComponents<MudAlert>().Where(component => HasTestId(component, "SearchUnavailableAlert")).Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_AllPluginsDisabled_WHEN_Render_THEN_WarningShownAndSearchDisabled()
        {
            var apiMock = TestContext.UseApiClientMock();
            var disabledPlugin = new SearchPlugin(false, "Legacy", "legacy", new[] { new SearchCategory("legacy", "Legacy") }, "http://plugins/legacy", "1.0");
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { disabledPlugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("ubuntu");

            target.WaitForAssertion(() =>
            {
                var warning = FindComponentByTestId<MudAlert>(target, "NoPluginAlert");
                GetChildContentText(warning.Instance.ChildContent).Should().Be("Enable at least one search plugin to run searches.");

                var startButton = FindComponentByTestId<MudButton>(target, "StartSearchButton");
                startButton.Instance.Disabled.Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_ManagePluginsEnablesSelection_WHEN_DialogConfirmed_THEN_PreferencesPersisted()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "legacy" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            var disabledPlugin = new SearchPlugin(false, "Legacy", "legacy", new[] { new SearchCategory("legacy", "Legacy") }, "http://plugins/legacy", "1.0");
            var enabledPlugin = new SearchPlugin(true, "Primary", "primary", new[] { new SearchCategory("primary", "Primary") }, "http://plugins/primary", "2.0");
            apiMock.SetupSequence(client => client.GetSearchPluginsAsync())
                .ReturnsAsync(new List<SearchPlugin> { disabledPlugin })
                .ReturnsAsync(new List<SearchPlugin> { enabledPlugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>();
            dialogMock.Setup(flow => flow.ShowSearchPluginsDialog()).ReturnsAsync(true);

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var iconButton = FindComponentByTestId<MudIconButton>(target, "ManagePluginsButton");
            var manageButton = iconButton.FindAll("button").First();
            manageButton.Click();

            target.WaitForAssertion(() =>
            {
                var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");
                pluginSelect.Instance.GetState(x => x.SelectedValues).Should().Contain("primary");
            });

            var storedPreferences = await TestContext.LocalStorage.GetItemAsync<SearchPreferences>(_preferencesStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedPreferences.Should().NotBeNull();
            storedPreferences!.SelectedPlugins.Should().Contain("primary");
        }

        [Fact]
        public void GIVEN_ManagePluginsReloadFails_WHEN_DialogConfirmed_THEN_ShowsSnackbarAndAlert()
        {
            var apiMock = TestContext.UseApiClientMock();
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            apiMock.SetupSequence(client => client.GetSearchPluginsAsync())
                .ReturnsAsync(new List<SearchPlugin> { plugin })
                .ReturnsFailure<IReadOnlyList<SearchPlugin>>(ApiFailureKind.ServerError, "Network error", HttpStatusCode.ServiceUnavailable);
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>();
            dialogMock.Setup(flow => flow.ShowSearchPluginsDialog()).ReturnsAsync(true);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                It.Is<string>(message => message.Contains("Unable to load search plugins")),
                Severity.Warning,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<Search>();
            var iconButton = FindComponentByTestId<MudIconButton>(target, "ManagePluginsButton");
            var manageButton = iconButton.FindAll("button").First();
            manageButton.Click();

            target.WaitForAssertion(() =>
            {
                snackbarMock.Verify();
                var alert = FindComponentByTestId<MudAlert>(target, "SearchUnavailableAlert");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Unable to load search plugins: Network error");
            });
        }

        [Fact]
        public void GIVEN_ManagePluginsCancelled_WHEN_Click_THEN_PluginListNotReloaded()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>();
            dialogMock.Setup(flow => flow.ShowSearchPluginsDialog()).ReturnsAsync(false);

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var iconButton = FindComponentByTestId<MudIconButton>(target, "ManagePluginsButton");
            var manageButton = iconButton.FindAll("button").First();
            manageButton.Click();

            apiMock.Verify(client => client.GetSearchPluginsAsync(), Times.Once());
        }

        [Fact]
        public void GIVEN_PluginLoadReturnsForbidden_WHEN_Render_THEN_DoesNotShowPluginLoadErrorSnackbar()
        {
            var navigationManager = UseTestNavigationManager();
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Search disabled", HttpStatusCode.Forbidden);
            apiMock.Setup(client => client.GetSearchesStatusAsync())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Search disabled", HttpStatusCode.Forbidden);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                navigationManager.Uri.Should().Be("http://localhost/login");
                navigationManager.LastForceLoad.Should().BeTrue();
            });

            snackbarMock.Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => message.Contains("Unable to load search plugins: Search disabled")),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_NoEnabledPlugins_WHEN_SubmitForm_THEN_ShowsEnablePluginSnackbar()
        {
            var apiMock = TestContext.UseApiClientMock();
            var disabledPlugin = new SearchPlugin(false, "Legacy", "legacy", new[] { new SearchCategory("legacy", "Legacy") }, "http://plugins/legacy", "1.0");
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { disabledPlugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                It.Is<string>(message => message.Contains("Enable the selected plugin before searching.")),
                Severity.Warning,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            var form = target.Find("form");
            form.Submit();

            target.WaitForAssertion(() => snackbarMock.Verify());
        }

        [Fact]
        public void GIVEN_SearchUnavailable_WHEN_SubmitForm_THEN_ShowsUnavailableSnackbar()
        {
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "Plugin load failure", HttpStatusCode.InternalServerError);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                It.Is<string>(message => message.Contains("Unable to load search plugins: Plugin load failure", StringComparison.Ordinal)),
                Severity.Warning,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            target.Find("form").Submit();

            target.WaitForAssertion(() => snackbarMock.Verify());
        }

        [Fact]
        public async Task GIVEN_MissingSelectedPluginsAndNoEnabledPlugins_WHEN_Rendered_THEN_PluginSelectionClearsAndCategoryFallsBack()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedCategory = "missing-category",
                SelectedPlugins = new HashSet<string>(new[] { "missing-plugin" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);

            var disabledPlugin = new SearchPlugin(false, "Legacy", "legacy", new[] { new SearchCategory("legacy", "Legacy") }, "http://plugins/legacy", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { disabledPlugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");
                pluginSelect.Instance.GetState(x => x.SelectedValues).Should().BeEmpty();

                var categorySelect = FindComponentByTestId<MudSelect<string>>(target, "CategorySelect");
                categorySelect.Instance.GetState(x => x.Value).Should().Be(SearchForm.AllCategoryId);
            });
        }

        [Fact]
        public void GIVEN_EmptyCriteria_WHEN_SubmitForm_THEN_ShowsEnterCriteriaSnackbar()
        {
            var apiMock = TestContext.UseApiClientMock();
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                It.Is<string>(message => message.Contains("Enter search criteria to start a job.")),
                Severity.Warning,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var form = target.Find("form");
            form.Submit();

            target.WaitForAssertion(() => snackbarMock.Verify());
        }

        [Fact]
        public async Task GIVEN_ValidCriteriaAndPlugins_WHEN_StartSearch_THEN_JobCompletesAndPersistsMetadata()
        {
            var jobId = 42;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var statusQueue = new Queue<IReadOnlyList<SearchStatus>>();
            statusQueue.Enqueue(Array.Empty<SearchStatus>());
            statusQueue.Enqueue(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 1) });
            statusQueue.Enqueue(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Stopped, 1) });

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(() =>
            {
                var next = statusQueue.Count > 1 ? statusQueue.Dequeue() : statusQueue.Peek();
                return next;
            });
            apiMock.Setup(client => client.StartSearchAsync("Ubuntu", It.IsAny<IReadOnlyCollection<string>>(), SearchForm.AllCategoryId)).ReturnsSuccessAsync(jobId);

            var resultQueue = new Queue<SearchResults>();
            resultQueue.Enqueue(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc", "Ubuntu 24.04", 1_500_000_000, "http://files/ubuntu", 10, 200, "http://site", "movies", 1_700_000_000)
            }, SearchJobStatus.Running, 1));
            resultQueue.Enqueue(new SearchResults(new List<SearchResult>(), SearchJobStatus.Stopped, 1));
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsSuccessAsync(() =>
            {
                if (resultQueue.Count > 1)
                {
                    return resultQueue.Dequeue();
                }

                return resultQueue.Peek();
            });

            apiMock.Setup(client => client.DeleteSearchAsync(jobId)).ReturnsSuccess(Task.CompletedTask);
            apiMock.Setup(client => client.StopSearchAsync(jobId)).ReturnsSuccess(Task.CompletedTask);

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            var startButton = FindComponentByTestId<MudButton>(target, "StartSearchButton");
            target.WaitForAssertion(() =>
            {
                startButton.Instance.Disabled.Should().BeFalse();
            });

            target.Find("form").Submit();

            await handler(CancellationToken.None);

            var resultsTable = target.FindComponent<DynamicTable<SearchResult>>();
            var results = resultsTable.Instance.Items.Should().NotBeNull().And.Subject;
            results.Should().ContainSingle(result => result.FileName == "Ubuntu 24.04");

            apiMock.Verify(client => client.StartSearchAsync("Ubuntu", It.Is<IReadOnlyCollection<string>>(plugins => plugins.Contains("movies")), SearchForm.AllCategoryId), Times.Once());
            apiMock.Verify(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()), Times.AtLeast(2));

            var storedMetadata = await TestContext.LocalStorage.GetItemAsync<List<SearchJobMetadata>>(_jobsStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedMetadata.Should().NotBeNull();
            storedMetadata!.Should().Contain(metadata => metadata.Id == jobId && metadata.Pattern == "Ubuntu");
        }

        [Fact]
        public async Task GIVEN_MatchingJobExists_WHEN_StartSearch_THEN_ReusesExistingJob()
        {
            var jobId = 11;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "Ubuntu",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Stopped, 1) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc", "Ubuntu 24.04", 1_500_000_000, "http://files/ubuntu", 10, 200, "http://site", "movies", 1_700_000_000)
            }, SearchJobStatus.Stopped, 1));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            var startButton = FindComponentByTestId<MudButton>(target, "StartSearchButton");
            target.WaitForAssertion(() =>
            {
                startButton.Instance.Disabled.Should().BeFalse();
            });

            target.Find("form").Submit();

            target.WaitForAssertion(() =>
            {
                var tabPanels = target.FindAll("[role='tab']");
                tabPanels.Count.Should().Be(1);
            });

            apiMock.Verify(client => client.StartSearchAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>()), Times.Never());
            apiMock.Verify(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()), Times.Once());
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_CloseAllJobs_THEN_StopsAndDeletesMetadata()
        {
            var jobId = 21;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "Ubuntu",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 0) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));
            apiMock.Setup(client => client.StopSearchAsync(jobId)).ReturnsSuccess(Task.CompletedTask);
            apiMock.Setup(client => client.DeleteSearchAsync(jobId)).ReturnsSuccess(Task.CompletedTask);

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            await target.InvokeAsync(() =>
            {
                var closeAllMudButton = FindComponentByTestId<MudIconButton>(target, "CloseAllJobsButton");
                var closeAllButton = closeAllMudButton.FindAll("button")[0];
                closeAllButton.Click();
            });

            target.WaitForAssertion(() =>
            {
                var emptyStateTitle = FindComponentByTestId<MudText>(target, "SearchEmptyStateTitle");
                GetChildContentText(emptyStateTitle.Instance.ChildContent).Should().Be("Start a search above.");
            });

            apiMock.Verify(client => client.StopSearchAsync(jobId), Times.Once());
            apiMock.Verify(client => client.DeleteSearchAsync(jobId), Times.Once());

            var storedMetadata = await TestContext.LocalStorage.GetItemAsync<List<SearchJobMetadata>>(_jobsStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedMetadata.Should().NotBeNull();
            storedMetadata!.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_ResultFetchFails_THEN_ShowsErrorAndSnackbar()
        {
            var jobId = 31;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var statusQueue = new Queue<IReadOnlyList<SearchStatus>>();
            statusQueue.Enqueue(Array.Empty<SearchStatus>());
            statusQueue.Enqueue(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 1) });

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(() =>
            {
                var next = statusQueue.Count > 1 ? statusQueue.Dequeue() : statusQueue.Peek();
                return next;
            });
            apiMock.Setup(client => client.StartSearchAsync("Ubuntu", It.IsAny<IReadOnlyCollection<string>>(), SearchForm.AllCategoryId)).ReturnsSuccessAsync(jobId);

            apiMock.SetupSequence(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SearchResults(new List<SearchResult>
                {
                    new SearchResult("http://desc", "Ubuntu 24.04", 1_500_000_000, "http://files/ubuntu", 10, 200, "http://site", "movies", 1_700_000_000)
                }, SearchJobStatus.Running, 1))
                .ReturnsFailure<SearchResults>(ApiFailureKind.ServerError, "Server error", HttpStatusCode.InternalServerError);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                It.Is<string>(message => message.Contains("Failed to load results for \"Ubuntu\": Server error")),
                Severity.Error,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            var startButton = FindComponentByTestId<MudButton>(target, "StartSearchButton");
            target.WaitForAssertion(() =>
            {
                startButton.Instance.Disabled.Should().BeFalse();
            });

            target.Find("form").Submit();

            target.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudTabs>(target, "JobTabs").Should().NotBeNull();
            });

            await handler(CancellationToken.None);

            snackbarMock.Verify();
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_ResultNotFound_THEN_StatusStopsWithoutSnackbar()
        {
            var jobId = 41;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(jobId, SearchJobStatus.Running, 0)
            });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsFailure(ApiFailureKind.NotFound, "Not found", HttpStatusCode.NotFound);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "Ubuntu",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<Search>();
            var refreshButton = FindComponentByTestId<MudIconButton>(target, "RefreshActiveJobButton");
            await target.InvokeAsync(() => refreshButton.Find("button").Click());

            apiMock.Verify(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce());
            snackbarMock.Verify(snackbar => snackbar.Add(
                It.IsAny<string>(),
                It.IsAny<Severity>(),
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_SearchStatusRequestGetsNoResponse_THEN_FlagsConnectionLoss()
        {
            var jobId = 51;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.SetupSequence(client => client.GetSearchesStatusAsync())
                .ReturnsAsync(Array.Empty<SearchStatus>())
                .ReturnsFailure<IReadOnlyList<SearchStatus>>(ApiFailureKind.NoResponse, "Network down");
            apiMock.Setup(client => client.StartSearchAsync("Ubuntu", It.IsAny<IReadOnlyCollection<string>>(), SearchForm.AllCategoryId)).ReturnsSuccessAsync(jobId);
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));
            dialogServiceMock
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReference.Object);

            var serverState = new UiServerState();
            serverState.ConnectionStatus = ConnectionStatus.Connected;
            var mainData = new UiMainData(
                new Dictionary<string, UiTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, UiCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState,
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Search>(parameters => parameters.AddCascadingValue(mainData));

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            var startButton = FindComponentByTestId<MudButton>(target, "StartSearchButton");
            target.WaitForAssertion(() =>
            {
                startButton.Instance.Disabled.Should().BeFalse();
            });

            target.Find("form").Submit();

            target.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudTabs>(target, "JobTabs").Should().NotBeNull();
            });

            await handler(CancellationToken.None);

            dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);

            target.Render();
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_SearchStatusRequestGetsApiError_THEN_ShowsSnackbarWithoutFlaggingConnectionLoss()
        {
            var jobId = 52;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.SetupSequence(client => client.GetSearchesStatusAsync())
                .ReturnsAsync(Array.Empty<SearchStatus>())
                .ReturnsFailure<IReadOnlyList<SearchStatus>>(ApiFailureKind.ServerError, "Server error", HttpStatusCode.InternalServerError);
            apiMock.Setup(client => client.StartSearchAsync("Ubuntu", It.IsAny<IReadOnlyCollection<string>>(), SearchForm.AllCategoryId)).ReturnsSuccessAsync(jobId);
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));

            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            var serverState = new UiServerState();
            var mainData = new UiMainData(
                new Dictionary<string, UiTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, UiCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState,
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Search>(parameters => parameters.AddCascadingValue(mainData));

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            var startButton = FindComponentByTestId<MudButton>(target, "StartSearchButton");
            target.WaitForAssertion(() =>
            {
                startButton.Instance.Disabled.Should().BeFalse();
            });

            target.Find("form").Submit();

            target.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudTabs>(target, "JobTabs").Should().NotBeNull();
            });

            await handler(CancellationToken.None);

            dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Never);
            snackbarMock.Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => message.Contains("Search polling failed: Server error", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    "search-polling-api-error"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_FilterPreferencesForName_WHEN_Render_THEN_OnlyMatchingResultsDisplayed()
        {
            var jobId = 61;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase),
                FilterText = "Ubuntu",
                SearchIn = SearchInScope.Names
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "Ubuntu",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Stopped, 2) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/ubuntu", "Ubuntu 24.04", 1_500_000_000, "http://files/ubuntu", 10, 200, "http://site/ubuntu", "movies", 1_700_000_000),
                new SearchResult("http://desc/fedora", "Fedora 39", 1_600_000_000, "http://files/fedora", 8, 150, "http://site/fedora", "movies", 1_700_000_000)
            }, SearchJobStatus.Stopped, 2));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var table = target.FindComponent<DynamicTable<SearchResult>>();
                var items = table.Instance.Items.Should().NotBeNull().And.Subject.ToList();
                items.Should().ContainSingle(result => result.FileName == "Ubuntu 24.04");
                items.Should().NotContain(result => result.FileName == "Fedora 39");
            });

            var summary = FindComponentByTestId<MudText>(target, "JobSummary");
            GetChildContentText(summary.Instance.ChildContent).Should().Be("1/2");
        }

        [Fact]
        public async Task GIVEN_FilterPreferencesWithSeedAndSizeBounds_WHEN_Render_THEN_SummaryReflectsFilteredCount()
        {
            var jobId = 62;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase),
                MinimumSeeds = 50,
                MaximumSeeds = 100,
                MinimumSize = 10,
                MinimumSizeUnit = SearchSizeUnit.Mebibytes,
                MaximumSize = 1,
                MaximumSizeUnit = SearchSizeUnit.Gibibytes
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "linux",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Stopped, 3) });
            var passingSize = 100_000_000;
            var failingLarge = 3L * 1024 * 1024 * 1024;
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/passing", "Passing Result", passingSize, "http://files/passing", 20, 80, "http://site/passing", "movies", 1_700_000_000),
                new SearchResult("http://desc/lowseed", "Low Seed Result", passingSize, "http://files/lowseed", 20, 30, "http://site/lowseed", "movies", 1_700_000_000),
                new SearchResult("http://desc/largesize", "Large Size Result", failingLarge, "http://files/largesize", 20, 70, "http://site/largesize", "movies", 1_700_000_000)
            }, SearchJobStatus.Stopped, 3));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var table = target.FindComponent<DynamicTable<SearchResult>>();
                var items = table.Instance.Items.Should().NotBeNull().And.Subject.ToList();
                items.Should().ContainSingle(result => result.FileName == "Passing Result");
                items.Should().NotContain(result => result.FileName == "Low Seed Result");
                items.Should().NotContain(result => result.FileName == "Large Size Result");
            });

            var summary = FindComponentByTestId<MudText>(target, "JobSummary");
            GetChildContentText(summary.Instance.ChildContent).Should().Be("1/3");
        }

        [Fact]
        public async Task GIVEN_SearchResultsWithLinks_WHEN_Render_THEN_NameAndSiteColumnsRenderAnchors()
        {
            var jobId = 63;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "links",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Stopped, 2) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/item1", "Item One", 1_500_000_000, "http://files/item1", 10, 200, "http://site/item1", "movies", 1_700_000_000),
                new SearchResult(string.Empty, "Item Two", 500_000_000, string.Empty, 5, 50, string.Empty, "movies", null)
            }, SearchJobStatus.Stopped, 2));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var links = target.FindComponents<MudLink>();
                links.Should().Contain(link => link.Instance.Href == "http://desc/item1");
                links.Should().Contain(link => link.Instance.Href == "http://site/item1");

                var table = target.FindComponent<DynamicTable<SearchResult>>();
                var items = table.Instance.Items.Should().NotBeNull().And.Subject;
                items.Should().ContainSingle(result => result.FileName == "Item Two");
            });

            var resultsTable = target.FindComponent<DynamicTable<SearchResult>>();
            resultsTable.Instance.Items.Should().Contain(result => result.PublishedOn == 1_700_000_000);
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelection_WHEN_DownloadInvoked_THEN_InvokesAddTorrentDialog()
        {
            var result = new SearchResult("http://desc/context", "Context Item", 1_024_000_000, "http://files/context", 5, 50, "http://site/context", "movies", 1_700_000_000);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>();
            dialogMock.Setup(flow => flow.InvokeAddTorrentLinkDialog(result.FileUrl)).Returns(Task.CompletedTask).Verifiable();

            var target = await RenderSearchWithResultsAsync(201, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var downloadItem = FindContextMenuItem(Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadItem.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => dialogMock.Verify());
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelection_WHEN_CopyName_THEN_ClipboardAndSnackbarUpdated()
        {
            TestContext.Clipboard.Clear();
            var result = new SearchResult("http://desc/name", "Name Result", 900_000_000, "http://files/name", 15, 80, "http://site/name", "movies", 1_700_000_000);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                "Name copied to clipboard.",
                Severity.Success,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            var target = await RenderSearchWithResultsAsync(202, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var copyNameItem = FindContextMenuItem(Icons.Material.Filled.ContentCopy, 0);
            await target.InvokeAsync(() => copyNameItem.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => TestContext.Clipboard.PeekLast().Should().Be("Name Result"));
            target.WaitForAssertion(() => snackbarMock.Verify());
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelection_WHEN_CopyDownloadLink_THEN_ClipboardAndSnackbarUpdated()
        {
            TestContext.Clipboard.Clear();
            var result = new SearchResult("http://desc/link", "Link Result", 1_100_000_000, "http://files/link", 25, 90, "http://site/link", "movies", 1_700_000_000);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                "Download link copied to clipboard.",
                Severity.Success,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            var target = await RenderSearchWithResultsAsync(203, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var copyDownloadItem = FindContextMenuItem(Icons.Material.Filled.ContentCopy, 1);
            await target.InvokeAsync(() => copyDownloadItem.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => TestContext.Clipboard.PeekLast().Should().Be("http://files/link"));
            target.WaitForAssertion(() => snackbarMock.Verify());
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelection_WHEN_CopyDescriptionLink_THEN_ClipboardAndSnackbarUpdated()
        {
            TestContext.Clipboard.Clear();
            var result = new SearchResult("http://desc/detail", "Detail Result", 1_200_000_000, "http://files/detail", 40, 120, "http://site/detail", "movies", 1_700_000_000);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                "Description link copied to clipboard.",
                Severity.Success,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            var target = await RenderSearchWithResultsAsync(204, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var copyDescriptionItem = FindContextMenuItem(Icons.Material.Filled.ContentCopy, 2);
            await target.InvokeAsync(() => copyDescriptionItem.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => TestContext.Clipboard.PeekLast().Should().Be("http://desc/detail"));
            target.WaitForAssertion(() => snackbarMock.Verify());
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelection_WHEN_OpenDescription_THEN_InvokesBrowserOpen()
        {
            var result = new SearchResult("http://desc/open", "Open Result", 1_000_000_000, "http://files/open", 12, 60, "http://site/open", "movies", 1_700_000_000);
            var openInvocation = TestContext.JSInterop.SetupVoid(
                "open",
                invocation => invocation.Arguments.Count == 2
                    && invocation.Arguments.ElementAt(0) as string == "http://desc/open"
                    && invocation.Arguments.ElementAt(1) as string == "http://desc/open");
            openInvocation.SetVoidResult();

            var target = await RenderSearchWithResultsAsync(205, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var openDescriptionItem = FindContextMenuItem(Icons.Material.Filled.OpenInNew);
            await target.InvokeAsync(() => openDescriptionItem.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                openInvocation.Invocations.Should().ContainSingle();
            });
        }

        [Fact]
        public async Task GIVEN_LongPressSelection_WHEN_CopyDownloadLinkInvoked_THEN_NormalizedContextMenuUsed()
        {
            TestContext.Clipboard.Clear();
            var result = new SearchResult("http://desc/longpress", "Long Press Result", 1_050_000_000, "http://files/longpress", 18, 70, "http://site/longpress", "movies", 1_700_000_000);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                "Download link copied to clipboard.",
                Severity.Success,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            var target = await RenderSearchWithResultsAsync(206, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result, useLongPress: true);

            var copyDownloadItem = FindContextMenuItem(Icons.Material.Filled.ContentCopy, 1);
            await target.InvokeAsync(() => copyDownloadItem.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => TestContext.Clipboard.PeekLast().Should().Be("http://files/longpress"));
            target.WaitForAssertion(() => snackbarMock.Verify());
        }

        [Fact]
        public async Task GIVEN_StaleMetadata_WHEN_Render_THEN_MetadataCleared()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 301,
                    Pattern = "Old",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var snapshot = TestContext.LocalStorage.Snapshot();
                snapshot.TryGetValue(_jobsStorageKey, out var storedValue).Should().BeTrue();
                storedValue.Should().BeOfType<List<SearchJobMetadata>>();
                ((List<SearchJobMetadata>)storedValue!).Should().BeEmpty();
            });
        }

        [Fact]
        public async Task GIVEN_AdvancedFiltersOnSmallScreen_WHEN_SearchStarts_THEN_CollapsesFilters()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase),
                FilterText = "1080p",
                MinimumSeeds = 10
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>(), Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());
            apiMock.Setup(client => client.StartSearchAsync("Ubuntu", It.IsAny<IReadOnlyCollection<string>>(), SearchForm.AllCategoryId)).ReturnsSuccessAsync(401);
            apiMock.Setup(client => client.GetSearchResultsAsync(401, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue(Breakpoint.Sm);
                parameters.AddCascadingValue(Orientation.Portrait);
            });

            target.WaitForAssertion(() =>
            {
                var advancedFilters = FindComponentByTestId<MudCollapse>(target, "AdvancedFiltersCollapse");
                advancedFilters.Instance.Expanded.Should().BeTrue();
            });

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            target.Find("form").Submit();

            target.WaitForAssertion(() =>
            {
                var advancedFilters = FindComponentByTestId<MudCollapse>(target, "AdvancedFiltersCollapse");
                advancedFilters.Instance.Expanded.Should().BeFalse();
            });
            target.WaitForAssertion(() =>
            {
                var searchForm = FindComponentByTestId<MudCollapse>(target, "SearchFormCollapse");
                searchForm.Instance.Expanded.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_StartSearchFails_WHEN_Submit_THEN_ShowsErrorSnackbar()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase),
                FilterText = "1080p"
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>(), Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());
            apiMock.Setup(client => client.StartSearchAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "boom", HttpStatusCode.InternalServerError);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                It.Is<string>(message => message.Contains("Failed to start search: boom")),
                Severity.Error,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var advancedBefore = FindComponentByTestId<MudCollapse>(target, "AdvancedFiltersCollapse");
            advancedBefore.Instance.Expanded.Should().BeTrue();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            target.Find("form").Submit();

            target.WaitForAssertion(() => snackbarMock.Verify());
            var advancedAfter = FindComponentByTestId<MudCollapse>(target, "AdvancedFiltersCollapse");
            advancedAfter.Instance.Expanded.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_StartSearchAuthenticationRequired_WHEN_Submit_THEN_NavigatesToLogin()
        {
            var navigationManager = UseTestNavigationManager();
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());
            apiMock.Setup(client => client.StartSearchAsync("Ubuntu", It.IsAny<IReadOnlyCollection<string>>(), SearchForm.AllCategoryId))
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Search disabled", HttpStatusCode.Forbidden);

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Find("input").Input("Ubuntu");

            target.Find("form").Submit();

            target.WaitForAssertion(() =>
            {
                navigationManager.Uri.Should().Be("http://localhost/login");
                navigationManager.LastForceLoad.Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_FilterInputs_WHEN_UserAdjustsValues_THEN_PreferencesPersisted()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase),
                FilterText = "hdr",
                SearchIn = SearchInScope.Everywhere,
                MinimumSeeds = 5,
                MaximumSeeds = 50,
                MinimumSize = 5,
                MinimumSizeUnit = SearchSizeUnit.Mebibytes,
                MaximumSize = 2,
                MaximumSizeUnit = SearchSizeUnit.Gibibytes
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>(), Xunit.TestContext.Current.CancellationToken);

            var categories = new[]
            {
                new SearchCategory("movies", "Movies"),
                new SearchCategory("tv", "TV")
            };
            var plugin = new SearchPlugin(true, "Movies", "movies", categories, "http://plugins/movies", "1.0");

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var advancedFilters = FindComponentByTestId<MudCollapse>(target, "AdvancedFiltersCollapse");
            advancedFilters.Instance.Expanded.Should().BeTrue();

            var categorySelect = FindComponentByTestId<MudSelect<string>>(target, "CategorySelect");
            await target.InvokeAsync(() => categorySelect.Instance.ValueChanged.InvokeAsync("tv"));

            var filterField = FindComponentByTestId<MudTextField<string>>(target, "FilterResults");
            await target.InvokeAsync(() => filterField.Instance.ValueChanged.InvokeAsync("dv"));

            var searchInSelect = FindComponentByTestId<MudSelect<SearchInScope>>(target, "SearchInScopeSelect");
            await target.InvokeAsync(() => searchInSelect.Instance.ValueChanged.InvokeAsync(SearchInScope.Names));

            var minSeedsField = FindComponentByTestId<MudNumericField<int?>>(target, "MinSeeders");
            await target.InvokeAsync(() => minSeedsField.Instance.ValueChanged.InvokeAsync(10));

            var maxSeedsField = FindComponentByTestId<MudNumericField<int?>>(target, "MaxSeeders");
            await target.InvokeAsync(() => maxSeedsField.Instance.ValueChanged.InvokeAsync(120));

            var minSizeField = FindComponentByTestId<MudNumericField<double?>>(target, "MinSize");
            await target.InvokeAsync(() => minSizeField.Instance.ValueChanged.InvokeAsync(8d));

            var sizeUnitSelects = target.FindComponents<MudSelect<SearchSizeUnit>>().ToList();
            var minSizeUnitSelect = sizeUnitSelects[0];
            await target.InvokeAsync(() => minSizeUnitSelect.Instance.ValueChanged.InvokeAsync(SearchSizeUnit.Gibibytes));

            var maxSizeField = FindComponentByTestId<MudNumericField<double?>>(target, "MaxSize");
            await target.InvokeAsync(() => maxSizeField.Instance.ValueChanged.InvokeAsync(4d));

            var maxSizeUnitSelect = sizeUnitSelects[1];
            await target.InvokeAsync(() => maxSizeUnitSelect.Instance.ValueChanged.InvokeAsync(SearchSizeUnit.Tebibytes));

            target.WaitForAssertion(() =>
            {
                var snapshot = TestContext.LocalStorage.Snapshot();
                snapshot.TryGetValue(_preferencesStorageKey, out var storedValue).Should().BeTrue();
                storedValue.Should().BeOfType<SearchPreferences>();
                var stored = (SearchPreferences)storedValue!;
                stored.SelectedCategory.Should().Be("tv");
                stored.FilterText.Should().Be("dv");
                stored.SearchIn.Should().Be(SearchInScope.Names);
                stored.MinimumSeeds.Should().Be(10);
                stored.MaximumSeeds.Should().Be(120);
                stored.MinimumSize.Should().Be(8);
                stored.MinimumSizeUnit.Should().Be(SearchSizeUnit.Gibibytes);
                stored.MaximumSize.Should().Be(4);
                stored.MaximumSizeUnit.Should().Be(SearchSizeUnit.Tebibytes);
            });
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelectionWithoutFileUrl_WHEN_DownloadInvoked_THEN_DialogNotCalled()
        {
            var result = new SearchResult("http://desc/no-file", "No File", 1_000, string.Empty, 1, 2, "http://site/no-file", "movies", null);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>();

            var target = await RenderSearchWithResultsAsync(207, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var downloadItem = FindContextMenuItem(Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadItem.Instance.OnClick.InvokeAsync());

            dialogMock.Verify(flow => flow.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelectionWithoutName_WHEN_CopyName_THEN_NoClipboardOrSnackbar()
        {
            TestContext.Clipboard.Clear();
            var result = new SearchResult("http://desc/no-name", string.Empty, 1_000, "http://files/no-name", 2, 3, "http://site/no-name", "movies", null);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Strict);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            var target = await RenderSearchWithResultsAsync(208, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var copyNameItem = FindContextMenuItem(Icons.Material.Filled.ContentCopy, 0);
            await target.InvokeAsync(() => copyNameItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelectionWithoutDownloadLink_WHEN_CopyDownloadLink_THEN_NoClipboardOrSnackbar()
        {
            TestContext.Clipboard.Clear();
            var result = new SearchResult("http://desc/no-dl", "No Download", 1_000, string.Empty, 2, 3, "http://site/no-dl", "movies", null);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Strict);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            var target = await RenderSearchWithResultsAsync(209, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var copyDownloadItem = FindContextMenuItem(Icons.Material.Filled.ContentCopy, 1);
            await target.InvokeAsync(() => copyDownloadItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ContextMenuSelectionWithoutDescriptionLink_WHEN_CopyDescriptionLink_THEN_NoClipboardOrSnackbar()
        {
            TestContext.Clipboard.Clear();
            var result = new SearchResult(string.Empty, "No Description", 1_000, "http://files/no-desc", 2, 3, "http://site/no-desc", "movies", null);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Strict);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            var target = await RenderSearchWithResultsAsync(210, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var copyDescriptionItem = FindContextMenuItem(Icons.Material.Filled.ContentCopy, 2);
            await target.InvokeAsync(() => copyDescriptionItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NullContextMenuItem_WHEN_HandleContextMenuInvoked_THEN_ContextNotSet()
        {
            var result = new SearchResult("http://desc/item", "Item", 1_000_000, "http://files/item", 1, 10, "http://site/item", "movies", null);
            var target = await RenderSearchWithResultsAsync(211, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, null, expectMenuOpen: false);
            GetContextMenuItems().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_LongPressWithoutItem_WHEN_HandleLongPressInvoked_THEN_ContextNotSet()
        {
            var result = new SearchResult("http://desc/item-long", "Item", 1_000_000, "http://files/item", 1, 10, "http://site/item", "movies", null);
            var target = await RenderSearchWithResultsAsync(212, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, null, useLongPress: true, expectMenuOpen: false);
            GetContextMenuItems().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ContextMenuReferenceMissing_WHEN_HandleContextMenuInvoked_THEN_NoMenuOpened()
        {
            var result = new SearchResult("http://desc/item2", "Item 2", 1_000_000, "http://files/item2", 1, 10, "http://site/item2", "movies", null);
            var target = await RenderSearchWithResultsAsync(213, new List<SearchResult> { result });

            await OpenContextMenuAsync(target, result);
            var openDescriptionItem = FindContextMenuItem(Icons.Material.Filled.OpenInNew);
            openDescriptionItem.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_PluginLoadReturnsForbidden_WHEN_HydrateJobsRuns_THEN_MetadataPreservedAndNavigatesToLogin()
        {
            var navigationManager = UseTestNavigationManager();
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 500,
                    Pattern = "Legacy",
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "disabled", HttpStatusCode.Forbidden);
            apiMock.Setup(client => client.GetSearchesStatusAsync())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "disabled", HttpStatusCode.Forbidden);

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                navigationManager.Uri.Should().Be("http://localhost/login");
                navigationManager.LastForceLoad.Should().BeTrue();
            });

            var stored = await TestContext.LocalStorage.GetItemAsync<List<SearchJobMetadata>>(_jobsStorageKey, Xunit.TestContext.Current.CancellationToken);
            stored.Should().NotBeNull();
            stored!.Should().HaveCount(1);
            stored[0].Id.Should().Be(500);
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_StopJobInvoked_THEN_StatusUpdated()
        {
            Mock<IApiClient>? apiMockReference = null;
            var target = await RenderSearchWithResultsAsync(300, new List<SearchResult>(), SearchJobStatus.Running, 0, apiMock =>
            {
                apiMock.Setup(client => client.StopSearchAsync(300)).ReturnsSuccess(Task.CompletedTask).Verifiable();
                apiMockReference = apiMock;
            });

            var stopButton = FindComponentByTestId<MudIconButton>(target, "StopActiveJobButton");
            await target.InvokeAsync(() => stopButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                var statusIcon = FindComponentByTestId<MudIcon>(target, "JobStatusIcon");
                statusIcon.Instance.Icon.Should().Be(Icons.Material.Filled.Stop);
                apiMockReference.Should().NotBeNull();
                apiMockReference!.Verify(client => client.StopSearchAsync(300), Times.Once());
            });
        }

        [Fact]
        public async Task GIVEN_StopJobFails_WHEN_Invoke_THEN_ShowsSnackbar()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
        {
            new SearchJobMetadata
            {
                Id = 301,
                Pattern = "Context",
                Plugins = new List<string> { "movies" }
            }
        }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", Array.Empty<SearchCategory>(), "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(301, SearchJobStatus.Running, 0) });
            apiMock.Setup(client => client.GetSearchResultsAsync(301, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));
            apiMock.Setup(client => client.StopSearchAsync(301))
                .ReturnsFailure(ApiFailureKind.ServerError, "stop failed", HttpStatusCode.InternalServerError);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add("Failed to stop \"Context\": stop failed", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var stopButton = FindComponentByTestId<MudIconButton>(target, "StopActiveJobButton");
            await target.InvokeAsync(() => stopButton.Find("button").Click());

            target.WaitForAssertion(() => snackbarMock.Verify());
            var statusIcon = FindComponentByTestId<MudIcon>(target, "JobStatusIcon");
            statusIcon.Instance.Icon.Should().Be(Icons.Material.Filled.Sync);
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_RefreshJobInvoked_THEN_ResultsReloaded()
        {
            var initialResults = new List<SearchResult>
        {
            new SearchResult("http://desc/initial", "Initial", 1_000_000, "http://files/initial", 1, 10, "http://site/initial", "movies", null)
        };
            var refreshedResults = new List<SearchResult>
        {
            new SearchResult("http://desc/refreshed", "Refreshed", 2_000_000, "http://files/refreshed", 2, 20, "http://site/refreshed", "movies", null)
        };

            var target = await RenderSearchWithResultsAsync(302, initialResults, SearchJobStatus.Running, initialResults.Count, apiMock =>
            {
                apiMock.SetupSequence(client => client.GetSearchResultsAsync(302, It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(new SearchResults(initialResults, SearchJobStatus.Running, initialResults.Count))
                    .ReturnsAsync(new SearchResults(refreshedResults, SearchJobStatus.Running, refreshedResults.Count))
                    .ReturnsAsync(new SearchResults(refreshedResults, SearchJobStatus.Running, refreshedResults.Count));
            });

            var tableBefore = target.FindComponent<DynamicTable<SearchResult>>();
            tableBefore.Instance.Items.Should().Contain(result => result.FileName == "Initial");

            var refreshButton = FindComponentByTestId<MudIconButton>(target, "RefreshActiveJobButton");
            await target.InvokeAsync(() => refreshButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                var table = target.FindComponent<DynamicTable<SearchResult>>();
                table.Instance.Items.Should().Contain(result => result.FileName == "Refreshed");
                table.Instance.Items.Should().NotContain(result => result.FileName == "Initial");
            });
        }

        [Fact]
        public async Task GIVEN_DeleteFails_WHEN_CloseAllJobs_THEN_JobRemains()
        {
            var apiFeedbackWorkflow = new Mock<IApiFeedbackWorkflow>(MockBehavior.Strict);
            apiFeedbackWorkflow
                .Setup(workflow => workflow.HandleFailureAsync(
                    It.IsAny<ApiResult>(),
                    It.IsAny<Func<string?, string>?>(),
                    It.IsAny<Severity>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            TestContext.Services.RemoveAll<IApiFeedbackWorkflow>();
            TestContext.Services.AddSingleton(apiFeedbackWorkflow.Object);

            var target = await RenderSearchWithResultsAsync(303, new List<SearchResult>
        {
            new SearchResult("http://desc/close", "Close", 1_000_000, "http://files/close", 1, 5, "http://site/close", "movies", null)
        }, SearchJobStatus.Stopped, 1, apiMock =>
        {
            apiMock.Setup(client => client.DeleteSearchAsync(303))
                .ReturnsFailure(ApiFailureKind.ServerError, "delete", HttpStatusCode.InternalServerError);
        });

            var closeAllButton = FindComponentByTestId<MudIconButton>(target, "CloseAllJobsButton");
            await target.InvokeAsync(() => closeAllButton.Find("button").Click());

            target.WaitForAssertion(() => target.FindComponents<DynamicTable<SearchResult>>().Should().HaveCount(1));
            apiFeedbackWorkflow.Verify(workflow => workflow.HandleFailureAsync(
                It.IsAny<ApiResult>(),
                It.IsAny<Func<string?, string>?>(),
                It.IsAny<Severity>(),
                It.IsAny<CancellationToken>()), Times.Once);
            var stored = await TestContext.LocalStorage.GetItemAsync<List<SearchJobMetadata>>(_jobsStorageKey, Xunit.TestContext.Current.CancellationToken);
            stored.Should().NotBeNull();
            stored!.Should().HaveCount(1);
        }

        [Fact]
        public async Task GIVEN_RunningJob_WHEN_CloseAllJobs_THEN_StopAndDeleteCalled()
        {
            Mock<IApiClient>? apiMockReference = null;
            var target = await RenderSearchWithResultsAsync(304, new List<SearchResult>(), SearchJobStatus.Running, 0, apiMock =>
            {
                apiMock.Setup(client => client.StopSearchAsync(304)).ReturnsSuccess(Task.CompletedTask).Verifiable();
                apiMock.Setup(client => client.DeleteSearchAsync(304)).ReturnsSuccess(Task.CompletedTask).Verifiable();
                apiMockReference = apiMock;
            });

            var closeAllButton = FindComponentByTestId<MudIconButton>(target, "CloseAllJobsButton");
            await target.InvokeAsync(() => closeAllButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                apiMockReference.Should().NotBeNull();
                apiMockReference!.Verify(client => client.StopSearchAsync(304), Times.Once());
                apiMockReference.Verify(client => client.DeleteSearchAsync(304), Times.Once());
            });

            target.WaitForAssertion(() => target.FindComponents<DynamicTable<SearchResult>>().Should().BeEmpty());
        }

        [Fact]
        public async Task GIVEN_DrawerClosed_WHEN_BackToolbarButtonClicked_THEN_NavigatesToHome()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
            });

            var backButton = FindIconButton(target, Icons.Material.Outlined.NavigateBefore);
            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            var navigationManager = TestContext.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            navigationManager.Uri.Should().Be(navigationManager.BaseUri);
        }

        [Fact]
        public async Task GIVEN_SearchFormToggles_WHEN_Clicked_THEN_ExpansionStateUpdates()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var searchFormCollapse = FindComponentByTestId<MudCollapse>(target, "SearchFormCollapse");
            searchFormCollapse.Instance.Expanded.Should().BeTrue();

            var toggleSearchFormButton = FindComponentByTestId<MudButton>(target, "ToggleSearchForm");
            await target.InvokeAsync(() => toggleSearchFormButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            searchFormCollapse.Instance.Expanded.Should().BeFalse();

            await target.InvokeAsync(() => toggleSearchFormButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            searchFormCollapse.Instance.Expanded.Should().BeTrue();

            var toggleAdvancedButton = FindComponentByTestId<MudButton>(target, "ToggleAdvancedFilters");
            GetChildContentText(toggleAdvancedButton.Instance.ChildContent).Should().Be("Show filters");
            await target.InvokeAsync(() => toggleAdvancedButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            GetChildContentText(toggleAdvancedButton.Instance.ChildContent).Should().Be("Hide filters");

            await target.InvokeAsync(() => toggleAdvancedButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            GetChildContentText(toggleAdvancedButton.Instance.ChildContent).Should().Be("Show filters");
        }

        [Fact]
        public async Task GIVEN_NoActiveJob_WHEN_ToolbarActionsInvoked_THEN_NoJobCommandsExecuted()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            var stopButton = FindComponentByTestId<MudIconButton>(target, "StopActiveJobButton");
            var refreshButton = FindComponentByTestId<MudIconButton>(target, "RefreshActiveJobButton");
            var closeButton = FindComponentByTestId<MudIconButton>(target, "CloseActiveJobButton");

            await target.InvokeAsync(() => stopButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => closeButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            apiMock.Verify(client => client.StopSearchAsync(It.IsAny<int>()), Times.Never());
            apiMock.Verify(client => client.GetSearchResultsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
            apiMock.Verify(client => client.DeleteSearchAsync(It.IsAny<int>()), Times.Never());
        }

        [Fact]
        public void GIVEN_MissingMetadata_WHEN_JobHydrated_THEN_FallbackJobLabelUsed()
        {
            var jobId = 777;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 0) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var jobTexts = target.FindComponents<MudText>();
                jobTexts.Any(text => string.Equals(GetChildContentText(text.Instance.ChildContent), "Job #777", StringComparison.Ordinal)).Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_JobStatusVariants_WHEN_TabsRender_THEN_IconsAndColorsMatchStatus()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata { Id = 710, Pattern = "Finished", Plugins = new List<string> { "movies" } },
                new SearchJobMetadata { Id = 711, Pattern = "Aborted", Plugins = new List<string> { "movies" } },
                new SearchJobMetadata { Id = 712, Pattern = "Other", Plugins = new List<string> { "movies" } }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(710, SearchJobStatus.Stopped, 1),
                new SearchStatus(711, SearchJobStatus.Stopped, 1),
                new SearchStatus(712, SearchJobStatus.Stopped, 1)
            });
            apiMock.Setup(client => client.GetSearchResultsAsync(710, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Stopped, 1));
            apiMock.Setup(client => client.GetSearchResultsAsync(711, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Stopped, 1));
            apiMock.Setup(client => client.GetSearchResultsAsync(712, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Stopped, 1));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            target.WaitForAssertion(() =>
            {
                var statusIcons = target.FindComponents<MudIcon>().Where(component => HasTestId(component, "JobStatusIcon")).ToList();
                statusIcons.Should().HaveCount(3);
                statusIcons.Should().OnlyContain(component => component.Instance.Icon == Icons.Material.Filled.Stop && component.Instance.Color == Color.Warning);
            });
        }

        [Fact]
        public async Task GIVEN_UnknownJobStatus_WHEN_TabsRender_THEN_DefaultIconAndColorAreUsed()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata { Id = 713, Pattern = "Unknown", Plugins = new List<string> { "movies" } }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(713, (SearchJobStatus)999, 1)
            });
            apiMock.Setup(client => client.GetSearchResultsAsync(713, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), (SearchJobStatus)999, 1));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            target.WaitForAssertion(() =>
            {
                var statusIcon = FindComponentByTestId<MudIcon>(target, "JobStatusIcon");
                statusIcon.Instance.Icon.Should().Be(Icons.Material.Filled.Task);
                statusIcon.Instance.Color.Should().Be(Color.Default);
            });
        }

        [Fact]
        public async Task GIVEN_ContextItemWithoutDescriptionLink_WHEN_OpenDescriptionActionInvoked_THEN_NoBrowserOpenCalled()
        {
            var result = new SearchResult(string.Empty, "No Description", 1_000, "http://files/no-desc", 2, 3, "http://site/no-desc", "movies", null);
            var openInvocation = TestContext.JSInterop.SetupVoid(
                "open",
                invocation => invocation.Arguments.Count == 2
                    && invocation.Arguments.ElementAt(0) is string first
                    && invocation.Arguments.ElementAt(1) is string second
                    && string.Equals(first, second, StringComparison.Ordinal));

            var target = await RenderSearchWithResultsAsync(802, new List<SearchResult> { result });
            await OpenContextMenuAsync(target, result);
            var openDescriptionItem = FindContextMenuItem(Icons.Material.Filled.OpenInNew);
            await target.InvokeAsync(() => openDescriptionItem.Instance.OnClick.InvokeAsync());

            openInvocation.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_PollingWithNoRunningJobs_WHEN_TimerTickRuns_THEN_ContinueWithoutAdditionalFetch()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 901,
                    Pattern = "CompletedJob",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(901, SearchJobStatus.Stopped, 1) });
            apiMock.Setup(client => client.GetSearchResultsAsync(901, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/result", "Result", 1_000, "http://files/result", 1, 2, "http://site/result", "movies", null)
            }, SearchJobStatus.Stopped, 1));

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>();

            var tickResult = await handler(CancellationToken.None);

            tickResult.Should().Be(ManagedTimerTickResult.Continue);
            apiMock.Verify(client => client.GetSearchResultsAsync(901, It.IsAny<int>(), It.IsAny<int>()), Times.Once());
        }

        [Fact]
        public async Task GIVEN_PollingWithCompletedTotalZero_WHEN_TimerTickRuns_THEN_RefetchesResults()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 902,
                    Pattern = "ZeroTotal",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 902,
                    Pattern = "ZeroTotal",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                },
                new SearchJobMetadata
                {
                    Id = 906,
                    Pattern = "Running",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(902, SearchJobStatus.Stopped, 0),
                new SearchStatus(906, SearchJobStatus.Running, 1)
            });
            apiMock.SetupSequence(client => client.GetSearchResultsAsync(902, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Stopped, 0))
                .ReturnsAsync(new SearchResults(new List<SearchResult>
                {
                    new SearchResult("http://desc/refetch", "Refetched", 1_000, "http://files/refetch", 1, 2, "http://site/refetch", "movies", null)
                }, SearchJobStatus.Stopped, 1));
            apiMock.Setup(client => client.GetSearchResultsAsync(906, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 1));

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>();

            await handler(CancellationToken.None);

            apiMock.Verify(client => client.GetSearchResultsAsync(902, It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_PollingStatusRequestThrowsNonHttp_WHEN_TimerTickRuns_THEN_PollingFailureSnackbarShown()
        {
            var jobId = 903;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.SetupSequence(client => client.GetSearchesStatusAsync())
                .ReturnsAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 1) })
                .ReturnsFailure<IReadOnlyList<SearchStatus>>(ApiFailureKind.UnexpectedResponse, "PollingBoom");
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 1));

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                "Search polling failed: PollingBoom",
                Severity.Error,
                It.IsAny<Action<SnackbarOptions>>(),
                "search-polling-api-error")).Returns((Snackbar?)null).Verifiable();

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<Search>();

            var tickResult = await handler(CancellationToken.None);

            tickResult.Should().Be(ManagedTimerTickResult.Continue);
            snackbarMock.Verify();
        }

        [Fact]
        public async Task GIVEN_PollingStatusRequestThrowsException_WHEN_TimerTickRuns_THEN_PollingStopsAndShowsFailureSnackbar()
        {
            var jobId = 913;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "Ubuntu",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.SetupSequence(client => client.GetSearchesStatusAsync())
                .ReturnsAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 1) })
                .ThrowsAsync(new InvalidOperationException("PollingBoom"));
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 1));

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                "Search polling stopped: PollingBoom",
                Severity.Error,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Search>();

            var tickResult = await handler(CancellationToken.None);

            tickResult.Should().Be(ManagedTimerTickResult.Stop);
            snackbarMock.Verify();
        }

        [Fact]
        public async Task GIVEN_PollingTokenAlreadyCancelled_WHEN_TimerTickRuns_THEN_StopsWithoutFetchingBatch()
        {
            var jobId = 904;
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 1) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 1));

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();
            var tickResult = await handler(cancellationTokenSource.Token);

            tickResult.Should().Be(ManagedTimerTickResult.Stop);
            apiMock.Verify(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()), Times.Once());
        }

        [Fact]
        public async Task GIVEN_PollingAuthFailure_WHEN_TimerTickRuns_THEN_CancelsPollingAndNavigatesToLoginWithForceLoad()
        {
            var navigationManager = UseTestNavigationManager();
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 905,
                    Pattern = "AuthFailure",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.SetupSequence(client => client.GetSearchesStatusAsync())
                .ReturnsAsync(new List<SearchStatus> { new SearchStatus(905, SearchJobStatus.Running, 1) })
                .ReturnsFailure<IReadOnlyList<SearchStatus>>(ApiFailureKind.AuthenticationRequired, "Search disabled", HttpStatusCode.Forbidden);
            apiMock.Setup(client => client.GetSearchResultsAsync(905, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 1));

            var capture = CapturePollHandlerRegistration();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>();

            var tickResult = await capture.InvokeAsync(CancellationToken.None);

            tickResult.Should().Be(ManagedTimerTickResult.Continue);
            capture.SubscriptionToken.IsCancellationRequested.Should().BeTrue();
            navigationManager.Uri.Should().Be("http://localhost/login");
            navigationManager.LastForceLoad.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ResultPollingAuthFailure_WHEN_TimerTickRuns_THEN_CancelsPollingAndNavigatesToLoginWithForceLoad()
        {
            var navigationManager = UseTestNavigationManager();
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 914,
                    Pattern = "AuthResultFailure",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync())
                .ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(914, SearchJobStatus.Running, 1) });
            apiMock.SetupSequence(client => client.GetSearchResultsAsync(914, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 1))
                .ReturnsFailure<SearchResults>(ApiFailureKind.AuthenticationRequired, "Search disabled", HttpStatusCode.Forbidden);

            var capture = CapturePollHandlerRegistration();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            TestContext.Render<Search>();

            var tickResult = await capture.InvokeAsync(CancellationToken.None);

            tickResult.Should().Be(ManagedTimerTickResult.Continue);
            capture.SubscriptionToken.IsCancellationRequested.Should().BeTrue();
            navigationManager.Uri.Should().Be("http://localhost/login");
            navigationManager.LastForceLoad.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_InitialStatusRequestGetsNoResponse_WHEN_Rendered_THEN_LostConnectionFlagSet()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync())
                .ReturnsFailure(ApiFailureKind.NoResponse, "initial failure");
            dialogServiceMock
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReference.Object);

            var serverState = new UiServerState();
            var mainData = new UiMainData(
                new Dictionary<string, UiTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, UiCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState,
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>(parameters => parameters.AddCascadingValue(mainData));

            dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);
        }

        [Fact]
        public void GIVEN_InitialStatusRequestGetsApiError_WHEN_Rendered_THEN_ShowsSnackbarWithoutLostConnectionFlag()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "initial failure", HttpStatusCode.InternalServerError);

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            var serverState = new UiServerState();
            var mainData = new UiMainData(
                new Dictionary<string, UiTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, UiCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState,
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Search>(parameters => parameters.AddCascadingValue(mainData));

            target.WaitForAssertion(() =>
            {
                dialogServiceMock.Verify(
                    service => service.ShowAsync<LostConnectionDialog>(
                        It.IsAny<string?>(),
                        It.IsAny<DialogOptions>()),
                    Times.Never);
                snackbarMock.Verify(
                    snackbar => snackbar.Add(
                        It.Is<string>(message => message.Contains("Search polling stopped: initial failure", StringComparison.Ordinal)),
                        Severity.Error,
                        It.IsAny<Action<SnackbarOptions>>(),
                        It.IsAny<string>()),
                    Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_DuplicateStatusEntries_WHEN_HydratingJobs_THEN_DuplicateJobNotAdded()
        {
            var jobId = 905;
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata { Id = jobId, Pattern = "Duplicate", Plugins = new List<string> { "movies" } }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(jobId, SearchJobStatus.Running, 0),
                new SearchStatus(jobId, SearchJobStatus.Running, 0)
            });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var tabs = target.FindAll("[role='tab']");
                tabs.Count.Should().Be(1);
            });

            apiMock.Verify(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()), Times.Once());
        }

        [Fact]
        public async Task GIVEN_StoredPreferencesWithEmptyCategory_WHEN_Rendered_THEN_DefaultCategoryApplied()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedCategory = string.Empty,
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var categorySelect = FindComponentByTestId<MudSelect<string>>(target, "CategorySelect");
            categorySelect.Instance.GetState(x => x.Value).Should().Be(SearchForm.AllCategoryId);
        }

        [Fact]
        public async Task GIVEN_TableRendered_WHEN_ColumnsBuilt_THEN_ExpectedColumnIdsExist()
        {
            var target = await RenderSearchWithResultsAsync(906, new List<SearchResult>
            {
                new SearchResult("http://desc/columns", "Columns", 1_000, "http://files/columns", 1, 2, "http://site/columns", "movies", 1_700_000_000)
            });

            var table = target.FindComponent<DynamicTable<SearchResult>>();
            var ids = table.Instance.ColumnDefinitions.Select(column => column.Id).ToList();
            ids.Should().Contain("engine_url");
            ids.Should().Contain("published_on");
            ids.Should().Contain("actions");
        }

        [Fact]
        public async Task GIVEN_TwoJobsWithSecondActive_WHEN_FirstTabClosed_THEN_ActiveIndexRebased()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata { Id = 907, Pattern = "First", Plugins = new List<string> { "movies" } },
                new SearchJobMetadata { Id = 908, Pattern = "Second", Plugins = new List<string> { "movies" } }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(907, SearchJobStatus.Stopped, 1),
                new SearchStatus(908, SearchJobStatus.Stopped, 1)
            });
            apiMock.Setup(client => client.GetSearchResultsAsync(907, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/first", "First Result", 1_000, "http://files/first", 1, 2, "http://site/first", "movies", null)
            }, SearchJobStatus.Stopped, 1));
            apiMock.Setup(client => client.GetSearchResultsAsync(908, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/second", "Second Result", 1_000, "http://files/second", 1, 2, "http://site/second", "movies", null)
            }, SearchJobStatus.Stopped, 1));
            apiMock.Setup(client => client.DeleteSearchAsync(It.IsAny<int>())).ReturnsSuccess(Task.CompletedTask);

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var tabs = FindComponentByTestId<MudTabs>(target, "JobTabs");
            await target.InvokeAsync(() => tabs.Instance.ActivePanelIndexChanged.InvokeAsync(1));

            var tabCloseButtons = target.FindComponents<MudIconButton>()
                .Where(button => button.Instance.Icon == Icons.Material.Filled.Close && button.Instance.Color == Color.Inherit)
                .ToList();
            await target.InvokeAsync(() => tabCloseButtons[0].Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            target.WaitForAssertion(() =>
            {
                target.FindAll("[role='tab']").Count.Should().Be(1);
                target.FindComponents<MudText>().Any(text => string.Equals(GetChildContentText(text.Instance.ChildContent), "Second", StringComparison.Ordinal)).Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_RunningAndCompletedJobs_WHEN_PollTickRuns_THEN_CompletedJobBatchIsNotRefetched()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata { Id = 909, Pattern = "Running", Plugins = new List<string> { "movies" } },
                new SearchJobMetadata { Id = 910, Pattern = "Completed", Plugins = new List<string> { "movies" } }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus>
            {
                new SearchStatus(909, SearchJobStatus.Running, 1),
                new SearchStatus(910, SearchJobStatus.Stopped, 1)
            });
            apiMock.Setup(client => client.GetSearchResultsAsync(909, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/running", "Running Result", 1_000, "http://files/running", 1, 2, "http://site/running", "movies", null)
            }, SearchJobStatus.Running, 1));
            apiMock.Setup(client => client.GetSearchResultsAsync(910, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc/completed", "Completed Result", 1_000, "http://files/completed", 1, 2, "http://site/completed", "movies", null)
            }, SearchJobStatus.Stopped, 1));

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>();

            await handler(CancellationToken.None);

            apiMock.Verify(client => client.GetSearchResultsAsync(910, It.IsAny<int>(), It.IsAny<int>()), Times.Once());
            apiMock.Verify(client => client.GetSearchResultsAsync(909, It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_RunningJobAndUnexpectedResultError_WHEN_PollTickRuns_THEN_GenericPollingFailureIsHandled()
        {
            var jobId = 911;
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata { Id = jobId, Pattern = "Unexpected", Plugins = new List<string> { "movies" } }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 1) });
            apiMock.SetupSequence(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 1))
                .ReturnsFailure<SearchResults>(ApiFailureKind.UnexpectedResponse, "UnexpectedResultFailure");

            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());
            snackbarMock.Setup(snackbar => snackbar.Add(
                "Failed to load results for \"Unexpected\": UnexpectedResultFailure",
                Severity.Error,
                It.IsAny<Action<SnackbarOptions>>(),
                It.IsAny<string>())).Returns((Snackbar?)null).Verifiable();

            var handler = CapturePollHandler();
            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Search>();

            await handler(CancellationToken.None);

            snackbarMock.Verify();
            var statusIcon = FindComponentByTestId<MudIcon>(target, "JobStatusIcon");
            statusIcon.Instance.Icon.Should().Be(Icons.Material.Filled.Sync);
        }

        [Fact]
        public async Task GIVEN_RunningJobAndStopFailsWithHttp_WHEN_CloseAllJobsClicked_THEN_DeleteDoesNotExecute()
        {
            var jobId = 912;
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata { Id = jobId, Pattern = "StopFail", Plugins = new List<string> { "movies" } }
            }, Xunit.TestContext.Current.CancellationToken);

            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, SearchJobStatus.Running, 0) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(new List<SearchResult>(), SearchJobStatus.Running, 0));
            apiMock.Setup(client => client.StopSearchAsync(jobId))
                .ReturnsFailure(ApiFailureKind.ServerError, "stop-http", HttpStatusCode.InternalServerError);
            apiMock.Setup(client => client.DeleteSearchAsync(jobId)).ReturnsSuccess(Task.CompletedTask).Verifiable();

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var closeAllButton = FindComponentByTestId<MudIconButton>(target, "CloseAllJobsButton");
            await target.InvokeAsync(() => closeAllButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                target.FindComponents<DynamicTable<SearchResult>>().Should().HaveCount(1);
            });

            apiMock.Verify(client => client.StopSearchAsync(jobId), Times.Once());
            apiMock.Verify(client => client.DeleteSearchAsync(jobId), Times.Never());

            var stored = await TestContext.LocalStorage.GetItemAsync<List<SearchJobMetadata>>(_jobsStorageKey, Xunit.TestContext.Current.CancellationToken);
            stored.Should().NotBeNull();
            stored!.Should().HaveCount(1);
        }

        [Fact]
        public async Task GIVEN_PluginSelectionDelegate_WHEN_Invoked_THEN_NullAndJoinedTextPathsReturned()
        {
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);

            var pluginA = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var pluginB = new SearchPlugin(true, "Shows", "shows", new[] { new SearchCategory("shows", "Shows") }, "http://plugins/shows", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { pluginA, pluginB });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");

            var selectionTextFunc = pluginSelect.Instance.MultiSelectionTextFunc;
            selectionTextFunc.Should().NotBeNull();
            selectionTextFunc!(null).Should().BeNull();
            selectionTextFunc(new List<string?> { "movies" }).Should().Be("movies");
            selectionTextFunc(new List<string?> { "movies", "shows" }).Should().Be("All enabled plugins");
        }

        [Fact]
        public async Task GIVEN_PluginSelectionAndFilterUpdates_WHEN_NullAndWhitespaceProvided_THEN_StateNormalizes()
        {
            var pluginA = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var pluginB = new SearchPlugin(true, "Shows", "shows", new[] { new SearchCategory("shows", "Shows") }, "http://plugins/shows", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { pluginA, pluginB });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();
            var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "PluginSelect");
            await target.InvokeAsync(() => pluginSelect.Instance.SelectedValuesChanged.InvokeAsync(default(IReadOnlyCollection<string?>)!));

            target.WaitForAssertion(() =>
            {
                pluginSelect.Instance.GetState(x => x.SelectedValues).Should().Contain("movies");
                pluginSelect.Instance.GetState(x => x.SelectedValues).Should().Contain("shows");
            });

            var filterField = FindComponentByTestId<MudTextField<string>>(target, "FilterResults");
            await target.InvokeAsync(() => filterField.Instance.ValueChanged.InvokeAsync("   "));

            var stored = await TestContext.LocalStorage.GetItemAsync<SearchPreferences>(_preferencesStorageKey, Xunit.TestContext.Current.CancellationToken);
            stored.Should().NotBeNull();
            stored!.FilterText.Should().BeNull();
        }

        [Fact]
        public void GIVEN_HashParameter_WHEN_ComponentRendered_THEN_ParameterValueIsApplied()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>(parameters => parameters.Add(parameter => parameter.Hash, "Hash"));
            target.Instance.Hash.Should().Be("Hash");
        }

        [Fact]
        public async Task GIVEN_ComponentDisposedTwice_WHEN_DisposeAsyncCalled_THEN_CompletesWithoutError()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(Array.Empty<SearchStatus>());

            TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();
            var target = TestContext.Render<Search>();

            await target.Instance.DisposeAsync();
            await target.Instance.DisposeAsync();
        }

        private async Task<IRenderedComponent<Search>> RenderSearchWithResultsAsync(int jobId, List<SearchResult> results, SearchJobStatus status = SearchJobStatus.Stopped, int? totalOverride = null, Action<Mock<IApiClient>>? configureMock = null)
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(_preferencesStorageKey, new SearchPreferences
            {
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase)
            }, Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsync(_jobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = jobId,
                    Pattern = "Context",
                    Category = SearchForm.AllCategoryId,
                    Plugins = new List<string> { "movies" }
                }
            }, Xunit.TestContext.Current.CancellationToken);

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPluginsAsync()).ReturnsSuccessAsync(new List<SearchPlugin> { plugin });
            var total = totalOverride ?? results.Count;
            apiMock.Setup(client => client.GetSearchesStatusAsync()).ReturnsSuccessAsync(new List<SearchStatus> { new SearchStatus(jobId, status, total) });
            apiMock.Setup(client => client.GetSearchResultsAsync(jobId, It.IsAny<int>(), It.IsAny<int>())).ReturnsSuccessAsync(new SearchResults(results, status, total));

            configureMock?.Invoke(apiMock);

            _popoverProvider = TestContext.Render<MudPopoverProvider>();
            TestContext.Render<MudSnackbarProvider>();

            var target = TestContext.Render<Search>();

            target.WaitForAssertion(() =>
            {
                var tabs = FindComponentByTestId<MudTabs>(target, "JobTabs");
                tabs.Should().NotBeNull();
            });

            return target;
        }

        private async Task OpenContextMenuAsync(IRenderedComponent<Search> target, SearchResult? item, bool useLongPress = false, bool expectMenuOpen = true)
        {
            var table = target.FindComponent<DynamicTable<SearchResult>>();

            if (useLongPress)
            {
                var longPressArgs = new LongPressEventArgs
                {
                    ClientX = 10,
                    ClientY = 20,
                    OffsetX = 5,
                    OffsetY = 6,
                    PageX = 15,
                    PageY = 16,
                    ScreenX = 25,
                    ScreenY = 26,
                    Type = "contextmenu"
                };
                var args = new TableDataLongPressEventArgs<SearchResult>(longPressArgs, new MudTd(), item);
                await target.InvokeAsync(() => table.Instance.OnTableDataLongPress.InvokeAsync(args));
            }
            else
            {
                var mouseArgs = new MouseEventArgs
                {
                    Button = 2,
                    Buttons = 2,
                    ClientX = 30,
                    ClientY = 40,
                    OffsetX = 2,
                    OffsetY = 3,
                    PageX = 50,
                    PageY = 60,
                    ScreenX = 70,
                    ScreenY = 80,
                    Type = "contextmenu"
                };
                var args = new TableDataContextMenuEventArgs<SearchResult>(mouseArgs, new MudTd(), item);
                await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(args));
            }

            if (!expectMenuOpen)
            {
                return;
            }

            target.WaitForAssertion(() =>
            {
                var menuItems = GetContextMenuItems();
                menuItems.Should().NotBeEmpty();
            });
        }

        private IReadOnlyList<IRenderedComponent<MudMenuItem>> GetContextMenuItems()
        {
            _popoverProvider.Should().NotBeNull();
            return _popoverProvider!.FindComponents<MudMenuItem>();
        }

        private IRenderedComponent<MudMenuItem> FindContextMenuItem(string icon, int iconOccurrence = 0)
        {
            return GetContextMenuItems()
                .Where(item => string.Equals(item.Instance.Icon, icon, StringComparison.Ordinal))
                .Skip(iconOccurrence)
                .First();
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> CapturePollHandler()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var timer = new Mock<IManagedTimer>();
            timer.SetupGet(t => t.State).Returns(ManagedTimerState.Running);
            timer.Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var factory = new Mock<IManagedTimerFactory>();
            factory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(timer.Object);

            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(factory.Object);

            return cancellationToken => handler!.Invoke(cancellationToken);
        }

        private PollHandlerRegistration CapturePollHandlerRegistration()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var subscriptionToken = CancellationToken.None;
            var timer = new Mock<IManagedTimer>();
            timer.SetupGet(t => t.State).Returns(ManagedTimerState.Running);
            timer.Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, token) =>
                {
                    handler = callback;
                    subscriptionToken = token;
                })
                .ReturnsAsync(true);

            var factory = new Mock<IManagedTimerFactory>();
            factory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(timer.Object);

            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(factory.Object);

            return new PollHandlerRegistration(cancellationToken => handler!.Invoke(cancellationToken), () => subscriptionToken);
        }

        private TestNavigationManager UseTestNavigationManager()
        {
            var navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll<NavigationManager>();
            TestContext.Services.AddSingleton<NavigationManager>(navigationManager);
            return navigationManager;
        }

        private sealed class PollHandlerRegistration
        {
            private readonly Func<CancellationToken, Task<ManagedTimerTickResult>> _invokeAsync;
            private readonly Func<CancellationToken> _subscriptionTokenAccessor;

            public PollHandlerRegistration(
                Func<CancellationToken, Task<ManagedTimerTickResult>> invokeAsync,
                Func<CancellationToken> subscriptionTokenAccessor)
            {
                _invokeAsync = invokeAsync;
                _subscriptionTokenAccessor = subscriptionTokenAccessor;
            }

            public CancellationToken SubscriptionToken
            {
                get { return _subscriptionTokenAccessor(); }
            }

            public Task<ManagedTimerTickResult> InvokeAsync(CancellationToken cancellationToken)
            {
                return _invokeAsync(cancellationToken);
            }
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            public bool LastForceLoad { get; private set; }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
