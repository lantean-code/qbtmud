using Microsoft.AspNetCore.Components.Web;

namespace Lantean.QBTMud.Components.UI
{
    public sealed class CellMouseEventArgs
    {
        public CellMouseEventArgs(MouseEventArgs mouseEventArgs, TdExtended cell)
        {
            MouseEventArgs = mouseEventArgs;
            Cell = cell;
        }

        public MouseEventArgs MouseEventArgs { get; }

        public TdExtended Cell { get; }
    }
}
