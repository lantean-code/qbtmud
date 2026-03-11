using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class NotificationsAppSettingsTab
    {
        [Parameter]
        [EditorRequired]
        public AppSettingsModel Settings { get; set; } = default!;

        [Parameter]
        public EventCallback SettingsChanged { get; set; }

        [Parameter]
        public int ReloadToken { get; set; }

        [Inject]
        protected ITorrentCompletionNotificationService TorrentCompletionNotificationService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        protected BrowserNotificationPermission NotificationPermission { get; private set; } = BrowserNotificationPermission.Unsupported;

        protected bool IsApplyingNotificationToggle { get; private set; }

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

        private int _loadedReloadToken = -1;

        protected override async Task OnParametersSetAsync()
        {
            if (_loadedReloadToken == ReloadToken)
            {
                return;
            }

            _loadedReloadToken = ReloadToken;
            NotificationPermission = await GetNotificationPermissionSafeAsync();
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

                await NotifySettingsChangedAsync();
            }
            catch (JSException exception)
            {
                Settings.NotificationsEnabled = false;
                SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Unable to update notification permission: %1", exception.Message), Severity.Error);
                await NotifySettingsChangedAsync();
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
            await NotifySettingsChangedAsync();
        }

        protected async Task OnDownloadFinishedNotificationsChanged(bool value)
        {
            if (Settings.DownloadFinishedNotificationsEnabled == value)
            {
                return;
            }

            Settings.DownloadFinishedNotificationsEnabled = value;
            await NotifySettingsChangedAsync();
        }

        protected async Task OnTorrentAddedSnackbarsWithNotificationsChanged(bool value)
        {
            if (Settings.TorrentAddedSnackbarsEnabledWithNotifications == value)
            {
                return;
            }

            Settings.TorrentAddedSnackbarsEnabledWithNotifications = value;
            await NotifySettingsChangedAsync();
        }

        private async Task NotifySettingsChangedAsync()
        {
            await SettingsChanged.InvokeAsync();
        }

        private string GetNotificationPermissionText()
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

        private Color GetNotificationPermissionColor()
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

        private async Task<BrowserNotificationPermission> GetNotificationPermissionSafeAsync()
        {
            try
            {
                return await TorrentCompletionNotificationService.GetPermissionAsync();
            }
            catch (JSException)
            {
                return BrowserNotificationPermission.Unsupported;
            }
            catch (InvalidOperationException)
            {
                return BrowserNotificationPermission.Unsupported;
            }
            catch (HttpRequestException)
            {
                return BrowserNotificationPermission.Unsupported;
            }
        }

        private string TranslateNotifications(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppNotifications", source, arguments);
        }
    }
}
