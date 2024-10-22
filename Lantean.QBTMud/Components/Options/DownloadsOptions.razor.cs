using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Components.Options
{
    public partial class DownloadsOptions : Options
    {
        protected string? TorrentContentLayout { get; set; }
        protected bool AddToTopOfQueue { get; set; }
        protected bool StartPausedEnabled { get; set; }
        protected string? TorrentStopCondition { get; set; }
        protected bool AutoDeleteMode { get; set; }
        protected bool PreallocateAll { get; set; }
        protected bool IncompleteFilesExt { get; set; }
        protected bool AutoTmmEnabled { get; set; }
        protected bool TorrentChangedTmmEnabled { get; set; }
        protected bool SavePathChangedTmmEnabled { get; set; }
        protected bool CategoryChangedTmmEnabled { get; set; }
        protected bool UseSubcategories { get; set; }
        protected string? SavePath { get; set; }
        protected bool TempPathEnabled { get; set; }
        protected string? TempPath { get; set; }
        protected bool ExportDirEnabled { get; set; }
        protected string? ExportDir { get; set; }
        protected bool ExportDirFinEnabled { get; set; }
        protected string? ExportDirFin { get; set; }
        protected Dictionary<string, SaveLocation> ScanDirs { get; set; } = [];
        protected bool ExcludedFileNamesEnabled { get; set; }
        protected string? ExcludedFileNames { get; set; }
        protected bool MailNotificationEnabled { get; set; }
        protected string? MailNotificationSender { get; set; }
        protected string? MailNotificationEmail { get; set; }
        protected string? MailNotificationSmtp { get; set; }
        protected bool MailNotificationSslEnabled { get; set; }
        protected bool MailNotificationAuthEnabled { get; set; }
        protected string? MailNotificationUsername { get; set; }
        protected string? MailNotificationPassword { get; set; }
        protected bool AutorunOnTorrentAddedEnabled { get; set; }
        protected string? AutorunOnTorrentAddedProgram { get; set; }
        protected bool AutorunEnabled { get; set; }
        protected string? AutorunProgram { get; set; }

        protected List<KeyValuePair<string, SaveLocation>> AddedScanDirs { get; set; } = [];

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            // when adding a torrent
            TorrentContentLayout = Preferences.TorrentContentLayout;
            AddToTopOfQueue = Preferences.AddToTopOfQueue;
            StartPausedEnabled = Preferences.StartPausedEnabled;
            TorrentStopCondition = Preferences.TorrentStopCondition;
            AutoDeleteMode = Preferences.AutoDeleteMode == 1;
            PreallocateAll = Preferences.PreallocateAll;
            IncompleteFilesExt = Preferences.IncompleteFilesExt;

            // saving management
            AutoTmmEnabled = Preferences.AutoTmmEnabled;
            TorrentChangedTmmEnabled = Preferences.TorrentChangedTmmEnabled;
            SavePathChangedTmmEnabled = Preferences.SavePathChangedTmmEnabled;
            CategoryChangedTmmEnabled = Preferences.CategoryChangedTmmEnabled;
            UseSubcategories = Preferences.UseSubcategories;
            SavePath = Preferences.SavePath;
            TempPathEnabled = Preferences.TempPathEnabled;
            TempPath = Preferences.TempPath;
            ExportDir = Preferences.ExportDir;
            ExportDirEnabled = !string.IsNullOrEmpty(Preferences.ExportDir);
            ExportDirFin = Preferences.ExportDirFin;
            ExportDirFinEnabled = !string.IsNullOrEmpty(Preferences.ExportDirFin);

            ScanDirs.Clear();
            foreach (var dir in Preferences.ScanDirs)
            {
                ScanDirs.Add(dir.Key, dir.Value);
            }

            ExcludedFileNamesEnabled = Preferences.ExcludedFileNamesEnabled;
            ExcludedFileNames = Preferences.ExcludedFileNames;

            // email notification
            MailNotificationEnabled = Preferences.MailNotificationEnabled;
            MailNotificationSender = Preferences.MailNotificationSender;
            MailNotificationEmail = Preferences.MailNotificationEmail;
            MailNotificationSmtp = Preferences.MailNotificationSmtp;
            MailNotificationSslEnabled = Preferences.MailNotificationSslEnabled;
            MailNotificationAuthEnabled = Preferences.MailNotificationAuthEnabled;
            MailNotificationUsername = Preferences.MailNotificationUsername;
            MailNotificationPassword = Preferences.MailNotificationPassword;

            // autorun
            AutorunOnTorrentAddedEnabled = Preferences.AutorunOnTorrentAddedEnabled;
            AutorunOnTorrentAddedProgram = Preferences.AutorunOnTorrentAddedProgram;
            AutorunEnabled = Preferences.AutorunEnabled;
            AutorunProgram = Preferences.AutorunProgram;

            AddedScanDirs.Clear();
            AddDefaultScanDir();

            return true;
        }

        protected async Task TorrentContentLayoutChanged(string value)
        {
            TorrentContentLayout = value;
            UpdatePreferences.TorrentContentLayout = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AddToTopOfQueueChanged(bool value)
        {
            AddToTopOfQueue = value;
            UpdatePreferences.AddToTopOfQueue = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task StartPausedEnabledChanged(bool value)
        {
            StartPausedEnabled = value;
            UpdatePreferences.StartPausedEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task TorrentStopConditionChanged(string value)
        {
            TorrentStopCondition = value;
            UpdatePreferences.TorrentStopCondition = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AutoDeleteModeChanged(bool value)
        {
            AutoDeleteMode = value;
            UpdatePreferences.AutoDeleteMode = value ? 1 : 0;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PreallocateAllChanged(bool value)
        {
            PreallocateAll = value;
            UpdatePreferences.PreallocateAll = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task IncompleteFilesExtChanged(bool value)
        {
            IncompleteFilesExt = value;
            UpdatePreferences.IncompleteFilesExt = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AutoTmmEnabledChanged(bool value)
        {
            AutoTmmEnabled = value;
            UpdatePreferences.AutoTmmEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task TorrentChangedTmmEnabledChanged(bool value)
        {
            TorrentChangedTmmEnabled = value;
            UpdatePreferences.TorrentChangedTmmEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SavePathChangedTmmEnabledChanged(bool value)
        {
            SavePathChangedTmmEnabled = value;
            UpdatePreferences.SavePathChangedTmmEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task CategoryChangedTmmEnabledChanged(bool value)
        {
            CategoryChangedTmmEnabled = value;
            UpdatePreferences.CategoryChangedTmmEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task UseSubcategoriesChanged(bool value)
        {
            UseSubcategories = value;
            UpdatePreferences.UseSubcategories = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SavePathChanged(string value)
        {
            SavePath = value;
            UpdatePreferences.SavePath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task TempPathEnabledChanged(bool value)
        {
            TempPathEnabled = value;
            UpdatePreferences.TempPathEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task TempPathChanged(string value)
        {
            TempPath = value;
            UpdatePreferences.TempPath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected void ExportDirEnabledChanged(bool value)
        {
            ExportDirEnabled = value;
        }

        protected async Task ExportDirChanged(string value)
        {
            ExportDir = value;
            UpdatePreferences.ExportDir = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected void ExportDirFinEnabledChanged(bool value)
        {
            ExportDirFinEnabled = value;
        }

        protected async Task ExportDirFinChanged(string value)
        {
            ExportDirFin = value;
            UpdatePreferences.ExportDirFin = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ScanDirsKeyChanged(string key, string value)
        {
            if (ScanDirs.Remove(key, out var location))
            {
                ScanDirs[value] = location;
            }
            UpdatePreferences.ScanDirs = ScanDirs;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ScanDirsValueChanged(string key, string value)
        {
            ScanDirs[key] = SaveLocation.Create(value);
            UpdatePreferences.ScanDirs = ScanDirs;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AddedScanDirsKeyChanged(int index, string key)
        {
            if (key == "")
            {
                return;
            }

            ScanDirs.Add(key, AddedScanDirs[index].Value);
            AddedScanDirs.RemoveAt(index);
            UpdatePreferences.ScanDirs = ScanDirs;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);

#pragma warning disable S2583 // Conditionally executed code should be reachable
            if (AddedScanDirs.Count == 0)
            {
                AddDefaultScanDir();
            }
#pragma warning restore S2583 // Conditionally executed code should be reachable
        }

        protected void AddedScanDirsValueChanged(int index, string value)
        {
            var existing = AddedScanDirs[index];
            AddedScanDirs[index] = new KeyValuePair<string, SaveLocation>(existing.Key, SaveLocation.Create(value));
        }

        protected void AddNewScanDir()
        {
            AddDefaultScanDir();
        }

        protected void RemoveAddedScanDir(int index)
        {
            AddedScanDirs.RemoveAt(index);
        }

        protected async Task RemoveExistingScanDir(string key)
        {
            ScanDirs.Remove(key);
            UpdatePreferences.ScanDirs = ScanDirs;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ExcludedFileNamesEnabledChanged(bool value)
        {
            ExcludedFileNamesEnabled = value;
            UpdatePreferences.ExcludedFileNamesEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ExcludedFileNamesChanged(string value)
        {
            ExcludedFileNames = value;
            UpdatePreferences.ExcludedFileNames = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationEnabledChanged(bool value)
        {
            MailNotificationEnabled = value;
            UpdatePreferences.MailNotificationEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationSenderChanged(string value)
        {
            MailNotificationSender = value;
            UpdatePreferences.MailNotificationSender = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationEmailChanged(string value)
        {
            MailNotificationEmail = value;
            UpdatePreferences.MailNotificationEmail = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationSmtpChanged(string value)
        {
            MailNotificationSmtp = value;
            UpdatePreferences.MailNotificationSmtp = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationSslEnabledChanged(bool value)
        {
            MailNotificationSslEnabled = value;
            UpdatePreferences.MailNotificationSslEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationAuthEnabledChanged(bool value)
        {
            MailNotificationAuthEnabled = value;
            UpdatePreferences.MailNotificationAuthEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationUsernameChanged(string value)
        {
            MailNotificationUsername = value;
            UpdatePreferences.MailNotificationUsername = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MailNotificationPasswordChanged(string value)
        {
            MailNotificationPassword = value;
            UpdatePreferences.MailNotificationPassword = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AutorunOnTorrentAddedEnabledChanged(bool value)
        {
            AutorunOnTorrentAddedEnabled = value;
            UpdatePreferences.AutorunOnTorrentAddedEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AutorunOnTorrentAddedProgramChanged(string value)
        {
            AutorunOnTorrentAddedProgram = value;
            UpdatePreferences.AutorunOnTorrentAddedProgram = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AutorunEnabledChanged(bool value)
        {
            AutorunEnabled = value;
            UpdatePreferences.AutorunEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AutorunProgramChanged(string value)
        {
            AutorunProgram = value;
            UpdatePreferences.AutorunProgram = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        private void AddDefaultScanDir()
        {
            AddedScanDirs.Add(new KeyValuePair<string, SaveLocation>("", SaveLocation.Create(1)));
        }

        protected Func<string, string?> IsValidNewKey => IsValidNewKeyFunc;

        private string? IsValidNewKeyFunc(string? key)
        {
            if (key is null)
            {
                return null;
            }
            if (ScanDirs.ContainsKey(key))
            {
                return "A folder with this path already exists";
            }

            return null;
        }
    }
}