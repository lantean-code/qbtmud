namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents current qbtmud build metadata.
    /// </summary>
    public sealed record AppBuildInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuildInfo"/> class.
        /// </summary>
        /// <param name="version">The current build version label.</param>
        /// <param name="source">The source used to resolve the version label.</param>
        public AppBuildInfo(string version, string source)
        {
            Version = version;
            Source = source;
        }

        /// <summary>
        /// Gets the current build version label.
        /// </summary>
        public string Version { get; init; }

        /// <summary>
        /// Gets the source used to resolve the version label.
        /// </summary>
        public string Source { get; init; }
    }
}
