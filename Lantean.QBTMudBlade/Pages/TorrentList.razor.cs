using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components;
using Lantean.QBTMudBlade.Interop;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class TorrentList
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

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

        protected string? SearchText { get; set; }

        protected HashSet<Torrent> SelectedItems { get; set; } = [];

        protected bool ToolbarButtonsEnabled => SelectedItems.Count > 0;

        protected DynamicTable<Torrent>? Table { get; set; }

        protected TorrentActions? ContextMenuActions { get; set; }

        protected Torrent? ContextMenuItem { get; set; }

        protected ContextMenu? ContextMenu { get; set; }

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

        protected void ShowTorrent()
        {
            var torrent = SelectedItems.FirstOrDefault();
            if (torrent is null)
            {
                return;
            }
            NavigationManager.NavigateTo($"/details/{torrent.Hash}");
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<Torrent> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
            //return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs.ClientX, eventArgs.MouseEventArgs.ClientY);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<Torrent> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
            //return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs.ClientX, eventArgs.LongPressEventArgs.ClientY);
        }

        protected async Task ShowContextMenu(Torrent? torrent, double x, double y)
        {
            if (torrent is not null)
            {
                ContextMenuItem = torrent;
            }

            await JSRuntime.ClearSelection();
            if (ContextMenuActions is null || ContextMenuActions.ActionsMenu is null)
            {
                return;
            }

            int? maxHeight = null;

            var mainContentSize = await JSRuntime.GetInnerDimensions(".mud-main-content");
            var contextMenuHeight = ContextMenuActions.CalculateMenuHeight();

            // the bottom position of the window will be rendered off screen
            if ((y - 64 + contextMenuHeight) >= (mainContentSize.Height))
            {
                // adjust the top of the context menu
                var overshoot = Math.Abs(mainContentSize.Height -  (y + contextMenuHeight));
                y -= overshoot;
                if (y < 70)
                {
                    y = 70;
                }

                if ((y - 64 + contextMenuHeight) >= mainContentSize.Height)
                {
                    maxHeight = (int)mainContentSize.Height - (int)y + 64;
                }
            }

#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
            ContextMenuActions.ActionsMenu.MaxHeight = maxHeight;
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.

            // emulate mouseeventargs for MudBlazor
            var mouseEventArgs = new MouseEventArgs
            {
                OffsetX = x,
                OffsetY = y,
            };

            await ContextMenuActions.ActionsMenu.OpenMenuAsync(mouseEventArgs);
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
            CreateColumnDefinition("Icon", t => t.State, IconColumn, iconOnly: true),
            CreateColumnDefinition("Name", t => t.Name, width: 400),
            CreateColumnDefinition("Size", t => t.Size, t => DisplayHelpers.Size(t.Size)),
            CreateColumnDefinition("Total Size", t => t.TotalSize, t => DisplayHelpers.Size(t.TotalSize), enabled: false),
            CreateColumnDefinition("Done", t => t.Progress, ProgressBarColumn, tdClass: "table-progress pl-1 pr-1"),
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
            cd.Class = "no-wrap";
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
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.Width = width;
            cd.Enabled = enabled;
            cd.IconOnly = iconOnly;

            return cd;
        }
    }
}