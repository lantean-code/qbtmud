namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Identifies a warmup initialization step.
    /// </summary>
    public enum AppWarmupStep
    {
        /// <summary>
        /// Loads translation aliases and the base/override WebUI translation dictionaries.
        /// </summary>
        WebUiLocalizer = 0,

        /// <summary>
        /// Loads the WebUI language catalog used for locale selection.
        /// </summary>
        WebUiLanguageCatalog = 1,

        /// <summary>
        /// Loads theme definitions and applies the selected theme.
        /// </summary>
        ThemeManager = 2
    }
}
