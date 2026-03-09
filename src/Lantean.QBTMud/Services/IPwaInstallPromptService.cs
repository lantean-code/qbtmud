using Lantean.QBTMud.Interop;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides browser install prompt interop operations for Progressive Web App flows.
    /// </summary>
    public interface IPwaInstallPromptService
    {
        /// <summary>
        /// Gets the current install prompt state from the browser.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current install prompt state.</returns>
        Task<PwaInstallPromptState> GetInstallPromptStateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to browser install prompt state updates.
        /// </summary>
        /// <param name="dotNetObjectReference">The .NET reference used for JavaScript callbacks.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The subscription identifier, or <c>0</c> when subscription failed.</returns>
        Task<long> SubscribeInstallPromptStateAsync(object dotNetObjectReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes from browser install prompt state updates.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnsubscribeInstallPromptStateAsync(long subscriptionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests that the browser displays the install prompt.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The browser prompt outcome.</returns>
        Task<string> RequestInstallPromptAsync(CancellationToken cancellationToken = default);
    }
}
