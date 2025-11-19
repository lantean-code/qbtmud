using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Converters
{
    internal sealed class NullableStringFloatJsonConverter : JsonConverter<float?>
    {
        public override float? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.String => ParseString(reader.GetString()),
                JsonTokenType.Number => reader.TryGetSingle(out var number) ? number : null,
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, float? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
        }

        private static float? ParseString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
            {
                return null;
            }

            return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }
    }
}
