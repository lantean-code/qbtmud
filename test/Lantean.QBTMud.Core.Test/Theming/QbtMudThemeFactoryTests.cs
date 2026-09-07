using AwesomeAssertions;
using Lantean.QBTMud.Core.Theming;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Core.Test.Theming
{
    public sealed class QbtMudThemeFactoryTests
    {
        [Fact]
        public void GIVEN_DefaultTheme_WHEN_Created_THEN_UsesLanteanDarkPalette()
        {
            var target = QbtMudThemeFactory.CreateDefaultTheme();

            target.PaletteDark.Primary.Should().Be(new MudColor("#22d3ee"));
            new MudColor(target.PaletteDark.PrimaryDarken).Should().Be(new MudColor("#7183a0"));
            target.PaletteDark.Secondary.Should().Be(new MudColor("#10b981"));
            target.PaletteDark.Success.Should().Be(new MudColor("#10b981"));
            target.PaletteDark.Background.Should().Be(new MudColor("#101126"));
            target.PaletteDark.Surface.Should().Be(new MudColor("#181a33"));
            target.PaletteDark.TextPrimary.Should().Be(new MudColor("#f5f7fa"));
            target.PaletteDark.TextSecondary.Should().Be(new MudColor("#b0b5c9"));
            target.Typography.Default.FontFamily.Should().Equal("Nunito Sans");
        }

        [Fact]
        public void GIVEN_DefaultTheme_WHEN_Created_THEN_UsesDeeperBrandAccentsOnLightSurfaces()
        {
            var target = QbtMudThemeFactory.CreateDefaultTheme();

            target.PaletteLight.Primary.Should().Be(new MudColor("#0e7490"));
            target.PaletteLight.Secondary.Should().Be(new MudColor("#047857"));
            target.PaletteLight.AppbarBackground.Should().Be(new MudColor("#101126"));
        }

        [Fact]
        public async Task GIVEN_BundledDefaultTheme_WHEN_Deserialized_THEN_PalettesMatchStartupTheme()
        {
            var json = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Themes", "qbtmud-default.json"), TestContext.Current.CancellationToken);
            var target = ThemeSerialization.DeserializeDefinition(json);
            var startupTheme = QbtMudThemeFactory.CreateDefaultTheme();

            target.Should().NotBeNull();
            target!.Theme.PaletteLight.Should().BeEquivalentTo(startupTheme.PaletteLight);
            target.Theme.PaletteDark.Should().BeEquivalentTo(startupTheme.PaletteDark);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_DefaultPalette_WHEN_CheckingTextAndButtons_THEN_ContrastIsAtLeastFourPointFive(bool darkMode)
        {
            var target = QbtMudThemeFactory.CreateDefaultTheme();
            var palette = darkMode ? (Palette)target.PaletteDark : target.PaletteLight;
            var pairs = new (MudColor Foreground, MudColor Background)[]
            {
                (palette.TextPrimary, palette.Background),
                (palette.TextPrimary, palette.Surface),
                (palette.TextSecondary, palette.Background),
                (palette.TextSecondary, palette.Surface),
                (palette.Primary, palette.Surface),
                (palette.Secondary, palette.Surface),
                (palette.DrawerText, palette.DrawerBackground),
                (palette.AppbarText, palette.AppbarBackground),
                (palette.PrimaryContrastText, palette.Primary),
                (palette.PrimaryContrastText, new MudColor(palette.PrimaryDarken)),
                (palette.SecondaryContrastText, palette.Secondary),
                (palette.TertiaryContrastText, palette.Tertiary),
                (palette.InfoContrastText, palette.Info),
                (palette.SuccessContrastText, palette.Success),
                (palette.WarningContrastText, palette.Warning),
                (palette.ErrorContrastText, palette.Error)
            };

            foreach (var pair in pairs)
            {
                var foreground = GetLuminance(pair.Foreground);
                var background = GetLuminance(pair.Background);
                var contrast = (Math.Max(foreground, background) + 0.05) / (Math.Min(foreground, background) + 0.05);
                contrast.Should().BeGreaterThanOrEqualTo(4.5, $"{pair.Foreground} must be readable on {pair.Background}");
            }
        }

        [Fact]
        public void GIVEN_DarkPalette_WHEN_RowSelected_THEN_HighlightRemainsDistinctFromPrimaryAccents()
        {
            var target = QbtMudThemeFactory.CreateDefaultTheme();
            var primary = GetLuminance(target.PaletteDark.Primary);
            var highlight = GetLuminance(new MudColor(target.PaletteDark.PrimaryDarken));

            ((primary + 0.05) / (highlight + 0.05)).Should().BeGreaterThanOrEqualTo(2.0);
        }

        private static double GetLuminance(MudColor color)
        {
            return 0.2126 * Linearize(color.R) + 0.7152 * Linearize(color.G) + 0.0722 * Linearize(color.B);
        }

        private static double Linearize(byte channel)
        {
            var value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
