using Lantean.QBTMud.EventHandlers;

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Microsoft.AspNetCore.Components.Web
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    [EventHandler("onlongpress", typeof(LongPressEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
    public static class EventHandlers
    {
    }
}
