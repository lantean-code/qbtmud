namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Loads WebUI localization dictionaries from file-based resources.
    /// </summary>
    public interface ILanguageFileLoader
    {
        /// <summary>
        /// Loads a localization dictionary for the specified file.
        /// </summary>
        /// <param name="fileName">The file name to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A dictionary when the resource exists and is valid; otherwise, <c>null</c>.
        /// </returns>
        ValueTask<Dictionary<string, string>?> LoadDictionaryAsync(string fileName, CancellationToken cancellationToken = default);
    }
}
