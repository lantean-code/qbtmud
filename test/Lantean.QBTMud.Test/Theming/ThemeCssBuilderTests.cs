using AwesomeAssertions;
using Lantean.QBTMud.Theming;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Theming
{
    public sealed class ThemeCssBuilderTests
    {
        [Fact]
        public void GIVEN_Theme_WHEN_BuildCssVariablesInvoked_THEN_BuildsPaletteValues()
        {
            var theme = new MudTheme();
            theme.PaletteLight.Primary = new MudColor("#010203");
            theme.PaletteLight.AppbarBackground = new MudColor("#040506");

            var result = ThemeCssBuilder.BuildCssVariables(theme, false);

            result.Should().Contain($"--mud-palette-primary: {theme.PaletteLight.Primary};");
            result.Should().Contain($"--mud-palette-primary-rgb: {theme.PaletteLight.Primary.ToString(MudColorOutputFormats.ColorElements)};");
            result.Should().Contain($"--mud-palette-appbar-background: {theme.PaletteLight.AppbarBackground};");
        }

        [Fact]
        public void GIVEN_ThemeWithTypography_WHEN_BuildCssVariablesInvoked_THEN_OutputsFontFamily()
        {
            var theme = new MudTheme();
            theme.Typography.Default.FontFamily = ["Open Sans", "Arial"];

            var result = ThemeCssBuilder.BuildCssVariables(theme, false);

            result.Should().Contain("--mud-typography-default-family: 'Open Sans', Arial;");
        }
    }
}
