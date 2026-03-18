using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor;

namespace Lantean.QBTMud.Test.Theming
{
    public sealed class ThemePaletteScalarHelperTests
    {
        public static IEnumerable<object[]> ScalarValues
        {
            get
            {
                foreach (var entry in ValueMap)
                {
                    yield return new object[] { entry.Key, entry.Value };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ScalarValues))]
        public void GIVEN_ScalarValue_WHEN_GetValueFromLightPalette_THEN_ReturnsExpected(ThemePaletteScalar valueType, double expected)
        {
            var theme = CreateTheme();

            var result = ThemePaletteScalarHelper.GetValue(theme, valueType, false);

            result.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(ScalarValues))]
        public void GIVEN_ScalarValue_WHEN_SetValueOnLightPalette_THEN_UpdatesExpected(ThemePaletteScalar valueType, double expected)
        {
            var theme = CreateTheme();
            const double value = 0.42;

            ThemePaletteScalarHelper.GetValue(theme, valueType, false).Should().Be(expected);

            ThemePaletteScalarHelper.SetValue(theme, valueType, value, false);

            ThemePaletteScalarHelper.GetValue(theme, valueType, false).Should().Be(value);
        }

        [Fact]
        public void GIVEN_DarkPalette_WHEN_GetValue_THEN_ReturnsDarkValue()
        {
            var theme = CreateTheme();

            var result = ThemePaletteScalarHelper.GetValue(theme, ThemePaletteScalar.BorderOpacity, true);

            result.Should().Be(DarkValueMap[ThemePaletteScalar.BorderOpacity]);
        }

        [Fact]
        public void GIVEN_DarkPalette_WHEN_SetValue_THEN_UpdatesDarkPalette()
        {
            var theme = CreateTheme();

            ThemePaletteScalarHelper.SetValue(theme, ThemePaletteScalar.BorderOpacity, 0.99, true);

            theme.Theme.PaletteDark.BorderOpacity.Should().Be(0.99);
        }

        [Fact]
        public void GIVEN_UnknownScalar_WHEN_GetValue_THEN_ReturnsBorderOpacity()
        {
            var theme = CreateTheme();
            var unknown = (ThemePaletteScalar)999;

            var result = ThemePaletteScalarHelper.GetValue(theme, unknown, false);

            result.Should().Be(ValueMap[ThemePaletteScalar.BorderOpacity]);
        }

        [Fact]
        public void GIVEN_UnknownScalar_WHEN_SetValue_THEN_DoesNotChangePalette()
        {
            var theme = CreateTheme();
            var unknown = (ThemePaletteScalar)999;

            ThemePaletteScalarHelper.SetValue(theme, unknown, 0.01, false);

            theme.Theme.PaletteLight.BorderOpacity.Should().Be(ValueMap[ThemePaletteScalar.BorderOpacity]);
        }

        private static ThemeDefinition CreateTheme()
        {
            var theme = new ThemeDefinition
            {
                Theme = new MudTheme()
            };

            ApplyScalarValues(theme.Theme.PaletteLight, ValueMap);
            ApplyScalarValues(theme.Theme.PaletteDark, DarkValueMap);

            return theme;
        }

        private static void ApplyScalarValues(Palette palette, IReadOnlyDictionary<ThemePaletteScalar, double> values)
        {
            foreach (var entry in values)
            {
                switch (entry.Key)
                {
                    case ThemePaletteScalar.BorderOpacity:
                        palette.BorderOpacity = entry.Value;
                        break;

                    case ThemePaletteScalar.HoverOpacity:
                        palette.HoverOpacity = entry.Value;
                        break;

                    case ThemePaletteScalar.RippleOpacity:
                        palette.RippleOpacity = entry.Value;
                        break;

                    case ThemePaletteScalar.RippleOpacitySecondary:
                        palette.RippleOpacitySecondary = entry.Value;
                        break;
                }
            }
        }

        private static readonly IReadOnlyDictionary<ThemePaletteScalar, double> ValueMap =
            new Dictionary<ThemePaletteScalar, double>
            {
                { ThemePaletteScalar.BorderOpacity, 0.11 },
                { ThemePaletteScalar.HoverOpacity, 0.22 },
                { ThemePaletteScalar.RippleOpacity, 0.33 },
                { ThemePaletteScalar.RippleOpacitySecondary, 0.44 }
            };

        private static readonly IReadOnlyDictionary<ThemePaletteScalar, double> DarkValueMap =
            new Dictionary<ThemePaletteScalar, double>
            {
                { ThemePaletteScalar.BorderOpacity, 0.55 },
                { ThemePaletteScalar.HoverOpacity, 0.66 },
                { ThemePaletteScalar.RippleOpacity, 0.77 },
                { ThemePaletteScalar.RippleOpacitySecondary, 0.88 }
            };
    }
}
