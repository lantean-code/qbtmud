using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBitTorrentClient.Converters;

namespace Lantean.QBitTorrentClient.Test.Converters
{
    public class StringFloatJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(new StringFloatJsonConverter());
            return o;
        }

        // -------- Read --------

        [Fact]
        public async Task GIVEN_StringNumber_WHEN_Read_THEN_ShouldParseFloat()
        {
            var options = CreateOptions();
            var json = "\"42\"";

            var result = JsonSerializer.Deserialize<float>(json, options);

            result.Should().Be(42f);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_StringInvalid_WHEN_Read_THEN_ShouldReturnZero()
        {
            var options = CreateOptions();
            var json = "\"not-a-number\"";

            var result = JsonSerializer.Deserialize<float>(json, options);

            result.Should().Be(0f);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_NumberToken_WHEN_Read_THEN_ShouldReadSingle()
        {
            var options = CreateOptions();
            var json = "123.0";

            var result = JsonSerializer.Deserialize<float>(json, options);

            result.Should().Be(123f);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_NullToken_WHEN_Read_THEN_ShouldReturnZero()
        {
            var options = CreateOptions();
            var json = "null";

            var result = JsonSerializer.Deserialize<float>(json, options);

            result.Should().Be(0f);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_UnsupportedToken_WHEN_Read_THEN_ShouldReturnZero()
        {
            var options = CreateOptions();
            var json = "true"; // bool token -> converter returns 0

            var result = JsonSerializer.Deserialize<float>(json, options);

            result.Should().Be(0f);
            await Task.CompletedTask;
        }

        // -------- Write --------

        [Fact]
        public async Task GIVEN_IntegerValue_WHEN_Write_THEN_ShouldEmitJsonStringContainingThatValue()
        {
            var options = CreateOptions();
            float value = 42f;

            var json = JsonSerializer.Serialize(value, options);

            // Should be a JSON string (quotes)
            json.Should().StartWith("\"").And.EndWith("\"");

            // Remove quotes and ensure it parses back to the original value
            var inner = json.Trim('"');
            float.TryParse(inner, out var parsed).Should().BeTrue();
            parsed.Should().Be(42f);

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_FractionalValue_WHEN_Write_THEN_ShouldEmitJsonStringParsableToSameValue()
        {
            var options = CreateOptions();
            float value = 1.5f; // exactly representable

            var json = JsonSerializer.Serialize(value, options);

            json.Should().StartWith("\"").And.EndWith("\"");
            var inner = json.Trim('"');

            float.TryParse(inner, out var parsed).Should().BeTrue();
            parsed.Should().Be(1.5f);

            await Task.CompletedTask;
        }

        // -------- Round-trip --------

        [Fact]
        public async Task GIVEN_Value_WHEN_RoundTrip_THEN_ShouldPreserveValue()
        {
            var options = CreateOptions();
            float original = 1.5f;

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<float>(json, options);

            round.Should().Be(1.5f);
            await Task.CompletedTask;
        }
    }
}
