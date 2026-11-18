using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components
{
    public partial class TrackersTab : IAsyncDisposable
    {
        private const string _trackerListContext = "TrackerListWidget";
        private const string _trackersAdditionContext = "TrackersAdditionDialog";
        private const string _appContext = "AppTrackersTab";

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _disposedValue;
        private bool _initializingSelection;
        private string? _reportedHash;
        private string? _requestedHash;
        private int _selectionVersion;

        private string? _sortColumn;
        private SortDirection _sortDirection;
        private IReadOnlyList<ColumnDefinition<TorrentTracker>>? _columnDefinitions;

        private const string _toolbar = nameof(_toolbar);
        private const string _context = nameof(_context);

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [Parameter]
        public EventCallback<string> DisplayedHashChanged { get; set; }

        protected string? DisplayedHash => _reportedHash ?? Hash;

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        protected IReadOnlyList<TorrentTracker>? TrackerList { get; set; }

        protected IEnumerable<TorrentTracker>? Trackers => GetTrackers();

        protected TorrentTracker? ContextMenuItem { get; set; }

        protected TorrentTracker? SelectedItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        protected DynamicTable<TorrentTracker>? Table { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (_disposedValue || Hash is null)
            {
                return;
            }

            if (!Active)
            {
                return;
            }

            if (DisplayedHashChanged.HasDelegate && _requestedHash == Hash)
            {
                return;
            }

            var hash = Hash;
            var version = ++_selectionVersion;
            _requestedHash = hash;
            _initializingSelection = true;
            try
            {
                var trackersResult = await ApiClient.GetTorrentTrackersAsync(hash);
                if (_disposedValue || hash != Hash || version != _selectionVersion)
                {
                    return;
                }
                if (trackersResult.IsFailure)
                {
                    if (_reportedHash is null)
                    {
                        TrackerList = [];
                    }
                    await ApiFeedbackWorkflow.HandleFailureAsync(trackersResult);
                }
                else
                {
                    TrackerList = trackersResult.Value;
                    if (_reportedHash != hash)
                    {
                        SelectedItem = null;
                        ContextMenuItem = null;
                    }
                    _reportedHash = hash;
                    await DisplayedHashChanged.InvokeAsync(hash);
                }

                await InvokeAsync(StateHasChanged);
            }
            finally
            {
                if (version == _selectionVersion)
                {
                    _initializingSelection = false;
                    if (_reportedHash != hash)
                    {
                        _requestedHash = null;
                    }
                }
            }
        }

        protected IEnumerable<TorrentTracker>? GetTrackers()
        {
            if (TrackerList is null)
            {
                return null;
            }

            var trackers = TrackerList.Where(t => !IsRealTracker(t)).ToList();
            trackers.AddRange(TrackerList.Where(IsRealTracker).OrderByDirection(_sortDirection, GetSortSelector()));

            return trackers.AsReadOnly();
        }

        private static bool IsRealTracker(TorrentTracker torrentTracker)
        {
            return !torrentTracker.Url.StartsWith("**");
        }

        private Func<TorrentTracker, object?> GetSortSelector()
        {
            var sortSelector = GetColumnDefinitions().FirstOrDefault(c => c.Id == _sortColumn)?.SortSelector;

            return sortSelector ?? (i => i.Url);
        }

        protected void SortDirectionChanged(SortDirection sortDirection)
        {
            _sortDirection = sortDirection;

            StateHasChanged();
        }

        protected void SortColumnChanged(string column)
        {
            _sortColumn = column;

            StateHasChanged();
        }

        protected async Task ColumnOptions()
        {
            if (Table is null)
            {
                return;
            }

            await Table.ShowColumnOptionsDialog();
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<TorrentTracker> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<TorrentTracker> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        protected async Task ShowContextMenu(TorrentTracker? tracker, EventArgs eventArgs)
        {
            if (tracker is not null && IsRealTracker(tracker))
            {
                ContextMenuItem = tracker;
            }
            else
            {
                ContextMenuItem = null;
            }

            if (ContextMenu is null)
            {
                return;
            }

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu.OpenMenuAsync(normalizedEventArgs);
        }

        protected void SelectedItemChanged(TorrentTracker torrentTracker)
        {
            SelectedItem = torrentTracker;
        }

        protected async Task AddTracker()
        {
            var hash = DisplayedHash;
            if (hash is null)
            {
                return;
            }

            var trackers = await DialogWorkflow.ShowAddTrackersDialog();
            if (trackers is null || trackers.Count == 0)
            {
                return;
            }

            var addResult = await ApiClient.AddTrackersToTorrentAsync(TorrentSelector.FromHash(hash), trackers);
            await ApiFeedbackWorkflow.ProcessResultAsync(addResult);
        }

        protected Task EditTrackerToolbar()
        {
            return EditTracker(SelectedItem);
        }

        protected Task EditTrackerContextMenu()
        {
            return EditTracker(ContextMenuItem);
        }

        protected async Task EditTracker(TorrentTracker? tracker)
        {
            var hash = DisplayedHash;
            if (hash is null || tracker is null)
            {
                return;
            }

            await DialogWorkflow.InvokeStringFieldDialog(
                TranslateTrackerList("Tracker editing"),
                TranslateTrackerList("Tracker URL:"),
                tracker.Url,
                async value =>
                {
                    var editResult = await ApiClient.EditTrackerAsync(hash, tracker.Url, value);
                    await ApiFeedbackWorkflow.ProcessResultAsync(editResult);
                });
        }

        protected Task RemoveTrackerToolbar()
        {
            return RemoveTracker(SelectedItem);
        }

        protected Task RemoveTrackerContextMenu()
        {
            return RemoveTracker(ContextMenuItem);
        }

        protected async Task RemoveTracker(TorrentTracker? tracker)
        {
            var hash = DisplayedHash;
            if (hash is null || tracker is null)
            {
                return;
            }

            var removeResult = await ApiClient.RemoveTrackersAsync(TorrentSelector.FromHash(hash), [tracker.Url]);
            await ApiFeedbackWorkflow.ProcessResultAsync(removeResult);
        }

        protected Task CopyTrackerUrlToolbar()
        {
            return CopyTrackerUrl(SelectedItem);
        }

        protected Task CopyTrackerUrlContextMenu()
        {
            return CopyTrackerUrl(ContextMenuItem);
        }

        protected async Task CopyTrackerUrl(TorrentTracker? tracker)
        {
            var hash = DisplayedHash;
            if (hash is null)
            {
                return;
            }

            if (tracker is null)
            {
                return;
            }

            await JSRuntime.WriteToClipboard(tracker.Url);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _refreshTimer ??= ManagedTimerFactory.Create("TrackersTabRefresh", TimeSpan.FromMilliseconds(RefreshInterval));
                await _refreshTimer.StartAsync(RefreshTickAsync, _timerCancellationToken.Token);
            }
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            if (_disposedValue)
            {
                return ManagedTimerTickResult.Stop;
            }

            if (_initializingSelection)
            {
                return ManagedTimerTickResult.Continue;
            }

            if (DisplayedHashChanged.HasDelegate && _reportedHash != Hash)
            {
                await OnParametersSetAsync();
                return ManagedTimerTickResult.Continue;
            }

            var hash = Hash;
            var version = _selectionVersion;
            if (Active && hash is not null)
            {
                var trackersResult = await ApiClient.GetTorrentTrackersAsync(hash);
                if (_disposedValue || hash != Hash || version != _selectionVersion)
                {
                    return _disposedValue ? ManagedTimerTickResult.Stop : ManagedTimerTickResult.Continue;
                }
                if (trackersResult.IsFailure)
                {
                    var failureKind = trackersResult.Failure?.Kind;
                    await ApiFeedbackWorkflow.HandleFailureAsync(trackersResult, failure =>
                    {
                        if (failure.Kind is ApiFailureKind.AuthenticationRequired or ApiFailureKind.NotFound)
                        {
                            _timerCancellationToken.CancelIfNotDisposed();
                        }

                        return failure.Kind == ApiFailureKind.NotFound
                            ? ApiFeedbackCustomFailureResult.StopHandling
                            : ApiFeedbackCustomFailureResult.ContinueWithWorkflow;
                    });

                    if (failureKind is ApiFailureKind.AuthenticationRequired or ApiFailureKind.NotFound)
                    {
                        return ManagedTimerTickResult.Stop;
                    }

                    return ManagedTimerTickResult.Continue;
                }

                TrackerList = trackersResult.Value;
                await InvokeAsync(StateHasChanged);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            return ManagedTimerTickResult.Continue;
        }

        protected IEnumerable<ColumnDefinition<TorrentTracker>> Columns => GetColumnDefinitions();

        private IReadOnlyList<ColumnDefinition<TorrentTracker>> GetColumnDefinitions()
        {
            _columnDefinitions ??= BuildColumnDefinitions();

            return _columnDefinitions;
        }

        private IReadOnlyList<ColumnDefinition<TorrentTracker>> BuildColumnDefinitions()
        {
            var tierLabel = TranslateTrackerList("Tier");
            var urlLabel = TranslateTrackerList("URL/Announce Endpoint");
            var statusLabel = TranslateTrackerList("Status");
            var peersLabel = TranslateTrackerList("Peers");
            var seedsLabel = TranslateTrackerList("Seeds");
            var leechesLabel = TranslateTrackerList("Leeches");
            var timesDownloadedLabel = TranslateTrackerList("Times Downloaded");
            var messageLabel = TranslateTrackerList("Message");

            return
            [
                new ColumnDefinition<TorrentTracker>(tierLabel, w => w.Tier, w => w.Tier > 0 ? w.Tier.ToString() : string.Empty, id: "tier"),
                new ColumnDefinition<TorrentTracker>(urlLabel, w => w.Url, id: "url"),
                new ColumnDefinition<TorrentTracker>(statusLabel, w => w.Status, id: "status"),
                new ColumnDefinition<TorrentTracker>(peersLabel, w => w.Peers, id: "peers"),
                new ColumnDefinition<TorrentTracker>(seedsLabel, w => w.Seeds, id: "seeds"),
                new ColumnDefinition<TorrentTracker>(leechesLabel, w => w.Leeches, id: "leeches"),
                new ColumnDefinition<TorrentTracker>(timesDownloadedLabel, w => w.Downloads, id: "times_downloaded"),
                new ColumnDefinition<TorrentTracker>(messageLabel, w => w.Message, id: "message"),
            ];
        }

        private string TranslateTrackerList(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_trackerListContext, source, arguments);
        }

        private string TranslateTrackersAddition(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_trackersAdditionContext, source, arguments);
        }

        private string TranslateApp(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_appContext, source, arguments);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await _timerCancellationToken.CancelAsync();
                    _timerCancellationToken.Dispose();
                    if (_refreshTimer is not null)
                    {
                        await _refreshTimer.DisposeAsync();
                    }

                    await Task.CompletedTask;
                }

                _disposedValue = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
