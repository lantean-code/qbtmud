namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Defines the preferred application theme mode.
    /// </summary>
    public enum ThemeModePreference
    {
        /// <summary>
        /// Follows the operating system and browser preference.
        /// </summary>
        System = 0,

        /// <summary>
        /// Forces light mode.
        /// </summary>
        Light = 1,

        /// <summary>
        /// Forces dark mode.
        /// </summary>
        Dark = 2
    }
}
