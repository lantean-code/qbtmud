using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Layout
{
    public partial class OtherLayout
    {
        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }
    }
}