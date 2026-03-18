using Lantean.QBTMud.Models;
using MudBlazor;
using MudBlazor.Utilities;
using System.Reflection;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides helpers to update theme palette derived color overrides.
    /// </summary>
    public static class ThemePaletteDerivedColorHelper
    {
        /// <summary>
        /// Gets a derived palette color value.
        /// </summary>
        /// <param name="theme">The theme to inspect.</param>
        /// <param name="colorType">The derived color to read.</param>
        /// <param name="useDarkPalette">Whether to read from the dark palette.</param>
        /// <returns>The resolved color and whether it is currently automatic.</returns>
        public static ThemeDerivedColorState GetColor(ThemeDefinition theme, ThemePaletteDerivedColor colorType, bool useDarkPalette)
        {
            var palette = useDarkPalette ? (Palette)theme.Theme.PaletteDark : theme.Theme.PaletteLight;
            var currentValue = GetDerivedValue(palette, colorType);
            var automaticValue = GetAutomaticValue(palette, colorType);
            var normalizedCurrent = NormalizeColorString(currentValue);
            var normalizedAutomatic = NormalizeColorString(automaticValue);
            var isAuto = string.Equals(normalizedCurrent, normalizedAutomatic, StringComparison.OrdinalIgnoreCase);

            return new ThemeDerivedColorState(new MudColor(normalizedCurrent), isAuto);
        }

        /// <summary>
        /// Updates a derived palette color override.
        /// </summary>
        /// <param name="theme">The theme to update.</param>
        /// <param name="colorType">The derived color to update.</param>
        /// <param name="colorValue">The color value to apply.</param>
        /// <param name="useDarkPalette">Whether to update the dark palette.</param>
        public static void SetColor(ThemeDefinition theme, ThemePaletteDerivedColor colorType, string colorValue, bool useDarkPalette)
        {
            var palette = useDarkPalette ? (Palette)theme.Theme.PaletteDark : theme.Theme.PaletteLight;
            SetDerivedValue(palette, colorType, colorValue);

            if (useDarkPalette)
            {
                theme.Theme.PaletteDark = (PaletteDark)palette;
            }
            else
            {
                theme.Theme.PaletteLight = (PaletteLight)palette;
            }
        }

        /// <summary>
        /// Resets a derived palette color override back to MudBlazor's automatic calculation.
        /// </summary>
        /// <param name="theme">The theme to update.</param>
        /// <param name="colorType">The derived color to reset.</param>
        /// <param name="useDarkPalette">Whether to reset the dark palette.</param>
        public static void ResetColor(ThemeDefinition theme, ThemePaletteDerivedColor colorType, bool useDarkPalette)
        {
            var palette = useDarkPalette ? (Palette)theme.Theme.PaletteDark : theme.Theme.PaletteLight;
            ResetDerivedValue(palette, colorType);

            if (useDarkPalette)
            {
                theme.Theme.PaletteDark = (PaletteDark)palette;
            }
            else
            {
                theme.Theme.PaletteLight = (PaletteLight)palette;
            }
        }

        private static string GetDerivedValue(Palette palette, ThemePaletteDerivedColor colorType)
        {
            return colorType switch
            {
                ThemePaletteDerivedColor.PrimaryDarken => palette.PrimaryDarken,
                ThemePaletteDerivedColor.PrimaryLighten => palette.PrimaryLighten,
                ThemePaletteDerivedColor.SecondaryDarken => palette.SecondaryDarken,
                ThemePaletteDerivedColor.SecondaryLighten => palette.SecondaryLighten,
                ThemePaletteDerivedColor.TertiaryDarken => palette.TertiaryDarken,
                ThemePaletteDerivedColor.TertiaryLighten => palette.TertiaryLighten,
                ThemePaletteDerivedColor.InfoDarken => palette.InfoDarken,
                ThemePaletteDerivedColor.InfoLighten => palette.InfoLighten,
                ThemePaletteDerivedColor.SuccessDarken => palette.SuccessDarken,
                ThemePaletteDerivedColor.SuccessLighten => palette.SuccessLighten,
                ThemePaletteDerivedColor.WarningDarken => palette.WarningDarken,
                ThemePaletteDerivedColor.WarningLighten => palette.WarningLighten,
                ThemePaletteDerivedColor.ErrorDarken => palette.ErrorDarken,
                ThemePaletteDerivedColor.ErrorLighten => palette.ErrorLighten,
                ThemePaletteDerivedColor.DarkDarken => palette.DarkDarken,
                ThemePaletteDerivedColor.DarkLighten => palette.DarkLighten,
                _ => palette.PrimaryDarken
            };
        }

        private static string GetAutomaticValue(Palette palette, ThemePaletteDerivedColor colorType)
        {
            return colorType switch
            {
                ThemePaletteDerivedColor.PrimaryDarken => palette.Primary.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.PrimaryLighten => palette.Primary.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.SecondaryDarken => palette.Secondary.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.SecondaryLighten => palette.Secondary.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.TertiaryDarken => palette.Tertiary.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.TertiaryLighten => palette.Tertiary.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.InfoDarken => palette.Info.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.InfoLighten => palette.Info.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.SuccessDarken => palette.Success.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.SuccessLighten => palette.Success.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.WarningDarken => palette.Warning.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.WarningLighten => palette.Warning.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.ErrorDarken => palette.Error.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.ErrorLighten => palette.Error.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.DarkDarken => palette.Dark.ColorRgbDarken().ToString(MudColorOutputFormats.RGB),
                ThemePaletteDerivedColor.DarkLighten => palette.Dark.ColorRgbLighten().ToString(MudColorOutputFormats.RGB),
                _ => palette.Primary.ColorRgbDarken().ToString(MudColorOutputFormats.RGB)
            };
        }

        private static void SetDerivedValue(Palette palette, ThemePaletteDerivedColor colorType, string colorValue)
        {
            switch (colorType)
            {
                case ThemePaletteDerivedColor.PrimaryDarken:
                    palette.PrimaryDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.PrimaryLighten:
                    palette.PrimaryLighten = colorValue;
                    break;

                case ThemePaletteDerivedColor.SecondaryDarken:
                    palette.SecondaryDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.SecondaryLighten:
                    palette.SecondaryLighten = colorValue;
                    break;

                case ThemePaletteDerivedColor.TertiaryDarken:
                    palette.TertiaryDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.TertiaryLighten:
                    palette.TertiaryLighten = colorValue;
                    break;

                case ThemePaletteDerivedColor.InfoDarken:
                    palette.InfoDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.InfoLighten:
                    palette.InfoLighten = colorValue;
                    break;

                case ThemePaletteDerivedColor.SuccessDarken:
                    palette.SuccessDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.SuccessLighten:
                    palette.SuccessLighten = colorValue;
                    break;

                case ThemePaletteDerivedColor.WarningDarken:
                    palette.WarningDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.WarningLighten:
                    palette.WarningLighten = colorValue;
                    break;

                case ThemePaletteDerivedColor.ErrorDarken:
                    palette.ErrorDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.ErrorLighten:
                    palette.ErrorLighten = colorValue;
                    break;

                case ThemePaletteDerivedColor.DarkDarken:
                    palette.DarkDarken = colorValue;
                    break;

                case ThemePaletteDerivedColor.DarkLighten:
                    palette.DarkLighten = colorValue;
                    break;
            }
        }

        private static void ResetDerivedValue(Palette palette, ThemePaletteDerivedColor colorType)
        {
            var fieldName = colorType switch
            {
                ThemePaletteDerivedColor.PrimaryDarken => "_primaryDarken",
                ThemePaletteDerivedColor.PrimaryLighten => "_primaryLighten",
                ThemePaletteDerivedColor.SecondaryDarken => "_secondaryDarken",
                ThemePaletteDerivedColor.SecondaryLighten => "_secondaryLighten",
                ThemePaletteDerivedColor.TertiaryDarken => "_tertiaryDarken",
                ThemePaletteDerivedColor.TertiaryLighten => "_tertiaryLighten",
                ThemePaletteDerivedColor.InfoDarken => "_infoDarken",
                ThemePaletteDerivedColor.InfoLighten => "_infoLighten",
                ThemePaletteDerivedColor.SuccessDarken => "_successDarken",
                ThemePaletteDerivedColor.SuccessLighten => "_successLighten",
                ThemePaletteDerivedColor.WarningDarken => "_warningDarken",
                ThemePaletteDerivedColor.WarningLighten => "_warningLighten",
                ThemePaletteDerivedColor.ErrorDarken => "_errorDarken",
                ThemePaletteDerivedColor.ErrorLighten => "_errorLighten",
                ThemePaletteDerivedColor.DarkDarken => "_darkDarken",
                ThemePaletteDerivedColor.DarkLighten => "_darkLighten",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(fieldName))
            {
                var field = typeof(Palette).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field is not null)
                {
                    field.SetValue(palette, null);
                    return;
                }
            }

            SetDerivedValue(palette, colorType, GetAutomaticValue(palette, colorType));
        }

        private static string NormalizeColorString(string colorValue)
        {
            return new MudColor(colorValue).ToString(MudColorOutputFormats.RGB);
        }
    }
}
