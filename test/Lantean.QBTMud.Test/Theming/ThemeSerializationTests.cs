using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor.ThemeManager;

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
                Theme = new ThemeManagerTheme
                {
                    FontFamily = "FontFamily"
                }
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
                Theme = new ThemeManagerTheme
                {
                    FontFamily = "FontFamily"
                }
            };

            var json = ThemeSerialization.SerializeDefinition(definition, false);

            var result = ThemeSerialization.DeserializeDefinition(json);

            result.Should().NotBeNull();
            result!.Id.Should().Be("Id");
            result.Name.Should().Be("Name");
            result.Theme.FontFamily.Should().Be("FontFamily");
        }

        [Fact]
        public void GIVEN_Theme_WHEN_Cloned_THEN_ReturnsDeepCopy()
        {
            var theme = new ThemeManagerTheme
            {
                FontFamily = "FontFamily"
            };
            theme.Theme.PaletteLight.Primary = "#123456";

            var clone = ThemeSerialization.CloneTheme(theme);

            clone.Should().NotBeSameAs(theme);
            clone.FontFamily.Should().Be("FontFamily");
            clone.Theme.PaletteLight.Primary.ToString().Should().Be(theme.Theme.PaletteLight.Primary.ToString());
        }
    }
}
