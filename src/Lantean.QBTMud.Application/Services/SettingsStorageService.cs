using System.Text.Json;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Default implementation of <see cref="ISettingsStorageService"/>.
    /// </summary>
    public sealed class SettingsStorageService : ISettingsStorageService
    {
        private static readonly JsonSerializerOptions _serializerOptions = ThemeSerialization.CreateSerializerOptions(writeIndented: false);

        private readonly ClientDataCacheState _clientDataCacheState;
        private readonly ILocalStorageService _localStorageService;
        private readonly IStorageRoutingService _storageRoutingService;
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsStorageService"/> class.
        /// </summary>
        /// <param name="clientDataCacheState">The scoped ClientData cache state.</param>
        /// <param name="localStorageService">The local storage service.</param>
        /// <param name="storageRoutingService">The storage routing service.</param>
        /// <param name="webApiCapabilityService">The Web API capability service.</param>
        /// <param name="clientDataStorageAdapter">The client data storage adapter.</param>
        /// <param name="apiFeedbackWorkflow">The API feedback workflow.</param>
        public SettingsStorageService(
            ClientDataCacheState clientDataCacheState,
            ILocalStorageService localStorageService,
            IStorageRoutingService storageRoutingService,
            IWebApiCapabilityService webApiCapabilityService,
            IClientDataStorageAdapter clientDataStorageAdapter,
            IApiFeedbackWorkflow apiFeedbackWorkflow)
        {
            _clientDataCacheState = clientDataCacheState;
            _localStorageService = localStorageService;
            _storageRoutingService = storageRoutingService;
            _webApiCapabilityService = webApiCapabilityService;
            _clientDataStorageAdapter = clientDataStorageAdapter;
            _apiFeedbackWorkflow = apiFeedbackWorkflow;
        }

        /// <inheritdoc />
        public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var storageType = await ResolveStorageTypeAsync(key, cancellationToken);
            if (storageType == StorageType.LocalStorage)
            {
                return await _localStorageService.GetItemAsync<T>(key, cancellationToken);
            }

            try
            {
                var prefixedKey = ToPrefixedKey(key);
                var loadedEntries = await GetCachedClientDataEntriesAsync(cancellationToken);
                if (loadedEntries is null)
                {
                    return await _localStorageService.GetItemAsync<T>(key, cancellationToken);
                }

                if (!loadedEntries.TryGetValue(prefixedKey, out var value)
                    || value.ValueKind == JsonValueKind.Undefined
                    || value.ValueKind == JsonValueKind.Null)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(value.GetRawText(), _serializerOptions);
            }
            catch (JsonException)
            {
                return await _localStorageService.GetItemAsync<T>(key, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var storageType = await ResolveStorageTypeAsync(key, cancellationToken);
            if (storageType == StorageType.LocalStorage)
            {
                return await _localStorageService.GetItemAsStringAsync(key, cancellationToken);
            }

            var prefixedKey = ToPrefixedKey(key);
            var loadedEntries = await GetCachedClientDataEntriesAsync(cancellationToken);
            if (loadedEntries is null)
            {
                return await _localStorageService.GetItemAsStringAsync(key, cancellationToken);
            }

            if (!loadedEntries.TryGetValue(prefixedKey, out var value)
                || value.ValueKind == JsonValueKind.Undefined
                || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return value.GetRawText();
        }

        /// <inheritdoc />
        public async ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var storageType = await ResolveStorageTypeAsync(key, cancellationToken);
            if (storageType == StorageType.LocalStorage)
            {
                await _localStorageService.SetItemAsync(key, data, cancellationToken);
                return;
            }

            var prefixedKey = ToPrefixedKey(key);
            var valueElement = JsonSerializer.SerializeToElement(data, _serializerOptions);
            var storeResult = await _clientDataStorageAdapter.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [prefixedKey] = valueElement
                },
                cancellationToken);

            if (!storeResult.Succeeded)
            {
                await HandleClientDataFailureAsync(storeResult.FailureResult, cancellationToken);
                await InvalidateCachedClientDataEntriesAsync(cancellationToken);
                await _localStorageService.SetItemAsync(key, data, cancellationToken);
                return;
            }

            await StoreCachedClientDataEntryAsync(prefixedKey, valueElement, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(data);

            var storageType = await ResolveStorageTypeAsync(key, cancellationToken);
            if (storageType == StorageType.LocalStorage)
            {
                await _localStorageService.SetItemAsStringAsync(key, data, cancellationToken);
                return;
            }

            var prefixedKey = ToPrefixedKey(key);
            var storeResult = await _clientDataStorageAdapter.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [prefixedKey] = data
                },
                cancellationToken);

            if (!storeResult.Succeeded)
            {
                await HandleClientDataFailureAsync(storeResult.FailureResult, cancellationToken);
                await InvalidateCachedClientDataEntriesAsync(cancellationToken);
                await _localStorageService.SetItemAsStringAsync(key, data, cancellationToken);
                return;
            }

            await StoreCachedClientDataEntryAsync(prefixedKey, JsonSerializer.SerializeToElement(data, _serializerOptions), cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var storageType = await ResolveStorageTypeAsync(key, cancellationToken);
            if (storageType == StorageType.LocalStorage)
            {
                await _localStorageService.RemoveItemAsync(key, cancellationToken);
                return;
            }

            var removeResult = await _clientDataStorageAdapter.RemovePrefixedEntriesAsync([ToPrefixedKey(key)], cancellationToken);
            if (!removeResult.Succeeded)
            {
                await HandleClientDataFailureAsync(removeResult.FailureResult, cancellationToken);
                await InvalidateCachedClientDataEntriesAsync(cancellationToken);
                await _localStorageService.RemoveItemAsync(key, cancellationToken);
                return;
            }

            await RemoveCachedClientDataEntryAsync(ToPrefixedKey(key), cancellationToken);
        }

        private async Task HandleClientDataFailureAsync(ApiResultBase? failureResult, CancellationToken cancellationToken)
        {
            if (failureResult is not null)
            {
                await _apiFeedbackWorkflow.HandleFailureAsync(failureResult, cancellationToken: cancellationToken);
            }
        }

        private async Task<StorageType> ResolveStorageTypeAsync(string key, CancellationToken cancellationToken)
        {
            var settings = await _storageRoutingService.GetSettingsAsync(cancellationToken);
            var configuredStorageType = _storageRoutingService.ResolveEffectiveStorageType(key, settings, supportsClientData: true);
            if (configuredStorageType == StorageType.LocalStorage)
            {
                return StorageType.LocalStorage;
            }

            var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
            return capabilityState.SupportsClientData
                ? StorageType.ClientData
                : StorageType.LocalStorage;
        }

        private async Task<IReadOnlyDictionary<string, JsonElement>?> GetCachedClientDataEntriesAsync(CancellationToken cancellationToken)
        {
            await _clientDataCacheState.CacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_clientDataCacheState.CachedEntries is not null)
                {
                    return _clientDataCacheState.CachedEntries;
                }

                var loadedResult = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
                if (!loadedResult.Succeeded || loadedResult.Entries is null)
                {
                    await HandleClientDataFailureAsync(loadedResult.FailureResult, cancellationToken);
                    return null;
                }

                _clientDataCacheState.CachedEntries = new Dictionary<string, JsonElement>(loadedResult.Entries, StringComparer.Ordinal);
                return _clientDataCacheState.CachedEntries;
            }
            finally
            {
                _clientDataCacheState.CacheSemaphore.Release();
            }
        }

        private async Task StoreCachedClientDataEntryAsync(string prefixedKey, JsonElement value, CancellationToken cancellationToken)
        {
            await _clientDataCacheState.CacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_clientDataCacheState.CachedEntries is null)
                {
                    return;
                }

                var updatedEntries = new Dictionary<string, JsonElement>(_clientDataCacheState.CachedEntries, StringComparer.Ordinal)
                {
                    [prefixedKey] = value
                };
                _clientDataCacheState.CachedEntries = updatedEntries;
            }
            finally
            {
                _clientDataCacheState.CacheSemaphore.Release();
            }
        }

        private async Task InvalidateCachedClientDataEntriesAsync(CancellationToken cancellationToken)
        {
            await _clientDataCacheState.CacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                _clientDataCacheState.CachedEntries = null;
            }
            finally
            {
                _clientDataCacheState.CacheSemaphore.Release();
            }
        }

        private async Task RemoveCachedClientDataEntryAsync(string prefixedKey, CancellationToken cancellationToken)
        {
            await _clientDataCacheState.CacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_clientDataCacheState.CachedEntries is null)
                {
                    return;
                }

                var updatedEntries = new Dictionary<string, JsonElement>(_clientDataCacheState.CachedEntries, StringComparer.Ordinal);
                updatedEntries.Remove(prefixedKey);
                _clientDataCacheState.CachedEntries = updatedEntries;
            }
            finally
            {
                _clientDataCacheState.CacheSemaphore.Release();
            }
        }

        private static string ToPrefixedKey(string key)
        {
            if (key.StartsWith(StorageKeys.Prefix, StringComparison.Ordinal))
            {
                return key;
            }

            return string.Concat(StorageKeys.Prefix, key);
        }
    }
}
