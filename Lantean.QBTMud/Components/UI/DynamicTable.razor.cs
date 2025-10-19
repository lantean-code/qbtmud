using Blazored.LocalStorage;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System;

namespace Lantean.QBTMud.Components.UI
{
    public partial class DynamicTable<T> : MudComponentBase
    {
        private static readonly string _typeName = typeof(T).Name;
        private readonly string _columnSelectionStorageKey = $"DynamicTable{_typeName}.ColumnSelection";
        private readonly string _columnSortStorageKey = $"DynamicTable{_typeName}.ColumnSort";
        private readonly string _columnWidthsStorageKey = $"DynamicTable{_typeName}.ColumnWidths";
        private readonly string _columnOrderStorageKey = $"DynamicTable{_typeName}.ColumnOrder";

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
        public Func<ColumnDefinition<T>, bool> ColumnFilter { get; set; } = t => true;

        [Parameter]
        public EventCallback<string> SortColumnChanged { get; set; }

        [Parameter]
        public EventCallback<SortDirection> SortDirectionChanged { get; set; }

        [Parameter]
        public EventCallback<HashSet<string>> SelectedColumnsChanged { get; set; }

        [Parameter]
        public EventCallback<TableDataContextMenuEventArgs<T>> OnTableDataContextMenu { get; set; }

        [Parameter]
        public EventCallback<TableDataLongPressEventArgs<T>> OnTableDataLongPress { get; set; }

        [Parameter]
        public Func<T, int, string>? RowClassFunc { get; set; }

        protected IEnumerable<T>? OrderedItems => GetOrderedItems();

        protected HashSet<string> SelectedColumns { get; set; } = [];

        private static readonly IReadOnlyList<ColumnDefinition<T>> EmptyColumns = Array.Empty<ColumnDefinition<T>>();

        private Dictionary<string, int?> _columnWidths = [];

        private Dictionary<string, int> _columnOrder = [];

        private string? _sortColumn;

        private SortDirection _sortDirection;

        private readonly Dictionary<string, TdExtended> _tds = [];

        private IReadOnlyList<ColumnDefinition<T>> _visibleColumns = EmptyColumns;

        private bool _columnsDirty = true;

        private IEnumerable<ColumnDefinition<T>>? _lastColumnDefinitions;

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
            else
            {
                SelectedColumns = selectedColumns;
            }

            _lastColumnDefinitions = ColumnDefinitions;
            MarkColumnsDirty();

            string? sortColumn;
            SortDirection sortDirection;

            var sortData = await LocalStorage.GetItemAsync<SortData>(_columnSortStorageKey);
            if (sortData is not null)
            {
                sortColumn = sortData.SortColumn;
                sortDirection = sortData.SortDirection;
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

            MarkColumnsDirty();

            var storedColumnsWidths = await LocalStorage.GetItemAsync<Dictionary<string, int?>>(_columnWidthsStorageKey);
            if (storedColumnsWidths is not null)
            {
                _columnWidths = storedColumnsWidths;
            }
            MarkColumnsDirty();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (!ReferenceEquals(_lastColumnDefinitions, ColumnDefinitions))
            {
                _lastColumnDefinitions = ColumnDefinitions;
                MarkColumnsDirty();
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

        protected IReadOnlyList<ColumnDefinition<T>> GetColumns()
        {
            if (!_columnsDirty)
            {
                return _visibleColumns;
            }

            _visibleColumns = BuildVisibleColumns();
            _columnsDirty = false;

            return _visibleColumns;
        }

        private IReadOnlyList<ColumnDefinition<T>> BuildVisibleColumns()
        {
            var filteredColumns = ColumnDefinitions
                .Where(c => SelectedColumns.Contains(c.Id))
                .Where(ColumnFilter)
                .ToList();

            if (filteredColumns.Count == 0)
            {
                return EmptyColumns;
            }

            List<ColumnDefinition<T>> orderedColumns;
            if (_columnOrder.Count == 0)
            {
                orderedColumns = filteredColumns;
            }
            else
            {
                var orderLookup = _columnOrder.OrderBy(entry => entry.Value).ToList();
                var columnDictionary = filteredColumns.ToDictionary(c => c.Id);
                orderedColumns = new List<ColumnDefinition<T>>(filteredColumns.Count);

                foreach (var (columnId, _) in orderLookup)
                {
                    if (!columnDictionary.TryGetValue(columnId, out var column))
                    {
                        continue;
                    }

                    orderedColumns.Add(column);
                }

                if (orderedColumns.Count != filteredColumns.Count)
                {
                    var existingIds = new HashSet<string>(orderedColumns.Select(c => c.Id));
                    foreach (var column in filteredColumns)
                    {
                        if (existingIds.Add(column.Id))
                        {
                            orderedColumns.Add(column);
                        }
                    }
                }
            }

            foreach (var column in orderedColumns)
            {
                if (_columnWidths.TryGetValue(column.Id, out var value))
                {
                    column.Width = value;
                }
            }

            return orderedColumns;
        }

        private async Task SetSort(string columnId, SortDirection sortDirection)
        {
            if (sortDirection == SortDirection.None)
            {
                return;
            }
            await LocalStorage.SetItemAsync(_columnSortStorageKey, new SortData(columnId, sortDirection));

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
        }

        protected async Task OnRowClickInternal(TableRowClickEventArgs<T> eventArgs)
        {
            if (eventArgs.Item is null)
            {
                return;
            }
            if (MultiSelection)
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
                }
                else if (eventArgs.MouseEventArgs.AltKey)
                {
                    SelectedItems.Clear();
                    SelectedItems.Add(eventArgs.Item);
                }
                else
                {
                    if (!SelectedItems.Contains(eventArgs.Item))
                    {
                        SelectedItems.Clear();
                        SelectedItems.Add(eventArgs.Item);
                    }
                }
            }
            else if (SelectOnRowClick && !SelectedItems.Contains(eventArgs.Item))
            {
                SelectedItems.Clear();
                SelectedItems.Add(eventArgs.Item);
                await SelectedItemChanged.InvokeAsync(eventArgs.Item);
            }

            await OnRowClick.InvokeAsync(eventArgs);
        }

