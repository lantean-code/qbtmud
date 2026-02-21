using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class AppSettings
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IAppBuildInfoService AppBuildInfoService { get; set; } = default!;

        [Inject]
        protected IAppSettingsService AppSettingsService { get; set; } = default!;

        [Inject]
        protected IAppUpdateService AppUpdateService { get; set; } = default!;

        [Inject]
        protected ITorrentCompletionNotificationService TorrentCompletionNotificationService { get; set; } = default!;

        [Inject]
        protected IStorageDiagnosticsService StorageDiagnosticsService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected bool IsLoading { get; private set; } = true;

        protected int ActiveTab { get; set; }

        protected bool IsCheckingUpdates { get; private set; }

        protected bool IsApplyingNotificationToggle { get; private set; }

        protected bool IsStorageBusy { get; private set; }

        protected AppBuildInfo CurrentBuildInfo { get; private set; } = new("unknown", "Unavailable");

        protected AppUpdateStatus? UpdateStatus { get; private set; }

        protected BrowserNotificationPermission NotificationPermission { get; private set; } = BrowserNotificationPermission.Unsupported;

        protected Models.AppSettings Settings { get; private set; } = Models.AppSettings.Default.Clone();

        protected IReadOnlyList<AppStorageEntry> StorageEntries { get; private set; } = Array.Empty<AppStorageEntry>();

        protected bool IsNotificationsUnavailable
        {
            get
            {
                return NotificationPermission switch
                {
                    BrowserNotificationPermission.Granted => false,
                    BrowserNotificationPermission.Denied => false,
                    BrowserNotificationPermission.Default => false,
                    BrowserNotificationPermission.Unsupported => true,
                    _ => true
                };
            }
        }

        protected override async Task OnInitializedAsync()
        {
            CurrentBuildInfo = AppBuildInfoService.GetCurrentBuildInfo();

            Settings = await AppSettingsService.GetSettingsAsync();

            try
            {
                NotificationPermission = await TorrentCompletionNotificationService.GetPermissionAsync();
            }
            catch (JSException)
            {
                NotificationPermission = BrowserNotificationPermission.Unsupported;
            }

            try
            {
                UpdateStatus = await AppUpdateService.GetUpdateStatusAsync();
            }
            catch
            {
                UpdateStatus = null;
            }

            await RefreshStorageEntriesAsync();

            IsLoading = false;
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task OnUpdateChecksChanged(bool value)
        {
            if (Settings.UpdateChecksEnabled == value)
            {
                return;
            }

            Settings.UpdateChecksEnabled = value;
            await PersistSettingsAsync();
        }

        protected async Task OnNotificationsEnabledChanged(bool value)
        {
            if (IsApplyingNotificationToggle)
            {
                return;
            }

            if (value && IsNotificationsUnavailable)
            {
                return;
            }

            IsApplyingNotificationToggle = true;

            try
            {
                if (value)
                {
                    NotificationPermission = await TorrentCompletionNotificationService.RequestPermissionAsync();
                    Settings.NotificationsEnabled = NotificationPermission == BrowserNotificationPermission.Granted;

                    if (!Settings.NotificationsEnabled)
                    {
                        SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Browser notification permission was not granted."), Severity.Warning);
                    }
                }
                else
                {
                    Settings.NotificationsEnabled = false;
                    NotificationPermission = await TorrentCompletionNotificationService.GetPermissionAsync();
                }

                await PersistSettingsAsync();
            }
            catch (JSException exception)
            {
                Settings.NotificationsEnabled = false;
                await PersistSettingsAsync();
                SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Unable to update notification permission: %1", exception.Message), Severity.Error);
            }
            finally
            {
                IsApplyingNotificationToggle = false;
            }
        }

        protected async Task OnTorrentAddedNotificationsChanged(bool value)
        {
            if (Settings.TorrentAddedNotificationsEnabled == value)
            {
                return;
            }

            Settings.TorrentAddedNotificationsEnabled = value;
            await PersistSettingsAsync();
        }

        protected async Task OnDownloadFinishedNotificationsChanged(bool value)
        {
            if (Settings.DownloadFinishedNotificationsEnabled == value)
            {
                return;
            }

            Settings.DownloadFinishedNotificationsEnabled = value;
            await PersistSettingsAsync();
        }

        protected async Task OnTorrentAddedSnackbarsWithNotificationsChanged(bool value)
        {
            if (Settings.TorrentAddedSnackbarsEnabledWithNotifications == value)
            {
                return;
            }

            Settings.TorrentAddedSnackbarsEnabledWithNotifications = value;
            await PersistSettingsAsync();
        }

        protected async Task CheckForUpdatesNowAsync()
        {
            if (IsCheckingUpdates)
            {
                return;
            }

            IsCheckingUpdates = true;

            try
            {
                UpdateStatus = await AppUpdateService.GetUpdateStatusAsync(forceRefresh: true);
            }
            catch
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateUpdates("Unable to check for updates."), Severity.Warning);
            }
            finally
            {
                IsCheckingUpdates = false;
            }
        }

        protected async Task RefreshStorageEntriesAsync()
        {
            if (IsStorageBusy)
            {
                return;
            }

            IsStorageBusy = true;

            try
            {
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
            }
            catch
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load local storage entries."), Severity.Error);
            }
            finally
            {
                IsStorageBusy = false;
            }
        }

        protected async Task RemoveStorageEntryAsync(string key)
        {
            if (IsStorageBusy)
            {
                return;
            }

            IsStorageBusy = true;

            try
            {
                await StorageDiagnosticsService.RemoveEntryAsync(key);
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
            }
            catch
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove local storage entry."), Severity.Error);
            }
            finally
            {
                IsStorageBusy = false;
            }
        }

        protected async Task ClearStorageEntriesAsync()
        {
            if (IsStorageBusy)
            {
                return;
            }

            IsStorageBusy = true;

            try
            {
                var removed = await StorageDiagnosticsService.ClearEntriesAsync();
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Removed %1 local storage entries.", removed), Severity.Info);
            }
            catch
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear local storage entries."), Severity.Error);
            }
            finally
            {
                IsStorageBusy = false;
            }
        }

        protected string GetLatestReleaseTag()
        {
            return UpdateStatus?.LatestRelease?.TagName ?? TranslateUpdates("Not available");
        }

        protected string GetUpdateStatusText()
        {
            if (UpdateStatus is null || UpdateStatus.LatestRelease is null || !UpdateStatus.CanCompareVersions)
            {
                return TranslateUpdates("Not available");
            }

            return UpdateStatus.IsUpdateAvailable
                ? TranslateUpdates("Update available")
                : TranslateUpdates("Up to date");
        }

        protected string GetNotificationPermissionText()
        {
            return NotificationPermission switch
            {
                BrowserNotificationPermission.Granted => TranslateNotifications("Granted"),
                BrowserNotificationPermission.Denied => TranslateNotifications("Denied"),
                BrowserNotificationPermission.Default => TranslateNotifications("Not requested"),
                BrowserNotificationPermission.Unsupported => TranslateNotifications("Unsupported"),
                _ => TranslateNotifications("Unsupported")
            };
        }

        protected Color GetNotificationPermissionColor()
        {
            return NotificationPermission switch
            {
                BrowserNotificationPermission.Granted => Color.Success,
                BrowserNotificationPermission.Denied => Color.Error,
                BrowserNotificationPermission.Default => Color.Warning,
                BrowserNotificationPermission.Unsupported => Color.Default,
                _ => Color.Default
            };
        }

        private async Task PersistSettingsAsync()
        {
            Settings = await AppSettingsService.SaveSettingsAsync(Settings);
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
    }
}
