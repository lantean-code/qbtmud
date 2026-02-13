using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Components.Options
{
    public partial class BehaviourOptions : Options
    {
        protected string? Locale { get; set; }

        protected bool? ConfirmTorrentDeletion { get; set; }

        protected bool? StatusBarExternalIp { get; set; }

        protected bool? FileLogEnabled { get; set; }

        protected string? FileLogPath { get; set; }

        protected bool? FileLogBackupEnabled { get; set; }

        protected int FileLogMaxSize { get; set; }

        protected bool? FileLogDeleteOld { get; set; }

        protected int FileLogAge { get; set; }

        protected int FileLogAgeType { get; set; }

        protected bool? PerformanceWarning { get; set; }

        protected IReadOnlyList<WebUiLanguageCatalogItem> LanguageOptions { get; private set; } = Array.Empty<WebUiLanguageCatalogItem>();

        protected override async Task OnInitializedAsync()
        {
            await WebUiLanguageCatalog.EnsureInitialized();
            LanguageOptions = WebUiLanguageCatalog.Languages;
        }

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            Locale = string.IsNullOrWhiteSpace(Preferences.Locale) ? "en" : Preferences.Locale;
            ConfirmTorrentDeletion = Preferences.ConfirmTorrentDeletion;
            StatusBarExternalIp = Preferences.StatusBarExternalIp;
            FileLogEnabled = Preferences.FileLogEnabled;
            FileLogPath = Preferences.FileLogPath;
            FileLogBackupEnabled = Preferences.FileLogBackupEnabled;
            FileLogMaxSize = Preferences.FileLogMaxSize;
            FileLogDeleteOld = Preferences.FileLogDeleteOld;
            FileLogAge = Preferences.FileLogAge;
            FileLogAgeType = Preferences.FileLogAgeType;
            PerformanceWarning = Preferences.PerformanceWarning;

            return true;
        }

        protected async Task LocaleChanged(string value)
        {
            Locale = value;
            UpdatePreferences.Locale = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ConfirmTorrentDeletionChanged(bool value)
        {
            ConfirmTorrentDeletion = value;
            UpdatePreferences.ConfirmTorrentDeletion = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task StatusBarExternalIpChanged(bool value)
        {
            StatusBarExternalIp = value;
            UpdatePreferences.StatusBarExternalIp = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task FileLogEnabledChanged(bool value)
        {
            FileLogEnabled = value;
            UpdatePreferences.FileLogEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task FileLogPathChanged(string value)
        {
            FileLogPath = value;
            UpdatePreferences.FileLogPath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task FileLogBackupEnabledChanged(bool value)
        {
            FileLogBackupEnabled = value;
            UpdatePreferences.FileLogBackupEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task FileLogMaxSizeChanged(int value)
        {
            FileLogMaxSize = value;
            UpdatePreferences.FileLogMaxSize = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task FileLogDeleteOldChanged(bool value)
        {
            FileLogDeleteOld = value;
            UpdatePreferences.FileLogDeleteOld = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task FileLogAgeChanged(int value)
        {
            FileLogAge = value;
            UpdatePreferences.FileLogAge = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task FileLogAgeTypeChanged(int value)
        {
            FileLogAgeType = value;
            UpdatePreferences.FileLogAgeType = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PerformanceWarningChanged(bool value)
        {
            PerformanceWarning = value;
            UpdatePreferences.PerformanceWarning = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }
    }
}
