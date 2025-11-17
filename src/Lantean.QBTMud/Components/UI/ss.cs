namespace Lantean.QBTMud.Components.UI
{
    /// <summary>
    /// Specifies which values MudBreakpointOrientationProvider will cascade.
    /// </summary>
    [Flags]
    public enum BreakpointOrientationProviderCascades
    {
        /// <summary>
        /// Cascade only the current breakpoint.
        /// </summary>
        Breakpoint = 1,

        /// <summary>
        /// Cascade only the current orientation.
        /// </summary>
        Orientation = 2,

        /// <summary>
        /// Cascade both breakpoint and orientation.
        /// </summary>
        Both = Breakpoint | Orientation
    }
}
