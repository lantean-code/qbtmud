using Lantean.QBTMudBlade.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMudBlade.Components
{
    // This is a very hacky approach but works for now.
    // This needs to inherit from MudMenu because MudMenuItem needs a MudMenu passed to it to control the close of the menu when an item is clicked.
    // MudPopover isn't ideal for this because that is designed to be used relative to an activator which in these cases it isn't.
    // Ideally this should be changed to use something like the way the DialogService works. 
    public partial class ContextMenu : MudMenu
    {
        private const double _diff = 64;

        private bool _open;
        private string? _popoverStyle;
        private string? _id;

        private double _x;
        private double _y;
        private bool _isResized = false;

        private const double _drawerWidth = 235;

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

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [Parameter]
        public bool InsideDrawer { get; set; }

        public new string? Label { get; }

        public new string? AriaLabel { get; }

        public new string? Icon { get; }

        public new Color IconColor { get; } = Color.Inherit;

        public new string? StartIcon { get; }

        public new string? EndIcon { get; }

        public new Color Color { get; } = Color.Default;

        public new Size Size { get; } = Size.Medium;

        public new Variant Variant { get; } = Variant.Text;

        public new bool PositionAtCursor { get; } = true;

        public new RenderFragment? ActivatorContent { get; } = null;

        public new MouseEvent ActivationEvent { get; } = MouseEvent.LeftClick;

        public new string? ListClass { get; } = "unselectable";

        public new string? PopoverClass { get; } = "unselectable";

        public ContextMenu()
        {
            AnchorOrigin = Origin.TopLeft;
            TransformOrigin = Origin.TopLeft;
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public new Task CloseMenuAsync()
        {
            _open = false;
            _popoverStyle = null;
            StateHasChanged();

            return OpenChanged.InvokeAsync(_open);
        }

        /// <summary>
        /// Opens the menu.
        /// </summary>
        /// <param name="args">
        /// The arguments of the calling mouse/pointer event.
        /// If <see cref="PositionAtCursor"/> is true, the menu will be positioned using the coordinates in this parameter.
        /// </param>
        public new async Task OpenMenuAsync(EventArgs args)
        {
            if (Disabled)
            {
                return;
            }

            // long press on iOS triggers selection, so clear it
            await JSRuntime.ClearSelection();

            _open = true;
            _isResized = false;
            StateHasChanged();

            var (x, y) = GetPositionFromArgs(args);
            _x = x;
            _y = y;

            SetPopoverStyle(x, y);

            StateHasChanged();

            await OpenChanged.InvokeAsync(_open);
        }

        /// <summary>
        /// Sets the popover style ONLY when there is an activator.
        /// </summary>
        private void SetPopoverStyle(double x, double y)
        {
            _popoverStyle = $"margin-top: {y.ToPx()}; margin-left: {x.ToPx()};";
        }

        /// <summary>
        /// Toggle the visibility of the menu.
        /// </summary>
        public new async Task ToggleMenuAsync(EventArgs args)
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
            if ((_y - _diff + contextMenuHeight.Value) >= (mainContentSize.Height))
            {
                // adjust the top of the context menu
                var overshoot = Math.Abs(mainContentSize.Height - (_y - _diff + contextMenuHeight.Value));
                _y -= overshoot;
                //if (_y < 70)
                //{
                //    _y = 70;
                //}

                if ((_y - _diff + contextMenuHeight) >= mainContentSize.Height)
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

            return (x - (DrawerOpen && !InsideDrawer ? _drawerWidth : 0), y - (InsideDrawer ? _diff : 0));
        }
    }
}