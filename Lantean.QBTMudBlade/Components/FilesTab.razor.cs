﻿using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.Dialogs;
using Lantean.QBTMudBlade.Filter;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Net;

namespace Lantean.QBTMudBlade.Components
{
    public partial class FilesTab : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;
        private const string _expandedNodesStorageKey = "FilesTab.ExpandedNodes";

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        private List<PropertyFilterDefinition<ContentItem>>? _filterDefinitions;
        private readonly Dictionary<string, RenderFragment<RowContext<ContentItem>>> _columnRenderFragments = [];

        private string? _previousHash;
        private string? _sortColumn;
        private SortDirection _sortDirection;

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
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        protected HashSet<string> ExpandedNodes { get; set; } = [];

        protected Dictionary<string, ContentItem>? FileList { get; set; }

        protected IEnumerable<ContentItem> Files => GetFiles();

        protected ContentItem? SelectedItem { get; set; }

        protected string? SearchText { get; set; }

        public IEnumerable<Func<ContentItem, bool>>? Filters { get; set; }

        private DynamicTable<ContentItem>? Table { get; set; }

        

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
            if (dialogResult.Canceled)
            {
                return;
            }

            _filterDefinitions = (List<PropertyFilterDefinition<ContentItem>>?)dialogResult.Data;
            if (_filterDefinitions is null)
            {
                Filters = null;
                return;
            }

            var filters = new List<Func<ContentItem, bool>>();
            foreach (var filterDefinition in _filterDefinitions)
            {
                var expression = Filter.FilterExpressionGenerator.GenerateExpression(filterDefinition, false);
                filters.Add(expression.Compile());
            }

            Filters = filters;
        }

        protected void RemoveFilter()
        {
            Filters = null;
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

        protected void SearchTextChanged(string value)
        {
            SearchText = value;
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
                        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
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

            if (Hash == _previousHash)
            {
                return;
            }

            _previousHash = Hash;

            var contents = await ApiClient.GetTorrentContents(Hash);
            FileList = DataManager.CreateContentsList(contents);

            var expandedNodes = await LocalStorage.GetItemAsync<HashSet<string>>($"{_expandedNodesStorageKey}.{Hash}");
            if (expandedNodes is not null)
            {
                ExpandedNodes = expandedNodes;
            }
            else
            {
                ExpandedNodes.Clear();
            }
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

        protected async Task RenameFile()
        {
            if (Hash is null || FileList is null || SelectedItem is null)
            {
                return;
            }

            var contentItem = FileList.Values.FirstOrDefault(c => c.Index == SelectedItem.Index);
            if (contentItem is null)
            {
                return;
            }

            var name = contentItem.GetFileName();
            await DialogService.ShowSingleFieldDialog("Rename", "New name", name, async value => await ApiClient.RenameFile(Hash, contentItem.Name, contentItem.Path + value));
        }

        protected void SortColumnChanged(string sortColumn)
        {
            _sortColumn = sortColumn;
        }

        protected void SortDirectionChanged(SortDirection sortDirection)
        {
            _sortDirection = sortDirection;
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

        private IEnumerable<ContentItem> GetChildren(ContentItem folder, int level)
        {
            level++;
            var descendantsKey = folder.GetDescendantsKey(level);

            foreach (var item in FileList!.Values.Where(f => f.Name.StartsWith(descendantsKey) && f.Level == level).OrderByDirection(_sortDirection, GetSortSelector()))
            {
                if (item.IsFolder)
                {
                    var descendants = GetChildren(item, level);
                    // if the filter returns some results then show folder item
                    if (descendants.Any())
                    {
                        yield return item;
                    }

                    // if the folder is not expanded - don't return children
                    if (!ExpandedNodes.Contains(item.Name))
                    {
                        continue;
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
            if (FileList is null || FileList.Values.Count == 0)
            {
                return new ReadOnlyCollection<ContentItem>([]);
            }

            var maxLevel = FileList.Values.Max(f => f.Level);
            // this is a flat file structure
            if (maxLevel == 0)
            {
                return FileList.Values.Where(FilterContentItem).OrderByDirection(_sortDirection, GetSortSelector()).ToList().AsReadOnly();
            }

            var list = new List<ContentItem>();

            var rootItems = FileList.Values.Where(c => c.Level == 0).OrderByDirection(_sortDirection, GetSortSelector()).ToList();
            foreach (var item in rootItems)
            {
                list.Add(item);

                if (item.IsFolder && ExpandedNodes.Contains(item.Name))
                {
                    var level = 0;
                    var descendants = GetChildren(item, level);
                    foreach (var descendant in descendants)
                    {
                        list.Add(descendant);
                    }
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
            CreateColumnDefinition("Name", c => c.Name, width: 400, initialDirection: SortDirection.Ascending, classFunc: c => c.IsFolder ? "pa-0" : "pa-3"),
            CreateColumnDefinition("Total Size", c => c.Size, c => DisplayHelpers.Size(c.Size)),
            CreateColumnDefinition("Progress", c => c.Progress, ProgressBarColumn, tdClass: "table-progress pl-2 pr-2"),
            CreateColumnDefinition("Priority", c => c.Priority, tdClass: "table-select pa-0"),
            CreateColumnDefinition("Remaining", c => c.Remaining, c => DisplayHelpers.Size(c.Remaining)),
            CreateColumnDefinition("Availability", c => c.Availability, c => c.Availability.ToString("0.00")),
        ];

        private static ColumnDefinition<ContentItem> CreateColumnDefinition(string name, Func<ContentItem, object?> selector, RenderFragment<RowContext<ContentItem>> rowTemplate, int? width = null, string? tdClass = null, Func<ContentItem, string?>? classFunc = null, bool enabled = true, SortDirection initialDirection = SortDirection.None)
        {
            var cd = new ColumnDefinition<ContentItem>(name, selector, rowTemplate);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.ClassFunc = classFunc;
            cd.Width = width;
            cd.Enabled = enabled;
            cd.InitialDirection = initialDirection;

            return cd;
        }

        private static ColumnDefinition<ContentItem> CreateColumnDefinition(string name, Func<ContentItem, object?> selector, Func<ContentItem, string>? formatter = null, int? width = null, string? tdClass = null, Func<ContentItem, string?>? classFunc = null, bool enabled = true, SortDirection initialDirection = SortDirection.None)
        {
            var cd = new ColumnDefinition<ContentItem>(name, selector, formatter);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.ClassFunc = classFunc;
            cd.Width = width;
            cd.Enabled = enabled;
            cd.InitialDirection = initialDirection;

            return cd;
        }
    }
}