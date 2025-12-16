using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public sealed class CellLongPressEventArgs
    {
        public CellLongPressEventArgs(LongPressEventArgs longPressEventArgs, TdExtended cell)
        {
            LongPressEventArgs = longPressEventArgs;
            Cell = cell;
        }

        public LongPressEventArgs LongPressEventArgs { get; }

        public TdExtended Cell { get; }
    }
}