        protected string RowStyleFuncInternal(T item, int index)
        {
            var style = "user-select: none; cursor: pointer;";
            if (SelectOnRowClick && SelectedItems.Contains(item))
            {
                style += " background-color: var(--mud-palette-gray-dark); color: var(--mud-palette-gray-light) !important;";
            }
            return style;
        }

        protected string RowClassFuncInternal(T item, int index)
        {
            if (RowClassFunc is not null)
            {
                return RowClassFunc(item, index);
            }

            return string.Empty;
        }

        protected async Task SelectedItemsChangedInternal(HashSet<T> selectedItems)
        {
            await SelectedItemsChanged.InvokeAsync(selectedItems);
            SelectedItems = selectedItems;
        }

        protected Task OnContextMenuInternal(MouseEventArgs eventArgs, string columnId, T item)
        {
            var data = _tds[columnId];
            return OnTableDataContextMenu.InvokeAsync(new TableDataContextMenuEventArgs<T>(eventArgs, data, item));
        }

        protected Task OnLongPressInternal(LongPressEventArgs eventArgs, string columnId, T item)
        {
            var data = _tds[columnId];
            return OnTableDataLongPress.InvokeAsync(new TableDataLongPressEventArgs<T>(eventArgs, data, item));
        }

        public async Task ShowColumnOptionsDialog()
        {
            var result = await DialogService.ShowColumnsOptionsDialog(ColumnDefinitions.Where(ColumnFilter).ToList(), SelectedColumns, _columnWidths, _columnOrder);

            if (result == default)
            {
                return;
            }

            if (!SelectedColumns.SetEquals(result.SelectedColumns))
            {
                SelectedColumns = result.SelectedColumns;
                await LocalStorage.SetItemAsync(_columnSelectionStorageKey, SelectedColumns);
                await SelectedColumnsChanged.InvokeAsync(SelectedColumns);
                MarkColumnsDirty();
            }

            if (!DictionaryEqual(_columnWidths, result.ColumnWidths))
            {
                _columnWidths = result.ColumnWidths;
                await LocalStorage.SetItemAsync(_columnWidthsStorageKey, _columnWidths);
                MarkColumnsDirty();
            }

            if (!DictionaryEqual(_columnOrder, result.ColumnOrder))
            {
                _columnOrder = result.ColumnOrder;
                await LocalStorage.SetItemAsync(_columnOrderStorageKey, _columnOrder);
                MarkColumnsDirty();
            }
        }

        private static bool DictionaryEqual<TKey, TValue>(Dictionary<TKey, TValue> left, Dictionary<TKey, TValue> right) where TKey : notnull
        {
            return left.Keys.Count == right.Keys.Count && left.Keys.All(k => right.ContainsKey(k) && Equals(left[k], right[k]));
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

        private string? GetColumnClass(ColumnDefinition<T> column, T data)
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
                        className = $"{className} {funcClass}";
                    }
                }
            }

            if (column.Width.HasValue)
            {
                className = $"overflow-cell {className}";
            }

            if (OnTableDataContextMenu.HasDelegate)
            {
                className = $"no-default-context-menu {className}";
            }

            return className;
        }

        private void MarkColumnsDirty()
        {
            _columnsDirty = true;
            _visibleColumns = EmptyColumns;
        }

        private sealed record SortData
        {
            public SortData(string sortColumn, SortDirection sortDirection)
            {
                SortColumn = sortColumn;
                SortDirection = sortDirection;
            }

            public string SortColumn { get; init; }

            public SortDirection SortDirection { get; init; }
        }
    }
}
