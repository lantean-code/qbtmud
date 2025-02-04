using Lantean.QBTMud.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Components.UI
{
    public partial class ContextMenu : MudComponentBase
    {
        private bool _open;
        private bool _showChildren;
        private string? _popoverStyle;
        private string? _id;

        private double _x;
        private double _y;
        private bool _isResized = false;

        private const double _diff = 64;

        private string Id
        {
            get
            {
                _id ??= Guid.NewGuid().ToString();

                return _id;
            }
        }

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        public IPopoverService PopoverService { get; set; } = default!;

        /// <summary>
        /// If true, compact vertical padding will be applied to all menu items.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupAppearance)]
        public bool Dense { get; set; }

        /// <summary>
        /// Set to true if you want to prevent page from scrolling when the menu is open
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupAppearance)]
        public bool LockScroll { get; set; }

        /// <summary>
        /// If true, the list menu will be same width as the parent.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupAppearance)]
        public bool FullWidth { get; set; }

        /// <summary>
        /// Sets the max height the menu can have when open.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupAppearance)]
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Set the anchor origin point to determine where the popover will open from.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupAppearance)]
        public Origin AnchorOrigin { get; set; } = Origin.TopLeft;

        /// <summary>
        /// Sets the transform origin point for the popover.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupAppearance)]
        public Origin TransformOrigin { get; set; } = Origin.TopLeft;

        /// <summary>
        /// If true, menu will be disabled.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.Behavior)]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets whether to show a ripple effect when the user clicks the button. Default is true.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.Appearance)]
        public bool Ripple { get; set; } = true;

        /// <summary>
        /// Determines whether the component has a drop-shadow. Default is true
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.Appearance)]
        public bool DropShadow { get; set; } = true;

        /// <summary>
        /// Add menu items here
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupBehavior)]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Fired when the menu <see cref="Open"/> property changes.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Menu.PopupBehavior)]
        public EventCallback<bool> OpenChanged { get; set; }

        [Parameter]
        public int AdjustmentX { get; set; }

        [Parameter]
        public int AdjustmentY { get; set; }

        protected MudMenu? FakeMenu { get; set; }

        protected void FakeOpenChanged(bool value)
        {
            if (!value)
            {
                _open = false;
            }

            StateHasChanged();
        }

        /// <summary>
        /// Opens the menu.
        /// </summary>
        /// <param name="args">
        /// The arguments of the calling mouse/pointer event.
        /// </param>
        public async Task OpenMenuAsync(EventArgs args)
        {
            if (Disabled)
            {
                return;
            }

            // long press on iOS triggers selection, so clear it
            await JSRuntime.ClearSelection();

            if (args is not LongPressEventArgs)
            {
                _showChildren = true;
            }

            _open = true;
            _isResized = false;
            StateHasChanged();

            var (x, y) = GetPositionFromArgs(args);
            _x = x;
            _y = y;

            SetPopoverStyle(x, y);

            StateHasChanged();

            await OpenChanged.InvokeAsync(_open);

            // long press on iOS triggers selection, so clear it
            await JSRuntime.ClearSelection();

            if (args is LongPressEventArgs)
            {
                await Task.Delay(1000);
                _showChildren = true;
            }
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public Task CloseMenuAsync()
        {
            _open = false;
            _popoverStyle = null;
            StateHasChanged();

            return OpenChanged.InvokeAsync(_open);
        }

        private void SetPopoverStyle(double x, double y)
        {
            _popoverStyle = $"margin-top: {y.ToPx()}; margin-left: {x.ToPx()};";
        }

        /// <summary>
        /// Toggle the visibility of the menu.
        /// </summary>
        public async Task ToggleMenuAsync(EventArgs args)
        {
            if (Disabled)
            {
                return;
            }

            if (_open)
            {
                await CloseMenuAsync();
            }
            else
            {
                await OpenMenuAsync(args);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_isResized)
            {
                await DeterminePosition();
            }
        }

        private async Task DeterminePosition()
        {
            var mainContentSize = await JSRuntime.GetInnerDimensions(".mud-main-content");
            double? contextMenuHeight = null;
            double? contextMenuWidth = null;

            var popoverHolder = PopoverService.ActivePopovers.FirstOrDefault(p => p.UserAttributes.ContainsKey("tracker") && (string?)p.UserAttributes["tracker"] == Id);

            var popoverSize = await JSRuntime.GetBoundingClientRect($"#popovercontent-{popoverHolder?.Id}");
            if (popoverSize.Height > 0)
            {
                contextMenuHeight = popoverSize.Height;
                contextMenuWidth = popoverSize.Width;
            }
            else
            {
                return;
            }

            // the bottom position of the popover will be rendered off screen
            if (_y - _diff + contextMenuHeight.Value >= mainContentSize.Height)
            {
                // adjust the top of the context menu
                var overshoot = Math.Abs(mainContentSize.Height - (_y - _diff + contextMenuHeight.Value));
                _y -= overshoot;

                if (_y - _diff + contextMenuHeight >= mainContentSize.Height)
                {
                    MaxHeight = (int)(mainContentSize.Height - _y + _diff);
                }
            }

            if (_x + contextMenuWidth.Value > mainContentSize.Width)
            {
                var overshoot = Math.Abs(mainContentSize.Width - (_x + contextMenuWidth.Value));
                _x -= overshoot;
            }

            SetPopoverStyle(_x, _y);
            _isResized = true;
            await InvokeAsync(StateHasChanged);
        }

        private (double x, double y) GetPositionFromArgs(EventArgs eventArgs)
        {
            double x, y;
            if (eventArgs is MouseEventArgs mouseEventArgs)
            {
                x = mouseEventArgs.ClientX;
                y = mouseEventArgs.ClientY;
            }
            else if (eventArgs is LongPressEventArgs longPressEventArgs)
            {
                x = longPressEventArgs.ClientX;
                y = longPressEventArgs.ClientY;
            }
            else
            {
                throw new NotSupportedException("Invalid eventArgs type.");
            }

            return (x + AdjustmentX, y + AdjustmentY);
        }
    }
}