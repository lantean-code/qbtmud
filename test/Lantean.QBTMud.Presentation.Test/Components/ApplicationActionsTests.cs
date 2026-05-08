using System.Net;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Infrastructure.Configuration;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;
using AppSettingsModel = Lantean.QBTMud.Core.Models.AppSettings;

namespace Lantean.QBTMud.Presentation.Test.Components
{
    public sealed class ApplicationActionsTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_IsMenuWithoutPreferences_WHEN_Rendered_THEN_RendersActionsExcludingRss()
        {
            var snackbarMock = TestContext.UseSnackbarMock();
            var apiClientMock = TestContext.UseApiClientMock();

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var menuItems = target.FindComponents<MudMenuItem>();
            menuItems.Should().HaveCount(19);
            menuItems.Any(item => HasTestId(item, "Action-speed")).Should().BeTrue();
            menuItems.Any(item => HasTestId(item, "Action-themes")).Should().BeTrue();
            menuItems.Any(item => HasTestId(item, "Action-appSettings")).Should().BeTrue();
            snackbarMock.Invocations.Should().BeEmpty();
            apiClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void GIVEN_SpeedHistoryDisabled_WHEN_Rendered_THEN_HidesSpeedAction()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();
            TestContext.Services.GetRequiredService<IAppSettingsStateService>().SetSettings(new AppSettingsModel
            {
                SpeedHistoryEnabled = false
            });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            target.FindComponents<MudMenuItem>().Any(item => HasTestId(item, "Action-speed")).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_RuntimeAppSettingsChange_WHEN_SpeedHistoryDisabled_THEN_HidesSpeedAction()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();
            var appSettingsStateService = TestContext.Services.GetRequiredService<IAppSettingsStateService>();

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            target.FindComponents<MudMenuItem>().Any(item => HasTestId(item, "Action-speed")).Should().BeTrue();

            appSettingsStateService.SetSettings(new AppSettingsModel
            {
                SpeedHistoryEnabled = false
            });

            target.WaitForAssertion(() =>
            {
                target.FindComponents<MudMenuItem>().Any(item => HasTestId(item, "Action-speed")).Should().BeFalse();
            });
        }

        [Fact]
        public void GIVEN_RssEnabled_WHEN_Rendered_THEN_RendersRssAction()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();

