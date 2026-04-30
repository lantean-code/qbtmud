using Lantean.QBTMud.Models;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Resolves and caches Web API capability information for the current authenticated session.
    /// </summary>
    public sealed class WebApiCapabilityService : IWebApiCapabilityService
    {
        private static readonly Version _clientDataMinimumVersion = new(2, 13, 1);

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
            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_cachedState is not null)
                {
                    return _cachedState;
                }

                var versionResult = await _apiClient.GetAPIVersionAsync();
                if (versionResult.IsFailure)
                {
                    return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
                }

                var rawVersion = versionResult.Value;
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
                    supportsClientData: parsedVersion >= _clientDataMinimumVersion);
                return _cachedState;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }
    }
}
