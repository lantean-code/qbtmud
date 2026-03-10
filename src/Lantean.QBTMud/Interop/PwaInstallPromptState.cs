namespace Lantean.QBTMud.Interop
{
    /// <summary>
    /// Represents the browser install prompt capabilities for the current page.
    /// </summary>
    public sealed class PwaInstallPromptState
    {
        /// <summary>
        /// Gets or sets a value indicating whether the app is already installed (standalone mode).
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the browser supports and currently allows prompting for install.
        /// </summary>
        public bool CanPrompt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current device is iOS or iPadOS.
        /// </summary>
        public bool IsIos { get; set; }
    }
}
