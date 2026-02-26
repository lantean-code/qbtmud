using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace Blazor.BrowserCapabilities.Test
{
    public sealed class BrowserCapabilitiesServiceTests
    {
        private const string JSImportIdentifier = "import";
        private const string JSImportPath = "./_content/Blazor.BrowserCapabilities/browser-capabilities.module.js";

        private readonly IJSRuntime _jSRuntime;
        private readonly IJSObjectReference _jSModule;
        private readonly ILogger<BrowserCapabilitiesService> _logger;
        private readonly BrowserCapabilitiesService _target;

        public BrowserCapabilitiesServiceTests()
        {
            _jSRuntime = Mock.Of<IJSRuntime>();
            _jSModule = Mock.Of<IJSObjectReference>();
            _logger = Mock.Of<ILogger<BrowserCapabilitiesService>>();
            _target = new BrowserCapabilitiesService(_jSRuntime, _logger);

            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<IJSObjectReference>(JSImportIdentifier, It.IsAny<CancellationToken>(), It.Is<object?[]?>(args => HasJSImportPath(args))))
                .Returns(new ValueTask<IJSObjectReference>(_jSModule));
        }

        [Fact]
        public async Task GIVEN_NotInitialized_WHEN_EnsureInitialized_THEN_LoadsCapabilitiesAndSetsState()
        {
            var capabilities = new BrowserCapabilities(
                SupportsHoverPointer: true,
                SupportsHover: true,
                SupportsFinePointer: true,
                SupportsCoarsePointer: false,
                SupportsPointerEvents: true,
                HasTouchInput: false,
                MaxTouchPoints: 0,
                PrefersReducedMotion: true,
                PrefersReducedData: true,
                PrefersDarkColorScheme: true,
                ForcedColorsActive: true,
                PrefersHighContrast: true,
                SupportsClipboardRead: true,
                SupportsClipboardWrite: true,
                SupportsShareApi: true,
                SupportsInstallPrompt: true,
                IsAppleMobilePlatform: false,
                IsStandaloneDisplayMode: false);

            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<BrowserCapabilities?>(capabilities));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.IsInitialized.Should().BeTrue();
            _target.Capabilities.Should().Be(capabilities);

            Mock.Get(_jSRuntime)
                .Verify(runtime => runtime.InvokeAsync<IJSObjectReference>(JSImportIdentifier, It.IsAny<CancellationToken>(), It.Is<object?[]?>(args => HasJSImportPath(args))), Times.Once);
            Mock.Get(_jSModule)
                .Verify(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()), Times.Once);
            Mock.Get(_jSModule)
                .Verify(module => module.InvokeAsync<bool>("supportsHoverPointer", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AlreadyInitialized_WHEN_EnsureInitializedInvokedAgain_THEN_DoesNotInvokeInteropTwice()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<BrowserCapabilities?>(BrowserCapabilities.Default));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.IsInitialized.Should().BeTrue();
            Mock.Get(_jSRuntime)
                .Verify(runtime => runtime.InvokeAsync<IJSObjectReference>(JSImportIdentifier, It.IsAny<CancellationToken>(), It.Is<object?[]?>(args => HasJSImportPath(args))), Times.Once);
            Mock.Get(_jSModule)
                .Verify(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_PrimaryInteropThrows_WHEN_EnsureInitialized_THEN_UsesFallbackInterop()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ThrowsAsync(new JSException("PrimaryFailure"));
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<bool>("supportsHoverPointer", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<bool>(true));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.IsInitialized.Should().BeTrue();
            _target.Capabilities.Should().BeEquivalentTo(new BrowserCapabilities(
                SupportsHoverPointer: true,
                SupportsHover: true,
                SupportsFinePointer: true,
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
                IsStandaloneDisplayMode: false));
            Mock.Get(_jSModule)
                .Verify(module => module.InvokeAsync<bool>("supportsHoverPointer", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_BothInteropCallsThrow_WHEN_EnsureInitialized_THEN_FallsBackToConservativeDefaults()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ThrowsAsync(new JSException("PrimaryFailure"));
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<bool>("supportsHoverPointer", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ThrowsAsync(new JSException("FallbackFailure"));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.IsInitialized.Should().BeTrue();
            _target.Capabilities.Should().Be(BrowserCapabilities.Default);
        }

        [Fact]
        public async Task GIVEN_InteropReturnsNull_WHEN_EnsureInitialized_THEN_UsesConservativeDefaults()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<BrowserCapabilities?>((BrowserCapabilities?)null));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.IsInitialized.Should().BeTrue();
            _target.Capabilities.Should().Be(BrowserCapabilities.Default);
            Mock.Get(_jSModule)
                .Verify(module => module.InvokeAsync<bool>("supportsHoverPointer", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_Initialized_WHEN_Disposed_THEN_ShouldDisposeJavaScriptModule()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<BrowserCapabilities?>(BrowserCapabilities.Default));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            await _target.DisposeAsync();

            Mock.Get(_jSModule)
                .Verify(module => module.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_InitializedAndAlreadyDisposed_WHEN_DisposeCalledAgain_THEN_ShouldReturnWithoutThrowing()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<BrowserCapabilities?>(BrowserCapabilities.Default));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            await _target.DisposeAsync();
            await _target.DisposeAsync();

            Mock.Get(_jSModule).Verify(module => module.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ModuleDisposeThrowsJsDisconnectedException_WHEN_Disposed_THEN_ShouldSwallowException()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<BrowserCapabilities?>(BrowserCapabilities.Default));
            Mock.Get(_jSModule)
                .Setup(module => module.DisposeAsync())
                .Throws(new JSDisconnectedException("Disconnected"));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            var action = async () =>
            {
                await _target.DisposeAsync();
            };

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_ModuleDisposeThrowsObjectDisposedException_WHEN_Disposed_THEN_ShouldSwallowException()
        {
            Mock.Get(_jSModule)
                .Setup(module => module.InvokeAsync<BrowserCapabilities?>("getCapabilities", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .Returns(new ValueTask<BrowserCapabilities?>(BrowserCapabilities.Default));
            Mock.Get(_jSModule)
                .Setup(module => module.DisposeAsync())
                .Throws(new ObjectDisposedException("JSModule"));

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            var action = async () =>
            {
                await _target.DisposeAsync();
            };

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_Disposed_WHEN_EnsureInitialized_THEN_ShouldThrowObjectDisposedException()
        {
            await _target.DisposeAsync();

            Func<Task> action = async () =>
            {
                await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<ObjectDisposedException>();
        }

        private static bool HasJSImportPath(object?[]? arguments)
        {
            if (arguments is null || arguments.Length != 1)
            {
                return false;
            }

            return string.Equals(arguments[0] as string, JSImportPath, StringComparison.Ordinal);
        }
    }
}
