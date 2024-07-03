using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Layout
{
    public partial class OtherLayout
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}