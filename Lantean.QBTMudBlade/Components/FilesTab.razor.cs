using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.Dialogs;
using Lantean.QBTMudBlade.Filter;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Net;

namespace Lantean.QBTMudBlade.Components
{
    public partial class FilesTab : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;

        private const string _columnStorageKey = "FilesTab.Columns";

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        private Func<ContentItem, object?> SortSelector { get; set; } = c => c.Name;

        private SortDirection SortDirection { get; set; } = SortDirection.Ascending;

        [Parameter]
        public bool Active { get; set; }

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [CascadingParameter]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        protected HashSet<string> ExpandedNodes { get; set; } = [];

        protected Dictionary<string, ContentItem>? FileList { get; set; }

        protected IEnumerable<ContentItem> Files => GetFiles();

        protected HashSet<ContentItem> SelectedItems { get; set; } = [];

        protected List<ColumnDefinition<ContentItem>> _columns = [];

        protected ContentItem? SelectedItem { get; set; }

        protected string? SearchText { get; set; }

        protected int? _selectedIndex { get; set; }

        protected HashSet<string> SelectedColumns { get; set; }

        public IEnumerable<Func<ContentItem, bool>>? Filters { get; set; }

        public FilesTab()
        {
            _columns.Add(CreateColumnDefinition("Name", c => c.Name, NameColumn, width: 200, initialDirection: SortDirection.Ascending));
            _columns.Add(CreateColumnDefinition("Total Size", c => c.Size, c => DisplayHelpers.Size(c.Size)));
            _columns.Add(CreateColumnDefinition("Progress", c => c.Progress, c => DisplayHelpers.Percentage(c.Progress)));
            _columns.Add(CreateColumnDefinition("Priority", c => c.Priority, PriorityColumn));
            _columns.Add(CreateColumnDefinition("Remaining", c => c.Remaining, c => DisplayHelpers.Size(c.Remaining)));
            _columns.Add(CreateColumnDefinition("Availability", c => c.Availability, c => c.Availability.ToString("0.00")));

            SelectedColumns = _columns.Where(c => c.Enabled).Select(c => c.Id).ToHashSet();
        }

        protected override async Task OnInitializedAsync()
        {
            if (!await LocalStorage.ContainKeyAsync(_columnStorageKey))
            {
                return;
            }

            var selectedColumns = await LocalStorage.GetItemAsync<HashSet<string>>(_columnStorageKey);
            if (selectedColumns is null)
            {
                return;
            }

            SelectedColumns = selectedColumns;
        }

        protected IEnumerable<ColumnDefinition<ContentItem>> GetColumns()
        {
            return _columns.Where(c => SelectedColumns.Contains(c.Id));
        }

        protected async Task ColumnOptions()
        {
            DialogParameters parameters = new DialogParameters
            {
                { "Columns", _columns }
            };

            var reference = await DialogService.ShowAsync<ColumnOptionsDialog<ContentItem>>("ColumnOptions", parameters, DialogHelper.FormDialogOptions);

            var result = await reference.Result;
            if (result.Canceled)
            {
                return;
            }

            SelectedColumns = (HashSet<string>)result.Data;

            await LocalStorage.SetItemAsync(_columnStorageKey, SelectedColumns);
        }

        protected async Task ShowFilterDialog()
        {
            var parameters = new DialogParameters
            {
                { nameof(FilterOptionsDialog<ContentItem>.FilterDefinitions), Filters },
            };

            var result = await DialogService.ShowAsync<FilterOptionsDialog<ContentItem>>("Filters", parameters, DialogHelper.FormDialogOptions);

            var dialogResult = await result.Result;
            if (dialogResult.Canceled)
            {
                return;
            }

            var filterDefinitions = (List<PropertyFilterDefinition<ContentItem>>?)dialogResult.Data;
            if (filterDefinitions is null)
            {
                return;
            }

            var filters = new List<Func<ContentItem, bool>>();
            foreach (var filterDefinition in filterDefinitions)
            {
                var expression = Filter.FilterExpressionGenerator.GenerateExpression(filterDefinition, false);
                filters.Add(expression.Compile());
            }

            Filters = filters;
        }

