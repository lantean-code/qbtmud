using System.Text.Json;
using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Default implementation of <see cref="IStorageRoutingService"/>.
    /// </summary>
    public sealed class StorageRoutingService : IStorageRoutingService
    {
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private readonly ILocalStorageService _localStorageService;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly IStorageCatalogService _storageCatalogService;
        private readonly ILocalStorageEntryAdapter _localStorageEntryAdapter;
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;
        private StorageRoutingSettings? _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageRoutingService"/> class.
        /// </summary>
        /// <param name="localStorageService">The local storage service.</param>
        /// <param name="clientDataStorageAdapter">The client data storage adapter.</param>
        /// <param name="webApiCapabilityService">The Web API capability service.</param>
        /// <param name="storageCatalogService">The routed storage catalog.</param>
        /// <param name="localStorageEntryAdapter">The local storage entry adapter.</param>
        /// <param name="apiFeedbackWorkflow">The API feedback workflow.</param>
        public StorageRoutingService(
            ILocalStorageService localStorageService,
            IClientDataStorageAdapter clientDataStorageAdapter,
            IWebApiCapabilityService webApiCapabilityService,
            IStorageCatalogService storageCatalogService,
            ILocalStorageEntryAdapter localStorageEntryAdapter,
            IApiFeedbackWorkflow apiFeedbackWorkflow)
        {
            _localStorageService = localStorageService;
            _clientDataStorageAdapter = clientDataStorageAdapter;
            _webApiCapabilityService = webApiCapabilityService;
            _storageCatalogService = storageCatalogService;
            _localStorageEntryAdapter = localStorageEntryAdapter;
            _apiFeedbackWorkflow = apiFeedbackWorkflow;
        }

        /// <inheritdoc />
        public async Task<StorageRoutingSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_cachedSettings is not null)
                {
                    return _cachedSettings.Clone();
                }

                StorageRoutingSettings? loadedSettings;
                try
                {
                    loadedSettings = await _localStorageService.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, cancellationToken);
                }
                catch (JsonException)
                {
                    loadedSettings = null;
                }

                _cachedSettings = NormalizeForCatalog(loadedSettings ?? StorageRoutingSettings.Default);
                return _cachedSettings.Clone();
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<StorageRoutingSettings> SaveSettingsAsync(StorageRoutingSettings settings, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var normalizedSettings = NormalizeForCatalog(settings);
            var currentSettings = await GetSettingsAsync(cancellationToken);
            if (AreEquivalent(currentSettings, normalizedSettings))
            {
                return currentSettings;
            }

            var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
            var supportsClientData = capabilityState.SupportsClientData;
            if (!supportsClientData && ContainsClientDataSelection(normalizedSettings))
            {
                throw new InvalidOperationException("ClientData storage is not supported by the current Web API version.");
            }

            foreach (var item in _storageCatalogService.Items)
            {
                var currentStorageType = ResolveConfiguredStorageType(item, currentSettings);
                var targetStorageType = ResolveConfiguredStorageType(item, normalizedSettings);
                if (currentStorageType == targetStorageType)
                {
                    continue;
                }

                if (!supportsClientData && currentStorageType == StorageType.ClientData)
                {
                    continue;
                }

                if (!await MigrateCatalogItemAsync(item, currentStorageType, targetStorageType, cancellationToken))
                {
                    throw new InvalidOperationException($"Unable to migrate storage item '{item.Id}'.");
                }
            }

            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, normalizedSettings, cancellationToken);
            _cachedSettings = normalizedSettings.Clone();
            return _cachedSettings.Clone();
        }

        /// <inheritdoc />
        public StorageType ResolveEffectiveStorageType(string key, StorageRoutingSettings settings, bool supportsClientData)
        {
            var configuredStorageType = ResolveConfiguredStorageType(key, settings);
            if (configuredStorageType == StorageType.ClientData && !supportsClientData)
            {
                return StorageType.LocalStorage;
            }

            return configuredStorageType;
        }

        private StorageType ResolveConfiguredStorageType(string key, StorageRoutingSettings settings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(settings);

            var matchedItem = _storageCatalogService.MatchItemByKey(key.Trim());
            if (matchedItem is null)
            {
                if (_storageCatalogService.IsLocalStorageOnlyKey(key))
                {
                    return StorageType.LocalStorage;
                }

                return settings.MasterStorageType;
            }

            return ResolveConfiguredStorageType(matchedItem, settings);
        }

        private static StorageType ResolveConfiguredStorageType(StorageCatalogItemDefinition item, StorageRoutingSettings settings)
        {
            if (settings.ItemStorageTypes.TryGetValue(item.Id, out var itemStorageType))
            {
                return itemStorageType;
            }

            if (settings.GroupStorageTypes.TryGetValue(item.GroupId, out var groupStorageType))
            {
                return groupStorageType;
            }

            return settings.MasterStorageType;
        }

        private async Task<bool> MigrateCatalogItemAsync(StorageCatalogItemDefinition item, StorageType sourceStorageType, StorageType targetStorageType, CancellationToken cancellationToken)
        {
            if (item.MatchMode == StorageCatalogItemMatchMode.ExactKey)
            {
                return await MigrateStorageKeyAsync(item.MatchPattern, item.SerializationMode, sourceStorageType, targetStorageType, cancellationToken);
            }

            if (sourceStorageType == StorageType.LocalStorage)
            {
                var localEntries = await GetLocalEntriesByPrefixAsync(item.MatchPattern, cancellationToken);
                foreach (var entry in localEntries)
                {
                    if (!await MigrateStorageKeyAsync(entry.Key, item.SerializationMode, sourceStorageType, targetStorageType, cancellationToken, sourceLocalValue: entry.Value))
                    {
                        return false;
                    }
                }

                return true;
            }

            var clientEntriesResult = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
            if (!clientEntriesResult.Succeeded || clientEntriesResult.Entries is null)
            {
                await HandleClientDataFailureAsync(clientEntriesResult.FailureResult, cancellationToken);
                return false;
            }

            var clientEntries = clientEntriesResult.Entries;
            foreach (var entry in clientEntries)
            {
                var unprefixedKey = ToUnprefixedKey(entry.Key);
                if (unprefixedKey is null)
                {
                    continue;
                }

                if (!unprefixedKey.StartsWith(item.MatchPattern, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!await MigrateStorageKeyAsync(unprefixedKey, item.SerializationMode, sourceStorageType, targetStorageType, cancellationToken, sourceClientValue: entry.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> MigrateStorageKeyAsync(
            string key,
            StorageItemSerializationMode serializationMode,
            StorageType sourceStorageType,
            StorageType targetStorageType,
            CancellationToken cancellationToken,
            string? sourceLocalValue = null,
            JsonElement? sourceClientValue = null)
        {
            var prefixedKey = ToPrefixedKey(key);

            if (sourceStorageType == StorageType.LocalStorage && targetStorageType == StorageType.ClientData)
            {
                var localValue = sourceLocalValue ?? await _localStorageService.GetItemAsStringAsync(key, cancellationToken);
                if (localValue is null)
                {
                    return true;
                }

                object? valueToStore = serializationMode == StorageItemSerializationMode.RawString
                    ? localValue
                    : ParseJsonElement(localValue);

                var storeResult = await _clientDataStorageAdapter.StorePrefixedEntriesAsync(
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        [prefixedKey] = valueToStore
                    },
                    cancellationToken);
                if (!storeResult.Succeeded)
                {
                    await HandleClientDataFailureAsync(storeResult.FailureResult, cancellationToken);
                    return false;
                }

                await _localStorageService.RemoveItemAsync(key, cancellationToken);
                return true;
            }

            if (sourceStorageType == StorageType.ClientData && targetStorageType == StorageType.LocalStorage)
            {
                JsonElement? clientValue = sourceClientValue;
                if (!clientValue.HasValue)
                {
                    var loadedResult = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync([prefixedKey], cancellationToken);
                    if (!loadedResult.Succeeded || loadedResult.Entries is null)
                    {
                        await HandleClientDataFailureAsync(loadedResult.FailureResult, cancellationToken);
                        return false;
                    }

                    var loaded = loadedResult.Entries;
                    if (!loaded.TryGetValue(prefixedKey, out var loadedValue))
                    {
                        return true;
                    }

                    clientValue = loadedValue;
                }

                var localValue = ConvertClientValueToLocalString(clientValue.Value, serializationMode);
                if (localValue is null)
                {
                    await _localStorageService.RemoveItemAsync(key, cancellationToken);
                }
                else
                {
                    await _localStorageService.SetItemAsStringAsync(key, localValue, cancellationToken);
                }

                var removeResult = await _clientDataStorageAdapter.RemovePrefixedEntriesAsync([prefixedKey], cancellationToken);
                if (!removeResult.Succeeded)
                {
                    await HandleClientDataFailureAsync(removeResult.FailureResult, cancellationToken);
                    return false;
                }

                return true;
            }

            return true;
        }

        private async Task HandleClientDataFailureAsync(ApiResultBase? failureResult, CancellationToken cancellationToken)
        {
            if (failureResult is not null)
            {
                await _apiFeedbackWorkflow.HandleFailureAsync(failureResult, cancellationToken: cancellationToken);
            }
        }

        private async Task<IReadOnlyDictionary<string, string?>> GetLocalEntriesByPrefixAsync(string keyPrefix, CancellationToken cancellationToken)
        {
            var entries = await _localStorageEntryAdapter.GetEntriesAsync(cancellationToken);
            var result = new Dictionary<string, string?>(StringComparer.Ordinal);

            foreach (var entry in entries)
            {
                if (entry is null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                var unprefixedKey = ToUnprefixedKey(entry.Key);
                if (unprefixedKey is null)
                {
                    continue;
                }

                if (!unprefixedKey.StartsWith(keyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                result[unprefixedKey] = entry.Value;
            }

            return result;
        }

        private static JsonElement ParseJsonElement(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static string? ConvertClientValueToLocalString(JsonElement value, StorageItemSerializationMode serializationMode)
        {
            if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (serializationMode == StorageItemSerializationMode.RawString)
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString() ?? string.Empty;
                }

                return value.GetRawText();
            }

            return value.GetRawText();
        }

        private StorageRoutingSettings NormalizeForCatalog(StorageRoutingSettings settings)
        {
            var normalized = StorageRoutingSettings.Normalize(settings);

            var knownGroupIds = _storageCatalogService.Groups
                .Select(group => group.Id)
                .ToHashSet(StringComparer.Ordinal);
            var knownItemIds = _storageCatalogService.Items
                .Select(item => item.Id)
                .ToHashSet(StringComparer.Ordinal);

            normalized.GroupStorageTypes = normalized.GroupStorageTypes
                .Where(entry => knownGroupIds.Contains(entry.Key))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            normalized.ItemStorageTypes = normalized.ItemStorageTypes
                .Where(entry => knownItemIds.Contains(entry.Key))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

            return normalized;
        }

        private static bool AreEquivalent(StorageRoutingSettings left, StorageRoutingSettings right)
        {
            if (left.MasterStorageType != right.MasterStorageType)
            {
                return false;
            }

            if (left.GroupStorageTypes.Count != right.GroupStorageTypes.Count)
            {
                return false;
            }

            if (left.ItemStorageTypes.Count != right.ItemStorageTypes.Count)
            {
                return false;
            }

            foreach (var (key, value) in left.GroupStorageTypes)
            {
                if (!right.GroupStorageTypes.TryGetValue(key, out var rightValue) || rightValue != value)
                {
                    return false;
                }
            }

            foreach (var (key, value) in left.ItemStorageTypes)
            {
                if (!right.ItemStorageTypes.TryGetValue(key, out var rightValue) || rightValue != value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsClientDataSelection(StorageRoutingSettings settings)
        {
            if (settings.MasterStorageType == StorageType.ClientData)
            {
                return true;
            }

            if (settings.GroupStorageTypes.Values.Any(value => value == StorageType.ClientData))
            {
                return true;
            }

            return settings.ItemStorageTypes.Values.Any(value => value == StorageType.ClientData);
        }

        private static string ToPrefixedKey(string key)
        {
            if (key.StartsWith(StorageKeys.Prefix, StringComparison.Ordinal))
            {
                return key;
            }

            return string.Concat(StorageKeys.Prefix, key);
        }

        private static string? ToUnprefixedKey(string prefixedKey)
        {
            if (!prefixedKey.StartsWith(StorageKeys.Prefix, StringComparison.Ordinal))
            {
                return null;
            }

            return prefixedKey[StorageKeys.Prefix.Length..];
        }
    }
}
