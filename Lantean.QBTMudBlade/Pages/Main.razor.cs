using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Main : IDisposable
    {
        private bool _refreshEnabled = true;

        protected bool DrawerOpen { get; set; } = true;

        protected int RefreshInterval { get; set; } = 1500;

        private int _requestId = 0;
        private bool _disposedValue;
        private readonly CancellationTokenSource _timerCancellationToken = new();

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        protected Models.MainData? TorrentList { get; set; }

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        protected Status Status { get; set; } = Status.All;

        protected string? SearchText { get; set; }

        protected FilterState FilterState => new FilterState(Category, Status, Tag, Tracker, TorrentList?.ServerState.UseSubcategories ?? false, SearchText);

        protected string? Version { get; set; }

        private async Task SearchTextChanged(string searchText)
        {
            SearchText = searchText == "" ? null : searchText;

            await InvokeAsync(StateHasChanged);
        }

        protected async Task SelectedTorrentChanged(string hash)
        {
            if (TorrentList is not null)
            {
                TorrentList.SelectedTorrentHash = hash;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnInitializedAsync()
        {
            if (!await ApiClient.CheckAuthState())
            {
                NavigationManager.NavigateTo("/login");
                return;
            }
            try
            {
                Version = await ApiClient.GetApplicationVersion();
                var data = await ApiClient.GetMainData(_requestId);
                TorrentList = DataManager.CreateMainData(data);
                _requestId = data.ResponseId;

                RefreshInterval = TorrentList.ServerState.RefreshInterval;

                await InvokeAsync(StateHasChanged);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                NavigationManager.NavigateTo("/login");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }
            if (firstRender)
            {
                using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RefreshInterval)))
                {
                    while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                    {
                        QBitTorrentClient.Models.MainData data;
                        try
                        {
                            data = await ApiClient.GetMainData(_requestId);
                        }
                        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
                        {
                            _timerCancellationToken.CancelIfNotDisposed();
                            return;
                        }

                        if (TorrentList is null || data.FullUpdate)
                        {
                            TorrentList = DataManager.CreateMainData(data);
                        }
                        else
                        {
                            DataManager.MergeMainData(data, TorrentList);
                        }

                        RefreshInterval = TorrentList.ServerState.RefreshInterval;
                        _requestId = data.ResponseId;
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        protected void DrawerToggle()
        {
            DrawerOpen = !DrawerOpen;
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