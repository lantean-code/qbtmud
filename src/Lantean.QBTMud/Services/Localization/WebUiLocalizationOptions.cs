namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Configuration for WebUI translation data.
    /// </summary>
    public sealed class WebUiLocalizationOptions
    {
        /// <summary>
        /// Gets or sets the base path for translation files within wwwroot.
        /// </summary>
        public string BasePath { get; set; } = "i18n";

        /// <summary>
        /// Gets or sets the filename format for base translation files.
        /// </summary>
        public string BaseFileNameFormat { get; set; } = "webui_{0}.json";

        /// <summary>
        /// Gets or sets the filename format for override translation files.
        /// </summary>
        public string OverrideFileNameFormat { get; set; } = "webui_overrides_{0}.json";

        /// <summary>
        /// Gets or sets the filename for the alias map.
        /// </summary>
        public string AliasFileName { get; set; } = "webui_aliases.json";
    }
}
