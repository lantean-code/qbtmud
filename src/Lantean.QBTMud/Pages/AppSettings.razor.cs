using Lantean.QBTMud.Interop;
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
        private const int StorageTabIndex = 3;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

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
        protected IStorageCatalogService StorageCatalogService { get; set; } = default!;

        [Inject]
        protected IStorageRoutingService StorageRoutingService { get; set; } = default!;

        [Inject]
        protected IWebApiCapabilityService WebApiCapabilityService { get; set; } = default!;

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

        protected bool IsCheckingUpdates { get; private set; }

        protected bool IsApplyingNotificationToggle { get; private set; }

        protected bool IsStorageBusy { get; private set; }

        protected bool IsReloading { get; private set; }

        protected AppBuildInfo CurrentBuildInfo { get; private set; } = new("unknown", "Unavailable");

        protected AppUpdateStatus? UpdateStatus { get; private set; }

        protected BrowserNotificationPermission NotificationPermission { get; private set; } = BrowserNotificationPermission.Unsupported;

        protected AppSettingsModel Settings { get; private set; } = AppSettingsModel.Default.Clone();

        protected StorageRoutingSettings StorageRoutingSettings { get; private set; } = StorageRoutingSettings.Default.Clone();

        protected WebApiCapabilityState WebApiCapabilityState { get; private set; } = new(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);

        protected IReadOnlyList<AppStorageEntry> StorageEntries { get; private set; } = Array.Empty<AppStorageEntry>();

        protected IReadOnlyList<StorageCatalogGroupDefinition> StorageGroups => StorageCatalogService.Groups;

        protected bool SupportsClientData => WebApiCapabilityState.SupportsClientData;

        protected bool IsStorageTabActivated { get; private set; }

        protected bool IsLoadingInitialStorageEntries => IsStorageTabActivated && !_hasLoadedInitialStorageEntries && IsStorageBusy;

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
        private readonly HashSet<string> _expandedStorageOverrideGroups = new(StringComparer.Ordinal);
        private bool _hasLoadedDeferredState;
        private bool _isStorageEntriesLoadRequested;
        private bool _hasLoadedInitialStorageEntries;

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !_hasLoadedDeferredState)
            {
                _hasLoadedDeferredState = true;
                await LoadDeferredStateAsync();
            }

            if (_isStorageEntriesLoadRequested && !_hasLoadedInitialStorageEntries)
            {
                _isStorageEntriesLoadRequested = false;
                await RefreshStorageEntriesAsync();
                _hasLoadedInitialStorageEntries = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected Task OnActiveTabChanged(int activeTab)
        {
            ActiveTab = activeTab;

            if (activeTab == StorageTabIndex)
            {
                EnsureStorageTabActivated();
            }

            return Task.CompletedTask;
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected void OnUpdateChecksChanged(bool value)
        {
            if (Settings.UpdateChecksEnabled == value)
            {
                return;
            }

            Settings.UpdateChecksEnabled = value;
        }

        protected void OnThemeModePreferenceChanged(ThemeModePreference value)
        {
            if (Settings.ThemeModePreference == value)
            {
                return;
            }

            Settings.ThemeModePreference = value;
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
            }
            catch (JSException exception)
            {
                Settings.NotificationsEnabled = false;
                SnackbarWorkflow.ShowTransientMessage(TranslateNotifications("Unable to update notification permission: %1", exception.Message), Severity.Error);
            }
            finally
            {
                IsApplyingNotificationToggle = false;
            }
        }

        protected void OnTorrentAddedNotificationsChanged(bool value)
        {
            if (Settings.TorrentAddedNotificationsEnabled == value)
            {
                return;
            }

            Settings.TorrentAddedNotificationsEnabled = value;
        }

        protected void OnDownloadFinishedNotificationsChanged(bool value)
        {
            if (Settings.DownloadFinishedNotificationsEnabled == value)
            {
                return;
            }

            Settings.DownloadFinishedNotificationsEnabled = value;
        }

        protected void OnTorrentAddedSnackbarsWithNotificationsChanged(bool value)
        {
            if (Settings.TorrentAddedSnackbarsEnabledWithNotifications == value)
            {
                return;
            }

            Settings.TorrentAddedSnackbarsEnabledWithNotifications = value;
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
                var webApiCapabilityTask = GetWebApiCapabilityStateAsync();
                var notificationPermissionTask = GetNotificationPermissionSafeAsync();
                var updateStatusTask = GetUpdateStatusSafeAsync();
                Task<IReadOnlyList<AppStorageEntry>>? storageEntriesTask = null;

                if (IsStorageTabActivated)
                {
                    IsStorageBusy = true;
                    storageEntriesTask = StorageDiagnosticsService.GetEntriesAsync();
                }

                var pendingTasks = new List<Task>
                {
                    settingsTask,
                    storageRoutingSettingsTask,
                    webApiCapabilityTask,
                    notificationPermissionTask,
                    updateStatusTask
                };

                if (storageEntriesTask is not null)
                {
                    pendingTasks.Add(storageEntriesTask);
                }

                await Task.WhenAll(pendingTasks);

                Settings = (await settingsTask).Clone();
                _savedSettings = Settings.Clone();

                StorageRoutingSettings = await storageRoutingSettingsTask;
                _savedStorageRoutingSettings = StorageRoutingSettings.Clone();

                WebApiCapabilityState = await webApiCapabilityTask;
                NotificationPermission = await notificationPermissionTask;
                UpdateStatus = await updateStatusTask;

                if (storageEntriesTask is not null)
                {
                    StorageEntries = await storageEntriesTask;
                    _hasLoadedInitialStorageEntries = true;
                    _isStorageEntriesLoadRequested = false;
                }
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
                IsStorageBusy = false;
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

            if (IsStorageTabActivated)
            {
                await RefreshStorageEntriesAsync();
            }
            SnackbarWorkflow.ShowTransientMessage(TranslateSettings("App settings saved."), Severity.Success);
        }

        protected void Undo()
        {
            if (!HasPendingChanges)
            {
                return;
            }

            Settings = _savedSettings.Clone();
            StorageRoutingSettings = _savedStorageRoutingSettings.Clone();
        }

        protected void OnMasterStorageTypeChanged(StorageType storageType)
        {
            if (StorageRoutingSettings.MasterStorageType == storageType
                && StorageRoutingSettings.GroupStorageTypes.Count == 0
                && StorageRoutingSettings.ItemStorageTypes.Count == 0)
            {
                return;
            }

            StorageRoutingSettings.MasterStorageType = storageType;
            StorageRoutingSettings.GroupStorageTypes.Clear();
            StorageRoutingSettings.ItemStorageTypes.Clear();
        }

        protected StorageType GetGroupStorageTypeValue(string groupId)
        {
            if (StorageRoutingSettings.GroupStorageTypes.TryGetValue(groupId, out var storageType))
            {
                return storageType;
            }

            return StorageRoutingSettings.MasterStorageType;
        }

        protected void OnGroupStorageTypeChanged(string groupId, StorageType storageType)
        {
            var group = StorageGroups.FirstOrDefault(candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal));
            var clearedOverrides = group is null
                ? 0
                : ClearGroupItemOverrides(group);

            var storageTypeChanged = false;
            if (storageType == StorageRoutingSettings.MasterStorageType)
            {
                storageTypeChanged = StorageRoutingSettings.GroupStorageTypes.Remove(groupId);
            }
            else if (!StorageRoutingSettings.GroupStorageTypes.TryGetValue(groupId, out var configuredStorageType)
                || configuredStorageType != storageType)
            {
                StorageRoutingSettings.GroupStorageTypes[groupId] = storageType;
                storageTypeChanged = true;
            }

            if (!storageTypeChanged && clearedOverrides == 0)
            {
                return;
            }

            if (group is null)
            {
                return;
            }

            var groupName = TranslateSettings(group.DisplayNameSource);
            if (clearedOverrides > 0)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Applied to all items in %1; cleared %2 item overrides.", groupName, clearedOverrides), Severity.Info);
                return;
            }

            SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Applied to all items in %1.", groupName), Severity.Info);
        }

        protected StorageType GetItemStorageTypeValue(StorageCatalogItemDefinition item)
        {
            if (StorageRoutingSettings.ItemStorageTypes.TryGetValue(item.Id, out var storageType))
            {
                return storageType;
            }

            return GetGroupStorageTypeValue(item.GroupId);
        }

        protected void OnItemStorageTypeChanged(StorageCatalogItemDefinition item, StorageType storageType)
        {
            var groupStorageType = GetGroupStorageTypeValue(item.GroupId);
            if (storageType == groupStorageType)
            {
                StorageRoutingSettings.ItemStorageTypes.Remove(item.Id);
                return;
            }

            StorageRoutingSettings.ItemStorageTypes[item.Id] = storageType;
        }

        protected int GetGroupOverrideCount(StorageCatalogGroupDefinition group)
        {
            return group.Items.Count(item => StorageRoutingSettings.ItemStorageTypes.ContainsKey(item.Id));
        }

        protected bool IsGroupOverridesExpanded(string groupId)
        {
            return _expandedStorageOverrideGroups.Contains(groupId);
        }

        protected void SetGroupOverridesExpanded(string groupId, bool expanded)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return;
            }

            if (expanded)
            {
                _expandedStorageOverrideGroups.Add(groupId);
            }
            else
            {
                _expandedStorageOverrideGroups.Remove(groupId);
            }
        }

        protected string GetGroupOverridesToggleText(StorageCatalogGroupDefinition group)
        {
            var groupName = TranslateSettings(group.DisplayNameSource);
            return IsGroupOverridesExpanded(group.Id)
                ? TranslateSettings("Hide %1 storage overrides", groupName)
                : TranslateSettings("Show %1 storage overrides", groupName);
        }

        protected void OnClearGroupOverrides(string groupId)
        {
            var group = StorageGroups.FirstOrDefault(candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal));
            if (group is null)
            {
                return;
            }

            var clearedOverrides = ClearGroupItemOverrides(group);
            if (clearedOverrides == 0)
            {
                return;
            }

            SnackbarWorkflow.ShowTransientMessage(
                TranslateSettings("Cleared %1 item overrides in %2.", clearedOverrides, TranslateSettings(group.DisplayNameSource)),
                Severity.Info);
        }

        protected string GetStorageTypeDisplayText(StorageType storageType)
        {
            return GetStorageTypeOptionText(storageType);
        }

        protected string GetStorageTypeOptionText(StorageType storageType)
        {
            return storageType switch
            {
                StorageType.ClientData => TranslateSettings("qBittorrent client data"),
                _ => TranslateSettings("Browser local storage")
            };
        }

        protected string GetWebApiVersionText()
        {
            if (string.IsNullOrWhiteSpace(WebApiCapabilityState.RawWebApiVersion))
            {
                return TranslateSettings("Unavailable");
            }

            return WebApiCapabilityState.RawWebApiVersion;
        }

        protected string GetClientDataSupportText()
        {
            return SupportsClientData
                ? TranslateSettings("Supported")
                : TranslateSettings("Not supported");
        }

        protected Color GetClientDataSupportColor()
        {
            return SupportsClientData
                ? Color.Success
                : Color.Warning;
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
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateUpdates("Unable to check for updates."), Severity.Warning);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateUpdates("Unable to check for updates."), Severity.Warning);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
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
            await InvokeAsync(StateHasChanged);

            try
            {
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
            }
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            finally
            {
                IsStorageBusy = false;
            }
        }

        protected async Task RemoveStorageEntryAsync(StorageType storageType, string key)
        {
            if (IsStorageBusy)
            {
                return;
            }

            IsStorageBusy = true;

            try
            {
                await StorageDiagnosticsService.RemoveEntryAsync(storageType, key);
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
            }
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
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
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Removed %1 storage entries.", removed), Severity.Info);
                NavigationManager.NavigateToHome(forceLoad: true);
            }
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
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

        private async Task LoadDeferredStateAsync()
        {
            var webApiCapabilityTask = GetWebApiCapabilityStateAsync();
            var notificationPermissionTask = GetNotificationPermissionSafeAsync();
            var updateStatusTask = GetUpdateStatusSafeAsync();

            await Task.WhenAll(webApiCapabilityTask, notificationPermissionTask, updateStatusTask);

            WebApiCapabilityState = await webApiCapabilityTask;
            NotificationPermission = await notificationPermissionTask;
            UpdateStatus = await updateStatusTask;

            await InvokeAsync(StateHasChanged);
        }

        private void EnsureStorageTabActivated()
        {
            if (IsStorageTabActivated)
            {
                if (!_hasLoadedInitialStorageEntries)
                {
                    _isStorageEntriesLoadRequested = true;
                }

                return;
            }

            IsStorageTabActivated = true;
            _isStorageEntriesLoadRequested = true;
        }

        private async Task<WebApiCapabilityState> GetWebApiCapabilityStateAsync()
        {
            try
            {
                return await WebApiCapabilityService.GetCapabilityStateAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
            }
            catch (JsonException)
            {
                return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
            }
            catch (InvalidOperationException)
            {
                return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
            }
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

        private async Task<AppUpdateStatus?> GetUpdateStatusSafeAsync()
        {
            try
            {
                return await AppUpdateService.GetUpdateStatusAsync();
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static bool AreSettingsEquivalent(AppSettingsModel left, AppSettingsModel right)
        {
            return left.UpdateChecksEnabled == right.UpdateChecksEnabled
                && left.NotificationsEnabled == right.NotificationsEnabled
                && left.ThemeModePreference == right.ThemeModePreference
                && left.DownloadFinishedNotificationsEnabled == right.DownloadFinishedNotificationsEnabled
                && left.TorrentAddedNotificationsEnabled == right.TorrentAddedNotificationsEnabled
                && left.TorrentAddedSnackbarsEnabledWithNotifications == right.TorrentAddedSnackbarsEnabledWithNotifications
                && string.Equals(left.DismissedReleaseTag, right.DismissedReleaseTag, StringComparison.Ordinal);
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

        private int ClearGroupItemOverrides(StorageCatalogGroupDefinition group)
        {
            var itemIdsWithOverrides = group.Items
                .Where(item => StorageRoutingSettings.ItemStorageTypes.ContainsKey(item.Id))
                .Select(item => item.Id)
                .ToList();

            foreach (var itemId in itemIdsWithOverrides)
            {
                _ = StorageRoutingSettings.ItemStorageTypes.Remove(itemId);
            }

            return itemIdsWithOverrides.Count;
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
    }
}
