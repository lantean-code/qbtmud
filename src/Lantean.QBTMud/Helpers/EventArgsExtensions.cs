using Microsoft.AspNetCore.Components.Web;

namespace Lantean.QBTMud.Helpers
{
    public static class EventArgsExtensions
    {
        public static EventArgs NormalizeForContextMenu(this EventArgs eventArgs)
        {
            ArgumentNullException.ThrowIfNull(eventArgs);

            if (eventArgs is LongPressEventArgs longPressEventArgs)
            {
                return longPressEventArgs.ToMouseEventArgs();
            }

            return eventArgs;
        }

        public static MouseEventArgs ToMouseEventArgs(this LongPressEventArgs longPressEventArgs)
        {
            ArgumentNullException.ThrowIfNull(longPressEventArgs);

            return new MouseEventArgs
            {
                Button = 2,
                Buttons = 2,
                ClientX = longPressEventArgs.ClientX,
                ClientY = longPressEventArgs.ClientY,
                OffsetX = longPressEventArgs.OffsetX,
                OffsetY = longPressEventArgs.OffsetY,
                PageX = longPressEventArgs.PageX,
                PageY = longPressEventArgs.PageY,
                ScreenX = longPressEventArgs.ScreenX,
                ScreenY = longPressEventArgs.ScreenY,
                Type = longPressEventArgs.Type ?? "contextmenu",
                Detail = -1,
            };
        }
    }
}