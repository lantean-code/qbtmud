namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents updated qbtmud app settings.
    /// </summary>
    public sealed class AppSettingsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsChangedEventArgs"/> class.
        /// </summary>
        /// <param name="settings">The updated settings.</param>
        public AppSettingsChangedEventArgs(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            Settings = settings.Clone();
        }

        /// <summary>
        /// Gets the updated settings snapshot.
        /// </summary>
        public AppSettings Settings { get; }
    }
}
