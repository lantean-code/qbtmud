using Lantean.QBitTorrentClient.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient
{
    public static class SerializerOptions
    {
        public static JsonSerializerOptions Options { get; }

        static SerializerOptions()
        {
            Options = new JsonSerializerOptions();
            Options.Converters.Add(new StringFloatJsonConverter());
            Options.Converters.Add(new NullableStringFloatJsonConverter());
            Options.Converters.Add(new SaveLocationJsonConverter());
            Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }
    }
}
