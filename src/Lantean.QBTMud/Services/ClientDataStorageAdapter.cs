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
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientDataStorageAdapter"/> class.
        /// </summary>
        /// <param name="apiClient">The qBittorrent API client.</param>
        /// <param name="apiFeedbackWorkflow">The API feedback workflow.</param>
        public ClientDataStorageAdapter(IApiClient apiClient, IApiFeedbackWorkflow apiFeedbackWorkflow)
        {
            _apiClient = apiClient;
            _apiFeedbackWorkflow = apiFeedbackWorkflow;
        }

        /// <inheritdoc />
        public async Task<ClientDataLoadResult> LoadPrefixedEntriesAsync(IEnumerable<string> prefixedKeys, CancellationToken cancellationToken = default)
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
                return ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(StringComparer.Ordinal));
            }

            var loadedResult = await _apiClient.LoadClientDataAsync(normalizedKeys, cancellationToken);
            if (loadedResult.IsFailure)
            {
                await _apiFeedbackWorkflow.HandleFailureAsync(loadedResult, cancellationToken: cancellationToken);
                return ClientDataLoadResult.Failure;
            }

            var loadedData = loadedResult.Value;
            var entries = loadedData
                .Where(entry => entry.Key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            return ClientDataLoadResult.FromEntries(entries);
        }

        /// <inheritdoc />
        public async Task<ClientDataLoadResult> LoadPrefixedEntriesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loadedResult = await _apiClient.LoadClientDataAsync(keys: null, cancellationToken);
            if (loadedResult.IsFailure)
            {
                await _apiFeedbackWorkflow.HandleFailureAsync(loadedResult, cancellationToken: cancellationToken);
                return ClientDataLoadResult.Failure;
            }

            var loadedData = loadedResult.Value;
            var entries = loadedData
                .Where(entry => entry.Key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            return ClientDataLoadResult.FromEntries(entries);
        }

        /// <inheritdoc />
        public async Task<ClientDataStorageResult> StorePrefixedEntriesAsync(IReadOnlyDictionary<string, object?> prefixedValues, CancellationToken cancellationToken = default)
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
                return ClientDataStorageResult.Success;
            }

            var payload = normalizedValues.ToDictionary(
                entry => entry.Key,
                entry => entry.Value is null ? (JsonElement?)null : JsonSerializer.SerializeToElement(entry.Value),
                StringComparer.Ordinal);

            var storeResult = await _apiClient.StoreClientDataAsync(payload, cancellationToken);
            if (!await _apiFeedbackWorkflow.ProcessResultAsync(storeResult, cancellationToken: cancellationToken))
            {
                return ClientDataStorageResult.Failure;
            }

            return ClientDataStorageResult.Success;
        }

        /// <inheritdoc />
        public Task<ClientDataStorageResult> RemovePrefixedEntriesAsync(IEnumerable<string> prefixedKeys, CancellationToken cancellationToken = default)
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
                return Task.FromResult(ClientDataStorageResult.Success);
            }

            return RemovePrefixedEntriesCoreAsync(removalKeys, cancellationToken);
        }

        private async Task<ClientDataStorageResult> RemovePrefixedEntriesCoreAsync(IReadOnlyCollection<string> removalKeys, CancellationToken cancellationToken)
        {
            var removeResult = await _apiClient.DeleteClientDataAsync(removalKeys, cancellationToken);
            if (!await _apiFeedbackWorkflow.ProcessResultAsync(removeResult, cancellationToken: cancellationToken))
            {
                return ClientDataStorageResult.Failure;
            }

            return ClientDataStorageResult.Success;
        }
    }
}
