using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ApiFeedbackWorkflowTests
    {
        private readonly Mock<ILostConnectionWorkflow> _lostConnectionWorkflow;
        private readonly Mock<ILanguageLocalizer> _languageLocalizer;
        private readonly Mock<ISnackbar> _snackbar;
        private readonly ApiFeedbackWorkflow _target;

        public ApiFeedbackWorkflowTests()
        {
            _lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            _languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            _snackbar = new Mock<ISnackbar>(MockBehavior.Strict);

            var snackbarWorkflow = new SnackbarWorkflow(_languageLocalizer.Object, _snackbar.Object);
            _target = new ApiFeedbackWorkflow(_lostConnectionWorkflow.Object, snackbarWorkflow, _languageLocalizer.Object);
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithUserMessage_WHEN_HandlingFailure_THEN_ShouldShowUserMessage()
        {
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _snackbar.Verify(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithoutUserMessage_WHEN_HandlingFailure_THEN_ShouldShowLocalizedDefaultMessage()
        {
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, null));

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
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.NoResponse, "UserMessage"));

            _lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _lostConnectionWorkflow.Verify(workflow => workflow.MarkLostConnectionAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_ProcessingResult_THEN_ShouldReturnTrueWithoutFeedback()
        {
            var succeeded = await _target.ProcessResultAsync(ApiResult.Success(), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            succeeded.Should().BeTrue();
            _lostConnectionWorkflow.VerifyNoOtherCalls();
            _languageLocalizer.VerifyNoOtherCalls();
            _snackbar.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithUserMessage_WHEN_ProcessingResult_THEN_ShouldReturnFalseAndShowUserMessage()
        {
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

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
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.NoResponse, "UserMessage"));

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
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _snackbar.Verify(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultWithoutUserMessage_WHEN_HandlingFailure_THEN_ShouldShowLocalizedDefaultMessage()
        {
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.ServerError, null));

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
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.Timeout, "UserMessage"));

            _lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask);

            await _target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            _lostConnectionWorkflow.Verify(workflow => workflow.MarkLostConnectionAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_HandlingFailure_THEN_ShouldThrowInvalidOperationException()
        {
            Func<Task> action = async () => await _target.HandleFailureAsync(ApiResult.Success(), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_SuccessfulGenericApiResult_WHEN_HandlingFailure_THEN_ShouldThrowInvalidOperationException()
        {
            Func<Task> action = async () => await _target.HandleFailureAsync(ApiResult<string>.Success("Value"), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_FailedApiResultAndCustomMessage_WHEN_HandlingFailure_THEN_ShouldUseCustomMessageAndSeverity()
        {
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            _snackbar
                .Setup(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null);

            await _target.HandleFailureAsync(result, message => $"Custom {message}", Severity.Warning, Xunit.TestContext.Current.CancellationToken);

            _snackbar.Verify(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultAndCustomMessage_WHEN_HandlingFailure_THEN_ShouldUseCustomMessageAndSeverity()
        {
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

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
    }
}
