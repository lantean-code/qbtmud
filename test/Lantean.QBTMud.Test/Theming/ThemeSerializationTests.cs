using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor;
using MudBlazor.Utilities;
using System.Text.Json.Nodes;

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
        public void GIVEN_JsonWithoutThemeObject_WHEN_Deserialized_THEN_ReturnsDefinitionWithDefaultTheme()
        {
            const string json = "{\"id\":\"Id\",\"name\":\"Name\",\"fontFamily\":\"FontFamily\"}";

            var result = ThemeSerialization.DeserializeDefinition(json);

            result.Should().NotBeNull();
            result!.Theme.Should().NotBeNull();
            result.Id.Should().Be("Id");
        }

        [Fact]
        public void GIVEN_JsonWithThemeButWithoutPalettes_WHEN_Deserialized_THEN_ReturnsDefinition()
        {
            const string json = "{\"id\":\"Id\",\"name\":\"Name\",\"fontFamily\":\"FontFamily\",\"theme\":{}}";

            var result = ThemeSerialization.DeserializeDefinition(json);

            result.Should().NotBeNull();
            result!.Theme.Should().NotBeNull();
            result.Theme.PaletteLight.Should().NotBeNull();
            result.Theme.PaletteDark.Should().NotBeNull();
        }

        [Fact]
        public void GIVEN_JsonWithoutPaletteTypeDiscriminator_WHEN_Deserialized_THEN_ShouldHandleLegacyThemePayload()
        {
            var definition = new ThemeDefinition
            {
                Id = "Id",
                Name = "Name",
                FontFamily = "FontFamily",
                Theme = new MudTheme()
            };
            definition.Theme.PaletteLight.Primary = "#123456";
            definition.Theme.PaletteDark.Primary = "#654321";

            var json = ThemeSerialization.SerializeDefinition(definition, false);
            var root = JsonNode.Parse(json).Should().BeOfType<JsonObject>().Subject;
            var theme = root["theme"].Should().BeOfType<JsonObject>().Subject;
            var paletteLight = theme["paletteLight"].Should().BeOfType<JsonObject>().Subject;
            var paletteDark = theme["paletteDark"].Should().BeOfType<JsonObject>().Subject;
            paletteLight.Remove("$type");
            paletteDark.Remove("$type");

            var result = ThemeSerialization.DeserializeDefinition(root.ToJsonString());

            result.Should().NotBeNull();
            result!.Theme.PaletteLight.Should().BeOfType<PaletteLight>();
            result.Theme.PaletteDark.Should().BeOfType<PaletteDark>();
            result.Theme.PaletteLight.Primary.R.Should().Be((byte)18);
            result.Theme.PaletteLight.Primary.G.Should().Be((byte)52);
            result.Theme.PaletteLight.Primary.B.Should().Be((byte)86);
            result.Theme.PaletteDark.Primary.R.Should().Be((byte)101);
            result.Theme.PaletteDark.Primary.G.Should().Be((byte)67);
            result.Theme.PaletteDark.Primary.B.Should().Be((byte)33);
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
