namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents an updated theme mode preference.
    /// </summary>
    public sealed class ThemeModePreferenceChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeModePreferenceChangedEventArgs"/> class.
        /// </summary>
        /// <param name="themeModePreference">The updated theme mode preference.</param>
        public ThemeModePreferenceChangedEventArgs(ThemeModePreference themeModePreference)
        {
            ThemeModePreference = themeModePreference;
        }

        /// <summary>
        /// Gets the updated theme mode preference.
        /// </summary>
        public ThemeModePreference ThemeModePreference { get; }
    }
}
