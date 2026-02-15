namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Represents localization dictionaries resolved for a single culture initialization.
    /// </summary>
    /// <param name="aliases">The loaded alias dictionary.</param>
    /// <param name="overrides">The loaded override dictionary.</param>
    /// <param name="translations">The loaded base translation dictionary.</param>
    /// <param name="loadedCultureName">The culture name associated with the loaded resources.</param>
    public sealed record LanguageResources(
        IReadOnlyDictionary<string, string> Aliases,
        IReadOnlyDictionary<string, string> Overrides,
        IReadOnlyDictionary<string, string> Translations,
        string LoadedCultureName);
}
