using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Theming
{
    public sealed class ThemeSerializationTests
    {
        [Fact]
        public void GIVEN_Definition_WHEN_SerializedIndented_THEN_OutputIndented()
        {
            var definition = new ThemeDefinition
            {
                Id = "Id",
                Name = "Name",
                FontFamily = "FontFamily",
                Theme = new MudTheme()
            };

            var indented = ThemeSerialization.SerializeDefinition(definition, true);
            var compact = ThemeSerialization.SerializeDefinition(definition, false);

            indented.Should().Contain("\n");
            compact.Should().NotContain("\n");
        }

        [Fact]
        public void GIVEN_WhitespaceJson_WHEN_Deserialized_THEN_ReturnsNull()
        {
            var result = ThemeSerialization.DeserializeDefinition(" ");

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_ValidJson_WHEN_Deserialized_THEN_ReturnsDefinition()
        {
            var definition = new ThemeDefinition
            {
                Id = "Id",
                Name = "Name",
                FontFamily = "FontFamily",
                Theme = new MudTheme()
            };

            var json = ThemeSerialization.SerializeDefinition(definition, false);

            var result = ThemeSerialization.DeserializeDefinition(json);

            result.Should().NotBeNull();
            result!.Id.Should().Be("Id");
            result.Name.Should().Be("Name");
            result.FontFamily.Should().Be("FontFamily");
        }

        [Fact]
        public void GIVEN_Definition_WHEN_Cloned_THEN_ReturnsDeepCopy()
        {
            var definition = new ThemeDefinition
            {
                FontFamily = "FontFamily",
                Theme = new MudTheme()
            };
            definition.Theme.PaletteLight.Primary = "#123456";

            var clone = ThemeSerialization.CloneDefinition(definition);

            clone.Should().NotBeSameAs(definition);
            clone.FontFamily.Should().Be("FontFamily");
            clone.Theme.PaletteLight.Primary.ToString().Should().Be(definition.Theme.PaletteLight.Primary.ToString());
        }

        [Fact]
        public void GIVEN_NullTheme_WHEN_CloneTheme_THEN_ReturnsDefaultTheme()
        {
            var clone = ThemeSerialization.CloneTheme(null);

            clone.Should().NotBeNull();
            clone.PaletteLight.Should().NotBeNull();
            clone.PaletteDark.Should().NotBeNull();
        }

        [Fact]
        public void GIVEN_Theme_WHEN_CloneTheme_THEN_ReturnsDeepCopy()
        {
            var theme = new MudTheme();
            theme.PaletteLight.Primary = new MudColor(1, 2, 3, 4);

            var clone = ThemeSerialization.CloneTheme(theme);

            clone.Should().NotBeSameAs(theme);
            clone.PaletteLight.Should().NotBeSameAs(theme.PaletteLight);
            clone.PaletteLight.Primary.R.Should().Be((byte)1);
            clone.PaletteLight.Primary.G.Should().Be((byte)2);
            clone.PaletteLight.Primary.B.Should().Be((byte)3);
            clone.PaletteLight.Primary.A.Should().Be((byte)4);
        }
    }
}
