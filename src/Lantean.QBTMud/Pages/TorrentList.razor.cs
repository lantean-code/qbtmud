using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.RegularExpressions;

namespace Lantean.QBTMud.Pages
{
    public partial class TorrentList : IAsyncDisposable
    {
        private bool _disposedValue;
        private bool _shortcutsRegistered;

        private static readonly KeyboardEvent _addTorrentFileKey = new("a") { AltKey = true };
        private static readonly KeyboardEvent _addTorrentLinkKey = new("l") { AltKey = true };

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        public IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [CascadingParameter]
        public IReadOnlyList<Torrent>? Torrents { get; set; }

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        [CascadingParameter(Name = "LostConnection")]
        public bool LostConnection { get; set; }

        [CascadingParameter(Name = "TorrentsVersion")]
        public int TorrentsVersion { get; set; }

        [CascadingParameter(Name = "SearchTermChanged")]
        public EventCallback<FilterSearchState> SearchTermChanged { get; set; }

        [CascadingParameter(Name = "SortColumnChanged")]
        public EventCallback<string> SortColumnChanged { get; set; }

        [CascadingParameter(Name = "SortDirectionChanged")]
        public EventCallback<SortDirection> SortDirectionChanged { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected string? SearchText { get; set; }

        protected TorrentFilterField SearchField { get; set; } = TorrentFilterField.Name;

        protected bool UseRegex { get; set; }

        protected bool IsRegexValid { get; set; } = true;

        protected string? SearchErrorText { get; set; }

        protected HashSet<Torrent> SelectedItems { get; set; } = [];

        protected bool ToolbarButtonsEnabled => _toolbarButtonsEnabled;

        protected DynamicTable<Torrent>? Table { get; set; }

        protected Torrent? ContextMenuItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        private object? _lastRenderedTorrents;
        private QBitTorrentClient.Models.Preferences? _lastPreferences;
        private bool _lastLostConnection;
        private bool _hasRendered;
        private int _lastSelectionCount;
        private int _lastTorrentsVersion = -1;
        private bool _pendingSelectionChange;
        private Task? _locationChangeRenderTask;

        private bool _toolbarButtonsEnabled;
        private IReadOnlyList<ColumnDefinition<Torrent>>? _columnsDefinitions;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            NavigationManager.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await RegisterShortcutsAsync();
            }

            if (_locationChangeRenderTask is not null && _locationChangeRenderTask.IsCompleted)
            {
                _locationChangeRenderTask = null;
            }
        }

        protected override bool ShouldRender()
        {
            if (!_hasRendered)
            {
                _hasRendered = true;
                _lastRenderedTorrents = Torrents;
                _lastPreferences = Preferences;
                _lastLostConnection = LostConnection;
                _lastTorrentsVersion = TorrentsVersion;
                _lastSelectionCount = SelectedItems.Count;
                _toolbarButtonsEnabled = _lastSelectionCount > 0;
                return true;
            }

            if (_pendingSelectionChange)
            {
                _pendingSelectionChange = false;
                _lastSelectionCount = SelectedItems.Count;
                _toolbarButtonsEnabled = _lastSelectionCount > 0;
                return true;
            }

            if (_lastTorrentsVersion != TorrentsVersion)
            {
                _lastTorrentsVersion = TorrentsVersion;
                _lastRenderedTorrents = Torrents;
                _lastPreferences = Preferences;
                _lastLostConnection = LostConnection;
                _lastSelectionCount = SelectedItems.Count;
                _toolbarButtonsEnabled = _lastSelectionCount > 0;
                return true;
            }

            if (!ReferenceEquals(_lastRenderedTorrents, Torrents))
            {
                _lastRenderedTorrents = Torrents;
                _lastPreferences = Preferences;
                _lastLostConnection = LostConnection;
                _lastSelectionCount = SelectedItems.Count;
                _toolbarButtonsEnabled = _lastSelectionCount > 0;
                return true;
            }

            if (!ReferenceEquals(_lastPreferences, Preferences))
            {
                _lastPreferences = Preferences;
                _lastSelectionCount = SelectedItems.Count;
                _toolbarButtonsEnabled = _lastSelectionCount > 0;
                return true;
            }

            if (_lastLostConnection != LostConnection)
            {
                _lastLostConnection = LostConnection;
                _lastSelectionCount = SelectedItems.Count;
                _toolbarButtonsEnabled = _lastSelectionCount > 0;
                return true;
            }

            if (_lastSelectionCount != SelectedItems.Count)
            {
                _lastSelectionCount = SelectedItems.Count;
                _toolbarButtonsEnabled = _lastSelectionCount > 0;
                return true;
            }

            return false;
        }

