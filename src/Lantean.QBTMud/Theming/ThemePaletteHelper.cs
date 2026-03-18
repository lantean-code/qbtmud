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
                ThemePaletteColor.Black => palette.Black,
                ThemePaletteColor.White => palette.White,
                ThemePaletteColor.Primary => palette.Primary,
                ThemePaletteColor.PrimaryContrastText => palette.PrimaryContrastText,
                ThemePaletteColor.Secondary => palette.Secondary,
                ThemePaletteColor.SecondaryContrastText => palette.SecondaryContrastText,
                ThemePaletteColor.Tertiary => palette.Tertiary,
                ThemePaletteColor.TertiaryContrastText => palette.TertiaryContrastText,
                ThemePaletteColor.Info => palette.Info,
                ThemePaletteColor.InfoContrastText => palette.InfoContrastText,
                ThemePaletteColor.Success => palette.Success,
                ThemePaletteColor.SuccessContrastText => palette.SuccessContrastText,
                ThemePaletteColor.Warning => palette.Warning,
                ThemePaletteColor.WarningContrastText => palette.WarningContrastText,
                ThemePaletteColor.Error => palette.Error,
                ThemePaletteColor.ErrorContrastText => palette.ErrorContrastText,
                ThemePaletteColor.Dark => palette.Dark,
                ThemePaletteColor.DarkContrastText => palette.DarkContrastText,
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
                ThemePaletteColor.TableLines => palette.TableLines,
                ThemePaletteColor.TableStriped => palette.TableStriped,
                ThemePaletteColor.TableHover => palette.TableHover,
                ThemePaletteColor.Divider => palette.Divider,
                ThemePaletteColor.DividerLight => palette.DividerLight,
                ThemePaletteColor.Skeleton => palette.Skeleton,
                ThemePaletteColor.GrayDefault => palette.GrayDefault,
                ThemePaletteColor.GrayLight => palette.GrayLight,
                ThemePaletteColor.GrayLighter => palette.GrayLighter,
                ThemePaletteColor.GrayDark => palette.GrayDark,
                ThemePaletteColor.GrayDarker => palette.GrayDarker,
                ThemePaletteColor.OverlayDark => palette.OverlayDark,
                ThemePaletteColor.OverlayLight => palette.OverlayLight,
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
                case ThemePaletteColor.Black:
                    palette.Black = colorValue;
                    break;

                case ThemePaletteColor.White:
                    palette.White = colorValue;
                    break;

                case ThemePaletteColor.Primary:
                    palette.Primary = colorValue;
                    break;

                case ThemePaletteColor.PrimaryContrastText:
                    palette.PrimaryContrastText = colorValue;
                    break;

                case ThemePaletteColor.Secondary:
                    palette.Secondary = colorValue;
                    break;

                case ThemePaletteColor.SecondaryContrastText:
                    palette.SecondaryContrastText = colorValue;
                    break;

                case ThemePaletteColor.Tertiary:
                    palette.Tertiary = colorValue;
                    break;

                case ThemePaletteColor.TertiaryContrastText:
                    palette.TertiaryContrastText = colorValue;
                    break;

                case ThemePaletteColor.Info:
                    palette.Info = colorValue;
                    break;

                case ThemePaletteColor.InfoContrastText:
                    palette.InfoContrastText = colorValue;
                    break;

                case ThemePaletteColor.Success:
                    palette.Success = colorValue;
                    break;

                case ThemePaletteColor.SuccessContrastText:
                    palette.SuccessContrastText = colorValue;
                    break;

                case ThemePaletteColor.Warning:
                    palette.Warning = colorValue;
                    break;

                case ThemePaletteColor.WarningContrastText:
                    palette.WarningContrastText = colorValue;
                    break;

                case ThemePaletteColor.Error:
                    palette.Error = colorValue;
                    break;

                case ThemePaletteColor.ErrorContrastText:
                    palette.ErrorContrastText = colorValue;
                    break;

                case ThemePaletteColor.Dark:
                    palette.Dark = colorValue;
                    break;

                case ThemePaletteColor.DarkContrastText:
                    palette.DarkContrastText = colorValue;
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

                case ThemePaletteColor.TableLines:
                    palette.TableLines = colorValue;
                    break;

                case ThemePaletteColor.TableStriped:
                    palette.TableStriped = colorValue;
                    break;

                case ThemePaletteColor.TableHover:
                    palette.TableHover = colorValue;
                    break;

                case ThemePaletteColor.Divider:
                    palette.Divider = colorValue;
                    break;

                case ThemePaletteColor.DividerLight:
                    palette.DividerLight = colorValue;
                    break;

                case ThemePaletteColor.Skeleton:
                    palette.Skeleton = colorValue;
                    break;

                case ThemePaletteColor.GrayDefault:
                    palette.GrayDefault = colorValue;
                    break;

                case ThemePaletteColor.GrayLight:
                    palette.GrayLight = colorValue;
                    break;

                case ThemePaletteColor.GrayLighter:
                    palette.GrayLighter = colorValue;
                    break;

                case ThemePaletteColor.GrayDark:
                    palette.GrayDark = colorValue;
                    break;

                case ThemePaletteColor.GrayDarker:
                    palette.GrayDarker = colorValue;
                    break;

                case ThemePaletteColor.OverlayDark:
                    palette.OverlayDark = colorValue;
                    break;

                case ThemePaletteColor.OverlayLight:
                    palette.OverlayLight = colorValue;
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
