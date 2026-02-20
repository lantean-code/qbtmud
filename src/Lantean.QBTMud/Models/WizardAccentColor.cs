using MudBlazor;

namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents the accent color configuration for a wizard step.
    /// </summary>
    public sealed class WizardAccentColor
    {
        private WizardAccentColor(WizardAccentColorKind kind, Color? paletteColor, string? cssColor)
        {
            Kind = kind;
            PaletteColor = paletteColor;
            CssColor = cssColor;
        }

        /// <summary>
        /// Gets the accent color source kind.
        /// </summary>
        public WizardAccentColorKind Kind { get; }

        /// <summary>
        /// Gets the palette color value when <see cref="Kind"/> is <see cref="WizardAccentColorKind.Palette"/>.
        /// </summary>
        public Color? PaletteColor { get; }

        /// <summary>
        /// Gets the raw CSS color value when <see cref="Kind"/> is <see cref="WizardAccentColorKind.Css"/>.
        /// </summary>
        public string? CssColor { get; }

        /// <summary>
        /// Creates a palette-based accent color value.
        /// </summary>
        /// <param name="color">The MudBlazor palette color to use.</param>
        /// <returns>A new accent color value.</returns>
        public static WizardAccentColor FromPalette(Color color)
        {
            return new WizardAccentColor(WizardAccentColorKind.Palette, color, null);
        }

        /// <summary>
        /// Creates a CSS-based accent color value.
        /// </summary>
        /// <param name="cssColor">The raw CSS color string.</param>
        /// <returns>A new accent color value.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="cssColor"/> is null or whitespace.</exception>
        public static WizardAccentColor FromCss(string cssColor)
        {
            if (string.IsNullOrWhiteSpace(cssColor))
            {
                throw new ArgumentException("CSS color value cannot be null or whitespace.", nameof(cssColor));
            }

            return new WizardAccentColor(WizardAccentColorKind.Css, null, cssColor);
        }
    }
}
