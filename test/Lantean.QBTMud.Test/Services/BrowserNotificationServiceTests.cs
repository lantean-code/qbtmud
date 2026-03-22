using AwesomeAssertions;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class BrowserNotificationServiceTests
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly BrowserNotificationService _target;

        public BrowserNotificationServiceTests()
        {
            _jsRuntime = Mock.Of<IJSRuntime>();
            _target = new BrowserNotificationService(_jsRuntime);
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
        public async Task GIVEN_JsRuntimeThrowsInvalidOperationException_WHEN_GetPermissionAsync_THEN_ShouldReturnUnsupported()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var result = await _target.GetPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unsupported);
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
        public async Task GIVEN_CurrentPermissionDenied_WHEN_RequestPermissionAsync_THEN_ShouldReturnDeniedWithoutInvokingRequest()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("denied");

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Denied);
            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsUnknownPermission_WHEN_RequestPermissionAsync_THEN_ShouldReturnDefault()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("unknown");

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsNullPermission_WHEN_RequestPermissionAsync_THEN_ShouldReturnDefault()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync((string?)null);

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Default);
        }

        [Fact]
        public async Task GIVEN_RequestPermissionCompletesAfterTimeoutWindow_WHEN_RequestPermissionAsync_THEN_ShouldAwaitBrowserResponse()
        {
            var taskCompletionSource = new TaskCompletionSource<string?>();
            using var timer = new Timer(_ => taskCompletionSource.TrySetResult("granted"), null, 400, Timeout.Infinite);

            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .Returns(new ValueTask<string?>(taskCompletionSource.Task));

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Granted);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeThrowsHttpRequestException_WHEN_RequestPermissionAsync_THEN_ShouldReturnUnsupported()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("default");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var result = await _target.RequestPermissionAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(BrowserNotificationPermission.Unsupported);
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

            await _target.ShowNotificationAsync("Title", "Body", Xunit.TestContext.Current.CancellationToken);

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

            var action = async () => await _target.ShowNotificationAsync("Title", "Body", Xunit.TestContext.Current.CancellationToken);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_NullReference_WHEN_SubscribePermissionChangesAsync_THEN_ShouldThrowArgumentNullException()
        {
            var action = async () => await _target.SubscribePermissionChangesAsync(null!, Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GIVEN_DotNetReference_WHEN_SubscribePermissionChangesAsync_THEN_ShouldReturnSubscriptionId()
        {
            var dotNetObjectReference = new object();
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<long>(
                    "qbt.subscribeNotificationPermission",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]>(args => MatchesArguments(args, dotNetObjectReference))))
                .ReturnsAsync(7);

            var result = await _target.SubscribePermissionChangesAsync(dotNetObjectReference, Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(7);
        }

        [Fact]
        public async Task GIVEN_SubscribeNeverCompletes_WHEN_SubscribePermissionChangesAsync_THEN_ShouldReturnZero()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<long>(
                    "qbt.subscribeNotificationPermission",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]>()))
                .Returns(new ValueTask<long>(new TaskCompletionSource<long>().Task));

            var result = await _target.SubscribePermissionChangesAsync(new object(), Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_SubscriptionIdNotPositive_WHEN_UnsubscribePermissionChangesAsync_THEN_ShouldSkipJsInterop()
        {
            await _target.UnsubscribePermissionChangesAsync(0, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SubscriptionIdPositive_WHEN_UnsubscribePermissionChangesAsync_THEN_ShouldInvokeJsInterop()
        {
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<object?>(
                    "qbt.unsubscribeNotificationPermission",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]>(args => MatchesArguments(args, 7L))))
                .ReturnsAsync((object?)null);

            await _target.UnsubscribePermissionChangesAsync(7, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_jsRuntime).Verify(
                runtime => runtime.InvokeAsync<object?>(
                    "qbt.unsubscribeNotificationPermission",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]>(args => MatchesArguments(args, 7L))),
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
