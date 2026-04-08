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
        [Fact]
        public async Task GIVEN_FailedApiResultWithUserMessage_WHEN_HandlingFailure_THEN_ShouldShowUserMessage()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null)
                .Verifiable();

            await target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            snackbar.Verify();
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithoutUserMessage_WHEN_HandlingFailure_THEN_ShouldShowLocalizedDefaultMessage()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, null));

            languageLocalizer
                .Setup(localizer => localizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.", It.IsAny<object[]>()))
                .Returns("DefaultMessage")
                .Verifiable();
            snackbar
                .Setup(service => service.Add("DefaultMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null)
                .Verifiable();

            await target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            languageLocalizer.Verify();
            snackbar.Verify();
        }

        [Fact]
        public async Task GIVEN_FailedConnectivityApiResult_WHEN_HandlingFailure_THEN_ShouldMarkLostConnection()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.NoResponse, "UserMessage"));

            lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            await target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            lostConnectionWorkflow.Verify();
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_HandlingIfFailure_THEN_ShouldReturnFalseWithoutFeedback()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);

            var handled = await target.HandleIfFailureAsync(ApiResult.Success(), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            handled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_FailedApiResultWithUserMessage_WHEN_HandlingIfFailure_THEN_ShouldReturnTrueAndShowUserMessage()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null)
                .Verifiable();

            var handled = await target.HandleIfFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            handled.Should().BeTrue();
            snackbar.Verify();
        }

        [Fact]
        public async Task GIVEN_FailedConnectivityApiResult_WHEN_HandlingIfFailure_THEN_ShouldReturnTrueAndMarkLostConnection()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.NoResponse, "UserMessage"));

            lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            var handled = await target.HandleIfFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            handled.Should().BeTrue();
            lostConnectionWorkflow.Verify();
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultWithUserMessage_WHEN_HandlingFailure_THEN_ShouldShowUserMessage()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            snackbar
                .Setup(service => service.Add("UserMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null)
                .Verifiable();

            await target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            snackbar.Verify();
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultWithoutUserMessage_WHEN_HandlingFailure_THEN_ShouldShowLocalizedDefaultMessage()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.ServerError, null));

            languageLocalizer
                .Setup(localizer => localizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.", It.IsAny<object[]>()))
                .Returns("DefaultMessage")
                .Verifiable();
            snackbar
                .Setup(service => service.Add("DefaultMessage", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null)
                .Verifiable();

            await target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            languageLocalizer.Verify();
            snackbar.Verify();
        }

        [Fact]
        public async Task GIVEN_FailedGenericConnectivityApiResult_WHEN_HandlingFailure_THEN_ShouldMarkLostConnection()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.Timeout, "UserMessage"));

            lostConnectionWorkflow
                .Setup(workflow => workflow.MarkLostConnectionAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            await target.HandleFailureAsync(result, cancellationToken: Xunit.TestContext.Current.CancellationToken);

            lostConnectionWorkflow.Verify();
        }

        [Fact]
        public async Task GIVEN_SuccessfulApiResult_WHEN_HandlingFailure_THEN_ShouldThrowInvalidOperationException()
        {
            var target = CreateTarget(
                new Mock<ILostConnectionWorkflow>(MockBehavior.Strict),
                new SnackbarWorkflow(Mock.Of<ILanguageLocalizer>(), Mock.Of<ISnackbar>()),
                new Mock<ILanguageLocalizer>(MockBehavior.Strict));

            Func<Task> action = async () => await target.HandleFailureAsync(ApiResult.Success(), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_SuccessfulGenericApiResult_WHEN_HandlingFailure_THEN_ShouldThrowInvalidOperationException()
        {
            var target = CreateTarget(
                new Mock<ILostConnectionWorkflow>(MockBehavior.Strict),
                new SnackbarWorkflow(Mock.Of<ILanguageLocalizer>(), Mock.Of<ISnackbar>()),
                new Mock<ILanguageLocalizer>(MockBehavior.Strict));

            Func<Task> action = async () => await target.HandleFailureAsync(ApiResult<string>.Success("Value"), cancellationToken: Xunit.TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_FailedApiResultAndCustomMessage_WHEN_HandlingFailure_THEN_ShouldUseCustomMessageAndSeverity()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            snackbar
                .Setup(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null)
                .Verifiable();

            await target.HandleFailureAsync(result, message => $"Custom {message}", Severity.Warning, Xunit.TestContext.Current.CancellationToken);

            snackbar.Verify();
        }

        [Fact]
        public async Task GIVEN_FailedGenericApiResultAndCustomMessage_WHEN_HandlingFailure_THEN_ShouldUseCustomMessageAndSeverity()
        {
            var lostConnectionWorkflow = new Mock<ILostConnectionWorkflow>(MockBehavior.Strict);
            var languageLocalizer = new Mock<ILanguageLocalizer>(MockBehavior.Strict);
            var snackbar = new Mock<ISnackbar>(MockBehavior.Strict);
            var snackbarWorkflow = new SnackbarWorkflow(languageLocalizer.Object, snackbar.Object);
            var target = CreateTarget(lostConnectionWorkflow, snackbarWorkflow, languageLocalizer);
            var result = ApiResult<string>.FailureResult(CreateFailure(ApiFailureKind.ServerError, "UserMessage"));

            snackbar
                .Setup(service => service.Add("Custom UserMessage", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Returns((Snackbar?)null)
                .Verifiable();

            await target.HandleFailureAsync(result, message => $"Custom {message}", Severity.Warning, Xunit.TestContext.Current.CancellationToken);

            snackbar.Verify();
        }

        private static ApiFeedbackWorkflow CreateTarget(
            Mock<ILostConnectionWorkflow> lostConnectionWorkflow,
            ISnackbarWorkflow snackbarWorkflow,
            Mock<ILanguageLocalizer> languageLocalizer)
        {
            return new ApiFeedbackWorkflow(lostConnectionWorkflow.Object, snackbarWorkflow, languageLocalizer.Object);
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
