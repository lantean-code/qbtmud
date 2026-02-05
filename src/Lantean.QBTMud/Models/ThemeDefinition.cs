using MudBlazor;

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
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets the display name of the theme.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the theme configuration.
        /// </summary>
        public MudTheme Theme { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the theme uses right-to-left layout.
        /// </summary>
        public bool RTL { get; set; }

        /// <summary>
        /// Gets or sets the font family used by the theme.
        /// </summary>
        public string FontFamily { get; set; } = "Roboto";

        /// <summary>
        /// Gets or sets the default border radius.
        /// </summary>
        public int DefaultBorderRadius { get; set; } = 4;

        /// <summary>
        /// Gets or sets the default elevation index.
        /// </summary>
        public int DefaultElevation { get; set; } = 1;

        /// <summary>
        /// Gets or sets the app bar elevation.
        /// </summary>
        public int AppBarElevation { get; set; } = 25;

        /// <summary>
        /// Gets or sets the drawer elevation.
        /// </summary>
        public int DrawerElevation { get; set; } = 2;

        /// <summary>
        /// Gets or sets the drawer clip mode.
        /// </summary>
        public DrawerClipMode DrawerClipMode { get; set; } = DrawerClipMode.Never;
    }
}
