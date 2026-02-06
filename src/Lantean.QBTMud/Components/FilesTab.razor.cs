using Lantean.QBitTorrentClient;
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
        private const string _expandedNodesStorageKey = "FilesTab.ExpandedNodes";

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _disposedValue;
        private static readonly ReadOnlyCollection<ContentItem> EmptyContentItems = new ReadOnlyCollection<ContentItem>(Array.Empty<ContentItem>());
        private ReadOnlyCollection<ContentItem> _visibleFiles = EmptyContentItems;
        private bool _filesDirty = true;

        private List<PropertyFilterDefinition<ContentItem>>? _filterDefinitions;
        private IReadOnlyList<ColumnDefinition<ContentItem>>? _columnDefinitions;

        private string? _previousHash;
        private string? _sortColumn;
        private SortDirection _sortDirection;

        [Parameter]
        public bool Active { get; set; }

        [Parameter, EditorRequired]
        public string Hash { get; set; } = "";

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected ISessionStorageService SessionStorage { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        protected HashSet<string> ExpandedNodes { get; set; } = [];

        protected Dictionary<string, ContentItem>? FileList { get; set; }

        protected IEnumerable<ContentItem> Files => GetFiles();

        protected ContentItem? SelectedItem { get; set; }

        protected ContentItem? ContextMenuItem { get; set; }

        protected string? SearchText { get; set; }

        public IEnumerable<Func<ContentItem, bool>>? Filters { get; set; }

        private DynamicTable<ContentItem>? Table { get; set; }

        private MudMenu? ContextMenu { get; set; }

        private MudTextField<string>? SearchInput { get; set; }

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
            var filterDefinitions = await DialogWorkflow.ShowFilterOptionsDialog(_filterDefinitions);
            if (filterDefinitions is null)
            {
                _filterDefinitions = null;
                Filters = null;
                MarkFilesDirty();
                return;
            }
            else
            {
                _filterDefinitions = filterDefinitions;
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
            if (!firstRender)
            {
                return;
            }

            _refreshTimer ??= ManagedTimerFactory.Create("FilesTabRefresh", TimeSpan.FromMilliseconds(RefreshInterval));
            await _refreshTimer.StartAsync(RefreshTickAsync, _timerCancellationToken.Token);
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
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
                    return ManagedTimerTickResult.Stop;
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

            if (cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            if (hasUpdates)
            {
                MarkFilesDirty();
                PruneSelectionIfMissing();
                await InvokeAsync(() =>
                {
                    SyncSearchTextFromInput();
                    StateHasChanged();
                });
            }

            return ManagedTimerTickResult.Continue;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (string.IsNullOrEmpty(Hash))
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

            var expandedNodes = await SessionStorage.GetItemAsync<HashSet<string>>($"{_expandedNodesStorageKey}.{Hash}");
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
                return RenameFiles();
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
            if (contentItems.Length == 1)
            {
                var contentItem = contentItems[0];
                var name = contentItem.GetFileName();
                await DialogWorkflow.InvokeStringFieldDialog(
                    WebUiLocalizer.Translate("TorrentContentTreeView", "Renaming"),
                    WebUiLocalizer.Translate("TorrentContentTreeView", "New name:"),
                    name,
                    async value => await ApiClient.RenameFile(Hash, contentItem.Name, contentItem.Path + value));
            }
            else
            {
                await DialogWorkflow.InvokeRenameFilesDialog(Hash);
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
            await SessionStorage.SetItemAsync($"{_expandedNodesStorageKey}.{Hash}", ExpandedNodes);
        }

        private static QBitTorrentClient.Models.Priority MapPriority(Priority priority)
        {
            return (QBitTorrentClient.Models.Priority)(int)priority;
        }

        private Func<ContentItem, object?> GetSortSelector()
        {
            var sortSelector = GetColumnDefinitions().FirstOrDefault(c => c.Id == _sortColumn)?.SortSelector;

            return sortSelector ?? (i => i.Name);
        }

        private IEnumerable<ContentItem> GetDescendants(ContentItem contentItem)
        {
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
            if (SyncSearchTextFromInput())
            {
                _filesDirty = true;
            }

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

        private bool SyncSearchTextFromInput()
        {
            if (SearchInput is null)
            {
                return false;
            }

            var currentValue = SearchInput.Value;
            if (string.Equals(SearchText, currentValue, StringComparison.Ordinal))
            {
                return false;
            }

            SearchText = currentValue;
            return true;
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

        private IReadOnlyList<ColumnDefinition<ContentItem>> GetColumnDefinitions()
        {
            _columnDefinitions ??= BuildColumnDefinitions();

            return _columnDefinitions;
        }

        private static string CreateKey(ContentItem item)
        {
            return item.Name.Replace("/", "_");
        }

        private IReadOnlyList<ColumnDefinition<ContentItem>> BuildColumnDefinitions()
        {
            var nameLabel = WebUiLocalizer.Translate("TransferListModel", "Name");
            var totalSizeLabel = WebUiLocalizer.Translate("TransferListModel", "Total Size");
            var progressLabel = WebUiLocalizer.Translate("TransferListModel", "Progress");
            var priorityLabel = WebUiLocalizer.Translate("PropertiesWidget", "Priority");
            var remainingLabel = WebUiLocalizer.Translate("TransferListModel", "Remaining");
            var availabilityLabel = WebUiLocalizer.Translate("TransferListModel", "Availability");

            return
            [
                ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>(
                    nameLabel,
                    c => c.Name,
                    NameColumn,
                    width: 400,
                    initialDirection: SortDirection.Ascending,
                    classFunc: c => c.IsFolder ? "pa-0" : "pa-2"),
                ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>(totalSizeLabel, c => c.Size, c => DisplayHelpers.Size(c.Size)),
                ColumnDefinitionHelper.CreateColumnDefinition(progressLabel, c => c.Progress, ProgressBarColumn, tdClass: "table-progress pl-2 pr-2"),
                ColumnDefinitionHelper.CreateColumnDefinition(priorityLabel, c => c.Priority, PriorityColumn, tdClass: "table-select pa-0"),
                ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>(remainingLabel, c => c.Remaining, c => DisplayHelpers.Size(c.Remaining)),
                ColumnDefinitionHelper.CreateColumnDefinition<ContentItem>(availabilityLabel, c => c.Availability, c => c.Availability.ToString("0.00")),
            ];
        }
    }
}
