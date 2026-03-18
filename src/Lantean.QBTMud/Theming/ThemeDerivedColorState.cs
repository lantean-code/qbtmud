using MudBlazor.Utilities;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Represents a derived palette color value and whether it is using the automatic MudBlazor calculation.
    /// </summary>
    public sealed class ThemeDerivedColorState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeDerivedColorState"/> class.
        /// </summary>
        /// <param name="color">The resolved color value.</param>
        /// <param name="isAuto">Whether the value is currently using MudBlazor's automatic derivation.</param>
        public ThemeDerivedColorState(MudColor color, bool isAuto)
        {
            Color = color;
            IsAuto = isAuto;
        }

        /// <summary>
        /// Gets the resolved derived color.
        /// </summary>
        public MudColor Color { get; }

        /// <summary>
        /// Gets a value indicating whether the derived color is automatically calculated.
        /// </summary>
        public bool IsAuto { get; }
    }
}
