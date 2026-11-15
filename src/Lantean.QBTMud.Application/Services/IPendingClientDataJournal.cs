using System.Text.Json;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Coordinates pending ClientData changes across browser tabs.
    /// </summary>
    public interface IPendingClientDataJournal
    {
        /// <summary>Reads the current pending changes.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A snapshot of pending values and removals.</returns>
        ValueTask<Dictionary<string, JsonElement?>> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>Atomically records a pending change without replacing unrelated entries.</summary>
        /// <param name="key">The prefixed settings key.</param>
        /// <param name="value">The pending value, or null for a removal.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The asynchronous operation.</returns>
        ValueTask SetAsync(string key, JsonElement? value, CancellationToken cancellationToken = default);

        /// <summary>Acknowledges only pending entries whose values still match the supplied snapshot.</summary>
        /// <param name="changes">The changes successfully replayed or migrated.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The pending changes remaining after acknowledgement.</returns>
        ValueTask<Dictionary<string, JsonElement?>> CompleteAsync(IReadOnlyDictionary<string, JsonElement?> changes, CancellationToken cancellationToken = default);

        /// <summary>Atomically removes a pending entry after an explicit deletion.</summary>
        /// <param name="key">The prefixed settings key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The asynchronous operation.</returns>
        ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Clears pending changes after an explicit storage reset.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The asynchronous operation.</returns>
        ValueTask ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>Deletes a migrated local source only when it is not a pending fallback.</summary>
        /// <param name="key">The unprefixed local storage key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The asynchronous operation.</returns>
        ValueTask RemoveLocalSourceAsync(string key, CancellationToken cancellationToken = default);
    }
}
