using Microsoft.JSInterop;

namespace Lantean.QBTMud.Interop
{
    public static class InteropHelper
    {
        public static async Task<BoundingClientRect?> GetBoundingClientRect(this IJSRuntime runtime, string selector)
        {
            return await runtime.InvokeAsync<BoundingClientRect?>("qbt.getBoundingClientRect", selector);
        }

        public static async Task<ClientSize?> GetWindowSize(this IJSRuntime runtime)
        {
            return await runtime.InvokeAsync<ClientSize?>("qbt.getWindowSize");
        }

        public static async Task<ClientSize?> GetInnerDimensions(this IJSRuntime runtime, string selector)
        {
            return await runtime.InvokeAsync<ClientSize?>("qbt.getInnerDimensions", selector);
        }

        public static async Task FileDownload(this IJSRuntime runtime, string url, string? filename = null)
        {
            await runtime.InvokeVoidAsync("qbt.triggerFileDownload", url, filename);
        }

        public static async Task Open(this IJSRuntime runtime, string url, bool newTab = false)
        {
            string? target = null;
            if (newTab)
            {
                target = url;
            }
            await runtime.InvokeVoidAsync("qbt.open", url, target);
        }

        public static async Task<MagnetRegistrationResult> RegisterMagnetHandler(this IJSRuntime runtime, string templateUrl)
        {
            return await runtime.InvokeAsync<MagnetRegistrationResult>("qbt.registerMagnetHandler", templateUrl);
        }

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

        public static async Task ClearSelection(this IJSRuntime runtime)
        {
            await runtime.InvokeVoidAsync("qbt.clearSelection");
        }
    }
}
