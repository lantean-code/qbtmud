using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides diagnostics and cleanup operations for qbtmud local storage data.
    /// </summary>
    public sealed class StorageDiagnosticsService : IStorageDiagnosticsService
    {
        private const string StorageKeyPrefix = "QbtMud.";
        private const int PreviewMaxLength = 160;

        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageDiagnosticsService"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime.</param>
        public StorageDiagnosticsService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AppStorageEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
        {
            var entries = await _jsRuntime.GetLocalStorageEntriesByPrefix(StorageKeyPrefix, cancellationToken);
            var result = entries
                .Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.Key))
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry =>
                {
                    var value = entry.Value;
                    var displayKey = entry.Key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal)
                        ? entry.Key[StorageKeyPrefix.Length..]
                        : entry.Key;
                    var preview = BuildPreview(value);
                    var length = value?.Length ?? 0;
                    return new AppStorageEntry(entry.Key, displayKey, value, preview, length);
                })
                .ToList();
            return result;
        }

        /// <inheritdoc />
        public async Task RemoveEntryAsync(string prefixedKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prefixedKey))
            {
                return;
            }

            if (!prefixedKey.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
            {
                return;
            }

            await _jsRuntime.RemoveLocalStorageEntry(prefixedKey, cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> ClearEntriesAsync(CancellationToken cancellationToken = default)
        {
            return _jsRuntime.ClearLocalStorageEntriesByPrefix(StorageKeyPrefix, cancellationToken);
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
    }
}
