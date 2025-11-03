using AwesomeAssertions;
using Lantean.QBitTorrentClient.Converters;
using System.Text.Json;

namespace Lantean.QBitTorrentClient.Test.Converters
{
    public class CommaSeparatedJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var o = new JsonSerializerOptions();
            o.Converters.Add(new CommaSeparatedJsonConverter());
            return o;
        }

        [Fact]
        public void GIVEN_EmptyString_WHEN_Read_THEN_ShouldReturnEmptyReadOnlyList()
        {
            var options = CreateOptions();
            var json = "\"\"";

            var result = JsonSerializer.Deserialize<IReadOnlyList<string>>(json, options);

            result.Should().NotBeNull();
            result!.Count.Should().Be(0);
        }

        [Fact]
        public void GIVEN_NullToken_WHEN_Read_THEN_ShouldReturnNull()
        {
            var options = CreateOptions();
            var json = "null";

            var result = JsonSerializer.Deserialize<IReadOnlyList<string>?>(json, options);

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_CommaSeparatedWithSpacesAndEmpties_WHEN_Read_THEN_ShouldSplitTrimAndRemoveEmpties()
        {
            var options = CreateOptions();
            // contains spaces and empty segments between commas
            var json = "\"  alpha ,  , beta ,,  , gamma  \"";

            var result = JsonSerializer.Deserialize<IReadOnlyList<string>>(json, options);

            result.Should().NotBeNull();
            result!.Count.Should().Be(3);
            result[0].Should().Be("alpha");
            result[1].Should().Be("beta");
            result[2].Should().Be("gamma");
        }

        [Fact]
        public void GIVEN_NonStringToken_WHEN_Read_THEN_ShouldThrowJsonException()
        {
            var options = CreateOptions();
            var json = "123"; // number token, not a string

            var act = () => JsonSerializer.Deserialize<IReadOnlyList<string>>(json, options)!;

            var ex = act.Should().Throw<JsonException>();
            ex.Which.Message.Should().Be("Must be of type string.");
        }

        [Fact]
        public void GIVEN_List_WHEN_Write_THEN_ShouldOutputSingleJsonStringCommaJoined()
        {
            var options = CreateOptions();
            IReadOnlyList<string> value = new[] { "a", "b", "c" };

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("\"a,b,c\"");
        }

        [Fact]
        public void GIVEN_EmptyList_WHEN_Write_THEN_ShouldOutputEmptyJsonString()
        {
            var options = CreateOptions();
            IReadOnlyList<string> value = Array.Empty<string>();

            var json = JsonSerializer.Serialize(value, options);

            json.Should().Be("\"\"");
        }

        [Fact]
        public void GIVEN_ReadResult_WHEN_AttemptToMutate_THEN_ShouldThrowNotSupportedException()
        {
            var options = CreateOptions();
            var json = "\"x,y\"";

            var result = JsonSerializer.Deserialize<IReadOnlyList<string>>(json, options)!;

            // Converter returns list.AsReadOnly() -> ReadOnlyCollection<string>
            var asList = (IList<string>)result;
            var act = () => asList.Add("z");

            act.Should().Throw<NotSupportedException>();
        }
    }
}
