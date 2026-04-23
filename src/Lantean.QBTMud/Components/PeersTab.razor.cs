using System.Data;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;
using UIComponents.Flags;
using MudPeer = Lantean.QBTMud.Models.Peer;

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
        private bool _showFlagsFromPeerData;
        private IReadOnlyList<ColumnDefinition<MudPeer>>? _columnDefinitions;
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
        public QBittorrentPreferences? Preferences { get; set; }

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

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        protected PeerList? PeerList { get; set; }

        protected IEnumerable<MudPeer> Peers => PeerList?.Peers.Select(p => p.Value) ?? [];

        protected bool ShowFlags => _showFlags.GetValueOrDefault();

        protected MudPeer? ContextMenuItem { get; set; }

        protected MudPeer? SelectedItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        protected DynamicTable<MudPeer>? Table { get; set; }

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
                _showFlags = null;
                _showFlagsFromPeerData = false;
            }

            var peersResult = await ApiClient.GetTorrentPeersDataAsync(Hash, _requestId);
            if (peersResult.IsFailure)
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(peersResult);
                return;
            }

            var peers = peersResult.Value;
            if (PeerList is null || peers.FullUpdate)
            {
                PeerList = PeerDataManager.CreatePeerList(peers);
            }
            else
            {
                PeerDataManager.MergeTorrentPeers(peers, PeerList);
            }
            _requestId = peers.RequestId;

            if (Preferences is not null && !_showFlagsFromPeerData)
            {
                _showFlags = Preferences.ResolvePeerCountries;
            }

            if (peers.ShowFlags.HasValue)
            {
                _showFlagsFromPeerData = true;
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

            var addResult = await ApiClient.AddPeersAsync(TorrentSelector.FromHash(Hash), peers);
            await ApiFeedbackWorkflow.ProcessResultAsync(addResult);
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

        private async Task BanPeer(MudPeer? peer)
        {
            if (Hash is null || peer is null)
            {
                return;
            }
            var banResult = await ApiClient.BanPeersAsync([new PeerId(peer.IPAddress, peer.Port)]);
            await ApiFeedbackWorkflow.ProcessResultAsync(banResult);
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<MudPeer> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<MudPeer> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        private async Task CopyPeer(MudPeer? peer)
        {
            if (peer is null)
            {
                return;
            }

            await ClipboardService.WriteToClipboard($"{peer.IPAddress}:{peer.Port}");
            SnackbarWorkflow.ShowTransientMessage(TranslateApp("Peer copied to clipboard."), Severity.Info);
        }

        protected async Task ShowContextMenu(MudPeer? peer, EventArgs eventArgs)
        {
            ContextMenuItem = peer;

            if (ContextMenu is null)
            {
                return;
            }

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu.OpenMenuAsync(normalizedEventArgs);
        }

        protected void SelectedItemChanged(MudPeer peer)
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
                var peersResult = await ApiClient.GetTorrentPeersDataAsync(Hash, _requestId);
                if (peersResult.IsFailure)
                {
                    if (peersResult.Failure?.Kind is ApiFailureKind.AuthenticationRequired or ApiFailureKind.NotFound)
                    {
                        _timerCancellationToken.CancelIfNotDisposed();
                        return ManagedTimerTickResult.Stop;
                    }

                    await ApiFeedbackWorkflow.HandleFailureAsync(peersResult);
                    return ManagedTimerTickResult.Continue;
                }

                var peers = peersResult.Value;
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

        protected IEnumerable<ColumnDefinition<MudPeer>> Columns => GetColumnDefinitions();

        protected bool FilterColumns(ColumnDefinition<MudPeer> column)
        {
            var countryColumnId = _countryColumnId ?? "country/region";
            if (string.Equals(column.Id, countryColumnId, StringComparison.Ordinal))
            {
                return ShowFlags;
            }

            return true;
        }

        private IReadOnlyList<ColumnDefinition<MudPeer>> GetColumnDefinitions()
        {
            _columnDefinitions ??= BuildColumnDefinitions();

            return _columnDefinitions;
        }

        private IReadOnlyList<ColumnDefinition<MudPeer>> BuildColumnDefinitions()
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
                new ColumnDefinition<MudPeer>(countryLabel, p => p.Country, CountryColumnTemplate, id: "country/region"),
                new ColumnDefinition<MudPeer>(ipLabel, p => p.IPAddress, id: "ip"),
                new ColumnDefinition<MudPeer>(portLabel, p => p.Port, id: "port"),
                new ColumnDefinition<MudPeer>(connectionLabel, p => p.Connection, id: "connection"),
                new ColumnDefinition<MudPeer>(flagsLabel, p => p.Flags, FlagsColumnTemplate, id: "flags"),
                new ColumnDefinition<MudPeer>(clientLabel, p => p.Client, id: "client"),
                new ColumnDefinition<MudPeer>(progressLabel, p => p.Progress, p => DisplayHelpers.Percentage(p.Progress), id: "progress"),
                new ColumnDefinition<MudPeer>(downSpeedLabel, p => p.DownloadSpeed, p => DisplayHelpers.Speed(p.DownloadSpeed), id: "download_speed"),
                new ColumnDefinition<MudPeer>(upSpeedLabel, p => p.UploadSpeed, p => DisplayHelpers.Speed(p.UploadSpeed), id: "upload_speed"),
                new ColumnDefinition<MudPeer>(downloadedLabel, p => p.Downloaded, p => DisplayHelpers.Size(p.Downloaded), id: "downloaded"),
                new ColumnDefinition<MudPeer>(uploadedLabel, p => p.Uploaded, p => DisplayHelpers.Size(p.Uploaded), id: "uploaded"),
                new ColumnDefinition<MudPeer>(relevanceLabel, p => p.Relevance, p => DisplayHelpers.Percentage(p.Relevance), id: "relevance"),
                new ColumnDefinition<MudPeer>(filesLabel, p => p.Files, id: "files"),
            ];
        }

        private static RenderFragment<RowContext<MudPeer>> FlagsColumnTemplate => context => builder =>
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

        private static RenderFragment<RowContext<MudPeer>> CountryColumnTemplate => context => builder =>
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
