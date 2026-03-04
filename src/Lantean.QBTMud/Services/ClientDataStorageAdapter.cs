using Lantean.QBitTorrentClient;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IClientDataStorageAdapter"/>.
    /// </summary>
    public sealed class ClientDataStorageAdapter : IClientDataStorageAdapter
    {
        /// <summary>
        /// Gets the qbtmud key prefix used in browser and ClientData storage.
        /// </summary>
        public const string StorageKeyPrefix = "QbtMud.";

        private readonly IApiClient _apiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientDataStorageAdapter"/> class.
        /// </summary>
        /// <param name="apiClient">The qBittorrent API client.</param>
        public ClientDataStorageAdapter(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, JsonElement>> LoadPrefixedEntriesAsync(IEnumerable<string> prefixedKeys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prefixedKeys);

            cancellationToken.ThrowIfCancellationRequested();

            var normalizedKeys = prefixedKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Where(key => key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (normalizedKeys.Length == 0)
            {
                return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            }

            var loaded = await _apiClient.LoadClientData(normalizedKeys);
            return loaded
                .Where(entry => entry.Key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, JsonElement>> LoadPrefixedEntriesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loaded = await _apiClient.LoadClientData();
            return loaded
                .Where(entry => entry.Key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public async Task StorePrefixedEntriesAsync(IReadOnlyDictionary<string, object?> prefixedValues, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prefixedValues);
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedValues = prefixedValues
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
                .Select(entry => new KeyValuePair<string, object?>(entry.Key.Trim(), entry.Value))
                .Where(entry => entry.Key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

            if (normalizedValues.Count == 0)
            {
                return;
            }

            await _apiClient.StoreClientData(normalizedValues);
        }

        /// <inheritdoc />
        public Task RemovePrefixedEntriesAsync(IEnumerable<string> prefixedKeys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prefixedKeys);

            cancellationToken.ThrowIfCancellationRequested();

            var removalValues = prefixedKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Where(key => key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToDictionary(key => key, _ => (object?)null, StringComparer.Ordinal);

            if (removalValues.Count == 0)
            {
                return Task.CompletedTask;
            }

            return _apiClient.StoreClientData(removalValues);
        }
    }
}
