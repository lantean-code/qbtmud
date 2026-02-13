using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Models
{
    public class ColumnDefinition<T>
    {
        public ColumnDefinition(string header, Func<T, object?> sortSelector, Func<T, string>? formatter = null, string? tdClass = null, int? width = null, string? id = null)
        {
            Header = header;
            DisplayHeader = header;
            SortSelector = sortSelector;
            Formatter = formatter;
            Class = tdClass;
            Width = width;
            Id = string.IsNullOrWhiteSpace(id) ? CreateId(header) : id;

            RowTemplate = (context) => (builder) => builder.AddContent(1, context.GetValue());
        }

        public ColumnDefinition(string header, Func<T, object?> sortSelector, RenderFragment<RowContext<T>> rowTemplate, Func<T, string>? formatter = null, string? tdClass = null, int? width = null, string? id = null)
        {
            Header = header;
            DisplayHeader = header;
            SortSelector = sortSelector;
            RowTemplate = rowTemplate;
            Formatter = formatter;
            Class = tdClass;
            Width = width;
            Id = string.IsNullOrWhiteSpace(id) ? CreateId(header) : id;
        }

        public string Id { get; }

        public string Header { get; set; }

        public string DisplayHeader { get; set; }

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
            return new RowContext<T>(DisplayHeader, data, Formatter is null ? SortSelector : Formatter);
        }

        private static string CreateId(string header)
        {
            return header.ToLowerInvariant().Replace(' ', '_');
        }
    }
}
