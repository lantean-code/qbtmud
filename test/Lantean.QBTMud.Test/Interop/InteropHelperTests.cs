using AwesomeAssertions;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Interop
{
    public sealed class InteropHelperTests
    {
        private readonly TestJsRuntime _target;

        public InteropHelperTests()
        {
            _target = new TestJsRuntime();
        }

        [Fact]
        public async Task GIVEN_FontUrl_WHEN_LoadGoogleFontInvoked_THEN_InvokesRuntime()
        {
            await _target.LoadGoogleFont("Url", "Id");

            _target.LastIdentifier.Should().Be("qbt.loadGoogleFont");
            _target.LastArguments.Should().ContainInOrder("Url", "Id");
        }

        [Fact]
        public async Task GIVEN_Selector_WHEN_GetBoundingClientRectInvoked_THEN_ReturnsResult()
        {
            var expected = new BoundingClientRect
            {
                Width = 12,
                Height = 34,
                Top = 1,
                Bottom = 2,
                Left = 3,
                Right = 4,
                X = 5,
                Y = 6
            };
            _target.EnqueueResult(expected);

            var result = await _target.GetBoundingClientRect("Selector");

            result.Should().BeSameAs(expected);
            _target.LastIdentifier.Should().Be("qbt.getBoundingClientRect");
            _target.LastArguments.Should().ContainSingle().Which.Should().Be("Selector");
        }

        [Fact]
        public async Task GIVEN_WindowSizeResult_WHEN_GetWindowSizeInvoked_THEN_ReturnsResult()
        {
            var expected = new ClientSize
            {
                Width = 1920,
                Height = 1080
            };
            _target.EnqueueResult(expected);

            var result = await _target.GetWindowSize();

            result.Should().BeSameAs(expected);
            _target.LastIdentifier.Should().Be("qbt.getWindowSize");
        }

        [Fact]
        public async Task GIVEN_Selector_WHEN_GetInnerDimensionsInvoked_THEN_ReturnsResult()
        {
            var expected = new ClientSize
            {
                Width = 800,
                Height = 600
            };
            _target.EnqueueResult(expected);

            var result = await _target.GetInnerDimensions("Selector");

            result.Should().BeSameAs(expected);
            _target.LastIdentifier.Should().Be("qbt.getInnerDimensions");
            _target.LastArguments.Should().ContainSingle().Which.Should().Be("Selector");
        }

        [Fact]
        public async Task GIVEN_DownloadRequest_WHEN_FileDownloadInvoked_THEN_InvokesRuntime()
        {
            await _target.FileDownload("Url", "File.json");

            _target.LastIdentifier.Should().Be("qbt.triggerFileDownload");
            _target.LastArguments.Should().ContainInOrder("Url", "File.json");
        }

        [Fact]
        public async Task GIVEN_NewTabFalse_WHEN_OpenInvoked_THEN_UsesNullTarget()
        {
            await _target.Open("Url");

            _target.LastIdentifier.Should().Be("qbt.open");
            _target.LastArguments.Should().ContainInOrder("Url", null);
        }

        [Fact]
        public async Task GIVEN_NewTabTrue_WHEN_OpenInvoked_THEN_UsesUrlTarget()
        {
            await _target.Open("Url", true);

            _target.LastIdentifier.Should().Be("qbt.open");
            _target.LastArguments.Should().ContainInOrder("Url", "Url");
        }

        [Fact]
        public async Task GIVEN_TemplateUrl_WHEN_RegisterMagnetHandlerInvoked_THEN_ReturnsResult()
        {
            var expected = new MagnetRegistrationResult
            {
                Status = "Status",
                Message = "Message"
            };
            _target.EnqueueResult(expected);

            var result = await _target.RegisterMagnetHandler("TemplateUrl", "HandlerName");

            result.Should().BeSameAs(expected);
            _target.LastIdentifier.Should().Be("qbt.registerMagnetHandler");
            _target.LastArguments.Should().ContainInOrder("TemplateUrl", "HandlerName");
        }

        [Fact]
        public async Task GIVEN_NotificationSupport_WHEN_IsNotificationsSupportedInvoked_THEN_ReturnsValue()
        {
            _target.EnqueueResult(true);

            var result = await _target.IsNotificationsSupported(TestContext.Current.CancellationToken);

            result.Should().BeTrue();
            _target.LastIdentifier.Should().Be("qbt.isNotificationSupported");
        }

        [Fact]
        public async Task GIVEN_GrantedPermission_WHEN_GetNotificationPermissionInvoked_THEN_ReturnsGranted()
        {
            _target.EnqueueResult("granted");

            var result = await _target.GetNotificationPermission(TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Granted);
            _target.LastIdentifier.Should().Be("qbt.getNotificationPermission");
        }

        [Fact]
        public async Task GIVEN_UnsupportedPermission_WHEN_RequestNotificationPermissionInvoked_THEN_ReturnsUnsupported()
        {
            _target.EnqueueResult("unsupported");

            var result = await _target.RequestNotificationPermission(TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unsupported);
            _target.LastIdentifier.Should().Be("qbt.requestNotificationPermission");
        }

        [Fact]
        public async Task GIVEN_DeniedPermission_WHEN_GetNotificationPermissionInvoked_THEN_ReturnsDenied()
        {
            _target.EnqueueResult("denied");

            var result = await _target.GetNotificationPermission(TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Denied);
        }

        [Fact]
        public async Task GIVEN_DefaultPermissionWithWhitespaceAndCasing_WHEN_GetNotificationPermissionInvoked_THEN_ReturnsDefault()
        {
            _target.EnqueueResult("  DeFaUlT ");

            var result = await _target.GetNotificationPermission(TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_UnknownPermission_WHEN_RequestNotificationPermissionInvoked_THEN_ReturnsDefault()
        {
            _target.EnqueueResult("unknown");

            var result = await _target.RequestNotificationPermission(TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_NullPermission_WHEN_RequestNotificationPermissionInvoked_THEN_ReturnsDefault()
        {
            _target.EnqueueResult(null);

            var result = await _target.RequestNotificationPermission(TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_NotificationPayload_WHEN_ShowNotificationInvoked_THEN_InvokesRuntime()
        {
            await _target.ShowNotification("Title", "Body", TestContext.Current.CancellationToken);

            _target.LastIdentifier.Should().Be("qbt.showNotification");
            _target.LastArguments.Should().ContainInOrder("Title", "Body");
        }

        [Fact]
        public async Task GIVEN_StoragePrefix_WHEN_GetLocalStorageEntriesByPrefixInvoked_THEN_ReturnsEntries()
        {
            var expected = new[]
            {
                new BrowserStorageEntry("QbtMud.Key", "Value")
            };
            _target.EnqueueResult(expected);

            var result = await _target.GetLocalStorageEntriesByPrefix("QbtMud.", TestContext.Current.CancellationToken);

            result.Should().HaveCount(1);
            result[0].Key.Should().Be("QbtMud.Key");
            _target.LastIdentifier.Should().Be("qbt.getLocalStorageEntriesByPrefix");
            _target.LastArguments.Should().ContainSingle().Which.Should().Be("QbtMud.");
        }

        [Fact]
        public async Task GIVEN_StorageEntriesAreNull_WHEN_GetLocalStorageEntriesByPrefixInvoked_THEN_ReturnsEmptyCollection()
        {
            _target.EnqueueResult(null);

            var result = await _target.GetLocalStorageEntriesByPrefix("QbtMud.", TestContext.Current.CancellationToken);

            result.Should().BeEmpty();
            _target.LastIdentifier.Should().Be("qbt.getLocalStorageEntriesByPrefix");
            _target.LastArguments.Should().ContainSingle().Which.Should().Be("QbtMud.");
        }

        [Fact]
        public async Task GIVEN_Key_WHEN_RemoveLocalStorageEntryInvoked_THEN_InvokesRuntime()
        {
            await _target.RemoveLocalStorageEntry("QbtMud.Key", TestContext.Current.CancellationToken);

            _target.LastIdentifier.Should().Be("qbt.removeLocalStorageEntry");
            _target.LastArguments.Should().ContainSingle().Which.Should().Be("QbtMud.Key");
        }

        [Fact]
        public async Task GIVEN_Prefix_WHEN_ClearLocalStorageEntriesByPrefixInvoked_THEN_ReturnsRemovedCount()
        {
            _target.EnqueueResult(2);

            var removed = await _target.ClearLocalStorageEntriesByPrefix("QbtMud.", TestContext.Current.CancellationToken);

            removed.Should().Be(2);
            _target.LastIdentifier.Should().Be("qbt.clearLocalStorageEntriesByPrefix");
            _target.LastArguments.Should().ContainSingle().Which.Should().Be("QbtMud.");
        }

        [Fact]
        public async Task GIVEN_PiecesBarInput_WHEN_RenderPiecesBarInvoked_THEN_InvokesRuntime()
        {
            var pieces = new[] { 1, 2, 3 };

            await _target.RenderPiecesBar("Id", "Hash", pieces, "Download", "Have", "Border");

            _target.LastIdentifier.Should().Be("qbt.renderPiecesBar");
            _target.LastArguments.Should().ContainInOrder("Id", "Hash", pieces, "Download", "Have", "Border");
        }

        [Fact]
        public async Task GIVEN_Value_WHEN_WriteToClipboardInvoked_THEN_InvokesRuntime()
        {
            await _target.WriteToClipboard("Value");

            _target.LastIdentifier.Should().Be("qbt.copyTextToClipboard");
            _target.LastArguments.Should().ContainSingle().Which.Should().Be("Value");
        }

        [Fact]
        public async Task GIVEN_JsException_WHEN_WriteToClipboardInvoked_THEN_DoesNotThrow()
        {
            var runtime = new Mock<IJSRuntime>(MockBehavior.Strict);
            runtime
                .Setup(r => r.InvokeAsync<IJSVoidResult>("qbt.copyTextToClipboard", It.IsAny<object?[]?>()))
                .ThrowsAsync(new JSException("Failure"));

            await runtime.Object.WriteToClipboard("Value");

            runtime.Verify(r => r.InvokeAsync<IJSVoidResult>("qbt.copyTextToClipboard", It.IsAny<object?[]?>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearSelection_WHEN_Invoked_THEN_InvokesRuntime()
        {
            await _target.ClearSelection();

            _target.LastIdentifier.Should().Be("qbt.clearSelection");
        }

        [Fact]
        public async Task GIVEN_BootstrapTheme_WHEN_RemoveInvoked_THEN_InvokesRuntime()
        {
            await _target.RemoveBootstrapTheme();

            _target.LastIdentifier.Should().Be("qbt.removeBootstrapTheme");
        }
    }
}
