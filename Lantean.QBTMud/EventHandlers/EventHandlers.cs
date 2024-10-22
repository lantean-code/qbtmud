using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.EventHandlers
{
    [EventHandler("onlongpress", typeof(LongPressEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {
    }
}