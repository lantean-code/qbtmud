using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Data;
using System.Net;
using UIComponents.Flags;

namespace Lantean.QBTMud.Components
{
    public partial class PeersTab : IAsyncDisposable
    {
        private bool _disposedValue;
        protected string? _oldHash;
        private int _requestId = 0;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool? _showFlags;
        private IReadOnlyList<ColumnDefinition<Peer>>? _columnDefinitions;
        private string? _countryColumnId;

        private const string _toolbar = nameof(_toolbar);
        private const string _context = nameof(_context);
        private const string _peerListContext = "PeerListWidget";
        private const string _appContext = "AppPeersTab";

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
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected Lantean.QBTMud.Services.Localization.ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

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
            SnackbarWorkflow.ShowTransientMessage(TranslateApp("Peer copied to clipboard."), Severity.Info);
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
                _refreshTimer ??= ManagedTimerFactory.Create("PeersTabRefresh", TimeSpan.FromMilliseconds(RefreshInterval));
                await _refreshTimer.StartAsync(RefreshTickAsync, _timerCancellationToken.Token);
            }
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Hash))
            {
                return ManagedTimerTickResult.Stop;
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
                    return ManagedTimerTickResult.Stop;
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
                await InvokeAsync(StateHasChanged);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            return ManagedTimerTickResult.Continue;
        }

        protected IEnumerable<ColumnDefinition<Peer>> Columns => GetColumnDefinitions();

        protected bool FilterColumns(ColumnDefinition<Peer> column)
        {
            var countryColumnId = _countryColumnId ?? "country/region";
            if (string.Equals(column.Id, countryColumnId, StringComparison.Ordinal))
            {
                return ShowFlags;
            }

            return true;
        }

        private IReadOnlyList<ColumnDefinition<Peer>> GetColumnDefinitions()
        {
            _columnDefinitions ??= BuildColumnDefinitions();

            return _columnDefinitions;
        }

        private IReadOnlyList<ColumnDefinition<Peer>> BuildColumnDefinitions()
        {
            var countryLabel = TranslatePeerList("Country/Region");
            var ipLabel = TranslatePeerList("IP/Address");
            var portLabel = TranslatePeerList("Port");
            var connectionLabel = TranslatePeerList("Connection");
            var flagsLabel = TranslatePeerList("Flags");
            var clientLabel = TranslatePeerList("Client");
            var progressLabel = TranslatePeerList("Progress");
            var downSpeedLabel = TranslatePeerList("Down Speed");
            var upSpeedLabel = TranslatePeerList("Up Speed");
            var downloadedLabel = TranslatePeerList("Downloaded");
            var uploadedLabel = TranslatePeerList("Uploaded");
            var relevanceLabel = TranslatePeerList("Relevance");
            var filesLabel = TranslatePeerList("Files");

            _countryColumnId = "country/region";

            return
            [
                new ColumnDefinition<Peer>(countryLabel, p => p.Country, CountryColumnTemplate, id: "country/region"),
                new ColumnDefinition<Peer>(ipLabel, p => p.IPAddress, id: "ip"),
                new ColumnDefinition<Peer>(portLabel, p => p.Port, id: "port"),
                new ColumnDefinition<Peer>(connectionLabel, p => p.Connection, id: "connection"),
                new ColumnDefinition<Peer>(flagsLabel, p => p.Flags, FlagsColumnTemplate, id: "flags"),
                new ColumnDefinition<Peer>(clientLabel, p => p.Client, id: "client"),
                new ColumnDefinition<Peer>(progressLabel, p => p.Progress, p => DisplayHelpers.Percentage(p.Progress), id: "progress"),
                new ColumnDefinition<Peer>(downSpeedLabel, p => p.DownloadSpeed, p => DisplayHelpers.Speed(p.DownloadSpeed), id: "download_speed"),
                new ColumnDefinition<Peer>(upSpeedLabel, p => p.UploadSpeed, p => DisplayHelpers.Speed(p.UploadSpeed), id: "upload_speed"),
                new ColumnDefinition<Peer>(downloadedLabel, p => p.Downloaded, p => DisplayHelpers.Size(p.Downloaded), id: "downloaded"),
                new ColumnDefinition<Peer>(uploadedLabel, p => p.Uploaded, p => DisplayHelpers.Size(p.Uploaded), id: "uploaded"),
                new ColumnDefinition<Peer>(relevanceLabel, p => p.Relevance, p => DisplayHelpers.Percentage(p.Relevance), id: "relevance"),
                new ColumnDefinition<Peer>(filesLabel, p => p.Files, id: "files"),
            ];
        }

        private static RenderFragment<RowContext<Peer>> FlagsColumnTemplate => context => builder =>
        {
            var flags = context.Data.Flags;
            if (string.IsNullOrWhiteSpace(flags))
            {
                return;
            }

            var flagsDescription = context.Data.FlagsDescription;

            builder.OpenElement(0, "span");
            if (!string.IsNullOrWhiteSpace(flagsDescription))
            {
                builder.AddAttribute(1, "title", flagsDescription);
            }
            builder.AddContent(2, flags);
            builder.CloseElement();
        };

        private static RenderFragment<RowContext<Peer>> CountryColumnTemplate => context => builder =>
        {
            var country = context.Data.Country;
            var countryCode = context.Data.CountryCode;
            var hasCountry = !string.IsNullOrWhiteSpace(country);
            var hasFlag = TryGetCountry(countryCode, out var flagCountry);

            if (!hasCountry && !hasFlag)
            {
                return;
            }

            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "peer-country");
            if (hasCountry)
            {
                builder.AddAttribute(2, "title", country);
            }

            var sequence = 3;
            if (hasFlag)
            {
                builder.OpenElement(sequence++, "span");
                builder.AddAttribute(sequence++, "class", "peer-country__flag");
                builder.OpenComponent<CountryFlag>(sequence++);
                builder.AddAttribute(sequence++, nameof(CountryFlag.Country), flagCountry);
                builder.AddAttribute(sequence++, nameof(CountryFlag.Size), FlagSize.Small);
                builder.AddAttribute(sequence++, nameof(CountryFlag.Background), "_content/BlazorFlags/flags.png");
                builder.AddAttribute(sequence++, nameof(CountryFlag.Class), "peer-country__flag-image");
                builder.CloseComponent();
                builder.CloseElement();
            }

            if (hasCountry)
            {
                builder.OpenElement(sequence++, "span");
                builder.AddAttribute(sequence++, "class", "peer-country__name");
                builder.AddContent(sequence++, country);
                builder.CloseElement();
            }

            builder.CloseElement();
        };

        private static bool TryGetCountry(string? countryCode, out Country country)
        {
            country = default;
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return false;
            }

            if (!Enum.TryParse(countryCode, true, out country))
            {
                country = default;
                return false;
            }

            return Enum.IsDefined(country);
        }

        private string TranslatePeerList(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_peerListContext, source, arguments);
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
