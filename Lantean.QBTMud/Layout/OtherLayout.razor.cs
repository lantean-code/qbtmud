using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Layout
{
    public partial class OtherLayout
    {
        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public Preferences? Preferences { get; set; }
    }
}