using Lantean.QBTMud.EventHandlers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class TdExtended : MudTd
    {
        private bool PreventDefaultOnLongPress => OnLongPress.HasDelegate;

        private bool PreventDefaultOnContextMenu => OnContextMenu.HasDelegate;

        private EventCallback<LongPressEventArgs> EffectiveOnLongPress => OnLongPress.HasDelegate
            ? EventCallback.Factory.Create<LongPressEventArgs>(this, OnLongPressInternal)
            : default;

        private EventCallback<MouseEventArgs> EffectiveOnContextMenu => OnContextMenu.HasDelegate
            ? EventCallback.Factory.Create<MouseEventArgs>(this, OnContextMenuInternal)
            : default;

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
