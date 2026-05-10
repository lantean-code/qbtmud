using MudBlazor;

namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents a theme that can be browsed in the preview dialog.
    /// </summary>
    public sealed class ThemePreviewDialogItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemePreviewDialogItem"/> class.
        /// </summary>
        /// <param name="themeId">The theme identifier.</param>
        /// <param name="name">The display name.</param>
        /// <param name="sourceLabel">The display source label.</param>
        /// <param name="theme">The theme to preview.</param>
        public ThemePreviewDialogItem(string themeId, string name, string sourceLabel, MudTheme theme)
        {
            ThemeId = themeId;
            Name = name;
            SourceLabel = sourceLabel;
            Theme = theme;
        }

        /// <summary>
        /// Gets the theme identifier.
        /// </summary>
        public string ThemeId { get; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the display source label.
        /// </summary>
        public string SourceLabel { get; }

        /// <summary>
        /// Gets the theme to preview.
        /// </summary>
        public MudTheme Theme { get; }
    }
}
