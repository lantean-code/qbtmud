using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class OptionsTests : RazorComponentTestBase<Options>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ISnackbar _snackbar;

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
                .Setup(client => client.GetApplicationPreferencesAsync())
                .ReturnsSuccessAsync(CreatePreferences());
            Mock.Get(_apiClient)
                .Setup(client => client.GetNetworkInterfacesAsync())
                .ReturnsSuccessAsync(Array.Empty<NetworkInterface>());

            TestContext.Render<MudPopoverProvider>();
        }

        [Fact]
        public void GIVEN_PreferencesLoaded_WHEN_Rendered_THEN_DisablesSaveAndUndo()
        {
            _apiClient.ClearInvocations();

            var target = RenderPage();

            FindIconButton(target, Icons.Material.Outlined.Save).Instance.Disabled.Should().BeTrue();
            FindIconButton(target, Icons.Material.Outlined.Undo).Instance.Disabled.Should().BeTrue();

            Mock.Get(_apiClient).Verify(client => client.GetApplicationPreferencesAsync(), Times.Once);
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
            _apiClient.ClearInvocations();

            var target = RenderPage();
            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SaveWithChanges_WHEN_Clicked_THEN_SavesReloadsPreferencesAndStaysOnPage()
        {
            var preferences = CreatePreferences();
            var savedPreferences = CreatePreferences(spec =>
            {
                spec.ConfirmTorrentDeletion = true;
            });

            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationPreferencesAsync())
                .ReturnsAsync(preferences)
                .ReturnsAsync(savedPreferences);
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsSuccess(Task.CompletedTask);

            var target = RenderPage(preferences, configureApi: false);
            _apiClient.ClearInvocations();
            _snackbar.ClearInvocations();

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetApplicationPreferencesAsync(), Times.Once);
            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(
                    It.Is<UpdatePreferences>(value => value.ConfirmTorrentDeletion == true)),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Options saved.", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
            navigationManager.Uri.Should().Be("http://localhost/settings");
            target.FindComponents<NavigationLock>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_SaveCompleted_WHEN_NavigatingAfterSave_THEN_DoesNotPromptUnsavedChanges()
        {
            var preferences = CreatePreferences();

            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsSuccess(Task.CompletedTask);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");
            _dialogWorkflow.ClearInvocations();

            var target = RenderPage(preferences, configureApi: false);

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                target.FindComponents<NavigationLock>().Should().BeEmpty();
            });
            target.FindComponents<NavigationLock>().Should().BeEmpty();

            navigationManager.NavigateTo("http://localhost/other");

            target.WaitForAssertion(() =>
            {
                navigationManager.Uri.Should().Be("http://localhost/other");
            });

            Mock.Get(_dialogWorkflow).Verify(
                workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SaveFails_WHEN_Clicked_THEN_ShowsLocalizedErrorAndStaysOnPage()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure");
            var snackbarMock = Mock.Get(_snackbar);
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            var target = RenderPage();
            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(snackbar => snackbar.Add("Unable to save options.", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
            navigationManager.Uri.Should().Be("http://localhost/settings");
        }

        [Fact]
        public async Task GIVEN_LocaleChanged_WHEN_Saved_THEN_ShouldPersistLocaleToLocalStorage()
        {
            var preferences = CreatePreferences();

            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsSuccess(Task.CompletedTask);

            await TestContext.LocalStorage.RemoveItemAsync(LanguageStorageKeys.PreferredLocale, Xunit.TestContext.Current.CancellationToken);

            var target = RenderPage(preferences, configureApi: false);
            await ActivateTab(target, 0);

            var languageSelect = FindSelect<string>(target, "UserInterfaceLanguage");
            await target.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            var storedLocale = await TestContext.LocalStorage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, Xunit.TestContext.Current.CancellationToken);
            storedLocale.Should().Be("fr");
        }

        [Fact]
        public async Task GIVEN_LocaleChanged_WHEN_Saved_THEN_ShowsReloadActionPrompt()
        {
            var preferences = CreatePreferences();
            Action<SnackbarOptions>? capturedOptions = null;
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsSuccess(Task.CompletedTask);
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(
                    "Language preference changed on server. Click Reload to apply it.",
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    "options-language-reload"))
                .Callback<string, Severity, Action<SnackbarOptions>, string>((_, _, configure, _) => capturedOptions = configure);

            var target = RenderPage(preferences, configureApi: false);
            await ActivateTab(target, 0);

            var languageSelect = FindSelect<string>(target, "UserInterfaceLanguage");
            await target.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(
                    "Language preference changed on server. Click Reload to apply it.",
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    "options-language-reload"),
                Times.Once);
            capturedOptions.Should().NotBeNull();

            var options = new SnackbarOptions(Severity.Warning, new SnackbarConfiguration());
            capturedOptions!(options);
            options.Action.Should().Be("Reload");
            options.RequireInteraction.Should().BeTrue();
            options.OnClick.Should().NotBeNull();

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            await options.OnClick!(null!);

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_RuntimePreferencesChangedAfterSave_WHEN_Saved_THEN_QBittorrentPreferencesChangedEventRaised()
        {
            var preferences = CreatePreferences();
            var savedPreferences = CreatePreferences(spec =>
            {
                spec.ConfirmTorrentDeletion = true;
            });
            var stateService = TestContext.Services.GetRequiredService<IQBittorrentPreferencesStateService>();
            var eventCount = 0;
            stateService.SetPreferences(CreateQBittorrentPreferences(preferences));
            stateService.Changed += (_, _) => eventCount++;
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationPreferencesAsync())
                .ReturnsAsync(preferences)
                .ReturnsAsync(savedPreferences);
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsSuccess(Task.CompletedTask);

            var target = RenderPage(preferences, configureApi: false);
            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            eventCount.Should().Be(1);
            stateService.Current.Should().Be(CreateQBittorrentPreferences(savedPreferences));
        }

        [Fact]
        public async Task GIVEN_RuntimePreferencesUnchangedAfterSave_WHEN_Saved_THEN_QBittorrentPreferencesChangedEventNotRaised()
        {
            var preferences = CreatePreferences();
            var savedPreferences = CreatePreferences(spec =>
            {
                spec.Upnp = true;
            });
            var stateService = TestContext.Services.GetRequiredService<IQBittorrentPreferencesStateService>();
            var eventCount = 0;
            stateService.SetPreferences(CreateQBittorrentPreferences(preferences));
            stateService.Changed += (_, _) => eventCount++;
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationPreferencesAsync())
                .ReturnsAsync(preferences)
                .ReturnsAsync(savedPreferences);
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsSuccess(Task.CompletedTask);

            var target = RenderPage(preferences, configureApi: false);
            await ActivateTab(target, 2);
            await SetSwitchValue(target, "Upnp", true);

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            eventCount.Should().Be(0);
            stateService.Current.Should().Be(CreateQBittorrentPreferences(preferences));
        }

        [Fact]
        public async Task GIVEN_PreferencesRefreshFailsAfterSave_WHEN_Saved_THEN_ShowsSuccessAndApiFailureWithoutPublishingChange()
        {
            var preferences = CreatePreferences();
            var stateService = TestContext.Services.GetRequiredService<IQBittorrentPreferencesStateService>();
            var eventCount = 0;
            stateService.SetPreferences(CreateQBittorrentPreferences(preferences));
            stateService.Changed += (_, _) => eventCount++;
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationPreferencesAsync())
                .ReturnsAsync(preferences)
                .ReturnsFailure(ApiFailureKind.ServerError, "Refresh failed");
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsSuccess(Task.CompletedTask);

            var target = RenderPage(preferences, configureApi: false);
            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            var saveButton = FindIconButton(target, Icons.Material.Outlined.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Options saved.", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Refresh failed", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
                Times.Once);
            eventCount.Should().Be(0);
            target.FindComponents<NavigationLock>().Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_NoPendingChanges_WHEN_NavigateAttempted_THEN_NavigatesWithoutPrompt()
        {
            _dialogWorkflow.ClearInvocations();
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
                .Setup(workflow => workflow.ShowConfirmDialog("Unsaved Changes", "Are you sure you want to leave without saving your changes?"))
                .ReturnsAsync(false);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/settings");

            var target = RenderPage();

            await ActivateTab(target, 0);
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);

            navigationManager.NavigateTo("http://localhost/other");

            navigationManager.Uri.Should().Be("http://localhost/settings");
            Mock.Get(_dialogWorkflow).Verify(
                workflow => workflow.ShowConfirmDialog("Unsaved Changes", "Are you sure you want to leave without saving your changes?"),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnsavedChanges_WHEN_NavigationConfirmed_THEN_Navigates()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog("Unsaved Changes", "Are you sure you want to leave without saving your changes?"))
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
                workflow => workflow.ShowConfirmDialog("Unsaved Changes", "Are you sure you want to leave without saving your changes?"),
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

        private IRenderedComponent<Options> RenderPage(Preferences? preferences = null, bool drawerOpen = false, bool configureApi = true)
        {
            if (configureApi)
            {
                Mock.Get(_apiClient)
                    .Setup(client => client.GetApplicationPreferencesAsync())
                    .ReturnsSuccessAsync(preferences ?? CreatePreferences());
            }

            return TestContext.Render<Options>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
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

        private static QBittorrentPreferences CreateQBittorrentPreferences(Preferences preferences)
        {
            return new PreferencesDataManager().CreateQBittorrentPreferences(preferences);
        }

        private static Preferences CreatePreferences(Action<PreferencesFactory.PreferencesSpec>? configure = null)
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.ConfirmTorrentDeletion = false;
                spec.Dht = false;
                spec.LimitUtpRate = false;
                spec.RecheckCompletedTorrents = false;
                spec.RssProcessingEnabled = false;
                spec.SavePath = "/downloads";
                spec.ScanDirs = [];
                spec.TempPath = "/temp";
                spec.TempPathEnabled = false;
                spec.Upnp = false;
                spec.WebUiAddress = "0.0.0.0";
                spec.WebUiPort = 8080;
                spec.WebUiUpnp = false;
                configure?.Invoke(spec);
            });
        }
    }
}
