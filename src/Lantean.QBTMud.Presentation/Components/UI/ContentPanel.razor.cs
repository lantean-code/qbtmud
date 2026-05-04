using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Components.UI
{
    public partial class ContentPanel
    {
        private string RootClassName
        {
            get
            {
                return new CssBuilder("content-panel")
                    .AddClass(RootClass)
                    .Build();
            }
        }

        private string ToolbarClassName
        {
            get
            {
                return new CssBuilder("content-panel__toolbar")
                    .AddClass("content-panel__toolbar--scroll", ToolbarScroll)
                    .AddClass(ToolbarClass)
                    .Build();
            }
        }

        private string BodyClassName
        {
            get
            {
                return new CssBuilder("content-panel__body")
                    .AddClass(BodyClass)
                    .Build();
            }
        }

        private string ContainerClassName
        {
            get
            {
                return new CssBuilder("content-panel__container")
                    .AddClass(ContainerClass)
                    .Build();
            }
        }

        [Parameter]
        public RenderFragment? ToolbarContent { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool ToolbarScroll { get; set; }

        [Parameter]
        public bool UseContainer { get; set; }

        [Parameter]
        public MaxWidth ContainerMaxWidth { get; set; } = MaxWidth.ExtraExtraLarge;

        [Parameter]
        public string? RootClass { get; set; }

        [Parameter]
        public string? ToolbarClass { get; set; }

        [Parameter]
        public string? BodyClass { get; set; }

        [Parameter]
        public string? ContainerClass { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    }
}
