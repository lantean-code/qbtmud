using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides diagnostics and cleanup operations for qbtmud storage data.
    /// </summary>
    public sealed class StorageDiagnosticsService : IStorageDiagnosticsService
    {
        private const int PreviewMaxLength = 160;

        private readonly IJSRuntime _jsRuntime;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly IWebApiCapabilityService _webApiCapabilityService;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageDiagnosticsService"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime.</param>
        /// <param name="clientDataStorageAdapter">The ClientData storage adapter.</param>
        /// <param name="webApiCapabilityService">The Web API capability service.</param>
        public StorageDiagnosticsService(
            IJSRuntime jsRuntime,
            IClientDataStorageAdapter clientDataStorageAdapter,
            IWebApiCapabilityService webApiCapabilityService)
        {
            _jsRuntime = jsRuntime;
            _clientDataStorageAdapter = clientDataStorageAdapter;
            _webApiCapabilityService = webApiCapabilityService;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AppStorageEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<AppStorageEntry>();
            var localEntries = await _jsRuntime.GetLocalStorageEntriesByPrefix(ClientDataStorageAdapter.StorageKeyPrefix, cancellationToken);

            foreach (var entry in localEntries.Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.Key)))
            {
                var displayKey = entry.Key.StartsWith(ClientDataStorageAdapter.StorageKeyPrefix, StringComparison.Ordinal)
                    ? entry.Key[ClientDataStorageAdapter.StorageKeyPrefix.Length..]
                    : entry.Key;
                var value = entry.Value;

                result.Add(new AppStorageEntry(
                    StorageType.LocalStorage,
                    entry.Key,
                    displayKey,
                    value,
                    BuildPreview(value),
                    value?.Length ?? 0));
            }

            var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
            if (capabilityState.SupportsClientData)
            {
                var clientEntries = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
                foreach (var (key, value) in clientEntries)
                {
                    var displayKey = key.StartsWith(ClientDataStorageAdapter.StorageKeyPrefix, StringComparison.Ordinal)
                        ? key[ClientDataStorageAdapter.StorageKeyPrefix.Length..]
                        : key;
                    var textValue = ConvertClientValueToText(value);

                    result.Add(new AppStorageEntry(
                        StorageType.ClientData,
                        key,
                        displayKey,
                        textValue,
                        BuildPreview(textValue),
                        textValue?.Length ?? 0));
                }
            }

            return result
                .OrderBy(entry => entry.DisplayKey, StringComparer.Ordinal)
                .ThenBy(entry => entry.StorageType)
                .ToList();
        }

        /// <inheritdoc />
        public async Task RemoveEntryAsync(StorageType storageType, string prefixedKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prefixedKey))
            {
                return;
            }

            if (!prefixedKey.StartsWith(ClientDataStorageAdapter.StorageKeyPrefix, StringComparison.Ordinal))
            {
                return;
            }

            if (storageType == StorageType.LocalStorage)
            {
                await _jsRuntime.RemoveLocalStorageEntry(prefixedKey, cancellationToken);
                return;
            }

            var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
            if (!capabilityState.SupportsClientData)
            {
                return;
            }

            await _clientDataStorageAdapter.RemovePrefixedEntriesAsync([prefixedKey], cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> ClearEntriesAsync(StorageType? storageType = null, CancellationToken cancellationToken = default)
        {
            var removedCount = 0;
            if (storageType is null || storageType == StorageType.LocalStorage)
            {
                removedCount += await _jsRuntime.ClearLocalStorageEntriesByPrefix(ClientDataStorageAdapter.StorageKeyPrefix, cancellationToken);
            }

            if (storageType is null || storageType == StorageType.ClientData)
            {
                var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
                if (capabilityState.SupportsClientData)
                {
                    var clientEntries = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
                    if (clientEntries.Count > 0)
                    {
                        await _clientDataStorageAdapter.RemovePrefixedEntriesAsync(clientEntries.Keys, cancellationToken);
                        removedCount += clientEntries.Count;
                    }
                }
            }

            return removedCount;
        }

        private static string BuildPreview(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= PreviewMaxLength
                ? value
                : string.Concat(value[..PreviewMaxLength], "...");
        }

        private static string? ConvertClientValueToText(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return value.GetRawText();
        }
    }
}
