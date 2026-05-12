namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents detected Web API capability information for the current session.
    /// </summary>
    public sealed record WebApiCapabilityState
    {
        /// <summary>
        /// Gets the Web API version, when available.
        /// </summary>
        public Version? WebApiVersion { get; }

        /// <summary>
        /// Gets a value indicating whether ClientData endpoints are supported.
        /// </summary>
        public bool SupportsClientData { get; }

        /// <summary>
        /// Gets a value indicating whether tracker error filter buckets are supported.
        /// </summary>
        public bool SupportsTrackerErrorFilters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiCapabilityState"/> class.
        /// </summary>
        /// <param name="webApiVersion">The Web API version, when available.</param>
        /// <param name="supportsClientData">A value indicating whether ClientData endpoints are supported.</param>
        /// <param name="supportsTrackerErrorFilters">A value indicating whether tracker error filter buckets are supported.</param>
        public WebApiCapabilityState(Version? webApiVersion, bool supportsClientData, bool supportsTrackerErrorFilters = false)
        {
            WebApiVersion = webApiVersion;
            SupportsClientData = supportsClientData;
            SupportsTrackerErrorFilters = supportsTrackerErrorFilters;
        }
    }
}
