using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class TdExtended : MudTd
    {
        [Parameter]
        public EventCallback<CellLongPressEventArgs> OnLongPress { get; set; }

        [Parameter]
        public EventCallback<CellMouseEventArgs> OnContextMenu { get; set; }

        protected Task OnLongPressInternal(LongPressEventArgs e)
        {
            return OnLongPress.InvokeAsync(new CellLongPressEventArgs(e, this));
        }

        protected Task OnContextMenuInternal(MouseEventArgs e)
        {
            return OnContextMenu.InvokeAsync(new CellMouseEventArgs(e, this));
        }
    }
}
