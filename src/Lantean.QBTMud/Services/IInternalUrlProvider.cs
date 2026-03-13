namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates absolute URLs for qbtmud routes based on the configured routing mode.
    /// </summary>
    public interface IInternalUrlProvider
    {
        /// <summary>
        /// Builds an absolute URL for an internal qbtmud route.
        /// </summary>
        /// <param name="path">The optional route path relative to the app root.</param>
        /// <param name="query">The optional raw query string without a leading question mark.</param>
        /// <returns>The absolute internal URL.</returns>
        string GetAbsoluteUrl(string? path = null, string? query = null);
    }
}
