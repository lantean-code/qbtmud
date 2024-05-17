using Blazored.LocalStorage;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Data.Common;

namespace Lantean.QBTMudBlade.Components
{
    public partial class DynamicTable<T> : MudComponentBase
    {
        private static readonly string _typeName = typeof(T).Name;
        private readonly string _columnSelectionStorageKey = $"DynamicTable{_typeName}.ColumnSelection";
        private readonly string _columnSortStorageKey = $"DynamicTable{_typeName}.ColumnSort";
        private readonly string _columnWidthsStorageKey = $"DynamicTable{_typeName}.ColumnWidths";

        [Inject]
        public ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IEnumerable<ColumnDefinition<T>> ColumnDefinitions { get; set; } = [];

        [Parameter]
        [EditorRequired]
        public IEnumerable<T>? Items { get; set; }

        [Parameter]
        public bool MultiSelection { get; set; }

        [Parameter]
        public bool Striped { get; set; }

        [Parameter]
        public bool Hover { get; set; }

        [Parameter]
        public bool PreSorted { get; set; }

        [Parameter]
        public bool SelectOnRowClick { get; set; }

        [Parameter]
        public EventCallback<TableRowClickEventArgs<T>> OnRowClick { get; set; }

        [Parameter]
        public HashSet<T> SelectedItems { get; set; } = [];

        [Parameter]
        public EventCallback<HashSet<T>> SelectedItemsChanged { get; set; }

        [Parameter]
        public EventCallback<T> SelectedItemChanged { get; set; }

        [Parameter]
        public T? SelectedItem { get; set; }

        [Parameter]
        public Func<ColumnDefinition<T>, bool> ColumnFilter { get; set; } = (t => true);

        [Parameter]
        public EventCallback<string> SortColumnChanged { get; set; }

        [Parameter]
        public EventCallback<SortDirection> SortDirectionChanged { get; set; }

        [Parameter]
        public EventCallback<HashSet<string>> SelectedColumnsChanged { get; set; }

        protected IEnumerable<T>? OrderedItems => GetOrderedItems();

        protected HashSet<string> SelectedColumns { get; set; } = [];

        private Dictionary<string, int?> _columnWidths = [];

        private MudTable<T>? Table { get; set; }

        private string? _sortColumn;

        private SortDirection _sortDirection;

        protected override async Task OnInitializedAsync()
        {
            HashSet<string> selectedColumns;
            var storedSelectedColumns = await LocalStorage.GetItemAsync<HashSet<string>>(_columnSelectionStorageKey);
            if (storedSelectedColumns is not null)
            {
                selectedColumns = storedSelectedColumns;
            }
            else
            {
                selectedColumns = ColumnDefinitions.Where(c => c.Enabled).Select(c => c.Id).ToHashSet();
            }

            if (!SelectedColumns.SetEquals(selectedColumns))
            {
                SelectedColumns = selectedColumns;
                await SelectedColumnsChanged.InvokeAsync(SelectedColumns);
            }

            string? sortColumn;
            SortDirection sortDirection;

            var storedColumnSort = await LocalStorage.GetItemAsync<Tuple<string, SortDirection>>(_columnSortStorageKey);
            if (storedColumnSort is not null)
            {
                sortColumn = storedColumnSort.Item1;
                sortDirection = storedColumnSort.Item2;
            }
            else
            {
                sortColumn = ColumnDefinitions.First(c => c.Enabled).Id;
                sortDirection = SortDirection.Ascending;
            }

            if (_sortColumn != sortColumn)
            {
                _sortColumn = sortColumn;
                await SortColumnChanged.InvokeAsync(_sortColumn);
            }

            if (_sortDirection != sortDirection)
            {
                _sortDirection = sortDirection;
                await SortDirectionChanged.InvokeAsync(_sortDirection);
            }

            var storedColumnsWidths = await LocalStorage.GetItemAsync<Dictionary<string, int?>>(_columnWidthsStorageKey);
            if (storedColumnsWidths is not null)
            {
                _columnWidths = storedColumnsWidths;
            }
        }

