using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net;
using MainUiData = Lantean.QBTMud.Models.MainData;

namespace Lantean.QBTMud.Pages
{
    public partial class Search : IAsyncDisposable
    {
        private const int _resultsBatchSize = 250;
        private const int _pollIntervalMilliseconds = 1500;
        private const string _searchPreferencesStorageKey = "Search.Preferences";
        private const string _searchJobsStorageKey = "Search.Jobs";

        private IReadOnlyList<SearchPlugin>? _plugins;
        private readonly List<SearchJobViewModel> _jobs = [];
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _disposedValue;
        private int _activeTabIndex = -1;
        private bool _searchUnavailable;
        private string? _searchUnavailableReason;
        private SearchResult? _contextMenuResult;
        private SearchPreferences _preferences = new();
        private bool _preferencesLoaded;
        private Dictionary<int, SearchJobMetadata> _jobMetadata = [];
        private IReadOnlyList<(SearchSizeUnit Unit, string Label)>? _sizeUnitOptions;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [CascadingParameter]
        public MainUiData? MainData { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        [CascadingParameter]
        public Orientation CurrentOrientation { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        protected SearchForm Model { get; set; } = new();

        protected bool HasJobs => _jobs.Count > 0;

        protected bool SearchFeatureAvailable => !_searchUnavailable;

        protected IReadOnlyList<SearchPlugin> EnabledPlugins => _plugins?.Where(plugin => plugin.Enabled).ToList() ?? [];

        protected bool HasEnabledPlugins => EnabledPlugins.Count > 0;

        protected bool HasUsablePluginSelection => ComputeHasUsablePluginSelection();

        protected bool CanStartNewSearch => SearchFeatureAvailable && HasUsablePluginSelection && !string.IsNullOrWhiteSpace(Model.SearchText);

        protected bool ShowSearchUnavailableMessage => _searchUnavailable;

        protected bool ShowNoPluginWarning => !_searchUnavailable && !HasEnabledPlugins;

        protected string SearchUnavailableMessage => _searchUnavailableReason ?? "Search is disabled in the connected qBittorrent instance.";

        protected IReadOnlyList<SearchJobViewModel> Jobs => _jobs;

        protected SearchJobViewModel? ActiveJob => _activeTabIndex >= 0 && _activeTabIndex < _jobs.Count ? _jobs[_activeTabIndex] : null;

        protected int ActiveTabIndex
        {
            get => _activeTabIndex;
            set
            {
                if (_jobs.Count == 0)
                {
                    _activeTabIndex = -1;
                    return;
                }

                var nextValue = Math.Clamp(value, 0, _jobs.Count - 1);
                if (_activeTabIndex == nextValue)
                {
                    return;
                }

                _activeTabIndex = nextValue;
                StateHasChanged();
            }
        }

        protected Dictionary<string, string> Categories => GetCategories();

        protected IEnumerable<SearchResult> Results => GetFilteredResults(ActiveJob);

        protected MudMenu? ResultContextMenu { get; set; }

        protected DynamicTable<SearchResult>? Table { get; set; }

        protected bool HasContextResult => _contextMenuResult is not null;

        protected IReadOnlyList<(SearchSizeUnit Unit, string Label)> SizeUnitOptionsList => _sizeUnitOptions ??= BuildSizeUnitOptions();

        protected bool ShowAdvancedFilters { get; set; }

        protected bool ShowSearchForm { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadPreferencesAsync();
            await LoadPluginsAsync();
            await LoadJobMetadataAsync();
            await HydrateJobsFromStatusAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            _refreshTimer ??= ManagedTimerFactory.Create("SearchPolling", TimeSpan.FromMilliseconds(_pollIntervalMilliseconds));
            await _refreshTimer.StartAsync(PollSearchJobsAsync, _timerCancellationToken.Token);
        }

        private async Task HydrateJobsFromStatusAsync()
        {
            if (_searchUnavailable)
            {
                await RemoveStaleMetadataAsync([]);
                return;
            }

            IReadOnlyList<SearchStatus> statuses;
            try
            {
                statuses = await ApiClient.GetSearchesStatus();
            }
            catch (HttpRequestException exception)
            {
                HandleConnectionFailure(exception);
                return;
            }

            if (statuses.Count == 0)
            {
                await RemoveStaleMetadataAsync([]);
                return;
            }

            foreach (var status in statuses)
            {
                if (_jobs.Any(job => job.Id == status.Id))
                {
                    continue;
                }

                var metadata = GetMetadataForJob(status.Id);
                var job = new SearchJobViewModel(status.Id, metadata.Pattern, metadata.Plugins, metadata.Category);
                job.UpdateStatus(status.Status, status.Total);
                TrackJob(job);
                await FetchJobSnapshot(job);
            }

            await RemoveStaleMetadataAsync(statuses.Select(s => s.Id).ToHashSet());
            await EnsurePollingStartedAsync();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task DoSearch(EditContext editContext)
        {
            if (!SearchFeatureAvailable)
            {
                Snackbar.Add(SearchUnavailableMessage, Severity.Warning);
                return;
            }

            if (!HasUsablePluginSelection)
            {
                Snackbar.Add("Enable the selected plugin before searching.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Model.SearchText))
            {
                Snackbar.Add("Enter search criteria to start a job.", Severity.Warning);
                return;
            }

            var normalizedPattern = Model.SearchText.Trim();
            var pluginSelection = GetPluginSelection();
            var existingJob = FindMatchingJob(normalizedPattern, Model.SelectedCategory, pluginSelection);

            if (existingJob is not null)
            {
                ActiveTabIndex = _jobs.IndexOf(existingJob);
                return;
            }

            try
            {
                await StartOrReplaceJob(normalizedPattern, pluginSelection, Model.SelectedCategory, existingJob: null);
                if (ShowAdvancedFilters)
                {
                    ShowAdvancedFilters = false;
                }
                if ((CurrentOrientation == Orientation.Portrait && CurrentBreakpoint <= Breakpoint.Sm) || (CurrentOrientation == Orientation.Landscape && CurrentBreakpoint <= Breakpoint.Md))
                {
                    ShowSearchForm = false;
                }
            }
            catch (HttpRequestException exception)
            {
                Snackbar.Add($"Failed to start search: {exception.Message}", Severity.Error);
            }
        }

        protected async Task ManagePlugins()
        {
            var updated = await DialogWorkflow.ShowSearchPluginsDialog();
            if (!updated)
            {
                return;
            }

            await LoadPluginsAsync(showError: true);
            await SavePreferencesAsync();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task StopJob(SearchJobViewModel job)
        {
            try
            {
                await ApiClient.StopSearch(job.Id);
                job.UpdateStatus("Stopped", job.Total);
                StateHasChanged();
                await StopPollingIfAllJobsCompletedAsync();
            }
            catch (HttpRequestException exception)
            {
                Snackbar.Add($"Failed to stop \"{job.Pattern}\": {exception.Message}", Severity.Error);
            }
        }

        protected async Task RefreshJob(SearchJobViewModel job)
        {
            job.ResetResults();
            await FetchJobSnapshot(job);
            StateHasChanged();
            await EnsurePollingStartedAsync();
        }

        protected async Task CloseJob(SearchJobViewModel job)
        {
            await StopAndDeleteJob(job);
            await RemoveJobAsync(job);
            await StopPollingIfAllJobsCompletedAsync();
        }

        protected async Task CloseAllJobs()
        {
            var jobs = _jobs.ToList();
            foreach (var job in jobs)
            {
                await CloseJob(job);
            }

            await StopPollingIfAllJobsCompletedAsync();
        }

        private IReadOnlyList<ColumnDefinition<SearchResult>>? _columns;

        protected IEnumerable<ColumnDefinition<SearchResult>> Columns => _columns ??= BuildColumns();

        private IReadOnlyList<ColumnDefinition<SearchResult>> BuildColumns()
        {
            var nameLabel = WebUiLocalizer.Translate("SearchResultsTable", "Name");
            var sizeLabel = WebUiLocalizer.Translate("SearchResultsTable", "Size");
            var seedersLabel = WebUiLocalizer.Translate("SearchResultsTable", "Seeders");
            var leechersLabel = WebUiLocalizer.Translate("SearchResultsTable", "Leechers");
            var engineLabel = WebUiLocalizer.Translate("SearchResultsTable", "Engine");
            var engineUrlLabel = WebUiLocalizer.Translate("SearchResultsTable", "Engine URL");
            var publishedLabel = WebUiLocalizer.Translate("SearchResultsTable", "Published On");

            return new List<ColumnDefinition<SearchResult>>
            {
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>(nameLabel, r => r.FileName ?? string.Empty, NameColumnTemplate, width: 320),
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>(sizeLabel, r => r.FileSize, r => DisplayHelpers.Size(r.FileSize), width: 120),
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>(seedersLabel, r => r.Seeders, width: 90),
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>(leechersLabel, r => r.Leechers, width: 90),
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>(engineLabel, r => r.EngineName ?? string.Empty, width: 150),
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>(engineUrlLabel, r => r.SiteUrl ?? string.Empty, SiteColumnTemplate, width: 220),
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>(publishedLabel, r => r.PublishedOn ?? 0, PublishedColumnTemplate, width: 150),
                ColumnDefinitionHelper.CreateColumnDefinition<SearchResult>("Actions", r => r.FileUrl ?? string.Empty, ActionColumnTemplate, width: 120, tdClass: "no-wrap")
            }.AsReadOnly();
        }

        private static RenderFragment<RowContext<SearchResult>> NameColumnTemplate => context => builder =>
        {
            var result = context.Data;
            var fileName = string.IsNullOrWhiteSpace(result.FileName) ? "-" : result.FileName;
            if (!string.IsNullOrWhiteSpace(result.DescriptionLink))
            {
                builder.OpenComponent<MudLink>(0);
                builder.AddAttribute(1, nameof(MudLink.Href), result.DescriptionLink);
                builder.AddAttribute(2, nameof(MudLink.Target), result.DescriptionLink);
                builder.AddAttribute(3, nameof(MudLink.ChildContent), (RenderFragment)(childBuilder =>
                {
                    childBuilder.AddContent(0, fileName);
                }));
                builder.CloseComponent();
            }
            else
            {
                builder.AddContent(4, fileName);
            }
        };

        private static RenderFragment<RowContext<SearchResult>> SiteColumnTemplate => context => builder =>
        {
            var url = context.Data.SiteUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                builder.AddContent(0, "-");
                return;
            }

            builder.OpenComponent<MudLink>(0);
            builder.AddAttribute(1, nameof(MudLink.Href), url);
            builder.AddAttribute(2, nameof(MudLink.Target), url);
            builder.AddAttribute(3, nameof(MudLink.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.AddContent(0, url);
            }));
            builder.CloseComponent();
        };

        private static RenderFragment<RowContext<SearchResult>> PublishedColumnTemplate => context => builder =>
        {
            builder.AddContent(0, DisplayHelpers.DateTime(context.Data.PublishedOn));
        };

        private RenderFragment<RowContext<SearchResult>> ActionColumnTemplate => context => builder =>
        {
            var disabled = string.IsNullOrWhiteSpace(context.Data.FileUrl);
            builder.OpenComponent<MudIconButton>(0);
            builder.AddAttribute(1, nameof(MudIconButton.Icon), Icons.Material.Filled.Download);
            builder.AddAttribute(2, nameof(MudIconButton.Color), Color.Primary);
            builder.AddAttribute(3, nameof(MudIconButton.Disabled), disabled);
            builder.AddAttribute(4, nameof(MudIconButton.Size), Size.Small);
            builder.AddAttribute(5, nameof(MudIconButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await DownloadResult(context.Data)));
            builder.CloseComponent();
        };

        protected static string GetJobStatusIcon(SearchJobViewModel job)
        {
            return job.Status switch
            {
                "Running" => Icons.Material.Filled.Sync,
                "Completed" => Icons.Material.Filled.CheckCircle,
                "Finished" => Icons.Material.Filled.CheckCircle,
                "Stopped" => Icons.Material.Filled.Stop,
                "Aborted" => Icons.Material.Filled.Error,
                "Error" => Icons.Material.Filled.Error,
                _ => Icons.Material.Filled.Task,
            };
        }

        protected static Color GetJobStatusColor(SearchJobViewModel job)
        {
            return job.Status switch
            {
                "Running" => Color.Info,
                "Completed" => Color.Success,
                "Finished" => Color.Success,
                "Stopped" => Color.Warning,
                "Aborted" => Color.Error,
                "Error" => Color.Error,
                _ => Color.Default,
            };
        }

        protected string GetJobResultSummary(SearchJobViewModel job)
        {
            var options = BuildFilterOptions();
            var visible = SearchFilterHelper.CountVisible(job.Results, options);
            var total = job.Total > 0 ? job.Total : job.Results.Count;

            if (total > 0)
            {
                return $"{visible}/{total}";
            }

            return $"{visible} {WebUiLocalizer.Translate("SearchEngineWidget", "Results")}";
        }

        protected Task StopActiveJob()
        {
            return ActiveJob is null ? Task.CompletedTask : StopJob(ActiveJob);
        }

        protected Task RefreshActiveJob()
        {
            return ActiveJob is null ? Task.CompletedTask : RefreshJob(ActiveJob);
        }

        protected Task CloseActiveJob()
        {
            return ActiveJob is null ? Task.CompletedTask : CloseJob(ActiveJob);
        }

        protected void ToggleAdvancedFilters()
        {
            ShowAdvancedFilters = !ShowAdvancedFilters;
        }

        protected void ToggleSearchForm()
        {
            ShowSearchForm = !ShowSearchForm;
        }

        private async Task LoadPluginsAsync(bool showError = false)
        {
            try
            {
                _plugins = await ApiClient.GetSearchPlugins();
                _searchUnavailable = false;
                _searchUnavailableReason = null;
                EnsureSelectedPluginsValid();
                EnsureValidCategory();
            }
            catch (HttpRequestException exception)
            {
                _plugins = [];
                _searchUnavailable = true;
                _searchUnavailableReason = "Search is disabled in the connected qBittorrent instance.";
                EnsureSelectedPluginsValid();
                EnsureValidCategory();
                if (showError)
                {
                    Snackbar.Add($"Unable to load search plugins: {exception.Message}", Severity.Warning);
                }
            }
        }

        private Dictionary<string, string> GetCategories()
        {
            if (_plugins is null || _plugins.Count == 0)
            {
                return new Dictionary<string, string>
                {
                    [SearchForm.AllCategoryId] = WebUiLocalizer.Translate("SearchEngineWidget", "All categories")
                };
            }

            var comparer = StringComparer.OrdinalIgnoreCase;
            var selected = new HashSet<string>(Model.SelectedPlugins, comparer);
            IEnumerable<SearchPlugin> targetPlugins;

            if (selected.Count == 0)
            {
                targetPlugins = HasEnabledPlugins ? EnabledPlugins : _plugins;
            }
            else
            {
                targetPlugins = _plugins.Where(plugin => selected.Contains(plugin.Name)).ToList();
                if (!targetPlugins.Any())
                {
                    targetPlugins = HasEnabledPlugins ? EnabledPlugins : _plugins;
                }
            }

            var categories = targetPlugins
                .SelectMany(plugin => plugin.SupportedCategories)
                .DistinctBy(category => category.Id)
                .ToDictionary(category => category.Id, category => category.Name);

            categories[SearchForm.AllCategoryId] = WebUiLocalizer.Translate("SearchEngineWidget", "All categories");

            return categories;
        }

        private IReadOnlyCollection<string> GetPluginSelection()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;

            if (Model.SelectedPlugins.Count == 0)
            {
                return HasEnabledPlugins
                    ? EnabledPlugins.Select(plugin => plugin.Name).OrderBy(name => name, comparer).ToArray()
                    : [];
            }

            return Model.SelectedPlugins.OrderBy(value => value, comparer).ToArray();
        }

        private void TrackJob(SearchJobViewModel job, int? replaceIndex = null)
        {
            if (replaceIndex is int index && index >= 0 && index < _jobs.Count)
            {
                _jobs[index] = job;
                ActiveTabIndex = index;
            }
            else
            {
                _jobs.Add(job);
                ActiveTabIndex = _jobs.Count - 1;
            }
        }

        private async Task RemoveJobAsync(SearchJobViewModel job)
        {
            var index = _jobs.IndexOf(job);
            if (index >= 0)
            {
                _jobs.RemoveAt(index);
            }

            if (_jobs.Count == 0)
            {
                _activeTabIndex = -1;
            }
            else if (_activeTabIndex >= _jobs.Count)
            {
                _activeTabIndex = _jobs.Count - 1;
            }

            StateHasChanged();
            await StopPollingIfAllJobsCompletedAsync();
        }

        private Task EnsurePollingStartedAsync()
        {
            return Task.CompletedTask;
        }

        private Task StopPollingIfAllJobsCompletedAsync()
        {
            return Task.CompletedTask;
        }

        private async Task<ManagedTimerTickResult> PollSearchJobsAsync(CancellationToken cancellationToken)
        {
            if (!_jobs.Any(job => job.IsRunning))
            {
                return ManagedTimerTickResult.Continue;
            }

            try
            {
                await InvokeAsync(async () => await SynchronizeJobsAsync(cancellationToken));
            }
            catch (OperationCanceledException)
            {
                return ManagedTimerTickResult.Stop;
            }
            catch (Exception exception)
            {
                await HandlePollingFailureAsync(exception);
                return ManagedTimerTickResult.Stop;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            return ManagedTimerTickResult.Continue;
        }

        private async Task SynchronizeJobsAsync(CancellationToken cancellationToken)
        {
            if (_jobs.Count == 0)
            {
                return;
            }

            IReadOnlyList<SearchStatus> statuses;
            try
            {
                statuses = await ApiClient.GetSearchesStatus();
            }
            catch (HttpRequestException exception)
            {
                HandleConnectionFailure(exception);
                return;
            }
            catch (Exception exception)
            {
                await HandlePollingFailureAsync(exception);
                return;
            }

            var statusLookup = statuses.ToDictionary(s => s.Id);

            foreach (var job in _jobs.ToList())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (statusLookup.TryGetValue(job.Id, out var status))
                {
                    job.UpdateStatus(status.Status, status.Total);
                }

                if (!ShouldFetchResults(job))
                {
                    continue;
                }

                await FetchJobResultsAsync(job);
            }

            StateHasChanged();
            await StopPollingIfAllJobsCompletedAsync();
        }

        private static bool ShouldFetchResults(SearchJobViewModel job)
        {
            if (job.IsRunning)
            {
                return true;
            }

            if (job.Total == 0)
            {
                return true;
            }

            return job.CurrentOffset < job.Total;
        }

        private async Task FetchJobSnapshot(SearchJobViewModel job)
        {
            await FetchJobResultsAsync(job);
        }

        private async Task FetchJobResultsAsync(SearchJobViewModel job)
        {
            try
            {
                var results = await ApiClient.GetSearchResults(job.Id, _resultsBatchSize, job.CurrentOffset);
                if (results.Results.Count > 0)
                {
                    job.AppendResults(results.Results);
                }

                job.UpdateStatus(results.Status, results.Total);
                if (!job.IsRunning)
                {
                    await StopPollingIfAllJobsCompletedAsync();
                }
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                job.UpdateStatus("Stopped", job.Total);
                await StopPollingIfAllJobsCompletedAsync();
            }
            catch (HttpRequestException exception)
            {
                job.SetError("Failed to load results.");
                Snackbar.Add($"Failed to load results for \"{job.Pattern}\": {exception.Message}", Severity.Error);
                await StopPollingIfAllJobsCompletedAsync();
            }
            catch (Exception exception)
            {
                await HandlePollingFailureAsync(exception);
            }
        }

        private SearchFilterOptions BuildFilterOptions()
        {
            return new SearchFilterOptions(
                string.IsNullOrWhiteSpace(Model.FilterText) ? null : Model.FilterText.Trim(),
                Model.SearchIn,
                Model.MinimumSeeds,
                Model.MaximumSeeds,
                Model.MinimumSize,
                Model.MinimumSizeUnit,
                Model.MaximumSize,
                Model.MaximumSizeUnit);
        }

        private IReadOnlyList<SearchResult> GetFilteredResults(SearchJobViewModel? job)
        {
            if (job is null || job.Results.Count == 0)
            {
                return [];
            }

            var options = BuildFilterOptions();
            return SearchFilterHelper.ApplyFilters(job.Results, options);
        }

        private SearchJobViewModel? FindMatchingJob(string pattern, string category, IReadOnlyCollection<string> plugins)
        {
            return _jobs.FirstOrDefault(job => job.Matches(pattern, category, plugins));
        }

        private async Task StartOrReplaceJob(string pattern, IReadOnlyCollection<string> plugins, string category, SearchJobViewModel? existingJob)
        {
            int? replaceIndex = null;
            if (existingJob is not null)
            {
                replaceIndex = _jobs.IndexOf(existingJob);
                await StopAndDeleteJob(existingJob);
            }

            var searchId = await ApiClient.StartSearch(pattern, plugins, category);
            var job = new SearchJobViewModel(searchId, pattern, plugins, category);

            TrackJob(job, replaceIndex);
            await PersistJobMetadataAsync(job);
            await EnsurePollingStartedAsync();
            await FetchJobSnapshot(job);
        }

        private async Task StopAndDeleteJob(SearchJobViewModel job)
        {
            try
            {
                if (job.IsRunning)
                {
                    await ApiClient.StopSearch(job.Id);
                }
            }
            catch (HttpRequestException)
            {
                // Ignore failures while stopping.
            }

            try
            {
                await ApiClient.DeleteSearch(job.Id);
            }
            catch (HttpRequestException)
            {
                // Ignore failures when the job is already deleted.
            }

            await RemoveJobMetadataAsync(job.Id);
        }

        private async Task LoadPreferencesAsync()
        {
            try
            {
                var stored = await LocalStorage.GetItemAsync<SearchPreferences>(_searchPreferencesStorageKey);
                _preferences = stored ?? new SearchPreferences();
            }
            catch
            {
                _preferences = new SearchPreferences();
            }

            NormalizePreferenceSets(_preferences);
            ApplyPreferencesToModel(_preferences);
            _preferencesLoaded = true;
        }

        private async Task LoadJobMetadataAsync()
        {
            try
            {
                var stored = await LocalStorage.GetItemAsync<List<SearchJobMetadata>>(_searchJobsStorageKey);
                _jobMetadata = stored?.ToDictionary(job => job.Id) ?? new Dictionary<int, SearchJobMetadata>();
            }
            catch
            {
                _jobMetadata = new Dictionary<int, SearchJobMetadata>();
            }
        }

        private static void NormalizePreferenceSets(SearchPreferences preferences)
        {
            preferences.SelectedPlugins = new HashSet<string>(preferences.SelectedPlugins ?? [], StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(preferences.SelectedCategory))
            {
                preferences.SelectedCategory = SearchForm.AllCategoryId;
            }
        }

        private void ApplyPreferencesToModel(SearchPreferences preferences)
        {
            Model.SelectedCategory = preferences.SelectedCategory;
            Model.SelectedPlugins = new HashSet<string>(preferences.SelectedPlugins, StringComparer.OrdinalIgnoreCase);
            Model.FilterText = preferences.FilterText;
            Model.SearchIn = preferences.SearchIn;
            Model.MinimumSeeds = preferences.MinimumSeeds;
            Model.MaximumSeeds = preferences.MaximumSeeds;
            Model.MinimumSize = preferences.MinimumSize;
            Model.MaximumSize = preferences.MaximumSize;
            Model.MinimumSizeUnit = preferences.MinimumSizeUnit;
            Model.MaximumSizeUnit = preferences.MaximumSizeUnit;

            EnsureSelectedPluginsValid();
            EnsureValidCategory();

            ShowAdvancedFilters = !string.IsNullOrWhiteSpace(Model.FilterText)
                || Model.SearchIn != SearchInScope.Everywhere
                || Model.MinimumSeeds.HasValue
                || Model.MaximumSeeds.HasValue
                || Model.MinimumSize.HasValue
                || Model.MaximumSize.HasValue;
        }

        private async Task SavePreferencesAsync()
        {
            if (!_preferencesLoaded)
            {
                return;
            }

            _preferences.SelectedCategory = Model.SelectedCategory;
            _preferences.SelectedPlugins = new HashSet<string>(Model.SelectedPlugins, StringComparer.OrdinalIgnoreCase);
            _preferences.FilterText = Model.FilterText;
            _preferences.SearchIn = Model.SearchIn;
            _preferences.MinimumSeeds = Model.MinimumSeeds;
            _preferences.MaximumSeeds = Model.MaximumSeeds;
            _preferences.MinimumSize = Model.MinimumSize;
            _preferences.MaximumSize = Model.MaximumSize;
            _preferences.MinimumSizeUnit = Model.MinimumSizeUnit;
            _preferences.MaximumSizeUnit = Model.MaximumSizeUnit;

            await LocalStorage.SetItemAsync(_searchPreferencesStorageKey, _preferences);
        }

        private ValueTask SaveJobMetadataAsync()
        {
            return LocalStorage.SetItemAsync(_searchJobsStorageKey, _jobMetadata.Values.ToList());
        }

        private async Task OnPluginsChanged(IEnumerable<string> values)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            Model.SelectedPlugins = values?.ToHashSet(comparer) ?? new HashSet<string>(comparer);
            EnsureSelectedPluginsValid();
            EnsureValidCategory();
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private string? PluginsSelectionText(List<string>? selectedPlugins)
        {
            if (selectedPlugins is null)
            {
                return null;
            }
            if (selectedPlugins.Count == EnabledPlugins.Count)
            {
                return "All enabled plugins";
            }
            return string.Join(", ", selectedPlugins);
        }

        private IReadOnlyList<(SearchSizeUnit Unit, string Label)> BuildSizeUnitOptions()
        {
            return new List<(SearchSizeUnit Unit, string Label)>
            {
                (SearchSizeUnit.Bytes, WebUiLocalizer.Translate("misc", "B")),
                (SearchSizeUnit.Kibibytes, WebUiLocalizer.Translate("misc", "KiB")),
                (SearchSizeUnit.Mebibytes, WebUiLocalizer.Translate("misc", "MiB")),
                (SearchSizeUnit.Gibibytes, WebUiLocalizer.Translate("misc", "GiB")),
                (SearchSizeUnit.Tebibytes, WebUiLocalizer.Translate("misc", "TiB")),
                (SearchSizeUnit.Pebibytes, WebUiLocalizer.Translate("misc", "PiB")),
                (SearchSizeUnit.Exbibytes, WebUiLocalizer.Translate("misc", "EiB"))
            }.AsReadOnly();
        }

        private async Task OnCategoryChanged(string value)
        {
            Model.SelectedCategory = value;
            EnsureValidCategory();
            await SavePreferencesAsync();
        }

        private async Task OnFilterTextChanged(string? value)
        {
            Model.FilterText = string.IsNullOrWhiteSpace(value) ? null : value;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private async Task OnSearchInChanged(SearchInScope scope)
        {
            Model.SearchIn = scope;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private async Task OnMinimumSeedsChanged(int? value)
        {
            Model.MinimumSeeds = value;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private async Task OnMaximumSeedsChanged(int? value)
        {
            Model.MaximumSeeds = value;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private async Task OnMinimumSizeChanged(double? value)
        {
            Model.MinimumSize = value;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private async Task OnMaximumSizeChanged(double? value)
        {
            Model.MaximumSize = value;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private async Task OnMinimumSizeUnitChanged(SearchSizeUnit unit)
        {
            Model.MinimumSizeUnit = unit;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private async Task OnMaximumSizeUnitChanged(SearchSizeUnit unit)
        {
            Model.MaximumSizeUnit = unit;
            await SavePreferencesAsync();
            StateHasChanged();
        }

        private SearchJobMetadata GetMetadataForJob(int jobId)
        {
            if (_jobMetadata.TryGetValue(jobId, out var metadata))
            {
                return metadata;
            }

            return new SearchJobMetadata
            {
                Id = jobId,
                Pattern = $"Job #{jobId}",
                Category = SearchForm.AllCategoryId,
                Plugins = []
            };
        }

        private async Task PersistJobMetadataAsync(SearchJobViewModel job)
        {
            _jobMetadata[job.Id] = new SearchJobMetadata
            {
                Id = job.Id,
                Pattern = job.Pattern,
                Category = job.Category,
                Plugins = job.Plugins.ToList()
            };

            await SaveJobMetadataAsync();
        }

        private async Task RemoveJobMetadataAsync(int jobId)
        {
            if (_jobMetadata.Remove(jobId))
            {
                await SaveJobMetadataAsync();
            }
        }

        private async Task RemoveStaleMetadataAsync(HashSet<int> activeJobIds)
        {
            var hasChanges = false;
            foreach (var jobId in _jobMetadata.Keys)
            {
                if (!activeJobIds.Contains(jobId))
                {
                    _jobMetadata.Remove(jobId);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await SaveJobMetadataAsync();
            }
        }

        private void EnsureSelectedPluginsValid()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var enabledNames = EnabledPlugins.Select(plugin => plugin.Name).ToHashSet(comparer);

            Model.SelectedPlugins ??= new HashSet<string>(comparer);

            if (enabledNames.Count == 0)
            {
                Model.SelectedPlugins.Clear();
                return;
            }

            Model.SelectedPlugins.RemoveWhere(name => !enabledNames.Contains(name));

            if (Model.SelectedPlugins.Count == 0)
            {
                Model.SelectedPlugins = new HashSet<string>(enabledNames, comparer);
            }
        }

        private void EnsureValidCategory()
        {
            var categories = GetCategories();
            if (!categories.ContainsKey(Model.SelectedCategory))
            {
                Model.SelectedCategory = SearchForm.AllCategoryId;
            }
        }

        private bool ComputeHasUsablePluginSelection()
        {
            if (!HasEnabledPlugins)
            {
                return false;
            }

            return GetPluginSelection().Count > 0;
        }

        private async Task DownloadResult(SearchResult result)
        {
            if (string.IsNullOrWhiteSpace(result?.FileUrl))
            {
                return;
            }

            await DialogWorkflow.InvokeAddTorrentLinkDialog(result.FileUrl);
        }

        protected async Task HandleResultContextMenu(TableDataContextMenuEventArgs<SearchResult> eventArgs)
        {
            if (eventArgs.Item is null || ResultContextMenu is null)
            {
                return;
            }

            _contextMenuResult = eventArgs.Item;
            var normalizedArgs = eventArgs.MouseEventArgs.NormalizeForContextMenu();
            await ResultContextMenu.OpenMenuAsync(normalizedArgs);
        }

        protected async Task HandleResultLongPress(TableDataLongPressEventArgs<SearchResult> eventArgs)
        {
            if (eventArgs.Item is null || ResultContextMenu is null)
            {
                return;
            }

            _contextMenuResult = eventArgs.Item;
            var normalizedArgs = eventArgs.LongPressEventArgs.ToMouseEventArgs();
            await ResultContextMenu.OpenMenuAsync(normalizedArgs);
        }

        protected Task DownloadResultFromContext()
        {
            if (_contextMenuResult is null)
            {
                return Task.CompletedTask;
            }

            return DownloadResult(_contextMenuResult);
        }

        protected Task OpenDescriptionFromContext()
        {
            return OpenLinkInNewTab(_contextMenuResult?.DescriptionLink);
        }

        protected async Task CopyNameFromContext()
        {
            if (string.IsNullOrWhiteSpace(_contextMenuResult?.FileName))
            {
                return;
            }

            await ClipboardService.WriteToClipboard(_contextMenuResult.FileName);
            Snackbar.Add("Name copied to clipboard.", Severity.Success);
        }

        protected async Task CopyDownloadLinkFromContext()
        {
            if (string.IsNullOrWhiteSpace(_contextMenuResult?.FileUrl))
            {
                return;
            }

            await ClipboardService.WriteToClipboard(_contextMenuResult.FileUrl);
            Snackbar.Add("Download link copied to clipboard.", Severity.Success);
        }

        protected async Task CopyDescriptionLinkFromContext()
        {
            if (string.IsNullOrWhiteSpace(_contextMenuResult?.DescriptionLink))
            {
                return;
            }

            await ClipboardService.WriteToClipboard(_contextMenuResult.DescriptionLink);
            Snackbar.Add("Description link copied to clipboard.", Severity.Success);
        }

        private Task OpenLinkInNewTab(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return Task.CompletedTask;
            }

            return JSRuntime.InvokeVoidAsync("open", url, url).AsTask();
        }

        private void HandleConnectionFailure(HttpRequestException exception)
        {
            if (MainData is not null)
            {
                MainData.LostConnection = true;
            }

            foreach (var job in _jobs)
            {
                job.SetError("Connection lost.");
            }
        }

        private Task HandlePollingFailureAsync(Exception? exception)
        {
            foreach (var job in _jobs)
            {
                job.SetError("Search polling failed.");
            }

            if (exception is not null)
            {
                Snackbar.Add($"Search polling stopped: {exception.GetBaseException().Message}", Severity.Error);
            }

            return Task.CompletedTask;
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                await _timerCancellationToken.CancelAsync();
                _timerCancellationToken.Dispose();
                if (_refreshTimer is not null)
                {
                    await _refreshTimer.DisposeAsync();
                    _refreshTimer = null;
                }
            }

            _disposedValue = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
