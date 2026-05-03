using AwesomeAssertions;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Services;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Services
{
    public sealed class LostConnectionWorkflowTests
    {
        [Fact]
        public async Task GIVEN_NoActiveDialog_WHEN_TriggerInvoked_THEN_ShouldShowLostConnectionDialog()
        {
            var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            dialogService
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.Is<string?>(title => title == null),
                    It.Is<DialogOptions>(options =>
                        options.CloseOnEscapeKey == false
                        && options.BackdropClick == false
                        && options.NoHeader == true
                        && options.FullWidth == true
                        && options.MaxWidth == MaxWidth.ExtraSmall
                        && options.BackgroundClass == "background-blur background-blur-strong")))
                .ReturnsAsync(dialogReference.Object);
            var target = new LostConnectionWorkflow(dialogService.Object, Mock.Of<ILogger<LostConnectionWorkflow>>());

            await target.MarkLostConnectionAsync();

            dialogService.Verify(
                service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_TriggerAlreadyInFlight_WHEN_TriggerInvokedAgain_THEN_ShouldReuseExistingRequest()
        {
            var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            var completionSource = new TaskCompletionSource<IDialogReference>();
            dialogService
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .Returns(completionSource.Task);
            var target = new LostConnectionWorkflow(dialogService.Object, Mock.Of<ILogger<LostConnectionWorkflow>>());

            var firstTrigger = target.MarkLostConnectionAsync();
            var secondTrigger = target.MarkLostConnectionAsync();

            secondTrigger.Should().NotBeNull();
            dialogService.Verify(
                service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()),
                Times.Once);

            completionSource.SetResult(dialogReference.Object);

            await firstTrigger;
        }

        [Fact]
        public async Task GIVEN_ShowDialogThrows_WHEN_TriggerInvoked_THEN_ShouldLogErrorAndAllowRetry()
        {
            var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<LostConnectionWorkflow>>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            var exception = new InvalidOperationException("boom");
            dialogReference.Setup(dialog => dialog.Close());
            dialogService
                .SetupSequence(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ThrowsAsync(exception)
                .ReturnsAsync(dialogReference.Object);
            logger
                .Setup(log => log.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            var target = new LostConnectionWorkflow(dialogService.Object, logger.Object);

            await target.MarkLostConnectionAsync();
            await target.MarkLostConnectionAsync();

            logger.Verify(log => log.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            dialogService.Verify(
                service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ConnectedState_WHEN_TriggerInvoked_THEN_ShouldMarkLostConnection()
        {
            var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            dialogService
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReference.Object);
            var target = new LostConnectionWorkflow(dialogService.Object, Mock.Of<ILogger<LostConnectionWorkflow>>());

            await target.MarkLostConnectionAsync();

            dialogService.Verify(
                service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ActiveDialog_WHEN_MarkLostConnectionInvokedAgain_THEN_ShouldNotShowAnotherDialog()
        {
            var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            dialogService
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReference.Object);
            var target = new LostConnectionWorkflow(dialogService.Object, Mock.Of<ILogger<LostConnectionWorkflow>>());

            await target.MarkLostConnectionAsync();
            await target.MarkLostConnectionAsync();

            dialogService.Verify(
                service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()),
                Times.Once);
        }
    }
}
