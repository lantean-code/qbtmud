using Blazored.LocalStorage;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
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

        private Dictionary<string, int?> _columnWidths = [];

        private string? _sortColumn;

        private SortDirection _sortDirection;

        private readonly Dictionary<string, TdExtended> _tds = [];

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
            var result = await DialogService.ShowColumnsOptionsDialog(ColumnDefinitions.Where(ColumnFilter).ToList(), SelectedColumns, _columnWidths);

            if (result == default)
            {
                return;
            }

            if (!SelectedColumns.SetEquals(result.SelectedColumns))
            {
                SelectedColumns = result.SelectedColumns;
                await LocalStorage.SetItemAsync(_columnSelectionStorageKey, SelectedColumns);
                await SelectedColumnsChanged.InvokeAsync(SelectedColumns);
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