using Blazor.BrowserCapabilities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.UI
{
    public partial class Tooltip : IDisposable
    {
        private const int DefaultTouchClickAutoHideDelay = 1200;

        private bool _effectiveVisible;
        private bool _effectiveVisibleInitialized;
        private bool _lastVisibleParameterValue;
        private bool _lastVisibleParameterValueInitialized;
        private Task? _touchAutoHideTask;
        private CancellationTokenSource? _touchAutoHideTokenSource;
        private bool _disposed;

        private bool UseTouchBehavior
        {
            get
            {
                if (!BrowserCapabilitiesService.IsInitialized)
                {
                    return true;
                }

                return !BrowserCapabilitiesService.Capabilities.SupportsHoverPointer;
            }
        }

        private bool EffectiveShowOnHover
        {
            get
            {
                return UseTouchBehavior ? false : ShowOnHover;
            }
        }

        private bool EffectiveShowOnFocus
        {
            get
            {
                return UseTouchBehavior ? false : ShowOnFocus;
            }
        }

        private bool EffectiveShowOnClick
        {
            get
            {
                return UseTouchBehavior ? true : ShowOnClick;
            }
        }

        private bool EffectiveVisible
        {
            get
            {
                if (!_effectiveVisibleInitialized)
                {
                    return Visible;
                }

                return _effectiveVisible;
            }
        }

        private bool IsTouchClickAutoHideEnabled
        {
            get
            {
                return UseTouchBehavior
                    && EffectiveShowOnClick
                    && !EffectiveShowOnHover
                    && !EffectiveShowOnFocus;
            }
        }

        [Inject]
        private IBrowserCapabilitiesService BrowserCapabilitiesService { get; set; } = default!;

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

        /// <summary>
        /// Releases resources held by the component.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases managed and unmanaged resources used by this instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                CancelTouchAutoHide();
            }

            _disposed = true;
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (!_effectiveVisibleInitialized)
            {
                _effectiveVisible = Visible;
                _effectiveVisibleInitialized = true;
                _lastVisibleParameterValue = Visible;
                _lastVisibleParameterValueInitialized = true;

                return;
            }

            if (VisibleChanged.HasDelegate || !_lastVisibleParameterValueInitialized || _lastVisibleParameterValue != Visible)
            {
                _effectiveVisible = Visible;
            }

            _lastVisibleParameterValue = Visible;
            _lastVisibleParameterValueInitialized = true;
        }

        private async Task OnVisibleChanged(bool visible)
        {
            if (_disposed)
            {
                return;
            }

            CancelTouchAutoHide();

            _effectiveVisible = visible;
            _effectiveVisibleInitialized = true;

            if (visible && IsTouchClickAutoHideEnabled)
            {
                StartTouchAutoHide();
            }

            if (VisibleChanged.HasDelegate)
            {
                await VisibleChanged.InvokeAsync(visible);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task AutoHideAfterTouchClickAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(DefaultTouchClickAutoHideDelay, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await InvokeAsync(async () =>
                {
                    if (!_effectiveVisible)
                    {
                        return;
                    }

                    _effectiveVisible = false;

                    if (VisibleChanged.HasDelegate)
                    {
                        await VisibleChanged.InvokeAsync(false);
                    }

                    StateHasChanged();
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void StartTouchAutoHide()
        {
            if (_disposed)
            {
                return;
            }

            var tokenSource = new CancellationTokenSource();
            _touchAutoHideTokenSource = tokenSource;
            _touchAutoHideTask = AutoHideAfterTouchClickAsync(tokenSource.Token);
        }

        private void CancelTouchAutoHide()
        {
            _touchAutoHideTokenSource?.Cancel();
            _touchAutoHideTokenSource?.Dispose();
            _touchAutoHideTokenSource = null;
            _touchAutoHideTask = null;
        }
    }
}
