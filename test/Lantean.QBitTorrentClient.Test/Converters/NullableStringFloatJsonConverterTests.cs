using AwesomeAssertions;
using Lantean.QBitTorrentClient.Converters;
using System.Text;
using System.Text.Json;

namespace Lantean.QBitTorrentClient.Test.Converters
{
    public sealed class NullableStringFloatJsonConverterTests
    {
        private readonly NullableStringFloatJsonConverter _target;
        private readonly JsonSerializerOptions _options;

        public NullableStringFloatJsonConverterTests()
        {
            _target = new NullableStringFloatJsonConverter();
            _options = new JsonSerializerOptions();
            _options.Converters.Add(_target);
        }

        [Fact]
        public void GIVEN_NullToken_WHEN_Read_THEN_ShouldReturnNull()
        {
            var reader = CreateReader("null");
            var value = _target.Read(ref reader, typeof(float?), _options);

            value.Should().BeNull();
        }

        [Fact]
        public void GIVEN_StringDash_WHEN_Read_THEN_ShouldReturnNull()
        {
            var value = JsonSerializer.Deserialize<float?>("\"-\"", _options);

            value.Should().BeNull();
        }

        [Fact]
        public void GIVEN_WhitespaceString_WHEN_Read_THEN_ShouldReturnNull()
        {
            var value = JsonSerializer.Deserialize<float?>("\"   \"", _options);

            value.Should().BeNull();
        }

        [Fact]
        public void GIVEN_InvalidString_WHEN_Read_THEN_ShouldReturnNull()
        {
            var value = JsonSerializer.Deserialize<float?>("\"invalid-value\"", _options);

            value.Should().BeNull();
        }

        [Fact]
        public void GIVEN_StringNumberWithThousands_WHEN_Read_THEN_ShouldReturnValue()
        {
            var value = JsonSerializer.Deserialize<float?>("\"1,234.5\"", _options);

            value.Should().Be(1234.5f);
        }

        [Fact]
        public void GIVEN_NumberToken_WHEN_Read_THEN_ShouldReturnValue()
        {
            var value = JsonSerializer.Deserialize<float?>("123.5", _options);

            value.Should().Be(123.5f);
        }

        [Fact]
        public void GIVEN_OutOfRangeNumberToken_WHEN_Read_THEN_ShouldReturnPositiveInfinity()
        {
            var value = JsonSerializer.Deserialize<float?>("1e50", _options);

            value.Should().Be(float.PositiveInfinity);
        }

        [Fact]
        public void GIVEN_UnsupportedToken_WHEN_Read_THEN_ShouldReturnNull()
        {
            var value = JsonSerializer.Deserialize<float?>("true", _options);

            value.Should().BeNull();
        }

        [Fact]
        public void GIVEN_NullValue_WHEN_Write_THEN_ShouldEmitNull()
        {
            var json = Write((float?)null);

            json.Should().Be("null");
        }

        [Fact]
        public void GIVEN_Value_WHEN_Write_THEN_ShouldEmitInvariantString()
        {
            var json = Write(1.25f);

            json.Should().Be("\"1.25\"");
        }

        private static Utf8JsonReader CreateReader(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);
            reader.Read();
            return reader;
        }

        private string Write(float? value)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new Utf8JsonWriter(memoryStream);
            _target.Write(writer, value, _options);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
}
