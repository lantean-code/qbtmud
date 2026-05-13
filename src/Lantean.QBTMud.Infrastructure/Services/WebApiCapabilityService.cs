using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Infrastructure.Services
{
    /// <summary>
    /// Resolves and caches Web API capability information for the current authenticated session.
    /// </summary>
    public sealed class WebApiCapabilityService : IWebApiCapabilityService
    {
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

                var versionResult = await _apiClient.GetAPIVersionAsync(cancellationToken);
                if (versionResult.IsFailure)
                {
                    return new WebApiCapabilityState(
                        webApiVersion: null,
                        supportsClientData: false,
                        supportsTrackerErrorFilters: false);
                }

                if (!WebApiCompatibilityProfile.TryCreate(versionResult.Value, out var compatibilityProfile))
                {
                    _cachedState = new WebApiCapabilityState(
                        webApiVersion: null,
                        supportsClientData: false,
                        supportsTrackerErrorFilters: false);
                    return _cachedState;
                }

                _cachedState = new WebApiCapabilityState(
                    compatibilityProfile.WebApiVersion,
                    supportsClientData: compatibilityProfile.SupportsClientData,
                    supportsTrackerErrorFilters: compatibilityProfile.SupportsTrackerErrorFilters);
                return _cachedState;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }
    }
}
