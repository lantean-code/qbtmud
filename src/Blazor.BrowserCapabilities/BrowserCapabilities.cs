namespace Blazor.BrowserCapabilities
{
    /// <summary>
    /// Represents stable browser and platform capabilities detected once during application startup.
    /// </summary>
    /// <remarks>
    /// This model is intended for capability checks that are unlikely to change during a user session.
    /// Dynamic runtime conditions such as viewport size, current orientation, and responsive breakpoints
    /// should be handled by dedicated runtime services.
    /// </remarks>
    /// <param name="SupportsHoverPointer">
    /// <see langword="true"/> when the primary input pointer supports both hover and fine precision.
    /// This is typically a desktop mouse or trackpad pointer profile.
    /// </param>
    /// <param name="SupportsHover">
    /// <see langword="true"/> when any hover capability is reported by the browser pointer media queries.
    /// </param>
    /// <param name="SupportsFinePointer">
    /// <see langword="true"/> when a fine pointer is available (for example, mouse or stylus-class precision).
    /// </param>
    /// <param name="SupportsCoarsePointer">
    /// <see langword="true"/> when a coarse pointer is available (for example, touch-first interaction).
    /// </param>
    /// <param name="SupportsPointerEvents">
    /// <see langword="true"/> when the platform exposes the Pointer Events API.
    /// </param>
    /// <param name="HasTouchInput">
    /// <see langword="true"/> when touch input is detected using navigator touch-point support and legacy signals.
    /// </param>
    /// <param name="MaxTouchPoints">
    /// The maximum number of simultaneous touch contact points reported by the browser.
    /// </param>
    /// <param name="PrefersReducedMotion">
    /// <see langword="true"/> when the user has requested reduced motion effects at OS/browser level.
    /// </param>
    /// <param name="PrefersReducedData">
    /// <see langword="true"/> when the user has requested reduced data usage where supported.
    /// </param>
    /// <param name="PrefersDarkColorScheme">
    /// <see langword="true"/> when the user prefers dark color schemes.
    /// </param>
    /// <param name="ForcedColorsActive">
    /// <see langword="true"/> when forced-colors/high-contrast color substitution is active.
    /// </param>
    /// <param name="PrefersHighContrast">
    /// <see langword="true"/> when the browser reports a preference for increased contrast.
    /// </param>
    /// <param name="SupportsClipboardRead">
    /// <see langword="true"/> when asynchronous clipboard read APIs are available.
    /// </param>
    /// <param name="SupportsClipboardWrite">
    /// <see langword="true"/> when asynchronous clipboard write APIs are available.
    /// </param>
    /// <param name="SupportsShareApi">
    /// <see langword="true"/> when the Web Share API is available.
    /// </param>
    /// <param name="SupportsInstallPrompt">
    /// <see langword="true"/> when the environment exposes the install prompt event surface.
    /// </param>
    /// <param name="IsAppleMobilePlatform">
    /// <see langword="true"/> when the platform appears to be iOS/iPadOS-like (including iPadOS desktop mode heuristic).
    /// </param>
    /// <param name="IsStandaloneDisplayMode">
    /// <see langword="true"/> when running in standalone display mode (for example, an installed PWA shell).
    /// </param>
    public sealed record BrowserCapabilities(
        bool SupportsHoverPointer,
        bool SupportsHover,
        bool SupportsFinePointer,
        bool SupportsCoarsePointer,
        bool SupportsPointerEvents,
        bool HasTouchInput,
        int MaxTouchPoints,
        bool PrefersReducedMotion,
        bool PrefersReducedData,
        bool PrefersDarkColorScheme,
        bool ForcedColorsActive,
        bool PrefersHighContrast,
        bool SupportsClipboardRead,
        bool SupportsClipboardWrite,
        bool SupportsShareApi,
        bool SupportsInstallPrompt,
        bool IsAppleMobilePlatform,
        bool IsStandaloneDisplayMode)
    {
        /// <summary>
        /// Gets a conservative, broadly safe capability set for environments where detection is unavailable.
        /// </summary>
        /// <remarks>
        /// All feature flags are disabled and values are zeroed to avoid optimistic assumptions.
        /// </remarks>
        public static BrowserCapabilities Default
        {
            get
            {
                return new BrowserCapabilities(
                    SupportsHoverPointer: false,
                    SupportsHover: false,
                    SupportsFinePointer: false,
                    SupportsCoarsePointer: false,
                    SupportsPointerEvents: false,
                    HasTouchInput: false,
                    MaxTouchPoints: 0,
                    PrefersReducedMotion: false,
                    PrefersReducedData: false,
                    PrefersDarkColorScheme: false,
                    ForcedColorsActive: false,
                    PrefersHighContrast: false,
                    SupportsClipboardRead: false,
                    SupportsClipboardWrite: false,
                    SupportsShareApi: false,
                    SupportsInstallPrompt: false,
                    IsAppleMobilePlatform: false,
                    IsStandaloneDisplayMode: false);
            }
        }
    }
}
