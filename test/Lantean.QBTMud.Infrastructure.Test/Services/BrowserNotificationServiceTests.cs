using AwesomeAssertions;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Infrastructure.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Infrastructure.Test.Services
{
    public sealed class BrowserNotificationServiceTests
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly BrowserNotificationService _target;

        public BrowserNotificationServiceTests()
        {
            _jsRuntime = Mock.Of<IJSRuntime>();
            _target = new BrowserNotificationService(_jsRuntime);

            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<long>("qbt.subscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync(1);
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync((object?)null);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsSupported_WHEN_IsSupportedAsync_THEN_ShouldReturnTrue()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<bool>("qbt.isNotificationSupported", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync(true);

            var result = await _target.IsSupportedAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsJsException_WHEN_IsSupportedAsync_THEN_ShouldReturnFalse()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<bool>("qbt.isNotificationSupported", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new JSException("Failure"));

            var result = await _target.IsSupportedAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsInvalidOperationException_WHEN_IsSupportedAsync_THEN_ShouldReturnFalse()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<bool>("qbt.isNotificationSupported", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var result = await _target.IsSupportedAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsHttpRequestException_WHEN_IsSupportedAsync_THEN_ShouldReturnFalse()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<bool>("qbt.isNotificationSupported", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var result = await _target.IsSupportedAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsGrantedPermission_WHEN_GetPermissionAsync_THEN_ShouldReturnGranted()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("granted");

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Granted);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsInsecurePermission_WHEN_GetPermissionAsync_THEN_ShouldReturnInsecure()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("insecure");

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Insecure);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsUnsupportedPermission_WHEN_GetPermissionAsync_THEN_ShouldReturnUnsupported()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("unsupported");

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unsupported);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsUnexpectedPermission_WHEN_GetPermissionAsync_THEN_ShouldReturnDefault()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("Unexpected");

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsDeniedPermission_WHEN_GetPermissionAsync_THEN_ShouldReturnDenied()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("denied");

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Denied);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsDefaultPermissionWithWhitespaceAndCasing_WHEN_GetPermissionAsync_THEN_ShouldReturnDefault()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("  DeFaUlT ");

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsInvalidOperationException_WHEN_GetPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_GetPermissionNeverCompletes_WHEN_GetPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .Returns(new ValueTask<string?>(new TaskCompletionSource<string?>().Task));

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsJsException_WHEN_GetPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new JSException("Failure"));

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsHttpRequestException_WHEN_GetPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsGrantedPermission_WHEN_RequestPermissionAsync_THEN_ShouldReturnGranted()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("granted");

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Granted);
        }

        [Fact]
        public async Task GIVEN_CachedPermissionDenied_WHEN_RequestPermissionAsync_THEN_ShouldReturnDeniedWithoutInvokingRequest()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("denied");

            _ = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Denied);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_CachedPermissionDeniedButFreshPermissionGranted_WHEN_RequestPermissionAsync_THEN_ShouldReturnGrantedWithoutInvokingRequest()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("denied")
                .ReturnsAsync("granted");

            _ = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Granted);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_CachedPermissionDefaultButFreshPermissionDenied_WHEN_RequestPermissionAsync_THEN_ShouldReturnDeniedWithoutInvokingRequest()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("denied");

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Denied);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_CachedPermissionDefaultButFreshPermissionUnknown_WHEN_RequestPermissionAsync_THEN_ShouldReturnUnknownWithoutInvokingRequest()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsUnknownPermission_WHEN_RequestPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("unknown");

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsNullPermission_WHEN_RequestPermissionAsync_THEN_ShouldReturnDefault()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync((string?)null);

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_RequestPermissionNeverCompletes_WHEN_RequestPermissionAsync_THEN_ShouldReturnDefault()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .Returns(new ValueTask<string?>(new TaskCompletionSource<string?>().Task));

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsHttpRequestException_WHEN_RequestPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsJsException_WHEN_RequestPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new JSException("Failure"));

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsInvalidOperationException_WHEN_RequestPermissionAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unknown);
        }

        [Fact]
        public async Task GIVEN_SubscriptionObject_WHEN_SubscribePermissionChangesAsync_THEN_ReturnsSubscriptionId()
        {
            var result = await _target.SubscribePermissionChangesAsync(new object(), Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_SubscriptionId_WHEN_UnsubscribePermissionChangesAsync_THEN_Unsubscribes()
        {
            await _target.UnsubscribePermissionChangesAsync(1, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeNotificationPermission", It.IsAny<CancellationToken>(), It.Is<object?[]>(args => MatchesArguments(args, 1L))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NonPositiveSubscriptionId_WHEN_UnsubscribePermissionChangesAsync_THEN_DoesNotInvokeJsInterop()
        {
            await _target.UnsubscribePermissionChangesAsync(0, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationPayload_WHEN_ShowNotificationAsync_THEN_ShouldInvokeJsInterop()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]>(args => MatchesArguments(args, "Title", "Body"))))
                .Returns(ValueTask.FromResult<IJSVoidResult>(default!));

            await _target.ShowNotificationAsync("Title", "Body", TestContext.Current.CancellationToken);

            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]>(args => MatchesArguments(args, "Title", "Body"))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ShowNotificationNeverCompletes_WHEN_ShowNotificationAsync_THEN_ShouldCompleteWithoutThrowing()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]>()))
                .Returns(new ValueTask<IJSVoidResult>(new TaskCompletionSource<IJSVoidResult>().Task));

            var action = async () => await _target.ShowNotificationAsync("Title", "Body", TestContext.Current.CancellationToken);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_GetPermissionCalledTwice_WHEN_FirstCallInitializesService_THEN_SecondCallUsesCachedPermission()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("granted");

            var firstPermission = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            var secondPermission = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            firstPermission.Should().Be(BrowserNotificationPermission.Granted);
            secondPermission.Should().Be(BrowserNotificationPermission.Granted);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Once);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<long>("qbt.subscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_InitialPermissionReadIsNonAuthoritative_WHEN_GetPermissionAsyncCalledAgain_THEN_ServiceRetriesPermissionRead()
        {
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new InvalidOperationException("Failure"))
                .ReturnsAsync("granted");

            var firstPermission = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            var secondPermission = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            firstPermission.Should().Be(BrowserNotificationPermission.Unknown);
            secondPermission.Should().Be(BrowserNotificationPermission.Granted);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Exactly(2));
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<long>("qbt.subscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SubscribeFailsDuringInitialization_WHEN_GetPermissionAsyncCalledAgain_THEN_SubscribeIsRetried()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("granted");
            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<long>("qbt.subscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync(0)
                .ReturnsAsync(0)
                .ReturnsAsync(0)
                .ReturnsAsync(7);

            _ = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            _ = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<long>("qbt.subscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Exactly(4));
        }

        [Fact]
        public async Task GIVEN_ServiceInitialized_WHEN_JsPermissionCallbackReceived_THEN_UpdatesCacheAndRaisesEvent()
        {
            var changedPermissions = new List<BrowserNotificationPermission>();
            _target.PermissionChanged += (_, args) => changedPermissions.Add(args.Permission);

            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default")
                .ReturnsAsync("granted");

            _ = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            await _target.OnNotificationPermissionChanged();
            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Granted);
            changedPermissions.Should().ContainSingle(permission => permission == BrowserNotificationPermission.Granted);
        }

        [Fact]
        public async Task GIVEN_ServiceInitialized_WHEN_JsPermissionRefreshFails_THEN_KeepsCachedPermissionWithoutRaisingEvent()
        {
            var changedPermissions = new List<BrowserNotificationPermission>();
            _target.PermissionChanged += (_, args) => changedPermissions.Add(args.Permission);

            Mock.Get(_jsRuntime)
                .SetupSequence(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("granted")
                .ThrowsAsync(new InvalidOperationException("Failure"));

            _ = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            await _target.OnNotificationPermissionChanged();
            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Granted);
            changedPermissions.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ServiceInitialized_WHEN_Disposed_THEN_UnsubscribesJsPermissionTracking()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("granted");

            _ = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);
            await _target.DisposeAsync();

            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Once);
        }

        private static bool MatchesArguments(object?[]? actualArguments, params object?[] expectedArguments)
        {
            if (actualArguments is null || actualArguments.Length != expectedArguments.Length)
            {
                return false;
            }

            for (var index = 0; index < actualArguments.Length; index++)
            {
                if (!Equals(actualArguments[index], expectedArguments[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
