using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Applies font selections to theme typography.
    /// </summary>
    public static class ThemeFontHelper
    {
        /// <summary>
        /// Applies the specified font family to all typography styles in the theme.
        /// </summary>
        /// <param name="theme">The theme to update.</param>
        /// <param name="fontFamily">The font family to apply.</param>
        public static void ApplyFont(ThemeDefinition theme, string fontFamily)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                return;
            }

            var typography = theme.Theme.Typography;
            var fonts = new[] { fontFamily, "Helvetica", "Arial", "sans-serif" };

            typography.Body1.FontFamily = fonts;
            typography.Body2.FontFamily = fonts;
            typography.Button.FontFamily = fonts;
            typography.Caption.FontFamily = fonts;
            typography.Default.FontFamily = fonts;
            typography.H1.FontFamily = fonts;
            typography.H2.FontFamily = fonts;
            typography.H3.FontFamily = fonts;
            typography.H4.FontFamily = fonts;
            typography.H5.FontFamily = fonts;
            typography.H6.FontFamily = fonts;
            typography.Overline.FontFamily = fonts;
            typography.Subtitle1.FontFamily = fonts;
            typography.Subtitle2.FontFamily = fonts;

            theme.FontFamily = fontFamily;
            theme.Theme.Typography = typography;
        }
    }
}
