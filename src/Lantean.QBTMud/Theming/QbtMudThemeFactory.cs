using MudBlazor;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides MudBlazor theme configurations for qbtmud.
    /// </summary>
    public static class QbtMudThemeFactory
    {
        /// <summary>
        /// Creates the default theme used by the application.
        /// </summary>
        /// <returns>The configured <see cref="MudTheme"/> instance.</returns>
        public static MudTheme CreateDefaultTheme()
        {
            var theme = new MudTheme
            {
                PaletteDark = new PaletteDark
                {
                    Background = "#0b1323",
                    Surface = "#111827",
                    AppbarBackground = "#111827",
                    DrawerBackground = "#0b1323",
                    DrawerText = "rgba(226, 236, 247, 0.82)",
                    DrawerIcon = "rgba(226, 236, 247, 0.72)",
                    TextPrimary = "rgba(226, 236, 247, 0.9)",
                    TextSecondary = "rgba(159, 182, 204, 0.82)",
                    TextDisabled = "rgba(159, 182, 204, 0.5)",
                    ActionDefault = "rgba(226, 236, 247, 0.72)",
                    ActionDisabled = "rgba(159, 182, 204, 0.5)",
                    ActionDisabledBackground = "rgba(6, 11, 20, 0.6)",
                    Primary = "#3b82f6",
                    Info = "#38bdf8",
                    Success = "#2dd4bf",
                    Warning = "#fbbf24",
                    Error = "#f87171",
                    LinesDefault = "rgba(226, 236, 247, 0.12)",
                    LinesInputs = "rgba(226, 236, 247, 0.2)",
                    Divider = "rgba(226, 236, 247, 0.12)",
                    DividerLight = "rgba(226, 236, 247, 0.06)",
                    TableLines = "rgba(226, 236, 247, 0.12)",
                    TableStriped = "rgba(226, 236, 247, 0.04)",
                    TableHover = "rgba(226, 236, 247, 0.08)"
                }
            };

            theme.Typography.Default.FontFamily = ["Nunito Sans"];

            return theme;
        }
    }
}
