using MudBlazor.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBTMud.Serialization
{
    /// <summary>
    /// Provides a stable JSON representation for <see cref="MudColor"/> that does not rely on constructor parameter names.
    /// </summary>
    internal sealed class MudColorJsonConverter : JsonConverter<MudColor>
    {
        public override bool HandleNull
        {
            get { return true; }
        }

        public override MudColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new MudColor();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    return new MudColor();
                }

                return new MudColor(value);
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected color payload to be a string or object, but found '{reader.TokenType}'.");
            }

            byte r = 0;
            byte g = 0;
            byte b = 0;
            byte a = 255;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new MudColor(r, g, b, a);
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected a property name token, but found '{reader.TokenType}'.");
                }

                var isRed = reader.ValueTextEquals("r") || reader.ValueTextEquals("R");
                var isGreen = reader.ValueTextEquals("g") || reader.ValueTextEquals("G");
                var isBlue = reader.ValueTextEquals("b") || reader.ValueTextEquals("B");
                var isAlpha = reader.ValueTextEquals("a") || reader.ValueTextEquals("A");

                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of color payload.");
                }

                if (reader.TokenType != JsonTokenType.Number)
                {
                    reader.Skip();
                    continue;
                }

                var component = reader.GetInt32();
                if (component is < 0 or > 255)
                {
                    throw new JsonException($"Color components must be between 0 and 255, but found '{component}'.");
                }

                var value = (byte)component;
                if (isRed)
                {
                    r = value;
                }
                else if (isGreen)
                {
                    g = value;
                }
                else if (isBlue)
                {
                    b = value;
                }
                else if (isAlpha)
                {
                    a = value;
                }
            }

            throw new JsonException("Unexpected end of color payload.");
        }

        public override void Write(Utf8JsonWriter writer, MudColor value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.R);
            writer.WriteNumber("g", value.G);
            writer.WriteNumber("b", value.B);
            writer.WriteNumber("a", value.A);
            writer.WriteEndObject();
        }
    }
}
