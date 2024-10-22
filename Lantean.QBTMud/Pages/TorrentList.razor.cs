﻿using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class TorrentList : IAsyncDisposable
    {
        private bool _disposedValue;

        private static readonly KeyboardEvent _addTorrentFileKey = new("a") { AltKey = true };
        private static readonly KeyboardEvent _addTorrentLinkKey = new("l") { AltKey = true };

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
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("#", t => t.Priority),
            ColumnDefinitionHelper.CreateColumnDefinition("Icon", t => t.State, IconColumn, iconOnly: true, width: 25),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Name", t => t.Name, width: 400),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Size", t => t.Size, t => DisplayHelpers.Size(t.Size)),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Total Size", t => t.TotalSize, t => DisplayHelpers.Size(t.TotalSize), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition("Done", t => t.Progress, ProgressBarColumn, tdClass: "table-progress"),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Status", t => t.State, t => DisplayHelpers.State(t.State)),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Seeds", t => t.NumberSeeds),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Peers", t => t.NumberLeeches),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Down Speed", t => t.DownloadSpeed, t => DisplayHelpers.Speed(t.DownloadSpeed)),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Up Speed", t => t.UploadSpeed, t => DisplayHelpers.Speed(t.UploadSpeed)),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("ETA", t => t.EstimatedTimeOfArrival, t => DisplayHelpers.Duration(t.EstimatedTimeOfArrival)),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Ratio", t => t.Ratio, t => t.Ratio.ToString("0.00")),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Category", t => t.Category),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Tags", t => t.Tags, t => string.Join(", ", t.Tags)),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Added On", t => t.AddedOn, t => DisplayHelpers.DateTime(t.AddedOn)),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Completed On", t => t.CompletionOn, t => DisplayHelpers.DateTime(t.CompletionOn), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Tracker", t => t.Tracker, enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Down Limit", t => t.DownloadLimit, t => DisplayHelpers.Size(t.DownloadLimit), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Up Limit", t => t.UploadLimit, t => DisplayHelpers.Size(t.UploadLimit), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Downloaded", t => t.Downloaded, t => DisplayHelpers.Size(t.Downloaded), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Uploaded", t => t.Uploaded, t => DisplayHelpers.Size(t.Uploaded), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Session Download", t => t.DownloadedSession, t => DisplayHelpers.Size(t.DownloadedSession), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Session Upload", t => t.UploadedSession, t => DisplayHelpers.Size(t.UploadedSession), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Remaining", t => t.AmountLeft, t => DisplayHelpers.Size(t.AmountLeft), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Time Active", t => t.TimeActive, t => DisplayHelpers.Duration(t.TimeActive), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Save path", t => t.SavePath, enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Completed", t => t.Completed, t => DisplayHelpers.Size(t.Completed), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Ratio Limit", t => t.RatioLimit, t => t.Ratio.ToString("0.00"), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Last Seen Complete", t => t.SeenComplete, t => DisplayHelpers.DateTime(t.SeenComplete), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Last Activity", t => t.LastActivity, t => DisplayHelpers.DateTime(t.LastActivity), enabled: false),
            ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Availability", t => t.Availability, t => t.Availability.ToString("0.##"), enabled: false),
            //ColumnDefinitionHelper.CreateColumnDefinition<Torrent>("Reannounce In", t => t.Reannounce, enabled: false),
        ];

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