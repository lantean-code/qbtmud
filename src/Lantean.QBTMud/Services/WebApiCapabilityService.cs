using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Resolves and caches Web API capability information for the current authenticated session.
    /// </summary>
    public sealed class WebApiCapabilityService : IWebApiCapabilityService
    {
        private static readonly Version ClientDataMinimumVersion = new(2, 13, 1);

        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private readonly IApiClient _apiClient;
        private WebApiCapabilityState? _cachedState;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiCapabilityService"/> class.
        /// </summary>
        /// <param name="apiClient">The qBittorrent API client.</param>
        public WebApiCapabilityService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <inheritdoc />
        public async Task<WebApiCapabilityState> GetCapabilityStateAsync(CancellationToken cancellationToken = default)
        {
            if (_cachedState is not null)
            {
                return _cachedState;
            }

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_cachedState is not null)
                {
                    return _cachedState;
                }

                string? rawVersion = null;
                try
                {
                    rawVersion = await _apiClient.GetAPIVersion();
                }
                catch
                {
                    return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
                }

                rawVersion = string.IsNullOrWhiteSpace(rawVersion)
                    ? null
                    : rawVersion.Trim();

                if (rawVersion is null || !Version.TryParse(rawVersion, out var parsedVersion))
                {
                    _cachedState = new WebApiCapabilityState(rawVersion, parsedWebApiVersion: null, supportsClientData: false);
                    return _cachedState;
                }

                _cachedState = new WebApiCapabilityState(
                    rawVersion,
                    parsedVersion,
                    supportsClientData: parsedVersion >= ClientDataMinimumVersion);
                return _cachedState;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }
    }
}
