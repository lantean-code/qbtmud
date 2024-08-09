using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.UI;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class TorrentList : IAsyncDisposable
    {
        private bool _disposedValue;

        private static KeyboardEvent _addTorrentFileKey = new KeyboardEvent("a") { AltKey = true };
        private static KeyboardEvent _addTorrentLinkKey = new KeyboardEvent("l") { AltKey = true };


        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [CascadingParameter]
        public IEnumerable<Torrent>? Torrents { get; set; }

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        [CascadingParameter(Name = "SearchTermChanged")]
        public EventCallback<string> SearchTermChanged { get; set; }

        [CascadingParameter(Name = "SortColumnChanged")]
        public EventCallback<string> SortColumnChanged { get; set; }

        [CascadingParameter(Name = "SortDirectionChanged")]
        public EventCallback<SortDirection> SortDirectionChanged { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected string? SearchText { get; set; }

        protected HashSet<Torrent> SelectedItems { get; set; } = [];

        protected bool ToolbarButtonsEnabled => SelectedItems.Count > 0;

        protected DynamicTable<Torrent>? Table { get; set; }

        protected Torrent? ContextMenuItem { get; set; }

        protected ContextMenu? ContextMenu { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await KeyboardService.RegisterKeypressEvent(_addTorrentFileKey, k => AddTorrentFile());
                await KeyboardService.RegisterKeypressEvent(_addTorrentLinkKey, k => AddTorrentLink());
            }
        }

        protected void SelectedItemsChanged(HashSet<Torrent> selectedItems)
        {
            SelectedItems = selectedItems;
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
            await SearchTermChanged.InvokeAsync(SearchText);
        }

        protected async Task AddTorrentFile()
        {
            await DialogService.InvokeAddTorrentFileDialog(ApiClient);
        }

        protected async Task AddTorrentLink()
        {
            await DialogService.InvokeAddTorrentLinkDialog(ApiClient);
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
                NavigationManager.NavigateTo($"/details/{torrent.Hash}");
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
            return [(ContextMenuItem is null ? "fake" : ContextMenuItem.Hash)];
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
            NavigationManager.NavigateTo($"/details/{torrent.Hash}");
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

            await ContextMenu.ToggleMenuAsync(eventArgs);
        }

        protected IEnumerable<ColumnDefinition<Torrent>> Columns => ColumnsDefinitions.Where(c => c.Id != "#" || Preferences?.QueueingEnabled == true);

        public static List<ColumnDefinition<Torrent>> ColumnsDefinitions { get; } =
        [
            CreateColumnDefinition("#", t => t.Priority),
            CreateColumnDefinition("Icon", t => t.State, IconColumn, iconOnly: true, width: 25),
            CreateColumnDefinition("Name", t => t.Name, width: 400),
            CreateColumnDefinition("Size", t => t.Size, t => DisplayHelpers.Size(t.Size)),
            CreateColumnDefinition("Total Size", t => t.TotalSize, t => DisplayHelpers.Size(t.TotalSize), enabled: false),
            CreateColumnDefinition("Done", t => t.Progress, ProgressBarColumn, tdClass: "table-progress"),
            CreateColumnDefinition("Status", t => t.State, t => DisplayHelpers.State(t.State)),
            CreateColumnDefinition("Seeds", t => t.NumberSeeds),
            CreateColumnDefinition("Peers", t => t.NumberLeeches),
            CreateColumnDefinition("Down Speed", t => t.DownloadSpeed, t => DisplayHelpers.Speed(t.DownloadSpeed)),
            CreateColumnDefinition("Up Speed", t => t.UploadSpeed, t => DisplayHelpers.Speed(t.UploadSpeed)),
            CreateColumnDefinition("ETA", t => t.EstimatedTimeOfArrival, t => DisplayHelpers.Duration(t.EstimatedTimeOfArrival)),
            CreateColumnDefinition("Ratio", t => t.Ratio, t => t.Ratio.ToString("0.00")),
            CreateColumnDefinition("Category", t => t.Category),
            CreateColumnDefinition("Tags", t => t.Tags, t => string.Join(", ", t.Tags)),
            CreateColumnDefinition("Added On", t => t.AddedOn, t => DisplayHelpers.DateTime(t.AddedOn)),
            CreateColumnDefinition("Completed On", t => t.CompletionOn, t => DisplayHelpers.DateTime(t.CompletionOn), enabled: false),
            CreateColumnDefinition("Tracker", t => t.Tracker, enabled: false),
            CreateColumnDefinition("Down Limit", t => t.DownloadLimit, t => DisplayHelpers.Size(t.DownloadLimit), enabled: false),
            CreateColumnDefinition("Up Limit", t => t.UploadLimit, t => DisplayHelpers.Size(t.UploadLimit), enabled: false),
            CreateColumnDefinition("Downloaded", t => t.Downloaded, t => DisplayHelpers.Size(t.Downloaded), enabled: false),
            CreateColumnDefinition("Uploaded", t => t.Uploaded, t => DisplayHelpers.Size(t.Uploaded), enabled: false),
            CreateColumnDefinition("Session Download", t => t.DownloadedSession, t => DisplayHelpers.Size(t.DownloadedSession), enabled: false),
            CreateColumnDefinition("Session Upload", t => t.UploadedSession, t => DisplayHelpers.Size(t.UploadedSession), enabled: false),
            CreateColumnDefinition("Remaining", t => t.AmountLeft, t => DisplayHelpers.Size(t.AmountLeft), enabled: false),
            CreateColumnDefinition("Time Active", t => t.TimeActive, t => DisplayHelpers.Duration(t.TimeActive), enabled: false),
            CreateColumnDefinition("Save path", t => t.SavePath, enabled: false),
            CreateColumnDefinition("Completed", t => t.Completed, t => DisplayHelpers.Size(t.Completed), enabled: false),
            CreateColumnDefinition("Ratio Limit", t => t.RatioLimit, t => t.Ratio.ToString("0.00"), enabled: false),
            CreateColumnDefinition("Last Seen Complete", t => t.SeenComplete, t => DisplayHelpers.DateTime(t.SeenComplete), enabled: false),
            CreateColumnDefinition("Last Activity", t => t.LastActivity, t => DisplayHelpers.DateTime(t.LastActivity), enabled: false),
            CreateColumnDefinition("Availability", t => t.Availability, t => t.Availability.ToString("0.##"), enabled: false),
            //CreateColumnDefinition("Reannounce In", t => t.Reannounce, enabled: false),
        ];

        private static ColumnDefinition<Torrent> CreateColumnDefinition(string name, Func<Torrent, object?> selector, RenderFragment<RowContext<Torrent>> rowTemplate, int? width = null, string? tdClass = null, bool enabled = true, bool iconOnly = false)
        {
            var cd = new ColumnDefinition<Torrent>(name, selector, rowTemplate);
            cd.Class = iconOnly ? "icon-cell" : "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.Width = width;
            cd.Enabled = enabled;
            cd.IconOnly = iconOnly;

            return cd;
        }

        private static ColumnDefinition<Torrent> CreateColumnDefinition(string name, Func<Torrent, object?> selector, Func<Torrent, string>? formatter = null, int? width = null, string? tdClass = null, bool enabled = true, bool iconOnly = false)
        {
            var cd = new ColumnDefinition<Torrent>(name, selector, formatter);
            cd.Class = iconOnly ? "icon-cell" : "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.Width = width;
            cd.Enabled = enabled;
            cd.IconOnly = iconOnly;

            return cd;
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
                    await KeyboardService.UnregisterKeypressEvent(_addTorrentFileKey);
                    await KeyboardService.UnregisterKeypressEvent(_addTorrentLinkKey);
                }

                _disposedValue = true;
            }
        }
    }
}