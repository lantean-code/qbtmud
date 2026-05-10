using MudBlazor;

namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents the input for the theme preview dialog.
    /// </summary>
    public sealed class ThemePreviewDialogRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemePreviewDialogRequest"/> class.
        /// </summary>
        /// <param name="items">The themes available for preview.</param>
        /// <param name="selectedThemeId">The initially selected theme identifier.</param>
        /// <param name="mode">The preview mode.</param>
        /// <param name="isDarkMode">Whether the preview starts in dark mode.</param>
        public ThemePreviewDialogRequest(
            IReadOnlyList<ThemePreviewDialogItem> items,
            string selectedThemeId,
            ThemePreviewDialogMode mode,
            bool isDarkMode)
        {
            Items = items;
            SelectedThemeId = selectedThemeId;
            Mode = mode;
            IsDarkMode = isDarkMode;
        }

        /// <summary>
        /// Gets the themes available for preview.
        /// </summary>
        public IReadOnlyList<ThemePreviewDialogItem> Items { get; }

        /// <summary>
        /// Gets the initially selected theme identifier.
        /// </summary>
        public string SelectedThemeId { get; }

        /// <summary>
        /// Gets the preview mode.
        /// </summary>
        public ThemePreviewDialogMode Mode { get; }

        /// <summary>
        /// Gets a value indicating whether the preview starts in dark mode.
        /// </summary>
        public bool IsDarkMode { get; }

        /// <summary>
        /// Gets or sets the currently applied persisted theme identifier.
        /// </summary>
        public string? CurrentThemeId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the details-mode save-and-apply action is enabled.
        /// </summary>
        public bool CanSaveAndApply { get; set; }

        /// <summary>
        /// Gets or sets the callback used to apply a persisted previewed theme.
        /// </summary>
        public Func<string, Task<bool>>? ApplyThemeAsync { get; set; }

        /// <summary>
        /// Gets or sets the callback used to save and apply the details preview theme.
        /// </summary>
        public Func<Task<bool>>? SaveAndApplyThemeAsync { get; set; }
    }
}
