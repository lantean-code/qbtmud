using Microsoft.JSInterop;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates <see cref="IBrowserStorageService"/> instances bound to browser storage scopes.
    /// </summary>
    public sealed class BrowserStorageServiceFactory : IBrowserStorageServiceFactory
    {
        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserStorageServiceFactory"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime used to access browser storage.</param>
        public BrowserStorageServiceFactory(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public IBrowserStorageService CreateLocalStorageService()
        {
            return new BrowserStorageService(_jsRuntime, "localStorage");
        }

        /// <inheritdoc />
        public IBrowserStorageService CreateSessionStorageService()
        {
            return new BrowserStorageService(_jsRuntime, "sessionStorage");
        }
    }
}
