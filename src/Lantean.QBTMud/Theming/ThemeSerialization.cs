using Lantean.QBTMud.Models;
using Lantean.QBTMud.Serialization;
using MudBlazor;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides JSON serialization helpers for themes.
    /// </summary>
    public static class ThemeSerialization
    {
        private const string TypeDiscriminatorPropertyName = "$type";
        private const string PaletteLightTypeDiscriminator = nameof(PaletteLight);
        private const string PaletteDarkTypeDiscriminator = nameof(PaletteDark);
        private static readonly JsonSerializerOptions _defaultOptions = CreateSerializerOptions(writeIndented: false);
        private static readonly JsonSerializerOptions _indentedOptions = CreateSerializerOptions(writeIndented: true);

        /// <summary>
        /// Serializes a <see cref="ThemeDefinition"/> to JSON.
        /// </summary>
        /// <param name="definition">The theme definition to serialize.</param>
        /// <param name="writeIndented">Whether to format the JSON output.</param>
        /// <returns>The serialized JSON payload.</returns>
        public static string SerializeDefinition(ThemeDefinition definition, bool writeIndented)
        {
            var options = writeIndented ? _indentedOptions : _defaultOptions;
            return JsonSerializer.Serialize(definition, options);
        }

        /// <summary>
        /// Deserializes a theme definition from JSON.
        /// </summary>
        /// <param name="json">The JSON payload to deserialize.</param>
        /// <returns>The parsed theme definition, or null when the payload is invalid.</returns>
        public static ThemeDefinition? DeserializeDefinition(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var normalizedJson = AddMissingPaletteTypeDiscriminators(json);
            return JsonSerializer.Deserialize<ThemeDefinition>(normalizedJson, _defaultOptions);
        }

        /// <summary>
        /// Creates a deep clone of a <see cref="ThemeDefinition"/> instance.
        /// </summary>
        /// <param name="definition">The theme definition to clone.</param>
        /// <returns>A deep copy of the theme definition.</returns>
        public static ThemeDefinition CloneDefinition(ThemeDefinition definition)
        {
            var json = JsonSerializer.Serialize(definition, _defaultOptions);
            return JsonSerializer.Deserialize<ThemeDefinition>(json, _defaultOptions) ?? new ThemeDefinition();
        }

        /// <summary>
        /// Creates a deep clone of a <see cref="MudTheme"/> instance.
        /// </summary>
        /// <param name="theme">The theme to clone.</param>
        /// <returns>A deep copy of the theme, or a default theme when the input is null.</returns>
        public static MudTheme CloneTheme(MudTheme? theme)
        {
            if (theme is null)
            {
                return new MudTheme();
            }

            var json = JsonSerializer.Serialize(theme, _defaultOptions);
            return JsonSerializer.Deserialize<MudTheme>(json, _defaultOptions) ?? new MudTheme();
        }

        internal static JsonSerializerOptions CreateSerializerOptions(bool writeIndented)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                AllowOutOfOrderMetadataProperties = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = writeIndented
            };

            options.Converters.Add(new MudColorJsonConverter());

            return options;
        }

        private static string AddMissingPaletteTypeDiscriminators(string json)
        {
            JsonNode? rootNode;
            try
            {
                rootNode = JsonNode.Parse(json);
            }
            catch (JsonException)
            {
                return json;
            }

            if (rootNode is not JsonObject rootObject)
            {
                return json;
            }

            if (!TryGetPropertyObject(rootObject, "theme", out var themeObject) || themeObject is null)
            {
                return json;
            }

            var changed = false;
            changed |= EnsureTypeDiscriminator(themeObject, "paletteLight", PaletteLightTypeDiscriminator);
            changed |= EnsureTypeDiscriminator(themeObject, "paletteDark", PaletteDarkTypeDiscriminator);

            if (!changed)
            {
                return json;
            }

            return rootObject.ToJsonString(_defaultOptions);
        }

        private static bool EnsureTypeDiscriminator(JsonObject themeObject, string palettePropertyName, string paletteTypeName)
        {
            if (!TryGetPropertyObject(themeObject, palettePropertyName, out var paletteObject) || paletteObject is null)
            {
                return false;
            }

            if (HasProperty(paletteObject, TypeDiscriminatorPropertyName))
            {
                return false;
            }

            paletteObject[TypeDiscriminatorPropertyName] = paletteTypeName;
            return true;
        }

        private static bool TryGetPropertyObject(JsonObject jsonObject, string propertyName, out JsonObject? propertyValue)
        {
            foreach (var pair in jsonObject)
            {
                if (string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = pair.Value as JsonObject;
                    return propertyValue is not null;
                }
            }

            propertyValue = null;
            return false;
        }

        private static bool HasProperty(JsonObject jsonObject, string propertyName)
        {
            foreach (var pair in jsonObject)
            {
                if (string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
