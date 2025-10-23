using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBitTorrentClient.Converters;
using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBitTorrentClient.Test.Converters
{
    public class SaveLocationJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(new SaveLocationJsonConverter());
            return o;
        }

        // -------- Read --------

        [Fact]
        public async Task GIVEN_String_WHEN_Read_THEN_ShouldReturnCustomPath()
        {
            var options = CreateOptions();
            var json = "\"/downloads\"";

            var result = JsonSerializer.Deserialize<SaveLocation>(json, options);

            result.Should().NotBeNull();
            result.SavePath.Should().Be("/downloads");
            result.IsDefaultFolder.Should().BeFalse();
            result.IsWatchedFolder.Should().BeFalse();

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_NumberZero_WHEN_Read_THEN_ShouldReturnWatchedFolder()
        {
            var options = CreateOptions();
            var json = "0";

            var result = JsonSerializer.Deserialize<SaveLocation>(json, options);

            result.Should().NotBeNull();
            result.IsWatchedFolder.Should().BeTrue();
            result.IsDefaultFolder.Should().BeFalse();
            result.SavePath.Should().BeNull();

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_NumberOne_WHEN_Read_THEN_ShouldReturnDefaultFolder()
        {
            var options = CreateOptions();
            var json = "1";

            var result = JsonSerializer.Deserialize<SaveLocation>(json, options);

            result.Should().NotBeNull();
            result.IsDefaultFolder.Should().BeTrue();
            result.IsWatchedFolder.Should().BeFalse();
            result.SavePath.Should().BeNull();

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_UnsupportedToken_WHEN_Read_THEN_ShouldThrowJsonException()
        {
            var options = CreateOptions();
            var json = "true"; // bool token is not supported

            var act = async () => JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            var ex = await act.Should().ThrowAsync<JsonException>();
            ex.Which.Message.Should().Contain("Unsupported token type");

            await Task.CompletedTask;
        }

        // -------- Write --------

        [Fact]
        public async Task GIVEN_WatchedFolder_WHEN_Write_THEN_ShouldEmitZero()
        {
            var options = CreateOptions();
            var value = SaveLocation.Create(0);

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("0");
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_DefaultFolder_WHEN_Write_THEN_ShouldEmitOne()
        {
            var options = CreateOptions();
            var value = SaveLocation.Create(1);

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("1");
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_CustomPath_WHEN_Write_THEN_ShouldEmitJsonString()
        {
            var options = CreateOptions();
            var value = SaveLocation.Create("/data/films");

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("\"/data/films\"");
            await Task.CompletedTask;
        }

        // -------- Round-trip sanity --------

        [Fact]
        public async Task GIVEN_PathString_WHEN_RoundTrip_THEN_ShouldPreserveCustomPath()
        {
            var options = CreateOptions();
            var original = SaveLocation.Create("/data");

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            round.SavePath.Should().Be("/data");
            round.IsDefaultFolder.Should().BeFalse();
            round.IsWatchedFolder.Should().BeFalse();

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_Zero_WHEN_RoundTrip_THEN_ShouldStayWatchedFolder()
        {
            var options = CreateOptions();
            var original = SaveLocation.Create(0);

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            round.IsWatchedFolder.Should().BeTrue();
            round.IsDefaultFolder.Should().BeFalse();
            round.SavePath.Should().BeNull();

            await Task.CompletedTask;
        }

        [Fact]
        public async Task GIVEN_One_WHEN_RoundTrip_THEN_ShouldStayDefaultFolder()
        {
            var options = CreateOptions();
            var original = SaveLocation.Create(1);

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            round.IsDefaultFolder.Should().BeTrue();
            round.IsWatchedFolder.Should().BeFalse();
            round.SavePath.Should().BeNull();

            await Task.CompletedTask;
        }
    }
}
