using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides browser notification API operations and permission change tracking.
    /// </summary>
    public interface IBrowserNotificationService
    {
        /// <summary>
        /// Occurs when the cached browser notification permission changes.
        /// </summary>
        event EventHandler<BrowserNotificationPermissionChangedEventArgs>? PermissionChanged;

        /// <summary>
        /// Determines whether the browser notification API is supported.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see langword="true"/> when notifications are supported; otherwise <see langword="false"/>.</returns>
        Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current browser notification permission.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current browser notification permission.</returns>
        Task<BrowserNotificationPermission> GetPermissionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests browser notification permission.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resulting browser notification permission.</returns>
        Task<BrowserNotificationPermission> RequestPermissionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to browser notification permission change callbacks.
        /// </summary>
        /// <param name="dotNetObjectReference">The .NET callback target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The subscription identifier, or <c>0</c> when subscription failed.</returns>
        Task<long> SubscribePermissionChangesAsync(object dotNetObjectReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes from browser notification permission change callbacks.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnsubscribePermissionChangesAsync(long subscriptionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Shows a browser notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="body">The notification body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ShowNotificationAsync(string title, string body, CancellationToken cancellationToken = default);
    }
}
