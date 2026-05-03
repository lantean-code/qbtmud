using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Presentation.Test.Services
{
    public sealed class ApiFeedbackWorkflowTests
    {
        private readonly Mock<ILostConnectionWorkflow> _lostConnectionWorkflow;
        private readonly Mock<ILanguageLocalizer> _languageLocalizer;
        private readonly Mock<ISnackbar> _snackbar;
        private readonly TestNavigationManager _navigationManager;
        private readonly ApiFeedbackWorkflow _target;

        public ApiFeedbackWorkflowTests()
        {
            _lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            _languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            _snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            _navigationManager = new TestNavigationManager();

            var snackbarWorkflow = new SnackbarWorkflow(_languageLocalizer.Object, _snackbar.Object);
            _target = new ApiFeedbackWorkflow(_lostConnectionWorkflow.Object, snackbarWorkflow, _languageLocalizer.Object, _navigationManager);
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithUserMessage_WHEN_HandlingFailure_THEN_ShouldShowUserMessage()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _snackbar.Verify(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithoutUserMessage_WHEN_HandlingFailure_THEN_ShouldShowLocalizedDefaultMessage()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, null));

            _languageLocalizer
                .Setup(localizer => localizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.", It.IsAny<object[]>()))
                .Returns("DefaultMessage");
            _snackbar
                .Setup(service => service.Add("DefaultMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _languageLocalizer.Verify(localizer => localizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.", It.IsAny<object[]>()), Times.Once);
            _snackbar.Verify(service => service.Add("DefaultMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedConnectivityApiResult_WHEN_HandlingFailure_THEN_ShouldMarkLostConnection()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.NoResponse, "UserMessage"));

            _lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _lostConnectionWorkflow.Verify(workflow => workflow.MarkLostConnectionAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AuthenticationFailure_WHEN_HandlingFailure_THEN_ShouldNavigateToLogin()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.AuthenticationRequired, "UserMessage"));

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _navigationManager.Uri.Should().EndWith("/login");
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_ProcessingResult_THEN_ShouldReturnTrueWithoutFeedback()
        {
            var succeeded = await _target.ProcessResultAsync(ApiResult.CreateSuccess(), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            succeeded.Should().BeTrue();
            _lostConnectionWorkflow.VerifyNoOtherCalls();
            _languageLocalizer.VerifyNoOtherCalls();
            _snackbar.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithUserMessage_WHEN_ProcessingResult_THEN_ShouldReturnFalseAndShowUserMessage()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            var succeeded = await _target.ProcessResultAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            succeeded.Should().BeFalse();
            _snackbar.Verify(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedConnectivityApiResult_WHEN_ProcessingResult_THEN_ShouldReturnFalseAndMarkLostConnection()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.NoResponse, "UserMessage"));

            _lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask);

            var succeeded = await _target.ProcessResultAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            succeeded.Should().BeFalse();
            _lostConnectionWorkflow.Verify(workflow => workflow.MarkLostConnectionAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultWithUserMessage_WHEN_HandlingFailure_THEN_ShouldShowUserMessage()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _snackbar.Verify(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultWithoutUserMessage_WHEN_HandlingFailure_THEN_ShouldShowLocalizedDefaultMessage()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, null));

            _languageLocalizer
                .Setup(localizer => localizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.", It.IsAny<object[]>()))
                .Returns("DefaultMessage");
            _snackbar
                .Setup(service => service.Add("DefaultMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _languageLocalizer.Verify(localizer => localizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.", It.IsAny<object[]>()), Times.Once);
            _snackbar.Verify(service => service.Add("DefaultMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedGenericConnectivityApiResult_WHEN_HandlingFailure_THEN_ShouldMarkLostConnection()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.Timeout, "UserMessage"));

            _lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _lostConnectionWorkflow.Verify(workflow => workflow.MarkLostConnectionAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_HandlingFailure_THEN_ShouldThrowInvalidOperationException()
        {
            Func<Task> action = async () => await _target.HandleFailureAsync(ApiResult.CreateSuccess(), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_SuccessfulGenericApiResult_WHEN_HandlingFailure_THEN_ShouldThrowInvalidOperationException()
        {
            Func<Task> action = async () => await _target.HandleFailureAsync(ApiResult.CreateSuccess("Value"), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_FailedApiResultAndCustomMessage_WHEN_HandlingFailure_THEN_ShouldUseCustomMessageAndSeverity()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, message => $"Custom {message}", Severity.Warning, Xunit.TestContext.Current.CancellationToken);

            _snackbar.Verify(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_HandlingFailureWithCustomCallback_THEN_ShouldThrowInvalidOperationException()
        {
            Func<Task> action = async () => await _target.HandleFailureAsync(
                ApiResult.CreateSuccess(),
                (_, _) => Task.FromResult(ApiFeedbackCustomFailureResult.StopHandling),
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_HandlingFailureWithSyncCustomCallback_THEN_ShouldThrowInvalidOperationException()
        {
            Func<Task> action = async () => await _target.HandleFailureAsync(
                ApiResult.CreateSuccess(),
                _ => ApiFeedbackCustomFailureResult.StopHandling,
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_NonStandardFailureHandledBySyncCustomCallback_WHEN_HandlingFailure_THEN_ShouldNotShowGenericFeedback()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.NotFound, "UserMessage"));

            await _target.HandleFailureAsync(
                result,
                _ => ApiFeedbackCustomFailureResult.StopHandling,
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _lostConnectionWorkflow.VerifyNoOtherCalls();
            _languageLocalizer.VerifyNoOtherCalls();
            _snackbar.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GIVEN_AuthenticationFailure_WHEN_HandlingFailureWithSyncCustomCallback_THEN_ShouldInvokeCustomCleanupAndSharedHandling()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.AuthenticationRequired, "UserMessage"));
            var callbackInvoked = false;

            await _target.HandleFailureAsync(
                result,
                failure =>
                {
                    callbackInvoked = failure.Kind == ApiFailureKind.AuthenticationRequired;
                    return ApiFeedbackCustomFailureResult.ContinueWithWorkflow;
                },
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            callbackInvoked.Should().BeTrue();
            _navigationManager.Uri.Should().EndWith("/login");
        }

        [Fact]
        public async Task GIVEN_NonStandardFailureHandledByCustomCallback_WHEN_HandlingFailureWithCustomCallback_THEN_ShouldNotShowGenericFeedback()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.NotFound, "UserMessage"));
            var customHandler = new Mock<Func<ApiFailure, CancellationToken, Task<ApiFeedbackCustomFailureResult>>>(MockBehavior.Strict);
            customHandler
                .Setup(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.NotFound), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiFeedbackCustomFailureResult.StopHandling);

            await _target.HandleFailureAsync(
                result,
                customHandler.Object,
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            customHandler.Verify(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.NotFound), It.IsAny<CancellationToken>()), Times.Once);
            _lostConnectionWorkflow.VerifyNoOtherCalls();
            _languageLocalizer.VerifyNoOtherCalls();
            _snackbar.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GIVEN_NonStandardFailureNotHandledByCustomCallback_WHEN_HandlingFailureWithCustomCallback_THEN_ShouldFallBackToGenericFeedback()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));
            var customHandler = new Mock<Func<ApiFailure, CancellationToken, Task<ApiFeedbackCustomFailureResult>>>(MockBehavior.Strict);
            customHandler
                .Setup(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.ServerError), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiFeedbackCustomFailureResult.ContinueWithWorkflow);
            _snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(
                result,
                customHandler.Object,
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            customHandler.Verify(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.ServerError), It.IsAny<CancellationToken>()), Times.Once);
            _snackbar.Verify(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AuthenticationFailure_WHEN_HandlingFailureWithCustomCallback_THEN_ShouldInvokeCustomCallbackBeforeSharedHandling()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.AuthenticationRequired, "UserMessage"));
            var customHandler = new Mock<Func<ApiFailure, CancellationToken, Task<ApiFeedbackCustomFailureResult>>>(MockBehavior.Strict);
            customHandler
                .Setup(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.AuthenticationRequired), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiFeedbackCustomFailureResult.ContinueWithWorkflow);

            await _target.HandleFailureAsync(
                result,
                customHandler.Object,
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            customHandler.Verify(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.AuthenticationRequired), It.IsAny<CancellationToken>()), Times.Once);
            _navigationManager.Uri.Should().EndWith("/login");
        }

        [Fact]
        public async Task GIVEN_AuthenticationFailureHandledByCustomCallback_WHEN_HandlingFailureWithCustomCallback_THEN_ShouldStopBeforeSharedHandling()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.AuthenticationRequired, "UserMessage"));
            var customHandler = new Mock<Func<ApiFailure, CancellationToken, Task<ApiFeedbackCustomFailureResult>>>(MockBehavior.Strict);
            customHandler
                .Setup(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.AuthenticationRequired), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiFeedbackCustomFailureResult.StopHandling);

            await _target.HandleFailureAsync(
                result,
                customHandler.Object,
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

            customHandler.Verify(handler => handler(It.Is<ApiFailure>(failure => failure.Kind == ApiFailureKind.AuthenticationRequired), It.IsAny<CancellationToken>()), Times.Once);
            _navigationManager.Uri.Should().Be("http://localhost/");
            _lostConnectionWorkflow.VerifyNoOtherCalls();
            _languageLocalizer.VerifyNoOtherCalls();
            _snackbar.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultAndCustomMessage_WHEN_HandlingFailure_THEN_ShouldUseCustomMessageAndSeverity()
        {
            var result = ApiResult.CreateFailure(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, message => $"Custom {message}", Severity.Warning, Xunit.TestContext.Current.CancellationToken);

            _snackbar.Verify(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        private static ApiFailure CreateFailure(ApiFailureKind kind, string? userMessage)
        {
            return new ApiFailure
            {
                Detail = "Detail",
                IsTransient = true,
                Kind = kind,
                Operation = "Operation",
                ResponseBody = "ResponseBody",
                UserMessage = userMessage ?? string.Empty
            };
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
