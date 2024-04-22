using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Converters
{
    internal class CommaSeparatedJsonConverter : JsonConverter<IReadOnlyList<string>>
    {
        public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Must be of type string.");
            }

            List<string> list;
            var value = reader.GetString();
            if (value is null)
            {
                list = [];
            }
            else
            {
                var values = value.Split(',');
                list = [.. values];
            }

            return list.AsReadOnly();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
        {
            var output = string.Join(',', value);

            writer.WriteStringValue(output);
        }
    }
}