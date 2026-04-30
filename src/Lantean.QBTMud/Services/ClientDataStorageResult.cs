namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the outcome of a ClientData storage operation.
    /// </summary>
    public sealed class ClientDataStorageResult
    {
        /// <summary>
        /// Gets a successful ClientData storage result.
        /// </summary>
        public static ClientDataStorageResult Success { get; } = new(true);

        /// <summary>
        /// Gets a failed ClientData storage result.
        /// </summary>
        public static ClientDataStorageResult Failure { get; } = new(false);

        private ClientDataStorageResult(bool succeeded)
        {
            Succeeded = succeeded;
        }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; }
    }
}
