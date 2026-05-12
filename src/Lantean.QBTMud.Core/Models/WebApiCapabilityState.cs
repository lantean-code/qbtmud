namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents detected Web API capability information for the current session.
    /// </summary>
    public sealed class WebApiCapabilityState
    {
        private static readonly Version _trackerErrorFiltersMinimumVersion = new(2, 15, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiCapabilityState"/> class.
        /// </summary>
        /// <param name="rawWebApiVersion">The raw Web API version string.</param>
        /// <param name="parsedWebApiVersion">The parsed Web API version, when valid.</param>
        /// <param name="supportsClientData">A value indicating whether ClientData endpoints are supported.</param>
        public WebApiCapabilityState(string? rawWebApiVersion, Version? parsedWebApiVersion, bool supportsClientData)
        {
            RawWebApiVersion = string.IsNullOrWhiteSpace(rawWebApiVersion)
                ? null
                : rawWebApiVersion.Trim();
            ParsedWebApiVersion = parsedWebApiVersion;
            SupportsClientData = supportsClientData;
        }

        /// <summary>
        /// Gets the raw Web API version string.
        /// </summary>
        public string? RawWebApiVersion { get; }

        /// <summary>
        /// Gets the parsed Web API version, when available.
        /// </summary>
        public Version? ParsedWebApiVersion { get; }

        /// <summary>
        /// Gets a value indicating whether ClientData endpoints are supported.
        /// </summary>
        public bool SupportsClientData { get; }

        /// <summary>
        /// Gets a value indicating whether tracker error filter buckets are supported.
        /// </summary>
        public bool SupportsTrackerErrorFilters
        {
            get
            {
                return ParsedWebApiVersion is not null
                    && ParsedWebApiVersion >= _trackerErrorFiltersMinimumVersion;
            }
        }
    }
}
