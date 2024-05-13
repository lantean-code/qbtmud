using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.Dialogs;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class TorrentList
    {
        private const string _columnSelectionStorageKey = "TorrentList.ColumnSelection";
        private const string _columnSortStorageKey = "TorrentList.ColumnSort";

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [CascadingParameter]
        public IEnumerable<Torrent>? Torrents { get; set; }

        protected IEnumerable<Torrent>? OrderedTorrents => GetOrderedTorrents();

        [CascadingParameter(Name = "SearchTermChanged")]
        public EventCallback<string> SearchTermChanged { get; set; }

        protected string? SearchText { get; set; }

        protected Torrent? SelectedTorrent { get; set; }

        protected HashSet<Torrent> SelectedItems { get; set; } = [];

        protected bool ToolbarButtonsEnabled => SelectedItems.Count > 0 || SelectedTorrent is not null;

        protected override async Task OnInitializedAsync()
        {
            var selectedColumns = await LocalStorage.GetItemAsync<HashSet<string>>(_columnSelectionStorageKey);
            if (selectedColumns is not null)
            {
                SelectedColumns = selectedColumns;
            }

            var columnSort = await LocalStorage.GetItemAsync<Tuple<string, SortDirection>>(_columnSortStorageKey);
            if (columnSort is not null)
            {
                _sortColumn = columnSort.Item1;
                _sortDirection = columnSort.Item2;
            }
        }

        protected override void OnParametersSet()
        {
            if (SelectedColumns.Count == 0)
            {
                SelectedColumns = _columns.Where(c => c.Enabled).Select(c => c.Id).ToHashSet();
                if (Preferences?.QueueingEnabled == false)
                {
                    SelectedColumns.Remove("#");
                }
            }
            _sortColumn ??= _columns.First(c => c.Enabled).Id;

            if (SelectedTorrent is not null && Torrents is not null && !Torrents.Contains(SelectedTorrent))
            {
                SelectedTorrent = null;
            }
        }

        private IEnumerable<Torrent>? GetOrderedTorrents()
        {
            if (Torrents is null)
            {
                return null;
            }

            var sortSelector = _columns.Find(c => c.Id == _sortColumn)?.SortSelector;

            return Torrents.OrderByDirection(_sortDirection, sortSelector ?? (t => t.Priority));
        }

        protected void SelectedItemsChanged(HashSet<Torrent> selectedItems)
        {
            SelectedItems = selectedItems;
            if (selectedItems.Count == 1)
            {
                SelectedTorrent = selectedItems.First();
            }
        }

        protected async Task SearchTextChanged(string text)
        {
            SearchText = text;
            await SearchTermChanged.InvokeAsync(SearchText);
        }

        protected async Task PauseTorrents(MouseEventArgs eventArgs)
        {
            await ApiClient.PauseTorrents(GetSelectedTorrents());

            SelectedItems.Clear();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task ResumeTorrents(MouseEventArgs eventArgs)
        {
            await ApiClient.ResumeTorrents(GetSelectedTorrents());

            SelectedItems.Clear();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task RemoveTorrents(MouseEventArgs eventArgs)
        {
            var reference = await DialogService.ShowAsync<DeleteDialog>("Remove torrent(s)?");
            var result = await reference.Result;
            if (result.Canceled)
            {
                return;
            }

            await ApiClient.DeleteTorrents(GetSelectedTorrents(), (bool)result.Data);

            SelectedItems.Clear();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddTorrentFile(MouseEventArgs eventArgs)
        {
            await DialogService.InvokeAddTorrentFileDialog(ApiClient);
        }

        protected async Task AddTorrentLink(MouseEventArgs eventArgs)
        {
            await DialogService.InvokeAddTorrentLinkDialog(ApiClient);
        }

        protected void RowClick(TableRowClickEventArgs<Torrent> eventArgs)
        {
            if (eventArgs.MouseEventArgs.CtrlKey)
            {
                if (SelectedItems.Contains(eventArgs.Item))
                {
                    SelectedItems.Remove(eventArgs.Item);
                }
                else
                {
                    SelectedItems.Add(eventArgs.Item);
                }

                return;
            }

            if (SelectedItems.Contains(eventArgs.Item))
            {
                SelectedItems.Remove(eventArgs.Item);
            }
            else
            {
                SelectedItems.Clear();
                SelectedItems.Add(eventArgs.Item);
            }

            SelectedTorrent = eventArgs.Item;

            if (eventArgs.MouseEventArgs.Detail > 1)
            {
                NavigationManager.NavigateTo("/details/" + SelectedTorrent);
            }
        }

        private IEnumerable<string> GetSelectedTorrents()
        {
            if (SelectedItems.Count > 0)
            {
                return SelectedItems.Select(t => t.Hash);
            }

            if (SelectedTorrent is not null)
            {
                return [SelectedTorrent.Hash];
            }

            return [];
        }

        protected void Options(MouseEventArgs eventArgs)
        {
            NavigationManager.NavigateTo("/options");
        }

        protected async Task ColumnOptions()
        {
            DialogParameters parameters = new DialogParameters
            {
                { "Columns", _columns }
            };

            var reference = await DialogService.ShowAsync<ColumnOptionsDialog<Torrent>>("ColumnOptions", parameters, DialogHelper.FormDialogOptions);

            var result = await reference.Result;
            if (result.Canceled)
            {
                return;
            }

            SelectedColumns = (HashSet<string>)result.Data;

            await LocalStorage.SetItemAsync(_columnSelectionStorageKey, SelectedColumns);
        }

        protected void ShowTorrent()
        {
            if (SelectedTorrent is null)
            {
                return;
            }
            NavigationManager.NavigateTo("/details/" + SelectedTorrent.Hash);
        }

        protected string RowStyle(Torrent torrent, int index)
        {
            var style = "user-select: none; cursor: pointer;";
            if (torrent == SelectedTorrent)
            {
                style += " background: #D3D3D3";
            }
            return style;
        }

        protected HashSet<string> SelectedColumns { get; set; } = new HashSet<string>();

        protected IEnumerable<ColumnDefinition<Torrent>> GetColumns()
        {
            return _columns.Where(c => SelectedColumns.Contains(c.Id));
        }

        private async Task SetSort(string columnId, SortDirection sortDirection)
        {
            _sortColumn = columnId;
            _sortDirection = sortDirection;

            await LocalStorage.SetItemAsync(_columnSortStorageKey, new Tuple<string, SortDirection>(columnId, sortDirection));
        }

        protected List<ColumnDefinition<Torrent>> _columns =
        [
            CreateColumnDefinition("#", t => t.Priority),
            CreateColumnDefinition("State Icon", t => t.State, IconColumn),
            CreateColumnDefinition("Name", t => t.Name, width: 200),
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
            CreateColumnDefinition("Completed", t => t.Completed, t => DisplayHelpers.DateTime(t.Completed), enabled: false),
            CreateColumnDefinition("Ratio Limit", t => t.RatioLimit, t => t.Ratio.ToString("0.00"), enabled: false),
            CreateColumnDefinition("Last Seen Complete", t => t.SeenComplete, t => DisplayHelpers.DateTime(t.SeenComplete), enabled: false),
            CreateColumnDefinition("Last Activity", t => t.LastActivity, t => DisplayHelpers.DateTime(t.LastActivity), enabled: false),
            CreateColumnDefinition("Availability", t => t.Availability, enabled: false),
            //CreateColumnDefinition("Reannounce In", t => t.Reannounce, enabled: false),
        ];

        private string? _sortColumn;
        private SortDirection _sortDirection;

        private static ColumnDefinition<Torrent> CreateColumnDefinition(string name, Func<Torrent, object?> selector, RenderFragment<RowContext<Torrent>> rowTemplate, int? width = null, string? tdClass = null, bool enabled = true)
        {
            var cd = new ColumnDefinition<Torrent>(name, selector, rowTemplate);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.Width = width;
            cd.Enabled = enabled;

            return cd;
        }

        private static ColumnDefinition<Torrent> CreateColumnDefinition(string name, Func<Torrent, object?> selector, Func<Torrent, string>? formatter = null, int? width = null, string? tdClass = null, bool enabled = true)
        {
            var cd = new ColumnDefinition<Torrent>(name, selector, formatter);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.Width = width;
            cd.Enabled = enabled;

            return cd;
        }
    }
}