using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Interop;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Infrastructure.Services
{
    /// <summary>
    /// JS interop implementation of <see cref="ILocalStorageEntryAdapter"/>.
    /// </summary>
    public sealed class LocalStorageEntryAdapter : ILocalStorageEntryAdapter
    {
        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalStorageEntryAdapter"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime.</param>
        public LocalStorageEntryAdapter(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<BrowserStorageEntry>> GetEntriesByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return await _jsRuntime.GetLocalStorageEntriesByPrefix(prefix, cancellationToken);
        }

        /// <inheritdoc />
        public async Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default)
        {
            await _jsRuntime.RemoveLocalStorageEntry(key, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> ClearEntriesByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return await _jsRuntime.ClearLocalStorageEntriesByPrefix(prefix, cancellationToken);
        }
    }
}
