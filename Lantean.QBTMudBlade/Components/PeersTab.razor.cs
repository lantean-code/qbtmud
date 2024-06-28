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
        protected string? _oldHash;
        private int _requestId = 0;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool? _showFlags;

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        protected PeerList? PeerList { get; set; }

        protected IEnumerable<Peer> Peers => PeerList?.Peers.Select(p => p.Value) ?? [];

        protected bool ShowFlags => _showFlags.GetValueOrDefault();

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

            if (Hash != _oldHash)
            {
                _oldHash = Hash;
                _requestId = 0;
                PeerList = null;
            }

            var peers = await ApiClient.GetTorrentPeersData(Hash, _requestId);
            if (PeerList is null || peers.FullUpdate)
            {
                PeerList = DataManager.CreatePeerList(peers);
            }
            else
            {
                DataManager.MergeTorrentPeers(peers, PeerList);
            }
            _requestId = peers.RequestId;

            if (Preferences is not null && _showFlags is null)
            {
                _showFlags = Preferences.ResolvePeerCountries;
            }

            if (peers.ShowFlags.HasValue)
            {
                _showFlags = peers.ShowFlags.Value;
            }

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
                            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
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