using System.Text.Json;
using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides diagnostics and cleanup operations for qbtmud storage data.
    /// </summary>
    public sealed class StorageDiagnosticsService : IStorageDiagnosticsService
    {
        private const int _previewMaxLength = 160;

        private readonly ILocalStorageEntryAdapter _localStorageEntryAdapter;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageDiagnosticsService"/> class.
        /// </summary>
        /// <param name="localStorageEntryAdapter">The local storage entry adapter.</param>
        /// <param name="clientDataStorageAdapter">The ClientData storage adapter.</param>
        /// <param name="webApiCapabilityService">The Web API capability service.</param>
        /// <param name="apiFeedbackWorkflow">The API feedback workflow.</param>
        public StorageDiagnosticsService(
            ILocalStorageEntryAdapter localStorageEntryAdapter,
            IClientDataStorageAdapter clientDataStorageAdapter,
            IWebApiCapabilityService webApiCapabilityService,
            IApiFeedbackWorkflow apiFeedbackWorkflow)
        {
            _localStorageEntryAdapter = localStorageEntryAdapter;
            _clientDataStorageAdapter = clientDataStorageAdapter;
            _webApiCapabilityService = webApiCapabilityService;
            _apiFeedbackWorkflow = apiFeedbackWorkflow;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AppStorageEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<AppStorageEntry>();
            var localEntries = await _localStorageEntryAdapter.GetEntriesAsync(cancellationToken);

            foreach (var entry in localEntries.Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.Key)))
            {
                var displayKey = entry.Key.StartsWith(StorageKeys.Prefix, StringComparison.Ordinal)
                    ? entry.Key[StorageKeys.Prefix.Length..]
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
                var clientEntriesResult = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
                if (clientEntriesResult.Succeeded && clientEntriesResult.Entries is not null)
                {
                    foreach (var (key, value) in clientEntriesResult.Entries)
                    {
                        var displayKey = key.StartsWith(StorageKeys.Prefix, StringComparison.Ordinal)
                            ? key[StorageKeys.Prefix.Length..]
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
                else
                {
                    await HandleClientDataFailureAsync(clientEntriesResult.FailureResult, cancellationToken);
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

            if (!prefixedKey.StartsWith(StorageKeys.Prefix, StringComparison.Ordinal))
            {
                return;
            }

            if (storageType == StorageType.LocalStorage)
            {
                await _localStorageEntryAdapter.RemoveEntryAsync(prefixedKey, cancellationToken);
                return;
            }

            var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
            if (!capabilityState.SupportsClientData)
            {
                return;
            }

            var removeResult = await _clientDataStorageAdapter.RemovePrefixedEntriesAsync([prefixedKey], cancellationToken);
            if (!removeResult.Succeeded)
            {
                await HandleClientDataFailureAsync(removeResult.FailureResult, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<int> ClearEntriesAsync(StorageType? storageType = null, CancellationToken cancellationToken = default)
        {
            var removedCount = 0;
            if (storageType is null || storageType == StorageType.LocalStorage)
            {
                removedCount += await _localStorageEntryAdapter.ClearEntriesAsync(cancellationToken);
            }

            if (storageType is null || storageType == StorageType.ClientData)
            {
                var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
                if (capabilityState.SupportsClientData)
                {
                    var clientEntriesResult = await _clientDataStorageAdapter.LoadPrefixedEntriesAsync(cancellationToken);
                    if (clientEntriesResult.Succeeded
                        && clientEntriesResult.Entries is not null
                        && clientEntriesResult.Entries.Count > 0)
                    {
                        var removeResult = await _clientDataStorageAdapter.RemovePrefixedEntriesAsync(clientEntriesResult.Entries.Keys, cancellationToken);
                        if (removeResult.Succeeded)
                        {
                            removedCount += clientEntriesResult.Entries.Count;
                        }
                        else
                        {
                            await HandleClientDataFailureAsync(removeResult.FailureResult, cancellationToken);
                        }
                    }
                    else if (!clientEntriesResult.Succeeded)
                    {
                        await HandleClientDataFailureAsync(clientEntriesResult.FailureResult, cancellationToken);
                    }
                }
            }

            return removedCount;
        }

        private async Task HandleClientDataFailureAsync(ApiResultBase? failureResult, CancellationToken cancellationToken)
        {
            if (failureResult is not null)
            {
                await _apiFeedbackWorkflow.HandleFailureAsync(failureResult, cancellationToken: cancellationToken);
            }
        }

        private static string BuildPreview(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= _previewMaxLength
                ? value
                : string.Concat(value[.._previewMaxLength], "...");
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
