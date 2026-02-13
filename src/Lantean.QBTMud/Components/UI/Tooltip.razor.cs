using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class Tooltip
    {
        private const string SupportsHoverPointerInterop = "qbt.supportsHoverPointer";

        private bool _supportsHoverPointer;
        private bool _supportsHoverPointerResolved;

        private bool UseTouchBehavior
        {
            get
            {
                if (!UseTouchOptimizedBehavior)
                {
                    return false;
                }

                if (!_supportsHoverPointerResolved)
                {
                    return true;
                }

                return !_supportsHoverPointer;
            }
        }

        private bool EffectiveShowOnHover
        {
            get
            {
                return UseTouchBehavior ? ShowOnHoverOnTouch : ShowOnHover;
            }
        }

        private bool EffectiveShowOnFocus
        {
            get
            {
                return UseTouchBehavior ? ShowOnFocusOnTouch : ShowOnFocus;
            }
        }

        private bool EffectiveShowOnClick
        {
            get
            {
                return UseTouchBehavior ? ShowOnClickOnTouch : ShowOnClick;
            }
        }

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <inheritdoc cref="MudTooltip.Text"/>
        [Parameter]
        public string? Text { get; set; }

        /// <inheritdoc cref="MudTooltip.TooltipContent"/>
        [Parameter]
        public RenderFragment? TooltipContent { get; set; }

        /// <inheritdoc cref="MudTooltip.ChildContent"/>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <inheritdoc cref="MudTooltip.Color"/>
        [Parameter]
        public Color Color { get; set; } = Color.Default;

        /// <inheritdoc cref="MudTooltip.Arrow"/>
        [Parameter]
        public bool Arrow { get; set; }

        /// <inheritdoc cref="MudTooltip.Duration"/>
        [Parameter]
        public double Duration { get; set; } = MudGlobal.TooltipDefaults.Duration.TotalMilliseconds;

        /// <inheritdoc cref="MudTooltip.Delay"/>
        [Parameter]
        public double Delay { get; set; } = MudGlobal.TooltipDefaults.Delay.TotalMilliseconds;

        /// <inheritdoc cref="MudTooltip.Placement"/>
        [Parameter]
        public Placement Placement { get; set; } = Placement.Bottom;

        /// <inheritdoc cref="MudTooltip.Inline"/>
        [Parameter]
        public bool Inline { get; set; } = true;

        /// <inheritdoc cref="MudTooltip.RootStyle"/>
        [Parameter]
        public string? RootStyle { get; set; }

        /// <inheritdoc cref="MudTooltip.RootClass"/>
        [Parameter]
        public string? RootClass { get; set; }

        /// <inheritdoc cref="MudTooltip.ShowOnHover"/>
        [Parameter]
        public bool ShowOnHover { get; set; } = true;

        /// <inheritdoc cref="MudTooltip.ShowOnFocus"/>
        [Parameter]
        public bool ShowOnFocus { get; set; } = true;

        /// <inheritdoc cref="MudTooltip.ShowOnClick"/>
        [Parameter]
        public bool ShowOnClick { get; set; }

        /// <summary>
        /// Shows the tooltip on touch-oriented devices when pointer hover is unavailable.
        /// </summary>
        [Parameter]
        public bool ShowOnHoverOnTouch { get; set; }

        /// <summary>
        /// Shows the tooltip on touch-oriented devices when pointer hover is unavailable.
        /// </summary>
        [Parameter]
        public bool ShowOnFocusOnTouch { get; set; }

        /// <summary>
        /// Shows the tooltip on touch-oriented devices when pointer hover is unavailable.
        /// </summary>
        [Parameter]
        public bool ShowOnClickOnTouch { get; set; }

        /// <summary>
        /// Enables automatic touch optimization by adapting tooltip trigger behavior when hover pointers are unavailable.
        /// </summary>
        [Parameter]
        public bool UseTouchOptimizedBehavior { get; set; } = true;

        /// <inheritdoc cref="MudTooltip.Visible"/>
        [Parameter]
        public bool Visible { get; set; }

        /// <inheritdoc cref="MudTooltip.VisibleChanged"/>
        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        /// <inheritdoc cref="MudTooltip.Disabled"/>
        [Parameter]
        public bool Disabled { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || !UseTouchOptimizedBehavior)
            {
                return;
            }

            bool supportsHoverPointer;
            try
            {
                supportsHoverPointer = await JSRuntime.InvokeAsync<bool>(SupportsHoverPointerInterop);
            }
            catch (JSException)
            {
                _supportsHoverPointerResolved = true;
                return;
            }

            _supportsHoverPointer = supportsHoverPointer;
            _supportsHoverPointerResolved = true;

            if (supportsHoverPointer)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
