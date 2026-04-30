using AwesomeAssertions;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class StartupExperienceWorkflowTests
    {
        private readonly IWelcomeWizardPlanBuilder _welcomeWizardPlanBuilder = Mock.Of<IWelcomeWizardPlanBuilder>(MockBehavior.Strict);
        private readonly IWelcomeWizardStateService _welcomeWizardStateService = Mock.Of<IWelcomeWizardStateService>(MockBehavior.Strict);
        private readonly IDialogService _dialogService = Mock.Of<IDialogService>(MockBehavior.Strict);
        private readonly ILanguageLocalizer _languageLocalizer = Mock.Of<ILanguageLocalizer>();
        private readonly IAppUpdateService _appUpdateService = Mock.Of<IAppUpdateService>(MockBehavior.Strict);
        private readonly IAppSettingsService _appSettingsService = Mock.Of<IAppSettingsService>(MockBehavior.Strict);
        private readonly ISnackbar _snackbar = Mock.Of<ISnackbar>(MockBehavior.Loose);
        private readonly ISnackbarWorkflow _snackbarWorkflow;

        public StartupExperienceWorkflowTests()
        {
            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] arguments) => FormatLocalizedString(source, arguments));
            _snackbarWorkflow = new SnackbarWorkflow(_languageLocalizer, _snackbar);
        }

        [Fact]
        public async Task GIVEN_NoPendingWizardSteps_WHEN_RunningWelcomeWizard_THEN_ShouldReturnTrueWithoutShowingDialog()
        {
            var target = CreateTarget();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(builder => builder.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(WelcomeWizardPlan.Empty);

            var result = await target.RunWelcomeWizardAsync("en", true, Xunit.TestContext.Current.CancellationToken);

            result.Should().BeTrue();
            Mock.Get(_welcomeWizardStateService)
                .Verify(service => service.MarkShownAsync(It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(_dialogService)
                .Verify(service => service.ShowAsync<WelcomeWizardDialog>(It.IsAny<string?>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_PendingWizardSteps_WHEN_RunningWelcomeWizard_THEN_ShouldMarkShownAndPassDialogParameters()
        {
            var target = CreateTarget();
            var plan = new WelcomeWizardPlan(
                true,
                [new WelcomeWizardStepDefinition("language", 0)]);
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(builder => builder.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);
            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.MarkShownAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardState());

            DialogParameters? capturedParameters = null;
            DialogOptions? capturedOptions = null;
            Mock.Get(_dialogService)
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(It.IsAny<string?>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions?>()))
                .Callback<string?, DialogParameters, DialogOptions?>((_, parameters, options) =>
                {
                    capturedParameters = parameters;
                    capturedOptions = options;
                })
                .ReturnsAsync(CreateDialogReference(DialogResult.Ok(true)));

            var result = await target.RunWelcomeWizardAsync("en", true, Xunit.TestContext.Current.CancellationToken);

            result.Should().BeTrue();
            Mock.Get(_welcomeWizardStateService)
                .Verify(service => service.MarkShownAsync(It.IsAny<CancellationToken>()), Times.Once);
            capturedParameters.Should().NotBeNull();
            capturedParameters!.Get<string?>(nameof(WelcomeWizardDialog.InitialLocale)).Should().Be("en");
            capturedParameters.Get<string[]>(nameof(WelcomeWizardDialog.PendingStepIds)).Should().BeEquivalentTo(["language"]);
            capturedParameters.Get<bool>(nameof(WelcomeWizardDialog.ShowWelcomeBackIntro)).Should().BeTrue();
            capturedOptions.Should().NotBeNull();
            capturedOptions!.FullScreen.Should().BeTrue();
            capturedOptions.FullWidth.Should().BeTrue();
            capturedOptions.BackdropClick.Should().BeFalse();
            capturedOptions.CloseOnEscapeKey.Should().BeFalse();
            capturedOptions.MaxWidth.Should().Be(MaxWidth.Medium);
        }

        [Fact]
        public async Task GIVEN_WizardDialogCanceled_WHEN_RunningWelcomeWizard_THEN_ShouldReturnFalse()
        {
            var target = CreateTarget();
            var plan = new WelcomeWizardPlan(false, [new WelcomeWizardStepDefinition("theme", 1)]);
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(builder => builder.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(plan);
            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.MarkShownAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardState());
            Mock.Get(_dialogService)
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(It.IsAny<string?>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions?>()))
                .ReturnsAsync(CreateDialogReference(DialogResult.Cancel()));

            var result = await target.RunWelcomeWizardAsync(null, false, Xunit.TestContext.Current.CancellationToken);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_UpdateChecksDisabled_WHEN_RunningUpdateCheck_THEN_ShouldNotQueryUpdateService()
        {
            var target = CreateTarget();

            await target.RunUpdateCheckAsync(false, null, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_appUpdateService)
                .Verify(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoAvailableUpdate_WHEN_RunningUpdateCheck_THEN_ShouldNotShowSnackbar()
        {
            var target = CreateTarget();
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    latestRelease: null,
                    isUpdateAvailable: false,
                    canCompareVersions: true,
                    checkedAtUtc: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

            await target.RunUpdateCheckAsync(true, null, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_DismissedUpdateTagMatches_WHEN_RunningUpdateCheck_THEN_ShouldNotShowSnackbar()
        {
            var target = CreateTarget();
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", "https://example.invalid", new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                    isUpdateAvailable: true,
                    canCompareVersions: true,
                    checkedAtUtc: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

            await target.RunUpdateCheckAsync(true, "v1.1.0", Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_UpdateAvailable_WHEN_RunningUpdateCheckTwice_THEN_ShouldOnlyShowSnackbarOnceAndPersistDismissal()
        {
            var target = CreateTarget();
            Action<SnackbarOptions>? capturedOptions = null;
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", "https://example.invalid", new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                    isUpdateAvailable: true,
                    canCompareVersions: true,
                    checkedAtUtc: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Callback<string, Severity, Action<SnackbarOptions>, string>((_, _, configure, _) => capturedOptions = configure);
            Mock.Get(_appSettingsService)
                .Setup(service => service.SaveDismissedReleaseTagAsync("v1.1.0", It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());

            await target.RunUpdateCheckAsync(true, null, Xunit.TestContext.Current.CancellationToken);
            await target.RunUpdateCheckAsync(true, null, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("A new qbtmud build (v1.1.0) is available.", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), "qbtmud-update-v1.1.0"),
                Times.Once);

            capturedOptions.Should().NotBeNull();
            var options = new SnackbarOptions(Severity.Info, new SnackbarConfiguration());
            capturedOptions!(options);
            options.Action.Should().Be("Dismiss");
            options.RequireInteraction.Should().BeTrue();
            options.OnClick.Should().NotBeNull();
            await options.OnClick!(null!);

            Mock.Get(_appSettingsService)
                .Verify(service => service.SaveDismissedReleaseTagAsync("v1.1.0", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_UpdateStatusThrows_WHEN_RunningUpdateCheck_THEN_ShouldSwallowException()
        {
            var target = CreateTarget();
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var action = async () => await target.RunUpdateCheckAsync(true, null, Xunit.TestContext.Current.CancellationToken);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_UpdateStatusCanceled_WHEN_RunningUpdateCheck_THEN_ShouldSwallowCancellation()
        {
            var target = CreateTarget();
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((bool _, CancellationToken cancellationToken) => Task.FromCanceled<AppUpdateStatus>(cancellationToken));

            var action = async () => await target.RunUpdateCheckAsync(true, null, new CancellationToken(true));

            await action.Should().NotThrowAsync();
        }

        private StartupExperienceWorkflow CreateTarget()
        {
            return new StartupExperienceWorkflow(
                _welcomeWizardPlanBuilder,
                _welcomeWizardStateService,
                _dialogService,
                _languageLocalizer,
                _appUpdateService,
                _appSettingsService,
                _snackbarWorkflow);
        }

        private static IDialogReference CreateDialogReference(DialogResult? result)
        {
            return CreateDialogReference(Task.FromResult(result));
        }

        private static IDialogReference CreateDialogReference(Task<DialogResult?> resultTask)
        {
            var reference = new Mock<IDialogReference>(MockBehavior.Strict);
            reference.SetupGet(dialog => dialog.Result).Returns(resultTask);
            return reference.Object;
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
    }
}
