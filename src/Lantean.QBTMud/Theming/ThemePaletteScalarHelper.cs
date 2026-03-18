using Lantean.QBTMud.Models;
using MudBlazor;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides helpers to update theme palette scalar settings.
    /// </summary>
    public static class ThemePaletteScalarHelper
    {
        /// <summary>
        /// Gets a scalar palette value.
        /// </summary>
        /// <param name="theme">The theme to inspect.</param>
        /// <param name="valueType">The scalar value to read.</param>
        /// <param name="useDarkPalette">Whether to read from the dark palette.</param>
        /// <returns>The scalar value.</returns>
        public static double GetValue(ThemeDefinition theme, ThemePaletteScalar valueType, bool useDarkPalette)
        {
            var palette = useDarkPalette ? (Palette)theme.Theme.PaletteDark : theme.Theme.PaletteLight;
            return GetScalarValue(palette, valueType);
        }

        /// <summary>
        /// Updates a scalar palette value.
        /// </summary>
        /// <param name="theme">The theme to update.</param>
        /// <param name="valueType">The scalar value to update.</param>
        /// <param name="value">The value to apply.</param>
        /// <param name="useDarkPalette">Whether to update the dark palette.</param>
        public static void SetValue(ThemeDefinition theme, ThemePaletteScalar valueType, double value, bool useDarkPalette)
        {
            var palette = useDarkPalette ? (Palette)theme.Theme.PaletteDark : theme.Theme.PaletteLight;
            SetScalarValue(palette, valueType, value);

            if (useDarkPalette)
            {
                theme.Theme.PaletteDark = (PaletteDark)palette;
            }
            else
            {
                theme.Theme.PaletteLight = (PaletteLight)palette;
            }
        }

        private static double GetScalarValue(Palette palette, ThemePaletteScalar valueType)
        {
            return valueType switch
            {
                ThemePaletteScalar.BorderOpacity => palette.BorderOpacity,
                ThemePaletteScalar.HoverOpacity => palette.HoverOpacity,
                ThemePaletteScalar.RippleOpacity => palette.RippleOpacity,
                ThemePaletteScalar.RippleOpacitySecondary => palette.RippleOpacitySecondary,
                _ => palette.BorderOpacity
            };
        }

        private static void SetScalarValue(Palette palette, ThemePaletteScalar valueType, double value)
        {
            switch (valueType)
            {
                case ThemePaletteScalar.BorderOpacity:
                    palette.BorderOpacity = value;
                    break;

                case ThemePaletteScalar.HoverOpacity:
                    palette.HoverOpacity = value;
                    break;

                case ThemePaletteScalar.RippleOpacity:
                    palette.RippleOpacity = value;
                    break;

                case ThemePaletteScalar.RippleOpacitySecondary:
                    palette.RippleOpacitySecondary = value;
                    break;
            }
        }
    }
}
