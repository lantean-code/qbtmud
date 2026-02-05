using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor;

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
    }
}
