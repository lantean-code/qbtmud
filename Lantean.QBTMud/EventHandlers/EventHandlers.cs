using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud
{
    [EventHandler("onlongpress", typeof(LongPressEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {
    }
}