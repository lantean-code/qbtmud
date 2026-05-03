using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Core.Models.AppSettings;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class NotificationsAppSettingsTab : IAsyncDisposable
    {
        private const string _notificationSynchronizationErrorText = "Unable to synchronize notification settings.";

        [Parameter]
        [EditorRequired]
        public AppSettingsModel Settings { get; set; } = default!;

        [Parameter]
        public EventCallback SettingsChanged { get; set; }

        [Parameter]
        public EventCallback NotificationsEnabledCorrected { get; set; }

        [Parameter]
        public int ReloadToken { get; set; }

        [Inject]
        protected IBrowserNotificationService BrowserNotificationService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        protected BrowserNotificationPermission NotificationPermission { get; private set; } = BrowserNotificationPermission.Unknown;

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
                    BrowserNotificationPermission.Unknown => false,
                    BrowserNotificationPermission.Insecure => true,
                    BrowserNotificationPermission.Unsupported => true,
                    _ => true
                };
            }
        }

        private int _loadedReloadToken = -1;
        private bool _pendingNotificationsEnabledCorrection;
        private bool _pendingNotificationEnableRequest;
        private BrowserNotificationPermission? _pendingPermissionChange;
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
                _pendingNotificationsEnabledCorrection = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_notificationPermissionSubscriptionId <= 0 && !_notificationPermissionSubscriptionRequested)
            {
                await SubscribeToNotificationPermissionChangesAsync();
            }

            if (!_pendingNotificationsEnabledCorrection)
            {
                return;
            }

            _pendingNotificationsEnabledCorrection = false;

            await NotifyNotificationsEnabledCorrectedAsync();
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
                    _pendingNotificationEnableRequest = true;
                    NotificationPermission = await BrowserNotificationService.RequestPermissionAsync();
                    Settings.NotificationsEnabled = NotificationPermission == BrowserNotificationPermission.Granted;

                    if (NotificationPermission == BrowserNotificationPermission.Granted)
                    {
                        _pendingNotificationEnableRequest = false;
                    }
                    else if (NotificationPermission == BrowserNotificationPermission.Unknown)
                    {
                        _pendingNotificationEnableRequest = false;
                        SnackbarWorkflow.ShowTransientMessage(TranslateNotifications(_notificationSynchronizationErrorText), Severity.Error);
                    }
                    else if (NotificationPermission == BrowserNotificationPermission.Insecure)
                    {
                        _pendingNotificationEnableRequest = false;
                        SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Browser notifications require HTTPS or localhost."), Severity.Warning);
                    }
                    else if (NotificationPermission != BrowserNotificationPermission.Default)
                    {
                        _pendingNotificationEnableRequest = false;
                        SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Browser notification permission was not granted."), Severity.Warning);
                    }
                }
                else
                {
                    _pendingNotificationEnableRequest = false;
                    Settings.NotificationsEnabled = false;
                    NotificationPermission = await BrowserNotificationService.GetPermissionAsync();
                }

                await NotifySettingsChangedAsync();
            }
            catch (JSException exception)
            {
                _pendingNotificationEnableRequest = false;
                Settings.NotificationsEnabled = false;
                SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Unable to update notification permission: %1", exception.Message), Severity.Error);
                await NotifySettingsChangedAsync();
            }
            finally
            {
                IsApplyingNotificationToggle = false;
                await ApplyPendingPermissionChangeAsync();
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

        private async Task NotifyNotificationsEnabledCorrectedAsync()
        {
            if (NotificationsEnabledCorrected.HasDelegate)
            {
                await NotificationsEnabledCorrected.InvokeAsync();
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
                BrowserNotificationPermission.Unknown => TranslateNotifications("Unknown"),
                BrowserNotificationPermission.Insecure => TranslateNotifications("Insecure"),
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
                BrowserNotificationPermission.Unknown => Color.Default,
                BrowserNotificationPermission.Insecure => Color.Warning,
                BrowserNotificationPermission.Unsupported => Color.Default,
                _ => Color.Default
            };
        }

        private string GetNotificationUnavailableMessage()
        {
            return TranslateNotifications("Browser notifications require HTTPS or localhost.");
        }

        private string GetNotificationSwitchKey()
        {
            return $"{NotificationPermission}:{IsApplyingNotificationToggle}:{Settings.NotificationsEnabled}";
        }

        private async Task<BrowserNotificationPermission> GetNotificationPermissionSafeAsync()
        {
            try
            {
                return await BrowserNotificationService.GetPermissionAsync();
            }
            catch (JSException)
            {
                return BrowserNotificationPermission.Unknown;
            }
            catch (InvalidOperationException)
            {
                return BrowserNotificationPermission.Unknown;
            }
            catch (HttpRequestException)
            {
                return BrowserNotificationPermission.Unknown;
            }
        }

        private static bool ShouldDisableNotificationsSetting(BrowserNotificationPermission permission)
        {
            return permission switch
            {
                BrowserNotificationPermission.Granted => false,
                BrowserNotificationPermission.Default => true,
                BrowserNotificationPermission.Unknown => false,
                BrowserNotificationPermission.Denied => true,
                BrowserNotificationPermission.Insecure => true,
                BrowserNotificationPermission.Unsupported => true,
                _ => true
            };
        }

        /// <summary>
        /// Updates the notification permission state after the browser reports a permissions change.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [JSInvokable]
        public async Task OnNotificationPermissionChanged()
        {
            try
            {
                await HandlePermissionChangedAsync(await GetNotificationPermissionSafeAsync());
            }
            catch (Exception)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateNotifications(_notificationSynchronizationErrorText), Severity.Error);
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

        private string TranslateNotifications(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppNotifications", source, arguments);
        }

        private async Task HandlePermissionChangedAsync(BrowserNotificationPermission parsedPermission)
        {
            if (IsApplyingNotificationToggle)
            {
                _pendingPermissionChange = parsedPermission;
                return;
            }

            var shouldEnableNotifications = _pendingNotificationEnableRequest
                && parsedPermission == BrowserNotificationPermission.Granted
                && !Settings.NotificationsEnabled;
            var shouldDisableNotifications = ShouldDisableNotificationsSetting(parsedPermission) && Settings.NotificationsEnabled;
            if (NotificationPermission == parsedPermission && !shouldEnableNotifications && !shouldDisableNotifications)
            {
                return;
            }

            NotificationPermission = parsedPermission;

            if (shouldEnableNotifications)
            {
                _pendingNotificationEnableRequest = false;
                Settings.NotificationsEnabled = true;
                await NotifySettingsChangedAsync();
                await InvokeAsync(StateHasChanged);
                return;
            }

            if (shouldDisableNotifications)
            {
                _pendingNotificationEnableRequest = false;
                Settings.NotificationsEnabled = false;
                await NotifyNotificationsEnabledCorrectedAsync();
                await InvokeAsync(StateHasChanged);
                return;
            }

            if (parsedPermission != BrowserNotificationPermission.Default)
            {
                _pendingNotificationEnableRequest = false;
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task SubscribeToNotificationPermissionChangesAsync()
        {
            _notificationPermissionSubscriptionRequested = true;
            _dotNetObjectReference ??= DotNetObjectReference.Create(this);

            for (var attempt = 0; attempt < 3 && _notificationPermissionSubscriptionId <= 0; attempt++)
            {
                _notificationPermissionSubscriptionId = await BrowserNotificationService.SubscribePermissionChangesAsync(_dotNetObjectReference);
                if (_notificationPermissionSubscriptionId > 0)
                {
                    break;
                }

                await Task.Yield();
            }

            if (_notificationPermissionSubscriptionId <= 0)
            {
                _notificationPermissionSubscriptionRequested = false;
            }
        }

        private async Task ApplyPendingPermissionChangeAsync()
        {
            if (_pendingPermissionChange is not BrowserNotificationPermission permission)
            {
                return;
            }

            _pendingPermissionChange = null;
            await HandlePermissionChangedAsync(permission);
        }
    }
}