        protected void SelectedItemsChanged(HashSet<Torrent> selectedItems)
        {
            SelectedItems = selectedItems;
            _toolbarButtonsEnabled = SelectedItems.Count > 0;
            _pendingSelectionChange = true;
            InvokeAsync(StateHasChanged);
        }

        protected async Task SortDirectionChangedHandler(SortDirection sortDirection)
        {
            await SortDirectionChanged.InvokeAsync(sortDirection);
        }

        protected async Task SortColumnChangedHandler(string columnId)
        {
            await SortColumnChanged.InvokeAsync(columnId);
        }

        protected async Task SearchTextChanged(string text)
        {
            SearchText = text;
            ValidateRegex();
            await PublishSearchStateAsync();
        }

        protected async Task OnSearchFieldChanged()
        {
            await PublishSearchStateAsync();
        }

        protected async Task OnUseRegexChanged()
        {
            ValidateRegex();
            await PublishSearchStateAsync();
        }

        protected async Task AddTorrentFile()
        {
            await DialogWorkflow.InvokeAddTorrentFileDialog();
        }

        protected async Task AddTorrentLink()
        {
            await DialogWorkflow.InvokeAddTorrentLinkDialog();
        }

        protected void RowClick(TableRowClickEventArgs<Torrent> eventArgs)
        {
            if (eventArgs.MouseEventArgs.Detail > 1)
            {
                var torrent = eventArgs.Item;
                if (torrent is null)
                {
                    return;
                }
                NavigationManager.NavigateTo($"./details/{torrent.Hash}");
            }
        }

        private IEnumerable<string> GetSelectedTorrentsHashes()
        {
            if (SelectedItems.Count > 0)
            {
                return SelectedItems.Select(t => t.Hash);
            }

            return [];
        }

        private IEnumerable<string> GetContextMenuTargetHashes()
        {
            if (ContextMenuItem is null)
            {
                return [];
            }

            var contextHash = ContextMenuItem.Hash;
            if (SelectedItems.Any(item => item.Hash == contextHash))
            {
                return SelectedItems.Select(item => item.Hash);
            }

            return [contextHash];
        }

        private async Task PublishSearchStateAsync()
        {
            var state = new FilterSearchState(SearchText, SearchField, UseRegex, IsRegexValid);
            await SearchTermChanged.InvokeAsync(state);
        }

