using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Lantean.QBTMud.Services
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
        private readonly IJSRuntime _jsRuntime;
        private StorageRoutingSettings? _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageRoutingService"/> class.
        /// </summary>
        /// <param name="localStorageService">The local storage service.</param>
        /// <param name="clientDataStorageAdapter">The client data storage adapter.</param>
        /// <param name="webApiCapabilityService">The Web API capability service.</param>
        /// <param name="storageCatalogService">The routed storage catalog.</param>
        /// <param name="jsRuntime">The JavaScript runtime.</param>
        public StorageRoutingService(
            ILocalStorageService localStorageService,
            IClientDataStorageAdapter clientDataStorageAdapter,
            IWebApiCapabilityService webApiCapabilityService,
            IStorageCatalogService storageCatalogService,
            IJSRuntime jsRuntime)
        {
            _localStorageService = localStorageService;
            _clientDataStorageAdapter = clientDataStorageAdapter;
            _webApiCapabilityService = webApiCapabilityService;
            _storageCatalogService = storageCatalogService;
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public async Task<StorageRoutingSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            if (_cachedSettings is not null)
            {
                return _cachedSettings.Clone();
            }

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
            if (!capabilityState.SupportsClientData && ContainsClientDataSelection(normalizedSettings))
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

                await MigrateCatalogItemAsync(item, currentStorageType, targetStorageType, cancellationToken);
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

        private async Task MigrateCatalogItemAsync(StorageCatalogItemDefinition item, StorageType sourceStorageType, StorageType targetStorageType, CancellationToken cancellationToken)
        {
            if (item.MatchMode == StorageCatalogItemMatchMode.ExactKey)
            {
                await MigrateStorageKeyAsync(item.MatchPattern, item.SerializationMode, sourceStorageType, targetStorageType, cancellationToken);
                return;
            }

            if (sourceStorageType == StorageType.LocalStorage)
            {
                var localEntries = await GetLocalEntriesByPrefixAsync(item.MatchPattern, cancellationToken);
                foreach (var entry in localEntries)
                {
                    await MigrateStorageKeyAsync(entry.Key, item.SerializationMode, sourceStorageType, targetStorageType, cancellationToken, sourceLocalValue: entry.Value);
                }

                return;
            }

            var clientEntries = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
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

                await MigrateStorageKeyAsync(unprefixedKey, item.SerializationMode, sourceStorageType, targetStorageType, cancellationToken, sourceClientValue: entry.Value);
            }
        }

        private async Task MigrateStorageKeyAsync(
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
                    return;
                }

                object? valueToStore = serializationMode == StorageItemSerializationMode.RawString
                    ? localValue
                    : ParseJsonElement(localValue);

                await _clientDataStorageAdapter.StorePrefixedEntriesAsync(
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        [prefixedKey] = valueToStore
                    },
                    cancellationToken);
                await _localStorageService.RemoveItemAsync(key, cancellationToken);
                return;
            }

            if (sourceStorageType == StorageType.ClientData && targetStorageType == StorageType.LocalStorage)
            {
                JsonElement? clientValue = sourceClientValue;
                if (!clientValue.HasValue)
                {
                    var loaded = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync([prefixedKey], cancellationToken);
                    if (!loaded.TryGetValue(prefixedKey, out var loadedValue))
                    {
                        return;
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

                await _clientDataStorageAdapter.RemovePrefixedEntriesAsync([prefixedKey], cancellationToken);
            }
        }

        private async Task<IReadOnlyDictionary<string, string?>> GetLocalEntriesByPrefixAsync(string keyPrefix, CancellationToken cancellationToken)
        {
            var prefixedPrefix = ToPrefixedKey(keyPrefix);
            var entries = await _jsRuntime.GetLocalStorageEntriesByPrefix(prefixedPrefix, cancellationToken);
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
            if (key.StartsWith(ClientDataStorageAdapter.StorageKeyPrefix, StringComparison.Ordinal))
            {
                return key;
            }

            return string.Concat(ClientDataStorageAdapter.StorageKeyPrefix, key);
        }

        private static string? ToUnprefixedKey(string prefixedKey)
        {
            if (!prefixedKey.StartsWith(ClientDataStorageAdapter.StorageKeyPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            return prefixedKey[ClientDataStorageAdapter.StorageKeyPrefix.Length..];
        }
    }
}
