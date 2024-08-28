using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Models
{
    public class ColumnDefinition<T>
    {
        public ColumnDefinition(string header, Func<T, object?> sortSelector, Func<T, string>? formatter = null, string? tdClass = null, int? width = null)
        {
            Header = header;
            SortSelector = sortSelector;
            Formatter = formatter;
            Class = tdClass;
            Width = width;

            RowTemplate = (context) => (builder) => builder.AddContent(1, context.GetValue());
        }

        public ColumnDefinition(string header, Func<T, object?> sortSelector, RenderFragment<RowContext<T>> rowTemplate, Func<T, string>? formatter = null, string? tdClass = null, int? width = null)
        {
            Header = header;
            SortSelector = sortSelector;
            RowTemplate = rowTemplate;
            Formatter = formatter;
            Class = tdClass;
            Width = width;
        }

        public string Id => Header.ToLowerInvariant().Replace(' ', '_');

        public string Header { get; set; }

        public Func<T, object?> SortSelector { get; set; }

        public RenderFragment<RowContext<T>> RowTemplate { get; set; }

        public bool IconOnly { get; set; }

        public int? Width { get; set; }

        public Func<T, string>? Formatter { get; set; }

        public string? Class { get; set; }

        public Func<T, string?>? ClassFunc { get; set; }

        public bool Enabled { get; set; } = true;

        public SortDirection InitialDirection { get; set; } = SortDirection.None;

        public RowContext<T> GetRowContext(T data)
        {
            return new RowContext<T>(Header, data, Formatter is null ? SortSelector : Formatter);
        }
    }
}