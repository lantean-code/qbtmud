using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Components
{
    /// <summary>
    /// A simple razor wrapper that only renders the child content without any additonal html markup
    /// </summary>
    public partial class NonRendering
    {
        /// <summary>
        /// The child content to be rendered
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}