using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Components.UI
{
    public partial class ToolbarTitle
    {
        private string ClassName
        {
            get
            {
                return new CssBuilder("toolbar-title")
                    .AddClass(Class)
                    .Build();
            }
        }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? Class { get; set; }
    }
}
