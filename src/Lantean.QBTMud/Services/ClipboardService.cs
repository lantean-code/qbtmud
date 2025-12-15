using Microsoft.JSInterop;

namespace Lantean.QBTMud.Services
{
    public class ClipboardService : IClipboardService
    {
        private readonly IJSRuntime _jSRuntime;

        public ClipboardService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task WriteToClipboard(string text)
        {
            try
            {
                await _jSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            }
            catch (JSException)
            {
                // Clipboard API unavailable or denied; ignore to avoid breaking UI.
            }
        }
    }
}
