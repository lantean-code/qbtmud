using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class StatusBarWorkflowTests
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>(MockBehavior.Strict);
        private readonly IDialogWorkflow _dialogWorkflow = Mock.Of<IDialogWorkflow>(MockBehavior.Strict);
        private readonly ISnackbar _snackbar = Mock.Of<ISnackbar>(MockBehavior.Loose);
        private readonly ILostConnectionWorkflow _lostConnectionWorkflow = Mock.Of<ILostConnectionWorkflow>();
        private readonly ILanguageLocalizer _languageLocalizer = Mock.Of<ILanguageLocalizer>();
        private readonly TestNavigationManager _navigationManager = new();
        private readonly ISnackbarWorkflow _snackbarWorkflow;
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;

        public StatusBarWorkflowTests()
        {
            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] arguments) => FormatLocalizedString(source, arguments));
            _snackbarWorkflow = new SnackbarWorkflow(_languageLocalizer, _snackbar);
            _apiFeedbackWorkflow = new ApiFeedbackWorkflow(_lostConnectionWorkflow, _snackbarWorkflow, _languageLocalizer, _navigationManager);
        }

        [Fact]
        public async Task GIVEN_ToggleFails_WHEN_TogglingAlternativeSpeedLimits_THEN_ShouldShowErrorAndReturnNull()
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient>(ApiFailureKind.ServerError, "Failure");

            var result = await target.ToggleAlternativeSpeedLimitsAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to toggle alternative speed limits: Failure", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StateLookupFails_WHEN_TogglingAlternativeSpeedLimits_THEN_ShouldShowErrorAndReturnNull()
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);
            Mock.Get(_apiClient)
                .Setup(client => client.GetAlternativeSpeedLimitsStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, bool>(ApiFailureKind.ServerError, "LookupFailure");

            var result = await target.ToggleAlternativeSpeedLimitsAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to toggle alternative speed limits: LookupFailure", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Theory]
        [InlineData(true, "Alternative speed limits: On")]
        [InlineData(false, "Alternative speed limits: Off")]
        public async Task GIVEN_ToggleSucceeds_WHEN_TogglingAlternativeSpeedLimits_THEN_ShouldShowStatusAndReturnValue(bool isEnabled, string expectedMessage)
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);
            Mock.Get(_apiClient)
                .Setup(client => client.GetAlternativeSpeedLimitsStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(isEnabled);

            var result = await target.ToggleAlternativeSpeedLimitsAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(isEnabled);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(expectedMessage, Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_GlobalDownloadRateConfirmed_WHEN_ShowingDialog_THEN_ShouldReturnAppliedRate()
        {
            var target = CreateTarget();
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalDownloadRateDialog(2048))
                .ReturnsAsync(4096L);

            var result = await target.ShowGlobalDownloadRateLimitAsync(2048, Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(4096);
        }

        [Fact]
        public async Task GIVEN_GlobalDownloadRateCanceled_WHEN_ShowingDialog_THEN_ShouldReturnNull()
        {
            var target = CreateTarget();
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalDownloadRateDialog(2048))
                .ReturnsAsync((long?)null);

            var result = await target.ShowGlobalDownloadRateLimitAsync(2048, Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_GlobalDownloadRateDialogFails_WHEN_ShowingDialog_THEN_ShouldShowErrorAndReturnNull()
        {
            var target = CreateTarget();
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalDownloadRateDialog(2048))
                .ThrowsAsync(new HttpRequestException("DownloadFailure"));

            var result = await target.ShowGlobalDownloadRateLimitAsync(2048, Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to set global download rate limit: DownloadFailure", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_GlobalUploadRateConfirmed_WHEN_ShowingDialog_THEN_ShouldReturnAppliedRate()
        {
            var target = CreateTarget();
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalUploadRateDialog(1024))
                .ReturnsAsync(3072L);

            var result = await target.ShowGlobalUploadRateLimitAsync(1024, Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(3072);
        }

        [Fact]
        public async Task GIVEN_GlobalUploadRateCanceled_WHEN_ShowingDialog_THEN_ShouldReturnNull()
        {
            var target = CreateTarget();
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalUploadRateDialog(1024))
                .ReturnsAsync((long?)null);

            var result = await target.ShowGlobalUploadRateLimitAsync(1024, Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_GlobalUploadRateDialogFails_WHEN_ShowingDialog_THEN_ShouldShowErrorAndReturnNull()
        {
            var target = CreateTarget();
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalUploadRateDialog(1024))
                .ThrowsAsync(new HttpRequestException("UploadFailure"));

            var result = await target.ShowGlobalUploadRateLimitAsync(1024, Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to set global upload rate limit: UploadFailure", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        private StatusBarWorkflow CreateTarget()
        {
            return new StatusBarWorkflow(_apiClient, _dialogWorkflow, _snackbarWorkflow, _languageLocalizer, _apiFeedbackWorkflow);
        }

        private static string FormatLocalizedString(string source, object[] arguments)
        {
            if (arguments.Length == 0)
            {
                return source;
            }

            var result = source;
            for (var i = 0; i < arguments.Length; i++)
            {
                result = result.Replace($"%{i + 1}", arguments[i]?.ToString(), StringComparison.Ordinal);
            }

            return result;
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
                NotifyLocationChanged(false);
            }
        }
    }
}
