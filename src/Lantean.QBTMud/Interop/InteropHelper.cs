using Microsoft.JSInterop;

namespace Lantean.QBTMud.Interop
{
    public static class InteropHelper
    {
        /// <summary>
        /// Gets the bounding client rectangle for the first element matching the selector.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="selector">The CSS selector for the element.</param>
        /// <returns>The bounding client rectangle, or null if not found.</returns>
        public static async Task<BoundingClientRect?> GetBoundingClientRect(this IJSRuntime runtime, string selector)
        {
            return await runtime.InvokeAsync<BoundingClientRect?>("qbt.getBoundingClientRect", selector);
        }

        /// <summary>
        /// Gets the current window size.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <returns>The window size.</returns>
        public static async Task<ClientSize?> GetWindowSize(this IJSRuntime runtime)
        {
            return await runtime.InvokeAsync<ClientSize?>("qbt.getWindowSize");
        }

        /// <summary>
        /// Gets the inner dimensions of the first element matching the selector.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="selector">The CSS selector for the element.</param>
        /// <returns>The element dimensions, or null if not found.</returns>
        public static async Task<ClientSize?> GetInnerDimensions(this IJSRuntime runtime, string selector)
        {
            return await runtime.InvokeAsync<ClientSize?>("qbt.getInnerDimensions", selector);
        }

        /// <summary>
        /// Triggers a file download in the browser.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="url">The URL or data URL to download.</param>
        /// <param name="filename">The suggested file name.</param>
        public static async Task FileDownload(this IJSRuntime runtime, string url, string? filename = null)
        {
            await runtime.InvokeVoidAsync("qbt.triggerFileDownload", url, filename);
        }

        /// <summary>
        /// Opens a URL in the current tab or a new tab.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="url">The URL to open.</param>
        /// <param name="newTab">True to open in a new tab; otherwise false.</param>
        public static async Task Open(this IJSRuntime runtime, string url, bool newTab = false)
        {
            string? target = null;
            if (newTab)
            {
                target = url;
            }
            await runtime.InvokeVoidAsync("qbt.open", url, target);
        }

        /// <summary>
        /// Registers a magnet handler with the browser for a template URL.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="templateUrl">The handler template URL.</param>
        /// <param name="handlerName">The display name for the handler registration.</param>
        /// <returns>The registration result.</returns>
        public static async Task<MagnetRegistrationResult> RegisterMagnetHandler(this IJSRuntime runtime, string templateUrl, string handlerName)
        {
            return await runtime.InvokeAsync<MagnetRegistrationResult>("qbt.registerMagnetHandler", templateUrl, handlerName);
        }

        /// <summary>
        /// Determines whether the browser notification API is supported.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see langword="true"/> when notifications are supported; otherwise <see langword="false"/>.</returns>
        public static async Task<bool> IsNotificationsSupported(this IJSRuntime runtime, CancellationToken cancellationToken = default)
        {
            return await runtime.InvokeAsync<bool>("qbt.isNotificationSupported", cancellationToken);
        }

        /// <summary>
        /// Gets current browser notification permission.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current notification permission.</returns>
        public static async Task<BrowserNotificationPermission> GetNotificationPermission(this IJSRuntime runtime, CancellationToken cancellationToken = default)
        {
            var rawPermission = await runtime.InvokeAsync<string?>("qbt.getNotificationPermission", cancellationToken);
            return ParseNotificationPermission(rawPermission);
        }

        /// <summary>
        /// Requests browser notification permission.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resulting notification permission.</returns>
        public static async Task<BrowserNotificationPermission> RequestNotificationPermission(this IJSRuntime runtime, CancellationToken cancellationToken = default)
        {
            var rawPermission = await runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", cancellationToken);
            return ParseNotificationPermission(rawPermission);
        }

        /// <summary>
        /// Shows a browser notification.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="body">The notification body text.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task ShowNotification(this IJSRuntime runtime, string title, string body, CancellationToken cancellationToken = default)
        {
            await runtime.InvokeVoidAsync("qbt.showNotification", cancellationToken, title, body);
        }

