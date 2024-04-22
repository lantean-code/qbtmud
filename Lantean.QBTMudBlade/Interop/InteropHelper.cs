using Microsoft.JSInterop;

namespace Lantean.QBTMudBlade.Interop
{
    public static class InteropHelper
    {
        public static async Task<BoundingClientRect?> GetBoundingClientRect(this IJSRuntime runtime, string id)
        {
            return await runtime.InvokeAsync<BoundingClientRect?>("qbt.getBoundingClientRect", id);
        }
    }
}