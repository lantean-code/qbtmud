using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Net;

namespace Lantean.QBTMud.Components
{
    public partial class FilesTab : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;
        private const string _expandedNodesStorageKey = "FilesTab.ExpandedNodes";

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;
        private static readonly ReadOnlyCollection<ContentItem> EmptyContentItems = new ReadOnlyCollection<ContentItem>(Array.Empty<ContentItem>());
        private ReadOnlyCollection<ContentItem> _visibleFiles = EmptyContentItems;
        private bool _filesDirty = true;

        private List<PropertyFilterDefinition<ContentItem>>? _filterDefinitions;
        private readonly Dictionary<string, RenderFragment<RowContext<ContentItem>>> _columnRenderFragments = [];

        private string? _previousHash;
        private string? _sortColumn;
        private SortDirection _sortDirection;

        [Parameter]
        public bool Active { get; set; }

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        protected HashSet<string> ExpandedNodes { get; set; } = [];

        protected Dictionary<string, ContentItem>? FileList { get; set; }

        protected IEnumerable<ContentItem> Files => GetFiles();

        protected ContentItem? SelectedItem { get; set; }

        protected ContentItem? ContextMenuItem { get; set; }

        protected string? SearchText { get; set; }

        public IEnumerable<Func<ContentItem, bool>>? Filters { get; set; }

        private DynamicTable<ContentItem>? Table { get; set; }

        private MudMenu? ContextMenu { get; set; }

        public FilesTab()
        {
            _columnRenderFragments.Add("Name", NameColumn);
            _columnRenderFragments.Add("Priority", PriorityColumn);
        }

        protected async Task ColumnOptions()
        {
            if (Table is null)
            {
                return;
            }

            await Table.ShowColumnOptionsDialog();
        }