        /// <summary>
        /// Gets local storage entries that match a key prefix.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="prefix">The key prefix.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Matching local storage entries.</returns>
        public static async Task<IReadOnlyList<BrowserStorageEntry>> GetLocalStorageEntriesByPrefix(this IJSRuntime runtime, string prefix, CancellationToken cancellationToken = default)
        {
            var entries = await runtime.InvokeAsync<BrowserStorageEntry[]?>("qbt.getLocalStorageEntriesByPrefix", cancellationToken, prefix);
            return entries ?? Array.Empty<BrowserStorageEntry>();
        }

        /// <summary>
        /// Removes a local storage entry by key.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="key">The storage key to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RemoveLocalStorageEntry(this IJSRuntime runtime, string key, CancellationToken cancellationToken = default)
        {
            await runtime.InvokeVoidAsync("qbt.removeLocalStorageEntry", cancellationToken, key);
        }

        /// <summary>
        /// Removes all local storage entries that match a key prefix.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="prefix">The key prefix.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of removed entries.</returns>
        public static async Task<int> ClearLocalStorageEntriesByPrefix(this IJSRuntime runtime, string prefix, CancellationToken cancellationToken = default)
        {
            return await runtime.InvokeAsync<int>("qbt.clearLocalStorageEntriesByPrefix", cancellationToken, prefix);
        }

        /// <summary>
        /// Renders a pieces bar visualization to the target element.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="id">The target element id.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="pieces">The pieces state data.</param>
        /// <param name="downloadingColor">The color for downloading pieces.</param>
        /// <param name="haveColor">The color for completed pieces.</param>
        /// <param name="borderColor">The color for the bar border.</param>
        public static async Task RenderPiecesBar(this IJSRuntime runtime, string id, string hash, int[] pieces, string? downloadingColor = null, string? haveColor = null, string? borderColor = null)
        {
            await runtime.InvokeVoidAsync("qbt.renderPiecesBar", id, hash, pieces, downloadingColor, haveColor, borderColor);
        }

        /// <summary>
        /// Loads a Google Fonts stylesheet into the document.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="url">The stylesheet URL to load.</param>
        /// <param name="id">An optional DOM id for the link element.</param>
        public static async Task LoadGoogleFont(this IJSRuntime runtime, string url, string? id = null)
        {
            await runtime.InvokeVoidAsync("qbt.loadGoogleFont", url, id);
        }

        /// <summary>
        /// Attempts to write text to the clipboard.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        /// <param name="value">The text to copy.</param>
        public static async Task WriteToClipboard(this IJSRuntime runtime, string value)
        {
            try
            {
                await runtime.InvokeVoidAsync("qbt.copyTextToClipboard", value);
            }
            catch (JSException)
            {
                // Clipboard API unavailable; ignore to avoid surfacing errors to the user.
            }
        }

        /// <summary>
        /// Clears the current text selection in the document.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        public static async Task ClearSelection(this IJSRuntime runtime)
        {
            await runtime.InvokeVoidAsync("qbt.clearSelection");
        }

        /// <summary>
        /// Removes the bootstrap theme style element if present.
        /// </summary>
        /// <param name="runtime">The JavaScript runtime.</param>
        public static async Task RemoveBootstrapTheme(this IJSRuntime runtime)
        {
            await runtime.InvokeVoidAsync("qbt.removeBootstrapTheme");
        }

        private static BrowserNotificationPermission ParseNotificationPermission(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return BrowserNotificationPermission.Default;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "granted" => BrowserNotificationPermission.Granted,
                "denied" => BrowserNotificationPermission.Denied,
                "default" => BrowserNotificationPermission.Default,
                "unsupported" => BrowserNotificationPermission.Unsupported,
                _ => BrowserNotificationPermission.Default
            };
        }
    }
}
