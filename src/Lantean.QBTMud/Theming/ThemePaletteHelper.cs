using Lantean.QBTMud.Models;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides helpers to update theme palette colors.
    /// </summary>
    public static class ThemePaletteHelper
    {
        /// <summary>
        /// Gets a palette color value.
        /// </summary>
        /// <param name="theme">The theme to inspect.</param>
        /// <param name="colorType">The palette color to read.</param>
        /// <param name="useDarkPalette">Whether to read from the dark palette.</param>
        /// <returns>The palette color value.</returns>
        public static MudColor GetColor(ThemeDefinition theme, ThemePaletteColor colorType, bool useDarkPalette)
        {
            var palette = useDarkPalette ? (Palette)theme.Theme.PaletteDark : theme.Theme.PaletteLight;
            return GetPaletteColor(palette, colorType);
        }

        /// <summary>
        /// Updates a palette color value.
        /// </summary>
        /// <param name="theme">The theme to update.</param>
        /// <param name="colorType">The palette color to update.</param>
        /// <param name="colorValue">The color value to apply.</param>
        /// <param name="useDarkPalette">Whether to update the dark palette.</param>
        public static void SetColor(ThemeDefinition theme, ThemePaletteColor colorType, string colorValue, bool useDarkPalette)
        {
            var palette = useDarkPalette ? (Palette)theme.Theme.PaletteDark : theme.Theme.PaletteLight;
            SetPaletteColor(palette, colorType, colorValue);

            if (useDarkPalette)
            {
                theme.Theme.PaletteDark = (PaletteDark)palette;
            }
            else
            {
                theme.Theme.PaletteLight = (PaletteLight)palette;
            }
        }

        private static MudColor GetPaletteColor(Palette palette, ThemePaletteColor colorType)
        {
            return colorType switch
            {
                ThemePaletteColor.Primary => palette.Primary,
                ThemePaletteColor.Secondary => palette.Secondary,
                ThemePaletteColor.Tertiary => palette.Tertiary,
                ThemePaletteColor.Info => palette.Info,
                ThemePaletteColor.Success => palette.Success,
                ThemePaletteColor.Warning => palette.Warning,
                ThemePaletteColor.Error => palette.Error,
                ThemePaletteColor.Dark => palette.Dark,
                ThemePaletteColor.Surface => palette.Surface,
                ThemePaletteColor.Background => palette.Background,
                ThemePaletteColor.BackgroundGray => palette.BackgroundGray,
                ThemePaletteColor.DrawerText => palette.DrawerText,
                ThemePaletteColor.DrawerIcon => palette.DrawerIcon,
                ThemePaletteColor.DrawerBackground => palette.DrawerBackground,
                ThemePaletteColor.AppbarText => palette.AppbarText,
                ThemePaletteColor.AppbarBackground => palette.AppbarBackground,
                ThemePaletteColor.LinesDefault => palette.LinesDefault,
                ThemePaletteColor.LinesInputs => palette.LinesInputs,
                ThemePaletteColor.Divider => palette.Divider,
                ThemePaletteColor.DividerLight => palette.DividerLight,
                ThemePaletteColor.TextPrimary => palette.TextPrimary,
                ThemePaletteColor.TextSecondary => palette.TextSecondary,
                ThemePaletteColor.TextDisabled => palette.TextDisabled,
                ThemePaletteColor.ActionDefault => palette.ActionDefault,
                ThemePaletteColor.ActionDisabled => palette.ActionDisabled,
                ThemePaletteColor.ActionDisabledBackground => palette.ActionDisabledBackground,
                _ => palette.Primary
            };
        }

        private static void SetPaletteColor(Palette palette, ThemePaletteColor colorType, string colorValue)
        {
            switch (colorType)
            {
                case ThemePaletteColor.Primary:
                    palette.Primary = colorValue;
                    break;

                case ThemePaletteColor.Secondary:
                    palette.Secondary = colorValue;
                    break;

                case ThemePaletteColor.Tertiary:
                    palette.Tertiary = colorValue;
                    break;

                case ThemePaletteColor.Info:
                    palette.Info = colorValue;
                    break;

                case ThemePaletteColor.Success:
                    palette.Success = colorValue;
                    break;

                case ThemePaletteColor.Warning:
                    palette.Warning = colorValue;
                    break;

                case ThemePaletteColor.Error:
                    palette.Error = colorValue;
                    break;

                case ThemePaletteColor.Dark:
                    palette.Dark = colorValue;
                    break;

                case ThemePaletteColor.Surface:
                    palette.Surface = colorValue;
                    break;

                case ThemePaletteColor.Background:
                    palette.Background = colorValue;
                    break;

                case ThemePaletteColor.BackgroundGray:
                    palette.BackgroundGray = colorValue;
                    break;

                case ThemePaletteColor.DrawerText:
                    palette.DrawerText = colorValue;
                    break;

                case ThemePaletteColor.DrawerIcon:
                    palette.DrawerIcon = colorValue;
                    break;

                case ThemePaletteColor.DrawerBackground:
                    palette.DrawerBackground = colorValue;
                    break;

                case ThemePaletteColor.AppbarText:
                    palette.AppbarText = colorValue;
                    break;

                case ThemePaletteColor.AppbarBackground:
                    palette.AppbarBackground = colorValue;
                    break;

                case ThemePaletteColor.LinesDefault:
                    palette.LinesDefault = colorValue;
                    break;

                case ThemePaletteColor.LinesInputs:
                    palette.LinesInputs = colorValue;
                    break;

                case ThemePaletteColor.Divider:
                    palette.Divider = colorValue;
                    break;

                case ThemePaletteColor.DividerLight:
                    palette.DividerLight = colorValue;
                    break;

                case ThemePaletteColor.TextPrimary:
                    palette.TextPrimary = colorValue;
                    break;

                case ThemePaletteColor.TextSecondary:
                    palette.TextSecondary = colorValue;
                    break;

                case ThemePaletteColor.TextDisabled:
                    palette.TextDisabled = colorValue;
                    break;

                case ThemePaletteColor.ActionDefault:
                    palette.ActionDefault = colorValue;
                    break;

                case ThemePaletteColor.ActionDisabled:
                    palette.ActionDisabled = colorValue;
                    break;

                case ThemePaletteColor.ActionDisabledBackground:
                    palette.ActionDisabledBackground = colorValue;
                    break;
            }
        }
    }
}
