using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.UI
{
    public class TableDataContextMenuEventArgs<T> : EventArgs
    {
        //
        // Summary:
        //     The coordinates of the click.
        public MouseEventArgs MouseEventArgs { get; }

        //
        // Summary:
        //     The row which was clicked.
        public MudTd Data { get; }

        //
        // Summary:
        //     The data related to the row which was clicked.
        public T? Item { get; }

        //
        // Summary:
        //     Creates a new instance.
        //
        // Parameters:
        //   mouseEventArgs:
        //     The coordinates of the click.
        //
        //   row:
        //     The row which was context-clicked.
        //
        //   item:
        //     The data related to the row which was context-clicked.
        public TableDataContextMenuEventArgs(MouseEventArgs mouseEventArgs, MudTd data, T? item)
        {
            MouseEventArgs = mouseEventArgs;
            Data = data;
            Item = item;
        }
    }
}
