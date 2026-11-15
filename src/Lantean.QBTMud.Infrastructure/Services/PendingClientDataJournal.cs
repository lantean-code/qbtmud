using System.Text.Json;
using Lantean.QBTMud.Application.Services;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Infrastructure.Services
{
    /// <summary>
    /// Maintains the local journal using browser transactions shared by all tabs.
    /// </summary>
    public sealed class PendingClientDataJournal : IPendingClientDataJournal
    {
        private readonly IJSRuntime _jsRuntime;

        /// <summary>Initialises the browser journal.</summary>
        /// <param name="jsRuntime">The browser interop runtime.</param>
        public PendingClientDataJournal(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public ValueTask<Dictionary<string, JsonElement?>> ReadAsync(CancellationToken cancellationToken = default)
        {
            return MutateAsync("read", null, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask SetAsync(string key, JsonElement? value, CancellationToken cancellationToken = default)
        {
            await MutateAsync("set", new Dictionary<string, JsonElement?> { [key] = value }, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask<Dictionary<string, JsonElement?>> CompleteAsync(IReadOnlyDictionary<string, JsonElement?> changes, CancellationToken cancellationToken = default)
        {
            return MutateAsync("complete", changes, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await MutateAsync("remove", key, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            await MutateAsync("clear", null, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask RemoveLocalSourceAsync(string key, CancellationToken cancellationToken = default)
        {
            await MutateAsync("removeLocalSource", key, cancellationToken);
        }

        private ValueTask<Dictionary<string, JsonElement?>> MutateAsync(string action, object? value, CancellationToken cancellationToken)
        {
            return _jsRuntime.InvokeAsync<Dictionary<string, JsonElement?>>("qbt.updatePendingClientDataJournal", cancellationToken,
                SettingsStorageService._pendingChangesStorageKey, action, value);
        }
    }
}
