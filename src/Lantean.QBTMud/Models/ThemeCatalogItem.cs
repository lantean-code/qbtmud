namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents a theme entry with source metadata.
    /// </summary>
    public sealed class ThemeCatalogItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeCatalogItem"/> class.
        /// </summary>
        /// <param name="id">The unique theme identifier.</param>
        /// <param name="name">The display name of the theme.</param>
        /// <param name="theme">The theme configuration.</param>
        /// <param name="source">The theme source.</param>
        /// <param name="sourcePath">The source path for server-provided themes.</param>
        public ThemeCatalogItem(string id, string name, ThemeDefinition theme, ThemeSource source, string? sourcePath)
        {
            Id = id;
            Name = name;
            Theme = theme;
            Source = source;
            SourcePath = sourcePath;
        }

        /// <summary>
        /// Gets the unique identifier for the theme.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the display name of the theme.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the theme configuration.
        /// </summary>
        public ThemeDefinition Theme { get; set; }

        /// <summary>
        /// Gets the theme source.
        /// </summary>
        public ThemeSource Source { get; }

        /// <summary>
        /// Gets the source path for server-provided themes, if available.
        /// </summary>
        public string? SourcePath { get; }

        /// <summary>
        /// Gets a value indicating whether the theme is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return Source == ThemeSource.Server; }
        }
    }
}
