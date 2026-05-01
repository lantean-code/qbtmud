using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Components.Web
{
    [EventHandler("onlongpress", typeof(Lantean.QBTMud.EventHandlers.LongPressEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {
    }
}
