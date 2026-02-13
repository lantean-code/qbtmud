using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Helpers
{
    public static class ColumnDefinitionHelper
    {
        public static ColumnDefinition<T> CreateColumnDefinition<T>(string name, Func<T, object?> selector, RenderFragment<RowContext<T>> rowTemplate, bool iconOnly = false, int? width = null, string? tdClass = null, Func<T, string?>? classFunc = null, bool enabled = true, SortDirection initialDirection = SortDirection.None, string? id = null)
        {
            var cd = new ColumnDefinition<T>(name, selector, rowTemplate, id: id);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.ClassFunc = classFunc;
            cd.Width = width;
            cd.Enabled = enabled;
            cd.InitialDirection = initialDirection;
            cd.IconOnly = iconOnly;
            return cd;
        }

        public static ColumnDefinition<T> CreateColumnDefinition<T>(string name, Func<T, object?> selector, Func<T, string>? formatter = null, bool iconOnly = false, int? width = null, string? tdClass = null, Func<T, string?>? classFunc = null, bool enabled = true, SortDirection initialDirection = SortDirection.None, string? id = null)
        {
            var cd = new ColumnDefinition<T>(name, selector, formatter, id: id);
            cd.Class = "no-wrap";
            if (tdClass is not null)
            {
                cd.Class += " " + tdClass;
            }
            cd.ClassFunc = classFunc;
            cd.Width = width;
            cd.Enabled = enabled;
            cd.InitialDirection = initialDirection;
            cd.IconOnly = iconOnly;

            return cd;
        }
    }
}
