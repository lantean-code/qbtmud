using Lantean.QBitTorrentClient.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Converters
{
    internal sealed class DownloadPathOptionJsonConverter : JsonConverter<DownloadPathOption>
    {
        public override DownloadPathOption? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.False => new DownloadPathOption(false, null),
                JsonTokenType.True => new DownloadPathOption(true, null),
                JsonTokenType.String => new DownloadPathOption(true, reader.GetString()),
                _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing download_path.")
            };
        }

        public override void Write(Utf8JsonWriter writer, DownloadPathOption? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (!value.Enabled)
            {
                writer.WriteBooleanValue(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(value.Path))
            {
                writer.WriteBooleanValue(true);
                return;
            }

            writer.WriteStringValue(value.Path);
        }
    }
}