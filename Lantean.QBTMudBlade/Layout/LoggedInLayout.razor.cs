using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Layout
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

        protected MainData? MainData { get; set; }

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        protected Status Status { get; set; } = Status.All;

        protected string Version { get; set; } = "";

        protected string? SearchText { get; set; }

        protected IEnumerable<Torrent> Torrents => GetTorrents();

        protected bool IsAuthenticated { get; set; }

        protected bool LostConnection { get; set; }

        private List<Torrent> GetTorrents()
        {
            if (MainData is null)
            {
                return [];
            }

            var filterState = new FilterState(Category, Status, Tag, Tracker, MainData.ServerState.UseSubcategories, SearchText);

            return MainData.Torrents.Values.Filter(filterState).ToList();
        }

        protected override async Task OnInitializedAsync()
        {
            if (!await ApiClient.CheckAuthState())
            {
                NavigationManager.NavigateTo("/login");
                return;
            }

            await InvokeAsync(StateHasChanged);

            Version = await ApiClient.GetApplicationVersion();
            var data = await ApiClient.GetMainData(_requestId);
            MainData = DataManager.CreateMainData(data);

            _requestId = data.ResponseId;
            _refreshInterval = MainData.ServerState.RefreshInterval;

            IsAuthenticated = true;
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

                        if (MainData is null || data.FullUpdate)
                        {
                            MainData = DataManager.CreateMainData(data);
                        }
                        else
                        {
                            DataManager.MergeMainData(data, MainData);
                        }

                        _refreshInterval = MainData.ServerState.RefreshInterval;
                        _requestId = data.ResponseId;
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        protected EventCallback<string> CategoryChanged => EventCallback.Factory.Create<string>(this, category => Category = category);

        protected EventCallback<Status> StatusChanged => EventCallback.Factory.Create<Status>(this, status => Status = status);

        protected EventCallback<string> TagChanged => EventCallback.Factory.Create<string>(this, tag => Tag = tag);

        protected EventCallback<string> TrackerChanged => EventCallback.Factory.Create<string>(this, tracker => Tracker = tracker);

        protected EventCallback<string> SearchTermChanged => EventCallback.Factory.Create<string>(this, term => SearchText = term);

        protected static (string, Color) GetConnectionIcon(string? status)
        {
            if (status is null)
            {
                return (Icons.Material.Outlined.SignalWifiOff, Color.Warning);
            }

            return (Icons.Material.Outlined.SignalWifi4Bar, Color.Success);
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