namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents the source type for a wizard step accent color.
    /// </summary>
    public enum WizardAccentColorKind
    {
        /// <summary>
        /// Uses a MudBlazor palette color.
        /// </summary>
        Palette,

        /// <summary>
        /// Uses a raw CSS color string.
        /// </summary>
        Css
    }
}
