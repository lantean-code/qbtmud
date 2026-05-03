using Microsoft.JSInterop;

namespace Lantean.QBTMud.BrowserCapabilities
{
    /// <summary>
    /// Detects and caches browser capabilities for the current session.
    /// </summary>
    public sealed class BrowserCapabilitiesService : IBrowserCapabilitiesService, IAsyncDisposable
    {
        private const string _jsImportIdentifier = "import";
        private const string _jsImportPath = "./_content/Lantean.QBTMud.Presentation/browser-capabilities.module.js";
        private const string _browserCapabilitiesExport = "getCapabilities";
        private const string _supportsHoverPointerExport = "supportsHoverPointer";

        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
        private readonly IJSRuntime _jSRuntime;
        private readonly ILogger<BrowserCapabilitiesService> _logger;

        private IJSObjectReference? _module;
        private BrowserCapabilityState _capabilities = BrowserCapabilityState.Default;
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
        public BrowserCapabilityState Capabilities
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
                catch (JSDisconnectedException ex)
                {
                    _logger.LogDebug(ex, "JS runtime disconnected while disposing the browser capabilities module.");
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.LogDebug(ex, "The browser capabilities module was already disposed when attempting to dispose it.");
                }

                _module = null;
            }

            _initializationLock.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<BrowserCapabilityState> LoadCapabilities(CancellationToken cancellationToken)
        {
            try
            {
                var module = await GetModule(cancellationToken);
                var capabilities = await module.InvokeAsync<BrowserCapabilityState?>(_browserCapabilitiesExport, cancellationToken);
                return capabilities ?? BrowserCapabilityState.Default;
            }
            catch (JSException ex)
            {
                _logger.LogDebug(ex, "Could not read browser capabilities object. Falling back to supportsHoverPointer interop.");
            }

            try
            {
                var module = await GetModule(cancellationToken);
                var supportsHoverPointer = await module.InvokeAsync<bool>(_supportsHoverPointerExport, cancellationToken);
                return new BrowserCapabilityState(
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
                return BrowserCapabilityState.Default;
            }
        }

        private async Task<IJSObjectReference> GetModule(CancellationToken cancellationToken)
        {
            if (_module is not null)
            {
                return _module;
            }

            _module = await _jSRuntime.InvokeAsync<IJSObjectReference>(_jsImportIdentifier, cancellationToken, _jsImportPath);
            return _module;
        }
    }
}
