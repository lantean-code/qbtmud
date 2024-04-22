using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMudBlade.Components
{
    public partial class FakeNavLink
    {
        [Parameter]
        public bool Active { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public string? Class { get; set; }

        [Parameter]
        public bool DisableRipple { get; set; }

        /// <summary>
        /// Icon to use if set.
        /// </summary>
        [Parameter]
        public string? Icon { get; set; }

        /// <summary>
        /// The color of the icon. It supports the theme colors, default value uses the themes drawer icon color.
        /// </summary>
        [Parameter]
        public Color IconColor { get; set; } = Color.Default;


        [Parameter]
        public string? Target { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        protected string Classname =>
             new CssBuilder("mud-nav-item")
                 .AddClass($"mud-ripple", !DisableRipple && !Disabled)
                 .AddClass(Class)
                 .Build();

        protected string LinkClassname =>
            new CssBuilder("mud-nav-link")
                .AddClass($"mud-nav-link-disabled", Disabled)
                .AddClass("active", Active)
                .Build();

        protected string IconClassname =>
            new CssBuilder("mud-nav-link-icon")
                .AddClass($"mud-nav-link-icon-default", IconColor == Color.Default)
                .Build();

        protected async Task OnClickHandler(MouseEventArgs ev)
        {
            if (Disabled)
            {
                return;
            }

            await OnClick.InvokeAsync(ev);
        }
    }
}
