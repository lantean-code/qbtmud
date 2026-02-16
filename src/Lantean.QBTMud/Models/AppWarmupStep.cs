namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Identifies a warmup initialization step.
    /// </summary>
    public enum AppWarmupStep
    {
        /// <summary>
        /// Initializes WebUI localization resources (aliases, base translations, and overrides).
        /// </summary>
        LanguageLocalizer = 0,

        /// <summary>
        /// Loads the WebUI language catalog used for locale selection.
        /// </summary>
        LanguageCatalog = 1,

        /// <summary>
        /// Loads theme definitions and applies the selected theme.
        /// </summary>
        ThemeManager = 2,

        /// <summary>
        /// Detects browser capabilities that are stable for the current session.
        /// </summary>
        BrowserCapabilities = 3
    }
}
