using MudBlazor.ThemeManager;

namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents a serializable theme definition.
    /// </summary>
    public sealed class ThemeDefinition
    {
        /// <summary>
        /// Gets the unique identifier for the theme.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets the display name of the theme.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the theme configuration.
        /// </summary>
        public ThemeManagerTheme Theme { get; init; } = new();
    }
}
