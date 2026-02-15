namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Stores localization resources currently active for translation lookups.
    /// </summary>
    public sealed class LanguageResourceProvider : ILanguageResourceProvider
    {
        private LanguageResources _resources = new LanguageResources(
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            string.Empty);

        /// <inheritdoc />
        public LanguageResources Resources
        {
            get { return _resources; }
        }

        /// <inheritdoc />
        public void SetResources(LanguageResources resources)
        {
            ArgumentNullException.ThrowIfNull(resources);
            _resources = resources;
        }
    }
}
