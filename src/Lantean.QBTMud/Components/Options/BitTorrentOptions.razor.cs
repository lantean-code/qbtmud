namespace Lantean.QBTMud.Components.Options
{
    public partial class BitTorrentOptions : Options
    {
        protected bool Dht { get; private set; }
        protected bool Pex { get; private set; }
        protected bool Lsd { get; private set; }
        protected int Encryption { get; private set; }
        protected bool AnonymousMode { get; private set; }
        protected int MaxActiveCheckingTorrents { get; private set; }
        protected bool QueueingEnabled { get; private set; }
        protected int MaxActiveDownloads { get; private set; }
        protected int MaxActiveUploads { get; private set; }
        protected int MaxActiveTorrents { get; private set; }
        protected bool DontCountSlowTorrents { get; private set; }
        protected int SlowTorrentDlRateThreshold { get; private set; }
        protected int SlowTorrentUlRateThreshold { get; private set; }
        protected int SlowTorrentInactiveTimer { get; private set; }
        protected bool MaxRatioEnabled { get; private set; }
        protected float MaxRatio { get; private set; }
        protected bool MaxSeedingTimeEnabled { get; private set; }
        protected int MaxSeedingTime { get; private set; }
        protected int MaxRatioAct { get; private set; }
        protected bool MaxInactiveSeedingTimeEnabled { get; private set; }
        protected int MaxInactiveSeedingTime { get; private set; }
        protected bool AddTrackersEnabled { get; private set; }
        protected string? AddTrackers { get; private set; }

        protected Func<int, string?> MaxActiveDownloadsValidation => MaxActiveDownloadsValidationFunc;

        protected Func<int, string?> MaxActiveUploadsValidation => MaxActiveUploadsValidationFunc;

        protected Func<int, string?> MaxActiveTorrentsValidation => MaxActiveTorrentsValidationFunc;

        protected Func<int, string?> SlowTorrentDlRateThresholdValidation => SlowTorrentDlRateThresholdValidationFunc;

        protected Func<int, string?> SlowTorrentUlRateThresholdValidation => SlowTorrentUlRateThresholdValidationFunc;

        protected Func<int, string?> SlowTorrentInactiveTimerValidation => SlowTorrentInactiveTimerValidationFunc;

        protected Func<int, string?> MaxRatioValidation => MaxRatioValidationFunc;

        protected Func<int, string?> MaxSeedingTimeValidation => MaxSeedingTimeValidationFunc;

        protected Func<int, string?> MaxInactiveSeedingTimeValidation => MaxInactiveSeedingTimeValidationFunc;

        private string? MaxActiveDownloadsValidationFunc(int value)
        {
            if (value < -1)
            {
                return WebUiLocalizer.Translate("HttpServer", "Maximum active downloads must be greater than -1.");
            }

            return null;
        }

        private string? MaxActiveUploadsValidationFunc(int value)
        {
            if (value < -1)
            {
                return WebUiLocalizer.Translate("HttpServer", "Maximum active uploads must be greater than -1.");
            }

            return null;
        }

        private string? MaxActiveTorrentsValidationFunc(int value)
        {
            if (value < -1)
            {
                return WebUiLocalizer.Translate("HttpServer", "Maximum active torrents must be greater than -1.");
            }

            return null;
        }

        private string? SlowTorrentDlRateThresholdValidationFunc(int value)
        {
            if (value < 1)
            {
                return WebUiLocalizer.Translate("HttpServer", "Download rate threshold must be greater than 0.");
            }

            return null;
        }

        private string? SlowTorrentUlRateThresholdValidationFunc(int value)
        {
            if (value < 1)
            {
                return WebUiLocalizer.Translate("HttpServer", "Upload rate threshold must be greater than 0.");
            }

            return null;
        }

        private string? SlowTorrentInactiveTimerValidationFunc(int value)
        {
            if (value < 1)
            {
                return WebUiLocalizer.Translate("HttpServer", "Torrent inactivity timer must be greater than 0.");
            }

            return null;
        }

        private string? MaxRatioValidationFunc(int value)
        {
            if (value < 0 || value > 9998)
            {
                return WebUiLocalizer.Translate("AppBitTorrentOptions", "Share ratio limit must be between 0 and 9998.");
            }

            return null;
        }

        private string? MaxSeedingTimeValidationFunc(int value)
        {
            if (value < 0 || value > 525600)
            {
                return WebUiLocalizer.Translate("AppBitTorrentOptions", "Seeding time limit must be between 0 and 525600 minutes.");
            }

            return null;
        }

        private string? MaxInactiveSeedingTimeValidationFunc(int value)
        {
            if (value < 0 || value > 525600)
            {
                return WebUiLocalizer.Translate("AppBitTorrentOptions", "Seeding time limit must be between 0 and 525600 minutes.");
            }

            return null;
        }

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            Dht = Preferences.Dht;
            Pex = Preferences.Pex;
            Lsd = Preferences.Lsd;
            Encryption = Preferences.Encryption;
            AnonymousMode = Preferences.AnonymousMode;
            MaxActiveCheckingTorrents = Preferences.MaxActiveCheckingTorrents;
            QueueingEnabled = Preferences.QueueingEnabled;
            MaxActiveDownloads = Preferences.MaxActiveDownloads;
            MaxActiveUploads = Preferences.MaxActiveUploads;
            MaxActiveTorrents = Preferences.MaxActiveTorrents;
            DontCountSlowTorrents = Preferences.DontCountSlowTorrents;
            SlowTorrentDlRateThreshold = Preferences.SlowTorrentDlRateThreshold;
            SlowTorrentUlRateThreshold = Preferences.SlowTorrentUlRateThreshold;
            SlowTorrentInactiveTimer = Preferences.SlowTorrentInactiveTimer;
            MaxRatioEnabled = Preferences.MaxRatioEnabled;
            MaxRatio = Preferences.MaxRatio;

            if (Preferences.MaxSeedingTimeEnabled)
            {
                MaxSeedingTimeEnabled = true;
                MaxSeedingTime = Preferences.MaxSeedingTime;
            }
            else
            {
                MaxSeedingTimeEnabled = false;
                MaxSeedingTime = 1440;
            }

            MaxRatioAct = Preferences.MaxRatioAct;

            if (Preferences.MaxInactiveSeedingTimeEnabled)
            {
                MaxInactiveSeedingTimeEnabled = true;
                MaxSeedingTime = Preferences.MaxInactiveSeedingTime;
            }
            else
            {
                MaxInactiveSeedingTimeEnabled = false;
                MaxInactiveSeedingTime = 1440;
            }

            AddTrackersEnabled = Preferences.AddTrackersEnabled;
            AddTrackers = Preferences.AddTrackers;

            return true;
        }

        protected async Task DhtChanged(bool value)
        {
            Dht = value;
            UpdatePreferences.Dht = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PexChanged(bool value)
        {
            Pex = value;
            UpdatePreferences.Pex = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task LsdChanged(bool value)
        {
            Lsd = value;
            UpdatePreferences.Lsd = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EncryptionChanged(int value)
        {
            Encryption = value;
            UpdatePreferences.Encryption = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AnonymousModeChanged(bool value)
        {
            AnonymousMode = value;
            UpdatePreferences.AnonymousMode = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxActiveCheckingTorrentsChanged(int value)
        {
            MaxActiveCheckingTorrents = value;
            UpdatePreferences.MaxActiveCheckingTorrents = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task QueueingEnabledChanged(bool value)
        {
            QueueingEnabled = value;
            UpdatePreferences.QueueingEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxActiveDownloadsChanged(int value)
        {
            MaxActiveDownloads = value;
            UpdatePreferences.MaxActiveDownloads = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxActiveUploadsChanged(int value)
        {
            MaxActiveUploads = value;
            UpdatePreferences.MaxActiveUploads = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxActiveTorrentsChanged(int value)
        {
            MaxActiveTorrents = value;
            UpdatePreferences.MaxActiveTorrents = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DontCountSlowTorrentsChanged(bool value)
        {
            DontCountSlowTorrents = value;
            UpdatePreferences.DontCountSlowTorrents = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SlowTorrentDlRateThresholdChanged(int value)
        {
            SlowTorrentDlRateThreshold = value;
            UpdatePreferences.SlowTorrentDlRateThreshold = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SlowTorrentUlRateThresholdChanged(int value)
        {
            SlowTorrentUlRateThreshold = value;
            UpdatePreferences.SlowTorrentUlRateThreshold = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SlowTorrentInactiveTimerChanged(int value)
        {
            SlowTorrentInactiveTimer = value;
            UpdatePreferences.SlowTorrentInactiveTimer = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxRatioEnabledChanged(bool value)
        {
            MaxRatioEnabled = value;
            UpdatePreferences.MaxRatioEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxRatioChanged(float value)
        {
            MaxRatio = value;
            UpdatePreferences.MaxRatio = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxSeedingTimeEnabledChanged(bool value)
        {
            MaxSeedingTimeEnabled = value;
            UpdatePreferences.MaxSeedingTimeEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxSeedingTimeChanged(int value)
        {
            MaxSeedingTime = value;
            UpdatePreferences.MaxSeedingTime = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxRatioActChanged(int value)
        {
            MaxRatioAct = value;
            UpdatePreferences.MaxRatioAct = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxInactiveSeedingTimeEnabledChanged(bool value)
        {
            MaxInactiveSeedingTimeEnabled = value;
            UpdatePreferences.MaxInactiveSeedingTimeEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxInactiveSeedingTimeChanged(int value)
        {
            MaxInactiveSeedingTime = value;
            UpdatePreferences.MaxInactiveSeedingTime = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AddTrackersEnabledChanged(bool value)
        {
            AddTrackersEnabled = value;
            UpdatePreferences.AddTrackersEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AddTrackersChanged(string value)
        {
            AddTrackers = value;
            UpdatePreferences.AddTrackers = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }
    }
}
