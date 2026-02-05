using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Theming
{
    public sealed class ThemePaletteHelperTests
    {
        public static IEnumerable<object[]> PaletteColors
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
        [MemberData(nameof(PaletteColors))]
        public void GIVEN_ColorType_WHEN_GetColorFromLightPalette_THEN_ReturnsExpected(ThemePaletteColor colorType, string expected)
        {
            var theme = CreateTheme();

            var result = ThemePaletteHelper.GetColor(theme, colorType, false);

            string.Equals(result.ToString(MudColorOutputFormats.Hex), expected, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(PaletteColors))]
        public void GIVEN_ColorType_WHEN_SetColorOnLightPalette_THEN_UpdatesExpected(ThemePaletteColor colorType, string expected)
        {
            var theme = CreateTheme();
            var value = "#ABCDEF";

            string.Equals(ThemePaletteHelper.GetColor(theme, colorType, false).ToString(MudColorOutputFormats.Hex), expected, StringComparison.OrdinalIgnoreCase).Should().BeTrue();

            ThemePaletteHelper.SetColor(theme, colorType, value, false);

            var palette = (Palette)theme.Theme.PaletteLight;
            string.Equals(GetPaletteValue(palette, colorType).ToString(MudColorOutputFormats.Hex), value, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
            string.Equals(ThemePaletteHelper.GetColor(theme, colorType, false).ToString(MudColorOutputFormats.Hex), value, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DarkPalette_WHEN_GetColor_THEN_ReturnsDarkValue()
        {
            var theme = CreateTheme();

            var result = ThemePaletteHelper.GetColor(theme, ThemePaletteColor.Primary, true);

            string.Equals(result.ToString(MudColorOutputFormats.Hex), "#AAAAAA", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DarkPalette_WHEN_SetColor_THEN_UpdatesDarkPalette()
        {
            var theme = CreateTheme();

            ThemePaletteHelper.SetColor(theme, ThemePaletteColor.Primary, "#123456", true);

            var palette = (Palette)theme.Theme.PaletteDark;
            string.Equals(palette.Primary.ToString(MudColorOutputFormats.Hex), "#123456", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_UnknownColor_WHEN_GetColor_THEN_ReturnsPrimary()
        {
            var theme = CreateTheme();
            var unknown = (ThemePaletteColor)999;

            var result = ThemePaletteHelper.GetColor(theme, unknown, false);

            string.Equals(result.ToString(MudColorOutputFormats.Hex), ColorMap[ThemePaletteColor.Primary], StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_UnknownColor_WHEN_SetColor_THEN_DoesNotChangePalette()
        {
            var theme = CreateTheme();
            var original = ThemePaletteHelper.GetColor(theme, ThemePaletteColor.Primary, false)
                .ToString(MudColorOutputFormats.Hex);
            var unknown = (ThemePaletteColor)999;

            ThemePaletteHelper.SetColor(theme, unknown, "#010101", false);

            string.Equals(
                    ThemePaletteHelper.GetColor(theme, ThemePaletteColor.Primary, false).ToString(MudColorOutputFormats.Hex),
                    original,
                    StringComparison.OrdinalIgnoreCase)
                .Should()
                .BeTrue();
        }

        private static ThemeDefinition CreateTheme()
        {
            var theme = new ThemeDefinition
            {
                Theme = new MudTheme()
            };
            var light = new PaletteLight();
            var dark = new PaletteDark();

            ApplyPaletteColors(light, ColorMap);
            ApplyPaletteColors(dark, DarkColorMap);

            theme.Theme.PaletteLight = light;
            theme.Theme.PaletteDark = (PaletteDark)dark;

            return theme;
        }

        private static void ApplyPaletteColors(Palette palette, IReadOnlyDictionary<ThemePaletteColor, string> colors)
        {
            foreach (var entry in colors)
            {
                var color = new MudColor(entry.Value);
                switch (entry.Key)
                {
                    case ThemePaletteColor.Primary:
                        palette.Primary = color;
                        break;

                    case ThemePaletteColor.Secondary:
                        palette.Secondary = color;
                        break;

                    case ThemePaletteColor.Tertiary:
                        palette.Tertiary = color;
                        break;

                    case ThemePaletteColor.Info:
                        palette.Info = color;
                        break;

                    case ThemePaletteColor.Success:
                        palette.Success = color;
                        break;

                    case ThemePaletteColor.Warning:
                        palette.Warning = color;
                        break;

                    case ThemePaletteColor.Error:
                        palette.Error = color;
                        break;

                    case ThemePaletteColor.Dark:
                        palette.Dark = color;
                        break;

                    case ThemePaletteColor.Surface:
                        palette.Surface = color;
                        break;

                    case ThemePaletteColor.Background:
                        palette.Background = color;
                        break;

                    case ThemePaletteColor.BackgroundGray:
                        palette.BackgroundGray = color;
                        break;

                    case ThemePaletteColor.DrawerText:
                        palette.DrawerText = color;
                        break;

                    case ThemePaletteColor.DrawerIcon:
                        palette.DrawerIcon = color;
                        break;

                    case ThemePaletteColor.DrawerBackground:
                        palette.DrawerBackground = color;
                        break;

                    case ThemePaletteColor.AppbarText:
                        palette.AppbarText = color;
                        break;

                    case ThemePaletteColor.AppbarBackground:
                        palette.AppbarBackground = color;
                        break;

                    case ThemePaletteColor.LinesDefault:
                        palette.LinesDefault = color;
                        break;

                    case ThemePaletteColor.LinesInputs:
                        palette.LinesInputs = color;
                        break;

                    case ThemePaletteColor.Divider:
                        palette.Divider = color;
                        break;

                    case ThemePaletteColor.DividerLight:
                        palette.DividerLight = color;
                        break;

                    case ThemePaletteColor.TextPrimary:
                        palette.TextPrimary = color;
                        break;

                    case ThemePaletteColor.TextSecondary:
                        palette.TextSecondary = color;
                        break;

                    case ThemePaletteColor.TextDisabled:
                        palette.TextDisabled = color;
                        break;

                    case ThemePaletteColor.ActionDefault:
                        palette.ActionDefault = color;
                        break;

                    case ThemePaletteColor.ActionDisabled:
                        palette.ActionDisabled = color;
                        break;

                    case ThemePaletteColor.ActionDisabledBackground:
                        palette.ActionDisabledBackground = color;
                        break;
                }
            }
        }

        private static MudColor GetPaletteValue(Palette palette, ThemePaletteColor colorType)
        {
            return colorType switch
            {
                ThemePaletteColor.Primary => palette.Primary,
                ThemePaletteColor.Secondary => palette.Secondary,
                ThemePaletteColor.Tertiary => palette.Tertiary,
                ThemePaletteColor.Info => palette.Info,
                ThemePaletteColor.Success => palette.Success,
                ThemePaletteColor.Warning => palette.Warning,
                ThemePaletteColor.Error => palette.Error,
                ThemePaletteColor.Dark => palette.Dark,
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
                ThemePaletteColor.Divider => palette.Divider,
                ThemePaletteColor.DividerLight => palette.DividerLight,
                ThemePaletteColor.TextPrimary => palette.TextPrimary,
                ThemePaletteColor.TextSecondary => palette.TextSecondary,
                ThemePaletteColor.TextDisabled => palette.TextDisabled,
                ThemePaletteColor.ActionDefault => palette.ActionDefault,
                ThemePaletteColor.ActionDisabled => palette.ActionDisabled,
                ThemePaletteColor.ActionDisabledBackground => palette.ActionDisabledBackground,
                _ => palette.Primary
            };
        }

        private static readonly IReadOnlyDictionary<ThemePaletteColor, string> ColorMap =
            new Dictionary<ThemePaletteColor, string>
            {
                { ThemePaletteColor.Primary, "#111111" },
                { ThemePaletteColor.Secondary, "#222222" },
                { ThemePaletteColor.Tertiary, "#333333" },
                { ThemePaletteColor.Info, "#444444" },
                { ThemePaletteColor.Success, "#555555" },
                { ThemePaletteColor.Warning, "#666666" },
                { ThemePaletteColor.Error, "#777777" },
                { ThemePaletteColor.Dark, "#888888" },
                { ThemePaletteColor.Surface, "#999999" },
                { ThemePaletteColor.Background, "#101010" },
                { ThemePaletteColor.BackgroundGray, "#111122" },
                { ThemePaletteColor.DrawerText, "#121212" },
                { ThemePaletteColor.DrawerIcon, "#131313" },
                { ThemePaletteColor.DrawerBackground, "#141414" },
                { ThemePaletteColor.AppbarText, "#151515" },
                { ThemePaletteColor.AppbarBackground, "#161616" },
                { ThemePaletteColor.LinesDefault, "#171717" },
                { ThemePaletteColor.LinesInputs, "#181818" },
                { ThemePaletteColor.Divider, "#191919" },
                { ThemePaletteColor.DividerLight, "#202020" },
                { ThemePaletteColor.TextPrimary, "#212121" },
                { ThemePaletteColor.TextSecondary, "#222233" },
                { ThemePaletteColor.TextDisabled, "#232323" },
                { ThemePaletteColor.ActionDefault, "#242424" },
                { ThemePaletteColor.ActionDisabled, "#252525" },
                { ThemePaletteColor.ActionDisabledBackground, "#262626" }
            };

        private static readonly IReadOnlyDictionary<ThemePaletteColor, string> DarkColorMap =
            new Dictionary<ThemePaletteColor, string>
            {
                { ThemePaletteColor.Primary, "#AAAAAA" },
                { ThemePaletteColor.Secondary, "#BBBBBB" },
                { ThemePaletteColor.Tertiary, "#CCCCCC" },
                { ThemePaletteColor.Info, "#DDDDDD" },
                { ThemePaletteColor.Success, "#EEEEEE" },
                { ThemePaletteColor.Warning, "#FAFAFA" },
                { ThemePaletteColor.Error, "#ABABAB" },
                { ThemePaletteColor.Dark, "#BCBCBC" },
                { ThemePaletteColor.Surface, "#CDCDCD" },
                { ThemePaletteColor.Background, "#DEDEDE" },
                { ThemePaletteColor.BackgroundGray, "#EFEFEF" },
                { ThemePaletteColor.DrawerText, "#111AAA" },
                { ThemePaletteColor.DrawerIcon, "#222BBB" },
                { ThemePaletteColor.DrawerBackground, "#333CCC" },
                { ThemePaletteColor.AppbarText, "#444DDD" },
                { ThemePaletteColor.AppbarBackground, "#555EEE" },
                { ThemePaletteColor.LinesDefault, "#666FFF" },
                { ThemePaletteColor.LinesInputs, "#777AAA" },
                { ThemePaletteColor.Divider, "#888BBB" },
                { ThemePaletteColor.DividerLight, "#999CCC" },
                { ThemePaletteColor.TextPrimary, "#AAAFFF" },
                { ThemePaletteColor.TextSecondary, "#BBBEEE" },
                { ThemePaletteColor.TextDisabled, "#CCCDDD" },
                { ThemePaletteColor.ActionDefault, "#DDDEEE" },
                { ThemePaletteColor.ActionDisabled, "#EEEFFF" },
                { ThemePaletteColor.ActionDisabledBackground, "#FFFFAA" }
            };
    }
}
