namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Stores the currently active localization resources for the WebUI.
    /// </summary>
    public interface ILanguageResourceProvider
    {
        /// <summary>
        /// Gets the currently active localization resources.
        /// </summary>
        LanguageResources Resources { get; }

        /// <summary>
        /// Replaces the currently active localization resources.
        /// </summary>
        /// <param name="resources">The resources to store.</param>
        void SetResources(LanguageResources resources);
    }
}
