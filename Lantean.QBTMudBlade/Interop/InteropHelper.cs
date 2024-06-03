using Microsoft.JSInterop;

namespace Lantean.QBTMudBlade.Interop
{
    public static class InteropHelper
    {
        public static async Task<BoundingClientRect?> GetBoundingClientRect(this IJSRuntime runtime, string id)
        {
            return await runtime.InvokeAsync<BoundingClientRect?>("qbt.getBoundingClientRect", id);
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
    }
}