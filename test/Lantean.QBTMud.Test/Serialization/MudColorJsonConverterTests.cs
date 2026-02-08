using AwesomeAssertions;
using Lantean.QBTMud.Serialization;
using MudBlazor.Utilities;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Serialization
{
    public sealed class MudColorJsonConverterTests
    {
        private readonly JsonSerializerOptions _options;

        public MudColorJsonConverterTests()
        {
            _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            _options.Converters.Add(new MudColorJsonConverter());
        }

        [Fact]
        public void GIVEN_NullJson_WHEN_Deserialized_THEN_ReturnsDefaultColor()
        {
            var color = JsonSerializer.Deserialize<MudColor>("null", _options);

            color.Should().NotBeNull();
            color!.R.Should().Be(0);
            color.G.Should().Be(0);
            color.B.Should().Be(0);
            color.A.Should().Be(255);
        }

        [Fact]
        public void GIVEN_WhitespaceStringJson_WHEN_Deserialized_THEN_ReturnsDefaultColor()
        {
            var color = JsonSerializer.Deserialize<MudColor>("\" \"", _options);

            color.Should().NotBeNull();
            color!.R.Should().Be(0);
            color.G.Should().Be(0);
            color.B.Should().Be(0);
            color.A.Should().Be(255);
        }

        [Fact]
        public void GIVEN_StringColorJson_WHEN_Deserialized_THEN_ParsesColor()
        {
            var color = JsonSerializer.Deserialize<MudColor>("\"#01020304\"", _options);

            color.Should().NotBeNull();
            color!.R.Should().Be(1);
            color.G.Should().Be(2);
            color.B.Should().Be(3);
            color.A.Should().Be(4);
        }

        [Fact]
        public void GIVEN_ObjectColorJson_WHEN_Deserialized_THEN_ParsesColor()
        {
            var color = JsonSerializer.Deserialize<MudColor>("{\"r\":1,\"g\":2,\"b\":3,\"a\":4}", _options);

            color.Should().NotBeNull();
            color!.R.Should().Be(1);
            color.G.Should().Be(2);
            color.B.Should().Be(3);
            color.A.Should().Be(4);
        }

        [Fact]
        public void GIVEN_ObjectColorJsonWithUpperCaseKeys_WHEN_Deserialized_THEN_ParsesColor()
        {
            var color = JsonSerializer.Deserialize<MudColor>("{\"R\":1,\"G\":2,\"B\":3,\"A\":4}", _options);

            color.Should().NotBeNull();
            color!.R.Should().Be(1);
            color.G.Should().Be(2);
            color.B.Should().Be(3);
            color.A.Should().Be(4);
        }

        [Fact]
        public void GIVEN_ObjectColorJsonWithNonNumericComponent_WHEN_Deserialized_THEN_IgnoresComponent()
        {
            var color = JsonSerializer.Deserialize<MudColor>("{\"r\":\"invalid\",\"g\":2,\"b\":3,\"a\":4}", _options);

            color.Should().NotBeNull();
            color!.R.Should().Be(0);
            color.G.Should().Be(2);
            color.B.Should().Be(3);
            color.A.Should().Be(4);
        }

        [Fact]
        public void GIVEN_ObjectColorJsonWithOutOfRangeComponent_WHEN_Deserialized_THEN_Throws()
        {
            var act = () => JsonSerializer.Deserialize<MudColor>("{\"r\":256,\"g\":2,\"b\":3,\"a\":4}", _options);

            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void GIVEN_NumberJson_WHEN_Deserialized_THEN_Throws()
        {
            var act = () => JsonSerializer.Deserialize<MudColor>("123", _options);

            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void GIVEN_TruncatedObjectJson_WHEN_Deserialized_THEN_Throws()
        {
            var act = () => JsonSerializer.Deserialize<MudColor>("{\"r\":1", _options);

            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void GIVEN_Color_WHEN_Serialized_THEN_WritesRgbaObject()
        {
            var json = JsonSerializer.Serialize(new MudColor(1, 2, 3, 4), _options);

            json.Should().Be("{\"r\":1,\"g\":2,\"b\":3,\"a\":4}");
        }
    }
}
