using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides data for runtime app-settings changes.
    /// </summary>
    public sealed class AppSettingsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousSettings">The previous runtime app-settings snapshot.</param>
        /// <param name="currentSettings">The current runtime app-settings snapshot.</param>
        public AppSettingsChangedEventArgs(AppSettings? previousSettings, AppSettings? currentSettings)
        {
            PreviousSettings = previousSettings;
            CurrentSettings = currentSettings;
        }

        /// <summary>
        /// Gets the previous runtime app-settings snapshot.
        /// </summary>
        public AppSettings? PreviousSettings { get; }

        /// <summary>
        /// Gets the current runtime app-settings snapshot.
        /// </summary>
        public AppSettings? CurrentSettings { get; }
    }
}
