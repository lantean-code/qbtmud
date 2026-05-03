using System.Net;
using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using ClientMainData = QBittorrent.ApiClient.Models.MainData;
using ClientPreferences = QBittorrent.ApiClient.Models.Preferences;
using MudMainData = Lantean.QBTMud.Core.Models.MainData;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Presentation.Test.Services
{
    public sealed class ShellSessionWorkflowTests
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>(MockBehavior.Strict);
        private readonly ITorrentDataManager _dataManager = Mock.Of<ITorrentDataManager>(MockBehavior.Strict);
        private readonly ISnackbar _snackbar = Mock.Of<ISnackbar>(MockBehavior.Loose);
        private readonly TestLocalStorageService _settingsStorageService = new();
        private readonly ILanguageLocalizer _languageLocalizer = Mock.Of<ILanguageLocalizer>();
        private readonly ISpeedHistoryService _speedHistoryService = Mock.Of<ISpeedHistoryService>(MockBehavior.Strict);
        private readonly IAppSettingsService _appSettingsService = Mock.Of<IAppSettingsService>(MockBehavior.Strict);
        private readonly ITorrentCompletionNotificationService _torrentCompletionNotificationService = Mock.Of<ITorrentCompletionNotificationService>(MockBehavior.Strict);
        private readonly TestNavigationManager _navigationManager = new();
        private readonly ISnackbarWorkflow _snackbarWorkflow;

        public ShellSessionWorkflowTests()
        {
            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] arguments) => FormatLocalizedString(source, arguments));
            _snackbarWorkflow = new SnackbarWorkflow(_languageLocalizer, _snackbar);
        }

        [Fact]
        public async Task GIVEN_AuthStateFalse_WHEN_LoadingShellSession_THEN_ShouldReturnAuthenticationRequired()
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(false);

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.AuthenticationRequired);
            Mock.Get(_apiClient)
                .Verify(client => client.InitializeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ConnectivityFailureDuringAuthCheck_WHEN_LoadingShellSession_THEN_ShouldReturnLostConnection()
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, bool>(ApiFailureKind.NoResponse, "Unavailable");

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.LostConnection);
        }

        [Fact]
        public async Task GIVEN_ServerErrorDuringAuthCheck_WHEN_LoadingShellSession_THEN_ShouldReturnRetryableFailureAndShowSnackbar()
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, bool>(ApiFailureKind.ServerError, "Failure", HttpStatusCode.InternalServerError);

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.RetryableFailure);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("qBittorrent returned an error. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), "logged-in-layout-startup-api-error"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AuthenticatedSession_WHEN_LoadingShellSession_THEN_ShouldInitializeBeforeLoadingStartupData()
        {
            var target = CreateTarget();
            var mainData = CreateMainData();
            var initializationStep = 0;
            Mock.Get(_apiClient)
                .Setup(client => client.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(true);
            Mock.Get(_apiClient)
                .Setup(client => client.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    initializationStep.Should().Be(0);
                    initializationStep = 1;
                    return Task.FromResult(ApiResult.CreateSuccess());
                });
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    initializationStep.Should().Be(1);
                    return Task.FromResult(AppSettings.Default.Clone());
                });
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    initializationStep.Should().Be(1);
                    return Task.FromResult(ApiResult.CreateSuccess(CreatePreferences("en")));
                });
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    initializationStep.Should().Be(1);
                    return Task.FromResult(ApiResult.CreateSuccess("Version"));
                });
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(0, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    initializationStep.Should().Be(1);
                    return Task.FromResult(ApiResult.CreateSuccess(CreateClientMainData()));
                });
            Mock.Get(_dataManager)
                .Setup(manager => manager.CreateMainData(It.IsAny<ClientMainData>()))
                .Returns(mainData);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.Ready);
            initializationStep.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_InitializationRequiresAuthentication_WHEN_LoadingShellSession_THEN_ShouldReturnAuthenticationRequired()
        {
            var target = CreateTarget();
            SetupAuthenticatedSessionWithoutInitialization();
            Mock.Get(_apiClient)
                .Setup(client => client.InitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient>(ApiFailureKind.AuthenticationRequired, "Unauthorized", HttpStatusCode.Forbidden);

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.AuthenticationRequired);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_InitializationLosesConnection_WHEN_LoadingShellSession_THEN_ShouldReturnLostConnection()
        {
            var target = CreateTarget();
            SetupAuthenticatedSessionWithoutInitialization();
            Mock.Get(_apiClient)
                .Setup(client => client.InitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient>(ApiFailureKind.NoResponse, "Unavailable");

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.LostConnection);
        }

        [Fact]
        public async Task GIVEN_InitializationReturnsApiError_WHEN_LoadingShellSession_THEN_ShouldReturnRetryableFailureAndShowSnackbar()
        {
            var target = CreateTarget();
            SetupAuthenticatedSessionWithoutInitialization();
            Mock.Get(_apiClient)
                .Setup(client => client.InitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient>(ApiFailureKind.ServerError, "Failure", HttpStatusCode.InternalServerError);

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.RetryableFailure);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("qBittorrent returned an error. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), "logged-in-layout-startup-api-error"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_InitializationThrows_WHEN_LoadingShellSession_THEN_ShouldReturnRetryableFailureAndShowSnackbar()
        {
            var target = CreateTarget();
            SetupAuthenticatedSessionWithoutInitialization();
            Mock.Get(_apiClient)
                .Setup(client => client.InitializeAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.RetryableFailure);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("qBittorrent returned an error. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), "logged-in-layout-startup-api-error"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_InitializationIsCanceled_WHEN_LoadingShellSession_THEN_ShouldRethrowCancellation()
        {
            var target = CreateTarget();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            SetupAuthenticatedSessionWithoutInitialization();
            Mock.Get(_apiClient)
                .Setup(client => client.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromCanceled<ApiResult>(cancellationTokenSource.Token));

            var action = async () => await target.LoadAsync(cancellationTokenSource.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SuccessfulLoadAndMissingRefreshedSettings_WHEN_LoadingShellSession_THEN_ShouldUseFallbackSettingsAndPersistLocale()
        {
            var target = CreateTarget();
            var mainData = CreateMainData(downloadSpeed: 10, uploadSpeed: 20);
            var settings = AppSettings.Default.Clone();
            SetupAuthenticatedSession();
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreatePreferences("fr"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync("Version");
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData(responseId: 9, fullUpdate: true));
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppSettings)null!);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);
            Mock.Get(_dataManager)
                .Setup(manager => manager.CreateMainData(It.IsAny<ClientMainData>()))
                .Returns(mainData);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), 10, 20, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.Ready);
            result.AppSettings.Should().BeSameAs(settings);
            result.Version.Should().Be("Version");
            result.MainData.Should().BeSameAs(mainData);
            result.RequestId.Should().Be(9);
            var storedLocale = await _settingsStorageService.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("fr");
            Mock.Get(_appSettingsService)
                .Verify(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_speedHistoryService)
                .Verify(service => service.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_speedHistoryService)
                .Verify(service => service.PushSampleAsync(It.IsAny<DateTime>(), 10, 20, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_BlankApiLocaleAndStoredLocale_WHEN_LoadingShellSession_THEN_ShouldClearStoredLocale()
        {
            var target = CreateTarget();
            var mainData = CreateMainData();
            await _settingsStorageService.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, "en", Xunit.TestContext.Current.CancellationToken);
            SetupAuthenticatedSession();
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreatePreferences(" "));
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync("Version");
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData());
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_dataManager)
                .Setup(manager => manager.CreateMainData(It.IsAny<ClientMainData>()))
                .Returns(mainData);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            var storedLocale = await _settingsStorageService.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_MismatchedStoredLocale_WHEN_LoadingShellSession_THEN_ShouldShowReloadAction()
        {
            var target = CreateTarget();
            var mainData = CreateMainData();
            await _settingsStorageService.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, "en", Xunit.TestContext.Current.CancellationToken);
            Action<SnackbarOptions>? capturedOptions = null;
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
                .Callback<string, Severity, Action<SnackbarOptions>, string>((_, _, configure, _) => capturedOptions = configure);
            SetupAuthenticatedSession();
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreatePreferences("fr"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync("Version");
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData());
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_dataManager)
                .Setup(manager => manager.CreateMainData(It.IsAny<ClientMainData>()))
                .Returns(mainData);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            capturedOptions.Should().NotBeNull();
            var options = new SnackbarOptions(Severity.Warning, new SnackbarConfiguration());
            capturedOptions!(options);
            options.Action.Should().Be("Reload");
            options.OnClick.Should().NotBeNull();
            await options.OnClick!(null!);
            _navigationManager.LastNavigationUri.Should().Be("./");
            _navigationManager.ForceLoad.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RecoveryLoad_WHEN_RunningRecoverAsync_THEN_ShouldUseProvidedRequestId()
        {
            var target = CreateTarget();
            var mainData = CreateMainData();
            SetupAuthenticatedSession();
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreatePreferences("en"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync("Version");
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(42, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData(responseId: 42));
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            Mock.Get(_dataManager)
                .Setup(manager => manager.CreateMainData(It.IsAny<ClientMainData>()))
                .Returns(mainData);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await target.RecoverAsync(42, Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.Ready);
            result.RequestId.Should().Be(42);
        }

        [Fact]
        public async Task GIVEN_PreferencesLoadRequiresAuthentication_WHEN_LoadingShellSession_THEN_ShouldReturnAuthenticationRequired()
        {
            var target = CreateTarget();
            SetupAuthenticatedSession();
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, ClientPreferences>(ApiFailureKind.AuthenticationRequired, "Unauthorized", HttpStatusCode.Forbidden);
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync("Version");
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData());
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.AuthenticationRequired);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_LoadTaskThrows_WHEN_LoadingShellSession_THEN_ShouldReturnRetryableFailure()
        {
            var target = CreateTarget();
            SetupAuthenticatedSession();
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreatePreferences("en"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromException<ApiResult<string>>(new InvalidOperationException("Failure")));
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData());
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());

            var result = await target.LoadAsync(Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionLoadOutcome.RetryableFailure);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("qBittorrent returned an error. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), "logged-in-layout-startup-api-error"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_LoadTaskCanceled_WHEN_LoadingShellSession_THEN_ShouldRethrowCancellation()
        {
            var target = CreateTarget();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            SetupAuthenticatedSession();
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreatePreferences("en"));
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromCanceled<ApiResult<string>>(cancellationTokenSource.Token));
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData());
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());

            var action = async () => await target.LoadAsync(cancellationTokenSource.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshReturnsFullUpdate_WHEN_RefreshingShellSession_THEN_ShouldRecreateMainDataAndReturnUpdated()
        {
            var target = CreateTarget();
            var mainData = CreateMainData(downloadSpeed: 30, uploadSpeed: 40);
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData(responseId: 11, fullUpdate: true));
            Mock.Get(_dataManager)
                .Setup(manager => manager.CreateMainData(It.IsAny<ClientMainData>()))
                .Returns(mainData);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), 30, 40, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await target.RefreshAsync(5, null, Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionRefreshOutcome.Updated);
            result.MainData.Should().BeSameAs(mainData);
            result.RequestId.Should().Be(11);
            result.ShouldRender.Should().BeTrue();
            result.TorrentsDirty.Should().BeTrue();
            Mock.Get(_torrentCompletionNotificationService)
                .Verify(service => service.ProcessTransitionsAsync(It.IsAny<IReadOnlyList<TorrentTransition>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshMergeHasNoChanges_WHEN_RefreshingShellSession_THEN_ShouldReturnNoChange()
        {
            var target = CreateTarget();
            var currentMainData = CreateMainData(downloadSpeed: 30, uploadSpeed: 40);
            var filterChanged = false;
            IReadOnlyList<TorrentTransition> transitions = Array.Empty<TorrentTransition>();
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData(responseId: 12, fullUpdate: false));
            Mock.Get(_dataManager)
                .Setup(manager => manager.MergeMainData(It.IsAny<ClientMainData>(), currentMainData, out filterChanged, out transitions))
                .Returns(false);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), 30, 40, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await target.RefreshAsync(5, currentMainData, Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionRefreshOutcome.NoChange);
            result.ShouldRender.Should().BeFalse();
            result.TorrentsDirty.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_RefreshMergeChangesFilters_WHEN_RefreshingShellSession_THEN_ShouldReturnUpdatedWithoutRender()
        {
            var target = CreateTarget();
            var currentMainData = CreateMainData(downloadSpeed: 30, uploadSpeed: 40);
            var filterChanged = true;
            IReadOnlyList<TorrentTransition> transitions = Array.Empty<TorrentTransition>();
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData(responseId: 13, fullUpdate: false));
            Mock.Get(_dataManager)
                .Setup(manager => manager.MergeMainData(It.IsAny<ClientMainData>(), currentMainData, out filterChanged, out transitions))
                .Returns(false);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), 30, 40, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await target.RefreshAsync(5, currentMainData, Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionRefreshOutcome.Updated);
            result.ShouldRender.Should().BeFalse();
            result.TorrentsDirty.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RefreshTransitionsCanceled_WHEN_RefreshingShellSession_THEN_ShouldRethrowCancellation()
        {
            var target = CreateTarget();
            var currentMainData = CreateMainData(downloadSpeed: 30, uploadSpeed: 40);
            var filterChanged = false;
            IReadOnlyList<TorrentTransition> transitions =
            [
                new TorrentTransition("Hash", "Name", false, false, true)
            ];
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData(responseId: 14, fullUpdate: false));
            Mock.Get(_dataManager)
                .Setup(manager => manager.MergeMainData(It.IsAny<ClientMainData>(), currentMainData, out filterChanged, out transitions))
                .Returns(true);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), 30, 40, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.ProcessTransitionsAsync(transitions, It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyList<TorrentTransition> _, CancellationToken cancellationToken) => Task.FromCanceled(cancellationToken));

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var action = async () => await target.RefreshAsync(5, currentMainData, cancellationTokenSource.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_RefreshTransitionsThrowUnexpectedException_WHEN_RefreshingShellSession_THEN_ShouldSwallowAndReturnUpdated()
        {
            var target = CreateTarget();
            var currentMainData = CreateMainData(downloadSpeed: 30, uploadSpeed: 40);
            var filterChanged = false;
            IReadOnlyList<TorrentTransition> transitions =
            [
                new TorrentTransition("Hash", "Name", false, false, true)
            ];
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(CreateClientMainData(responseId: 15, fullUpdate: false));
            Mock.Get(_dataManager)
                .Setup(manager => manager.MergeMainData(It.IsAny<ClientMainData>(), currentMainData, out filterChanged, out transitions))
                .Returns(true);
            Mock.Get(_speedHistoryService)
                .Setup(service => service.PushSampleAsync(It.IsAny<DateTime>(), 30, 40, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.ProcessTransitionsAsync(transitions, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var result = await target.RefreshAsync(5, currentMainData, Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionRefreshOutcome.Updated);
        }

        [Theory]
        [InlineData(ApiFailureKind.AuthenticationRequired, ShellSessionRefreshOutcome.AuthenticationRequired)]
        [InlineData(ApiFailureKind.NoResponse, ShellSessionRefreshOutcome.LostConnection)]
        public async Task GIVEN_RefreshFailsWithKnownOutcome_WHEN_RefreshingShellSession_THEN_ShouldReturnExpectedOutcome(ApiFailureKind failureKind, ShellSessionRefreshOutcome expectedOutcome)
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, ClientMainData>(failureKind, "Failure");

            var result = await target.RefreshAsync(5, CreateMainData(), Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(expectedOutcome);
        }

        [Fact]
        public async Task GIVEN_RefreshFailsWithServerError_WHEN_RefreshingShellSession_THEN_ShouldReturnRetryableFailureAndShowSnackbar()
        {
            var target = CreateTarget();
            Mock.Get(_apiClient)
                .Setup(client => client.GetMainDataAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, ClientMainData>(ApiFailureKind.ServerError, "Failure", HttpStatusCode.InternalServerError);

            var result = await target.RefreshAsync(5, CreateMainData(), Xunit.TestContext.Current.CancellationToken);

            result.Outcome.Should().Be(ShellSessionRefreshOutcome.RetryableFailure);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("qBittorrent returned an error. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), "logged-in-layout-refresh-api-error"),
                Times.Once);
        }

        private void SetupAuthenticatedSession()
        {
            SetupAuthenticatedSessionWithoutInitialization();
            Mock.Get(_apiClient)
                .Setup(client => client.InitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);
        }

        private void SetupAuthenticatedSessionWithoutInitialization()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.CheckAuthStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(true);
        }

        private ShellSessionWorkflow CreateTarget()
        {
            return new ShellSessionWorkflow(
                _apiClient,
                _dataManager,
                _snackbarWorkflow,
                _settingsStorageService,
                _languageLocalizer,
                _speedHistoryService,
                _appSettingsService,
                _torrentCompletionNotificationService,
                _navigationManager);
        }

        private static ClientPreferences CreatePreferences(string locale)
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.Locale = locale;
            });
        }

        private static ClientMainData CreateClientMainData(int responseId = 1, bool fullUpdate = true)
        {
            return new ClientMainData(responseId, fullUpdate, null, null, null, null, null, null, null, null, null);
        }

        private static MudMainData CreateMainData(long downloadSpeed = 0, long uploadSpeed = 0)
        {
            return new MudMainData(
                new Dictionary<string, MudTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState
                {
                    DownloadInfoSpeed = downloadSpeed,
                    UploadInfoSpeed = uploadSpeed
                },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
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
            public string? LastNavigationUri { get; private set; }

            public bool ForceLoad { get; private set; }

            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastNavigationUri = uri;
                ForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
