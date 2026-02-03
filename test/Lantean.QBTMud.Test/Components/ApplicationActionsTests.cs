using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using System.Text.Json;
using CategoryModel = Lantean.QBTMud.Models.Category;
using MainDataModel = Lantean.QBTMud.Models.MainData;
using ServerStateModel = Lantean.QBTMud.Models.ServerState;
using TorrentModel = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Test.Components
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
            snackbarMock.Invocations.Should().BeEmpty();
            apiClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GIVEN_DarkModeMenuItem_WHEN_Clicked_THEN_InvokesCallback()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();
            bool? newValue = null;

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.IsDarkMode, false);
                parameters.Add(p => p.DarkModeChanged, EventCallback.Factory.Create<bool>(this, value => newValue = value));
            });

            var darkMode = FindMenuItem(target, "Action-darkMode");
            await target.InvokeAsync(() => darkMode.Instance.OnClick.InvokeAsync());

            var items = target.FindComponents<MudMenuItem>().ToList();
            var darkIndex = items.FindIndex(item => HasTestId(item, "Action-darkMode"));
            var aboutIndex = items.FindIndex(item => HasTestId(item, "Action-about"));
            darkIndex.Should().BeGreaterThanOrEqualTo(0);
            aboutIndex.Should().BeGreaterThan(darkIndex);
            newValue.Should().BeTrue();
            GetChildContentText(darkMode.Instance.ChildContent).Should().Be("Switch to light mode");
            darkMode.Instance.IconColor.Should().Be(Color.Info);
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
            apiClientMock.Setup(c => c.StartTorrents(true)).Returns(Task.CompletedTask);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");
            await target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.StartTorrents(true), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents started", StringComparison.OrdinalIgnoreCase)), Severity.Success, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAllInvoked_WHEN_Succeeds_THEN_ApiCalledAndInfoShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StopTorrents(true)).Returns(Task.CompletedTask);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");
            await target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.StopTorrents(true), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents stopped", StringComparison.OrdinalIgnoreCase)), Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LostConnection_WHEN_StartAllInvoked_THEN_ShowsWarningAndSkipsApi()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);

            var mainData = CreateMainData();
            mainData.LostConnection = true;

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
                parameters.AddCascadingValue(mainData);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");
            await target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("not reachable", StringComparison.OrdinalIgnoreCase)), Severity.Warning, null, null), Times.Once);
            apiClientMock.Verify(c => c.StartTorrents(true, Array.Empty<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_StartAll_WHEN_HttpRequestException_THEN_ErrorShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StartTorrents(true)).ThrowsAsync(new HttpRequestException("boom"));

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var startItem = FindMenuItem(target, "StartAllTorrents");
            await target.InvokeAsync(() => startItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("Unable to start torrents", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAll_WHEN_HttpRequestException_THEN_ErrorShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StopTorrents(true)).ThrowsAsync(new HttpRequestException("fail"));

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");
            await target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("Unable to stop torrents", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StartAllAlreadyInProgress_WHEN_ClickedAgain_THEN_SubsequentRequestIgnored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            var startSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            apiClientMock.Setup(c => c.StartTorrents(true)).Returns(() => startSource.Task);

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

            apiClientMock.Verify(c => c.StartTorrents(true), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents started", StringComparison.OrdinalIgnoreCase)), Severity.Success, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAllAlreadyInProgress_WHEN_ClickedAgain_THEN_SubsequentRequestIgnored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            var stopSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            apiClientMock.Setup(c => c.StopTorrents(true)).Returns(() => stopSource.Task);

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

            apiClientMock.Verify(c => c.StopTorrents(true), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("All torrents stopped", StringComparison.OrdinalIgnoreCase)), Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StopAllLostConnection_WHEN_Invoked_THEN_ShowsWarning()
        {
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.UseApiClientMock();
            var mainData = CreateMainData();
            mainData.LostConnection = true;

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
                parameters.AddCascadingValue(mainData);
            });

            var stopItem = FindMenuItem(target, "StopAllTorrents");
            await target.InvokeAsync(() => stopItem.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(It.Is<string>(msg => msg.Contains("not reachable", StringComparison.OrdinalIgnoreCase)), Severity.Warning, null, null), Times.Once);
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
            TestContext.JSInterop.Setup<MagnetRegistrationResult>("qbt.registerMagnetHandler", _ => true)
                .SetResult(new MagnetRegistrationResult { Status = "success" });

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var registerItem = FindMenuItem(target, "RegisterMagnetHandler");
            await target.InvokeAsync(() => registerItem.Instance.OnClick.InvokeAsync());

            var invocation = TestContext.JSInterop.Invocations.Single(i => i.Identifier == "qbt.registerMagnetHandler");
            invocation.Arguments.First().Should().Be("http://localhost/#download=%s");
        }

        [Fact]
        public async Task GIVEN_ResetWebUI_WHEN_Invoked_THEN_PreferencesSentAndNavigated()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock();
            apiClientMock.Setup(c => c.SetApplicationPreferences(It.IsAny<UpdatePreferences>())).Returns(Task.CompletedTask);

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var resetItem = FindMenuItem(target, "ResetWebUI");
            await target.InvokeAsync(() => resetItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.SetApplicationPreferences(It.Is<UpdatePreferences>(p => p.AlternativeWebuiEnabled == false)), Times.Once);
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Be(TestContext.Services.GetRequiredService<NavigationManager>().BaseUri);
            snackbarMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_Logout_WHEN_Confirmed_THEN_LogsOutAndNavigates()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var speedHistoryMock = TestContext.AddSingletonMock<ISpeedHistoryService>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.Logout()).Returns(Task.CompletedTask);
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

            apiClientMock.Verify(c => c.Logout(), Times.Once);
            speedHistoryMock.Verify(s => s.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Exit_WHEN_Confirmed_THEN_ShutdownCalled()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.Shutdown()).Returns(Task.CompletedTask);
            dialogMock.Setup(d => d.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Returns<string, string, Func<Task>>((_, _, callback) => callback());

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, true);
                parameters.Add(p => p.Preferences, null);
            });

            var exitItem = FindMenuItem(target, "Exit");
            await target.InvokeAsync(() => exitItem.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.Shutdown(), Times.Once);
        }

        [Fact]
        public void GIVEN_NavMode_WHEN_Rendered_THEN_DarkModeToggleNotShown()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();

            var target = TestContext.Render<ApplicationActions>(parameters =>
            {
                parameters.Add(p => p.IsMenu, false);
                parameters.Add(p => p.Preferences, CreatePreferences(rssEnabled: true));
                parameters.Add(p => p.IsDarkMode, true);
            });

            target.FindComponents<MudNavLink>()
                .Any(item => HasTestId(item, "Action-darkMode"))
                .Should().BeFalse();
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

        private static Preferences CreatePreferences(bool rssEnabled)
        {
            var json = $"{{\"rss_processing_enabled\":{(rssEnabled ? "true" : "false")}}}";
            return JsonSerializer.Deserialize<Preferences>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private static MainDataModel CreateMainData()
        {
            var serverState = JsonSerializer.Deserialize<ServerStateModel>("{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            return new MainDataModel(
                new Dictionary<string, TorrentModel>(),
                Array.Empty<string>(),
                new Dictionary<string, CategoryModel>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState,
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
        }
    }
}
