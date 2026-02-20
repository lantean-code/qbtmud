using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
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
using System.Globalization;
using System.Text.Json;

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
        private readonly ITorrentCompletionNotificationService _torrentCompletionNotificationService = Mock.Of<ITorrentCompletionNotificationService>();
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
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
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
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
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
            TestContext.Services.RemoveAll<ITorrentCompletionNotificationService>();
            TestContext.Services.RemoveAll<IWelcomeWizardStateService>();

            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_themeManagerService);
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.AddSingleton(_languageCatalog);
            TestContext.Services.AddSingleton(_languageInitializationService);
            TestContext.Services.AddSingleton(_keyboardService);
            TestContext.Services.AddSingleton(_appSettingsService);
            TestContext.Services.AddSingleton(_torrentCompletionNotificationService);
            TestContext.Services.AddSingleton(_welcomeWizardStateService);

            _target = new WelcomeWizardDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_InitialLocaleProvided_WHEN_Rendered_THEN_SelectsResolvedLocale()
        {
            var dialog = await _target.RenderDialogAsync("fr-FR");

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            languageSelect.Instance.Value.Should().Be("fr");
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
            Mock.Get(_torrentCompletionNotificationService)
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
            Mock.Get(_torrentCompletionNotificationService)
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
            Mock.Get(_torrentCompletionNotificationService)
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

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferences(It.Is<UpdatePreferences>(preferences =>
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

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()), Times.Never);
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

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferences(It.Is<UpdatePreferences>(preferences =>
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
            var localStorage = new Mock<ILocalStorageService>(MockBehavior.Loose);
            localStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Message"));

            TestContext.Services.RemoveAll<ILocalStorageService>();
            TestContext.Services.AddSingleton<ILocalStorageService>(localStorage.Object);

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
            var localStorage = new Mock<ILocalStorageService>(MockBehavior.Loose);
            localStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new JSException("Message"));

            TestContext.Services.RemoveAll<ILocalStorageService>();
            TestContext.Services.AddSingleton<ILocalStorageService>(localStorage.Object);

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
            var localStorage = new Mock<ILocalStorageService>(MockBehavior.Loose);
            localStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new JsonException("Message"));

            TestContext.Services.RemoveAll<ILocalStorageService>();
            TestContext.Services.AddSingleton<ILocalStorageService>(localStorage.Object);

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

            var localStorage = new Mock<ILocalStorageService>(MockBehavior.Loose);
            localStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException(cancellationTokenSource.Token));

            TestContext.Services.RemoveAll<ILocalStorageService>();
            TestContext.Services.AddSingleton<ILocalStorageService>(localStorage.Object);

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
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
                .ThrowsAsync(new HttpRequestException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageUpdateThrowsInvalidOperation_WHEN_LocaleSelected_THEN_ShowsSnackbarError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
                .ThrowsAsync(new InvalidOperationException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageUpdateThrowsJsException_WHEN_LocaleSelected_THEN_ShowsSnackbarError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
                .ThrowsAsync(new JSException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageUpdateThrowsJsonException_WHEN_LocaleSelected_THEN_ShowsSnackbarError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
                .ThrowsAsync(new JsonException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
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
            themeSelect.Instance.Value.Should().Be("theme1");
        }

        [Fact]
        public async Task GIVEN_GetPermissionThrowsOnInitialize_WHEN_NotificationsStepRendered_THEN_ShowsUnsupportedPermissionState()
        {
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("Message"));

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
        public async Task GIVEN_GetPermissionReturnsUnknownEnum_WHEN_NotificationsStepRendered_THEN_UsesUnsupportedFallback()
        {
            Mock.Get(_torrentCompletionNotificationService)
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

            Mock.Get(_torrentCompletionNotificationService).Verify(
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
            Mock.Get(_torrentCompletionNotificationService)
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

            Mock.Get(_torrentCompletionNotificationService).Verify(
                service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
                Times.Once);
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
            summaryChip.Instance.UserAttributes["data-test-summary-color"]?.ToString().Should().Be("Success");
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
