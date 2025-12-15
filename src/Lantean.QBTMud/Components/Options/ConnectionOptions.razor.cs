namespace Lantean.QBTMud.Components.Options
{
    public partial class ConnectionOptions : Options
    {
        protected int BittorrentProtocol { get; private set; }
        protected int ListenPort { get; private set; }
        protected bool Upnp { get; private set; }
        protected bool MaxConnecEnabled { get; private set; }
        protected int MaxConnec { get; private set; }
        protected bool MaxConnecPerTorrentEnabled { get; private set; }
        protected int MaxConnecPerTorrent { get; private set; }
        protected bool MaxUploadsEnabled { get; private set; }
        protected int MaxUploads { get; private set; }
        protected bool MaxUploadsPerTorrentEnabled { get; private set; }
        protected int MaxUploadsPerTorrent { get; private set; }
        protected bool I2pEnabled { get; private set; }
        protected string? I2pAddress { get; private set; }
        protected int I2pPort { get; private set; }
        protected bool I2pMixedMode { get; private set; }
        protected bool ProxyDisabled { get; private set; }
        protected bool ProxySocks4 { get; private set; }
        protected string? ProxyType { get; private set; }
        protected string? ProxyIp { get; private set; }
        protected int ProxyPort { get; private set; }
        protected bool ProxyAuthEnabled { get; private set; }
        protected string? ProxyUsername { get; private set; }
        protected string? ProxyPassword { get; private set; }
        protected bool ProxyHostnameLookup { get; private set; }
        protected bool ProxyBittorrent { get; private set; }
        protected bool ProxyPeerConnections { get; private set; }
        protected bool ProxyRss { get; private set; }
        protected bool ProxyMisc { get; private set; }
        protected bool IpFilterEnabled { get; private set; }
        protected string? IpFilterPath { get; private set; }
        protected bool IpFilterTrackers { get; private set; }
        protected string? BannedIPs { get; private set; }

        protected Func<int, string?> MaxConnectValidation = value =>
        {
            if (value < 0)
            {
                return "Maximum number of connections limit must be greater than 0 or disabled.";
            }

            return null;
        };

        protected Func<int, string?> MaxConnecPerTorrentValidation = value =>
        {
            if (value < 0)
            {
                return "Maximum number of connections per torrent limit must be greater than 0 or disabled.";
            }

            return null;
        };

        protected Func<int, string?> MaxUploadsValidation = value =>
        {
            if (value < 0)
            {
                return "Global number of upload slots limit must be greater than 0 or disabled.";
            }

            return null;
        };

        protected Func<int, string?> MaxUploadsPerTorrentValidation = value =>
        {
            if (value < 0)
            {
                return "Maximum number of upload slots per torrent limit must be greater than 0 or disabled.";
            }

            return null;
        };

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            BittorrentProtocol = Preferences.BittorrentProtocol;
            ListenPort = Preferences.ListenPort;
            Upnp = Preferences.Upnp;
            if (Preferences.MaxConnec > 0)
            {
                MaxConnecEnabled = true;
                MaxConnec = Preferences.MaxConnec;
            }
            else
            {
                MaxConnecEnabled = false;
                MaxConnec = 500;
            }

            if (Preferences.MaxConnecPerTorrent > 0)
            {
                MaxConnecPerTorrentEnabled = true;
                MaxConnecPerTorrent = Preferences.MaxConnecPerTorrent;
            }
            else
            {
                MaxConnecPerTorrentEnabled = false;
                MaxConnecPerTorrent = 100;
            }

            if (Preferences.MaxUploads > 0)
            {
                MaxUploadsEnabled = true;
                MaxUploads = Preferences.MaxUploads;
            }
            else
            {
                MaxUploadsEnabled = false;
                MaxUploads = 20;
            }

            if (Preferences.MaxUploadsPerTorrent > 0)
            {
                MaxUploadsPerTorrentEnabled = true;
                MaxUploadsPerTorrent = Preferences.MaxUploadsPerTorrent;
            }
            else
            {
                MaxUploadsPerTorrentEnabled = false;
                MaxUploadsPerTorrent = 4;
            }

            I2pEnabled = Preferences.I2pEnabled;
            I2pAddress = Preferences.I2pAddress;
            I2pPort = Preferences.I2pPort;
            I2pMixedMode = Preferences.I2pMixedMode;

            ProxyType = Preferences.ProxyType;
            ProxyIp = Preferences.ProxyIp;
            ProxyPort = Preferences.ProxyPort;
            ProxyAuthEnabled = Preferences.ProxyAuthEnabled;
            ProxyUsername = Preferences.ProxyUsername;
            ProxyPassword = Preferences.ProxyPassword;
            ProxyHostnameLookup = Preferences.ProxyHostnameLookup;
            ProxyBittorrent = Preferences.ProxyBittorrent;
            ProxyPeerConnections = Preferences.ProxyPeerConnections;
            ProxyRss = Preferences.ProxyRss;
            ProxyMisc = Preferences.ProxyMisc;

            IpFilterEnabled = Preferences.IpFilterEnabled;
            IpFilterPath = Preferences.IpFilterPath;
            IpFilterTrackers = Preferences.IpFilterTrackers;
            BannedIPs = Preferences.BannedIPs;

            return true;
        }

        protected async Task BittorrentProtocolChanged(int value)
        {
            BittorrentProtocol = value;
            UpdatePreferences.BittorrentProtocol = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ListenPortChanged(int value)
        {
            ListenPort = value;
            UpdatePreferences.ListenPort = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task UpnpChanged(bool value)
        {
            Upnp = value;
            UpdatePreferences.Upnp = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected void MaxConnecEnabledChanged(bool value)
        {
            MaxConnecEnabled = value;
        }

        protected async Task MaxConnecChanged(int value)
        {
            MaxConnec = value;
            UpdatePreferences.MaxConnec = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected void MaxConnecPerTorrentEnabledChanged(bool value)
        {
            MaxConnecPerTorrentEnabled = value;
        }

        protected async Task MaxConnecPerTorrentChanged(int value)
        {
            MaxConnecPerTorrent = value;
            UpdatePreferences.MaxConnecPerTorrent = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected void MaxUploadsEnabledChanged(bool value)
        {
            MaxUploadsEnabled = value;
        }

        protected async Task MaxUploadsChanged(int value)
        {
            MaxUploads = value;
            UpdatePreferences.MaxUploads = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected void MaxUploadsPerTorrentEnabledChanged(bool value)
        {
            MaxUploadsPerTorrentEnabled = value;
        }

        protected async Task MaxUploadsPerTorrentChanged(int value)
        {
            MaxUploadsPerTorrent = value;
            UpdatePreferences.MaxUploadsPerTorrent = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pEnabledChanged(bool value)
        {
            I2pEnabled = value;
            UpdatePreferences.I2pEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pAddressChanged(string value)
        {
            I2pAddress = value;
            UpdatePreferences.I2pAddress = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pPortChanged(int value)
        {
            I2pPort = value;
            UpdatePreferences.I2pPort = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pMixedModeChanged(bool value)
        {
            I2pMixedMode = value;
            UpdatePreferences.I2pMixedMode = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyTypeChanged(string value)
        {
            ProxyType = value;
            UpdatePreferences.ProxyType = value;
            ProxyDisabled = value == "None";
            ProxySocks4 = value == "SOCKS4";
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyIpChanged(string value)
        {
            ProxyIp = value;
            UpdatePreferences.ProxyIp = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyPortChanged(int value)
        {
            ProxyPort = value;
            UpdatePreferences.ProxyPort = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyAuthEnabledChanged(bool value)
        {
            ProxyAuthEnabled = value;
            UpdatePreferences.ProxyAuthEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyUsernameChanged(string value)
        {
            ProxyUsername = value;
            UpdatePreferences.ProxyUsername = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyPasswordChanged(string value)
        {
            ProxyPassword = value;
            UpdatePreferences.ProxyPassword = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyHostnameLookupChanged(bool value)
        {
            ProxyHostnameLookup = value;
            UpdatePreferences.ProxyHostnameLookup = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyBittorrentChanged(bool value)
        {
            ProxyBittorrent = value;
            UpdatePreferences.ProxyBittorrent = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyPeerConnectionsChanged(bool value)
        {
            ProxyPeerConnections = value;
            UpdatePreferences.ProxyPeerConnections = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyRssChanged(bool value)
        {
            ProxyRss = value;
            UpdatePreferences.ProxyRss = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ProxyMiscChanged(bool value)
        {
            ProxyMisc = value;
            UpdatePreferences.ProxyMisc = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task IpFilterEnabledChanged(bool value)
        {
            IpFilterEnabled = value;
            UpdatePreferences.IpFilterEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task IpFilterPathChanged(string value)
        {
            IpFilterPath = value;
            UpdatePreferences.IpFilterPath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task IpFilterTrackersChanged(bool value)
        {
            IpFilterTrackers = value;
            UpdatePreferences.IpFilterTrackers = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BannedIPsChanged(string value)
        {
            BannedIPs = value;
            UpdatePreferences.BannedIPs = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task GenerateRandomPort()
        {
            var random = new Random();
            var port = random.Next(MinPortValue, MaxPortValue);

            await ListenPortChanged(port);
        }
    }
}