        private void ValidateRegex()
        {
            if (!UseRegex)
            {
                IsRegexValid = true;
                SearchErrorText = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                IsRegexValid = true;
                SearchErrorText = null;
                return;
            }

            try
            {
                _ = new Regex(SearchText, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                IsRegexValid = true;
                SearchErrorText = null;
            }
            catch (ArgumentException)
            {
                IsRegexValid = false;
                SearchErrorText = WebUiLocalizer.Translate("AppTorrentList", "Invalid regular expression");
            }
        }

        public async Task ColumnOptions()
        {
            if (Table is null)
            {
                return;
            }

            await Table.ShowColumnOptionsDialog();
        }

        protected void ShowTorrentToolbar()
        {
            var torrent = SelectedItems.FirstOrDefault();

            NavigateToTorrent(torrent);
        }

        protected void ShowTorrentContextMenu()
        {
            NavigateToTorrent(ContextMenuItem);
        }

        protected void NavigateToTorrent(Torrent? torrent)
        {
            if (torrent is null)
            {
                return;
            }
            NavigationManager.NavigateTo($"./details/{torrent.Hash}");
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<Torrent> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<Torrent> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        protected async Task ShowContextMenu(Torrent? torrent, EventArgs eventArgs)
        {
            if (torrent is not null)
            {
                ContextMenuItem = torrent;
            }

            if (ContextMenu is null)
            {
                return;
            }

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu.OpenMenuAsync(normalizedEventArgs);
        }

        protected IEnumerable<ColumnDefinition<Torrent>> Columns => ColumnsDefinitions.Where(c => c.Id != "#" || Preferences?.QueueingEnabled == true);

        private IReadOnlyList<ColumnDefinition<Torrent>> ColumnsDefinitions => _columnsDefinitions ??= BuildColumnsDefinitions(WebUiLocalizer);

        internal static IReadOnlyList<ColumnDefinition<Torrent>> BuildColumnsDefinitions(IWebUiLocalizer localizer)
        {
            var progressColumn = CreateProgressBarColumn(localizer);
            var iconColumn = CreateIconColumn();
            var statusIconLabel = localizer.Translate("TransferListModel", "Status Icon");
            var nameLabel = localizer.Translate("TransferListModel", "Name");
            var sizeLabel = localizer.Translate("TransferListModel", "Size");
            var totalSizeLabel = localizer.Translate("TransferListModel", "Total Size");
            var progressLabel = localizer.Translate("TransferListModel", "Progress");
            var statusLabel = localizer.Translate("TransferListModel", "Status");
            var seedsLabel = localizer.Translate("TransferListModel", "Seeds");
            var peersLabel = localizer.Translate("TransferListModel", "Peers");
            var downSpeedLabel = localizer.Translate("TransferListModel", "Down Speed");
            var upSpeedLabel = localizer.Translate("TransferListModel", "Up Speed");
            var etaLabel = localizer.Translate("TransferListModel", "ETA");
            var ratioLabel = localizer.Translate("TransferListModel", "Ratio");
            var popularityLabel = localizer.Translate("TransferListModel", "Popularity");
            var categoryLabel = localizer.Translate("TransferListModel", "Category");
            var tagsLabel = localizer.Translate("TransferListModel", "Tags");
            var addedOnLabel = localizer.Translate("TransferListModel", "Added On");
            var completedOnLabel = localizer.Translate("TransferListModel", "Completed On");
            var trackerLabel = localizer.Translate("TransferListModel", "Tracker");
            var downLimitLabel = localizer.Translate("TransferListModel", "Down Limit");
            var upLimitLabel = localizer.Translate("TransferListModel", "Up Limit");
            var downloadedLabel = localizer.Translate("TransferListModel", "Downloaded");
            var uploadedLabel = localizer.Translate("TransferListModel", "Uploaded");
            var sessionDownloadLabel = localizer.Translate("TransferListModel", "Session Download");
            var sessionUploadLabel = localizer.Translate("TransferListModel", "Session Upload");
            var remainingLabel = localizer.Translate("TransferListModel", "Remaining");
            var timeActiveLabel = localizer.Translate("TransferListModel", "Time Active");
            var savePathLabel = localizer.Translate("TransferListModel", "Save path");
            var completedLabel = localizer.Translate("TransferListModel", "Completed");
            var ratioLimitLabel = localizer.Translate("TransferListModel", "Ratio Limit");
            var lastSeenLabel = localizer.Translate("TransferListModel", "Last Seen Complete");
            var lastActivityLabel = localizer.Translate("TransferListModel", "Last Activity");
            var availabilityLabel = localizer.Translate("TransferListModel", "Availability");
            var incompleteSavePathLabel = localizer.Translate("TransferListModel", "Incomplete Save Path");
            var infoHashV1Label = localizer.Translate("TransferListModel", "Info Hash v1");
            var infoHashV2Label = localizer.Translate("TransferListModel", "Info Hash v2");
            var reannounceLabel = localizer.Translate("TransferListModel", "Reannounce In");
            var privateLabel = localizer.Translate("TransferListModel", "Private");

            return new List<ColumnDefinition<Torrent>>
            {
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("#", t => t.Priority, id: "#"),
                ColumnDefinitionHelper.CreateColumnDefinition(statusIconLabel, t => t.State, iconColumn, iconOnly: true, width: 25, tdClass: "table-icon", id: "icon"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(nameLabel, t => t.Name, width: 400, id: "name"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(sizeLabel, t => t.Size, t => DisplayHelpers.Size(t.Size), id: "size"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(totalSizeLabel, t => t.TotalSize, t => DisplayHelpers.Size(t.TotalSize), enabled: false, id: "total_size"),
                ColumnDefinitionHelper.CreateColumnDefinition(progressLabel, t => t.Progress, progressColumn, tdClass: "table-progress", id: "done"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(statusLabel, t => t.State, t => DisplayHelpers.State(t.State), id: "status"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(seedsLabel, t => t.NumberSeeds, id: "seeds"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(peersLabel, t => t.NumberLeeches, id: "peers"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(downSpeedLabel, t => t.DownloadSpeed, t => DisplayHelpers.Speed(t.DownloadSpeed), id: "down_speed"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(upSpeedLabel, t => t.UploadSpeed, t => DisplayHelpers.Speed(t.UploadSpeed), id: "up_speed"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(etaLabel, t => t.EstimatedTimeOfArrival, t => DisplayHelpers.Duration(t.EstimatedTimeOfArrival), id: "eta"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(ratioLabel, t => t.Ratio, t => t.Ratio.ToString("0.00"), id: "ratio"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(popularityLabel, t => t.Popularity, t => t.Popularity.ToString("0.00"), id: "popularity"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(categoryLabel, t => t.Category, id: "category"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(tagsLabel, t => t.Tags, t => string.Join(", ", t.Tags), id: "tags"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(addedOnLabel, t => t.AddedOn, t => DisplayHelpers.DateTime(t.AddedOn), id: "added_on"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(completedOnLabel, t => t.CompletionOn, t => DisplayHelpers.DateTime(t.CompletionOn), enabled: false, id: "completed_on"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(trackerLabel, t => t.Tracker, enabled: false, id: "tracker"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(downLimitLabel, t => t.DownloadLimit, t => DisplayHelpers.Size(t.DownloadLimit), enabled: false, id: "down_limit"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(upLimitLabel, t => t.UploadLimit, t => DisplayHelpers.Size(t.UploadLimit), enabled: false, id: "up_limit"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(downloadedLabel, t => t.Downloaded, t => DisplayHelpers.Size(t.Downloaded), enabled: false, id: "downloaded"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(uploadedLabel, t => t.Uploaded, t => DisplayHelpers.Size(t.Uploaded), enabled: false, id: "uploaded"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(sessionDownloadLabel, t => t.DownloadedSession, t => DisplayHelpers.Size(t.DownloadedSession), enabled: false, id: "session_download"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(sessionUploadLabel, t => t.UploadedSession, t => DisplayHelpers.Size(t.UploadedSession), enabled: false, id: "session_upload"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(remainingLabel, t => t.AmountLeft, t => DisplayHelpers.Size(t.AmountLeft), enabled: false, id: "remaining"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(timeActiveLabel, t => t.TimeActive, t => DisplayHelpers.Duration(t.TimeActive), enabled: false, id: "time_active"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(savePathLabel, t => t.SavePath, enabled: false, id: "save_path"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(completedLabel, t => t.Completed, t => DisplayHelpers.Size(t.Completed), enabled: false, id: "completed"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(ratioLimitLabel, t => t.RatioLimit, t => DisplayHelpers.RatioLimit(t.RatioLimit), enabled: false, id: "ratio_limit"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(lastSeenLabel, t => t.SeenComplete, t => DisplayHelpers.DateTime(t.SeenComplete), enabled: false, id: "last_seen_complete"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(lastActivityLabel, t => t.LastActivity, t => DisplayHelpers.DateTime(t.LastActivity), enabled: false, id: "last_activity"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(availabilityLabel, t => t.Availability, t => t.Availability.ToString("0.##"), enabled: false, id: "availability"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(incompleteSavePathLabel, t => t.DownloadPath, t => DisplayHelpers.EmptyIfNull(t.DownloadPath), enabled: false, id: "incomplete_save_path"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(infoHashV1Label, t => t.InfoHashV1, t => DisplayHelpers.EmptyIfNull(t.InfoHashV1), enabled: false, id: "info_hash_v1"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(infoHashV2Label, t => t.InfoHashV2, t => DisplayHelpers.EmptyIfNull(t.InfoHashV2), enabled: false, id: "info_hash_v2"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(reannounceLabel, t => t.Reannounce, t => DisplayHelpers.Duration(t.Reannounce), enabled: false, id: "reannounce_in"),
                ColumnDefinitionHelper.CreateColumnDefinition<Torrent>(privateLabel, t => t.IsPrivate, t => DisplayHelpers.Bool(t.IsPrivate), enabled: false, id: "private")
            }.AsReadOnly();
        }

        private static RenderFragment<RowContext<Torrent>> CreateProgressBarColumn(IWebUiLocalizer localizer)
        {
            var title = localizer.Translate("TransferListModel", "Progress");
            return context => builder =>
            {
                var value = (float?)context.GetValue();
                var color = value < 1 ? Color.Success : Color.Info;

                builder.OpenComponent<MudProgressLinear>(0);
                builder.AddAttribute(1, "title", title);
                builder.AddAttribute(2, nameof(MudProgressLinear.Color), color);
                builder.AddAttribute(3, nameof(MudProgressLinear.Value), (double)(value ?? 0) * 100);
                builder.AddAttribute(4, nameof(MudProgressLinear.Class), "progress-expand");
                builder.AddAttribute(5, nameof(MudProgressLinear.Size), Size.Large);
                builder.AddAttribute(6, nameof(MudProgressLinear.ChildContent), (RenderFragment)(childBuilder =>
                {
                    childBuilder.AddContent(7, DisplayHelpers.Percentage(value));
                }));
                builder.CloseComponent();
            };
        }

        private static RenderFragment<RowContext<Torrent>> CreateIconColumn()
        {
            return context => builder =>
            {
                var (icon, color) = DisplayHelpers.GetStateIcon((string?)context.GetValue());
                builder.OpenComponent<MudIcon>(0);
                builder.AddAttribute(1, nameof(MudIcon.Icon), icon);
                builder.AddAttribute(2, nameof(MudIcon.Color), color);
                builder.CloseComponent();
            };
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await UnregisterShortcutsAsync();
                    NavigationManager.LocationChanged -= OnLocationChanged;
                }

                _disposedValue = true;
            }
        }

        private async Task RegisterShortcutsAsync()
        {
            if (_shortcutsRegistered)
            {
                return;
            }

            await KeyboardService.RegisterKeypressEvent(_addTorrentFileKey, k => AddTorrentFile());
            await KeyboardService.RegisterKeypressEvent(_addTorrentLinkKey, k => AddTorrentLink());
            _shortcutsRegistered = true;
        }

        private async Task UnregisterShortcutsAsync()
        {
            if (!_shortcutsRegistered)
            {
                return;
            }

            await KeyboardService.UnregisterKeypressEvent(_addTorrentFileKey);
            await KeyboardService.UnregisterKeypressEvent(_addTorrentLinkKey);
            _shortcutsRegistered = false;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (_disposedValue)
            {
                return;
            }

            _locationChangeRenderTask = InvokeAsync(UnregisterShortcutsAsync);
        }
    }
}
