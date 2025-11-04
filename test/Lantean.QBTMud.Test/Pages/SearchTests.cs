using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Test.Infrastructure;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class SearchTests : RazorComponentTestBase<Search>
    {
        private const string PreferencesStorageKey = "Search.Preferences";
        private const string JobsStorageKey = "Search.Jobs";

        [Fact]
        public void GIVEN_NoStoredPreferences_WHEN_Render_THEN_DefaultsApplied()
        {
            var apiMock = TestContext.UseApiClientMock();
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            apiMock.Setup(client => client.GetSearchPlugins()).ReturnsAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatus()).ReturnsAsync(Array.Empty<SearchStatus>());

            TestContext.RenderComponent<MudPopoverProvider>();
            TestContext.RenderComponent<MudSnackbarProvider>();

            var target = TestContext.RenderComponent<Search>();

            var criteriaField = FindComponentByTestId<MudTextField<string>>(target, "Criteria");
            criteriaField.Instance.Value.Should().BeNull();

            var categorySelect = FindComponentByTestId<MudSelect<string>>(target, "category-select");
            categorySelect.Instance.Value.Should().Be(SearchForm.AllCategoryId);

            var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "plugin-select");
            pluginSelect.Instance.SelectedValues.Should().Contain("movies");

            var toggleAdvancedButton = FindComponentByTestId<MudButton>(target, "toggle-advanced-filters");
            toggleAdvancedButton.Markup.Should().Contain("Show filters");

            var startButton = FindComponentByTestId<MudButton>(target, "start-search-button");
            startButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_PersistedPreferences_WHEN_Render_THEN_AdvancedFiltersExpanded()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(PreferencesStorageKey, new SearchPreferences
            {
                SelectedCategory = "movies",
                SelectedPlugins = new HashSet<string>(new[] { "movies" }, StringComparer.OrdinalIgnoreCase),
                FilterText = "1080p",
                SearchIn = SearchInScope.Names,
                MinimumSeeds = 5
            });

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPlugins()).ReturnsAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatus()).ReturnsAsync(Array.Empty<SearchStatus>());

            TestContext.RenderComponent<MudPopoverProvider>();
            TestContext.RenderComponent<MudSnackbarProvider>();

            var target = TestContext.RenderComponent<Search>();

            target.WaitForState(() =>
            {
                return FindComponentByTestId<MudCollapse>(target, "advanced-filters-collapse").Instance.Expanded;
            }, TimeSpan.FromSeconds(2));

            var advancedFiltersCollapse = FindComponentByTestId<MudCollapse>(target, "advanced-filters-collapse");
            advancedFiltersCollapse.Instance.Expanded.Should().BeTrue();

            var searchInSelect = FindComponentByTestId<MudSelect<SearchInScope>>(target, "search-in-select");
            searchInSelect.Instance.Value.Should().Be(SearchInScope.Names);

            var pluginSelect = FindComponentByTestId<MudSelect<string>>(target, "plugin-select");
            pluginSelect.Instance.SelectedValues.Should().Contain("movies");
        }

        [Fact]
        public async Task GIVEN_PersistedJobs_WHEN_Render_THEN_JobSummaryDisplayed()
        {
            var plugin = new SearchPlugin(true, "Movies", "movies", new[] { new SearchCategory("movies", "Movies") }, "http://plugins/movies", "1.0");
            await TestContext.LocalStorage.SetItemAsync(PreferencesStorageKey, new SearchPreferences());
            await TestContext.LocalStorage.SetItemAsync(JobsStorageKey, new List<SearchJobMetadata>
            {
                new SearchJobMetadata
                {
                    Id = 11,
                    Pattern = "Ubuntu",
                    Category = "movies",
                    Plugins = new List<string> { "movies" }
                }
            });

            var apiMock = TestContext.UseApiClientMock();
            apiMock.Setup(client => client.GetSearchPlugins()).ReturnsAsync(new List<SearchPlugin> { plugin });
            apiMock.Setup(client => client.GetSearchesStatus()).ReturnsAsync(new List<SearchStatus>
            {
                new SearchStatus(11, "Stopped", 2)
            });
            apiMock.Setup(client => client.GetSearchResults(11, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new SearchResults(new List<SearchResult>
            {
                new SearchResult("http://desc", "Ubuntu 24.04", 1_500_000_000, "http://files/ubuntu", 10, 200, "http://site", "movies", 1_700_000_000)
            }, "Stopped", 2));

            TestContext.RenderComponent<MudPopoverProvider>();
            TestContext.RenderComponent<MudSnackbarProvider>();

            var target = TestContext.RenderComponent<Search>();

            target.WaitForState(() =>
            {
                return FindComponentByTestId<MudText>(target, "job-summary").Markup.Contains("Results: 1/2");
            }, TimeSpan.FromSeconds(2));

            var jobTabSummary = FindComponentByTestId<MudText>(target, "job-summary");
            jobTabSummary.Markup.Should().Contain("Results: 1/2");

            var statusChip = FindComponentByTestId<MudChip<string>>(target, "job-status-chip");
            statusChip.Markup.Should().Contain("Stopped");

            var resultsTable = target.FindComponent<DynamicTable<SearchResult>>();
            resultsTable.Markup.Should().Contain("Ubuntu 24.04");
        }
    }
}
