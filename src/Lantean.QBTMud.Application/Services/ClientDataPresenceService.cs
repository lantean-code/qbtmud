using System.Text.Json;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Detects whether qbtmud-owned ClientData already exists for the current qBittorrent instance.
    /// </summary>
    public sealed class ClientDataPresenceService : IClientDataPresenceService
    {
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientDataPresenceService"/> class.
        /// </summary>
        /// <param name="webApiCapabilityService">The Web API capability service.</param>
        /// <param name="clientDataStorageAdapter">The ClientData storage adapter.</param>
        public ClientDataPresenceService(IWebApiCapabilityService webApiCapabilityService, IClientDataStorageAdapter clientDataStorageAdapter)
        {
            _webApiCapabilityService = webApiCapabilityService;
            _clientDataStorageAdapter = clientDataStorageAdapter;
        }

        /// <inheritdoc />
        public async Task<bool> HasStoredClientDataAsync(CancellationToken cancellationToken = default)
        {
            var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
            if (!capabilityState.SupportsClientData)
            {
                return false;
            }

            try
            {
                var loadResult = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
                return loadResult.Succeeded
                    && loadResult.Entries is not null
                    && loadResult.Entries.Count > 0;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
