using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class OptionsTests : RazorComponentTestBase<Options>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ISnackbar _snackbar;
        private readonly IRenderedComponent<Options> _target;

        public OptionsTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();
            _snackbar = Mock.Of<ISnackbar>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton(_dialogWorkflow);
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.AddSingleton(_snackbar);

            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationPreferences())
                .ReturnsAsync(CreatePreferences());
            Mock.Get(_apiClient)
                .Setup(client => client.GetNetworkInterfaces())
                .ReturnsAsync(Array.Empty<NetworkInterface>());

            TestContext.Render<MudPopoverProvider>();

            _target = RenderPage();
        }

        [Fact]
        public void GIVEN_PreferencesLoaded_WHEN_Rendered_THEN_DisablesSaveAndUndo()
        {
            Mock.Get(_apiClient).Invocations.Clear();

            var target = RenderPage();

            FindIconButton(target, Icons.Material.Outlined.Save).Instance.Disabled.Should().BeTrue();
            FindIconButton(target, Icons.Material.Outlined.Undo).Instance.Disabled.Should().BeTrue();

            Mock.Get(_apiClient).Verify(client => client.GetApplicationPreferences(), Times.Once);
        }

        [Fact]
        public void GIVEN_LostConnection_WHEN_Rendered_THEN_DisablesSaveAndUndo()
        {
            var target = RenderPage(lostConnection: true);

            FindIconButton(target, Icons.Material.Outlined.Save).Instance.Disabled.Should().BeTrue();
            FindIconButton(target, Icons.Material.Outlined.Undo).Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_PreferencesChanged_WHEN_Toggled_THEN_EnablesSaveAndUndo()
        {
            var target = RenderPage();

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            FindIconButton(target, Icons.Material.Outlined.Save).Instance.Disabled.Should().BeFalse();
            FindIconButton(target, Icons.Material.Outlined.Undo).Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SaveWithoutChanges_WHEN_Clicked_THEN_DoesNotCallApi()
        {
            Mock.Get(_apiClient).Invocations.Clear();

            var target = RenderPage();
            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SaveWithChanges_WHEN_Clicked_THEN_SavesAndNavigatesHome()
        {
            var preferences = CreatePreferences();

            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationPreferences())
                .ReturnsAsync(preferences)
                .ReturnsAsync(preferences);
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
                .Returns(Task.CompletedTask);

            Mock.Get(_apiClient).Invocations.Clear();
            var target = RenderPage(preferences, configureApi: false);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetApplicationPreferences(), Times.Exactly(2));
            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferences(
                    It.Is<UpdatePreferences>(value => value.ConfirmTorrentDeletion == true)),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Options saved.", Severity.Success, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_NoPendingChanges_WHEN_NavigateAttempted_THEN_NavigatesWithoutPrompt()
        {
            Mock.Get(_dialogWorkflow).Invocations.Clear();
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            RenderPage();

            navigationManager.NavigateTo("http://localhost/other");

            navigationManager.Uri.Should().Be("http://localhost/other");
            Mock.Get(_dialogWorkflow).Verify(
                workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_UnsavedChanges_WHEN_NavigationCanceled_THEN_StaysOnPage()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog("Unsaved Changed", "Are you sure you want to leave without saving your changes?"))
                .ReturnsAsync(false);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            var target = RenderPage();

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            navigationManager.NavigateTo("http://localhost/other");

            navigationManager.Uri.Should().Be("http://localhost/settings");
            Mock.Get(_dialogWorkflow).Verify(
                workflow => workflow.ShowConfirmDialog("Unsaved Changed", "Are you sure you want to leave without saving your changes?"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnsavedChanges_WHEN_NavigationConfirmed_THEN_Navigates()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog("Unsaved Changed", "Are you sure you want to leave without saving your changes?"))
                .ReturnsAsync(true);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            var target = RenderPage();

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            navigationManager.NavigateTo("http://localhost/other");

            target.WaitForAssertion(() =>
            {
                navigationManager.Uri.Should().Be("http://localhost/other");
            });
            Mock.Get(_dialogWorkflow).Verify(
                workflow => workflow.ShowConfirmDialog("Unsaved Changed", "Are you sure you want to leave without saving your changes?"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_BehaviourOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);
            await ClickUndo(target);

            FindSwitch(target, "ConfirmTorrentDeletion").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DownloadsOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 1);
            await SetSwitchValue(target, "TempPathEnabled", true);
            await ClickUndo(target);

            FindSwitch(target, "TempPathEnabled").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConnectionOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 2);
            await SetSwitchValue(target, "Upnp", true);
            await ClickUndo(target);

            FindSwitch(target, "Upnp").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SpeedOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 3);
            await SetSwitchValue(target, "LimitUtpRate", true);
            await ClickUndo(target);

            FindSwitch(target, "LimitUtpRate").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_BitTorrentOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 4);
            await SetSwitchValue(target, "Dht", true);
            await ClickUndo(target);

            FindSwitch(target, "Dht").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_RssOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 5);
            await SetSwitchValue(target, "RssProcessingEnabled", true);
            await ClickUndo(target);

            FindSwitch(target, "RssProcessingEnabled").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_WebUiOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 6);
            await SetSwitchValue(target, "WebUiUpnp", true);
            await ClickUndo(target);

            FindSwitch(target, "WebUiUpnp").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_AdvancedOptionsChanged_WHEN_UndoClicked_THEN_ResetsValues()
        {
            var target = RenderPage();

            await ActivateTab(target, 7);
            await SetSwitchValue(target, "RecheckCompletedTorrents", true);
            await ClickUndo(target);

            FindSwitch(target, "RecheckCompletedTorrents").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_BackClicked_WHEN_Invoked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            var target = RenderPage();
            var backButton = FindIconButton(target, Icons.Material.Outlined.NavigateBefore);

            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_DrawerOpen_WHEN_Rendered_THEN_HidesBackButton()
        {
            var target = RenderPage(drawerOpen: true);

            target.FindComponents<MudIconButton>()
                .Should()
                .NotContain(button => button.Instance.Icon == Icons.Material.Outlined.NavigateBefore);
        }

        private IRenderedComponent<Options> RenderPage(Preferences? preferences = null, bool drawerOpen = false, bool lostConnection = false, bool configureApi = true)
        {
            if (configureApi)
            {
                Mock.Get(_apiClient)
                    .Setup(client => client.GetApplicationPreferences())
                    .ReturnsAsync(preferences ?? CreatePreferences());
            }

            return TestContext.Render<Options>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                parameters.AddCascadingValue("LostConnection", lostConnection);
            });
        }

        private static async Task ActivateTab(IRenderedComponent<Options> target, int index)
        {
            var tabs = target.FindComponent<MudTabs>();
            await target.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(index));
        }

        private static async Task ClickUndo(IRenderedComponent<Options> target)
        {
            var undoButton = FindIconButton(target, Icons.Material.Outlined.Undo);
            await target.InvokeAsync(() => undoButton.Instance.OnClick.InvokeAsync());
        }

        private static async Task SetSwitchValue(IRenderedComponent<Options> target, string testId, bool value)
        {
            var field = FindSwitch(target, testId);
            await target.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static Preferences CreatePreferences()
        {
            const string json = """
            {
                "confirm_torrent_deletion": false,
                "temp_path_enabled": false,
                "temp_path": "/temp",
                "save_path": "/downloads",
                "scan_dirs": {},
                "upnp": false,
                "limit_utp_rate": false,
                "dht": false,
                "rss_processing_enabled": false,
                "web_ui_upnp": false,
                "web_ui_port": 8080,
                "web_ui_address": "0.0.0.0",
                "recheck_completed_torrents": false
            }
            """;

            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }
    }
}
