using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class WelcomeWizardDialogTests : RazorComponentTestBase<WelcomeWizardDialog>
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>();
        private readonly IThemeManagerService _themeManagerService = Mock.Of<IThemeManagerService>();
        private readonly ISnackbar _snackbar = Mock.Of<ISnackbar>();
        private readonly ILanguageCatalog _languageCatalog = Mock.Of<ILanguageCatalog>();
        private readonly ILanguageInitializationService _languageInitializationService = Mock.Of<ILanguageInitializationService>();
        private readonly IAppSettingsService _appSettingsService = Mock.Of<IAppSettingsService>();
        private readonly IBrowserNotificationService _browserNotificationService = Mock.Of<IBrowserNotificationService>();
        private readonly IWelcomeWizardStateService _welcomeWizardStateService = Mock.Of<IWelcomeWizardStateService>();
        private readonly IKeyboardService _keyboardService;
        private readonly Mock<IKeyboardService> _keyboardServiceMock;
        private readonly TestNavigationManager _navigationManager;
        private readonly WelcomeWizardDialogTestDriver _target;

        public WelcomeWizardDialogTests()
        {
            _navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll<Microsoft.AspNetCore.Components.NavigationManager>();
            TestContext.Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(_navigationManager);

            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .Returns(Task.CompletedTask);

            var themeManagerServiceMock = Mock.Get(_themeManagerService);
            themeManagerServiceMock
                .Setup(service => service.EnsureInitialized())
                .Returns(Task.CompletedTask);
            themeManagerServiceMock
                .SetupGet(service => service.Themes)
                .Returns(new List<ThemeCatalogItem>
                {
                    CreateTheme("theme1", "Theme1"),
                    CreateTheme("theme2", "Theme2"),
                });
            themeManagerServiceMock
                .SetupGet(service => service.CurrentThemeId)
                .Returns("theme1");
            themeManagerServiceMock
                .Setup(service => service.ApplyTheme(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var languageCatalogMock = Mock.Get(_languageCatalog);
            languageCatalogMock
                .SetupGet(catalog => catalog.Languages)
                .Returns(new List<LanguageCatalogItem>
                {
                    new("en", "English"),
                    new("fr", "Francais"),
                });
            languageCatalogMock
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var languageInitializationServiceMock = Mock.Get(_languageInitializationService);
            languageInitializationServiceMock
                .Setup(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_appSettingsService)
                .Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppSettings settings, CancellationToken _) => settings.Clone());
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.SubscribePermissionChangesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.UnsubscribePermissionChangesAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.AcknowledgeStepsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardState());

            _keyboardService = Mock.Of<IKeyboardService>();
            _keyboardServiceMock = Mock.Get(_keyboardService);
            _keyboardServiceMock
                .Setup(service => service.Focus())
                .Returns(Task.CompletedTask);
            _keyboardServiceMock
                .Setup(service => service.UnFocus())
                .Returns(Task.CompletedTask);

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<IThemeManagerService>();
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.RemoveAll<ILanguageCatalog>();
            TestContext.Services.RemoveAll<ILanguageInitializationService>();
            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.RemoveAll<IAppSettingsService>();
            TestContext.Services.RemoveAll<IBrowserNotificationService>();
            TestContext.Services.RemoveAll<IWelcomeWizardStateService>();

            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_themeManagerService);
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.AddSingleton(_languageCatalog);
            TestContext.Services.AddSingleton(_languageInitializationService);
            TestContext.Services.AddSingleton(_keyboardService);
            TestContext.Services.AddSingleton(_appSettingsService);
            TestContext.Services.AddSingleton(_browserNotificationService);
            TestContext.Services.AddSingleton(_welcomeWizardStateService);

            _target = new WelcomeWizardDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_InitialLocaleProvided_WHEN_Rendered_THEN_SelectsResolvedLocale()
        {
            var dialog = await _target.RenderDialogAsync("fr-FR");

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            languageSelect.Instance.GetState(x => x.Value).Should().Be("fr");
        }

        [Fact]
        public async Task GIVEN_FirstStepActive_WHEN_Rendered_THEN_BackDisabledAndLanguageSelectVisible()
        {
            var dialog = await _target.RenderDialogAsync();

            var backButton = FindButton(dialog.Component, "WelcomeWizardBack");
            backButton.Instance.Disabled.Should().BeTrue();

            FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_DialogRendered_WHEN_FirstRenderCompletes_THEN_FocusesKeyboardService()
        {
            await _target.RenderDialogAsync();

            _keyboardServiceMock.Verify(service => service.Focus(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReturningUserWithPendingStep_WHEN_Rendered_THEN_ShowsWelcomeBackIntroBeforePendingStep()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                },
                showWelcomeBackIntro: true);

            FindComponentByTestId<MudStack>(dialog.Component, "WelcomeWizardIntroCard").Should().NotBeNull();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
            {
                FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_WelcomeFlowWithIntroNotificationsAndDone_WHEN_ActiveStepChanges_THEN_CurrentStepColorMatchesStepAccent()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                },
                showWelcomeBackIntro: true);

            var stepper = dialog.Component.FindComponent<MudStepper>();
            stepper.Instance.CurrentStepColor.Should().Be(Color.Info);

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            stepper.Instance.CurrentStepColor.Should().Be(Color.Warning);

            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            stepper.Instance.CurrentStepColor.Should().Be(Color.Success);
        }

        [Fact]
        public async Task GIVEN_DefaultFlow_WHEN_DoneStepSelectedFromStepperBeforeVisitingAllSteps_THEN_DoneStepIsBlocked()
        {
            var dialog = await _target.RenderDialogAsync();

            var stepper = dialog.Component.FindComponent<MudStepper>();
            var stepButtons = stepper.FindAll("button.mud-step");
            stepButtons[2].HasAttribute("disabled").Should().BeTrue();
            await stepButtons[2].ClickAsync(new MouseEventArgs());

            stepper.Instance.GetState(x => x.ActiveIndex).Should().Be(0);
            FindButton(dialog.Component, "WelcomeWizardNext").Should().NotBeNull();
            dialog.Component.FindComponents<MudButton>()
                .Any(component => HasTestId(component, "WelcomeWizardFinish"))
                .Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DefaultFlow_WHEN_UnvisitedIntermediateStepSelectedFromStepper_THEN_SelectionIsBlocked()
        {
            var dialog = await _target.RenderDialogAsync();

            var stepper = dialog.Component.FindComponent<MudStepper>();
            var stepButtons = stepper.FindAll("button.mud-step");
            stepButtons[1].HasAttribute("disabled").Should().BeTrue();
            await stepButtons[1].ClickAsync(new MouseEventArgs());

            stepper.Instance.GetState(x => x.ActiveIndex).Should().Be(0);
            FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_DefaultFlow_WHEN_StepsWerePreviouslyVisited_THEN_StepperCanNavigateBackToThem()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var stepper = dialog.Component.FindComponent<MudStepper>();
            var stepButtons = stepper.FindAll("button.mud-step");
            stepButtons[1].HasAttribute("disabled").Should().BeFalse();
            await stepButtons[1].ClickAsync(new MouseEventArgs());

            stepper.Instance.GetState(x => x.ActiveIndex).Should().Be(1);
            FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_NotificationsPendingOnly_WHEN_Rendered_THEN_ShowsOnlyNotificationStep()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled").Should().NotBeNull();
            var hasDownloadCompletedCheckbox = dialog.Component
                .FindComponents<MudCheckBox<bool>>()
                .Any(component => HasTestId(component, "WelcomeWizardDownloadFinishedNotificationsEnabled"));
            var hasTorrentAddedCheckbox = dialog.Component
                .FindComponents<MudCheckBox<bool>>()
                .Any(component => HasTestId(component, "WelcomeWizardTorrentAddedNotificationsEnabled"));
            hasDownloadCompletedCheckbox.Should().BeFalse();
            hasTorrentAddedCheckbox.Should().BeFalse();

            var hasLanguageStep = dialog.Component
                .FindComponents<MudSelect<string>>()
                .Any(component => HasTestId(component, "WelcomeWizardLanguageSelect"));
            var hasThemeStep = dialog.Component
                .FindComponents<MudSelect<string>>()
                .Any(component => HasTestId(component, "WelcomeWizardThemeSelect"));

            hasLanguageStep.Should().BeFalse();
            hasThemeStep.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionDenied_WHEN_NotificationsEnabled_THEN_PersistsDisabledSettingAndShowsWarning()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionGranted_WHEN_NotificationsEnabled_THEN_PersistsEnabledSetting()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionUnknown_WHEN_NotificationsEnabled_THEN_PersistsDisabledSettingAndShowsError()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unknown);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to synchronize notification settings.", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionRequestReturnsInsecure_WHEN_NotificationsEnabled_THEN_PersistsDisabledSettingAndShowsWarning()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Insecure);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Browser notifications require HTTPS or localhost.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionInsecure_WHEN_NotificationsStepRendered_THEN_ShowsDisabledToggleAndAlert()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Insecure);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");
            var permissionChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardNotificationPermission");
            var insecureAlert = FindComponentByTestId<MudAlert>(dialog.Component, "WelcomeWizardNotificationInsecureAlert");

            notificationSwitch.Instance.Disabled.Should().BeTrue();
            permissionChip.Instance.Color.Should().Be(Color.Warning);
            GetChildContentText(permissionChip.Instance.ChildContent).Should().Be("Permission: Insecure");
            GetChildContentText(insecureAlert.Instance.ChildContent).Should().Be("Browser notifications require HTTPS or localhost.");
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionInsecure_WHEN_NotificationsEnabled_THEN_DoesNotPersistOrRequestPermission()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Insecure);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_browserNotificationService).Verify(
                service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionGranted_WHEN_DownloadCompletedTypeUnchecked_THEN_PersistsDisabledSetting()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            var downloadCompletedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardDownloadFinishedNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => downloadCompletedCheckbox.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.DownloadFinishedNotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionGrantedAndAddedTypeEnabled_WHEN_SnackbarToggleEnabled_THEN_PersistsSetting()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            var torrentAddedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardTorrentAddedNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => torrentAddedCheckbox.Instance.ValueChanged.InvokeAsync(true));

            var snackbarToggle = FindSwitch(dialog.Component, "WelcomeWizardTorrentAddedSnackbarsEnabledWithNotifications");
            await dialog.Component.InvokeAsync(() => snackbarToggle.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => settings.TorrentAddedSnackbarsEnabledWithNotifications),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionRequestThrows_WHEN_NotificationsEnabled_THEN_PersistsDisabledSettingAndShowsError()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("Message"));

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_PendingWizardSteps_WHEN_Finished_THEN_AcknowledgesShownSteps()
        {
            await TestContext.LocalStorage.RemoveItemAsync(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                },
                showWelcomeBackIntro: true);

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_welcomeWizardStateService).Verify(
                service => service.AcknowledgeStepsAsync(
                    It.Is<IEnumerable<string>>(stepIds =>
                        stepIds.Count() == 1 &&
                        stepIds.Contains(WelcomeWizardStepCatalog.NotificationsStepId)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageStep_WHEN_NextClicked_THEN_ShowsThemeStep()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
            {
                FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_LanguageStep_WHEN_BackInvoked_THEN_StaysOnLanguageStep()
        {
            var dialog = await _target.RenderDialogAsync();

            var backButton = FindButton(dialog.Component, "WelcomeWizardBack");
            await dialog.Component.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_BackClicked_THEN_ShowsLanguageStep()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var backButton = FindButton(dialog.Component, "WelcomeWizardBack");
            await backButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
            {
                FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_LanguageSelected_WHEN_ValueChanged_THEN_UpdatesPreferences()
        {
            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");

            var previousCurrentCulture = CultureInfo.CurrentCulture;
            var previousCurrentUiCulture = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.DefaultThreadCurrentCulture;
            var previousUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

            try
            {
                await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCurrentCulture;
                CultureInfo.CurrentUICulture = previousCurrentUiCulture;
                CultureInfo.DefaultThreadCurrentCulture = previousCulture;
                CultureInfo.DefaultThreadCurrentUICulture = previousUiCulture;
            }

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(It.Is<UpdatePreferences>(preferences =>
                string.Equals(preferences.Locale, "fr", StringComparison.Ordinal))), Times.Once);
            Mock.Get(_languageInitializationService).Verify(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageSelected_WHEN_ValueChanged_THEN_PersistsLocaleInLocalStorage()
        {
            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("fr");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GIVEN_LocaleEmpty_WHEN_LocaleChanged_THEN_DoesNotUpdatePreferences(string? locale)
        {
            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync(locale));

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()), Times.Never);
            Mock.Get(_languageInitializationService).Verify(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData("@")]
        [InlineData("en@")]
        [InlineData("en@latin")]
        [InlineData("en@cyrillic")]
        [InlineData("en@Abcd")]
        [InlineData("en@foo")]
        [InlineData("invalid$$")]
        public async Task GIVEN_LocaleVariant_WHEN_LocaleChanged_THEN_UpdatesPreferences(string locale)
        {
            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");

            var previousCurrentCulture = CultureInfo.CurrentCulture;
            var previousCurrentUiCulture = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.DefaultThreadCurrentCulture;
            var previousUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

            try
            {
                await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync(locale));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCurrentCulture;
                CultureInfo.CurrentUICulture = previousCurrentUiCulture;
                CultureInfo.DefaultThreadCurrentCulture = previousCulture;
                CultureInfo.DefaultThreadCurrentUICulture = previousUiCulture;
            }

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(It.Is<UpdatePreferences>(preferences =>
                string.Equals(preferences.Locale, locale, StringComparison.Ordinal))), Times.Once);
            Mock.Get(_languageInitializationService).Verify(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeSelected_THEN_AppliesTheme()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("theme2"));

            Mock.Get(_themeManagerService).Verify(service => service.ApplyTheme("theme2"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeModePreferenceStored_WHEN_ThemeStepRendered_THEN_SelectUsesStoredValue()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    ThemeModePreference = ThemeModePreference.Light
                });

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeModeSelect = FindSelect<ThemeModePreference>(dialog.Component, "WelcomeWizardThemeModePreference");
            themeModeSelect.Instance.GetState(x => x.Value).Should().Be(ThemeModePreference.Light);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeModePreferenceChanged_THEN_PersistsAppSettings()
        {
            var dialog = await _target.RenderDialogAsync();
            _themeManagerService.ClearInvocations();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeModeSelect = FindSelect<ThemeModePreference>(dialog.Component, "WelcomeWizardThemeModePreference");
            await dialog.Component.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.Dark));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => settings.ThemeModePreference == ThemeModePreference.Dark),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_themeManagerService).Verify(
                service => service.ApplyPersistedThemeModePreference(ThemeModePreference.Dark),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeModePreferenceUnchanged_THEN_DoesNotPersistAppSettings()
        {
            var dialog = await _target.RenderDialogAsync();
            _themeManagerService.ClearInvocations();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeModeSelect = FindSelect<ThemeModePreference>(dialog.Component, "WelcomeWizardThemeModePreference");
            await dialog.Component.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.System));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_themeManagerService).Verify(
                service => service.ApplyPersistedThemeModePreference(It.IsAny<ThemeModePreference>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeSelectionIsEmpty_THEN_DoesNotApplyTheme()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync(" "));

            Mock.Get(_themeManagerService).Verify(service => service.ApplyTheme(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ApplyThemeThrows_THEN_ShowsSnackbarError()
        {
            Mock.Get(_themeManagerService)
                .Setup(service => service.ApplyTheme(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("theme2"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeIdMissing_THEN_DoneStepRenders()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("missing"));

            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            FindButton(dialog.Component, "WelcomeWizardFinish").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_LastStep_WHEN_FinishClicked_THEN_StoresCompletionAndClosesDialog()
        {
            await TestContext.LocalStorage.RemoveItemAsync(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(true);

            var stored = await TestContext.LocalStorage.GetItemAsync<bool?>(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);
            stored.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogRendered_WHEN_Closed_THEN_UnFocusesKeyboardService()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            await dialog.Reference.Result;

            dialog.Provider.WaitForAssertion(() =>
            {
                _keyboardServiceMock.Verify(service => service.UnFocus(), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_LastStep_WHEN_OpenOptionsClicked_THEN_NavigatesAndClosesDialog()
        {
            await TestContext.LocalStorage.RemoveItemAsync(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var openOptionsButton = FindButton(dialog.Component, "WelcomeWizardOpenOptions");
            await openOptionsButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            _navigationManager.LastUri.Should().EndWith("/settings");

            var stored = await TestContext.LocalStorage.GetItemAsync<bool?>(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);
            stored.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FinishWriteFails_WHEN_FinishClicked_THEN_ShowsSnackbarError()
        {
            var settingsStorage = new Mock<ISettingsStorageService>(MockBehavior.Loose);
            settingsStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Message"));

            TestContext.Services.RemoveAll<ISettingsStorageService>();
            TestContext.Services.AddSingleton<ISettingsStorageService>(settingsStorage.Object);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FinishWriteThrowsJsException_WHEN_FinishClicked_THEN_ShowsSnackbarError()
        {
            var settingsStorage = new Mock<ISettingsStorageService>(MockBehavior.Loose);
            settingsStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new JSException("Message"));

            TestContext.Services.RemoveAll<ISettingsStorageService>();
            TestContext.Services.AddSingleton<ISettingsStorageService>(settingsStorage.Object);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FinishWriteThrowsJsonException_WHEN_FinishClicked_THEN_ShowsSnackbarError()
        {
            var settingsStorage = new Mock<ISettingsStorageService>(MockBehavior.Loose);
            settingsStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new JsonException("Message"));

            TestContext.Services.RemoveAll<ISettingsStorageService>();
            TestContext.Services.AddSingleton<ISettingsStorageService>(settingsStorage.Object);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FinishWriteCanceled_WHEN_FinishClicked_THEN_DoesNotShowSnackbarError()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var settingsStorage = new Mock<ISettingsStorageService>(MockBehavior.Loose);
            settingsStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException(cancellationTokenSource.Token));

            TestContext.Services.RemoveAll<ISettingsStorageService>();
            TestContext.Services.AddSingleton<ISettingsStorageService>(settingsStorage.Object);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");

            await dialog.Component.InvokeAsync(() => finishButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_LanguageUpdateFails_WHEN_LocaleSelected_THEN_ShowsSnackbarError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Message");

            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageUpdateFailsWithoutUserMessage_WHEN_LocaleSelected_THEN_ShowsDefaultApiFailureMessage()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsFailure(ApiFailureKind.ServerError, string.Empty);

            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "Unable to update language: qBittorrent returned an error. Please try again.",
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ApplyThemeThrowsJsException_THEN_ShowsSnackbarError()
        {
            Mock.Get(_themeManagerService)
                .Setup(service => service.ApplyTheme(It.IsAny<string>()))
                .ThrowsAsync(new JSException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("theme2"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ApplyThemeThrowsJsonException_THEN_ShowsSnackbarError()
        {
            Mock.Get(_themeManagerService)
                .Setup(service => service.ApplyTheme(It.IsAny<string>()))
                .ThrowsAsync(new JsonException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("theme2"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ApplyThemeCanceled_THEN_DoesNotShowSnackbarError()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Mock.Get(_themeManagerService)
                .Setup(service => service.ApplyTheme(It.IsAny<string>()))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");

            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("theme2"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ThemeIdUnsetAndLanguageUnset_WHEN_DoneStepRendered_THEN_RendersSuccessfully()
        {
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(new List<ThemeCatalogItem>());
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns((string?)null);

            Mock.Get(_languageCatalog)
                .SetupGet(catalog => catalog.Languages)
                .Returns(new List<LanguageCatalogItem>());

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            FindButton(dialog.Component, "WelcomeWizardFinish").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_CurrentThemeIdMissing_WHEN_Rendered_THEN_SelectsFirstTheme()
        {
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns((string?)null);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            themeSelect.Instance.GetState(x => x.Value).Should().Be("theme1");
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionUnsupported_WHEN_NotificationsStepRendered_THEN_ShowsUnsupportedPermissionState()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unsupported);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            var permissionChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardNotificationPermission");
            permissionChip.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionChip.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionUnsupported_WHEN_NotificationsStepRendered_THEN_NotificationsSwitchIsDisabled()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unsupported);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            notificationSwitch.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionUnknownAndNotificationsEnabled_WHEN_NotificationsStepRendered_THEN_KeepsNotificationsEnabled()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unknown);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            notificationSwitch.Instance.Value.Should().BeTrue();
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_GetPermissionReturnsUnknownEnum_WHEN_NotificationsStepRendered_THEN_UsesUnsupportedFallback()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((BrowserNotificationPermission)99);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            var permissionChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardNotificationPermission");
            permissionChip.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionChip.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public async Task GIVEN_NotificationsInitiallyEnabled_WHEN_Disabled_THEN_RefreshesPermissionAndPersistsDisabled()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_browserNotificationService).Verify(
                service => service.GetPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationToggleIsApplying_WHEN_EnabledAgain_THEN_SecondToggleIsIgnored()
        {
            var requestPermissionCompletion = new TaskCompletionSource<BrowserNotificationPermission>();
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .Returns(requestPermissionCompletion.Task);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            var firstToggleTask = dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));
            var secondToggleTask = dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            requestPermissionCompletion.SetResult(BrowserNotificationPermission.Granted);

            await firstToggleTask;
            await secondToggleTask;

            Mock.Get(_browserNotificationService).Verify(
                service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestPendingAndPermissionChangesToGrantedBeforeRequestCompletes_WHEN_RequestCompletesWithDefault_THEN_PersistsEnabledSetting()
        {
            var requestPermissionCompletion = new TaskCompletionSource<BrowserNotificationPermission>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .Returns(requestPermissionCompletion.Task);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            var toggleTask = dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            requestPermissionCompletion.SetResult(BrowserNotificationPermission.Default);
            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.OnNotificationPermissionChanged());

            await toggleTask;

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestPendingAndPermissionChangesToDeniedBeforeRequestCompletes_WHEN_RequestCompletesWithDefault_THEN_PersistsDisabledSetting()
        {
            var requestPermissionCompletion = new TaskCompletionSource<BrowserNotificationPermission>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .Returns(requestPermissionCompletion.Task);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");

            var toggleTask = dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));
            dialog.Component.WaitForAssertion(() =>
            {
                Mock.Get(_browserNotificationService).Verify(
                    service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.OnNotificationPermissionChanged());

            requestPermissionCompletion.SetResult(BrowserNotificationPermission.Default);
            await toggleTask;

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_PermissionDoesNotChange_WHEN_PermissionChangeRaised_THEN_SettingsAreNotPersisted()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.OnNotificationPermissionChanged());

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(BrowserNotificationPermission.Default)]
        [InlineData(BrowserNotificationPermission.Insecure)]
        [InlineData(BrowserNotificationPermission.Unsupported)]
        public async Task GIVEN_NotificationsEnabledAndPermissionDisablesNotifications_WHEN_PermissionChanges_THEN_PersistsDisabledSetting(BrowserNotificationPermission permission)
        {
            Mock.Get(_browserNotificationService)
                .SetupSequence(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted)
                .ReturnsAsync(permission);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.OnNotificationPermissionChanged());

            FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled").Instance.Value.Should().BeFalse();
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationsEnabledAndPermissionChangesToUnknown_WHEN_PermissionChanges_THEN_NotificationsRemainEnabled()
        {
            Mock.Get(_browserNotificationService)
                .SetupSequence(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted)
                .ReturnsAsync(BrowserNotificationPermission.Unknown);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.OnNotificationPermissionChanged());

            FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled").Instance.Value.Should().BeTrue();
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationsEnabledAndPermissionChangesToUnknownEnum_WHEN_PermissionChanges_THEN_PersistsDisabledSetting()
        {
            Mock.Get(_browserNotificationService)
                .SetupSequence(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted)
                .ReturnsAsync((BrowserNotificationPermission)99);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.OnNotificationPermissionChanged());

            FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled").Instance.Value.Should().BeFalse();
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData("js")]
        [InlineData("invalid")]
        [InlineData("http")]
        public async Task GIVEN_GetPermissionThrows_WHEN_NotificationsStepRendered_THEN_PermissionFallsBackToUnknown(string exceptionKind)
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    return exceptionKind switch
                    {
                        "js" => Task.FromException<BrowserNotificationPermission>(new JSException("Failure")),
                        "invalid" => Task.FromException<BrowserNotificationPermission>(new InvalidOperationException("Failure")),
                        _ => Task.FromException<BrowserNotificationPermission>(new HttpRequestException("Failure"))
                    };
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            var permissionChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardNotificationPermission");
            GetChildContentText(permissionChip.Instance.ChildContent).Should().Be("Permission: Unknown");
        }

        [Fact]
        public async Task GIVEN_SubscribeReturnsNoIdentifier_WHEN_WizardRenders_THEN_SubscriptionRetryPathIsUsed()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.SubscribePermissionChangesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            dialog.Component.WaitForAssertion(() =>
            {
                Mock.Get(_browserNotificationService).Verify(
                    service => service.SubscribePermissionChangesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                    Times.AtLeast(2));
            });
        }

        [Fact]
        public async Task GIVEN_PermissionChangePersistenceThrows_WHEN_PermissionChanges_THEN_ShowsSynchronizationError()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });
            Mock.Get(_appSettingsService)
                .Setup(service => service.SaveSettingsAsync(
                    It.Is<AppSettings>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });

            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.OnNotificationPermissionChanged());

            dialog.Component.WaitForAssertion(() =>
            {
                Mock.Get(_snackbar).Verify(
                    snackbar => snackbar.Add(
                        It.Is<string>(message => string.Equals(message, "Unable to synchronize notification settings.", StringComparison.Ordinal)),
                        Severity.Error,
                        It.IsAny<Action<SnackbarOptions>>(),
                        It.IsAny<string>()),
                    Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_DownloadFinishedNotificationUnchanged_WHEN_ValueChanged_THEN_DoesNotPersist()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var downloadCompletedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardDownloadFinishedNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => downloadCompletedCheckbox.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_TorrentAddedNotificationUnchanged_WHEN_ValueChanged_THEN_DoesNotPersist()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var torrentAddedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardTorrentAddedNotificationsEnabled");

            await dialog.Component.InvokeAsync(() => torrentAddedCheckbox.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_TorrentAddedSnackbarPreferenceUnchanged_WHEN_ValueChanged_THEN_DoesNotPersist()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = true,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    UpdateChecksEnabled = true
                });

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var snackbarSwitch = FindSwitch(dialog.Component, "WelcomeWizardTorrentAddedSnackbarsEnabledWithNotifications");

            await dialog.Component.InvokeAsync(() => snackbarSwitch.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_PendingStepIdsIncludeUnknownDuplicateAndOutOfOrder_WHEN_Rendered_THEN_StepsAreKnownUniqueAndOrdered()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId,
                    "unknown.step",
                    WelcomeWizardStepCatalog.ThemeStepId,
                    $" {WelcomeWizardStepCatalog.NotificationsStepId} "
                });

            var stepTitles = dialog.Component
                .FindComponents<MudStep>()
                .Select(component => component.Instance.Title)
                .ToList();

            stepTitles.Should().ContainInOrder(["Theme", "Notifications", "Done"]);
            stepTitles.Should().HaveCount(3);
        }

        [Fact]
        public async Task GIVEN_NotificationsStepIncludedAndNotificationsDisabled_WHEN_DoneStepRendered_THEN_ShowsDisabledSummary()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var summaryChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardDoneNotificationsChip");

            GetChildContentText(summaryChip.Instance.ChildContent).Should().Be("Notifications: Disabled");
            summaryChip.Instance.Color.Should().Be(Color.Warning);
            summaryChip.Instance.UserAttributes.Should().ContainKey("data-test-summary-color");
            summaryChip.Instance.UserAttributes["data-test-summary-color"]?.ToString().Should().Be("Default");
        }

        [Fact]
        public async Task GIVEN_NotificationsEnabledWithNoTypesSelected_WHEN_DoneStepRendered_THEN_ShowsNoneSelectedSummary()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            var downloadCompletedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardDownloadFinishedNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => downloadCompletedCheckbox.Instance.ValueChanged.InvokeAsync(false));

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var summaryChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardDoneNotificationsChip");

            GetChildContentText(summaryChip.Instance.ChildContent).Should().Be("Notifications: None selected");
            summaryChip.Instance.Color.Should().Be(Color.Warning);
            summaryChip.Instance.UserAttributes["data-test-summary-color"]?.ToString().Should().Be("Warning");
        }

        [Fact]
        public async Task GIVEN_NotificationsEnabledWithTorrentAddedOnly_WHEN_DoneStepRendered_THEN_ShowsTorrentAddedSummary()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            var downloadCompletedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardDownloadFinishedNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => downloadCompletedCheckbox.Instance.ValueChanged.InvokeAsync(false));

            var torrentAddedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardTorrentAddedNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => torrentAddedCheckbox.Instance.ValueChanged.InvokeAsync(true));

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var summaryChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardDoneNotificationsChip");

            GetChildContentText(summaryChip.Instance.ChildContent).Should().Be("Notifications: Torrent added");
            summaryChip.Instance.Color.Should().Be(Color.Warning);
            summaryChip.Instance.UserAttributes["data-test-summary-color"]?.ToString().Should().Be("Success");
        }

        [Fact]
        public async Task GIVEN_NotificationsEnabledWithAllTypesSelected_WHEN_DoneStepRendered_THEN_ShowsCombinedSummary()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                });
            var notificationSwitch = FindSwitch(dialog.Component, "WelcomeWizardNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => notificationSwitch.Instance.ValueChanged.InvokeAsync(true));

            var torrentAddedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "WelcomeWizardTorrentAddedNotificationsEnabled");
            await dialog.Component.InvokeAsync(() => torrentAddedCheckbox.Instance.ValueChanged.InvokeAsync(true));

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var summaryChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardDoneNotificationsChip");

            GetChildContentText(summaryChip.Instance.ChildContent).Should().Be("Notifications: Download completed, Torrent added");
            summaryChip.Instance.Color.Should().Be(Color.Warning);
            summaryChip.Instance.UserAttributes["data-test-summary-color"]?.ToString().Should().Be("Success");
        }

        [Fact]
        public async Task GIVEN_StorageStep_WHEN_Rendered_THEN_BrowserLocalStorageIsSelectedAndNextIsEnabled()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var storageSelection = FindComponentByTestId<MudRadioGroup<StorageType>>(dialog.Component, "WelcomeWizardStorageSelection");
            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");

            storageSelection.Instance.Value.Should().Be(StorageType.LocalStorage);
            nextButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_StorageStep_WHEN_Rendered_THEN_BrowserLocalStorageCardIsRenderedFirst()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var cards = dialog.Component.FindComponents<MudCard>();

            cards.Should().HaveCount(2);
            HasTestId(cards[0], "WelcomeWizardStorageLocalStorageCard").Should().BeTrue();
            HasTestId(cards[1], "WelcomeWizardStorageClientDataCard").Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_StorageStepWithInvalidPersistedMasterStorageType_WHEN_Rendered_THEN_DefaultsToLocalStorageSelection()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = (StorageType)99
                });

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.AddSingleton<IStorageRoutingService>(storageRoutingService.Object);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var storageSelection = FindComponentByTestId<MudRadioGroup<StorageType>>(dialog.Component, "WelcomeWizardStorageSelection");
            storageSelection.Instance.Value.Should().Be(StorageType.LocalStorage);
        }

        [Fact]
        public async Task GIVEN_StorageStep_WHEN_Rendered_THEN_ShowsFriendlyStorageOptionNamesAndDescriptions()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var clientDataTitle = FindComponentByTestId<MudText>(dialog.Component, "WelcomeWizardStorageClientDataTitle");
            var clientDataDescription = FindComponentByTestId<MudText>(dialog.Component, "WelcomeWizardStorageClientDataDescription");
            var localStorageTitle = FindComponentByTestId<MudText>(dialog.Component, "WelcomeWizardStorageLocalStorageTitle");
            var localStorageDescription = FindComponentByTestId<MudText>(dialog.Component, "WelcomeWizardStorageLocalStorageDescription");

            GetChildContentText(clientDataTitle.Instance.ChildContent).Should().Be("qBittorrent client data");
            GetChildContentText(clientDataDescription.Instance.ChildContent).Should().Be("Saves settings in qBittorrent, so they follow this server across browsers and devices.");
            GetChildContentText(localStorageTitle.Instance.ChildContent).Should().Be("Browser local storage");
            GetChildContentText(localStorageDescription.Instance.ChildContent).Should().Be("Saves settings only in this browser profile on this device.");
        }

        [Fact]
        public async Task GIVEN_StorageStepWithSelection_WHEN_FinishClicked_THEN_AppliesStorageSelection()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageRoutingSettings settings, CancellationToken _) => settings.Clone());

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.AddSingleton<IStorageRoutingService>(storageRoutingService.Object);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var storageSelection = FindComponentByTestId<MudRadioGroup<StorageType>>(dialog.Component, "WelcomeWizardStorageSelection");
            await dialog.Component.InvokeAsync(() => storageSelection.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            storageRoutingService.Verify(
                service => service.SaveSettingsAsync(
                    It.Is<StorageRoutingSettings>(settings =>
                        settings.MasterStorageType == StorageType.ClientData
                        && settings.GroupStorageTypes.Count == 0
                        && settings.ItemStorageTypes.Count == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StorageStepWithDefaultSelection_WHEN_FinishClicked_THEN_AppliesLocalStorageSelection()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageRoutingSettings settings, CancellationToken _) => settings.Clone());

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.AddSingleton<IStorageRoutingService>(storageRoutingService.Object);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            storageRoutingService.Verify(
                service => service.SaveSettingsAsync(
                    It.Is<StorageRoutingSettings>(settings =>
                        settings.MasterStorageType == StorageType.LocalStorage
                        && settings.GroupStorageTypes.Count == 0
                        && settings.ItemStorageTypes.Count == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_LocalStorageSelection_WHEN_DoneStepRendered_THEN_SummaryUsesFriendlyStorageName()
        {
            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var storageSelection = FindComponentByTestId<MudRadioGroup<StorageType>>(dialog.Component, "WelcomeWizardStorageSelection");
            await dialog.Component.InvokeAsync(() => storageSelection.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var summaryChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardDoneStorageChip");
            GetChildContentText(summaryChip.Instance.ChildContent).Should().Be("Storage: Browser local storage");
        }

        [Fact]
        public async Task GIVEN_StorageSelectionSaveFails_WHEN_FinishClicked_THEN_ShowsSnackbarErrorAndSkipsCompletionWrite()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("StorageError"));

            var settingsStorage = new Mock<ISettingsStorageService>(MockBehavior.Strict);

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<ISettingsStorageService>();
            TestContext.Services.AddSingleton<IStorageRoutingService>(storageRoutingService.Object);
            TestContext.Services.AddSingleton<ISettingsStorageService>(settingsStorage.Object);

            var dialog = await _target.RenderDialogAsync(
                pendingStepIds: new[]
                {
                    WelcomeWizardStepCatalog.StorageStepId
                });

            var storageSelection = FindComponentByTestId<MudRadioGroup<StorageType>>(dialog.Component, "WelcomeWizardStorageSelection");
            await dialog.Component.InvokeAsync(() => storageSelection.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
            settingsStorage.Verify(
                service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_LanguageAndThemeStepsIncluded_WHEN_DoneStepRendered_THEN_SummaryChipsMatchStepAccents()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var languageChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardDoneLanguageChip");
            var themeChip = FindComponentByTestId<MudChip<string>>(dialog.Component, "WelcomeWizardDoneThemeChip");

            languageChip.Instance.Color.Should().Be(Color.Info);
            themeChip.Instance.Color.Should().Be(Color.Primary);
        }

        [Fact]
        public async Task GIVEN_LanguageCatalogInitializationIsDeferred_WHEN_InitiallyRendered_THEN_InitialStepCollectionIsEmptyUntilInitializationCompletes()
        {
            var ensureInitializedCompletion = new TaskCompletionSource();
            Mock.Get(_languageCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(ensureInitializedCompletion.Task);

            var renderTask = _target.RenderDialogAsync();
            await Task.Yield();
            var dialog = await renderTask;

            dialog.Component.FindComponents<MudStep>().Should().HaveCount(0);

            ensureInitializedCompletion.SetResult();

            dialog.Component.WaitForAssertion(() =>
            {
                dialog.Component.FindComponents<MudStep>().Should().NotBeEmpty();
            });
        }

        [Fact]
        public async Task GIVEN_KeyboardFocusThrows_WHEN_DialogClosed_THEN_UnFocusIsNotCalled()
        {
            _keyboardServiceMock
                .Setup(service => service.Focus())
                .ThrowsAsync(new InvalidOperationException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());
            await dialog.Reference.Result;

            _keyboardServiceMock.Verify(service => service.UnFocus(), Times.Never);
        }

        private static ThemeCatalogItem CreateTheme(string id, string name)
        {
            var definition = new ThemeDefinition
            {
                Id = id,
                Name = name
            };

            return new ThemeCatalogItem(id, name, definition, ThemeSource.Local, sourcePath: null);
        }
    }

    internal sealed class TestNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        public string LastUri { get; private set; } = "http://localhost/";

        protected override void NavigateToCore(string uri, Microsoft.AspNetCore.Components.NavigationOptions options)
        {
            var absolute = ToAbsoluteUri(uri).ToString();
            LastUri = absolute;
            Uri = absolute;
        }
    }

    internal sealed class WelcomeWizardDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public WelcomeWizardDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<WelcomeWizardDialogRenderContext> RenderDialogAsync(
            string? initialLocale = null,
            IReadOnlyList<string>? pendingStepIds = null,
            bool? showWelcomeBackIntro = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();
            if (!string.IsNullOrWhiteSpace(initialLocale))
            {
                parameters.Add(nameof(WelcomeWizardDialog.InitialLocale), initialLocale);
            }

            if (pendingStepIds is not null)
            {
                parameters.Add(nameof(WelcomeWizardDialog.PendingStepIds), pendingStepIds);
            }

            if (showWelcomeBackIntro.HasValue)
            {
                parameters.Add(nameof(WelcomeWizardDialog.ShowWelcomeBackIntro), showWelcomeBackIntro.Value);
            }

            var reference = await dialogService.ShowAsync<WelcomeWizardDialog>(title: null, parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<WelcomeWizardDialog>();

            return new WelcomeWizardDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class WelcomeWizardDialogRenderContext
    {
        public WelcomeWizardDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<WelcomeWizardDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<WelcomeWizardDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
