using Microsoft.JSInterop;

namespace Lantean.QBTMudBlade.Interop
{
    public static class InteropHelper
    {
        public static async Task<BoundingClientRect> GetBoundingClientRect(this IJSRuntime runtime, string selector)
        {
            return await runtime.InvokeAsync<BoundingClientRect>("qbt.getBoundingClientRect", selector);
        }
        
        public static async Task<ClientSize> GetWindowSize(this IJSRuntime runtime)
        {
            return await runtime.InvokeAsync<ClientSize>("qbt.getWindowSize");
        }
        
        public static async Task<ClientSize> GetInnerDimensions(this IJSRuntime runtime, string selector)
        {
            return await runtime.InvokeAsync<ClientSize>("qbt.getInnerDimensions", selector);
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

        public static async Task RenderPiecesBar(this IJSRuntime runtime, string id, string hash, int[] pieces, string? downloadingColor = null, string? haveColor = null, string? borderColor = null)
        {
            await runtime.InvokeVoidAsync("qbt.renderPiecesBar", id, hash, pieces, downloadingColor, haveColor, borderColor);
        }

        public static async Task WriteToClipboard(this IJSRuntime runtime, string value)
        {
            await runtime.InvokeVoidAsync("qbt.copyTextToClipboard", value);
        }

        public static async Task ClearSelection(this IJSRuntime runtime)
        {
            await runtime.InvokeVoidAsync("qbt.clearSelection");
        }
    }
}