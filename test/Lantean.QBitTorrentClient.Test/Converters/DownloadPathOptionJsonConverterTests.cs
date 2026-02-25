using AwesomeAssertions;
using Lantean.QBitTorrentClient.Converters;
using Lantean.QBitTorrentClient.Models;
using System.Text;
using System.Text.Json;

namespace Lantean.QBitTorrentClient.Test.Converters
{
    public class DownloadPathOptionJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(new DownloadPathOptionJsonConverter());
            return o;
        }

        // -------- Read --------

        [Fact]
        public void GIVEN_JsonNull_WHEN_Read_THEN_ShouldReturnNull()
        {
            var options = CreateOptions();
            var json = "null";

            var result = JsonSerializer.Deserialize<DownloadPathOption>(json, options);

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_JsonNull_WHEN_ReadDirectlyWithConverter_THEN_ShouldReturnNull()
        {
            var options = CreateOptions();
            var converter = new DownloadPathOptionJsonConverter();
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("null"));
            reader.Read();

            var result = converter.Read(ref reader, typeof(DownloadPathOption), options);

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_JsonFalse_WHEN_Read_THEN_ShouldReturnDisabledWithNullPath()
        {
            var options = CreateOptions();
            var json = "false";

            var result = JsonSerializer.Deserialize<DownloadPathOption>(json, options);

            result.Should().NotBeNull();
            result!.Enabled.Should().BeFalse();
            result.Path.Should().BeNull();
        }

        [Fact]
        public void GIVEN_JsonTrue_WHEN_Read_THEN_ShouldReturnEnabledWithNullPath()
        {
            var options = CreateOptions();
            var json = "true";

            var result = JsonSerializer.Deserialize<DownloadPathOption>(json, options);

            result.Should().NotBeNull();
            result!.Enabled.Should().BeTrue();
            result.Path.Should().BeNull();
        }

        [Fact]
        public void GIVEN_JsonString_WHEN_Read_THEN_ShouldReturnEnabledWithThatPath()
        {
            var options = CreateOptions();
            var json = "\"/downloads\"";

            var result = JsonSerializer.Deserialize<DownloadPathOption>(json, options);

            result.Should().NotBeNull();
            result!.Enabled.Should().BeTrue();
            result.Path.Should().Be("/downloads");
        }

        [Fact]
        public void GIVEN_UnexpectedToken_WHEN_Read_THEN_ShouldThrowJsonException()
        {
            var options = CreateOptions();
            var json = "123"; // number token, not supported

            var act = () => JsonSerializer.Deserialize<DownloadPathOption>(json, options)!;

            var ex = act.Should().Throw<JsonException>();
            ex.Which.Message.Should().Contain("Unexpected token");
        }

        // -------- Write --------

        [Fact]
        public void GIVEN_NullValue_WHEN_Write_THEN_ShouldEmitJsonNull()
        {
            var options = CreateOptions();
            DownloadPathOption? value = null;

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("null");
        }

        [Fact]
        public void GIVEN_NullValue_WHEN_WrittenDirectlyWithConverter_THEN_ShouldEmitJsonNull()
        {
            var options = CreateOptions();
            var converter = new DownloadPathOptionJsonConverter();
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            converter.Write(writer, null, options);
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            json.Should().Be("null");
        }

        [Fact]
        public void GIVEN_Disabled_WHEN_Write_THEN_ShouldEmitFalse()
        {
            var options = CreateOptions();
            var value = new DownloadPathOption(false, null);

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("false");
        }

        [Fact]
        public void GIVEN_EnabledWithNullPath_WHEN_Write_THEN_ShouldEmitTrue()
        {
            var options = CreateOptions();
            var value = new DownloadPathOption(true, null);

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("true");
        }

        [Fact]
        public void GIVEN_EnabledWithWhitespacePath_WHEN_Write_THEN_ShouldEmitTrue()
        {
            var options = CreateOptions();
            var value = new DownloadPathOption(true, "   ");

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("true");
        }

        [Fact]
        public void GIVEN_EnabledWithPath_WHEN_Write_THEN_ShouldEmitThatString()
        {
            var options = CreateOptions();
            var value = new DownloadPathOption(true, "/dl/path");

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("\"/dl/path\"");
        }

        // -------- Round-trip sanity --------

        [Fact]
        public void GIVEN_PathString_WHEN_RoundTrip_THEN_ShouldPreserveEnabledTrueAndPath()
        {
            var options = CreateOptions();
            var original = new DownloadPathOption(true, "/data");

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<DownloadPathOption>(json, options)!;

            round.Enabled.Should().BeTrue();
            round.Path.Should().Be("/data");
        }
    }
}
