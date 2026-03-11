using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Diagnostics;
using System.Text.Json;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class UpdatesAppSettingsTab
    {
        [Parameter]
        [EditorRequired]
        public AppSettingsModel Settings { get; set; } = default!;

        [Parameter]
        public EventCallback SettingsChanged { get; set; }

        [Parameter]
        public int ReloadToken { get; set; }

        [Inject]
        protected IAppBuildInfoService AppBuildInfoService { get; set; } = default!;

        [Inject]
        protected IAppUpdateService AppUpdateService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        protected bool IsCheckingUpdates { get; private set; }

        protected AppBuildInfo CurrentBuildInfo { get; private set; } = new("unknown", "Unavailable");

        protected AppUpdateStatus? UpdateStatus { get; private set; }

        private int _loadedReloadToken = -1;
        private bool _isInitialized;

        protected override async Task OnParametersSetAsync()
        {
            if (!_isInitialized)
            {
                CurrentBuildInfo = AppBuildInfoService.GetCurrentBuildInfo();
                _isInitialized = true;
            }

            if (_loadedReloadToken == ReloadToken)
            {
                return;
            }

            _loadedReloadToken = ReloadToken;
            UpdateStatus = await GetUpdateStatusSafeAsync();
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

        private async Task OnUpdateChecksChanged(bool value)
        {
            if (Settings.UpdateChecksEnabled == value)
            {
                return;
            }

            Settings.UpdateChecksEnabled = value;
            await SettingsChanged.InvokeAsync();
        }

        private string GetLatestReleaseTag()
        {
            return UpdateStatus?.LatestRelease?.TagName ?? TranslateUpdates("Not available");
        }

        private string GetUpdateStatusText()
        {
            if (UpdateStatus is null || UpdateStatus.LatestRelease is null || !UpdateStatus.CanCompareVersions)
            {
                return TranslateUpdates("Not available");
            }

            return UpdateStatus.IsUpdateAvailable
                ? TranslateUpdates("Update available")
                : TranslateUpdates("Up to date");
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

        private string TranslateUpdates(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppUpdates", source, arguments);
        }
    }
}
