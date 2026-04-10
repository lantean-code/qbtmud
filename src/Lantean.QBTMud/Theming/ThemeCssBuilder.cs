using System.Globalization;
using System.Text;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Theming
{
    public static class ThemeCssBuilder
    {
        private const string _palettePrefix = "mud-palette";
        private const string _typographyPrefix = "mud-typography";

        /// <summary>
        /// Builds the CSS variables for the provided theme and mode.
        /// </summary>
        /// <param name="theme">The theme to generate variables for.</param>
        /// <param name="isDarkMode">Whether to use the dark palette.</param>
        /// <returns>The CSS variables in a <c>:root</c> selector.</returns>
        public static string BuildCssVariables(MudTheme theme, bool isDarkMode)
        {
            var palette = isDarkMode ? (Palette)theme.PaletteDark : theme.PaletteLight;
            var builder = new StringBuilder();

            builder.Append(":root{");
            AppendPalette(builder, palette);
            AppendTypography(builder, theme);
            builder.Append("}");

            return builder.ToString();
        }

        private static void AppendPalette(StringBuilder builder, Palette palette)
        {
            builder.Append($"--{_palettePrefix}-black: {palette.Black};");
            builder.Append($"--{_palettePrefix}-white: {palette.White};");

            builder.Append($"--{_palettePrefix}-primary: {palette.Primary};");
            builder.Append($"--{_palettePrefix}-primary-rgb: {palette.Primary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-primary-text: {palette.PrimaryContrastText};");
            builder.Append($"--{_palettePrefix}-primary-darken: {palette.PrimaryDarken};");
            builder.Append($"--{_palettePrefix}-primary-lighten: {palette.PrimaryLighten};");
            builder.Append($"--{_palettePrefix}-primary-hover: {palette.Primary.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-secondary: {palette.Secondary};");
            builder.Append($"--{_palettePrefix}-secondary-rgb: {palette.Secondary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-secondary-text: {palette.SecondaryContrastText};");
            builder.Append($"--{_palettePrefix}-secondary-darken: {palette.SecondaryDarken};");
            builder.Append($"--{_palettePrefix}-secondary-lighten: {palette.SecondaryLighten};");
            builder.Append($"--{_palettePrefix}-secondary-hover: {palette.Secondary.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-tertiary: {palette.Tertiary};");
            builder.Append($"--{_palettePrefix}-tertiary-rgb: {palette.Tertiary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-tertiary-text: {palette.TertiaryContrastText};");
            builder.Append($"--{_palettePrefix}-tertiary-darken: {palette.TertiaryDarken};");
            builder.Append($"--{_palettePrefix}-tertiary-lighten: {palette.TertiaryLighten};");
            builder.Append($"--{_palettePrefix}-tertiary-hover: {palette.Tertiary.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-info: {palette.Info};");
            builder.Append($"--{_palettePrefix}-info-rgb: {palette.Info.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-info-text: {palette.InfoContrastText};");
            builder.Append($"--{_palettePrefix}-info-darken: {palette.InfoDarken};");
            builder.Append($"--{_palettePrefix}-info-lighten: {palette.InfoLighten};");
            builder.Append($"--{_palettePrefix}-info-hover: {palette.Info.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-success: {palette.Success};");
            builder.Append($"--{_palettePrefix}-success-rgb: {palette.Success.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-success-text: {palette.SuccessContrastText};");
            builder.Append($"--{_palettePrefix}-success-darken: {palette.SuccessDarken};");
            builder.Append($"--{_palettePrefix}-success-lighten: {palette.SuccessLighten};");
            builder.Append($"--{_palettePrefix}-success-hover: {palette.Success.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-warning: {palette.Warning};");
            builder.Append($"--{_palettePrefix}-warning-rgb: {palette.Warning.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-warning-text: {palette.WarningContrastText};");
            builder.Append($"--{_palettePrefix}-warning-darken: {palette.WarningDarken};");
            builder.Append($"--{_palettePrefix}-warning-lighten: {palette.WarningLighten};");
            builder.Append($"--{_palettePrefix}-warning-hover: {palette.Warning.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-error: {palette.Error};");
            builder.Append($"--{_palettePrefix}-error-rgb: {palette.Error.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-error-text: {palette.ErrorContrastText};");
            builder.Append($"--{_palettePrefix}-error-darken: {palette.ErrorDarken};");
            builder.Append($"--{_palettePrefix}-error-lighten: {palette.ErrorLighten};");
            builder.Append($"--{_palettePrefix}-error-hover: {palette.Error.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-dark: {palette.Dark};");
            builder.Append($"--{_palettePrefix}-dark-rgb: {palette.Dark.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-dark-text: {palette.DarkContrastText};");
            builder.Append($"--{_palettePrefix}-dark-darken: {palette.DarkDarken};");
            builder.Append($"--{_palettePrefix}-dark-lighten: {palette.DarkLighten};");
            builder.Append($"--{_palettePrefix}-dark-hover: {palette.Dark.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{_palettePrefix}-text-primary: {palette.TextPrimary};");
            builder.Append($"--{_palettePrefix}-text-primary-rgb: {palette.TextPrimary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-text-secondary: {palette.TextSecondary};");
            builder.Append($"--{_palettePrefix}-text-secondary-rgb: {palette.TextSecondary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-text-disabled: {palette.TextDisabled};");
            builder.Append($"--{_palettePrefix}-text-disabled-rgb: {palette.TextDisabled.ToString(MudColorOutputFormats.ColorElements)};");

            builder.Append($"--{_palettePrefix}-action-default: {palette.ActionDefault};");
            builder.Append($"--{_palettePrefix}-action-default-hover: {palette.ActionDefault.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");
            builder.Append($"--{_palettePrefix}-action-disabled: {palette.ActionDisabled};");
            builder.Append($"--{_palettePrefix}-action-disabled-background: {palette.ActionDisabledBackground};");

            builder.Append($"--{_palettePrefix}-surface: {palette.Surface};");
            builder.Append($"--{_palettePrefix}-surface-rgb: {palette.Surface.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-background: {palette.Background};");
            builder.Append($"--{_palettePrefix}-background-gray: {palette.BackgroundGray};");
            builder.Append($"--{_palettePrefix}-drawer-background: {palette.DrawerBackground};");
            builder.Append($"--{_palettePrefix}-drawer-text: {palette.DrawerText};");
            builder.Append($"--{_palettePrefix}-drawer-icon: {palette.DrawerIcon};");
            builder.Append($"--{_palettePrefix}-appbar-background: {palette.AppbarBackground};");
            builder.Append($"--{_palettePrefix}-appbar-text: {palette.AppbarText};");

            builder.Append($"--{_palettePrefix}-lines-default: {palette.LinesDefault};");
            builder.Append($"--{_palettePrefix}-lines-inputs: {palette.LinesInputs};");
            builder.Append($"--{_palettePrefix}-table-lines: {palette.TableLines};");
            builder.Append($"--{_palettePrefix}-table-striped: {palette.TableStriped};");
            builder.Append($"--{_palettePrefix}-table-hover: {palette.TableHover};");
            builder.Append($"--{_palettePrefix}-divider: {palette.Divider};");
            builder.Append($"--{_palettePrefix}-divider-rgb: {palette.Divider.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{_palettePrefix}-divider-light: {palette.DividerLight};");
            builder.Append($"--{_palettePrefix}-skeleton: {palette.Skeleton};");
            builder.Append($"--{_palettePrefix}-gray-default: {palette.GrayDefault};");
            builder.Append($"--{_palettePrefix}-gray-light: {palette.GrayLight};");
            builder.Append($"--{_palettePrefix}-gray-lighter: {palette.GrayLighter};");
            builder.Append($"--{_palettePrefix}-gray-dark: {palette.GrayDark};");
            builder.Append($"--{_palettePrefix}-gray-darker: {palette.GrayDarker};");
            builder.Append($"--{_palettePrefix}-overlay-dark: {palette.OverlayDark};");
            builder.Append($"--{_palettePrefix}-overlay-light: {palette.OverlayLight};");
            builder.Append($"--{_palettePrefix}-border-opacity: {palette.BorderOpacity.ToString(CultureInfo.InvariantCulture)};");
        }

        private static void AppendTypography(StringBuilder builder, MudTheme theme)
        {
            var families = theme.Typography.Default.FontFamily ?? Array.Empty<string>();
            builder.Append($"--{_typographyPrefix}-default-family: {FormatFontFamily(families)};");
        }

        private static string FormatFontFamily(string[] fontFamilies)
        {
            return string.Join(", ", fontFamilies.Select(font => font.Contains(' ') ? $"'{font}'" : font));
        }
    }
}
