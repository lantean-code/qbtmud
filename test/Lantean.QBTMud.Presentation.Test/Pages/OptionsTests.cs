using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Pages
{
    public sealed class OptionsTests : RazorComponentTestBase<Options>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ISnackbar _snackbar;

        public OptionsTests()
        {
            TestContext.Services.RemoveAll<Lantean.QBTMud.Application.Services.IWebApiCapabilityService>();
            TestContext.Services.AddScoped<Lantean.QBTMud.Application.Services.IWebApiCapabilityService, Lantean.QBTMud.Infrastructure.Services.WebApiCapabilityService>();
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
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync("2.14.1");

            TestContext.Render<MudPopoverProvider>();
        }

        [Fact]
        public async Task GIVEN_Version53NonSpeedChange_WHEN_Saving_THEN_DoesNotReadOrWriteBatchLimits()
        {
            Mock.Get(_apiClient).Setup(client => client.GetAPIVersionAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync("2.16.2");
            Mock.Get(_apiClient).Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsAsync(ApiResult.CreateSuccess());
            var target = RenderPage();
            await SetSwitchValue(target, "ConfirmTorrentDeletion", true);
            await target.InvokeAsync(() => FindIconButton(target, Icons.Material.Outlined.Save).Instance.OnClick.InvokeAsync());
            Mock.Get(_apiClient).Verify(client => client.GetSpeedLimitsAsync(It.IsAny<CancellationToken>()), Times.Never());
            Mock.Get(_apiClient).Verify(client => client.SetSpeedLimitsAsync(It.IsAny<SpeedLimits>(), It.IsAny<CancellationToken>()), Times.Never());
            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(It.Is<UpdatePreferences>(preferences => preferences.ConfirmTorrentDeletion == true)), Times.Once());
            target.FindComponents<NavigationLock>().Should().BeEmpty();
        }

        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_SpeedLimitEdited_WHEN_Saving_THEN_UsesBatchApiOnlyWhenSupported(string version, bool supported)
        {
            Mock.Get(_apiClient).Setup(client => client.GetAPIVersionAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(version);
            Mock.Get(_apiClient).Setup(client => client.GetSpeedLimitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new SpeedLimits(1024, 2048, 3072, 4096));
            Mock.Get(_apiClient).Setup(client => client.SetSpeedLimitsAsync(It.IsAny<SpeedLimits>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResult.CreateSuccess());
            Mock.Get(_apiClient).Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsAsync(ApiResult.CreateSuccess());
            var target = RenderPage();
            await ActivateTab(target, 3);
            var upload = FindComponentByTestId<MudNumericField<int>>(target, "UpLimit");
            await target.InvokeAsync(() => upload.Instance.ValueChanged.InvokeAsync(0));
            await target.InvokeAsync(() => FindIconButton(target, Icons.Material.Outlined.Save).Instance.OnClick.InvokeAsync());
            Mock.Get(_apiClient).Verify(client => client.GetSpeedLimitsAsync(It.IsAny<CancellationToken>()), supported ? Times.Once() : Times.Never());
            Mock.Get(_apiClient).Verify(client => client.SetSpeedLimitsAsync(
                It.Is<SpeedLimits>(limits => limits.UploadLimit == 0 && limits.DownloadLimit == 2048 && limits.AlternativeUploadLimit == 3072 && limits.AlternativeDownloadLimit == 4096),
                It.IsAny<CancellationToken>()), supported ? Times.Once() : Times.Never());
            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(
                It.Is<UpdatePreferences>(preferences => supported ? preferences.UpLimit == null : preferences.UpLimit == 0)), Times.Once());
            target.FindComponents<NavigationLock>().Should().BeEmpty();
        }

        [Theory]
        [InlineData("UpLimit")]
        [InlineData("DlLimit")]
        [InlineData("AltUpLimit")]
        [InlineData("AltDlLimit")]
        public async Task GIVEN_Version53SpeedField_WHEN_Saving_THEN_PreservesOtherCurrentServerLimits(string field)
        {
            Mock.Get(_apiClient).Setup(client => client.GetAPIVersionAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync("2.16.2");
            Mock.Get(_apiClient).Setup(client => client.GetSpeedLimitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new SpeedLimits(1024, 2048, 3072, 4096));
            Mock.Get(_apiClient).Setup(client => client.SetSpeedLimitsAsync(It.IsAny<SpeedLimits>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResult.CreateSuccess());
            Mock.Get(_apiClient).Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsAsync(ApiResult.CreateSuccess());
            var target = RenderPage();
            await ActivateTab(target, 3);
            var input = FindComponentByTestId<MudNumericField<int>>(target, field);
            await target.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(10));
            await target.InvokeAsync(() => FindIconButton(target, Icons.Material.Outlined.Save).Instance.OnClick.InvokeAsync());
            Mock.Get(_apiClient).Verify(client => client.SetSpeedLimitsAsync(
                It.Is<SpeedLimits>(limits => limits.UploadLimit == (field == "UpLimit" ? 10240 : 1024)
                    && limits.DownloadLimit == (field == "DlLimit" ? 10240 : 2048)
                    && limits.AlternativeUploadLimit == (field == "AltUpLimit" ? 10240 : 3072)
                    && limits.AlternativeDownloadLimit == (field == "AltDlLimit" ? 10240 : 4096)), It.IsAny<CancellationToken>()), Times.Once());
            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(It.Is<UpdatePreferences>(preferences =>
                preferences.UpLimit == null && preferences.DlLimit == null && preferences.AltUpLimit == null && preferences.AltDlLimit == null)), Times.Once());
        }

        [Theory]
        [InlineData("Read")]
        [InlineData("BatchWrite")]
        [InlineData("PreferencesWrite")]
        public async Task GIVEN_SpeedSaveFailure_WHEN_Saving_THEN_RetainsPendingChangesForRetry(string operation)
        {
            var failure = new ApiFailure { Kind = ApiFailureKind.ServerError, Operation = operation, UserMessage = "Failure" };
            Mock.Get(_apiClient).Setup(client => client.GetAPIVersionAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync("2.16.2");
            Mock.Get(_apiClient).Setup(client => client.GetSpeedLimitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(operation == "Read" ? ApiResult.CreateFailure<SpeedLimits>(failure) : ApiResult.CreateSuccess(new SpeedLimits(1024, 2048, 3072, 4096)));
            Mock.Get(_apiClient).Setup(client => client.SetSpeedLimitsAsync(It.IsAny<SpeedLimits>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(operation == "BatchWrite" ? ApiResult.CreateFailure(failure) : ApiResult.CreateSuccess());
            Mock.Get(_apiClient).Setup(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()))
                .ReturnsAsync(operation == "PreferencesWrite" ? ApiResult.CreateFailure(failure) : ApiResult.CreateSuccess());
            var target = RenderPage();
            await ActivateTab(target, 3);
            var input = FindComponentByTestId<MudNumericField<int>>(target, "AltDlLimit");
            await target.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(10));
            await target.InvokeAsync(() => FindIconButton(target, Icons.Material.Outlined.Save).Instance.OnClick.InvokeAsync());
            FindIconButton(target, Icons.Material.Outlined.Save).Instance.Disabled.Should().BeFalse();
            target.FindComponents<NavigationLock>().Should().ContainSingle();
            input.Instance.GetState(component => component.Value).Should().Be(10);
            Mock.Get(_apiClient).Verify(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>()), operation == "PreferencesWrite" ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_ImmediateApiKeyChange_WHEN_SwitchingTabsAndUndoingOtherSettings_THEN_ShouldPreserveKeyWithoutPendingKeyEdit(bool delete)
        {
            Mock.Get(_apiClient).Setup(client => client.GetApplicationPreferencesAsync())
                .ReturnsSuccessAsync(PreferencesFactory.CreatePreferences(spec => spec.WebUiApiKey = "WebUiApiKey"));
            Mock.Get(_apiClient).Setup(client => client.RotateAPIKeyAsync(It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new ApiKey("Key"));
            Mock.Get(_apiClient).Setup(client => client.DeleteAPIKeyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResult.CreateSuccess());
            Mock.Get(_dialogWorkflow).Setup(service => service.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var target = RenderPage();
            await ActivateTab(target, 6);

            await target.InvokeAsync(() => FindButton(target, delete ? "ApiKeyDelete" : "ApiKeyRotate").Instance.OnClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));
            await ActivateTab(target, 0);
            await ActivateTab(target, 6);

            var expectedKey = delete ? string.Empty : "Key";
            FindComponentByTestId<MudTextField<string>>(target, "WebUiApiKey").Instance.GetState(field => field.Value).Should().Be(expectedKey);
            FindIconButton(target, Icons.Material.Outlined.Save).Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => FindComponentByTestId<MudTextField<string>>(target, "WebUiAddress").Instance.ValueChanged.InvokeAsync("WebUiAddress"));
            await target.InvokeAsync(() => FindIconButton(target, Icons.Material.Outlined.Undo).Instance.OnClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));

            FindComponentByTestId<MudTextField<string>>(target, "WebUiApiKey").Instance.GetState(field => field.Value).Should().Be(expectedKey);
            FindIconButton(target, Icons.Material.Outlined.Save).Instance.Disabled.Should().BeTrue();
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
