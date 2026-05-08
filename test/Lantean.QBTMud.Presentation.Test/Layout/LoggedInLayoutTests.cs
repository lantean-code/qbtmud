using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Core.Helpers;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Layout;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

using ClientModels = QBittorrent.ApiClient.Models;

using MudCategory = Lantean.QBTMud.Core.Models.Category;
using MudMainData = Lantean.QBTMud.Core.Models.MainData;
using MudServerState = Lantean.QBTMud.Core.Models.ServerState;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Presentation.Test.Layout
{
    public sealed class LoggedInLayoutTests : RazorComponentTestBase<LoggedInLayout>
    {
        private const string _pendingDownloadStorageKey = "LoggedInLayout.PendingDownload";
        private const string _lastProcessedDownloadStorageKey = "LoggedInLayout.LastProcessedDownload";
        private const string _preferredLocaleStorageKey = "WebUiLocalization.PreferredLocale.v1";

        private readonly IApiClient _apiClient = Mock.Of<IApiClient>(MockBehavior.Strict);
        private readonly ITorrentDataManager _dataManager = Mock.Of<ITorrentDataManager>();
        private readonly ISpeedHistoryService _speedHistoryService = Mock.Of<ISpeedHistoryService>();
        private readonly IManagedTimerFactory _managedTimerFactory = Mock.Of<IManagedTimerFactory>();
        private readonly IManagedTimerRegistry _timerRegistry = Mock.Of<IManagedTimerRegistry>();
        private readonly IManagedTimer _refreshTimer = Mock.Of<IManagedTimer>();
        private readonly IDialogWorkflow _dialogWorkflow = Mock.Of<IDialogWorkflow>();
        private readonly IDialogService _dialogService = Mock.Of<IDialogService>();
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly ISnackbar _snackbar = Mock.Of<ISnackbar>();
        private readonly IAppSettingsService _appSettingsService = Mock.Of<IAppSettingsService>();
        private readonly IAppUpdateService _appUpdateService = Mock.Of<IAppUpdateService>();
        private readonly ITorrentCompletionNotificationService _torrentCompletionNotificationService = Mock.Of<ITorrentCompletionNotificationService>();
        private readonly IWelcomeWizardPlanBuilder _welcomeWizardPlanBuilder = Mock.Of<IWelcomeWizardPlanBuilder>();
        private readonly IWelcomeWizardStateService _welcomeWizardStateService = Mock.Of<IWelcomeWizardStateService>();
        private readonly TestNavigationManager _navigationManager;

        public LoggedInLayoutTests()
        {
            _navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll<NavigationManager>();
            TestContext.Services.AddSingleton<NavigationManager>(_navigationManager);

            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock.Setup(c => c.CheckAuthStateAsync()).ReturnsSuccessAsync(true);
            apiClientMock.Setup(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(true);
            apiClientMock.Setup(c => c.InitializeAsync(It.IsAny<CancellationToken>())).ReturnsSuccess(Task.CompletedTask);
            apiClientMock.Setup(c => c.GetApplicationPreferencesAsync()).ReturnsSuccessAsync(CreatePreferences());
            apiClientMock.Setup(c => c.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(CreatePreferences());
            apiClientMock.Setup(c => c.GetApplicationVersionAsync()).ReturnsSuccessAsync("Version");
            apiClientMock.Setup(c => c.GetApplicationVersionAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync("Version");
            apiClientMock.Setup(c => c.GetMainDataAsync(It.IsAny<int>())).ReturnsSuccessAsync(CreateClientMainData());
            apiClientMock.Setup(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsSuccessAsync(CreateClientMainData());

            var dataManagerMock = Mock.Get(_dataManager);
            dataManagerMock.Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(CreateMainData());

            var speedHistoryServiceMock = Mock.Get(_speedHistoryService);
            speedHistoryServiceMock.Setup(s => s.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            speedHistoryServiceMock.Setup(s => s.PushSampleAsync(It.IsAny<DateTime>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var refreshTimerMock = Mock.Get(_refreshTimer);
            refreshTimerMock.Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            refreshTimerMock.Setup(t => t.UpdateIntervalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var managedTimerFactoryMock = Mock.Get(_managedTimerFactory);
            managedTimerFactoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<int>())).Returns(_refreshTimer);

            var timerRegistryMock = Mock.Get(_timerRegistry);
            timerRegistryMock.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            _dialogServiceMock = Mock.Get(_dialogService);
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .ReturnsAsync(CreateDialogReference(DialogResult.Ok(true)));
            _dialogServiceMock
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(Mock.Of<IDialogReference>(MockBehavior.Loose));

            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_appSettingsService)
                .Setup(service => service.SaveDismissedReleaseTagAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.0.0", "v1.0.0", "https://example.invalid", DateTime.UtcNow),
                    false,
                    true,
                    DateTime.UtcNow));
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.InitializeAsync(It.IsAny<IReadOnlyDictionary<string, MudTorrent>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.ProcessAsync(It.IsAny<IReadOnlyDictionary<string, MudTorrent>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.ProcessTransitionsAsync(It.IsAny<IReadOnlyList<TorrentTransition>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(isReturningUser: false, pendingSteps: Array.Empty<WelcomeWizardStepDefinition>()));
            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.MarkShownAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardState());

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.RemoveAll<ISpeedHistoryService>();
            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.RemoveAll<IManagedTimerRegistry>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.RemoveAll<IDialogService>();
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.RemoveAll<IAppSettingsService>();
            TestContext.Services.RemoveAll<IAppUpdateService>();
            TestContext.Services.RemoveAll<ITorrentCompletionNotificationService>();
            TestContext.Services.RemoveAll<IWelcomeWizardPlanBuilder>();
            TestContext.Services.RemoveAll<IWelcomeWizardStateService>();
            TestContext.Services.RemoveAll<ILostConnectionWorkflow>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_dataManager);
            TestContext.Services.AddSingleton(_speedHistoryService);
            TestContext.Services.AddSingleton(_managedTimerFactory);
            TestContext.Services.AddSingleton(_timerRegistry);
            TestContext.Services.AddSingleton(_dialogWorkflow);
            TestContext.Services.AddSingleton(_dialogService);
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.AddSingleton(_appSettingsService);
            TestContext.Services.AddSingleton(_appUpdateService);
            TestContext.Services.AddSingleton(_torrentCompletionNotificationService);
            TestContext.Services.AddSingleton(_welcomeWizardPlanBuilder);
            TestContext.Services.AddSingleton(_welcomeWizardStateService);
            TestContext.Services.AddScoped<ILostConnectionWorkflow, LostConnectionWorkflow>();
        }

        [Fact]
        public void GIVEN_WelcomeWizardIncomplete_WHEN_Rendered_THEN_ShowsWizardDialog()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps: new[]
                    {
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.LanguageStepId, 0)
                    }));

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogService).Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Once);
            });
        }

        [Fact]
        public void GIVEN_WelcomeWizardCompleted_WHEN_Rendered_THEN_DoesNotShowWizardDialog()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(isReturningUser: true, pendingSteps: Array.Empty<WelcomeWizardStepDefinition>()));

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogService).Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Never);
            });
        }

        [Fact]
        public void GIVEN_LoadedAppSettings_WHEN_Rendered_THEN_RuntimeAppSettingsStateIsSeeded()
        {
            var settings = AppSettings.Default.Clone();
            settings.SpeedHistoryEnabled = false;
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            _ = RenderLayout(new List<IManagedTimer>());

            TestContext.Services.GetRequiredService<IAppSettingsStateService>().Current!.SpeedHistoryEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_WelcomeWizardCanceled_WHEN_Rendered_THEN_DoesNotShowPwaPrompt()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.LanguageStepId, 0)
                    ]));
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .ReturnsAsync(CreateDialogReference(DialogResult.Cancel()));

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogService).Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Once);
            });

            await WaitForDurationAsync(TimeSpan.FromSeconds(3));

            target.FindComponents<PwaInstallPrompt>().Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_WelcomeWizardPendingStepsAndLocale_WHEN_Rendered_THEN_ShowsWizardDialog()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.LanguageStepId, 0)
                    ]));

            var target = RenderLayout(new List<IManagedTimer>(), preferences: CreatePreferences(locale: "en"));

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogService).Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_WelcomeWizardReturnsNullResult_WHEN_Rendered_THEN_DoesNotShowPwaPrompt()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.LanguageStepId, 0)
                    ]));
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .ReturnsAsync(CreateDialogReference(Task.FromResult<DialogResult?>(null)));

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogService).Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Once);
            });

            await WaitForDurationAsync(TimeSpan.FromSeconds(3));

            target.FindComponents<PwaInstallPrompt>().Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_WelcomeWizardPreviouslyCompleted_WHEN_Rendered_THEN_ShowsPwaPromptAfterDelay()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(isReturningUser: true, pendingSteps: Array.Empty<WelcomeWizardStepDefinition>()));

            var target = RenderLayout(new List<IManagedTimer>());

            target.FindComponents<PwaInstallPrompt>().Should().BeEmpty();
            target.WaitForAssertion(
                () => target.FindComponents<PwaInstallPrompt>().Should().ContainSingle(),
                timeout: TimeSpan.FromSeconds(5));

            Mock.Get(_dialogService).Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogParameters>(),
                It.IsAny<DialogOptions?>()), Times.Never);
        }

        [Fact]
        public void GIVEN_WelcomeWizardPendingSteps_WHEN_Finished_THEN_ShowsPwaPromptAfterDelay()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();

            var dialogResultTaskSource = new TaskCompletionSource<DialogResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.LanguageStepId, 0)
                    ]));

            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .ReturnsAsync(CreateDialogReference(dialogResultTaskSource.Task));

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogService).Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Once);
            });

            target.FindComponents<PwaInstallPrompt>().Should().BeEmpty();

            dialogResultTaskSource.SetResult(DialogResult.Ok(true));

            target.WaitForAssertion(
                () => target.FindComponents<PwaInstallPrompt>().Should().ContainSingle(),
                timeout: TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void GIVEN_WelcomeWizardPendingSteps_WHEN_Rendered_THEN_PassesPlanParametersAndMarksShown()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();

            var expectedStepIds = new[]
            {
                WelcomeWizardStepCatalog.NotificationsStepId
            };
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: true,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.NotificationsStepId, 2)
                    ]));

            DialogParameters? capturedParameters = null;
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .Callback<string?, DialogParameters, DialogOptions?>((_, parameters, _) => capturedParameters = parameters)
                .ReturnsAsync(CreateDialogReference(DialogResult.Ok(true)));

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                capturedParameters.Should().NotBeNull();
                capturedParameters!.Get<string[]>(nameof(WelcomeWizardDialog.PendingStepIds)).Should().BeEquivalentTo(expectedStepIds);
                capturedParameters.Get<bool>(nameof(WelcomeWizardDialog.ShowWelcomeBackIntro)).Should().BeTrue();
            });

            Mock.Get(_welcomeWizardStateService)
                .Verify(service => service.MarkShownAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GIVEN_WelcomeWizardOnSmallBreakpoint_WHEN_Rendered_THEN_UsesFullScreenDialogOptions()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.ThemeStepId, 0)
                    ]));

            DialogOptions? capturedOptions = null;
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .Callback<string?, DialogParameters, DialogOptions?>((_, _, options) => capturedOptions = options)
                .ReturnsAsync(CreateDialogReference(DialogResult.Ok(true)));

            var target = RenderLayout(new List<IManagedTimer>(), breakpoint: Breakpoint.Sm);

            target.WaitForAssertion(() =>
            {
                capturedOptions.Should().NotBeNull();
                capturedOptions!.FullScreen.Should().BeTrue();
                capturedOptions.FullWidth.Should().BeTrue();
                capturedOptions.MaxWidth.Should().Be(MaxWidth.Medium);
            });
        }

        [Fact]
        public void GIVEN_WelcomeWizardOnLargeBreakpoint_WHEN_Rendered_THEN_DoesNotUseFullScreenDialogOptions()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.ThemeStepId, 0)
                    ]));

            DialogOptions? capturedOptions = null;
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .Callback<string?, DialogParameters, DialogOptions?>((_, _, options) => capturedOptions = options)
                .ReturnsAsync(CreateDialogReference(DialogResult.Ok(true)));

            var target = RenderLayout(new List<IManagedTimer>(), breakpoint: Breakpoint.Lg);

            target.WaitForAssertion(() =>
            {
                capturedOptions.Should().NotBeNull();
                capturedOptions!.FullScreen.Should().BeFalse();
                capturedOptions.FullWidth.Should().BeTrue();
                capturedOptions.MaxWidth.Should().Be(MaxWidth.Medium);
            });
        }

        [Fact]
        public void GIVEN_WelcomeWizardOnNoneBreakpoint_WHEN_Rendered_THEN_UsesFullScreenDialogOptions()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(
                    isReturningUser: false,
                    pendingSteps:
                    [
                        new WelcomeWizardStepDefinition(WelcomeWizardStepCatalog.ThemeStepId, 0)
                    ]));

            DialogOptions? capturedOptions = null;
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .Callback<string?, DialogParameters, DialogOptions?>((_, _, options) => capturedOptions = options)
                .ReturnsAsync(CreateDialogReference(DialogResult.Ok(true)));

            var target = RenderLayout(new List<IManagedTimer>(), breakpoint: Breakpoint.None);

            target.WaitForAssertion(() =>
            {
                capturedOptions.Should().NotBeNull();
                capturedOptions!.FullScreen.Should().BeTrue();
                capturedOptions.FullWidth.Should().BeTrue();
                capturedOptions.MaxWidth.Should().Be(MaxWidth.Medium);
            });
        }

        [Fact]
        public async Task GIVEN_StoredLocaleMissing_WHEN_Initialized_THEN_ShouldSyncFromApiWithoutWarning()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            await TestContext.LocalStorage.RemoveItemAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            RenderLayout(new List<IManagedTimer>(), preferences: CreatePreferences(locale: "fr"));

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("fr");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Language preference changed on server. Click Reload to apply it.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_StoredLocalePresentAndApiLocaleBlank_WHEN_Initialized_THEN_ShouldClearStoredLocaleWithoutWarning()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            await TestContext.LocalStorage.SetItemAsStringAsync(_preferredLocaleStorageKey, "en", Xunit.TestContext.Current.CancellationToken);
            RenderLayout(new List<IManagedTimer>(), preferences: CreatePreferences(locale: "   "));

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().BeNull();

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Language preference changed on server. Click Reload to apply it.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_StoredLocaleMismatch_WHEN_Initialized_THEN_ShouldShowWarningWithReloadAction()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();
            await TestContext.LocalStorage.SetItemAsStringAsync(_preferredLocaleStorageKey, "en", Xunit.TestContext.Current.CancellationToken);

            Action<SnackbarOptions>? snackbarOptions = null;
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Callback<string, Severity, Action<SnackbarOptions>, string>((_, _, options, _) => snackbarOptions = options);
            RenderLayout(new List<IManagedTimer>(), preferences: CreatePreferences(locale: "fr"));

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("fr");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Language preference changed on server. Click Reload to apply it.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);

            snackbarOptions.Should().NotBeNull();

            var options = new SnackbarOptions(Severity.Warning, new SnackbarConfiguration());
            snackbarOptions!(options);

            options.RequireInteraction.Should().BeTrue();
            options.Action.Should().Be("Reload");
            options.CloseAfterNavigation.Should().BeTrue();
            options.OnClick.Should().NotBeNull();

            _navigationManager.LastNavigationUri.Should().NotBe("./");
            await options.OnClick!(null!);
            _navigationManager.LastNavigationUri.Should().Be("./");
            _navigationManager.ForceLoad.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ApiLocaleUsesSystemDefaultMarker_WHEN_Initialized_THEN_ShouldPersistEnglishWithoutWarning()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            await TestContext.LocalStorage.RemoveItemAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            RenderLayout(new List<IManagedTimer>(), preferences: CreatePreferences(locale: "C"));

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("en");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Language preference changed on server. Click Reload to apply it.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_StoredLocaleUsesSystemDefaultMarker_WHEN_ApiLocaleMatchesNormalizedValue_THEN_ShouldNormalizeStoredLocaleWithoutWarning()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            await TestContext.LocalStorage.SetItemAsStringAsync(_preferredLocaleStorageKey, "C", Xunit.TestContext.Current.CancellationToken);
            RenderLayout(new List<IManagedTimer>(), preferences: CreatePreferences(locale: "en"));

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("en");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Language preference changed on server. Click Reload to apply it.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_UpdateAvailableAndNotDismissed_WHEN_Rendered_THEN_ShowsPersistentUpdateSnackbar()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", "https://example.invalid", DateTime.UtcNow),
                    true,
                    true,
                    DateTime.UtcNow));

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("A new qbtmud build (v1.1.0) is available.", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_UpdateDismissedTagMatchesLatest_WHEN_Rendered_THEN_DoesNotShowUpdateSnackbar()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = false,
                    DismissedReleaseTag = "v1.1.0"
                });
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", "https://example.invalid", DateTime.UtcNow),
                    true,
                    true,
                    DateTime.UtcNow));

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("A new qbtmud build (v1.1.0) is available.", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_UpdateSnackbarDismissAction_WHEN_Clicked_THEN_PersistsDismissedReleaseTag()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            Action<SnackbarOptions>? capturedOptions = null;
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Callback<string, Severity, Action<SnackbarOptions>, string>((_, _, options, _) => capturedOptions = options);

            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_appSettingsService)
                .Setup(service => service.SaveDismissedReleaseTagAsync("v1.1.0", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = false,
                    DismissedReleaseTag = "v1.1.0"
                });
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", "https://example.invalid", DateTime.UtcNow),
                    true,
                    true,
                    DateTime.UtcNow));

            RenderLayout(new List<IManagedTimer>());

            capturedOptions.Should().NotBeNull();
            var options = new SnackbarOptions(Severity.Info, new SnackbarConfiguration());
            capturedOptions!(options);

            options.RequireInteraction.Should().BeTrue();
            options.Action.Should().Be("Dismiss");
            options.OnClick.Should().NotBeNull();

            await options.OnClick!(null!);

            Mock.Get(_appSettingsService)
                .Verify(service => service.SaveDismissedReleaseTagAsync("v1.1.0", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GIVEN_UpdateChecksDisabled_WHEN_Rendered_THEN_DoesNotCallUpdateService()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();
            _appUpdateService.ClearInvocations();

            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    UpdateChecksEnabled = false,
                    NotificationsEnabled = false
                });

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_appUpdateService)
                .Verify(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GIVEN_RefreshSettingsReturnsNull_WHEN_Rendered_THEN_UsesFallbackSettingsForUpdateCheck()
        {
            DisposeDefaultTarget();

            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppSettings)null!);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    UpdateChecksEnabled = false,
                    NotificationsEnabled = false
                });

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_appSettingsService).Verify(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GIVEN_UpdateAvailableWithoutReleaseTag_WHEN_Rendered_THEN_DoesNotShowUpdateSnackbar()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();

            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    latestRelease: null,
                    true,
                    true,
                    DateTime.UtcNow));

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_StartupUpdateCheckCanceledOnDispose_WHEN_Rendered_THEN_SwallowsCancellation()
        {
            DisposeDefaultTarget();
            var updateCheckStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(async (bool _, CancellationToken cancellationToken) =>
                {
                    updateCheckStarted.SetResult(true);
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return null!;
                });

            var target = RenderLayout(new List<IManagedTimer>());

            await updateCheckStarted.Task;

            var action = async () => await target.Instance.DisposeAsync();

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_NullPreferences_WHEN_Initialized_THEN_CompletesWithoutLocaleWarning()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();
            await TestContext.LocalStorage.SetItemAsStringAsync(_preferredLocaleStorageKey, "en", Xunit.TestContext.Current.CancellationToken);
            Mock.Get(_apiClient).Setup(c => c.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync((ClientModels.Preferences)null!);

            TestContext.Render<LoggedInLayout>(parameters =>
            {
                parameters.Add(p => p.Body, builder => { });
                parameters.AddCascadingValue(Breakpoint.Lg);
                parameters.AddCascadingValue(Orientation.Landscape);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("TimerDrawerOpen", false);
                parameters.AddCascadingValue("IsDarkMode", false);
            });

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(_preferredLocaleStorageKey, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("en");
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Language preference changed on server. Click Reload to apply it.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_NoTimers_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsDefault()
        {
            var target = RenderLayout(new List<IManagedTimer>());
            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            button.Instance.Color.Should().Be(Color.Default);
        }

        [Fact]
        public void GIVEN_AllTimersRunning_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsSuccess()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Running)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.Timer);

            button.Instance.Color.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_PausedTimerPresent_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsWarning()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Paused)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.PauseCircle);

            button.Instance.Color.Should().Be(Color.Warning);
        }

        [Fact]
        public void GIVEN_FaultedTimerPresent_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsError()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Faulted)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.Error);

            button.Instance.Color.Should().Be(Color.Error);
        }

        [Fact]
        public void GIVEN_NotAllTimersRunning_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsDefault()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Stopped)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            button.Instance.Color.Should().Be(Color.Default);
        }

        [Fact]
        public void GIVEN_UnknownTimerState_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsDefault()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer((ManagedTimerState)42)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            button.Instance.Color.Should().Be(Color.Default);
        }

        [Fact]
        public void GIVEN_NoTimers_WHEN_Rendered_THEN_TimerTooltipShowsEmptyMessage()
        {
            var target = RenderLayout(new List<IManagedTimer>());
            var tooltip = FindTimerTooltip(target);

            tooltip.Instance.Text.Should().Be("No timers registered.");
        }

        [Fact]
        public void GIVEN_TimersPresent_WHEN_Rendered_THEN_TimerTooltipShowsCounts()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Paused),
                CreateTimer(ManagedTimerState.Stopped),
                CreateTimer(ManagedTimerState.Faulted)
            };

            var target = RenderLayout(timers);

            var tooltip = FindTimerTooltip(target);

            tooltip.Instance.Text.Should().Be("Timers: 1 running, 1 paused, 1 stopped, 1 faulted");
        }

        [Fact]
        public async Task GIVEN_TimerDrawerClosed_WHEN_TimerIconClicked_THEN_CallbackInvoked()
        {
            var opened = false;
            var callback = EventCallback.Factory.Create<bool>(this, value => opened = value);

            var target = RenderLayout(new List<IManagedTimer>(), timerDrawerOpen: false, timerDrawerOpenChanged: callback);
            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            await target.InvokeAsync(() => button.Find("button").Click());

            opened.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoTimerDrawerCallback_WHEN_TimerIconClicked_THEN_TimerDrawerStateToggles()
        {
            var target = RenderLayout(new List<IManagedTimer>(), timerDrawerOpen: false);
            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            await target.InvokeAsync(() => button.Find("button").Click());

            target.Instance.TimerDrawerOpen.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_Unauthenticated_WHEN_Initialized_THEN_NavigatesToLoginAndShowsProgress()
        {
            DisposeDefaultTarget();
            await TestContext.SessionStorage.SetItemAsync(_pendingDownloadStorageKey, "magnet:?xt=urn:btih:ABC", Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_apiClient).Setup(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(false);

            var target = RenderLayout(new List<IManagedTimer>());

            _navigationManager.LastNavigationUri.Should().Be("login");
            _navigationManager.ForceLoad.Should().BeFalse();
            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_StartupAuthCheckFailsWithNoResponse_WHEN_Initialized_THEN_ShowsLostConnectionWithoutNavigatingToLogin()
        {
            DisposeDefaultTarget();
            await TestContext.SessionStorage.SetItemAsync(_pendingDownloadStorageKey, "magnet:?xt=urn:btih:ABC", Xunit.TestContext.Current.CancellationToken);
            _dialogService.ClearInvocations();

            Mock.Get(_apiClient)
                .Setup(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.NoResponse, "Unavailable");

            var target = RenderLayout(new List<IManagedTimer>());

            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            target.WaitForAssertion(() =>
            {
                _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()), Times.Once);
            });
            _navigationManager.LastNavigationUri.Should().BeNull();

            var pending = await TestContext.SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().Be("magnet:?xt=urn:btih:ABC");
        }

        [Fact]
        public void GIVEN_StartupAuthCheckFailsWithApiError_WHEN_Initialized_THEN_ShowsSnackbarWithoutNavigatingToLoginOrLostConnectionDialog()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            _snackbar.ClearInvocations();

            Mock.Get(_apiClient)
                .Setup(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError);

            var target = RenderLayout(new List<IManagedTimer>());

            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            _navigationManager.LastNavigationUri.Should().BeNull();
            Mock.Get(_refreshTimer).Verify(
                timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "qBittorrent returned an error. Please try again.",
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StartupAuthCheckFailsWithApiError_WHEN_RecoveryTickSucceeds_THEN_LoadsShellWithoutFullReload()
        {
            DisposeDefaultTarget();
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;

            Mock.Get(_apiClient).SetupSequence(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError)
                .ReturnsAsync(true);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), body: CreateProbeBody());

            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.WaitForAssertion(() =>
            {
                target.FindComponent<LayoutProbe>().Should().NotBeNull();
            });
            _navigationManager.LastNavigationUri.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_StartupAuthCheckFailsWithApiError_WHEN_RecoveryTickRequiresAuthentication_THEN_NavigatesToLoginAndStops()
        {
            DisposeDefaultTarget();
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;

            Mock.Get(_apiClient).SetupSequence(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError)
                .ReturnsAsync(false);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), body: CreateProbeBody());

            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
            target.WaitForAssertion(() => _navigationManager.LastNavigationUri.Should().Be("login"));
            _navigationManager.ForceLoad.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_StartupAuthCheckFailsWithApiError_WHEN_RecoveryTickLosesConnection_THEN_ShowsLostConnectionAndStops()
        {
            DisposeDefaultTarget();
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;

            Mock.Get(_apiClient).SetupSequence(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError)
                .ReturnsFailure<bool>(ApiFailureKind.NoResponse, "Unavailable");

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), body: CreateProbeBody());

            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StartupAuthCheckFailsWithApiError_WHEN_RecoveryTickFailsAgain_THEN_ContinuesStartupRecovery()
        {
            DisposeDefaultTarget();
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            _snackbar.ClearInvocations();

            Mock.Get(_apiClient).SetupSequence(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError)
                .ReturnsFailure<bool>(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), body: CreateProbeBody());

            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "qBittorrent returned an error. Please try again.",
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    "logged-in-layout-startup-api-error"),
                Times.Exactly(2));
        }

        [Fact]
        public void GIVEN_DataManagerReturnsNullMainData_WHEN_RenderedWithProbe_THEN_ProvidesEmptyTorrentList()
        {
            DisposeDefaultTarget();

            var target = RenderLayout(new List<IManagedTimer>(), body: CreateProbeBody(), configureMainData: false);
            var probe = target.FindComponent<LayoutProbe>();

            probe.Instance.Torrents.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_Rendered_WHEN_PageTitleResolved_THEN_UsesLocalizedWebUiTitleAndVersion()
        {
            DisposeDefaultTarget();

            var target = RenderLayout(new List<IManagedTimer>());
            var pageTitle = target.FindComponent<PageTitle>();

            GetChildContentText(pageTitle.Instance.ChildContent).Should().Be("qBittorrent Version WebUI");
        }

        [Fact]
        public async Task GIVEN_LostConnectionWorkflowMarksLostConnection_WHEN_Rendered_THEN_ShowsLostConnectionDialog()
        {
            _dialogService.ClearInvocations();
            var target = RenderLayout(new List<IManagedTimer>());

            var lostConnectionWorkflow = TestContext.Services.GetRequiredService<ILostConnectionWorkflow>();
            await target.InvokeAsync(() => lostConnectionWorkflow.MarkLostConnectionAsync());

            target.WaitForAssertion(() =>
            {
                _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_LostConnectionWorkflowAlreadyMarkedLost_WHEN_LayoutRerenders_THEN_ShowsLostConnectionDialogOnlyOnce()
        {
            _dialogService.ClearInvocations();
            var target = RenderLayout(new List<IManagedTimer>());
            var lostConnectionWorkflow = TestContext.Services.GetRequiredService<ILostConnectionWorkflow>();

            await target.InvokeAsync(() => lostConnectionWorkflow.MarkLostConnectionAsync());

            target.WaitForAssertion(() =>
            {
                _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()), Times.Once);
            });

            target.Render();

            target.WaitForAssertion(() =>
            {
                _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()), Times.Once);
            });
        }

        [Fact]
        public void GIVEN_StatusLabelsEnabled_WHEN_Rendered_THEN_ShowsLabelText()
        {
            var mainData = CreateMainData(serverState: CreateServerState());

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            GetChildContentText(freeSpace.Instance.ChildContent).Should().StartWith("Free space: ");

            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().Contain("nodes");
        }

        [Fact]
        public void GIVEN_StatusLabelsDisabled_WHEN_Rendered_THEN_OmitsLabelText()
        {
            var mainData = CreateMainData(serverState: CreateServerState());

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            GetChildContentText(freeSpace.Instance.ChildContent).Should().NotStartWith("Free space: ");

            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().NotContain("nodes");
        }

        [Fact]
        public void GIVEN_StatusLabelsWithLandscapeMd_WHEN_Rendered_THEN_ShowsLabelText()
        {
            var mainData = CreateMainData(serverState: CreateServerState());

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, breakpoint: Breakpoint.Md, orientation: Orientation.Landscape);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            GetChildContentText(freeSpace.Instance.ChildContent).Should().StartWith("Free space: ");

            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().Contain("nodes");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndNoAddresses_WHEN_Rendered_THEN_ShowsNotAvailableLabel()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: string.Empty, v6: string.Empty));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IP: N/A");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledWithDualAddresses_WHEN_Rendered_THEN_ShowsBothLabels()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: "1.1.1.1", v6: "2.2.2.2"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IPs: 1.1.1.1, 2.2.2.2");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledWithSingleAddress_WHEN_Rendered_THEN_ShowsSingleLabel()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: "1.1.1.1", v6: string.Empty));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IP: 1.1.1.1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndNoAddressesWithLabelsHidden_WHEN_Rendered_THEN_HidesExternalIp()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: string.Empty, v6: string.Empty));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            HasComponentWithTestId<MudText>(target, "Status-ExternalIp").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndDualAddressesWithLabelsHidden_WHEN_Rendered_THEN_ShowsCombinedValue()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: "1.1.1.1", v6: "2.2.2.2"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("1.1.1.1, 2.2.2.2");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndSingleAddressWithLabelsHidden_WHEN_Rendered_THEN_ShowsSingleValue()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: string.Empty, v6: "2.2.2.2"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("2.2.2.2");
        }

        [Fact]
        public void GIVEN_ConnectionStatusFirewalled_WHEN_Rendered_THEN_ShowsWarningIcon()
        {
            var mainData = CreateMainData(serverState: CreateServerState(connectionStatus: ConnectionStatus.Firewalled));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            tooltip.Instance.Text.Should().Be("Connection status: Firewalled");

            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");
            icon.Instance.Icon.Should().Be(Icons.Material.Outlined.SignalWifiStatusbarConnectedNoInternet4);
            icon.Instance.Color.Should().Be(Color.Warning);
        }

        [Fact]
        public void GIVEN_ConnectionStatusConnected_WHEN_Rendered_THEN_ShowsSuccessIcon()
        {
            var mainData = CreateMainData(serverState: CreateServerState(connectionStatus: ConnectionStatus.Connected));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            tooltip.Instance.Text.Should().Be("Connection status: Connected");

            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");
            icon.Instance.Icon.Should().Be(Icons.Material.Outlined.SignalWifi4Bar);
            icon.Instance.Color.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_ConnectionStatusUnknown_WHEN_Rendered_THEN_ShowsErrorIcon()
        {
            var mainData = CreateMainData(serverState: CreateServerState(connectionStatus: (ConnectionStatus)999));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            tooltip.Instance.Text.Should().Be("999");

            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");
            icon.Instance.Icon.Should().Be(Icons.Material.Outlined.SignalWifiOff);
            icon.Instance.Color.Should().Be(Color.Error);
        }

        [Fact]
        public async Task GIVEN_TorrentQueryStateUpdated_WHEN_FilterAndSortValuesChange_THEN_StateUpdatesAndVersionChanges()
        {
            var torrents = new List<MudTorrent>
            {
                CreateTorrent("Hash1", "Alpha", "Cat1", new[] { "Tag1" }, "Tracker1", TorrentState.Downloading),
                CreateTorrent("Hash2", "Beta", "Cat2", Array.Empty<string>(), "Tracker2", TorrentState.StoppedUploading)
            };
            var mainData = CreateMainData(torrents: torrents, serverState: CreateServerState());
            var queryState = TestContext.Services.GetRequiredService<ITorrentQueryState>();

            var target = RenderLayoutWithProbe(mainData);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            await target.InvokeAsync(() => queryState.SetCategory(FilterHelper.CATEGORY_ALL));
            await target.InvokeAsync(() => queryState.SetStatus(Status.All));
            await target.InvokeAsync(() => queryState.SetTag(FilterHelper.TAG_ALL));
            await target.InvokeAsync(() => queryState.SetTracker(FilterHelper.TRACKER_ALL));
            await target.InvokeAsync(() => queryState.SetSearch(new FilterSearchState(null, TorrentFilterField.Name, false, true)));

            await target.InvokeAsync(() => queryState.SetCategory("Cat1"));
            await target.InvokeAsync(() => queryState.SetStatus(Status.Downloading));
            await target.InvokeAsync(() => queryState.SetTag("Tag1"));
            await target.InvokeAsync(() => queryState.SetTracker("Tracker1"));
            await target.InvokeAsync(() => queryState.SetSearch(new FilterSearchState("Alpha", TorrentFilterField.Name, false, true)));
            await target.InvokeAsync(() => queryState.SetSortColumn("Name"));
            await target.InvokeAsync(() => queryState.SetSortDirection(SortDirection.Descending));

            target.WaitForAssertion(() =>
            {
                probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
            });

            queryState.SortColumn.Should().Be("Name");
            queryState.SortDirection.Should().Be(SortDirection.Descending);
        }

        [Fact]
        public void GIVEN_StartupPreferences_WHEN_Rendered_THEN_SeedsQBittorrentPreferencesStateAndCascadesSnapshot()
        {
            var preferences = CreatePreferences(statusBarExternalIp: true, locale: "en");
            var expected = new PreferencesDataManager().CreateQBittorrentPreferences(preferences);

            var target = RenderLayout(new List<IManagedTimer>(), preferences: preferences, body: CreateProbeBody());

            target.WaitForAssertion(() =>
            {
                var probe = target.FindComponent<LayoutProbe>();
                probe.Instance.Preferences.Should().Be(expected);
                TestContext.Services.GetRequiredService<IQBittorrentPreferencesStateService>().Current.Should().Be(expected);
            });
        }

        [Fact]
        public async Task GIVEN_QBittorrentPreferencesStateChanges_WHEN_Rendered_THEN_CascadedSnapshotAndRefreshIntervalUpdate()
        {
            var preferences = CreatePreferences(locale: "en");
            var updatedPreferences = new PreferencesDataManager().CreateQBittorrentPreferences(preferences) with
            {
                StatusBarExternalIp = true,
                RefreshInterval = 2500
            };
            var target = RenderLayout(new List<IManagedTimer>(), preferences: preferences, body: CreateProbeBody());
            var stateService = TestContext.Services.GetRequiredService<IQBittorrentPreferencesStateService>();
            _refreshTimer.ClearInvocations();

            await target.InvokeAsync(() => stateService.SetPreferences(updatedPreferences));

            target.WaitForAssertion(() =>
            {
                var probe = target.FindComponent<LayoutProbe>();
                probe.Instance.Preferences.Should().Be(updatedPreferences);
                Mock.Get(_refreshTimer).Verify(
                    timer => timer.UpdateIntervalAsync(TimeSpan.FromMilliseconds(2500), It.IsAny<CancellationToken>()),
                    Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleSucceeds_WHEN_Enabled_THEN_UpdatesStateAndShowsSnackbar()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: false));
            _snackbar.ClearInvocations();

            Mock.Get(_apiClient).Setup(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>())).ReturnsSuccess(Task.CompletedTask);
            Mock.Get(_apiClient).Setup(c => c.GetAlternativeSpeedLimitsStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            await target.InvokeAsync(() => button.Find("button").Click());

            mainData.ServerState.UseAltSpeedLimits.Should().BeTrue();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Alternative speed limits: On", It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleSucceeds_WHEN_Disabled_THEN_UpdatesStateAndShowsSnackbar()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: true));
            _snackbar.ClearInvocations();

            Mock.Get(_apiClient).Setup(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>())).ReturnsSuccess(Task.CompletedTask);
            Mock.Get(_apiClient).Setup(c => c.GetAlternativeSpeedLimitsStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(false);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            await target.InvokeAsync(() => button.Find("button").Click());

            mainData.ServerState.UseAltSpeedLimits.Should().BeFalse();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Alternative speed limits: Off", It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleFails_WHEN_Clicked_THEN_ShowsErrorSnackbar()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: false));
            _snackbar.ClearInvocations();

            Mock.Get(_apiClient).Setup(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>())).ReturnsFailure(ApiFailureKind.ServerError, "Fail", HttpStatusCode.InternalServerError);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            await target.InvokeAsync(() => button.Find("button").Click());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to toggle alternative speed limits: Fail", It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleInProgress_WHEN_ClickedAgain_THEN_IgnoresSecondRequest()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: false));
            var toggleTaskSource = new TaskCompletionSource<bool>();
            _snackbar.ClearInvocations();

            Mock.Get(_apiClient).Setup(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>())).ReturnsSuccess(toggleTaskSource.Task);
            Mock.Get(_apiClient).Setup(c => c.GetAlternativeSpeedLimitsStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            var firstClick = target.InvokeAsync(() => button.Find("button").Click());
            await target.InvokeAsync(() => button.Find("button").Click());

            Mock.Get(_apiClient).Verify(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>()), Times.Once);

            toggleTaskSource.SetResult(true);
            await firstClick;
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleThrows_WHEN_Clicked_THEN_AllowsRetry()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: false));

            Mock.Get(_apiClient)
                .Setup(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Toggle failed"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = FindComponentByTestId<MudIconButton>(target, "Status-AltSpeedButton");

            var act = async () => await button.Find("button").ClickAsync(new MouseEventArgs());

            await act.Should().ThrowAsync<InvalidOperationException>();
            button.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_GlobalDownloadRateDialogConfirmed_WHEN_StatusBarIndicatorClicked_THEN_UpdatesStateAndRenderedText()
        {
            var serverState = CreateServerState();
            serverState.DownloadRateLimit = 2048;
            var mainData = CreateMainData(serverState: serverState);

            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalDownloadRateDialog(2048))
                .ReturnsAsync(4096)
                .Verifiable();

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.Find($"[data-test-id='{TestIdHelper.For("Status-DownloadButton")}']");

            await target.InvokeAsync(() => button.Click());

            target.WaitForAssertion(() =>
            {
                mainData.ServerState.DownloadRateLimit.Should().Be(4096);
                Mock.Get(_dialogWorkflow).Verify();

                var text = FindComponentByTestId<MudText>(target, "Status-Download");
                GetChildContentText(text.Instance.ChildContent).Should().Be(
                    $"{DisplayHelpers.Speed(10)} [{DisplayHelpers.Speed(4096)}] ({DisplayHelpers.Size(100)})");
            });
        }

        [Fact]
        public async Task GIVEN_GlobalUploadRateDialogConfirmed_WHEN_StatusBarIndicatorClicked_THEN_UpdatesStateAndRenderedText()
        {
            var serverState = CreateServerState();
            serverState.UploadRateLimit = 1024;
            var mainData = CreateMainData(serverState: serverState);

            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalUploadRateDialog(1024))
                .ReturnsAsync(3072)
                .Verifiable();

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.Find($"[data-test-id='{TestIdHelper.For("Status-UploadButton")}']");

            await target.InvokeAsync(() => button.Click());

            target.WaitForAssertion(() =>
            {
                mainData.ServerState.UploadRateLimit.Should().Be(3072);
                Mock.Get(_dialogWorkflow).Verify();

                var text = FindComponentByTestId<MudText>(target, "Status-Upload");
                GetChildContentText(text.Instance.ChildContent).Should().Be(
                    $"{DisplayHelpers.Speed(20)} [{DisplayHelpers.Speed(3072)}] ({DisplayHelpers.Size(200)})");
            });
        }

        [Fact]
        public async Task GIVEN_GlobalRateDialogsCanceled_WHEN_StatusBarIndicatorsClicked_THEN_LeavesStateUnchanged()
        {
            var serverState = CreateServerState();
            serverState.DownloadRateLimit = 2048;
            serverState.UploadRateLimit = 1024;
            var mainData = CreateMainData(serverState: serverState);

            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalDownloadRateDialog(2048))
                .ReturnsAsync((long?)null)
                .Verifiable();
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalUploadRateDialog(1024))
                .ReturnsAsync((long?)null)
                .Verifiable();

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var downloadButton = target.Find($"[data-test-id='{TestIdHelper.For("Status-DownloadButton")}']");
            var uploadButton = target.Find($"[data-test-id='{TestIdHelper.For("Status-UploadButton")}']");

            await target.InvokeAsync(() => downloadButton.Click());
            await target.InvokeAsync(() => uploadButton.Click());

            target.WaitForAssertion(() =>
            {
                mainData.ServerState.DownloadRateLimit.Should().Be(2048);
                mainData.ServerState.UploadRateLimit.Should().Be(1024);
                Mock.Get(_dialogWorkflow).Verify();
            });
        }

        [Fact]
        public async Task GIVEN_GlobalRateDialogFails_WHEN_StatusBarIndicatorsClicked_THEN_ShowsErrorSnackbarAndLeavesStateUnchanged()
        {
            var serverState = CreateServerState();
            serverState.DownloadRateLimit = 2048;
            serverState.UploadRateLimit = 1024;
            var mainData = CreateMainData(serverState: serverState);
            _snackbar.ClearInvocations();

            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalDownloadRateDialog(2048))
                .ThrowsAsync(new HttpRequestException("DownloadFail"));
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.InvokeGlobalUploadRateDialog(1024))
                .ThrowsAsync(new HttpRequestException("UploadFail"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var downloadButton = target.Find($"[data-test-id='{TestIdHelper.For("Status-DownloadButton")}']");
            var uploadButton = target.Find($"[data-test-id='{TestIdHelper.For("Status-UploadButton")}']");

            await target.InvokeAsync(() => downloadButton.Click());
            await target.InvokeAsync(() => uploadButton.Click());

            target.WaitForAssertion(() =>
            {
                mainData.ServerState.DownloadRateLimit.Should().Be(2048);
                mainData.ServerState.UploadRateLimit.Should().Be(1024);
            });
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to set global download rate limit: DownloadFail", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to set global upload rate limit: UploadFail", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_RefreshIntervalZero_WHEN_Initialized_THEN_DoesNotUpdateInterval()
        {
            var mainData = CreateMainData(serverState: CreateServerState());
            mainData.ServerState.RefreshInterval = 0;
            _refreshTimer.ClearInvocations();

            RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(refreshInterval: 0));

            Mock.Get(_refreshTimer).Verify(t => t.UpdateIntervalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GIVEN_AuthenticationStillPending_WHEN_Rendered_THEN_DoesNotStartRefreshLoop()
        {
            var tickSource = new TaskCompletionSource<bool>();

            Mock.Get(_apiClient).Setup(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccess(tickSource.Task);

            var target = RenderLayout(new List<IManagedTimer>());

            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            Mock.Get(_refreshTimer).Verify(
                timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_AuthenticationCompletesAfterFirstRender_WHEN_MainLoopStarts_THEN_NoResponseTickMarksLostConnection()
        {
            var tickSource = new TaskCompletionSource<bool>();
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());

            Mock.Get(_apiClient).Setup(c => c.CheckAuthStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccess(tickSource.Task);
            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData())
                .ReturnsFailure(ApiFailureKind.NoResponse, "qBittorrent client is not reachable.");

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), body: probeBody);

            Mock.Get(_refreshTimer).Verify(
                timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            tickSource.SetResult(true);

            target.WaitForAssertion(() =>
            {
                target.FindComponent<LayoutProbe>().Should().NotBeNull();
                handler.Should().NotBeNull();
            });

            var probe = target.FindComponent<LayoutProbe>();

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshTickNoResponse_WHEN_Ticked_THEN_LostConnectionSetAndStops()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            _dialogService.ClearInvocations();

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData())
                .ReturnsFailure(ApiFailureKind.NoResponse, "qBittorrent client is not reachable.");

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshTickApiError_WHEN_Ticked_THEN_ShowsSnackbarAndContinuesWithoutLostConnectionDialog()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            _dialogService.ClearInvocations();
            _snackbar.ClearInvocations();

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData())
                .ReturnsFailure(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError);

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "qBittorrent returned an error. Please try again.",
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshTickUnauthorized_WHEN_Ticked_THEN_NavigatesToLoginWithoutLostConnectionDialog()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            _dialogService.ClearInvocations();

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Unauthorized", HttpStatusCode.Unauthorized);

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
            target.WaitForAssertion(() => _navigationManager.LastNavigationUri.Should().Be("login"));
            _navigationManager.ForceLoad.Should().BeTrue();
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshTickForbidden_WHEN_Ticked_THEN_NavigatesToLoginWithoutLostConnectionDialog()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            _dialogService.ClearInvocations();

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Forbidden", HttpStatusCode.Forbidden);

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
            target.WaitForAssertion(() => _navigationManager.LastNavigationUri.Should().Be("login"));
            _navigationManager.ForceLoad.Should().BeTrue();
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshTickFullUpdate_WHEN_Ticked_THEN_RecreatesMainData()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var initialData = CreateMainData(serverState: CreateServerState());
            var updatedData = CreateMainData(serverState: CreateServerState(connectionStatus: ConnectionStatus.Connected));

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: true));

            Mock.Get(_dataManager).SetupSequence(m => m.CreateMainData(It.IsAny<ClientModels.MainData>()))
                .Returns(initialData)
                .Returns(updatedData);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: initialData, body: probeBody, configureMainData: false);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);

            target.Render();
            probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
        }

        [Fact]
        public async Task GIVEN_RefreshTickMergeFilterChanged_WHEN_Ticked_THEN_MarksDirty()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            var filterChanged = true;

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false));

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            IReadOnlyList<TorrentTransition> transitions = Array.Empty<TorrentTransition>();
            Mock.Get(_dataManager).Setup(m => m.MergeMainData(It.IsAny<ClientModels.MainData>(), mainData, out filterChanged, out transitions)).Returns(false);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            target.WaitForAssertion(() => handler.Should().NotBeNull());
            target.WaitForAssertion(() => target.FindComponents<MudProgressLinear>().Should().BeEmpty());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.Render();
            probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
        }

        [Fact]
        public async Task GIVEN_RefreshTickMergeDataChanged_WHEN_Ticked_THEN_IncrementsVersion()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            var filterChanged = false;

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false));

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            IReadOnlyList<TorrentTransition> transitions = Array.Empty<TorrentTransition>();
            Mock.Get(_dataManager).Setup(m => m.MergeMainData(It.IsAny<ClientModels.MainData>(), mainData, out filterChanged, out transitions)).Returns(true);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            target.WaitForAssertion(() => handler.Should().NotBeNull());
            target.WaitForAssertion(() => target.FindComponents<MudProgressLinear>().Should().BeEmpty());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.Render();
            probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
        }

        [Fact]
        public void GIVEN_InitializedLayout_WHEN_Rendered_THEN_DoesNotProcessTorrentCompletionNotifications()
        {
            DisposeDefaultTarget();
            _torrentCompletionNotificationService.ClearInvocations();

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_torrentCompletionNotificationService).Verify(
                service => service.ProcessTransitionsAsync(It.IsAny<IReadOnlyList<TorrentTransition>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshTick_WHEN_Ticked_THEN_ProcessesTorrentCompletionNotifications()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var mainData = CreateMainData(serverState: CreateServerState());
            var filterChanged = false;

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false));

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            IReadOnlyList<TorrentTransition> transitions =
            [
                new TorrentTransition("Hash1", "Name1", false, false, true)
            ];
            Mock.Get(_dataManager).Setup(m => m.MergeMainData(It.IsAny<ClientModels.MainData>(), mainData, out filterChanged, out transitions)).Returns(true);

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            _torrentCompletionNotificationService.ClearInvocations();

            handler.Should().NotBeNull();
            await handler!(CancellationToken.None);

            Mock.Get(_torrentCompletionNotificationService).Verify(
                service => service.ProcessTransitionsAsync(It.Is<IReadOnlyList<TorrentTransition>>(batch => batch.Count == 1), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshTickNotificationProcessingFails_WHEN_Ticked_THEN_Continues()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var mainData = CreateMainData(serverState: CreateServerState());
            var filterChanged = false;

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false));

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            IReadOnlyList<TorrentTransition> transitions =
            [
                new TorrentTransition("Hash1", "Name1", false, false, true)
            ];
            Mock.Get(_dataManager).Setup(m => m.MergeMainData(It.IsAny<ClientModels.MainData>(), mainData, out filterChanged, out transitions)).Returns(true);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.ProcessTransitionsAsync(It.IsAny<IReadOnlyList<TorrentTransition>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            handler.Should().NotBeNull();
            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            Mock.Get(_torrentCompletionNotificationService).Verify(
                service => service.ProcessTransitionsAsync(It.IsAny<IReadOnlyList<TorrentTransition>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("magnet:?dn=missing")]
        [InlineData("http://example.com/file.torrent\n")]
        [InlineData("not a uri")]
        [InlineData("ftp://example.com/file.torrent")]
        [InlineData("http://example.com/file.txt")]
        public async Task GIVEN_InvalidPendingDownload_WHEN_Initialized_THEN_Removed(string pending)
        {
            await TestContext.SessionStorage.SetItemAsync(_pendingDownloadStorageKey, pending, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                var stored = TestContext.SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                stored.Should().BeNull();
            });
        }

        [Fact]
        public async Task GIVEN_PendingDownloadTooLong_WHEN_Initialized_THEN_Removed()
        {
            var pending = new string('a', 8200);
            await TestContext.SessionStorage.SetItemAsync(_pendingDownloadStorageKey, pending, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                var stored = TestContext.SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                stored.Should().BeNull();
            });
        }

        [Fact]
        public async Task GIVEN_ValidPendingMagnet_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            await TestContext.SessionStorage.SetItemAsync(_pendingDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_dialogWorkflow).Setup(d => d.InvokeAddTorrentLinkDialog(magnet)).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(magnet), Times.Once);
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            var lastProcessed = await TestContext.SessionStorage.GetItemAsync<string>(_lastProcessedDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
            lastProcessed.Should().Be(magnet);
        }

        [Fact]
        public async Task GIVEN_ValidPendingTorrentLink_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var link = "http://example.com/file.torrent";
            await TestContext.SessionStorage.SetItemAsync(_pendingDownloadStorageKey, link, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_dialogWorkflow).Setup(d => d.InvokeAddTorrentLinkDialog(link)).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(link), Times.Once);
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            var lastProcessed = await TestContext.SessionStorage.GetItemAsync<string>(_lastProcessedDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
            lastProcessed.Should().Be(link);
        }

        [Fact]
        public async Task GIVEN_PendingMatchesProcessed_WHEN_Initialized_THEN_ClearsPendingAndNavigates()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            await TestContext.SessionStorage.SetItemAsync(_pendingDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);
            await TestContext.SessionStorage.SetItemAsync(_lastProcessedDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);

            RenderLayout(new List<IManagedTimer>());

            _navigationManager.LastNavigationUri.Should().Be("./");
            _navigationManager.ForceLoad.Should().BeTrue();
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
        }

        [Fact]
        public void GIVEN_DownloadInFragment_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/#download=magnet:?xt=urn:btih:ABC");
            Mock.Get(_dialogWorkflow).Setup(d => d.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC")).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC"), Times.Once);
        }

        [Fact]
        public void GIVEN_DownloadInHashRouteQuery_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/#/?download=magnet:?xt=urn:btih:ABC");
            Mock.Get(_dialogWorkflow).Setup(d => d.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC")).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC"), Times.Once);
        }

        [Fact]
        public void GIVEN_DownloadInQuery_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/?download=http://example.com/file.torrent");
            Mock.Get(_dialogWorkflow).Setup(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent")).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent"), Times.Once);
        }

        [Fact]
        public void GIVEN_DownloadKeyWithoutValue_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/#download");

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_DownloadValueDecodedWhitespace_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetRawUri("http://localhost/?download=%20");

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DownloadAlreadyProcessed_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            await TestContext.SessionStorage.SetItemAsync(_lastProcessedDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);
            _navigationManager.SetUri("http://localhost/#download=magnet:?xt=urn:btih:ABC");

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_InvalidDownloadValue_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/?download=http://example.com/file.txt");

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_FragmentDelimiterOnly_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/#");

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_QueryWithoutDownload_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/?view=all");

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_DialogWorkflowThrows_WHEN_Initialized_THEN_Throws()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            _navigationManager.SetUri($"http://localhost/#download={magnet}");
            Mock.Get(_dialogWorkflow).Setup(d => d.InvokeAddTorrentLinkDialog(magnet))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            Action action = () => RenderLayout(new List<IManagedTimer>());

            action.Should().Throw<Exception>();
        }

        [Fact]
        public async Task GIVEN_NavigationAfterAuth_WHEN_LocationChanges_THEN_ProcessesDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            Mock.Get(_dialogWorkflow).Setup(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent")).Returns(Task.CompletedTask);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() => target.FindComponent<MudAppBar>().Should().NotBeNull());

            await target.InvokeAsync(() => _navigationManager.NavigateTo("http://localhost/?download=http://example.com/file.torrent"));

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogWorkflow).Verify(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent"), Times.AtLeastOnce);
            });
        }

        [Fact]
        public async Task GIVEN_PwaPromptDelayScheduled_WHEN_Disposed_THEN_DoesNotThrow()
        {
            DisposeDefaultTarget();
            _dialogService.ClearInvocations();
            Mock.Get(_welcomeWizardPlanBuilder)
                .Setup(service => service.BuildPlanAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardPlan(isReturningUser: true, pendingSteps: Array.Empty<WelcomeWizardStepDefinition>()));

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForState(() => target.FindComponents<MudAppBar>().Any());

            var action = async () => await target.Instance.DisposeAsync();

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public void GIVEN_MenuProvided_WHEN_Initialized_THEN_MenuShown()
        {
            var menu = TestContext.Render<Menu>();
            var target = RenderLayout(new List<IManagedTimer>(), menu: menu.Instance);

            target.WaitForAssertion(() => menu.FindComponents<MudMenu>().Should().NotBeEmpty());
        }

        [Fact]
        public async Task GIVEN_DefaultRender_WHEN_Disposed_THEN_Disposes()
        {
            var target = RenderLayout(new List<IManagedTimer>());

            await target.Instance.DisposeAsync();

            await target.Instance.DisposeAsync();
        }

        [Fact]
        public async Task GIVEN_DisposedLayout_WHEN_Disposed_THEN_DisposesRefreshTimer()
        {
            DisposeDefaultTarget();
            var timer = new Mock<IManagedTimer>();
            timer.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
            Mock.Get(_managedTimerFactory).Setup(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<int>())).Returns(timer.Object);

            var target = RenderLayout(new List<IManagedTimer>());

            await target.Instance.DisposeAsync();

            timer.Verify(t => t.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ToggleInProgress_WHEN_ClickedAgain_THEN_DoesNotCallApiTwice()
        {
            DisposeDefaultTarget();
            var completionSource = new TaskCompletionSource<bool>();
            Mock.Get(_apiClient).Setup(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>())).ReturnsSuccess(completionSource.Task);
            Mock.Get(_apiClient).Setup(c => c.GetAlternativeSpeedLimitsStateAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(true);

            var target = RenderLayout(new List<IManagedTimer>());
            var button = target.FindComponents<MudIconButton>()
                .Single(component => component.Instance.Icon == Icons.Material.Outlined.Speed);

            var firstClick = button.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());

            target.WaitForAssertion(() => Mock.Get(_apiClient).Verify(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>()), Times.Once));

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(null));

            Mock.Get(_apiClient).Verify(c => c.ToggleAlternativeSpeedLimitsAsync(It.IsAny<CancellationToken>()), Times.Once);

            completionSource.SetResult(true);
            await firstClick;
        }

        [Fact]
        public void GIVEN_UpdateStatusThrows_WHEN_Rendered_THEN_SwallowsExceptionWithoutSnackbar()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("A new qbtmud build (v1.1.0) is available.", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_UpdateStatusCanceled_WHEN_Rendered_THEN_SwallowsCancellationWithoutSnackbar()
        {
            DisposeDefaultTarget();
            _snackbar.ClearInvocations();
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((bool _, CancellationToken cancellationToken) => Task.FromCanceled<AppUpdateStatus>(cancellationToken));

            RenderLayout(new List<IManagedTimer>());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("A new qbtmud build (v1.1.0) is available.", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshTickNotificationProcessingCanceled_WHEN_Ticked_THEN_RethrowsCancellation()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var mainData = CreateMainData(serverState: CreateServerState());
            var filterChanged = false;

            Mock.Get(_apiClient).SetupSequence(c => c.GetMainDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false));

            Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            IReadOnlyList<TorrentTransition> transitions =
            [
                new TorrentTransition("Hash1", "Name1", false, false, true)
            ];
            Mock.Get(_dataManager).Setup(m => m.MergeMainData(It.IsAny<ClientModels.MainData>(), mainData, out filterChanged, out transitions)).Returns(true);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.ProcessTransitionsAsync(It.IsAny<IReadOnlyList<TorrentTransition>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyList<TorrentTransition> _, CancellationToken cancellationToken) => Task.FromCanceled(cancellationToken));

            Mock.Get(_refreshTimer)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            handler.Should().NotBeNull();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var action = async () => await handler!(cancellationTokenSource.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public void GIVEN_BlankVersion_WHEN_PageTitleResolved_THEN_OmitsVersionText()
        {
            DisposeDefaultTarget();
            Mock.Get(_apiClient).Setup(c => c.GetApplicationVersionAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(" ");

            var target = RenderLayout(new List<IManagedTimer>());
            var pageTitle = target.FindComponent<PageTitle>();

            GetChildContentText(pageTitle.Instance.ChildContent).Should().Be("qBittorrent WebUI");
        }

        [Fact]
        public void GIVEN_RefreshTimerMissingDuringInitialization_WHEN_Rendered_THEN_CreatesTimerOnFirstRender()
        {
            DisposeDefaultTarget();
            var deferredRefreshTimer = new Mock<IManagedTimer>(MockBehavior.Strict);
            deferredRefreshTimer
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            deferredRefreshTimer
                .Setup(t => t.DisposeAsync())
                .Returns(ValueTask.CompletedTask);
            Mock.Get(_managedTimerFactory)
                .SetupSequence(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<int>()))
                .Returns((IManagedTimer)null!)
                .Returns(deferredRefreshTimer.Object);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                deferredRefreshTimer.Verify(
                    t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });
        }

        private IRenderedComponent<LoggedInLayout> RenderLayout(
            IReadOnlyList<IManagedTimer> timers,
            MudMainData? mainData = null,
            ClientModels.Preferences? preferences = null,
            Breakpoint breakpoint = Breakpoint.Lg,
            Orientation orientation = Orientation.Landscape,
            bool isDarkMode = false,
            bool timerDrawerOpen = false,
            EventCallback<bool>? timerDrawerOpenChanged = null,
            RenderFragment? body = null,
            Menu? menu = null,
            bool configureMainData = true)
        {
            return RenderLayout(
                TestContext,
                timers,
                mainData,
                preferences,
                breakpoint,
                orientation,
                isDarkMode,
                timerDrawerOpen,
                timerDrawerOpenChanged,
                body,
                menu,
                configureMainData);
        }

        private IRenderedComponent<LoggedInLayout> RenderLayout(
            ComponentTestContext context,
            IReadOnlyList<IManagedTimer> timers,
            MudMainData? mainData = null,
            ClientModels.Preferences? preferences = null,
            Breakpoint breakpoint = Breakpoint.Lg,
            Orientation orientation = Orientation.Landscape,
            bool isDarkMode = false,
            bool timerDrawerOpen = false,
            EventCallback<bool>? timerDrawerOpenChanged = null,
            RenderFragment? body = null,
            Menu? menu = null,
            bool configureMainData = true)
        {
            Mock.Get(_timerRegistry).Setup(r => r.GetTimers()).Returns(timers);
            if (configureMainData)
            {
                Mock.Get(_dataManager).Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData ?? CreateMainData());
            }
            Mock.Get(_apiClient).Setup(c => c.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(preferences ?? CreatePreferences());

            return context.Render<LoggedInLayout>(parameters =>
            {
                parameters.Add(p => p.Body, body ?? (builder => { }));
                parameters.AddCascadingValue(breakpoint);
                parameters.AddCascadingValue(orientation);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("TimerDrawerOpen", timerDrawerOpen);
                if (timerDrawerOpenChanged.HasValue)
                {
                    parameters.AddCascadingValue("TimerDrawerOpenChanged", timerDrawerOpenChanged.Value);
                }
                parameters.AddCascadingValue("IsDarkMode", isDarkMode);
                if (menu is not null)
                {
                    parameters.AddCascadingValue(menu);
                }
            });
        }

        private IRenderedComponent<LoggedInLayout> RenderLayoutWithProbe(MudMainData mainData)
        {
            var body = CreateProbeBody();
            return RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: body);
        }

        private static RenderFragment CreateProbeBody()
        {
            return builder =>
            {
                builder.OpenComponent<LayoutProbe>(0);
                builder.CloseComponent();
            };
        }

        private void DisposeDefaultTarget()
        {
        }

        private void ResetDialogInvocations()
        {
            _dialogWorkflow.ClearInvocations();
        }

        private static async Task WaitForDurationAsync(TimeSpan duration)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < duration)
            {
                await Task.Yield();
            }
        }

        private static IRenderedComponent<MudIconButton> FindTimerButton(IRenderedComponent<LoggedInLayout> target, string icon)
        {
            target.WaitForState(() => target.FindComponents<MudIconButton>().Any());

            return target.FindComponents<MudIconButton>().Single(button => button.Instance.Icon == icon);
        }

        private static IRenderedComponent<MudTooltip> FindTimerTooltip(IRenderedComponent<LoggedInLayout> target)
        {
            return target.FindComponents<MudTooltip>()
                .Single(t =>
                {
                    var text = t.Instance.Text ?? string.Empty;
                    return text == "No timers registered."
                        || text.StartsWith("Timers:", StringComparison.Ordinal);
                });
        }

        private static bool HasComponentWithTestId<TComponent>(IRenderedComponent<LoggedInLayout> target, string testId)
            where TComponent : MudComponentBase
        {
            var expected = TestIdHelper.For(testId);
            return target.FindComponents<TComponent>().Any(component =>
            {
                if (component.Instance.UserAttributes is null)
                {
                    return false;
                }

                return component.Instance.UserAttributes.TryGetValue("data-test-id", out var value)
                    && string.Equals(value?.ToString(), expected, StringComparison.Ordinal);
            });
        }

        private static IDialogReference CreateDialogReference(DialogResult? result)
        {
            return CreateDialogReference(Task.FromResult(result));
        }

        private static IDialogReference CreateDialogReference(Task<DialogResult?> resultTask)
        {
            var reference = new Mock<IDialogReference>(MockBehavior.Strict);
            reference.SetupGet(dialogReference => dialogReference.Result).Returns(resultTask);
            return reference.Object;
        }

        private static IManagedTimer CreateTimer(ManagedTimerState state)
        {
            var timer = new Mock<IManagedTimer>();
            timer.SetupGet(t => t.State).Returns(state);
            timer.SetupGet(t => t.Name).Returns("Name");
            timer.SetupGet(t => t.Interval).Returns(TimeSpan.FromSeconds(1));
            return timer.Object;
        }

        private static MudMainData CreateMainData(
            IEnumerable<MudTorrent>? torrents = null,
            MudServerState? serverState = null)
        {
            var torrentList = torrents?.ToDictionary(t => t.Hash, t => t) ?? new Dictionary<string, MudTorrent>();
            return new MudMainData(
                torrentList,
                Array.Empty<string>(),
                new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState ?? CreateServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
        }

        private static MudServerState CreateServerState(
            ConnectionStatus? connectionStatus = ConnectionStatus.Connected,
            string v4 = "1.1.1.1",
            string v6 = "2.2.2.2",
            bool useAltSpeedLimits = false)
        {
            return new MudServerState
            {
                ConnectionStatus = connectionStatus,
                DHTNodes = 1,
                DownloadInfoData = 100,
                DownloadInfoSpeed = 10,
                DownloadRateLimit = 0,
                UploadInfoData = 200,
                UploadInfoSpeed = 20,
                UploadRateLimit = 100,
                FreeSpaceOnDisk = 1024,
                RefreshInterval = 1500,
                UseAltSpeedLimits = useAltSpeedLimits,
                LastExternalAddressV4 = v4,
                LastExternalAddressV6 = v6
            };
        }

        private static MudTorrent CreateTorrent(string hash, string name, string category, IReadOnlyCollection<string> tags, string tracker, TorrentState? state)
        {
            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                availability: 0,
                category: category,
                completed: 0,
                completionOn: 0,
                contentPath: "ContentPath",
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: "InfoHashV1",
                infoHashV2: "InfoHashV2",
                lastActivity: 0,
                magnetUri: "MagnetUri",
                maxRatio: 0,
                maxSeedingTime: 0,
                name: name,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath: "SavePath",
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state: state,
                superSeeding: false,
                tags: tags.ToArray(),
                timeActive: 0,
                totalSize: 0,
                tracker: tracker,
                trackersCount: 0,
                hasTrackerError: false,
                hasTrackerWarning: false,
                hasOtherAnnounceError: false,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed: 0,
                reannounce: 0,
                inactiveSeedingTimeLimit: 0,
                maxInactiveSeedingTime: 0,
                popularity: 0,
                downloadPath: "DownloadPath",
                rootPath: "RootPath",
                isPrivate: false,
                shareLimitAction: ClientModels.ShareLimitAction.Default,
                comment: "Comment");
        }

        private static ClientModels.MainData CreateClientMainData(bool fullUpdate = true)
        {
            return new ClientModels.MainData(1, fullUpdate, null, null, null, null, null, null, null, null, null);
        }

        private static ClientModels.Preferences CreatePreferences(bool statusBarExternalIp = false, string? locale = null, int refreshInterval = 1500)
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.Locale = locale!;
                spec.RefreshInterval = refreshInterval;
                spec.RssProcessingEnabled = false;
                spec.StatusBarExternalIp = statusBarExternalIp;
            });
        }

        private sealed class LayoutProbe : ComponentBase
        {
            [CascadingParameter]
            public IReadOnlyList<MudTorrent>? Torrents { get; set; }

            [CascadingParameter(Name = "TorrentsVersion")]
            public int TorrentsVersion { get; set; }

            [CascadingParameter]
            public MudMainData? MudMainData { get; set; }

            [CascadingParameter]
            public QBittorrentPreferences? Preferences { get; set; }
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            public string? LastNavigationUri { get; private set; }

            public bool ForceLoad { get; private set; }

            public void SetUri(string uri)
            {
                Uri = ToAbsoluteUri(uri).ToString();
            }

            public void SetRawUri(string uri)
            {
                Uri = uri;
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastNavigationUri = uri;
                ForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
                NotifyLocationChanged(false);
            }
        }
    }
}
