using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides access to the theme catalog and current theme selection.
    /// </summary>
    public interface IThemeManagerService
    {
        /// <summary>
        /// Occurs when the active theme changes.
        /// </summary>
        event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Gets the available themes.
        /// </summary>
        IReadOnlyList<ThemeCatalogItem> Themes { get; }

        /// <summary>
        /// Gets the currently applied theme.
        /// </summary>
        ThemeCatalogItem? CurrentTheme { get; }

        /// <summary>
        /// Gets the currently applied theme identifier.
        /// </summary>
        string? CurrentThemeId { get; }

        /// <summary>
        /// Gets the currently applied font family.
        /// </summary>
        string CurrentFontFamily { get; }

        /// <summary>
        /// Ensures the theme catalog has been loaded.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureInitialized();

        /// <summary>
        /// Reloads server-provided themes and rebuilds the catalog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ReloadServerThemes();

        /// <summary>
        /// Applies the specified theme.
        /// </summary>
        /// <param name="themeId">The theme identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ApplyTheme(string themeId);

        /// <summary>
        /// Saves a local theme definition.
        /// </summary>
        /// <param name="definition">The theme definition to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveLocalTheme(ThemeDefinition definition);

        /// <summary>
        /// Deletes a local theme definition.
        /// </summary>
        /// <param name="themeId">The theme identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteLocalTheme(string themeId);
    }
}
