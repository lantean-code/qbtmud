using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Theming
{
    public sealed class ThemePaletteDerivedColorHelperTests
    {
        public static IEnumerable<object[]> DerivedColors
        {
            get
            {
                foreach (var entry in ColorMap)
                {
                    yield return new object[] { entry.Key, entry.Value };
                }
            }
        }

        [Theory]
        [MemberData(nameof(DerivedColors))]
        public void GIVEN_DerivedColor_WHEN_GetColorFromLightPalette_THEN_ReturnsExpected(ThemePaletteDerivedColor colorType, string expected)
        {
            var theme = CreateTheme();

            var result = ThemePaletteDerivedColorHelper.GetColor(theme, colorType, false);

            string.Equals(result.Color.ToString(MudColorOutputFormats.Hex), expected, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
            result.IsAuto.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DerivedColors))]
        public void GIVEN_DerivedColor_WHEN_SetColorOnLightPalette_THEN_UpdatesExpected(ThemePaletteDerivedColor colorType, string expected)
        {
            var theme = CreateTheme();
            const string value = "#ABCDEF";

            string.Equals(ThemePaletteDerivedColorHelper.GetColor(theme, colorType, false).Color.ToString(MudColorOutputFormats.Hex), expected, StringComparison.OrdinalIgnoreCase).Should().BeTrue();

            ThemePaletteDerivedColorHelper.SetColor(theme, colorType, value, false);

            var result = ThemePaletteDerivedColorHelper.GetColor(theme, colorType, false);
            string.Equals(result.Color.ToString(MudColorOutputFormats.Hex), value, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
            result.IsAuto.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DefaultPalette_WHEN_GetColor_THEN_ReturnsAutomaticState()
        {
            var theme = new ThemeDefinition
            {
                Theme = new MudTheme()
            };

            var result = ThemePaletteDerivedColorHelper.GetColor(theme, ThemePaletteDerivedColor.PrimaryDarken, false);

            result.IsAuto.Should().BeTrue();
            string.Equals(result.Color.ToString(MudColorOutputFormats.RGB), theme.Theme.PaletteLight.Primary.ColorRgbDarken().ToString(MudColorOutputFormats.RGB), StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_OverriddenDerivedColor_WHEN_ResetColor_THEN_RestoresAutomaticState()
        {
            var theme = CreateTheme();

            ThemePaletteDerivedColorHelper.ResetColor(theme, ThemePaletteDerivedColor.PrimaryDarken, false);

            var result = ThemePaletteDerivedColorHelper.GetColor(theme, ThemePaletteDerivedColor.PrimaryDarken, false);
            result.IsAuto.Should().BeTrue();
            string.Equals(result.Color.ToString(MudColorOutputFormats.RGB), theme.Theme.PaletteLight.Primary.ColorRgbDarken().ToString(MudColorOutputFormats.RGB), StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_UnknownDerivedColor_WHEN_GetColor_THEN_ReturnsPrimaryDarken()
        {
            var theme = new ThemeDefinition
            {
                Theme = new MudTheme()
            };
            var unknown = (ThemePaletteDerivedColor)999;

            var result = ThemePaletteDerivedColorHelper.GetColor(theme, unknown, false);

            result.IsAuto.Should().BeTrue();
            string.Equals(result.Color.ToString(MudColorOutputFormats.RGB), theme.Theme.PaletteLight.Primary.ColorRgbDarken().ToString(MudColorOutputFormats.RGB), StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        private static ThemeDefinition CreateTheme()
        {
            var theme = new ThemeDefinition
            {
                Theme = new MudTheme()
            };
            var light = new PaletteLight();
            var dark = new PaletteDark();

            ApplyDerivedColors(light, ColorMap);
            ApplyDerivedColors(dark, DarkColorMap);

            theme.Theme.PaletteLight = light;
            theme.Theme.PaletteDark = dark;

            return theme;
        }

        private static void ApplyDerivedColors(Palette palette, IReadOnlyDictionary<ThemePaletteDerivedColor, string> colors)
        {
            foreach (var entry in colors)
            {
                switch (entry.Key)
                {
                    case ThemePaletteDerivedColor.PrimaryDarken:
                        palette.PrimaryDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.PrimaryLighten:
                        palette.PrimaryLighten = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.SecondaryDarken:
                        palette.SecondaryDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.SecondaryLighten:
                        palette.SecondaryLighten = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.TertiaryDarken:
                        palette.TertiaryDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.TertiaryLighten:
                        palette.TertiaryLighten = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.InfoDarken:
                        palette.InfoDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.InfoLighten:
                        palette.InfoLighten = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.SuccessDarken:
                        palette.SuccessDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.SuccessLighten:
                        palette.SuccessLighten = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.WarningDarken:
                        palette.WarningDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.WarningLighten:
                        palette.WarningLighten = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.ErrorDarken:
                        palette.ErrorDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.ErrorLighten:
                        palette.ErrorLighten = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.DarkDarken:
                        palette.DarkDarken = entry.Value;
                        break;

                    case ThemePaletteDerivedColor.DarkLighten:
                        palette.DarkLighten = entry.Value;
                        break;
                }
            }
        }

        private static readonly IReadOnlyDictionary<ThemePaletteDerivedColor, string> ColorMap =
            new Dictionary<ThemePaletteDerivedColor, string>
            {
                { ThemePaletteDerivedColor.PrimaryDarken, "#111111" },
                { ThemePaletteDerivedColor.PrimaryLighten, "#121212" },
                { ThemePaletteDerivedColor.SecondaryDarken, "#131313" },
                { ThemePaletteDerivedColor.SecondaryLighten, "#141414" },
                { ThemePaletteDerivedColor.TertiaryDarken, "#151515" },
                { ThemePaletteDerivedColor.TertiaryLighten, "#161616" },
                { ThemePaletteDerivedColor.InfoDarken, "#171717" },
                { ThemePaletteDerivedColor.InfoLighten, "#181818" },
                { ThemePaletteDerivedColor.SuccessDarken, "#191919" },
                { ThemePaletteDerivedColor.SuccessLighten, "#202020" },
                { ThemePaletteDerivedColor.WarningDarken, "#212121" },
                { ThemePaletteDerivedColor.WarningLighten, "#222222" },
                { ThemePaletteDerivedColor.ErrorDarken, "#232323" },
                { ThemePaletteDerivedColor.ErrorLighten, "#242424" },
                { ThemePaletteDerivedColor.DarkDarken, "#252525" },
                { ThemePaletteDerivedColor.DarkLighten, "#262626" }
            };

        private static readonly IReadOnlyDictionary<ThemePaletteDerivedColor, string> DarkColorMap =
            new Dictionary<ThemePaletteDerivedColor, string>
            {
                { ThemePaletteDerivedColor.PrimaryDarken, "#AAAAAA" },
                { ThemePaletteDerivedColor.PrimaryLighten, "#ABABAB" },
                { ThemePaletteDerivedColor.SecondaryDarken, "#ACACAC" },
                { ThemePaletteDerivedColor.SecondaryLighten, "#ADADAD" },
                { ThemePaletteDerivedColor.TertiaryDarken, "#AEAEAE" },
                { ThemePaletteDerivedColor.TertiaryLighten, "#AFAFAF" },
                { ThemePaletteDerivedColor.InfoDarken, "#B0B0B0" },
                { ThemePaletteDerivedColor.InfoLighten, "#B1B1B1" },
                { ThemePaletteDerivedColor.SuccessDarken, "#B2B2B2" },
                { ThemePaletteDerivedColor.SuccessLighten, "#B3B3B3" },
                { ThemePaletteDerivedColor.WarningDarken, "#B4B4B4" },
                { ThemePaletteDerivedColor.WarningLighten, "#B5B5B5" },
                { ThemePaletteDerivedColor.ErrorDarken, "#B6B6B6" },
                { ThemePaletteDerivedColor.ErrorLighten, "#B7B7B7" },
                { ThemePaletteDerivedColor.DarkDarken, "#B8B8B8" },
                { ThemePaletteDerivedColor.DarkLighten, "#B9B9B9" }
            };
    }
}
