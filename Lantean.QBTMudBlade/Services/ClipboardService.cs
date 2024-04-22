using Microsoft.JSInterop;

namespace Lantean.QBTMudBlade.Services
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
            await _jSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }
}
