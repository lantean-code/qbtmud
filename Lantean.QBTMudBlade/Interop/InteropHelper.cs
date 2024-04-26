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
    }
}