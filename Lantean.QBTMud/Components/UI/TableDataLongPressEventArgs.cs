using Lantean.QBTMud.EventHandlers;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public class TableDataLongPressEventArgs<T> : EventArgs
    {
        //
        // Summary:
        //     The coordinates of the click.
        public LongPressEventArgs LongPressEventArgs { get; }

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
        public TableDataLongPressEventArgs(LongPressEventArgs longPressEventArgs, MudTd data, T? item)
        {
            LongPressEventArgs = longPressEventArgs;
            Data = data;
            Item = item;
        }
    }
}