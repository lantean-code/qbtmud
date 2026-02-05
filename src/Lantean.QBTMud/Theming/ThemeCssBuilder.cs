using MudBlazor;
using MudBlazor.Utilities;
using System.Globalization;
using System.Text;

namespace Lantean.QBTMud.Theming
{
    public static class ThemeCssBuilder
    {
        private const string PalettePrefix = "mud-palette";
        private const string TypographyPrefix = "mud-typography";

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
            builder.Append($"--{PalettePrefix}-black: {palette.Black};");
            builder.Append($"--{PalettePrefix}-white: {palette.White};");

            builder.Append($"--{PalettePrefix}-primary: {palette.Primary};");
            builder.Append($"--{PalettePrefix}-primary-rgb: {palette.Primary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-primary-text: {palette.PrimaryContrastText};");
            builder.Append($"--{PalettePrefix}-primary-darken: {palette.PrimaryDarken};");
            builder.Append($"--{PalettePrefix}-primary-lighten: {palette.PrimaryLighten};");
            builder.Append($"--{PalettePrefix}-primary-hover: {palette.Primary.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-secondary: {palette.Secondary};");
            builder.Append($"--{PalettePrefix}-secondary-rgb: {palette.Secondary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-secondary-text: {palette.SecondaryContrastText};");
            builder.Append($"--{PalettePrefix}-secondary-darken: {palette.SecondaryDarken};");
            builder.Append($"--{PalettePrefix}-secondary-lighten: {palette.SecondaryLighten};");
            builder.Append($"--{PalettePrefix}-secondary-hover: {palette.Secondary.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-tertiary: {palette.Tertiary};");
            builder.Append($"--{PalettePrefix}-tertiary-rgb: {palette.Tertiary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-tertiary-text: {palette.TertiaryContrastText};");
            builder.Append($"--{PalettePrefix}-tertiary-darken: {palette.TertiaryDarken};");
            builder.Append($"--{PalettePrefix}-tertiary-lighten: {palette.TertiaryLighten};");
            builder.Append($"--{PalettePrefix}-tertiary-hover: {palette.Tertiary.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-info: {palette.Info};");
            builder.Append($"--{PalettePrefix}-info-rgb: {palette.Info.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-info-text: {palette.InfoContrastText};");
            builder.Append($"--{PalettePrefix}-info-darken: {palette.InfoDarken};");
            builder.Append($"--{PalettePrefix}-info-lighten: {palette.InfoLighten};");
            builder.Append($"--{PalettePrefix}-info-hover: {palette.Info.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-success: {palette.Success};");
            builder.Append($"--{PalettePrefix}-success-rgb: {palette.Success.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-success-text: {palette.SuccessContrastText};");
            builder.Append($"--{PalettePrefix}-success-darken: {palette.SuccessDarken};");
            builder.Append($"--{PalettePrefix}-success-lighten: {palette.SuccessLighten};");
            builder.Append($"--{PalettePrefix}-success-hover: {palette.Success.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-warning: {palette.Warning};");
            builder.Append($"--{PalettePrefix}-warning-rgb: {palette.Warning.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-warning-text: {palette.WarningContrastText};");
            builder.Append($"--{PalettePrefix}-warning-darken: {palette.WarningDarken};");
            builder.Append($"--{PalettePrefix}-warning-lighten: {palette.WarningLighten};");
            builder.Append($"--{PalettePrefix}-warning-hover: {palette.Warning.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-error: {palette.Error};");
            builder.Append($"--{PalettePrefix}-error-rgb: {palette.Error.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-error-text: {palette.ErrorContrastText};");
            builder.Append($"--{PalettePrefix}-error-darken: {palette.ErrorDarken};");
            builder.Append($"--{PalettePrefix}-error-lighten: {palette.ErrorLighten};");
            builder.Append($"--{PalettePrefix}-error-hover: {palette.Error.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-dark: {palette.Dark};");
            builder.Append($"--{PalettePrefix}-dark-rgb: {palette.Dark.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-dark-text: {palette.DarkContrastText};");
            builder.Append($"--{PalettePrefix}-dark-darken: {palette.DarkDarken};");
            builder.Append($"--{PalettePrefix}-dark-lighten: {palette.DarkLighten};");
            builder.Append($"--{PalettePrefix}-dark-hover: {palette.Dark.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");

