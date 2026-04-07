using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Services.Localization;
using MudBlazor;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IStartupExperienceWorkflow"/>.
    /// </summary>
    public sealed class StartupExperienceWorkflow : IStartupExperienceWorkflow
    {
        private readonly IWelcomeWizardPlanBuilder _welcomeWizardPlanBuilder;
        private readonly IWelcomeWizardStateService _welcomeWizardStateService;
        private readonly IDialogService _dialogService;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly IAppUpdateService _appUpdateService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ISnackbarWorkflow _snackbarWorkflow;
        private bool _updateSnackbarShown;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupExperienceWorkflow"/> class.
        /// </summary>
        /// <param name="welcomeWizardPlanBuilder">The welcome wizard plan builder.</param>
        /// <param name="welcomeWizardStateService">The welcome wizard state service.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="languageLocalizer">The language localizer.</param>
        /// <param name="appUpdateService">The app update service.</param>
        /// <param name="appSettingsService">The app settings service.</param>
        /// <param name="snackbarWorkflow">The snackbar workflow.</param>
        public StartupExperienceWorkflow(
            IWelcomeWizardPlanBuilder welcomeWizardPlanBuilder,
            IWelcomeWizardStateService welcomeWizardStateService,
            IDialogService dialogService,
            ILanguageLocalizer languageLocalizer,
            IAppUpdateService appUpdateService,
            IAppSettingsService appSettingsService,
            ISnackbarWorkflow snackbarWorkflow)
        {
            _welcomeWizardPlanBuilder = welcomeWizardPlanBuilder;
            _welcomeWizardStateService = welcomeWizardStateService;
            _dialogService = dialogService;
            _languageLocalizer = languageLocalizer;
            _appUpdateService = appUpdateService;
            _appSettingsService = appSettingsService;
            _snackbarWorkflow = snackbarWorkflow;
        }

        /// <inheritdoc />
        public async Task<bool> RunWelcomeWizardAsync(string? initialLocale, bool useFullScreenDialog, CancellationToken cancellationToken = default)
        {
            var plan = await _welcomeWizardPlanBuilder.BuildPlanAsync(cancellationToken);
            if (!plan.ShouldShowWizard)
            {
                return true;
            }

            await _welcomeWizardStateService.MarkShownAsync(cancellationToken);

            var parameters = new DialogParameters
            {
                { nameof(WelcomeWizardDialog.InitialLocale), initialLocale },
                { nameof(WelcomeWizardDialog.PendingStepIds), plan.PendingSteps.Select(step => step.Id).ToArray() },
                { nameof(WelcomeWizardDialog.ShowWelcomeBackIntro), plan.IsReturningUser }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
                BackdropClick = false,
                NoHeader = false,
                FullWidth = true,
                FullScreen = useFullScreenDialog,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "background-blur background-blur-strong"
            };

            var title = _languageLocalizer.Translate("AppWelcomeWizard", plan.IsReturningUser ? "Welcome back" : "Welcome");
            var dialogReference = await _dialogService.ShowAsync<WelcomeWizardDialog>(title, parameters, options);
            var dialogResult = await dialogReference.Result;

            return dialogResult is { Canceled: false };
        }

        /// <inheritdoc />
        public async Task RunUpdateCheckAsync(bool updateChecksEnabled, string? dismissedReleaseTag, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!updateChecksEnabled)
                {
                    return;
                }

                var status = await _appUpdateService.GetUpdateStatusAsync(cancellationToken: cancellationToken);
                var latestTag = status.LatestRelease?.TagName;
                if (!status.IsUpdateAvailable || string.IsNullOrWhiteSpace(latestTag))
                {
                    return;
                }

                if (string.Equals(dismissedReleaseTag, latestTag, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (_updateSnackbarShown)
                {
                    return;
                }

                _updateSnackbarShown = true;

                _snackbarWorkflow.ShowActionMessage(
                    _languageLocalizer.Translate("AppUpdates", "A new qbtmud build (%1) is available.", latestTag),
                    Severity.Info,
                    _languageLocalizer.Translate("AppUpdates", "Dismiss"),
                    async _ =>
                    {
                        await _appSettingsService.SaveDismissedReleaseTagAsync(latestTag, cancellationToken);
                    },
                    key: $"qbtmud-update-{latestTag}");
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
            }
            catch (Exception)
            {
            }
        }
    }
}