            var preferences = CreatePreferences(rssEnabled: true);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, preferences);
            });

            target.FindComponents<MudMenuItem>().Any(item => HasTestId(item, "Action-rss")).Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_StartAllInvoked_WHEN_Succeeds_THEN_ApiCalledAndSuccessShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>())).ReturnsSuccess(Task.CompletedTask);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");
            await target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents started", StringComparison.OrdinalIgnoreCase)), Severity.Success, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAllInvoked_WHEN_Succeeds_THEN_ApiCalledAndInfoShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>())).ReturnsSuccess(Task.CompletedTask);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");
            await target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents stopped", StringComparison.OrdinalIgnoreCase)), Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StartAll_WHEN_NoResponse_THEN_ShowsLostConnectionDialog()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            apiClientMock.Setup(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.NoResponse, "Unavailable");
            dialogServiceMock
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReference.Object);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");
            await target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()), Times.Once);
            dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StartAll_WHEN_ApiError_THEN_ErrorShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>())).ReturnsFailure(ApiFailureKind.ServerError, "boom", HttpStatusCode.InternalServerError);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");
            await target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add("boom", Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StartAll_WHEN_AuthenticationRequired_THEN_NavigatesToLoginWithoutShowingError()
        {
            var navigationManager = UseTestNavigationManager();
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Unauthorized", HttpStatusCode.Unauthorized);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");
            await target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().EndWith("/login");
            snackbarMock.Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_StopAll_WHEN_ApiError_THEN_ErrorShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>())).ReturnsFailure(ApiFailureKind.ServerError, "fail", HttpStatusCode.InternalServerError);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");
            await target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add("fail", Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAll_WHEN_AuthenticationRequired_THEN_NavigatesToLoginWithoutShowingError()
        {
            var navigationManager = UseTestNavigationManager();
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Unauthorized", HttpStatusCode.Unauthorized);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");
            await target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().EndWith("/login");
            snackbarMock.Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_StartAllAlreadyInProgress_WHEN_ClickedAgain_THEN_SubsequentRequestIgnored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            var startSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            apiClientMock.Setup(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>())).ReturnsSuccess(startSource.Task);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");

            var first = target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());
            var second = target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            startSource.SetResult();

            await Task.WhenAll(first, second);

            apiClientMock.Verify(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents started", StringComparison.OrdinalIgnoreCase)), Severity.Success, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAllAlreadyInProgress_WHEN_ClickedAgain_THEN_SubsequentRequestIgnored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            var stopSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            apiClientMock.Setup(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>())).ReturnsSuccess(stopSource.Task);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");

            var first = target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());
            var second = target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            stopSource.SetResult();

            await Task.WhenAll(first, second);

            apiClientMock.Verify(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents stopped", StringComparison.OrdinalIgnoreCase)), Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAll_WHEN_NoResponse_THEN_ShowsLostConnectionDialog()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());
            apiClientMock.Setup(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.NoResponse, "Unavailable");
            dialogServiceMock
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReference.Object);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");
            await target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.IsAll(selector)), It.IsAny<CancellationToken>()), Times.Once);
            dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_Success_THEN_ShowsSuccess()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetResult(new MagnetRegistrationResult { Status = "success" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("registered", StringComparison.OrdinalIgnoreCase)), Severity.Success, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_Unsupported_THEN_ShowsWarning()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetResult(new MagnetRegistrationResult { Status = "unsupported" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("does not support", StringComparison.OrdinalIgnoreCase)), Severity.Warning, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_Insecure_THEN_ShowsWarning()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetResult(new MagnetRegistrationResult { Status = "insecure" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("HTTPS", StringComparison.OrdinalIgnoreCase)), Severity.Warning, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_ErrorMessage_THEN_ShowsError()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetResult(new MagnetRegistrationResult { Status = "unknown", Message = "Oops" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("Oops", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_StatusUnknownNoMessage_THEN_ShowsDefaultError()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetResult(new MagnetRegistrationResult { Status = "unknown" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("Unable to register", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_StatusIsNull_THEN_ShowsDefaultError()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetResult(new MagnetRegistrationResult { Status = null });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("Unable to register", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_InProgress_THEN_SubsequentClicksIgnored()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            var jsRuntimeMock = TestContext.AddSingletonMock<IJSRuntime>(MockBehavior.Strict);
            var registrationSource = new TaskCompletionSource<MagnetRegistrationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            jsRuntimeMock.Setup(r => r.InvokeAsync<MagnetRegistrationResult>("qbt.registerMagnetHandler", It.IsAny<object?[]?>()))
                .Returns(() => new ValueTask<MagnetRegistrationResult>(registrationSource.Task));

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");

            var first = target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());
            var second = target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            registrationSource.SetResult(new MagnetRegistrationResult { Status = "success" });

            await Task.WhenAll(first, second);

            jsRuntimeMock.Verify(r => r.InvokeAsync<MagnetRegistrationResult>("qbt.registerMagnetHandler", It.IsAny<object?[]?>()), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("registered", StringComparison.OrdinalIgnoreCase)), Severity.Success, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_JsThrows_THEN_ShowsError()
        {
            TestContext.UseApiClientMock();
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetException(new JSException("fail"));

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("fail", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RegisterMagnetHandler_WHEN_Invoked_THEN_UsesBaseUriTemplate()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock(MockBehavior.Loose);
            var registerInvocation = TestContext.JSInterop.Setup<MagnetRegistrationResult>(
                "qbt.registerMagnetHandler",
                invocation => invocation.Arguments.Count == 2
                    && invocation.Arguments.ElementAt(0) as string == "http://localhost/?download=%s"
                    && invocation.Arguments.ElementAt(1) as string == "qBittorrent WebUI magnet handler");
            registerInvocation.SetResult(new MagnetRegistrationResult { Status = "success" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            registerInvocation.Invocations.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_HashRouting_WHEN_RegisterMagnetHandlerInvoked_THEN_UsesHashTemplate()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.Services.RemoveAll<RoutingMode>();
            TestContext.Services.RemoveAll<IInternalUrlProvider>();
            TestContext.Services.AddSingleton(typeof(RoutingMode), RoutingMode.Hash);
            TestContext.Services.AddScoped<IInternalUrlProvider, InternalUrlProvider>();

            var registerInvocation = TestContext.JSInterop.Setup<MagnetRegistrationResult>(
                "qbt.registerMagnetHandler",
                invocation => invocation.Arguments.Count == 2
                    && invocation.Arguments.ElementAt(0) as string == "http://localhost/#/?download=%s"
                    && invocation.Arguments.ElementAt(1) as string == "qBittorrent WebUI magnet handler");
            registerInvocation.SetResult(new MagnetRegistrationResult { Status = "success" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            registerInvocation.Invocations.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_ResetWebUI_WHEN_Invoked_THEN_PreferencesSentAndNavigated()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock();
            apiClientMock.Setup(c => c.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>())).ReturnsAsync(ApiResult.CreateSuccess());

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var resetItem = FindMenuItem(target, "ResetWebUI");
            await target.InvokeAsync(() => resetItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.SetApplicationPreferencesAsync(It.Is<UpdatePreferences>(p => p.AlternativeWebuiEnabled == false)), Times.Once);
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Be(TestContext.Services.GetRequiredService<NavigationManager>().BaseUri);
            snackbarMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ResetWebUI_WHEN_UpdateFails_THEN_DoesNotNavigateAndShowsError()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(c => c.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Reset failed", HttpStatusCode.InternalServerError);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var resetItem = FindMenuItem(target, "ResetWebUI");
            await target.InvokeAsync(() => resetItem.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/other");
            snackbarMock.Verify(s => s.Add("Reset failed", Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Logout_WHEN_Confirmed_THEN_LogsOutAndNavigates()
        {
            var navigationManager = UseTestNavigationManager();
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var speedHistoryMock = TestContext.AddSingletonMock<ISpeedHistoryService>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.LogoutAsync()).ReturnsSuccess(Task.CompletedTask);
            speedHistoryMock.Setup(s => s.ClearAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            dialogMock.Setup(d => d.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Returns<string, string, Func<Task>>((_, _, callback) => callback());

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var logoutItem = FindMenuItem(target, "Logout");
            await target.InvokeAsync(() => logoutItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.LogoutAsync(), Times.Once);
            speedHistoryMock.Verify(s => s.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
            navigationManager.Uri.Should().EndWith("/login");
            navigationManager.LastForceLoad.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_Logout_WHEN_NoResponse_THEN_MarksLostConnectionWithoutNavigatingToLogin()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            var speedHistoryMock = TestContext.AddSingletonMock<ISpeedHistoryService>(MockBehavior.Strict);
            var dialogReference = new Mock<IDialogReference>(MockBehavior.Strict);
            dialogReference.Setup(dialog => dialog.Close());

            apiClientMock
                .Setup(c => c.LogoutAsync())
                .ReturnsFailure(ApiFailureKind.NoResponse, "Unavailable");
            dialogWorkflowMock
                .Setup(d => d.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Returns<string, string, Func<Task>>((_, _, callback) => callback());
            dialogServiceMock
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReference.Object);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var logoutItem = FindMenuItem(target, "Logout");
            await target.InvokeAsync(() => logoutItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.LogoutAsync(), Times.Once);
            speedHistoryMock.Verify(s => s.ClearAsync(It.IsAny<CancellationToken>()), Times.Never);
            dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Once);
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/login");
        }

        [Fact]
        public async Task GIVEN_Logout_WHEN_AlreadyUnauthorized_THEN_NavigatesToLoginWithoutLostConnectionDialog()
        {
            var navigationManager = UseTestNavigationManager();
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var dialogServiceMock = TestContext.AddSingletonMock<IDialogService>(MockBehavior.Strict);
            var speedHistoryMock = TestContext.AddSingletonMock<ISpeedHistoryService>(MockBehavior.Strict);

            apiClientMock
                .Setup(c => c.LogoutAsync())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Unauthorized", HttpStatusCode.Unauthorized);
            speedHistoryMock
                .Setup(s => s.ClearAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            dialogWorkflowMock
                .Setup(d => d.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Returns<string, string, Func<Task>>((_, _, callback) => callback());

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var logoutItem = FindMenuItem(target, "Logout");
            await target.InvokeAsync(() => logoutItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.LogoutAsync(), Times.Once);
            speedHistoryMock.Verify(s => s.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
            dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogOptions>()), Times.Never);
            navigationManager.Uri.Should().EndWith("/login");
            navigationManager.LastForceLoad.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ResetWebUi_WHEN_AuthenticationRequired_THEN_NavigatesToLoginWithForceLoad()
        {
            var navigationManager = UseTestNavigationManager();
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);

            apiClientMock
                .Setup(c => c.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Unauthorized", HttpStatusCode.Unauthorized);
            dialogWorkflowMock
                .Setup(d => d.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Returns<string, string, Func<Task>>((_, _, callback) => callback());

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var resetItem = FindMenuItem(target, "ResetWebUI");
            await target.InvokeAsync(() => resetItem.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().EndWith("/login");
            navigationManager.LastForceLoad.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_Logout_WHEN_ApiError_THEN_ShowsErrorWithoutNavigatingToLogin()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var speedHistoryMock = TestContext.AddSingletonMock<ISpeedHistoryService>(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);

            apiClientMock
                .Setup(c => c.LogoutAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "Server", HttpStatusCode.InternalServerError);
            dialogWorkflowMock
                .Setup(d => d.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Returns<string, string, Func<Task>>((_, _, callback) => callback());

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var logoutItem = FindMenuItem(target, "Logout");
            await target.InvokeAsync(() => logoutItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.LogoutAsync(), Times.Once);
            speedHistoryMock.Verify(s => s.ClearAsync(It.IsAny<CancellationToken>()), Times.Never);
            snackbarMock.Verify(
                snackbar => snackbar.Add(
                    "Server",
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string>()),
                Times.Once);
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/login");
        }

        [Fact]
        public async Task GIVEN_Exit_WHEN_Confirmed_THEN_ShutdownCalled()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.ShutdownAsync()).ReturnsSuccess(Task.CompletedTask);
            dialogMock.Setup(d => d.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Returns<string, string, Func<Task>>((_, _, callback) => callback());

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var exitItem = FindMenuItem(target, "Exit");
            await target.InvokeAsync(() => exitItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.ShutdownAsync(), Times.Once);
        }

        [Fact]
        public void GIVEN_NavMode_WHEN_Rendered_THEN_AppSettingsActionShown()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, false);
                parameters.Add(p => p.Preferences, CreatePreferences(rssEnabled: true));
            });

            target.FindComponents<MudNavLink>()
                .Any(item => HasTestId(item, "Action-appSettings"))
                .Should().BeTrue();
        }

        private static IRenderedComponent<MudMenuItem> FindMenuItem(IRenderedComponent<ApplicationActions> target, string testId)
        {
            return FindComponentByTestId<MudMenuItem>(target, testId);
        }

        private static string? TryGetUserAttribute<TComponent>(IRenderedComponent<TComponent> component, string attribute)
            where TComponent : MudComponentBase
        {
            if (component.Instance.UserAttributes is null)
            {
                return null;
            }

            return component.Instance.UserAttributes.TryGetValue(attribute, out var value)
                ? value?.ToString()
                : null;
        }

        private static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<ApplicationActions> target, string testId)
            where TComponent : MudComponentBase
        {
            var expected = TestIdHelper.For(testId);
            return target.FindComponents<TComponent>()
                .First(component => component.Instance.UserAttributes is not null
                    && component.Instance.UserAttributes.TryGetValue("data-test-id", out var value)
                    && string.Equals(value?.ToString(), expected, StringComparison.Ordinal));
        }

        [Fact]
        public async Task GIVEN_NavigateBackAction_WHEN_Clicked_THEN_NavigatesHome()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, false);
                parameters.Add(p => p.Preferences, CreatePreferences(rssEnabled: true));
            });

            var backLink = target.FindComponents<MudNavLink>().First();
            await target.InvokeAsync(() => backLink.Instance.OnClick.InvokeAsync());

            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Be(TestContext.Services.GetRequiredService<NavigationManager>().BaseUri);
        }

        private static QBittorrentPreferences CreatePreferences(bool rssEnabled)
        {
            return PreferencesFactory.CreateQBittorrentPreferences(spec =>
            {
                spec.RssProcessingEnabled = rssEnabled;
            });
        }

        private TestNavigationManager UseTestNavigationManager()
        {
            var navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll<NavigationManager>();
            TestContext.Services.AddSingleton<NavigationManager>(navigationManager);
            return navigationManager;
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            public bool LastForceLoad { get; private set; }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