        protected async Task RemoveFilter()
        {
            Filters = null;
            await InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected static float CalculateProgress(IEnumerable<ContentItem> items)
        {
            return (float)items.Sum(i => i.Downloaded) / items.Sum(i => i.Size);
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

        protected async Task SearchTextChanged(string value)
        {
            SearchText = value;
            await InvokeAsync(StateHasChanged);
            if (FileList is null)
            {
                return;
            }
            SelectedItems = FileList.Values.Where(f => f.Priority != Priority.DoNotDownload).ToHashSet();
        }

        protected async Task EnabledValueChanged(ContentItem contentItem, bool value)
        {
            if (Hash is null)
            {
                return;
            }

            await ApiClient.SetFilePriority(Hash, [contentItem.Index], MapPriority(value ? Priority.Normal : Priority.DoNotDownload));
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
                    if (Active && Hash is not null)
                    {
                        IReadOnlyList<QBitTorrentClient.Models.FileData> files;
                        try
                        {
                            files = await ApiClient.GetTorrentContents(Hash);
                        }
                        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
                        {
                            _timerCancellationToken.CancelIfNotDisposed();
                            return;
                        }

                        if (FileList is null)
                        {
                            FileList = DataManager.CreateContentsList(files);
                        }
                        else
                        {
                            DataManager.MergeContentsList(files, FileList);
                        }
                    }

                    await InvokeAsync(StateHasChanged);
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

            var contents = await ApiClient.GetTorrentContents(Hash);
            FileList = DataManager.CreateContentsList(contents);

            SelectedItems = FileList.Values.Where(f => f.Priority != Priority.DoNotDownload).ToHashSet();
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
                fileIndexes = GetChildren(contentItem).Where(c => !c.IsFolder).Select(c => c.Index);
            }
            else
            {
                fileIndexes = [contentItem.Index];
            }

            await ApiClient.SetFilePriority(Hash, fileIndexes, MapPriority(priority));
        }

        protected string RowClass(ContentItem contentItem, int index)
        {
            if (contentItem.Level == 0)
            {
                return "d-table-row";
            }
            if (ExpandedNodes.Contains(contentItem.Path))
            {
                return "d-table-row";
            }
            return "d-none";
        }

        protected async Task RenameFile()
        {
            if (Hash is null || FileList is null || _selectedIndex is null)
            {
                return;
            }
            var contentItem = FileList.Values.FirstOrDefault(c => c.Index == _selectedIndex.Value);
            if (contentItem is null)
            {
                return;
            }
            var name = contentItem.GetFileName();
            await DialogService.ShowSingleFieldDialog("Rename", "New name", name, async v => await ApiClient.RenameFile(Hash, contentItem.Name, contentItem.Path + v));
        }

        protected void RowClick(TableRowClickEventArgs<ContentItem> eventArgs)
        {
            _selectedIndex = eventArgs.Item.Index;
        }

        protected string RowStyle(ContentItem item, int index)
        {
            var style = "user-select: none; cursor: pointer;";
            if (_selectedIndex != item.Index)
            {
                return style;
            }
            return $"{style} background: #D3D3D3";
        }

        protected async Task SelectedItemsChanged(HashSet<ContentItem> selectedItems)
        {
            if (Hash is null || Files is null)
            {
                return;
            }

            var unselectedItems = Files.Except(SelectedItems);

            if (unselectedItems.Any())
            {
                await ApiClient.SetFilePriority(Hash, unselectedItems.Select(c => c.Index), QBitTorrentClient.Models.Priority.DoNotDownload);

                foreach (var item in unselectedItems)
                {
                    Files.First(f => f == item).Priority = Priority.DoNotDownload;
                }

                await InvokeAsync(StateHasChanged);
            }

            var existingDoNotDownloads = Files.Where(f => f.Priority == Priority.DoNotDownload);
            var newlySelectedFiles = selectedItems.Where(f => existingDoNotDownloads.Contains(f));

            if (newlySelectedFiles.Any())
            {
                await ApiClient.SetFilePriority(Hash, newlySelectedFiles.Select(c => c.Index), QBitTorrentClient.Models.Priority.Normal);

                foreach (var item in newlySelectedFiles)
                {
                    Files.First(f => f == item).Priority = Priority.Normal;
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetSort(Func<ContentItem, object?> sortSelector, SortDirection sortDirection)
        {
            SortSelector = sortSelector;
            SortDirection = sortDirection;
        }

        protected void ToggleNode(ContentItem contentItem, MouseEventArgs args)
        {
            if (ExpandedNodes.Contains(contentItem.Name))
            {
                ExpandedNodes.Remove(contentItem.Name);
            }
            else
            {
                ExpandedNodes.Add(contentItem.Name);
            }
        }

        private static QBitTorrentClient.Models.Priority MapPriority(Priority priority)
        {
            return (QBitTorrentClient.Models.Priority)(int)priority;
        }

        private IEnumerable<ContentItem> GetChildren(ContentItem contentItem)
        {
            if (!contentItem.IsFolder || Files is null)
            {
                return [];
            }

            return Files.Where(f => f.Name.StartsWith(contentItem.Name + Extensions.DirectorySeparator) && !f.IsFolder);
        }

        private IEnumerable<ContentItem> GetDescendants(ContentItem folder, int level)
        {
            level++;
            var descendantsKey = folder.GetDescendantsKey(level);
            foreach (var item in FileList!.Values.Where(f => f.Name.StartsWith(descendantsKey)).OrderByDirection(SortDirection, SortSelector))
            {
                if (item.IsFolder)
                {
                    var descendants = GetDescendants(item, level);
                    // if the filter returns some resutls then show folder item
                    if (descendants.Any())
                    {
                        yield return item;
                    }
                    // then show children
                    foreach (var descendant in descendants)
                    {
                        yield return descendant;
                    }
                }
                else
                {
                    if (FilterContentItem(item))
                    {
                        yield return item;
                    }
                }
            }
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
            if (FileList is null)
            {
                return new ReadOnlyCollection<ContentItem>([]);
            }

            var maxLevel = FileList.Values.Max(f => f.Level);
            // this is a flat file structure
            if (maxLevel == 0)
            {
                return FileList.Values.Where(FilterContentItem).OrderByDirection(SortDirection, SortSelector).ToList().AsReadOnly();
            }

            var list = new List<ContentItem>();

            var folders = FileList.Values.Where(c => c.IsFolder && c.Level == 0).OrderByDirection(SortDirection, SortSelector).ToList();
            foreach (var folder in folders)
            {
                list.Add(folder);
                var level = 0;
                var descendants = GetDescendants(folder, level);
                foreach (var descendant in descendants)
                {
                    list.Add(descendant);
                }
            }

            return list.AsReadOnly();
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

            var files = FileList.Values.Where(f => f.Availability < value).Select(f => f.Index);

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

            var files = GetFiles().Select(f => f.Index);

            if (!files.Any())
            {
                return;
            }

            await ApiClient.SetFilePriority(Hash, files, priority);
        }

        private static ColumnDefinition<ContentItem> CreateColumnDefinition(string name, Func<ContentItem, object?> selector, RenderFragment<RowContext<ContentItem>> rowTemplate, int? width = null, string? tdClass = null, bool enabled = true, SortDirection initialDirection = SortDirection.None)
        {
            var cd = new ColumnDefinition<ContentItem>(name, selector, rowTemplate);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.Width = width;
            cd.Enabled = enabled;
            cd.InitialDirection = initialDirection;

            return cd;
        }

        private static ColumnDefinition<ContentItem> CreateColumnDefinition(string name, Func<ContentItem, object?> selector, Func<ContentItem, string>? formatter = null, int? width = null, string? tdClass = null, bool enabled = true, SortDirection initialDirection = SortDirection.None)
        {
            var cd = new ColumnDefinition<ContentItem>(name, selector, formatter);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.Width = width;
            cd.Enabled = enabled;
            cd.InitialDirection = initialDirection;

            return cd;
        }
    }
}