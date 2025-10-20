using System;
using Lantean.QBTMud;
using Microsoft.AspNetCore.Components.Web;

namespace Lantean.QBTMud.Helpers
{
    public static class EventArgsExtensions
    {
        public static EventArgs NormalizeForContextMenu(this EventArgs eventArgs)
        {
            if (eventArgs is null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            if (eventArgs is LongPressEventArgs longPressEventArgs)
            {
                return longPressEventArgs.ToMouseEventArgs();
            }

            return eventArgs;
        }

        public static MouseEventArgs ToMouseEventArgs(this LongPressEventArgs longPressEventArgs)
        {
            if (longPressEventArgs is null)
            {
                throw new ArgumentNullException(nameof(longPressEventArgs));
            }

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
            };
        }
    }
}
