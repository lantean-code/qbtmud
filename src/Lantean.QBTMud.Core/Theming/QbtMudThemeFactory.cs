using MudBlazor;

namespace Lantean.QBTMud.Core.Theming
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
                PaletteLight = new PaletteLight
                {
                    Background = "#f5f7fa",
                    BackgroundGray = "#edf0f5",
                    Surface = "#ffffff",
                    AppbarBackground = "#101126",
                    AppbarText = "#f5f7fa",
                    DrawerBackground = "#ffffff",
                    DrawerText = "#181a33",
                    DrawerIcon = "#565d78",
                    TextPrimary = "#181a33",
                    TextSecondary = "#565d78",
                    TextDisabled = "rgba(86, 93, 120, 0.5)",
                    ActionDefault = "#565d78",
                    ActionDisabled = "rgba(86, 93, 120, 0.38)",
                    ActionDisabledBackground = "rgba(16, 17, 38, 0.08)",
                    Primary = "#0e7490",
                    PrimaryContrastText = "#ffffff",
                    Secondary = "#047857",
                    SecondaryContrastText = "#ffffff",
                    Tertiary = "#0e7490",
                    TertiaryContrastText = "#ffffff",
                    Info = "#0e7490",
                    InfoContrastText = "#ffffff",
                    Success = "#047857",
                    SuccessContrastText = "#ffffff",
                    Warning = "#92400e",
                    WarningContrastText = "#ffffff",
                    Error = "#b91c1c",
                    ErrorContrastText = "#ffffff",
                    LinesDefault = "#d4d8e3",
                    LinesInputs = "#858ba3",
                    Divider = "#d4d8e3",
                    DividerLight = "#e8ebf2",
                    TableLines = "#d4d8e3",
                    TableStriped = "rgba(16, 17, 38, 0.02)",
                    TableHover = "rgba(14, 116, 144, 0.06)"
                },
                PaletteDark = new PaletteDark
                {
                    Background = "#101126",
                    BackgroundGray = "#0c0d1d",
                    Surface = "#181a33",
                    AppbarBackground = "#181a33",
                    AppbarText = "#f5f7fa",
                    DrawerBackground = "#101126",
                    DrawerText = "#f5f7fa",
                    DrawerIcon = "#b0b5c9",
                    TextPrimary = "#f5f7fa",
                    TextSecondary = "#b0b5c9",
                    TextDisabled = "rgba(176, 181, 201, 0.5)",
                    ActionDefault = "#b0b5c9",
                    ActionDisabled = "rgba(176, 181, 201, 0.5)",
                    ActionDisabledBackground = "rgba(12, 13, 29, 0.6)",
                    Primary = "#22d3ee",
                    PrimaryDarken = "#7183a0",
                    PrimaryContrastText = "#101126",
                    Secondary = "#10b981",
                    SecondaryContrastText = "#101126",
                    Tertiary = "#22d3ee",
                    TertiaryContrastText = "#101126",
                    Info = "#22d3ee",
                    InfoContrastText = "#101126",
                    Success = "#10b981",
                    SuccessContrastText = "#101126",
                    Warning = "#fbbf24",
                    WarningContrastText = "#101126",
                    Error = "#f87171",
                    ErrorContrastText = "#101126",
                    LinesDefault = "#30334c",
                    LinesInputs = "#686e8a",
                    Divider = "#30334c",
                    DividerLight = "rgba(245, 247, 250, 0.06)",
                    TableLines = "#30334c",
                    TableStriped = "rgba(245, 247, 250, 0.03)",
                    TableHover = "rgba(34, 211, 238, 0.08)"
                }
            };

            theme.Typography.Default.FontFamily = ["Nunito Sans"];

            return theme;
        }
    }
}
