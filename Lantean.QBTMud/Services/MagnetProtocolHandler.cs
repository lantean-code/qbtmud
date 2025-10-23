using Lantean.QBTMud.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Services
{
    public class MagnetProtocolHandler: IProtocolHandler
    {
        private readonly IJSRuntime _jSRuntime;
        public MagnetProtocolHandler(IJSRuntime jSRuntime, string url)
        {
            _jSRuntime = jSRuntime;
            Url = url;
        }
        public string Url { get; private set; }

        public async Task RegisterProtocol()
        {
            await _jSRuntime.InvokeVoidAsync("qbt.registerProtocolHandler", "magnet", Url);
        }
    }
}