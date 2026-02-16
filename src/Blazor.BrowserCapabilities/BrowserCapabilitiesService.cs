using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Blazor.BrowserCapabilities
{
    /// <summary>
    /// Detects and caches browser capabilities for the current session.
    /// </summary>
    public sealed class BrowserCapabilitiesService : IBrowserCapabilitiesService, IAsyncDisposable
    {
        private const string JSImportIdentifier = "import";
        private const string JSImportPath = "./_content/Blazor.BrowserCapabilities/browser-capabilities.module.js";
        private const string BrowserCapabilitiesExport = "getCapabilities";
        private const string SupportsHoverPointerExport = "supportsHoverPointer";

        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
        private readonly IJSRuntime _jSRuntime;
        private readonly ILogger<BrowserCapabilitiesService> _logger;

        private IJSObjectReference? _module;
        private BrowserCapabilities _capabilities = BrowserCapabilities.Default;
        private bool _isInitialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserCapabilitiesService"/> class.
        /// </summary>
        /// <param name="jSRuntime">The JavaScript runtime.</param>
        /// <param name="logger">The logger instance.</param>
        public BrowserCapabilitiesService(IJSRuntime jSRuntime, ILogger<BrowserCapabilitiesService> logger)
        {
            _jSRuntime = jSRuntime;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }
        }

        /// <inheritdoc />
        public BrowserCapabilities Capabilities
        {
            get
            {
                return _capabilities;
            }
        }

        /// <inheritdoc />
        public async ValueTask EnsureInitialized(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isInitialized)
            {
                return;
            }

            await _initializationLock.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                _capabilities = await LoadCapabilities(cancellationToken);
                _isInitialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        /// <summary>
        /// Releases managed resources used by this instance.
        /// </summary>
        /// <returns>A task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_module is not null)
            {
                try
                {
                    await _module.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                }
                catch (ObjectDisposedException)
                {
                }

                _module = null;
            }

            _initializationLock.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<BrowserCapabilities> LoadCapabilities(CancellationToken cancellationToken)
        {
            try
            {
                var module = await GetModule(cancellationToken);
                var capabilities = await module.InvokeAsync<BrowserCapabilities?>(BrowserCapabilitiesExport, cancellationToken);
                return capabilities ?? BrowserCapabilities.Default;
            }
            catch (JSException ex)
            {
                _logger.LogDebug(ex, "Could not read browser capabilities object. Falling back to supportsHoverPointer interop.");
            }

            try
            {
                var module = await GetModule(cancellationToken);
                var supportsHoverPointer = await module.InvokeAsync<bool>(SupportsHoverPointerExport, cancellationToken);
                return new BrowserCapabilities(
                    SupportsHoverPointer: supportsHoverPointer,
                    SupportsHover: supportsHoverPointer,
                    SupportsFinePointer: supportsHoverPointer,
                    SupportsCoarsePointer: false,
                    SupportsPointerEvents: false,
                    HasTouchInput: false,
                    MaxTouchPoints: 0,
                    PrefersReducedMotion: false,
                    PrefersReducedData: false,
                    PrefersDarkColorScheme: false,
                    ForcedColorsActive: false,
                    PrefersHighContrast: false,
                    SupportsClipboardRead: false,
                    SupportsClipboardWrite: false,
                    SupportsShareApi: false,
                    SupportsInstallPrompt: false,
                    IsAppleMobilePlatform: false,
                    IsStandaloneDisplayMode: false);
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "Could not determine browser capabilities. Falling back to conservative defaults.");
                return BrowserCapabilities.Default;
            }
        }

        private async Task<IJSObjectReference> GetModule(CancellationToken cancellationToken)
        {
            if (_module is not null)
            {
                return _module;
            }

            _module = await _jSRuntime.InvokeAsync<IJSObjectReference>(JSImportIdentifier, cancellationToken, JSImportPath);
            return _module;
        }
    }
}
