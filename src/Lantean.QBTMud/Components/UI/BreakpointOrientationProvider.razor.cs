using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace Lantean.QBTMud.Components.UI
{
    public partial class BreakpointOrientationProvider : MudComponentBase, IBrowserViewportObserver, IAsyncDisposable
    {
        [Inject]
        private IBrowserViewportService BrowserViewportService { get; set; } = default!;

        /// <summary>
        /// Content that will receive the cascaded breakpoint and/or orientation.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Controls which values this provider will cascade.
        /// Default is Breakpoint to match MudBreakpointProvider behavior.
        /// Use Both to also cascade Orientation.
        /// </summary>
        [Parameter]
        public BreakpointOrientationProviderCascades Cascades { get; set; } = BreakpointOrientationProviderCascades.Breakpoint;

        /// <summary>
        /// The current breakpoint. You can bind this via @bind-CurrentBreakpoint.
        /// </summary>
        [Parameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        /// <summary>
        /// Raised when the current breakpoint changes.
        /// </summary>
        [Parameter]
        public EventCallback<Breakpoint> OnBreakpointChanged { get; set; }

        /// <summary>
        /// The current orientation (Horizontal = landscape, Vertical = portrait).
        /// You can bind this via @bind-CurrentOrientation.
        /// </summary>
        [Parameter]
        public Orientation CurrentOrientation { get; set; }

        /// <summary>
        /// Raised when the current orientation changes.
        /// </summary>
        [Parameter]
        public EventCallback<Orientation> OnOrientationChanged { get; set; }

        /// <summary>
        /// Optional resize options for this specific provider.
        /// If not set, the global BrowserViewportService.ResizeOptions are used.
        /// </summary>
        [Parameter]
        public ResizeOptions? Options { get; set; }

        private Breakpoint _lastBreakpoint;
        private Orientation _lastOrientation;

        private bool _hasBreakpoint;
        private bool _hasOrientation;

        private readonly Guid _observerId = Guid.NewGuid();

        private bool ProvideBreakpoint => Cascades.HasFlag(BreakpointOrientationProviderCascades.Breakpoint);

        private bool ProvideOrientation => Cascades.HasFlag(BreakpointOrientationProviderCascades.Orientation);

        Guid IBrowserViewportObserver.Id => _observerId;

        ResizeOptions IBrowserViewportObserver.ResizeOptions
        {
            get
            {
                if (Options is not null)
                {
                    return Options;
                }

                return BrowserViewportService.ResizeOptions;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await BrowserViewportService.SubscribeAsync(this);
        }

        public async Task NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs args)
        {
            var hasChanges = false;

            if (ProvideBreakpoint)
            {
                var breakpoint = args.Breakpoint;

                if (!_hasBreakpoint || breakpoint != _lastBreakpoint)
                {
                    _lastBreakpoint = breakpoint;
                    _hasBreakpoint = true;

                    CurrentBreakpoint = breakpoint;
                    hasChanges = true;

                    if (OnBreakpointChanged.HasDelegate)
                    {
                        await OnBreakpointChanged.InvokeAsync(breakpoint);
                    }
                }
            }

            if (ProvideOrientation)
            {
                var size = args.BrowserWindowSize;

                // Treat width >= height as landscape (Horizontal) and otherwise portrait (Vertical).
                var newOrientation = size.Width >= size.Height
                    ? Orientation.Landscape
                    : Orientation.Portrait;

                if (!_hasOrientation || newOrientation != _lastOrientation)
                {
                    _lastOrientation = newOrientation;
                    _hasOrientation = true;

                    CurrentOrientation = newOrientation;
                    hasChanges = true;

                    if (OnOrientationChanged.HasDelegate)
                    {
                        await OnOrientationChanged.InvokeAsync(newOrientation);
                    }
                }
            }

            if (hasChanges)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await BrowserViewportService.UnsubscribeAsync(this);
        }
    }
}
