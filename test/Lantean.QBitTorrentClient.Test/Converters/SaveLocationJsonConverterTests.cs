using AwesomeAssertions;
using Lantean.QBitTorrentClient.Converters;
using Lantean.QBitTorrentClient.Models;
using System.Text.Json;

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
        public void GIVEN_String_WHEN_Read_THEN_ShouldReturnCustomPath()
        {
            var options = CreateOptions();
            var json = "\"/downloads\"";

            var result = JsonSerializer.Deserialize<SaveLocation>(json, options);

            result.Should().NotBeNull();
            result.SavePath.Should().Be("/downloads");
            result.IsDefaultFolder.Should().BeFalse();
            result.IsWatchedFolder.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NumberZero_WHEN_Read_THEN_ShouldReturnWatchedFolder()
        {
            var options = CreateOptions();
            var json = "0";

            var result = JsonSerializer.Deserialize<SaveLocation>(json, options);

            result.Should().NotBeNull();
            result.IsWatchedFolder.Should().BeTrue();
            result.IsDefaultFolder.Should().BeFalse();
            result.SavePath.Should().BeNull();
        }

        [Fact]
        public void GIVEN_NumberOne_WHEN_Read_THEN_ShouldReturnDefaultFolder()
        {
            var options = CreateOptions();
            var json = "1";

            var result = JsonSerializer.Deserialize<SaveLocation>(json, options);

            result.Should().NotBeNull();
            result.IsDefaultFolder.Should().BeTrue();
            result.IsWatchedFolder.Should().BeFalse();
            result.SavePath.Should().BeNull();
        }

        [Fact]
        public void GIVEN_UnsupportedToken_WHEN_Read_THEN_ShouldThrowJsonException()
        {
            var options = CreateOptions();
            var json = "true"; // bool token is not supported

            var act = () => JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            var ex = act.Should().Throw<JsonException>();
            ex.Which.Message.Should().Contain("Unsupported token type");
        }

        // -------- Write --------

        [Fact]
        public void GIVEN_WatchedFolder_WHEN_Write_THEN_ShouldEmitZero()
        {
            var options = CreateOptions();
            var value = SaveLocation.Create(0);

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("0");
        }

        [Fact]
        public void GIVEN_DefaultFolder_WHEN_Write_THEN_ShouldEmitOne()
        {
            var options = CreateOptions();
            var value = SaveLocation.Create(1);

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("1");
        }

        [Fact]
        public void GIVEN_CustomPath_WHEN_Write_THEN_ShouldEmitJsonString()
        {
            var options = CreateOptions();
            var value = SaveLocation.Create("/data/films");

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("\"/data/films\"");
        }

        // -------- Round-trip sanity --------

        [Fact]
        public void GIVEN_PathString_WHEN_RoundTrip_THEN_ShouldPreserveCustomPath()
        {
            var options = CreateOptions();
            var original = SaveLocation.Create("/data");

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            round.SavePath.Should().Be("/data");
            round.IsDefaultFolder.Should().BeFalse();
            round.IsWatchedFolder.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Zero_WHEN_RoundTrip_THEN_ShouldStayWatchedFolder()
        {
            var options = CreateOptions();
            var original = SaveLocation.Create(0);

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            round.IsWatchedFolder.Should().BeTrue();
            round.IsDefaultFolder.Should().BeFalse();
            round.SavePath.Should().BeNull();
        }

        [Fact]
        public void GIVEN_One_WHEN_RoundTrip_THEN_ShouldStayDefaultFolder()
        {
            var options = CreateOptions();
            var original = SaveLocation.Create(1);

            var json = JsonSerializer.Serialize(original, options);
            var round = JsonSerializer.Deserialize<SaveLocation>(json, options)!;

            round.IsDefaultFolder.Should().BeTrue();
            round.IsWatchedFolder.Should().BeFalse();
            round.SavePath.Should().BeNull();
        }

        [Fact]
        public void GIVEN_ObjectInt_WHEN_Create_THEN_ShouldUseIntegerBranch()
        {
            var location = SaveLocation.Create((object)0);

            location.IsWatchedFolder.Should().BeTrue();
            location.IsDefaultFolder.Should().BeFalse();
            location.SavePath.Should().BeNull();
        }

        [Fact]
        public void GIVEN_ObjectString_WHEN_Create_THEN_ShouldUseStringBranch()
        {
            var location = SaveLocation.Create((object)"1");

            location.IsDefaultFolder.Should().BeTrue();
            location.IsWatchedFolder.Should().BeFalse();
            location.SavePath.Should().BeNull();
        }

        [Fact]
        public void GIVEN_UnsupportedObject_WHEN_Create_THEN_ShouldThrowArgumentOutOfRangeException()
        {
            var act = () => SaveLocation.Create((object)true);

            var ex = act.Should().Throw<ArgumentOutOfRangeException>();
            ex.Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void GIVEN_InvalidInteger_WHEN_Create_THEN_ShouldThrowArgumentOutOfRangeException()
        {
            var act = () => SaveLocation.Create(2);

            var ex = act.Should().Throw<ArgumentOutOfRangeException>();
            ex.Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void GIVEN_NullString_WHEN_Create_THEN_ShouldThrowArgumentOutOfRangeException()
        {
            var act = () => SaveLocation.Create((string?)null);

            var ex = act.Should().Throw<ArgumentOutOfRangeException>();
            ex.Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void GIVEN_StringZero_WHEN_Create_THEN_ShouldReturnWatchedFolder()
        {
            var location = SaveLocation.Create("0");

            location.IsWatchedFolder.Should().BeTrue();
            location.IsDefaultFolder.Should().BeFalse();
            location.SavePath.Should().BeNull();
        }

        [Fact]
        public void GIVEN_WatchedFolder_WHEN_ToValue_THEN_ShouldReturnZero()
        {
            var value = SaveLocation.Create(0).ToValue();

            value.Should().Be(0);
        }

        [Fact]
        public void GIVEN_DefaultFolder_WHEN_ToValue_THEN_ShouldReturnOne()
        {
            var value = SaveLocation.Create(1).ToValue();

            value.Should().Be(1);
        }

        [Fact]
        public void GIVEN_SavePath_WHEN_ToValue_THEN_ShouldReturnPathString()
        {
            var value = SaveLocation.Create("/data").ToValue();

            value.Should().Be("/data");
        }

        [Fact]
        public void GIVEN_EmptySaveLocation_WHEN_ToValue_THEN_ShouldThrowInvalidOperationException()
        {
            var target = new SaveLocation();

            var act = () => target.ToValue();

            act.Should().Throw<InvalidOperationException>().WithMessage("Invalid value.");
        }

        [Fact]
        public void GIVEN_SavePath_WHEN_ToString_THEN_ShouldReturnPathString()
        {
            var result = SaveLocation.Create("/downloads").ToString();

            result.Should().Be("/downloads");
        }
    }
}
