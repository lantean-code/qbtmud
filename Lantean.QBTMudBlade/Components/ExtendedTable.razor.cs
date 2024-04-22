using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components
{
    public partial class ExtendedTable<T> : MudTable<T>
    {
        [Parameter]
        public IEnumerable<ColumnDefinition<T>>? ColumnDefinitions { get; set; }

        [Parameter]
        public HashSet<ColumnDefinition<T>> SelectedColumns { get; set; } = [];

        private Func<T, object?>? _sortSelector;
        private SortDirection _sortDirection;

        private IEnumerable<string>? _selectedColumns;

        protected override void OnParametersSet()
        {
            if (ColumnDefinitions is not null)
            {
                var activeColumns = GetActiveColummns(ColumnDefinitions);
                ColGroup ??= ColGroupFragment(activeColumns);
                HeaderContent ??= HeaderContentFragment(activeColumns);
                RowTemplate ??= RowTemplateFragment(activeColumns);
                _selectedColumns ??= ColumnDefinitions.Where(c => c.Enabled).Select(c => c.Id).ToList();
                _sortSelector ??= ColumnDefinitions.First(c => c.Enabled).SortSelector;
                Items = GetOrderedItems(Items, _sortSelector);
            }
            base.OnParametersSet();
        }

        private IEnumerable<T>? GetOrderedItems(IEnumerable<T>? items, Func<T, object?> sortSelector)
        {
            if (items is null)
            {
                return null;
            }

            return items.OrderByDirection(_sortDirection, sortSelector);
        }

        private void SetSort(Func<T, object?> sortSelector, SortDirection sortDirection)
        {
            _sortSelector = sortSelector;
            _sortDirection = sortDirection;
        }

        private IEnumerable<ColumnDefinition<T>>? GetColumns()
        {
            if (ColumnDefinitions is null)
            {
                return null;
            }

            return GetActiveColummns(ColumnDefinitions);
        }

        private IEnumerable<ColumnDefinition<T>> GetActiveColummns(IEnumerable<ColumnDefinition<T>> columns)
        {
            if (_selectedColumns is null)
            {
                return columns;
            }
            return columns.Where(c => _selectedColumns.Contains(c.Id));
        }

        //private RenderFragment CreateColGroup()
        //{
        //    return builder =>
        //    {
        //        var selectedColumns = GetColumns();
        //        if (selectedColumns is null)
        //        {
        //            return;
        //        }

        //        if (MultiSelection)
        //        {
        //            builder.OpenElement(0, "col");
        //            builder.CloseElement();
        //        }

        //        int sequence = 1;
        //        foreach (var width in selectedColumns.Select(c => c.Width))
        //        {
        //            builder.OpenElement(sequence++, "col");
        //            if (width.HasValue)
        //            {
        //                builder.AddAttribute(sequence++, "style", $"width: {width.Value}px");
        //            }
        //            builder.CloseElement();
        //        }
        //    };
        //}

        //private RenderFragment CreateHeaderContent()
        //{
        //    return builder =>
        //    {
        //        var selectedColumns = GetColumns();
        //        if (selectedColumns is null)
        //        {
        //            return;
        //        }

        //        int sequence = 0;
        //        foreach (var columnDefinition in selectedColumns)
        //        {
        //            builder.OpenComponent<MudTh>(sequence);
        //            if (columnDefinition.SortSelector is not null)
        //            {
        //                builder.OpenComponent<MudTableSortLabel<T>>(sequence++);
        //                builder.AddAttribute(sequence++, "SortDirectionChanged", EventCallback.Factory.Create<SortDirection>(this, c => SetSort(columnDefinition.SortSelector, c)));
        //                RenderFragment childContent = b => b.AddContent(0, columnDefinition.Header);
        //                builder.AddAttribute(sequence++, "ChildContent", childContent);
        //                builder.CloseComponent();
        //            }
        //            else
        //            {
        //                RenderFragment childContent = b => b.AddContent(0, columnDefinition.Header);
        //                builder.AddAttribute(sequence++, "ChildContent", childContent);
        //            }
        //            builder.CloseComponent();
        //        }
        //    };
        //}

        //private RenderFragment<T> CreateRowTemplate()
        //{
        //    return context => builder =>
        //    {
        //        var selectedColumns = GetColumns();
        //        if (selectedColumns is null)
        //        {
        //            return;
        //        }

        //        int sequence = 0;
        //        foreach (var columnDefinition in selectedColumns)
        //        {
        //            builder.OpenComponent<MudTd>(sequence++);
        //            builder.AddAttribute(sequence++, "DataLabel", columnDefinition.Header);
        //            builder.AddAttribute(sequence++, "Class", columnDefinition.Class);
        //            RenderFragment childContent = b => b.AddContent(0, columnDefinition.RowTemplate(columnDefinition.GetRowContext(context)));
        //            builder.AddAttribute(sequence++, "ChildContent", childContent);
        //            builder.CloseComponent();
        //        }
        //    };
        //}
    }
}