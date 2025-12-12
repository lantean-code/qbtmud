using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Data;
using System.Net;

namespace Lantean.QBTMud.Components
{
    public partial class PeersTab : IAsyncDisposable
    {
        private bool _disposedValue;
        protected string? _oldHash;
        private int _requestId = 0;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool? _showFlags;

        private const string _toolbar = nameof(_toolbar);
        private const string _context = nameof(_context);

        [Parameter, EditorRequired]
        public string Hash { get; set; } = "";

        [Parameter]
        public bool Active { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IPeerDataManager PeerDataManager { get; set; } = default!;

        [Inject]
        protected IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IPeriodicTimerFactory TimerFactory { get; set; } = default!;

        protected PeerList? PeerList { get; set; }

        protected IEnumerable<Peer> Peers => PeerList?.Peers.Select(p => p.Value) ?? [];

        protected bool ShowFlags => _showFlags.GetValueOrDefault();

        protected Peer? ContextMenuItem { get; set; }

        protected Peer? SelectedItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        protected DynamicTable<Peer>? Table { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (string.IsNullOrWhiteSpace(Hash))
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
                PeerList = PeerDataManager.CreatePeerList(peers);
            }
            else
            {
                PeerDataManager.MergeTorrentPeers(peers, PeerList);
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

        protected async Task AddPeer()
        {
            if (Hash is null)
            {
                return;
            }

            var peers = await DialogWorkflow.ShowAddPeersDialog();
            if (peers is null || peers.Count == 0)
            {
                return;
            }

            await ApiClient.AddPeers([Hash], peers);
        }

        protected Task BanPeerToolbar()
        {
            return BanPeer(SelectedItem);
        }

        protected Task BanPeerContextMenu()
        {
            return BanPeer(ContextMenuItem);
        }

        protected async Task CopyPeerContextMenu()
        {
            await CopyPeer(ContextMenuItem);
        }

        private async Task BanPeer(Peer? peer)
        {
            if (Hash is null || peer is null)
            {
                return;
            }
            await ApiClient.BanPeers([new QBitTorrentClient.Models.PeerId(peer.IPAddress, peer.Port)]);
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<Peer> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<Peer> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        private async Task CopyPeer(Peer? peer)
        {
            if (peer is null)
            {
                return;
            }

            await ClipboardService.WriteToClipboard($"{peer.IPAddress}:{peer.Port}");
            Snackbar.Add("Peer copied to clipboard.", Severity.Info);
        }

        protected async Task ShowContextMenu(Peer? peer, EventArgs eventArgs)
        {
            ContextMenuItem = peer;

            if (ContextMenu is null)
            {
                return;
            }

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu.OpenMenuAsync(normalizedEventArgs);
        }

        protected void SelectedItemChanged(Peer peer)
        {
            SelectedItem = peer;
        }

        protected async Task ColumnOptions()
        {
            if (Table is null)
            {
                return;
            }

            await Table.ShowColumnOptionsDialog();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await using (var timer = TimerFactory.Create(TimeSpan.FromMilliseconds(RefreshInterval)))
                {
                    while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(_timerCancellationToken.Token))
                    {
                        if (string.IsNullOrWhiteSpace(Hash))
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
                                PeerList = PeerDataManager.CreatePeerList(peers);
                            }
                            else
                            {
                                PeerDataManager.MergeTorrentPeers(peers, PeerList);
                            }

                            _requestId = peers.RequestId;
                        }
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        protected IEnumerable<ColumnDefinition<Peer>> Columns => ColumnsDefinitions.Where(c => c.Id != "country/region" || _showFlags == true);

        public static List<ColumnDefinition<Peer>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<Peer>("Country/Region", p => p.Country),
            new ColumnDefinition<Peer>("IP", p => p.IPAddress),
            new ColumnDefinition<Peer>("Port", p => p.Port),
            new ColumnDefinition<Peer>("Connection", p => p.Connection),
            new ColumnDefinition<Peer>("Flags", p => p.Flags),
            new ColumnDefinition<Peer>("Client", p => p.Client),
            new ColumnDefinition<Peer>("Progress", p => p.Progress, p => DisplayHelpers.Percentage(p.Progress)),
            new ColumnDefinition<Peer>("Download Speed", p => p.DownloadSpeed, p => DisplayHelpers.Speed(p.DownloadSpeed)),
            new ColumnDefinition<Peer>("Upload Speed", p => p.UploadSpeed, p => DisplayHelpers.Speed(p.UploadSpeed)),
            new ColumnDefinition<Peer>("Downloaded", p => p.Downloaded, p => @DisplayHelpers.Size(p.Downloaded)),
            new ColumnDefinition<Peer>("Uploaded", p => p.Uploaded, p => @DisplayHelpers.Size(p.Uploaded)),
            new ColumnDefinition<Peer>("Relevance", p => p.Relevance, p => @DisplayHelpers.Percentage(p.Relevance)),
            new ColumnDefinition<Peer>("Files", p => p.Files),
        ];

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await _timerCancellationToken.CancelAsync();
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
