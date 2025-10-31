using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Converters
{
    internal class StringFloatJsonConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (float.TryParse(reader.GetString(), out var value))
                {
                    return value;
                }

                return 0;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetSingle(out var value))
                {
                    return value;
                }

                return 0;
            }

            return 0;
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}