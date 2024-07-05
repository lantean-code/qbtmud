using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMudBlade.Components
{
    public partial class MudTdExtended : MudTd
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
