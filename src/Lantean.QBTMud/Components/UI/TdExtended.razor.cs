using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class TdExtended : MudTd
    {
        [Parameter]
        public EventCallback<LongPressEventArgs> OnLongPress { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnContextMenu { get; set; }

        protected Task OnLongPressInternal(LongPressEventArgs e)
        {
            return OnLongPress.InvokeAsync(e);
        }

        protected Task OnContextMenuInternal(MouseEventArgs e)
        {
            return OnContextMenu.InvokeAsync(e);
        }
    }
}