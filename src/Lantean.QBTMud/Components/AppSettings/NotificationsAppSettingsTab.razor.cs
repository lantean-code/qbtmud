using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class NotificationsAppSettingsTab : IAsyncDisposable
    {
        [Parameter]
        [EditorRequired]
        public AppSettingsModel Settings { get; set; } = default!;

        [Parameter]
        public EventCallback SettingsChanged { get; set; }

        [Parameter]
        public EventCallback<AppSettingsModel> SettingsCorrected { get; set; }

        [Parameter]
        public int ReloadToken { get; set; }

        [Inject]
        protected IBrowserNotificationService BrowserNotificationService { get; set; } = default!;

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
                    BrowserNotificationPermission.Insecure => true,
                    BrowserNotificationPermission.Unsupported => true,
                    _ => true
                };
            }
        }

        private int _loadedReloadToken = -1;
        private AppSettingsModel? _pendingCorrectedSettings;
        private DotNetObjectReference<NotificationsAppSettingsTab>? _dotNetObjectReference;
        private long _notificationPermissionSubscriptionId;
        private bool _notificationPermissionSubscriptionRequested;

        protected override async Task OnParametersSetAsync()
        {
            if (_loadedReloadToken == ReloadToken)
            {
                return;
            }

            _loadedReloadToken = ReloadToken;
            NotificationPermission = await GetNotificationPermissionSafeAsync();

            if (ShouldDisableNotificationsSetting(NotificationPermission) && Settings.NotificationsEnabled)
            {
                Settings.NotificationsEnabled = false;
                _pendingCorrectedSettings = Settings.Clone();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !_notificationPermissionSubscriptionRequested)
            {
                await SubscribeToNotificationPermissionChangesAsync();
            }

            if (_pendingCorrectedSettings is null)
            {
                return;
            }

            var correctedSettings = _pendingCorrectedSettings;
            _pendingCorrectedSettings = null;

            await NotifySettingsCorrectedAsync(correctedSettings);
        }

        /// <summary>
        /// Updates the notification permission state after the browser reports a permissions change.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [JSInvokable]
        public async Task OnNotificationPermissionChanged()
        {
            if (IsApplyingNotificationToggle)
            {
                return;
            }

            var parsedPermission = await GetNotificationPermissionSafeAsync();
            var shouldDisableNotifications = ShouldDisableNotificationsSetting(parsedPermission) && Settings.NotificationsEnabled;
            if (NotificationPermission == parsedPermission && !shouldDisableNotifications)
            {
                return;
            }

            NotificationPermission = parsedPermission;

            if (shouldDisableNotifications)
            {
                Settings.NotificationsEnabled = false;
                await NotifySettingsCorrectedAsync(Settings.Clone());
                await InvokeAsync(StateHasChanged);
                return;
            }

            await InvokeAsync(StateHasChanged);
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
                    NotificationPermission = await BrowserNotificationService.RequestPermissionAsync();
                    Settings.NotificationsEnabled = NotificationPermission == BrowserNotificationPermission.Granted;

                    if (NotificationPermission == BrowserNotificationPermission.Insecure)
                    {
                        SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Browser notifications require HTTPS or localhost."), Severity.Warning);
                    }
                    else if (!Settings.NotificationsEnabled)
                    {
                        SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Browser notification permission was not granted."), Severity.Warning);
                    }
                }
                else
                {
                    Settings.NotificationsEnabled = false;
                    NotificationPermission = await BrowserNotificationService.GetPermissionAsync();
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

        private async Task NotifySettingsCorrectedAsync(AppSettingsModel settings)
        {
            if (SettingsCorrected.HasDelegate)
            {
                await SettingsCorrected.InvokeAsync(settings);
                return;
            }

            await NotifySettingsChangedAsync();
        }

        private string GetNotificationPermissionText()
        {
            return NotificationPermission switch
            {
                BrowserNotificationPermission.Granted => TranslateNotifications("Granted"),
                BrowserNotificationPermission.Denied => TranslateNotifications("Denied"),
                BrowserNotificationPermission.Default => TranslateNotifications("Not requested"),
                BrowserNotificationPermission.Insecure => TranslateNotifications("Requires HTTPS or localhost"),
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
                BrowserNotificationPermission.Insecure => Color.Warning,
                BrowserNotificationPermission.Unsupported => Color.Default,
                _ => Color.Default
            };
        }

        private string GetNotificationUnavailableMessage()
        {
            return TranslateNotifications("Browser notifications require HTTPS or localhost.");
        }

        private async Task<BrowserNotificationPermission> GetNotificationPermissionSafeAsync()
        {
            try
            {
                return await BrowserNotificationService.GetPermissionAsync();
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

        /// <summary>
        /// Releases notification permission change subscriptions held by the component.
        /// </summary>
        /// <returns>A task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await BrowserNotificationService.UnsubscribePermissionChangesAsync(_notificationPermissionSubscriptionId);

            _dotNetObjectReference?.Dispose();
            _dotNetObjectReference = null;
            _notificationPermissionSubscriptionId = 0;
            _notificationPermissionSubscriptionRequested = false;
        }

        private static bool ShouldDisableNotificationsSetting(BrowserNotificationPermission permission)
        {
            return permission switch
            {
                BrowserNotificationPermission.Granted => false,
                BrowserNotificationPermission.Default => true,
                BrowserNotificationPermission.Denied => true,
                BrowserNotificationPermission.Insecure => true,
                BrowserNotificationPermission.Unsupported => true,
                _ => true
            };
        }

        private async Task SubscribeToNotificationPermissionChangesAsync()
        {
            _notificationPermissionSubscriptionRequested = true;
            _dotNetObjectReference ??= DotNetObjectReference.Create(this);
            _notificationPermissionSubscriptionId = await BrowserNotificationService.SubscribePermissionChangesAsync(_dotNetObjectReference);
        }

        private string TranslateNotifications(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppNotifications", source, arguments);
        }
    }
}
