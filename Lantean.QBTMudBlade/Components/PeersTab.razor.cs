using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace Lantean.QBTMudBlade.Components
{
    public partial class PeersTab : IAsyncDisposable
    {
        private bool _disposedValue;

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [CascadingParameter]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        protected PeerList? PeerList { get; set; }

        protected IEnumerable<Peer> Peers => PeerList?.Peers.Select(p => p.Value) ?? [];

        private int _requestId = 0;
        private readonly CancellationTokenSource _timerCancellationToken = new();

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

            var torrentPeers = await ApiClient.GetTorrentPeersData(Hash, _requestId);
            PeerList = DataManager.CreatePeerList(torrentPeers);
            _requestId = torrentPeers.RequestId;

            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RefreshInterval)))
                {
                    while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                    {
                        if (Hash is null)
                        {
                            return;
                        }

                        if (Active)
                        {
                            QBitTorrentClient.Models.TorrentPeers peers;
                            try
                            {
                                peers = await ApiClient.GetTorrentPeersData(Hash, _requestId);
                            }
                            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
                            {
                                _timerCancellationToken.CancelIfNotDisposed();
                                return;
                            }
                            if (PeerList is null || peers.FullUpdate)
                            {
                                PeerList = DataManager.CreatePeerList(peers);
                            }
                            else
                            {
                                DataManager.MergeTorrentPeers(peers, PeerList);
                            }

                            _requestId = peers.RequestId;
                        }
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();

                    await Task.Delay(0);
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