        protected async Task ShowFilterDialog()
        {
            var parameters = new DialogParameters
            {
                { nameof(FilterOptionsDialog<ContentItem>.FilterDefinitions), _filterDefinitions },
            };

            var result = await DialogService.ShowAsync<FilterOptionsDialog<ContentItem>>("Filters", parameters, DialogHelper.FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            _filterDefinitions = (List<PropertyFilterDefinition<ContentItem>>?)dialogResult.Data;
            if (_filterDefinitions is null)
            {
                Filters = null;
                MarkFilesDirty();
                return;
            }

            var filters = new List<Func<ContentItem, bool>>();
            foreach (var filterDefinition in _filterDefinitions)
            {
                var expression = Filter.FilterExpressionGenerator.GenerateExpression(filterDefinition, false);
                filters.Add(expression.Compile());
            }

            Filters = filters;
            MarkFilesDirty();
        }

        protected void RemoveFilter()
        {
            Filters = null;
            MarkFilesDirty();
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
                if (disposing && Files is not null)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();

                    await Task.CompletedTask;
                }

                _disposedValue = true;
            }
        }

        protected static Priority GetPriority(IEnumerable<ContentItem> items)
        {
            var distinctPriorities = items.Select(i => i.Priority).Distinct();
            if (distinctPriorities.Count() == 1)
            {
                return distinctPriorities.First();
            }

            return Priority.Mixed;
        }

        protected void SearchTextChanged(string value)
        {
            SearchText = value;
            MarkFilesDirty();
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<ContentItem> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<ContentItem> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        private async Task ShowContextMenu(ContentItem? contentItem, EventArgs eventArgs)
        {
            ContextMenuItem = contentItem;

            if (ContextMenu is null)
            {
                return;
            }

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu.OpenMenuAsync(normalizedEventArgs);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (!firstRender)
            {
                return;
            }

            using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RefreshInterval)))
            {
                while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                {
                    var hasUpdates = false;
                    if (Active && Hash is not null)
                    {
                        IReadOnlyList<QBitTorrentClient.Models.FileData> files;
                        try
                        {
                            files = await ApiClient.GetTorrentContents(Hash);
                        }
                        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
                        {
                            _timerCancellationToken.CancelIfNotDisposed();
                            return;
                        }

                        if (FileList is null)
                        {
                            FileList = DataManager.CreateContentsList(files);
                            hasUpdates = true;
                        }
                        else
                        {
                            hasUpdates = DataManager.MergeContentsList(files, FileList);
                        }
                    }

                    if (hasUpdates)
                    {
                        MarkFilesDirty();
                        PruneSelectionIfMissing();
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

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

            if (Hash == _previousHash)
            {
                return;
            }

            _previousHash = Hash;

            var contents = await ApiClient.GetTorrentContents(Hash);
            FileList = DataManager.CreateContentsList(contents);
            MarkFilesDirty();
            PruneSelectionIfMissing();

            var expandedNodes = await LocalStorage.GetItemAsync<HashSet<string>>($"{_expandedNodesStorageKey}.{Hash}");
            if (expandedNodes is not null)
            {
                ExpandedNodes = expandedNodes;
            }
            else
            {
                ExpandedNodes.Clear();
            }

            MarkFilesDirty();
        }

        protected async Task PriorityValueChanged(ContentItem contentItem, Priority priority)
        {
            if (Hash is null)
            {
                return;
            }

            IEnumerable<int> fileIndexes;
            if (contentItem.IsFolder)
            {
                fileIndexes = GetDescendants(contentItem).Where(c => !c.IsFolder).Select(c => c.Index);
            }
            else
            {
                fileIndexes = [contentItem.Index];
            }

            await ApiClient.SetFilePriority(Hash, fileIndexes, MapPriority(priority));
        }

        protected Task RenameFileToolbar()
        {
            if (SelectedItem is null)
            {
                return Task.CompletedTask;
            }

            return RenameFiles(SelectedItem);
        }

        protected Task RenameFileContextMenu()
        {
            if (ContextMenuItem is null)
            {
                return Task.CompletedTask;
            }

            return RenameFiles(ContextMenuItem);
        }

        private async Task RenameFiles(params ContentItem[] contentItems)
        {
            if (Hash is null || contentItems.Length == 0)
            {
                return;
            }

            if (contentItems.Length == 1)
            {
                var contentItem = contentItems[0];
                var name = contentItem.GetFileName();
                await DialogService.InvokeStringFieldDialog("Rename", "New name", name, async value => await ApiClient.RenameFile(Hash, contentItem.Name, contentItem.Path + value));
            }
            else
            {
                await DialogService.InvokeRenameFilesDialog(Hash);
            }
        }

        protected void SortColumnChanged(string sortColumn)
        {
            _sortColumn = sortColumn;
            MarkFilesDirty();
        }

        protected void SortDirectionChanged(SortDirection sortDirection)
        {
            _sortDirection = sortDirection;
            MarkFilesDirty();
        }

        protected void SelectedItemChanged(ContentItem item)
        {
            SelectedItem = item;
        }

        protected async Task ToggleNode(ContentItem contentItem)
        {
            if (ExpandedNodes.Contains(contentItem.Name))
            {
                ExpandedNodes.Remove(contentItem.Name);
            }
            else
            {
                ExpandedNodes.Add(contentItem.Name);
            }

            MarkFilesDirty();
            await LocalStorage.SetItemAsync($"{_expandedNodesStorageKey}.{Hash}", ExpandedNodes);
        }

        private static QBitTorrentClient.Models.Priority MapPriority(Priority priority)
        {
            return (QBitTorrentClient.Models.Priority)(int)priority;
        }

        private Func<ContentItem, object?> GetSortSelector()
        {
            var sortSelector = ColumnsDefinitions.Find(c => c.Id == _sortColumn)?.SortSelector;

            return sortSelector ?? (i => i.Name);
        }

        private IEnumerable<ContentItem> GetDescendants(ContentItem contentItem)
        {
            if (!contentItem.IsFolder || Files is null)
            {
                return [];
            }

            return FileList!.Values.Where(f => f.Name.StartsWith(contentItem.Name + Extensions.DirectorySeparator) && !f.IsFolder);
        }

        private bool FilterContentItem(ContentItem item)
        {
            if (Filters is not null)
            {
                foreach (var filter in Filters)
                {
                    var result = filter(item);
                    if (!result)
                    {
                        return false;
                    }
                }
            }

            if (!FilterHelper.FilterTerms(item.Name, SearchText))
            {
                return false;
            }

            return true;
        }

        private ReadOnlyCollection<ContentItem> GetFiles()
        {
            if (!_filesDirty)
            {
                return _visibleFiles;
            }

            _visibleFiles = BuildVisibleFiles();
            _filesDirty = false;

            return _visibleFiles;
        }

        private ReadOnlyCollection<ContentItem> BuildVisibleFiles()
        {
            if (FileList is null || FileList.Values.Count == 0)
            {
                return EmptyContentItems;
            }

            var lookup = BuildChildrenLookup();
            if (!lookup.TryGetValue(string.Empty, out var roots))
            {
                return EmptyContentItems;
            }

            var sortSelector = GetSortSelector();
            var orderedRoots = roots.OrderByDirection(_sortDirection, sortSelector).ToList();
            var result = new List<ContentItem>(FileList.Values.Count);

            foreach (var item in orderedRoots)
            {
                if (item.IsFolder)
                {
                    result.Add(item);

                    if (!ExpandedNodes.Contains(item.Name))
                    {
                        continue;
                    }

                    var descendants = GetVisibleDescendants(item, lookup, sortSelector);
                    result.AddRange(descendants);
                }
                else
                {
                    if (FilterContentItem(item))
                    {
                        result.Add(item);
                    }
                }
            }

            return new ReadOnlyCollection<ContentItem>(result);
        }

        private Dictionary<string, List<ContentItem>> BuildChildrenLookup()
        {
            var lookup = new Dictionary<string, List<ContentItem>>(FileList!.Count);

            foreach (var item in FileList!.Values)
            {
                var parentPath = item.Level == 0 ? string.Empty : item.Name.GetDirectoryPath();
                if (!lookup.TryGetValue(parentPath, out var children))
                {
                    children = [];
                    lookup[parentPath] = children;
                }

                children.Add(item);
            }

            return lookup;
        }

        private List<ContentItem> GetVisibleDescendants(ContentItem folder, Dictionary<string, List<ContentItem>> lookup, Func<ContentItem, object?> sortSelector)
        {
            if (!lookup.TryGetValue(folder.Name, out var children))
            {
                return [];
            }

            var orderedChildren = children.OrderByDirection(_sortDirection, sortSelector).ToList();
            var visible = new List<ContentItem>();

            foreach (var child in orderedChildren)
            {
                if (child.IsFolder)
                {
                    var descendants = GetVisibleDescendants(child, lookup, sortSelector);
                    if (descendants.Count != 0)
                    {
                        visible.Add(child);

                        if (ExpandedNodes.Contains(child.Name))
                        {
                            visible.AddRange(descendants);
                        }
                    }
                }
                else if (FilterContentItem(child))
                {
                    visible.Add(child);
                }
            }

            return visible;
        }

        private void MarkFilesDirty()
        {
            _filesDirty = true;
        }

        private void PruneSelectionIfMissing()
        {
            if (SelectedItem is not null && (FileList is null || !FileList.ContainsKey(SelectedItem.Name)))
            {
                SelectedItem = null;
            }

            if (ContextMenuItem is not null && (FileList is null || !FileList.ContainsKey(ContextMenuItem.Name)))
            {
                ContextMenuItem = null;
            }
        }

        protected async Task DoNotDownloadLessThan100PercentAvailability()
        {
            await LessThanXAvailability(1f, QBitTorrentClient.Models.Priority.DoNotDownload);
        }

        protected async Task DoNotDownloadLessThan80PercentAvailability()
        {
            await LessThanXAvailability(0.8f, QBitTorrentClient.Models.Priority.DoNotDownload);
        }

        protected async Task DoNotDownloadCurrentlyFilteredFiles()
        {
            await CurrentlyFilteredFiles(QBitTorrentClient.Models.Priority.DoNotDownload);
        }

        protected async Task NormalPriorityLessThan100PercentAvailability()
        {
            await LessThanXAvailability(1f, QBitTorrentClient.Models.Priority.Normal);
        }

        protected async Task NormalPriorityLessThan80PercentAvailability()
        {
            await LessThanXAvailability(0.8f, QBitTorrentClient.Models.Priority.Normal);
        }

        protected async Task NormalPriorityCurrentlyFilteredFiles()
        {
            await CurrentlyFilteredFiles(QBitTorrentClient.Models.Priority.Normal);
        }

        private async Task LessThanXAvailability(float value, QBitTorrentClient.Models.Priority priority)
        {
            if (Hash is null || FileList is null)
            {
                return;
            }

            var files = FileList.Values.Where(f => !f.IsFolder && f.Availability < value).Select(f => f.Index);

            if (!files.Any())
            {
                return;
            }

            await ApiClient.SetFilePriority(Hash, files, priority);
        }

        protected async Task CurrentlyFilteredFiles(QBitTorrentClient.Models.Priority priority)
        {
            if (Hash is null || FileList is null)
            {
                return;
            }

            var files = GetFiles().Where(f => !f.IsFolder).Select(f => f.Index);

            if (!files.Any())
            {
                return;
            }

            await ApiClient.SetFilePriority(Hash, files, priority);
        }

        protected IEnumerable<ColumnDefinition<ContentItem>> Columns => GetColumnDefinitions();

        private IEnumerable<ColumnDefinition<ContentItem>> GetColumnDefinitions()
        {
            foreach (var columnDefinition in ColumnsDefinitions)
            {
                if (_columnRenderFragments.TryGetValue(columnDefinition.Header, out var fragment))
                {
                    columnDefinition.RowTemplate = fragment;
                }

                yield return columnDefinition;
            }
        }

        public static List<ColumnDefinition<ContentItem>> ColumnsDefinitions { get; } =
        [
            ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>("Name", c => c.Name, width: 400, initialDirection: SortDirection.Ascending, classFunc: c => c.IsFolder ? "pa-0" : "pa-2"),
            ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>("Total Size", c => c.Size, c => DisplayHelpers.Size(c.Size)),
            ColumnDefinitionHelper.CreateColumnDefinition("Progress", c => c.Progress, ProgressBarColumn, tdClass: "table-progress pl-2 pr-2"),
            ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>("Priority", c => c.Priority, tdClass: "table-select pa-0"),
            ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>("Remaining", c => c.Remaining, c => DisplayHelpers.Size(c.Remaining)),
            ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>("Availability", c => c.Availability, c => c.Availability.ToString("0.00")),
        ];
    }
}