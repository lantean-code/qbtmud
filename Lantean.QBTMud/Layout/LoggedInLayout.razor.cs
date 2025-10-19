using System;
using System.Linq;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Layout
{
    public partial class LoggedInLayout : IDisposable
    {
        private readonly bool _refreshEnabled = true;

        private int _requestId = 0;
        private bool _disposedValue;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private int _refreshInterval = 1500;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public Menu? Menu { get; set; }

        protected MainData? MainData { get; set; }

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        protected Status Status { get; set; } = Status.All;

        protected QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        protected string? SortColumn { get; set; }

        protected SortDirection SortDirection { get; set; }

        protected string Version { get; set; } = "";

        protected string? SearchText { get; set; }

        protected IReadOnlyList<Torrent> Torrents => GetTorrents();

        protected bool IsAuthenticated { get; set; }

        protected bool LostConnection { get; set; }

        private IReadOnlyList<Torrent> _visibleTorrents = Array.Empty<Torrent>();

        private bool _torrentsDirty = true;
        private int _torrentsVersion;

        private IReadOnlyList<Torrent> GetTorrents()
        {
            if (!_torrentsDirty)
            {
                return _visibleTorrents;
            }

            if (MainData is null)
            {
                _visibleTorrents = Array.Empty<Torrent>();
                _torrentsDirty = false;
                return _visibleTorrents;
            }

            var filterState = new FilterState(Category, Status, Tag, Tracker, MainData.ServerState.UseSubcategories, SearchText);
            _visibleTorrents = MainData.Torrents.Values.Filter(filterState).ToList();
            _torrentsDirty = false;

            return _visibleTorrents;
        }

        protected override async Task OnInitializedAsync()
        {
            if (!await ApiClient.CheckAuthState())
            {
                NavigationManager.NavigateTo("/login");
                return;
            }

            await InvokeAsync(StateHasChanged);

            Preferences = await ApiClient.GetApplicationPreferences();
            Version = await ApiClient.GetApplicationVersion();
            var data = await ApiClient.GetMainData(_requestId);
            MainData = DataManager.CreateMainData(data, Version);
            MarkTorrentsDirty();

            _requestId = data.ResponseId;
            _refreshInterval = MainData.ServerState.RefreshInterval;

            IsAuthenticated = true;

            Menu?.ShowMenu(Preferences);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (firstRender)
            {
                using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_refreshInterval)))
                {
                    while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                    {
                        if (!IsAuthenticated)
                        {
                            return;
                        }
                        QBitTorrentClient.Models.MainData data;
                        try
                        {
                            data = await ApiClient.GetMainData(_requestId);
                        }
                        catch (HttpRequestException)
                        {
                            if (MainData is not null)
                            {
                                MainData.LostConnection = true;
                            }
                            _timerCancellationToken.CancelIfNotDisposed();
                            await InvokeAsync(StateHasChanged);
                            return;
                        }

                        var shouldRender = false;

                        if (MainData is null || data.FullUpdate)
                        {
                            MainData = DataManager.CreateMainData(data, Version);
                            MarkTorrentsDirty();
                            shouldRender = true;
                        }
                        else
                        {
                            var dataChanged = DataManager.MergeMainData(data, MainData, out var filterChanged);
                            if (filterChanged)
                            {
                                MarkTorrentsDirty();
                            }
                            else if (dataChanged)
                            {
                                IncrementTorrentsVersion();
                            }
                            shouldRender = dataChanged;
                        }

                        if (MainData is not null)
                        {
                            _refreshInterval = MainData.ServerState.RefreshInterval;
                        }
                        _requestId = data.ResponseId;
                        if (shouldRender)
                        {
                            await InvokeAsync(StateHasChanged);
                        }
                    }
                }
            }
        }

        protected EventCallback<string> CategoryChanged => EventCallback.Factory.Create<string>(this, OnCategoryChanged);

        protected EventCallback<Status> StatusChanged => EventCallback.Factory.Create<Status>(this, OnStatusChanged);

        protected EventCallback<string> TagChanged => EventCallback.Factory.Create<string>(this, OnTagChanged);

        protected EventCallback<string> TrackerChanged => EventCallback.Factory.Create<string>(this, OnTrackerChanged);

        protected EventCallback<string> SearchTermChanged => EventCallback.Factory.Create<string>(this, OnSearchTermChanged);

        protected EventCallback<string> SortColumnChanged => EventCallback.Factory.Create<string>(this, columnId => SortColumn = columnId);

        protected EventCallback<SortDirection> SortDirectionChanged => EventCallback.Factory.Create<SortDirection>(this, sortDirection => SortDirection = sortDirection);

        protected static (string, Color) GetConnectionIcon(string? status)
        {
            if (status is null)
            {
                return (Icons.Material.Outlined.SignalWifiOff, Color.Warning);
            }

            return (Icons.Material.Outlined.SignalWifi4Bar, Color.Success);
        }

        private void OnCategoryChanged(string category)
        {
            if (Category == category)
            {
                return;
            }

            Category = category;
            MarkTorrentsDirty();
        }

        private void OnStatusChanged(Status status)
        {
            if (Status == status)
            {
                return;
            }

            Status = status;
            MarkTorrentsDirty();
        }

        private void OnTagChanged(string tag)
        {
            if (Tag == tag)
            {
                return;
            }

            Tag = tag;
            MarkTorrentsDirty();
        }

        private void OnTrackerChanged(string tracker)
        {
            if (Tracker == tracker)
            {
                return;
            }

            Tracker = tracker;
            MarkTorrentsDirty();
        }

        private void OnSearchTermChanged(string term)
        {
            if (SearchText == term)
            {
                return;
            }

            SearchText = term;
            MarkTorrentsDirty();
        }

        private void MarkTorrentsDirty()
        {
            _torrentsDirty = true;
            IncrementTorrentsVersion();
        }

        private void IncrementTorrentsVersion()
        {
            unchecked
            {
                _torrentsVersion++;
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
