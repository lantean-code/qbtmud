using System.Text.Json;
using QBittorrent.ApiClient;

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

            var loadedResult = await _apiClient.LoadClientDataAsync(normalizedKeys, cancellationToken);
            if (!loadedResult.TryGetValue(out var loadedData))
            {
                return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            }

            return loadedData
                .Where(entry => entry.Key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, JsonElement>> LoadPrefixedEntriesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loadedResult = await _apiClient.LoadClientDataAsync(keys: null, cancellationToken);
            if (!loadedResult.TryGetValue(out var loadedData))
            {
                return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            }

            return loadedData
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

            var payload = normalizedValues.ToDictionary(
                entry => entry.Key,
                entry => entry.Value is null ? (JsonElement?)null : JsonSerializer.SerializeToElement(entry.Value),
                StringComparer.Ordinal);

            await _apiClient.StoreClientDataAsync(payload, cancellationToken);
        }

        /// <inheritdoc />
        public Task RemovePrefixedEntriesAsync(IEnumerable<string> prefixedKeys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prefixedKeys);

            cancellationToken.ThrowIfCancellationRequested();

            var removalKeys = prefixedKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Where(key => key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (removalKeys.Length == 0)
            {
                return Task.CompletedTask;
            }

            return _apiClient.DeleteClientDataAsync(removalKeys, cancellationToken);
        }
    }
}
