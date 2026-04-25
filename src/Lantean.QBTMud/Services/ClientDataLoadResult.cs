using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the outcome of a ClientData load operation.
    /// </summary>
    public sealed class ClientDataLoadResult
    {
        /// <summary>
        /// Gets a failed ClientData load result.
        /// </summary>
        public static ClientDataLoadResult Failure { get; } = new(false, null);

        private ClientDataLoadResult(bool succeeded, IReadOnlyDictionary<string, JsonElement>? entries)
        {
            Succeeded = succeeded;
            Entries = entries;
        }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the loaded ClientData entries.
        /// </summary>
        public IReadOnlyDictionary<string, JsonElement>? Entries { get; }

        /// <summary>
        /// Creates a successful ClientData load result.
        /// </summary>
        /// <param name="entries">The loaded ClientData entries.</param>
        /// <returns>The successful ClientData load result.</returns>
        public static ClientDataLoadResult FromEntries(IReadOnlyDictionary<string, JsonElement> entries)
        {
            return new ClientDataLoadResult(true, entries);
        }
    }
}
