namespace Blazor.BrowserCapabilities
{
    /// <summary>
    /// Provides cached browser capability information gathered during startup.
    /// </summary>
    public interface IBrowserCapabilitiesService
    {
        /// <summary>
        /// Gets a value indicating whether capability detection has completed.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the current capability snapshot.
        /// </summary>
        BrowserCapabilities Capabilities { get; }

        /// <summary>
        /// Initializes browser capability detection if it has not already completed.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask EnsureInitialized(CancellationToken cancellationToken = default);
    }
}
