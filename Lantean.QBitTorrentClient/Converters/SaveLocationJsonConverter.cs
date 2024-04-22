using Lantean.QBitTorrentClient.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Converters
{
    public class SaveLocationJsonConverter : JsonConverter<SaveLocation>
    {
        public override SaveLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return SaveLocation.Create(reader.GetString());
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return SaveLocation.Create(reader.GetInt32());
            }

            throw new JsonException($"Unsupported token type {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, SaveLocation value, JsonSerializerOptions options)
        {
            if (value.IsWatchedFolder)
            {
                writer.WriteNumberValue(0);
            }
            else if (value.IsDefaltFolder)
            {
                writer.WriteNumberValue(1);
            }
            else if (value.SavePath is not null)
            {
                writer.WriteStringValue(value.SavePath);
            }
        }
    }
}