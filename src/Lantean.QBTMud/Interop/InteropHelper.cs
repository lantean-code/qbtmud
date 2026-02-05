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
        /// <returns>The registration result.</returns>
        public static async Task<MagnetRegistrationResult> RegisterMagnetHandler(this IJSRuntime runtime, string templateUrl)
        {
            return await runtime.InvokeAsync<MagnetRegistrationResult>("qbt.registerMagnetHandler", templateUrl);
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
    }
}