        private IEnumerable<T>? GetOrderedItems()
        {
            if (Items is null)
            {
                return null;
            }

            if (PreSorted)
            {
                return Items;
            }

            var sortSelector = ColumnDefinitions.FirstOrDefault(c => c.Id == _sortColumn)?.SortSelector;
            if (sortSelector is null)
            {
                return Items;
            }

            return Items.OrderByDirection(_sortDirection, sortSelector);
        }

        protected IEnumerable<ColumnDefinition<T>> GetColumns()
        {
            var filteredColumns = ColumnDefinitions.Where(c => SelectedColumns.Contains(c.Id)).Where(ColumnFilter);
            foreach (var column in filteredColumns)
            {
                if (_columnWidths.TryGetValue(column.Id, out var value))
                {
                    column.Width = value;
                }

                yield return column;
            }
        }

        private async Task SetSort(string columnId, SortDirection sortDirection)
        {
            if (_sortColumn != columnId)
            {
                _sortColumn = columnId;
                await SortColumnChanged.InvokeAsync(_sortColumn);
            }

            if (_sortDirection != sortDirection)
            {
                _sortDirection = sortDirection;
                await SortDirectionChanged.InvokeAsync(_sortDirection);
            }

            await LocalStorage.SetItemAsync(_columnSortStorageKey, new Tuple<string, SortDirection>(columnId, sortDirection));
        }

        protected async Task OnRowClickInternal(TableRowClickEventArgs<T> eventArgs)
        {
            SelectedItem = eventArgs.Item;

            await SelectedItemChanged.InvokeAsync(SelectedItem);
            await OnRowClick.InvokeAsync(eventArgs);
        }

        protected string RowStyleFuncInternal(T item, int index)
        {
            var style = "user-select: none; cursor: pointer;";
            if (EqualityComparer<T>.Default.Equals(item, SelectedItem))
            {
                style += " background-color: var(--mud-palette-dark-darken)";
            }
            return style;
        }

        protected async Task SelectedItemsChangedInternal(HashSet<T> selectedItems)
        {
            await SelectedItemsChanged.InvokeAsync(selectedItems);
        }

        public async Task ShowColumnOptionsDialog()
        {
            var result = await DialogService.ShowColumnsOptionsDialog(ColumnDefinitions.Where(ColumnFilter).ToList(), _columnWidths);

            if (result == default)
            {
                return;
            }

            if (!SelectedColumns.SetEquals(result.SelectedColumns))
            {
                SelectedColumns = result.SelectedColumns;
                await SelectedColumnsChanged.InvokeAsync(SelectedColumns);
                await LocalStorage.SetItemAsync(_columnSelectionStorageKey, SelectedColumns);
            }

            if (!DictionaryEqual(_columnWidths, result.ColumnWidths))
            {
                _columnWidths = result.ColumnWidths;
                await LocalStorage.SetItemAsync(_columnWidthsStorageKey, _columnWidths);
            }
        }

        private static bool DictionaryEqual(Dictionary<string, int?> left, Dictionary<string, int?> right)
        {
            return left.Keys.Count == right.Keys.Count && left.Keys.All(k => right.ContainsKey(k) && left[k] == right[k]);
        }

        private static string? GetColumnStyle(ColumnDefinition<T> column)
        {
            string? style = null;
            if (column.Width.HasValue)
            {
                style = $"width: {column.Width.Value}px; max-width: {column.Width.Value}px;";
            }

            return style;
        }

        private static string? GetColumnClass(ColumnDefinition<T> column, T data)
        {
            var className = column.Class;
            if (column.ClassFunc is not null)
            {
                var funcClass = column.ClassFunc(data);
                if (funcClass is not null)
                {
                    if (className is null)
                    {
                        className = funcClass;
                    }
                    else
                    {
                        className = $"{className} {column.ClassFunc(data)}";
                    }
                }
            }

            if (column.Width.HasValue)
            {
                className = $"overflow-cell {className}";
            }

            return className;
        }
    }
}