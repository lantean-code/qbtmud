using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade
{
    [EventHandler("onlongpress", typeof(LongPressEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {
    }
}
