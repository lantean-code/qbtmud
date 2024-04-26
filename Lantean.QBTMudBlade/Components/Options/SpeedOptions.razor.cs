namespace Lantean.QBTMudBlade.Components.Options
{
    public partial class SpeedOptions : Options
    {
        protected int UpLimit { get; private set; }
        protected int DlLimit { get; private set; }
        protected int AltUpLimit { get; private set; }
        protected int AltDlLimit { get; private set; }
        protected int BittorrentProtocol { get; private set; }
        protected bool LimitUtpRate { get; private set; }
        protected bool LimitTcpOverhead { get; private set; }
        protected bool LimitLanPeers { get; private set; }
        protected bool SchedulerEnabled { get; private set; }
        protected TimeSpan ScheduleFrom { get; private set; }
        protected TimeSpan ScheduleTo { get; private set; }
        protected int SchedulerDays { get; private set; }

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            UpLimit = Preferences.UpLimit;
            DlLimit = Preferences.DlLimit;
            AltUpLimit = Preferences.AltUpLimit;
            AltDlLimit = Preferences.AltDlLimit;
            BittorrentProtocol = Preferences.BittorrentProtocol;
            LimitUtpRate = Preferences.LimitUtpRate;
            LimitTcpOverhead = Preferences.LimitTcpOverhead;
            LimitLanPeers = Preferences.LimitLanPeers;
            SchedulerEnabled = Preferences.SchedulerEnabled;
            ScheduleFrom = TimeSpan.FromMinutes((Preferences.ScheduleFromHour * 60) + Preferences.ScheduleFromMin);
            ScheduleTo = TimeSpan.FromMinutes((Preferences.ScheduleToHour * 60) + Preferences.ScheduleToMin);
            SchedulerDays = Preferences.SchedulerDays;

            return true;
        }

        protected async Task UpLimitChanged(int value)
        {
            UpLimit = value;
            UpdatePreferences.UpLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DlLimitChanged(int value)
        {
            DlLimit = value;
            UpdatePreferences.DlLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AltUpLimitChanged(int value)
        {
            AltUpLimit = value;
            UpdatePreferences.AltUpLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AltDlLimitChanged(int value)
        {
            AltDlLimit = value;
            UpdatePreferences.AltDlLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BittorrentProtocolChanged(int value)
        {
            BittorrentProtocol = value;
            UpdatePreferences.BittorrentProtocol = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task LimitUtpRateChanged(bool value)
        {
            LimitUtpRate = value;
            UpdatePreferences.LimitUtpRate = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task LimitTcpOverheadChanged(bool value)
        {
            LimitTcpOverhead = value;
            UpdatePreferences.LimitTcpOverhead = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task LimitLanPeersChanged(bool value)
        {
            LimitLanPeers = value;
            UpdatePreferences.LimitLanPeers = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SchedulerEnabledChanged(bool value)
        {
            SchedulerEnabled = value;
            UpdatePreferences.SchedulerEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ScheduleFromChanged(TimeSpan? value)
        {
            if (value is null)
            {
                return;
            }
            
            ScheduleFrom = value.Value;
            bool hasChanged = false;
            if (value.Value.Hours != Preferences?.ScheduleFromHour)
            {
                UpdatePreferences.ScheduleFromHour = value.Value.Hours;
                hasChanged = true;
            }
            if (value.Value.Minutes != Preferences?.ScheduleFromMin)
            {
                UpdatePreferences.ScheduleFromMin = value.Value.Minutes;
                hasChanged = true;
            }
            if (hasChanged)
            {
                await PreferencesChanged.InvokeAsync(UpdatePreferences);
            }
        }

        protected async Task ScheduleToChanged(TimeSpan? value)
        {
            if (value is null)
            {
                return;
            }

            ScheduleTo = value.Value;
            bool hasChanged = false;
            if (value.Value.Hours != Preferences?.ScheduleToHour)
            {
                UpdatePreferences.ScheduleToHour = value.Value.Hours;
                hasChanged = true;
            }
            if (value.Value.Minutes != Preferences?.ScheduleToMin)
            {
                UpdatePreferences.ScheduleToMin = value.Value.Minutes;
                hasChanged = true;
            }
            if (hasChanged)
            {
                await PreferencesChanged.InvokeAsync(UpdatePreferences);
            }
        }

        protected async Task SchedulerDaysChanged(int value)
        {
            SchedulerDays = value;
            UpdatePreferences.SchedulerDays = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }
    }
}