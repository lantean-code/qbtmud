using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net;

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

        private string? _sortColumn;
        private SortDirection _sortDirection;
        private IReadOnlyList<ColumnDefinition<TorrentTracker>>? _columnDefinitions;

        private const string _toolbar = nameof(_toolbar);
        private const string _context = nameof(_context);

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

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

        protected IReadOnlyList<TorrentTracker>? TrackerList { get; set; }

        protected IEnumerable<TorrentTracker>? Trackers => GetTrackers();

        protected TorrentTracker? ContextMenuItem { get; set; }

        protected TorrentTracker? SelectedItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        protected DynamicTable<TorrentTracker>? Table { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (Hash is null)
            {
                return;
            }

            if (!Active)
            {
                return;
            }

            TrackerList = await ApiClient.GetTorrentTrackers(Hash);

            await InvokeAsync(StateHasChanged);
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
            if (Hash is null)
            {
                return;
            }

            var trackers = await DialogWorkflow.ShowAddTrackersDialog();
            if (trackers is null || trackers.Count == 0)
            {
                return;
            }

            await ApiClient.AddTrackersToTorrent(trackers, hashes: Hash);
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
            if (Hash is null || tracker is null)
            {
                return;
            }

            await DialogWorkflow.InvokeStringFieldDialog(
                TranslateTrackerList("Tracker editing"),
                TranslateTrackerList("Tracker URL:"),
                tracker.Url,
                async value => await ApiClient.EditTracker(Hash, tracker.Url, value));
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
            if (Hash is null || tracker is null)
            {
                return;
            }

            await ApiClient.RemoveTrackers([tracker.Url], hashes: Hash);
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
            if (Hash is null)
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
            if (Active && Hash is not null)
            {
                try
                {
                    TrackerList = await ApiClient.GetTorrentTrackers(Hash);
                }
                catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
                {
                    _timerCancellationToken.CancelIfNotDisposed();
                    return ManagedTimerTickResult.Stop;
                }

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
