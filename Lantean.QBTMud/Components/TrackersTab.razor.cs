using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net;

namespace Lantean.QBTMud.Components
{
    public partial class TrackersTab : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        private string? _sortColumn;
        private SortDirection _sortDirection;

        private const string _toolbar = nameof(_toolbar);
        private const string _context = nameof(_context);

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

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
            var sortSelector = ColumnsDefinitions.Find(c => c.Id == _sortColumn)?.SortSelector;

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

            var trackers = await DialogService.ShowAddTrackersDialog();
            if (trackers is null || trackers.Count == 0)
            {
                return;
            }

            await ApiClient.AddTrackersToTorrent(trackers, hashes: new[] { Hash });
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

            await DialogService.InvokeStringFieldDialog("Edit Tracker", "Tracker URL", tracker.Url, async (value) => await ApiClient.EditTracker(Hash, tracker.Url, value));
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

            await ApiClient.RemoveTrackers([tracker.Url], hashes: new[] { Hash });
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
                using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RefreshInterval)))
                {
                    while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
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
                                return;
                            }
                        }

                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        protected IEnumerable<ColumnDefinition<TorrentTracker>> Columns => ColumnsDefinitions;

        public static List<ColumnDefinition<TorrentTracker>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<TorrentTracker>("Tier", w => w.Tier, w => w.Tier > 0 ? w.Tier.ToString() : ""),
            new ColumnDefinition<TorrentTracker>("URL", w => w.Url),
            new ColumnDefinition<TorrentTracker>("Status", w => w.Status),
            new ColumnDefinition<TorrentTracker>("Peers", w => w.Peers),
            new ColumnDefinition<TorrentTracker>("Seeds", w => w.Seeds),
            new ColumnDefinition<TorrentTracker>("Leeches", w => w.Leeches),
            new ColumnDefinition<TorrentTracker>("Times Downloaded", w => w.Downloads),
            new ColumnDefinition<TorrentTracker>("Message", w => w.Message),
        ];

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();

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
