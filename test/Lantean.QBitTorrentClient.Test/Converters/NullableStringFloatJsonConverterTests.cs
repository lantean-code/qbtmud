using AwesomeAssertions;
using Lantean.QBitTorrentClient.Converters;
using System.Text.Json;

namespace Lantean.QBitTorrentClient.Test.Converters
{
    public class NullableStringFloatJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new NullableStringFloatJsonConverter());
            return options;
        }

        [Fact]
        public async Task GIVEN_StringDash_WHEN_Read_THEN_ShouldReturnNull()
        {
            var options = CreateOptions();

            var value = JsonSerializer.Deserialize<float?>("\"-\"", options);

            value.Should().BeNull();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_StringNumber_WHEN_Read_THEN_ShouldReturnValue()
        {
            var options = CreateOptions();

            var value = JsonSerializer.Deserialize<float?>("\"123.5\"", options);

            value.Should().Be(123.5f);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_Value_WHEN_Write_THEN_ShouldEmitString()
        {
            var options = CreateOptions();

            var json = JsonSerializer.Serialize((float?)1.25f, options);

            json.Should().Be("\"1.25\"");
            await Task.CompletedTask;
        }
    }
}
