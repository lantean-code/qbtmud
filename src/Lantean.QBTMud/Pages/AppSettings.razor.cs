using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Text.Json;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Pages
{
    public partial class AppSettings
    {
        private const int _storageTabIndex = 3;
        private const int _pwaTabIndex = 4;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IAppSettingsService AppSettingsService { get; set; } = default!;

        [Inject]
        protected IStorageRoutingService StorageRoutingService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "LostConnection")]
        public bool LostConnection { get; set; }

        [CascadingParameter(Name = "AppSettings")]
        public AppSettingsModel? CascadedAppSettings { get; set; }

        protected bool IsLoading { get; private set; } = true;

        protected int ActiveTab { get; private set; }

        protected bool IsReloading { get; private set; }

        protected bool IsStorageBusy { get; private set; }

        protected int ReloadToken { get; private set; }

        protected AppSettingsModel Settings { get; private set; } = AppSettingsModel.Default.Clone();

        protected StorageRoutingSettings StorageRoutingSettings { get; private set; } = StorageRoutingSettings.Default.Clone();

        protected bool IsStorageTabActive => ActiveTab == _storageTabIndex;

        protected bool IsPwaTabActive => ActiveTab == _pwaTabIndex;

        protected bool HasPendingChanges
        {
            get
            {
                return !AreSettingsEquivalent(Settings, _savedSettings)
                    || !AreStorageRoutingEquivalent(StorageRoutingSettings, _savedStorageRoutingSettings);
            }
        }

        private AppSettingsModel _savedSettings = AppSettingsModel.Default.Clone();
        private StorageRoutingSettings _savedStorageRoutingSettings = StorageRoutingSettings.Default.Clone();

        protected override async Task OnInitializedAsync()
        {
            var storageRoutingSettingsTask = StorageRoutingService.GetSettingsAsync();
            if (CascadedAppSettings is not null)
            {
                Settings = CascadedAppSettings.Clone();
                _savedSettings = Settings.Clone();
            }
            else
            {
                Settings = await AppSettingsService.GetSettingsAsync();
                _savedSettings = Settings.Clone();
            }

            StorageRoutingSettings = await storageRoutingSettingsTask;
            _savedStorageRoutingSettings = StorageRoutingSettings.Clone();

            IsLoading = false;
        }

        protected Task OnActiveTabChanged(int activeTab)
        {
            ActiveTab = activeTab;
            return Task.CompletedTask;
        }

        protected Task OnSettingsChanged()
        {
            return InvokeAsync(StateHasChanged);
        }

        protected async Task OnNotificationsEnabledCorrected()
        {
            Settings.NotificationsEnabled = false;

            if (!_savedSettings.NotificationsEnabled)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            try
            {
                var correctedSettings = _savedSettings.Clone();
                correctedSettings.NotificationsEnabled = false;
                _savedSettings = await AppSettingsService.SaveSettingsAsync(correctedSettings);
            }
            catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or JsonException or JSException)
            {
                Debug.WriteLine(exception);
            }

            await InvokeAsync(StateHasChanged);
        }

        protected Task OnStorageRoutingChanged()
        {
            return InvokeAsync(StateHasChanged);
        }

        protected Task OnStorageBusyChanged(bool value)
        {
            IsStorageBusy = value;
            return InvokeAsync(StateHasChanged);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task ValidateExit(LocationChangingContext context)
        {
            if (!HasPendingChanges)
            {
                return;
            }

            var exit = await ShowDiscardChangesDialogAsync();

            if (!exit)
            {
                context.PreventNavigation();
            }
        }

        protected async Task Reload()
        {
            if (IsReloading || IsStorageBusy)
            {
                return;
            }

            if (HasPendingChanges)
            {
                var discardChanges = await ShowDiscardChangesDialogAsync();
                if (!discardChanges)
                {
                    return;
                }
            }

            IsReloading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var settingsTask = AppSettingsService.RefreshSettingsAsync();
                var storageRoutingSettingsTask = StorageRoutingService.GetSettingsAsync();

                await Task.WhenAll(settingsTask, storageRoutingSettingsTask);

                Settings = (await settingsTask).Clone();
                _savedSettings = Settings.Clone();

                StorageRoutingSettings = await storageRoutingSettingsTask;
                _savedStorageRoutingSettings = StorageRoutingSettings.Clone();

                ReloadToken++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to refresh app settings."), Severity.Error);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to refresh app settings."), Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to refresh app settings."), Severity.Error);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to refresh app settings."), Severity.Error);
            }
            finally
            {
                IsReloading = false;
            }
        }

        protected async Task Save()
        {
            if (!HasPendingChanges)
            {
                return;
            }

            var settingsChanged = !AreSettingsEquivalent(Settings, _savedSettings);
            var storageRoutingChanged = !AreStorageRoutingEquivalent(StorageRoutingSettings, _savedStorageRoutingSettings);

            if (storageRoutingChanged)
            {
                try
                {
                    StorageRoutingSettings = await StorageRoutingService.SaveSettingsAsync(StorageRoutingSettings);
                    _savedStorageRoutingSettings = StorageRoutingSettings.Clone();
                }
                catch (InvalidOperationException exception)
                {
                    SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to save storage settings: %1", exception.Message), Severity.Error);
                    return;
                }
                catch (HttpRequestException exception)
                {
                    Debug.WriteLine(exception);
                    SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to save storage settings."), Severity.Error);
                    return;
                }
                catch (JsonException exception)
                {
                    Debug.WriteLine(exception);
                    SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to save storage settings."), Severity.Error);
                    return;
                }
                catch (JSException exception)
                {
                    Debug.WriteLine(exception);
                    SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to save storage settings."), Severity.Error);
                    return;
                }
            }

            if (settingsChanged)
            {
                Settings = await AppSettingsService.SaveSettingsAsync(Settings);
                _savedSettings = Settings.Clone();
            }

            if (storageRoutingChanged)
            {
                ReloadToken++;
            }

            SnackbarWorkflow.ShowTransientMessage(TranslateSettings("App settings saved."), Severity.Success);
        }

        protected Task Undo()
        {
            if (!HasPendingChanges)
            {
                return Task.CompletedTask;
            }

            Settings = _savedSettings.Clone();
            StorageRoutingSettings = _savedStorageRoutingSettings.Clone();

            return InvokeAsync(StateHasChanged);
        }

        private static bool AreSettingsEquivalent(AppSettingsModel left, AppSettingsModel right)
        {
            return left.UpdateChecksEnabled == right.UpdateChecksEnabled
                && left.NotificationsEnabled == right.NotificationsEnabled
                && left.ThemeModePreference == right.ThemeModePreference
                && left.DownloadFinishedNotificationsEnabled == right.DownloadFinishedNotificationsEnabled
                && left.TorrentAddedNotificationsEnabled == right.TorrentAddedNotificationsEnabled
                && left.TorrentAddedSnackbarsEnabledWithNotifications == right.TorrentAddedSnackbarsEnabledWithNotifications
                && string.Equals(left.DismissedReleaseTag, right.DismissedReleaseTag, StringComparison.Ordinal)
                && string.Equals(left.ThemeRepositoryIndexUrl, right.ThemeRepositoryIndexUrl, StringComparison.Ordinal);
        }

        private static bool AreStorageRoutingEquivalent(StorageRoutingSettings left, StorageRoutingSettings right)
        {
            if (left.MasterStorageType != right.MasterStorageType)
            {
                return false;
            }

            if (left.GroupStorageTypes.Count != right.GroupStorageTypes.Count)
            {
                return false;
            }

            if (left.ItemStorageTypes.Count != right.ItemStorageTypes.Count)
            {
                return false;
            }

            foreach (var (key, value) in left.GroupStorageTypes)
            {
                if (!right.GroupStorageTypes.TryGetValue(key, out var rightValue) || rightValue != value)
                {
                    return false;
                }
            }

            foreach (var (key, value) in left.ItemStorageTypes)
            {
                if (!right.ItemStorageTypes.TryGetValue(key, out var rightValue) || rightValue != value)
                {
                    return false;
                }
            }

            return true;
        }

        private Task<bool> ShowDiscardChangesDialogAsync()
        {
            return DialogWorkflow.ShowConfirmDialog(
                TranslateSettings("Unsaved Changes"),
                TranslateSettings("Are you sure you want to leave without saving your changes?"));
        }

        private string TranslateSettings(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppSettings", source, arguments);
        }

        private string TranslateUpdates(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppUpdates", source, arguments);
        }

        private string TranslateNotifications(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppNotifications", source, arguments);
        }

        private string TranslatePwa(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppPwaInstallPrompt", source, arguments);
        }
    }
}
