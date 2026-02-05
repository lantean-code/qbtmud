using MudBlazor;

namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Provides data for theme change notifications.
    /// </summary>
    public sealed class ThemeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="theme">The applied MudBlazor theme.</param>
        /// <param name="fontFamily">The applied font family.</param>
        /// <param name="themeId">The applied theme identifier.</param>
        public ThemeChangedEventArgs(MudTheme theme, string fontFamily, string? themeId)
        {
            Theme = theme;
            FontFamily = fontFamily;
            ThemeId = themeId;
        }

        /// <summary>
        /// Gets the applied MudBlazor theme.
        /// </summary>
        public MudTheme Theme { get; }

        /// <summary>
        /// Gets the applied font family.
        /// </summary>
        public string FontFamily { get; }

        /// <summary>
        /// Gets the applied theme identifier.
        /// </summary>
        public string? ThemeId { get; }
    }
}
