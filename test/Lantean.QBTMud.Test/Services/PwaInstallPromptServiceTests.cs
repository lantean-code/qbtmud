using AwesomeAssertions;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Microsoft.JSInterop;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class PwaInstallPromptServiceTests
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly PwaInstallPromptService _target;

        public PwaInstallPromptServiceTests()
        {
            _jSRuntime = Mock.Of<IJSRuntime>();
            _target = new PwaInstallPromptService(_jSRuntime);
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsState_WHEN_GetInstallPromptStateAsync_THEN_ShouldReturnState()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<PwaInstallPromptState>("qbt.getInstallPromptState", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsInstalled = true,
                    CanPrompt = true,
                    IsIos = true,
                    IsPromptInProgress = true
                });

            var result = await _target.GetInstallPromptStateAsync(Xunit.TestContext.Current.CancellationToken);

            result.IsInstalled.Should().BeTrue();
            result.CanPrompt.Should().BeTrue();
            result.IsIos.Should().BeTrue();
            result.IsPromptInProgress.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_JsRuntimeReturnsNull_WHEN_GetInstallPromptStateAsync_THEN_ShouldReturnDefaultState()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<PwaInstallPromptState>("qbt.getInstallPromptState", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync((PwaInstallPromptState)null!);

            var result = await _target.GetInstallPromptStateAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.IsInstalled.Should().BeFalse();
            result.CanPrompt.Should().BeFalse();
            result.IsIos.Should().BeFalse();
            result.IsPromptInProgress.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NullReference_WHEN_SubscribeInstallPromptStateAsync_THEN_ShouldThrowArgumentNullException()
        {
            var action = async () => await _target.SubscribeInstallPromptStateAsync(null!, Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GIVEN_ReferenceProvided_WHEN_SubscribeInstallPromptStateAsync_THEN_ShouldReturnSubscriptionId()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<long>("qbt.subscribeInstallPromptState", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync(7);

            var result = await _target.SubscribeInstallPromptStateAsync(new object(), Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(7);
        }

        [Fact]
        public async Task GIVEN_SubscriptionIdNotPositive_WHEN_UnsubscribeInstallPromptStateAsync_THEN_ShouldSkipJsInterop()
        {
            await _target.UnsubscribeInstallPromptStateAsync(0, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_jSRuntime).Verify(
                runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeInstallPromptState", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SubscriptionIdPositive_WHEN_UnsubscribeInstallPromptStateAsync_THEN_ShouldInvokeJsInterop()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeInstallPromptState", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync((object?)null);

            await _target.UnsubscribeInstallPromptStateAsync(7, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_jSRuntime).Verify(
                runtime => runtime.InvokeAsync<object?>("qbt.unsubscribeInstallPromptState", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestInstallPromptReturnsOutcome_WHEN_RequestInstallPromptAsync_THEN_ShouldReturnOutcome()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<string>("qbt.requestInstallPrompt", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync("accepted");

            var result = await _target.RequestInstallPromptAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be("accepted");
        }

        [Fact]
        public async Task GIVEN_RequestInstallPromptReturnsNull_WHEN_RequestInstallPromptAsync_THEN_ShouldReturnUnknown()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<string>("qbt.requestInstallPrompt", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync((string)null!);

            var result = await _target.RequestInstallPromptAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be("unknown");
        }

        [Fact]
        public async Task GIVEN_TestInstallPromptStateReturned_WHEN_ShowInstallPromptTestAsync_THEN_ShouldReturnState()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<PwaInstallPromptState>("qbt.showInstallPromptTest", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsInstalled = false,
                    CanPrompt = true,
                    IsIos = false,
                    IsPromptInProgress = false
                });

            var result = await _target.ShowInstallPromptTestAsync(Xunit.TestContext.Current.CancellationToken);

            result.IsInstalled.Should().BeFalse();
            result.CanPrompt.Should().BeTrue();
            result.IsIos.Should().BeFalse();
            result.IsPromptInProgress.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_TestInstallPromptStateIsNull_WHEN_ShowInstallPromptTestAsync_THEN_ShouldReturnDefaultState()
        {
            Mock.Get(_jSRuntime)
                .Setup(runtime => runtime.InvokeAsync<PwaInstallPromptState>("qbt.showInstallPromptTest", It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .ReturnsAsync((PwaInstallPromptState)null!);

            var result = await _target.ShowInstallPromptTestAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.IsInstalled.Should().BeFalse();
            result.CanPrompt.Should().BeFalse();
            result.IsIos.Should().BeFalse();
            result.IsPromptInProgress.Should().BeFalse();
        }
    }
}
