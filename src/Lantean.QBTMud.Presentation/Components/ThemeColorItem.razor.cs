using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Components
{
    public partial class ThemeColorItem
    {
        private bool _isOpen;

        [Parameter]
        public string Name { get; set; } = string.Empty;

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        [Parameter]
        public MudColor Color { get; set; } = new("#000000");

        [Parameter]
        public EventCallback<MudColor> ColorChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public ColorPickerView ColorPickerView { get; set; } = ColorPickerView.Spectrum;

        protected string SwatchColor
        {
            get { return Color.ToString(); }
        }

        protected string SwatchLabel
        {
            get
            {
                var format = Color.A < byte.MaxValue ? MudColorOutputFormats.HexA : MudColorOutputFormats.Hex;
                return Color.ToString(format).ToUpperInvariant();
            }
        }

        protected void ToggleOpen()
        {
            if (Disabled)
            {
                return;
            }

            _isOpen = !_isOpen;
        }

        protected async Task OnColorChanged(MudColor value)
        {
            Color = value;
            if (ColorChanged.HasDelegate)
            {
                await ColorChanged.InvokeAsync(value);
            }
        }
    }
}
