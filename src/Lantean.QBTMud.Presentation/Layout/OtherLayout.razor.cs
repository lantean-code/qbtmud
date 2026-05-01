using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Layout
{
    public partial class OtherLayout
    {
        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "DrawerOpenChanged")]
        public EventCallback<bool> DrawerOpenChanged { get; set; }

        [CascadingParameter]
        public QBittorrentPreferences? Preferences { get; set; }

        protected async Task OnDrawerOpenChanged(bool value)
        {
            DrawerOpen = value;
            if (DrawerOpenChanged.HasDelegate)
            {
                await DrawerOpenChanged.InvokeAsync(value);
            }
        }
    }
}
