using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class DynamicTable<T> : MudComponentBase, IAsyncDisposable
    {
        private static readonly string _typeName = typeof(T).Name;
        private static readonly KeyboardEvent _arrowUpKey = new("ArrowUp");
        private static readonly KeyboardEvent _arrowDownKey = new("ArrowDown");
        private static readonly KeyboardEvent _shiftArrowUpKey = new("ArrowUp") { ShiftKey = true };
        private static readonly KeyboardEvent _shiftArrowDownKey = new("ArrowDown") { ShiftKey = true };
        private static readonly KeyboardEvent _enterKey = new("Enter");
        private readonly string _columnSelectionStorageKey = $"DynamicTable{_typeName}.ColumnSelection.{{_tableId}}";
        private readonly string _columnSortStorageKey = $"DynamicTable{_typeName}.ColumnSort.{{_tableId}}";
        private readonly string _columnWidthsStorageKey = $"DynamicTable{_typeName}.ColumnWidths.{{_tableId}}";
        private readonly string _columnOrderStorageKey = $"DynamicTable{_typeName}.ColumnOrder.{{_tableId}}";

        [Inject]
        public ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        public IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IEnumerable<ColumnDefinition<T>> ColumnDefinitions { get; set; } = [];

        /// <summary>
        /// Optional identifier to scope persisted column/sort state; useful when multiple tables share the same item type.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public string TableId { get; set; } = default!;

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
        public Func<T, string>? RowKeyFunc { get; set; }

        [CascadingParameter(Name = "IsDarkMode")]
        public bool IsDarkMode { get; set; }

        [Parameter]
        public HashSet<T> SelectedItems { get; set; } = [];

        [Parameter]
        public EventCallback<HashSet<T>> SelectedItemsChanged { get; set; }

        [Parameter]
        public EventCallback<T> SelectedItemChanged { get; set; }

        [Parameter]
        public EventCallback<T> OnSelectedItemEnter { get; set; }

        [Parameter]
        public Func<ColumnDefinition<T>, bool> ColumnFilter { get; set; } = t => true;

        [Parameter]
        public object? ColumnFilterState { get; set; }

        [Parameter]
        public Func<T, string>? RowTestIdFunc { get; set; }

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

        private DateTimeOffset? _suppressRowClickUntil;

        private IReadOnlyList<ColumnDefinition<T>> _visibleColumns = EmptyColumns;

        private bool _columnsDirty = true;

        private IEnumerable<ColumnDefinition<T>>? _lastColumnDefinitions;

        private object? _lastColumnFilterState;

        private bool _initialized;

        private bool _keyboardHandlersRegistered;

        private bool _disposedValue;


        protected override async Task OnInitializedAsync()
        {
            var columnSelectionStorageKey = _columnSelectionStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
            var columnWidthsStorageKey = _columnWidthsStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
            var columnOrderStorageKey = _columnOrderStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
            var columnSortStorageKey = GetColumnSortStorageKey();

            HashSet<string> selectedColumns;
            var storedSelectedColumns = await LocalStorage.GetItemAsync<HashSet<string>>(columnSelectionStorageKey);
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

            var sortData = await LocalStorage.GetItemAsync<SortData>(columnSortStorageKey);
            if (sortData is not null)
            {
                sortColumn = sortData.SortColumn;
                sortDirection = sortData.SortDirection;
            }
            else
            {
                var defaultColumn = ColumnDefinitions.FirstOrDefault(c => SelectedColumns.Contains(c.Id))
                    ?? ColumnDefinitions.FirstOrDefault(c => c.Enabled);
                sortColumn = defaultColumn?.Id;
                sortDirection = defaultColumn?.InitialDirection == SortDirection.None
                    ? SortDirection.Ascending
                    : defaultColumn?.InitialDirection ?? SortDirection.Ascending;
            }

            var visibleColumns = ColumnDefinitions
                .Where(c => SelectedColumns.Contains(c.Id))
                .Where(ColumnFilter)
                .ToList();

            if (visibleColumns.Count == 0)
            {
                sortColumn = null;
                sortDirection = SortDirection.None;
                await LocalStorage.RemoveItemAsync(columnSortStorageKey);
            }
            else if (sortColumn is null || visibleColumns.All(c => c.Id != sortColumn))
            {
                var fallbackColumn = visibleColumns[0];
                sortColumn = fallbackColumn.Id;
                sortDirection = fallbackColumn.InitialDirection == SortDirection.None
                    ? SortDirection.Ascending
                    : fallbackColumn.InitialDirection;
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

            var storedColumnsWidths = await LocalStorage.GetItemAsync<Dictionary<string, int?>>(columnWidthsStorageKey);
            if (storedColumnsWidths is not null)
            {
                _columnWidths = storedColumnsWidths;
            }

            var storedColumnOrder = await LocalStorage.GetItemAsync<Dictionary<string, int>>(columnOrderStorageKey);
            if (storedColumnOrder is not null)
            {
                _columnOrder = storedColumnOrder;
            }
            MarkColumnsDirty();

            _initialized = true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            var ensureSort = false;
            if (!ReferenceEquals(_lastColumnDefinitions, ColumnDefinitions))
            {
                _lastColumnDefinitions = ColumnDefinitions;
                MarkColumnsDirty();
                ensureSort = _initialized;
            }

            if (!Equals(_lastColumnFilterState, ColumnFilterState))
            {
                _lastColumnFilterState = ColumnFilterState;
                MarkColumnsDirty();
                ensureSort = _initialized;
            }

            if (ensureSort)
            {
                var columnSortStorageKey = GetColumnSortStorageKey();
                _ = InvokeAsync(() => EnsureSortColumnValidAsync(columnSortStorageKey));
            }

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                await RegisterKeyboardHandlersAsync();
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
            var columnSortStorageKey = _columnSortStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
            await LocalStorage.SetItemAsync(columnSortStorageKey, new SortData(columnId, sortDirection));

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

            await EnsureSortColumnValidAsync(columnSortStorageKey);
        }

        protected async Task OnRowClickInternal(TableRowClickEventArgs<T> eventArgs)
        {
            if (_suppressRowClickUntil is not null)
            {
                if (DateTimeOffset.UtcNow <= _suppressRowClickUntil.Value)
                {
                    _suppressRowClickUntil = null;
                    return;
                }

                _suppressRowClickUntil = null;
            }

            if (eventArgs.Item is null)
            {
                return;
            }

            if (MultiSelection)
            {
                if (eventArgs.MouseEventArgs.CtrlKey && eventArgs.MouseEventArgs.ShiftKey)
                {
                    AddRangeSelection(eventArgs.Item, replaceSelection: false, useFurthest: false);
                }
                else if (eventArgs.MouseEventArgs.ShiftKey)
                {
                    AddRangeSelection(eventArgs.Item, replaceSelection: true, useFurthest: true);
                }
                else if (eventArgs.MouseEventArgs.CtrlKey)
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

            await SelectedItemsChangedInternal(SelectedItems);
            await OnRowClick.InvokeAsync(eventArgs);
        }

        protected string RowStyleFuncInternal(T item, int index)
        {
            var style = "-webkit-touch-callout: none; -webkit-user-select: none; -moz-user-select: none; -ms-user-select: none; user-select: none; cursor: pointer;";
            if (SelectOnRowClick && SelectedItems.Contains(item))
            {
                if (IsDarkMode)
                {
                    style += " background-color: var(--mud-palette-primary-darken); color: var(--mud-palette-primary-text) !important; --mud-palette-text-primary: var(--mud-palette-primary-text); --mud-palette-text-secondary: var(--mud-palette-primary-text);";
                }
                else
                {
                    style += " background-color: var(--mud-palette-primary-lighten); color: var(--mud-palette-primary-text) !important; --mud-palette-text-primary: var(--mud-palette-primary-text); --mud-palette-text-secondary: var(--mud-palette-primary-text);";
                }
            }
            return style;
        }

        private void AddRangeSelection(T item, bool replaceSelection, bool useFurthest)
        {
            var orderedItems = OrderedItems?.ToList();
            if (orderedItems is null || orderedItems.Count == 0)
            {
                if (replaceSelection)
                {
                    SelectedItems.Clear();
                }
                SelectedItems.Add(item);
                return;
            }

            var clickedIndex = orderedItems.IndexOf(item);
            if (clickedIndex < 0)
            {
                if (replaceSelection)
                {
                    SelectedItems.Clear();
                }
                SelectedItems.Add(item);
                return;
            }

            var rangeIndex = -1;
            var rangeDistance = useFurthest ? -1 : int.MaxValue;
            for (var i = 0; i < orderedItems.Count; i++)
            {
                if (!SelectedItems.Contains(orderedItems[i]))
                {
                    continue;
                }

                var distance = Math.Abs(i - clickedIndex);
                if (useFurthest ? distance > rangeDistance : distance < rangeDistance)
                {
                    rangeDistance = distance;
                    rangeIndex = i;
                }
            }

            if (rangeIndex < 0)
            {
                if (replaceSelection)
                {
                    SelectedItems.Clear();
                }
                SelectedItems.Add(item);
                return;
            }

            var startIndex = Math.Min(clickedIndex, rangeIndex);
            var endIndex = Math.Max(clickedIndex, rangeIndex);
            if (replaceSelection)
            {
                SelectedItems.Clear();
            }
            for (var i = startIndex; i <= endIndex; i++)
            {
                SelectedItems.Add(orderedItems[i]);
            }
        }

        private async Task HandleKeyboardNavigation(KeyboardEvent keyboardEvent)
        {
            if (keyboardEvent.Key == "Enter")
            {
                await HandleEnterKeyAsync();
                return;
            }

            if (keyboardEvent.Key is not "ArrowUp" and not "ArrowDown")
            {
                return;
            }

            if (!MultiSelection && !SelectOnRowClick)
            {
                return;
            }

            var orderedItems = OrderedItems?.ToList();
            if (orderedItems is null || orderedItems.Count == 0)
            {
                return;
            }

            if (SelectedItems.Count == 0)
            {
                await SelectSingleItemAsync(orderedItems[0]);
                return;
            }

            var moveUp = keyboardEvent.Key == "ArrowUp";
            var addSelection = keyboardEvent.ShiftKey && MultiSelection;
            var targetIndex = GetBoundarySelectedIndex(orderedItems, moveUp);
            if (targetIndex < 0)
            {
                await SelectSingleItemAsync(orderedItems[0]);
                return;
            }

            var nextIndex = moveUp ? targetIndex - 1 : targetIndex + 1;
            if (nextIndex < 0 || nextIndex >= orderedItems.Count)
            {
                return;
            }

            if (addSelection)
            {
                SelectedItems.Add(orderedItems[nextIndex]);
                await SelectedItemsChangedInternal(SelectedItems);
                return;
            }

            await SelectSingleItemAsync(orderedItems[nextIndex]);
        }

        private async Task HandleEnterKeyAsync()
        {
            if (!OnSelectedItemEnter.HasDelegate)
            {
                return;
            }

            if (SelectedItems.Count != 1)
            {
                return;
            }

            var item = SelectedItems.First();
            await OnSelectedItemEnter.InvokeAsync(item);
        }

        private async Task SelectSingleItemAsync(T item)
        {
            if (SelectedItems.Count == 1 && SelectedItems.Contains(item))
            {
                return;
            }

            SelectedItems.Clear();
            SelectedItems.Add(item);
            await SelectedItemsChangedInternal(SelectedItems);

            if (!MultiSelection && SelectOnRowClick)
            {
                await SelectedItemChanged.InvokeAsync(item);
            }
        }

        private int GetBoundarySelectedIndex(IReadOnlyList<T> orderedItems, bool useTop)
        {
            var boundaryIndex = -1;
            for (var i = 0; i < orderedItems.Count; i++)
            {
                if (!SelectedItems.Contains(orderedItems[i]))
                {
                    continue;
                }

                if (boundaryIndex < 0)
                {
                    boundaryIndex = i;
                    continue;
                }

                if (useTop && i < boundaryIndex)
                {
                    boundaryIndex = i;
                }
                else if (!useTop && i > boundaryIndex)
                {
                    boundaryIndex = i;
                }
            }

            return boundaryIndex;
        }

        private async Task RegisterKeyboardHandlersAsync()
        {
            if (_keyboardHandlersRegistered)
            {
                return;
            }

            await KeyboardService.RegisterKeypressEvent(_arrowUpKey, HandleKeyboardNavigation);
            await KeyboardService.RegisterKeypressEvent(_arrowDownKey, HandleKeyboardNavigation);
            await KeyboardService.RegisterKeypressEvent(_shiftArrowUpKey, HandleKeyboardNavigation);
            await KeyboardService.RegisterKeypressEvent(_shiftArrowDownKey, HandleKeyboardNavigation);
            await KeyboardService.RegisterKeypressEvent(_enterKey, HandleKeyboardNavigation);
            _keyboardHandlersRegistered = true;
        }

        private async Task UnregisterKeyboardHandlersAsync()
        {
            if (!_keyboardHandlersRegistered)
            {
                return;
            }

            await KeyboardService.UnregisterKeypressEvent(_arrowUpKey);
            await KeyboardService.UnregisterKeypressEvent(_arrowDownKey);
            await KeyboardService.UnregisterKeypressEvent(_shiftArrowUpKey);
            await KeyboardService.UnregisterKeypressEvent(_shiftArrowDownKey);
            await KeyboardService.UnregisterKeypressEvent(_enterKey);
            _keyboardHandlersRegistered = false;
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await UnregisterKeyboardHandlersAsync();
                }

                _disposedValue = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
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

        protected Task OnContextMenuInternal(CellMouseEventArgs eventArgs, T item)
        {
            return OnTableDataContextMenu.InvokeAsync(new TableDataContextMenuEventArgs<T>(eventArgs.MouseEventArgs, eventArgs.Cell, item));
        }

        protected Task OnLongPressInternal(CellLongPressEventArgs eventArgs, T item)
        {
            _suppressRowClickUntil = DateTimeOffset.UtcNow.AddMilliseconds(500);
            return OnTableDataLongPress.InvokeAsync(new TableDataLongPressEventArgs<T>(eventArgs.LongPressEventArgs, eventArgs.Cell, item));
        }

        public async Task ShowColumnOptionsDialog()
        {
            var result = await DialogWorkflow.ShowColumnsOptionsDialog(ColumnDefinitions.Where(ColumnFilter).ToList(), SelectedColumns, _columnWidths, _columnOrder);

            if (result == default)
            {
                return;
            }

            var columnSelectionStorageKey = _columnSelectionStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
            var columnSortStorageKey = _columnSortStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
            var columnWidthsStorageKey = _columnWidthsStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
            var columnOrderStorageKey = _columnOrderStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);

            if (!SelectedColumns.SetEquals(result.SelectedColumns))
            {
                SelectedColumns = result.SelectedColumns;
                await LocalStorage.SetItemAsync(columnSelectionStorageKey, SelectedColumns);
                await SelectedColumnsChanged.InvokeAsync(SelectedColumns);
                MarkColumnsDirty();
            }

            if (!DictionaryEqual(_columnWidths, result.ColumnWidths))
            {
                _columnWidths = result.ColumnWidths;
                await LocalStorage.SetItemAsync(columnWidthsStorageKey, _columnWidths);
                MarkColumnsDirty();
            }

            if (!DictionaryEqual(_columnOrder, result.ColumnOrder))
            {
                _columnOrder = result.ColumnOrder;
                await LocalStorage.SetItemAsync(columnOrderStorageKey, _columnOrder);
                MarkColumnsDirty();
            }

            await EnsureSortColumnValidAsync(columnSortStorageKey);
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
                className = string.IsNullOrWhiteSpace(className)
                    ? "overflow-cell"
                    : $"overflow-cell {className}";
            }

            if (OnTableDataContextMenu.HasDelegate)
            {
                className = string.IsNullOrWhiteSpace(className)
                    ? "no-default-context-menu"
                    : $"no-default-context-menu {className}";
            }

            if (OnTableDataLongPress.HasDelegate)
            {
                className = string.IsNullOrWhiteSpace(className)
                    ? "unselectable"
                    : $"unselectable {className}";
            }

            return className;
        }

        private string? GetRowTestId(T item)
        {
            if (RowKeyFunc is null)
            {
                return null;
            }

            var key = RowKeyFunc(item);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return TestIdHelper.For($"Row-{TableId}-{key}");
        }

        private void MarkColumnsDirty()
        {
            _columnsDirty = true;
            _visibleColumns = EmptyColumns;
        }

        private string GetColumnSortStorageKey()
        {
            return _columnSortStorageKey.Replace("{_tableId}", TableId, StringComparison.Ordinal);
        }

        private async Task EnsureSortColumnValidAsync(string columnSortStorageKey)
        {
            var visibleColumns = ColumnDefinitions
                .Where(c => SelectedColumns.Contains(c.Id))
                .Where(ColumnFilter)
                .ToList();

            if (visibleColumns.Count == 0)
            {
                if (_sortColumn is not null || _sortDirection != SortDirection.None)
                {
                    _sortColumn = null;
                    _sortDirection = SortDirection.None;
                    await LocalStorage.RemoveItemAsync(columnSortStorageKey);
                    await SortColumnChanged.InvokeAsync(_sortColumn);
                    await SortDirectionChanged.InvokeAsync(_sortDirection);
                }
                return;
            }

            if (_sortColumn is not null && visibleColumns.Any(c => c.Id == _sortColumn))
            {
                return;
            }

            var fallbackColumn = visibleColumns[0];
            var fallbackDirection = fallbackColumn.InitialDirection == SortDirection.None
                ? SortDirection.Ascending
                : fallbackColumn.InitialDirection;

            await SetSort(fallbackColumn.Id, fallbackDirection);
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
