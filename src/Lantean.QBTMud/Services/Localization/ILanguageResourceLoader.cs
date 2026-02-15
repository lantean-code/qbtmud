namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Loads localization resources required by the WebUI localizer.
    /// </summary>
    public interface ILanguageResourceLoader
    {
        /// <summary>
        /// Ensures localization resources are initialized for the current UI culture.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        ValueTask EnsureInitialized(CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads aliases, base translations, and overrides for the specified locale.
        /// </summary>
        /// <param name="locale">The locale to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous loading operation.</returns>
        ValueTask LoadLocaleAsync(string locale, CancellationToken cancellationToken = default);
    }
}