            builder.Append($"--{PalettePrefix}-text-primary: {palette.TextPrimary};");
            builder.Append($"--{PalettePrefix}-text-primary-rgb: {palette.TextPrimary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-text-secondary: {palette.TextSecondary};");
            builder.Append($"--{PalettePrefix}-text-secondary-rgb: {palette.TextSecondary.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-text-disabled: {palette.TextDisabled};");
            builder.Append($"--{PalettePrefix}-text-disabled-rgb: {palette.TextDisabled.ToString(MudColorOutputFormats.ColorElements)};");

            builder.Append($"--{PalettePrefix}-action-default: {palette.ActionDefault};");
            builder.Append($"--{PalettePrefix}-action-default-hover: {palette.ActionDefault.SetAlpha(palette.HoverOpacity).ToString(MudColorOutputFormats.RGBA)};");
            builder.Append($"--{PalettePrefix}-action-disabled: {palette.ActionDisabled};");
            builder.Append($"--{PalettePrefix}-action-disabled-background: {palette.ActionDisabledBackground};");

            builder.Append($"--{PalettePrefix}-surface: {palette.Surface};");
            builder.Append($"--{PalettePrefix}-surface-rgb: {palette.Surface.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-background: {palette.Background};");
            builder.Append($"--{PalettePrefix}-background-gray: {palette.BackgroundGray};");
            builder.Append($"--{PalettePrefix}-drawer-background: {palette.DrawerBackground};");
            builder.Append($"--{PalettePrefix}-drawer-text: {palette.DrawerText};");
            builder.Append($"--{PalettePrefix}-drawer-icon: {palette.DrawerIcon};");
            builder.Append($"--{PalettePrefix}-appbar-background: {palette.AppbarBackground};");
            builder.Append($"--{PalettePrefix}-appbar-text: {palette.AppbarText};");

            builder.Append($"--{PalettePrefix}-lines-default: {palette.LinesDefault};");
            builder.Append($"--{PalettePrefix}-lines-inputs: {palette.LinesInputs};");
            builder.Append($"--{PalettePrefix}-table-lines: {palette.TableLines};");
            builder.Append($"--{PalettePrefix}-table-striped: {palette.TableStriped};");
            builder.Append($"--{PalettePrefix}-table-hover: {palette.TableHover};");
            builder.Append($"--{PalettePrefix}-divider: {palette.Divider};");
            builder.Append($"--{PalettePrefix}-divider-rgb: {palette.Divider.ToString(MudColorOutputFormats.ColorElements)};");
            builder.Append($"--{PalettePrefix}-divider-light: {palette.DividerLight};");
            builder.Append($"--{PalettePrefix}-skeleton: {palette.Skeleton};");
            builder.Append($"--{PalettePrefix}-gray-default: {palette.GrayDefault};");
            builder.Append($"--{PalettePrefix}-gray-light: {palette.GrayLight};");
            builder.Append($"--{PalettePrefix}-gray-lighter: {palette.GrayLighter};");
            builder.Append($"--{PalettePrefix}-gray-dark: {palette.GrayDark};");
            builder.Append($"--{PalettePrefix}-gray-darker: {palette.GrayDarker};");
            builder.Append($"--{PalettePrefix}-overlay-dark: {palette.OverlayDark};");
            builder.Append($"--{PalettePrefix}-overlay-light: {palette.OverlayLight};");
            builder.Append($"--{PalettePrefix}-border-opacity: {palette.BorderOpacity.ToString(CultureInfo.InvariantCulture)};");
        }

        private static void AppendTypography(StringBuilder builder, MudTheme theme)
        {
            var families = theme.Typography.Default.FontFamily ?? Array.Empty<string>();
            builder.Append($"--{TypographyPrefix}-default-family: {FormatFontFamily(families)};");
        }

        private static string FormatFontFamily(string[] fontFamilies)
        {
            return string.Join(", ", fontFamilies.Select(font => font.Contains(' ') ? $"'{font}'" : font));
        }
    }
}
