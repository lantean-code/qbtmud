using Lantean.QBTMud.Models;
using Lantean.QBTMud.Serialization;
using System.Text.Json;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides JSON serialization helpers for themes.
    /// </summary>
    public static class ThemeSerialization
    {
        private static readonly JsonSerializerOptions _defaultOptions = CreateOptions(writeIndented: false);
        private static readonly JsonSerializerOptions _indentedOptions = CreateOptions(writeIndented: true);

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

            return JsonSerializer.Deserialize<ThemeDefinition>(json, _defaultOptions);
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

        private static JsonSerializerOptions CreateOptions(bool writeIndented)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = writeIndented
            };

            options.Converters.Add(new MudColorJsonConverter());

            return options;
        }
    }
}
