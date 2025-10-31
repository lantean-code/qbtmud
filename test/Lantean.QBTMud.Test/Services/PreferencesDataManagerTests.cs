using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public class PreferencesDataManagerTests
    {
        private readonly PreferencesDataManager _target;

        public PreferencesDataManagerTests()
        {
            _target = new PreferencesDataManager();
        }

        // ---------- Builders ----------

        private static UpdatePreferences BuildAllSetA()
        {
            return new UpdatePreferences
            {
                AddToTopOfQueue = true,
                AddStoppedEnabled = true,
                AddTrackers = "a, b, c",
                AddTrackersEnabled = true,
                AltDlLimit = 1,
                AltUpLimit = 2,
                AlternativeWebuiEnabled = true,
                AlternativeWebuiPath = "/alt-a",
                AnnounceIp = "1.2.3.4",
                AnnouncePort = 6881,
                AnnounceToAllTiers = true,
                AnnounceToAllTrackers = true,
                AnonymousMode = true,
                AppInstanceName = "app-a",
                AsyncIoThreads = 3,
                AutoDeleteMode = 4,
                AutoTmmEnabled = true,
                AutorunEnabled = true,
                AutorunOnTorrentAddedEnabled = true,
                AutorunOnTorrentAddedProgram = "prog-added-a",
                AutorunProgram = "prog-a",
                BannedIPs = "10.0.0.1;10.0.0.2",
                BdecodeDepthLimit = 5,
                BdecodeTokenLimit = 6,
                BittorrentProtocol = 7,
                BlockPeersOnPrivilegedPorts = true,
                BypassAuthSubnetWhitelist = "192.168.0.0/24",
                BypassAuthSubnetWhitelistEnabled = true,
                BypassLocalAuth = true,
                CategoryChangedTmmEnabled = true,
                CheckingMemoryUse = 8,
                ConnectionSpeed = 9,
                CurrentInterfaceAddress = "eth0-addr-a",
                CurrentInterfaceName = "eth0-name-a",
                CurrentNetworkInterface = "eth0-a",
                Dht = true,
                DhtBootstrapNodes = "node-a;node-b",
                DiskCache = 10,
                DiskCacheTtl = 11,
                DiskIoReadMode = 12,
                DiskIoType = 13,
                DiskIoWriteMode = 14,
                DiskQueueSize = 15,
                DlLimit = 16,
                DontCountSlowTorrents = true,
                DyndnsDomain = "dyndns-a",
                DyndnsEnabled = true,
                DyndnsPassword = "dyndns-pass-a",
                DyndnsService = 17,
                DyndnsUsername = "dyndns-user-a",
                EmbeddedTrackerPort = 18,
                EmbeddedTrackerPortForwarding = true,
                EnableCoalesceReadWrite = true,
                EnableEmbeddedTracker = true,
                EnableMultiConnectionsFromSameIp = true,
                EnablePieceExtentAffinity = true,
                EnableUploadSuggestions = true,
                Encryption = 19,
                ExcludedFileNames = "*.tmp;*.bak",
                ExcludedFileNamesEnabled = true,
                ExportDir = "/export-a",
                ExportDirFin = "/export-fin-a",
                FileLogAge = 20,
                FileLogAgeType = 21,
                FileLogBackupEnabled = true,
                FileLogDeleteOld = true,
                FileLogEnabled = true,
                FileLogMaxSize = 22,
                FileLogPath = "/log-a",
                FilePoolSize = 23,
                HashingThreads = 24,
                I2pAddress = "i2p-addr-a",
                I2pEnabled = true,
                I2pInboundLength = 25,
                I2pInboundQuantity = 26,
                I2pMixedMode = true,
                I2pOutboundLength = 27,
                I2pOutboundQuantity = 28,
                I2pPort = 29,
                IdnSupportEnabled = true,
                IncompleteFilesExt = true,
                UseUnwantedFolder = true,
                IpFilterEnabled = true,
                IpFilterPath = "/ipfilter-a",
                IpFilterTrackers = true,
                LimitLanPeers = true,
                LimitTcpOverhead = true,
                LimitUtpRate = true,
                ListenPort = 30,
                SslEnabled = true,
                SslListenPort = 4430,
                Locale = "en-A",
                Lsd = true,
                MailNotificationAuthEnabled = true,
                MailNotificationEmail = "mail@a",
                MailNotificationEnabled = true,
                MailNotificationPassword = "mail-pass-a",
                MailNotificationSender = "sender-a",
                MailNotificationSmtp = "smtp-a",
                MailNotificationSslEnabled = true,
                MailNotificationUsername = "mail-user-a",
                MarkOfTheWeb = true,
                MaxActiveCheckingTorrents = 31,
                MaxActiveDownloads = 32,
                MaxActiveTorrents = 33,
                MaxActiveUploads = 34,
                MaxConcurrentHttpAnnounces = 35,
                MaxConnec = 36,
                MaxConnecPerTorrent = 37,
                MaxInactiveSeedingTime = 38,
                MaxInactiveSeedingTimeEnabled = true,
                MaxRatio = 1.1f,
                MaxRatioAct = 39,
                MaxRatioEnabled = null, // kept null to avoid Validate() conflict in tests that don't call Validate
                MaxSeedingTime = 40,
                MaxSeedingTimeEnabled = null, // same as above
                MaxUploads = 41,
                MaxUploadsPerTorrent = 42,
                MemoryWorkingSetLimit = 43,
                MergeTrackers = true,
                OutgoingPortsMax = 44,
                OutgoingPortsMin = 45,
                PeerTos = 46,
                PeerTurnover = 47,
                PeerTurnoverCutoff = 48,
                PeerTurnoverInterval = 49,
                PerformanceWarning = true,
                Pex = true,
                PreallocateAll = true,
                ProxyAuthEnabled = true,
                ProxyBittorrent = true,
                ProxyHostnameLookup = true,
                ProxyIp = "proxy-ip-a",
                ProxyMisc = true,
                ProxyPassword = "proxy-pass-a",
                ProxyPeerConnections = true,
                ProxyPort = 8080,
                ProxyRss = true,
                ProxyType = "http",
                ProxyUsername = "proxy-user-a",
                PythonExecutablePath = "/python-a",
                QueueingEnabled = true,
                RandomPort = true,
                ReannounceWhenAddressChanged = true,
                RecheckCompletedTorrents = true,
                RefreshInterval = 50,
                RequestQueueSize = 51,
                ResolvePeerCountries = true,
                ResumeDataStorageType = "fastresume",
                RssAutoDownloadingEnabled = true,
                RssDownloadRepackProperEpisodes = true,
                RssFetchDelay = 5200L,
                RssMaxArticlesPerFeed = 52,
                RssProcessingEnabled = true,
                RssRefreshInterval = 53,
                RssSmartEpisodeFilters = "s01e01",
                SavePath = "/save-a",
                SavePathChangedTmmEnabled = true,
                SaveResumeDataInterval = 54,
                SaveStatisticsInterval = 540,
                ScanDirs = new Dictionary<string, SaveLocation>
                {
                    ["watch"] = SaveLocation.Create(0),
                    ["default"] = SaveLocation.Create(1),
                    ["custom"] = SaveLocation.Create("/dls/a")
                },
                ScheduleFromHour = 1,
                ScheduleFromMin = 2,
                ScheduleToHour = 3,
                ScheduleToMin = 4,
                SchedulerDays = 5,
                SchedulerEnabled = true,
                SendBufferLowWatermark = 55,
                SendBufferWatermark = 56,
                SendBufferWatermarkFactor = 57,
                SlowTorrentDlRateThreshold = 58,
                SlowTorrentInactiveTimer = 59,
                SlowTorrentUlRateThreshold = 60,
                SocketBacklogSize = 61,
                SocketReceiveBufferSize = 62,
                SocketSendBufferSize = 63,
                SsrfMitigation = true,
                StopTrackerTimeout = 64,
                TempPath = "/tmp-a",
                TempPathEnabled = true,
                TorrentChangedTmmEnabled = true,
                TorrentContentLayout = "original",
                TorrentContentRemoveOption = "to_trash",
                TorrentFileSizeLimit = 65,
                TorrentStopCondition = "metadata_received",
                UpLimit = 66,
                UploadChokingAlgorithm = 67,
                UploadSlotsBehavior = 68,
                Upnp = true,
                UpnpLeaseDuration = 69,
                UseCategoryPathsInManualMode = true,
                UseHttps = true,
                IgnoreSslErrors = true,
                UseSubcategories = true,
                UtpTcpMixedMode = 70,
                ValidateHttpsTrackerCertificate = true,
                WebUiAddress = "0.0.0.0",
                WebUiApiKey = "api-key-a",
                WebUiBanDuration = 71,
                WebUiClickjackingProtectionEnabled = true,
                WebUiCsrfProtectionEnabled = true,
                WebUiCustomHttpHeaders = "X-Header: A",
                WebUiDomainList = "a.example.com",
                WebUiHostHeaderValidationEnabled = true,
                WebUiHttpsCertPath = "/cert-a",
                WebUiHttpsKeyPath = "/key-a",
                WebUiMaxAuthFailCount = 72,
                WebUiPort = 8081,
                WebUiReverseProxiesList = "10.0.0.0/8",
                WebUiReverseProxyEnabled = true,
                WebUiSecureCookieEnabled = true,
                WebUiSessionTimeout = 73,
                WebUiUpnp = true,
                WebUiUseCustomHttpHeadersEnabled = true,
                WebUiUsername = "admin-a",
                WebUiPassword = "pass-a",
                ConfirmTorrentDeletion = true,
                ConfirmTorrentRecheck = true,
                StatusBarExternalIp = true
            };
        }

        private static UpdatePreferences BuildAllSetB_AllNonNull()
        {
            return new UpdatePreferences
            {
                AddToTopOfQueue = false,
                AddStoppedEnabled = false,
                AddTrackers = "x, y",
                AddTrackersEnabled = false,
                AltDlLimit = 101,
                AltUpLimit = 102,
                AlternativeWebuiEnabled = false,
                AlternativeWebuiPath = "/alt-b",
                AnnounceIp = "5.6.7.8",
                AnnouncePort = 6882,
                AnnounceToAllTiers = false,
                AnnounceToAllTrackers = false,
                AnonymousMode = false,
                AppInstanceName = "app-b",
                AsyncIoThreads = 103,
                AutoDeleteMode = 104,
                AutoTmmEnabled = false,
                AutorunEnabled = false,
                AutorunOnTorrentAddedEnabled = false,
                AutorunOnTorrentAddedProgram = "prog-added-b",
                AutorunProgram = "prog-b",
                BannedIPs = "10.1.1.1",
                BdecodeDepthLimit = 105,
                BdecodeTokenLimit = 106,
                BittorrentProtocol = 107,
                BlockPeersOnPrivilegedPorts = false,
                BypassAuthSubnetWhitelist = "10.10.0.0/16",
                BypassAuthSubnetWhitelistEnabled = false,
                BypassLocalAuth = false,
                CategoryChangedTmmEnabled = false,
                CheckingMemoryUse = 108,
                ConnectionSpeed = 109,
                CurrentInterfaceAddress = "eth1-addr-b",
                CurrentInterfaceName = "eth1-name-b",
                CurrentNetworkInterface = "eth1-b",
                Dht = false,
                DhtBootstrapNodes = "node-c",
                DiskCache = 110,
                DiskCacheTtl = 111,
                DiskIoReadMode = 112,
                DiskIoType = 113,
                DiskIoWriteMode = 114,
                DiskQueueSize = 115,
                DlLimit = 116,
                DontCountSlowTorrents = false,
                DyndnsDomain = "dyndns-b",
                DyndnsEnabled = false,
                DyndnsPassword = "dyndns-pass-b",
                DyndnsService = 117,
                DyndnsUsername = "dyndns-user-b",
                EmbeddedTrackerPort = 118,
                EmbeddedTrackerPortForwarding = false,
                EnableCoalesceReadWrite = false,
                EnableEmbeddedTracker = false,
                EnableMultiConnectionsFromSameIp = false,
                EnablePieceExtentAffinity = false,
                EnableUploadSuggestions = false,
                Encryption = 119,
                ExcludedFileNames = "*.cache",
                ExcludedFileNamesEnabled = false,
                ExportDir = "/export-b",
                ExportDirFin = "/export-fin-b",
                FileLogAge = 120,
                FileLogAgeType = 121,
                FileLogBackupEnabled = false,
                FileLogDeleteOld = false,
                FileLogEnabled = false,
                FileLogMaxSize = 122,
                FileLogPath = "/log-b",
                FilePoolSize = 123,
                HashingThreads = 124,
                I2pAddress = "i2p-addr-b",
                I2pEnabled = false,
                I2pInboundLength = 125,
                I2pInboundQuantity = 126,
                I2pMixedMode = false,
                I2pOutboundLength = 127,
                I2pOutboundQuantity = 128,
                I2pPort = 129,
                IdnSupportEnabled = false,
                IncompleteFilesExt = false,
                UseUnwantedFolder = false,
                IpFilterEnabled = false,
                IpFilterPath = "/ipfilter-b",
                IpFilterTrackers = false,
                LimitLanPeers = false,
                LimitTcpOverhead = false,
                LimitUtpRate = false,
                ListenPort = 130,
                SslEnabled = false,
                SslListenPort = 4431,
                Locale = "en-B",
                Lsd = false,
                MailNotificationAuthEnabled = false,
                MailNotificationEmail = "mail@b",
                MailNotificationEnabled = false,
                MailNotificationPassword = "mail-pass-b",
                MailNotificationSender = "sender-b",
                MailNotificationSmtp = "smtp-b",
                MailNotificationSslEnabled = false,
                MailNotificationUsername = "mail-user-b",
                MarkOfTheWeb = false,
                MaxActiveCheckingTorrents = 131,
                MaxActiveDownloads = 132,
                MaxActiveTorrents = 133,
                MaxActiveUploads = 134,
                MaxConcurrentHttpAnnounces = 135,
                MaxConnec = 136,
                MaxConnecPerTorrent = 137,
                MaxInactiveSeedingTime = 238, // non-null and different from A
                MaxInactiveSeedingTimeEnabled = false, // non-null here
                MaxRatio = 2.2f, // non-null here
                MaxRatioAct = 139,
                MaxRatioEnabled = true, // non-null here
                MaxSeedingTime = 240,   // non-null here
                MaxSeedingTimeEnabled = true, // non-null here
                MaxUploads = 141,
                MaxUploadsPerTorrent = 142,
                MemoryWorkingSetLimit = 143,
                MergeTrackers = false,
                OutgoingPortsMax = 144,
                OutgoingPortsMin = 145,
                PeerTos = 146,
                PeerTurnover = 147,
                PeerTurnoverCutoff = 148,
                PeerTurnoverInterval = 149,
                PerformanceWarning = false,
                Pex = false,
                PreallocateAll = false,
                ProxyAuthEnabled = false,
                ProxyBittorrent = false,
                ProxyHostnameLookup = false,
                ProxyIp = "proxy-ip-b",
                ProxyMisc = false,
                ProxyPassword = "proxy-pass-b",
                ProxyPeerConnections = false,
                ProxyPort = 8888,
                ProxyRss = false,
                ProxyType = "socks5",
                ProxyUsername = "proxy-user-b",
                PythonExecutablePath = "/python-b",
                QueueingEnabled = false,
                RandomPort = false,
                ReannounceWhenAddressChanged = false,
                RecheckCompletedTorrents = false,
                RefreshInterval = 150,
                RequestQueueSize = 151,
                ResolvePeerCountries = false,
                ResumeDataStorageType = "sqlite",
                RssAutoDownloadingEnabled = false,
                RssDownloadRepackProperEpisodes = false,
                RssFetchDelay = 5300L,
                RssMaxArticlesPerFeed = 152,
                RssProcessingEnabled = false,
                RssRefreshInterval = 153,
                RssSmartEpisodeFilters = "s02e02",
                SavePath = "/save-b",
                SavePathChangedTmmEnabled = false,
                SaveResumeDataInterval = 154,
                SaveStatisticsInterval = 1540,
                ScanDirs = new Dictionary<string, SaveLocation>
                {
                    ["watch"] = SaveLocation.Create(0),
                    ["default"] = SaveLocation.Create(1),
                    ["custom"] = SaveLocation.Create("/dls/b")
                },
                ScheduleFromHour = 11,
                ScheduleFromMin = 12,
                ScheduleToHour = 13,
                ScheduleToMin = 14,
                SchedulerDays = 15,
                SchedulerEnabled = false,
                SendBufferLowWatermark = 155,
                SendBufferWatermark = 156,
                SendBufferWatermarkFactor = 157,
                SlowTorrentDlRateThreshold = 158,
                SlowTorrentInactiveTimer = 159,
                SlowTorrentUlRateThreshold = 160,
                SocketBacklogSize = 161,
                SocketReceiveBufferSize = 162,
                SocketSendBufferSize = 163,
                SsrfMitigation = false,
                StopTrackerTimeout = 164,
                TempPath = "/tmp-b",
                TempPathEnabled = false,
                TorrentChangedTmmEnabled = false,
                TorrentContentLayout = "subfolder",
                TorrentContentRemoveOption = "delete",
                TorrentFileSizeLimit = 165,
                TorrentStopCondition = "files_checked",
                UpLimit = 166,
                UploadChokingAlgorithm = 167,
                UploadSlotsBehavior = 168,
                Upnp = false,
                UpnpLeaseDuration = 169,
                UseCategoryPathsInManualMode = false,
                UseHttps = false,
                IgnoreSslErrors = false,
                UseSubcategories = false,
                UtpTcpMixedMode = 170,
                ValidateHttpsTrackerCertificate = false,
                WebUiAddress = "127.0.0.1",
                WebUiApiKey = "api-key-b",
                WebUiBanDuration = 171,
                WebUiClickjackingProtectionEnabled = false,
                WebUiCsrfProtectionEnabled = false,
                WebUiCustomHttpHeaders = "X-Header: B",
                WebUiDomainList = "b.example.com",
                WebUiHostHeaderValidationEnabled = false,
                WebUiHttpsCertPath = "/cert-b",
                WebUiHttpsKeyPath = "/key-b",
                WebUiMaxAuthFailCount = 172,
                WebUiPort = 8181,
                WebUiReverseProxiesList = "192.168.0.0/16",
                WebUiReverseProxyEnabled = false,
                WebUiSecureCookieEnabled = false,
                WebUiSessionTimeout = 173,
                WebUiUpnp = false,
                WebUiUseCustomHttpHeadersEnabled = false,
                WebUiUsername = "admin-b",
                WebUiPassword = "pass-b",
                ConfirmTorrentDeletion = false,
                ConfirmTorrentRecheck = false,
                StatusBarExternalIp = false
            };
        }

        private static UpdatePreferences BuildPartialChange()
        {
            // Only a handful of fields are non-null; the rest null (=> retain originals).
            return new UpdatePreferences
            {
                AddToTopOfQueue = false,               // bool
                AltDlLimit = 222,                      // int
                SavePath = "/save-partial",            // string
                MaxRatio = 3.3f,                       // float (leave MaxRatioEnabled null)
                ProxyIp = "proxy-new",                 // string
                TempPathEnabled = false,               // bool
                WebUiPort = 9090,                      // int
                RssFetchDelay = 7777L,                 // long
                ScanDirs = new Dictionary<string, SaveLocation>
                {
                    ["watch"] = SaveLocation.Create(0),
                    ["custom"] = SaveLocation.Create("/new/custom")
                },
                // everything else null => “do not change”
            };
        }

        private static void AssertAllEqual(UpdatePreferences actual, UpdatePreferences expected)
        {
            // value-by-value assertions (no reflection)
            actual.AddToTopOfQueue.Should().Be(expected.AddToTopOfQueue);
            actual.AddStoppedEnabled.Should().Be(expected.AddStoppedEnabled);
            actual.AddTrackers.Should().Be(expected.AddTrackers);
            actual.AddTrackersEnabled.Should().Be(expected.AddTrackersEnabled);
            actual.AltDlLimit.Should().Be(expected.AltDlLimit);
            actual.AltUpLimit.Should().Be(expected.AltUpLimit);
            actual.AlternativeWebuiEnabled.Should().Be(expected.AlternativeWebuiEnabled);
            actual.AlternativeWebuiPath.Should().Be(expected.AlternativeWebuiPath);
            actual.AnnounceIp.Should().Be(expected.AnnounceIp);
            actual.AnnouncePort.Should().Be(expected.AnnouncePort);
            actual.AnnounceToAllTiers.Should().Be(expected.AnnounceToAllTiers);
            actual.AnnounceToAllTrackers.Should().Be(expected.AnnounceToAllTrackers);
            actual.AnonymousMode.Should().Be(expected.AnonymousMode);
            actual.AppInstanceName.Should().Be(expected.AppInstanceName);
            actual.AsyncIoThreads.Should().Be(expected.AsyncIoThreads);
            actual.AutoDeleteMode.Should().Be(expected.AutoDeleteMode);
            actual.AutoTmmEnabled.Should().Be(expected.AutoTmmEnabled);
            actual.AutorunEnabled.Should().Be(expected.AutorunEnabled);
            actual.AutorunOnTorrentAddedEnabled.Should().Be(expected.AutorunOnTorrentAddedEnabled);
            actual.AutorunOnTorrentAddedProgram.Should().Be(expected.AutorunOnTorrentAddedProgram);
            actual.AutorunProgram.Should().Be(expected.AutorunProgram);
            actual.BannedIPs.Should().Be(expected.BannedIPs);
            actual.BdecodeDepthLimit.Should().Be(expected.BdecodeDepthLimit);
            actual.BdecodeTokenLimit.Should().Be(expected.BdecodeTokenLimit);
            actual.BittorrentProtocol.Should().Be(expected.BittorrentProtocol);
            actual.BlockPeersOnPrivilegedPorts.Should().Be(expected.BlockPeersOnPrivilegedPorts);
            actual.BypassAuthSubnetWhitelist.Should().Be(expected.BypassAuthSubnetWhitelist);
            actual.BypassAuthSubnetWhitelistEnabled.Should().Be(expected.BypassAuthSubnetWhitelistEnabled);
            actual.BypassLocalAuth.Should().Be(expected.BypassLocalAuth);
            actual.CategoryChangedTmmEnabled.Should().Be(expected.CategoryChangedTmmEnabled);
            actual.CheckingMemoryUse.Should().Be(expected.CheckingMemoryUse);
            actual.ConnectionSpeed.Should().Be(expected.ConnectionSpeed);
            actual.CurrentInterfaceAddress.Should().Be(expected.CurrentInterfaceAddress);
            actual.CurrentInterfaceName.Should().Be(expected.CurrentInterfaceName);
            actual.CurrentNetworkInterface.Should().Be(expected.CurrentNetworkInterface);
            actual.Dht.Should().Be(expected.Dht);
            actual.DhtBootstrapNodes.Should().Be(expected.DhtBootstrapNodes);
            actual.DiskCache.Should().Be(expected.DiskCache);
            actual.DiskCacheTtl.Should().Be(expected.DiskCacheTtl);
            actual.DiskIoReadMode.Should().Be(expected.DiskIoReadMode);
            actual.DiskIoType.Should().Be(expected.DiskIoType);
            actual.DiskIoWriteMode.Should().Be(expected.DiskIoWriteMode);
            actual.DiskQueueSize.Should().Be(expected.DiskQueueSize);
            actual.DlLimit.Should().Be(expected.DlLimit);
            actual.DontCountSlowTorrents.Should().Be(expected.DontCountSlowTorrents);
            actual.DyndnsDomain.Should().Be(expected.DyndnsDomain);
            actual.DyndnsEnabled.Should().Be(expected.DyndnsEnabled);
            actual.DyndnsPassword.Should().Be(expected.DyndnsPassword);
            actual.DyndnsService.Should().Be(expected.DyndnsService);
            actual.DyndnsUsername.Should().Be(expected.DyndnsUsername);
            actual.EmbeddedTrackerPort.Should().Be(expected.EmbeddedTrackerPort);
            actual.EmbeddedTrackerPortForwarding.Should().Be(expected.EmbeddedTrackerPortForwarding);
            actual.EnableCoalesceReadWrite.Should().Be(expected.EnableCoalesceReadWrite);
            actual.EnableEmbeddedTracker.Should().Be(expected.EnableEmbeddedTracker);
            actual.EnableMultiConnectionsFromSameIp.Should().Be(expected.EnableMultiConnectionsFromSameIp);
            actual.EnablePieceExtentAffinity.Should().Be(expected.EnablePieceExtentAffinity);
            actual.EnableUploadSuggestions.Should().Be(expected.EnableUploadSuggestions);
            actual.Encryption.Should().Be(expected.Encryption);
            actual.ExcludedFileNames.Should().Be(expected.ExcludedFileNames);
            actual.ExcludedFileNamesEnabled.Should().Be(expected.ExcludedFileNamesEnabled);
            actual.ExportDir.Should().Be(expected.ExportDir);
            actual.ExportDirFin.Should().Be(expected.ExportDirFin);
            actual.FileLogAge.Should().Be(expected.FileLogAge);
            actual.FileLogAgeType.Should().Be(expected.FileLogAgeType);
            actual.FileLogBackupEnabled.Should().Be(expected.FileLogBackupEnabled);
            actual.FileLogDeleteOld.Should().Be(expected.FileLogDeleteOld);
            actual.FileLogEnabled.Should().Be(expected.FileLogEnabled);
            actual.FileLogMaxSize.Should().Be(expected.FileLogMaxSize);
            actual.FileLogPath.Should().Be(expected.FileLogPath);
            actual.FilePoolSize.Should().Be(expected.FilePoolSize);
            actual.HashingThreads.Should().Be(expected.HashingThreads);
            actual.I2pAddress.Should().Be(expected.I2pAddress);
            actual.I2pEnabled.Should().Be(expected.I2pEnabled);
            actual.I2pInboundLength.Should().Be(expected.I2pInboundLength);
            actual.I2pInboundQuantity.Should().Be(expected.I2pInboundQuantity);
            actual.I2pMixedMode.Should().Be(expected.I2pMixedMode);
            actual.I2pOutboundLength.Should().Be(expected.I2pOutboundLength);
            actual.I2pOutboundQuantity.Should().Be(expected.I2pOutboundQuantity);
            actual.I2pPort.Should().Be(expected.I2pPort);
            actual.IdnSupportEnabled.Should().Be(expected.IdnSupportEnabled);
            actual.IncompleteFilesExt.Should().Be(expected.IncompleteFilesExt);
            actual.UseUnwantedFolder.Should().Be(expected.UseUnwantedFolder);
            actual.IpFilterEnabled.Should().Be(expected.IpFilterEnabled);
            actual.IpFilterPath.Should().Be(expected.IpFilterPath);
            actual.IpFilterTrackers.Should().Be(expected.IpFilterTrackers);
            actual.LimitLanPeers.Should().Be(expected.LimitLanPeers);
            actual.LimitTcpOverhead.Should().Be(expected.LimitTcpOverhead);
            actual.LimitUtpRate.Should().Be(expected.LimitUtpRate);
            actual.ListenPort.Should().Be(expected.ListenPort);
            actual.SslEnabled.Should().Be(expected.SslEnabled);
            actual.SslListenPort.Should().Be(expected.SslListenPort);
            actual.Locale.Should().Be(expected.Locale);
            actual.Lsd.Should().Be(expected.Lsd);
            actual.MailNotificationAuthEnabled.Should().Be(expected.MailNotificationAuthEnabled);
            actual.MailNotificationEmail.Should().Be(expected.MailNotificationEmail);
            actual.MailNotificationEnabled.Should().Be(expected.MailNotificationEnabled);
            actual.MailNotificationPassword.Should().Be(expected.MailNotificationPassword);
            actual.MailNotificationSender.Should().Be(expected.MailNotificationSender);
            actual.MailNotificationSmtp.Should().Be(expected.MailNotificationSmtp);
            actual.MailNotificationSslEnabled.Should().Be(expected.MailNotificationSslEnabled);
            actual.MailNotificationUsername.Should().Be(expected.MailNotificationUsername);
            actual.MarkOfTheWeb.Should().Be(expected.MarkOfTheWeb);
            actual.MaxActiveCheckingTorrents.Should().Be(expected.MaxActiveCheckingTorrents);
            actual.MaxActiveDownloads.Should().Be(expected.MaxActiveDownloads);
            actual.MaxActiveTorrents.Should().Be(expected.MaxActiveTorrents);
            actual.MaxActiveUploads.Should().Be(expected.MaxActiveUploads);
            actual.MaxConcurrentHttpAnnounces.Should().Be(expected.MaxConcurrentHttpAnnounces);
            actual.MaxConnec.Should().Be(expected.MaxConnec);
            actual.MaxConnecPerTorrent.Should().Be(expected.MaxConnecPerTorrent);
            actual.MaxInactiveSeedingTime.Should().Be(expected.MaxInactiveSeedingTime);
            actual.MaxInactiveSeedingTimeEnabled.Should().Be(expected.MaxInactiveSeedingTimeEnabled);
            actual.MaxRatio.Should().Be(expected.MaxRatio);
            actual.MaxRatioAct.Should().Be(expected.MaxRatioAct);
            actual.MaxRatioEnabled.Should().Be(expected.MaxRatioEnabled);
            actual.MaxSeedingTime.Should().Be(expected.MaxSeedingTime);
            actual.MaxSeedingTimeEnabled.Should().Be(expected.MaxSeedingTimeEnabled);
            actual.MaxUploads.Should().Be(expected.MaxUploads);
            actual.MaxUploadsPerTorrent.Should().Be(expected.MaxUploadsPerTorrent);
            actual.MemoryWorkingSetLimit.Should().Be(expected.MemoryWorkingSetLimit);
            actual.MergeTrackers.Should().Be(expected.MergeTrackers);
            actual.OutgoingPortsMax.Should().Be(expected.OutgoingPortsMax);
            actual.OutgoingPortsMin.Should().Be(expected.OutgoingPortsMin);
            actual.PeerTos.Should().Be(expected.PeerTos);
            actual.PeerTurnover.Should().Be(expected.PeerTurnover);
            actual.PeerTurnoverCutoff.Should().Be(expected.PeerTurnoverCutoff);
            actual.PeerTurnoverInterval.Should().Be(expected.PeerTurnoverInterval);
            actual.PerformanceWarning.Should().Be(expected.PerformanceWarning);
            actual.Pex.Should().Be(expected.Pex);
            actual.PreallocateAll.Should().Be(expected.PreallocateAll);
            actual.ProxyAuthEnabled.Should().Be(expected.ProxyAuthEnabled);
            actual.ProxyBittorrent.Should().Be(expected.ProxyBittorrent);
            actual.ProxyHostnameLookup.Should().Be(expected.ProxyHostnameLookup);
            actual.ProxyIp.Should().Be(expected.ProxyIp);
            actual.ProxyMisc.Should().Be(expected.ProxyMisc);
            actual.ProxyPassword.Should().Be(expected.ProxyPassword);
            actual.ProxyPeerConnections.Should().Be(expected.ProxyPeerConnections);
            actual.ProxyPort.Should().Be(expected.ProxyPort);
            actual.ProxyRss.Should().Be(expected.ProxyRss);
            actual.ProxyType.Should().Be(expected.ProxyType);
            actual.ProxyUsername.Should().Be(expected.ProxyUsername);
            actual.PythonExecutablePath.Should().Be(expected.PythonExecutablePath);
            actual.QueueingEnabled.Should().Be(expected.QueueingEnabled);
            actual.RandomPort.Should().Be(expected.RandomPort);
            actual.ReannounceWhenAddressChanged.Should().Be(expected.ReannounceWhenAddressChanged);
            actual.RecheckCompletedTorrents.Should().Be(expected.RecheckCompletedTorrents);
            actual.RefreshInterval.Should().Be(expected.RefreshInterval);
            actual.RequestQueueSize.Should().Be(expected.RequestQueueSize);
            actual.ResolvePeerCountries.Should().Be(expected.ResolvePeerCountries);
            actual.ResumeDataStorageType.Should().Be(expected.ResumeDataStorageType);
            actual.RssAutoDownloadingEnabled.Should().Be(expected.RssAutoDownloadingEnabled);
            actual.RssDownloadRepackProperEpisodes.Should().Be(expected.RssDownloadRepackProperEpisodes);
            actual.RssFetchDelay.Should().Be(expected.RssFetchDelay);
            actual.RssMaxArticlesPerFeed.Should().Be(expected.RssMaxArticlesPerFeed);
            actual.RssProcessingEnabled.Should().Be(expected.RssProcessingEnabled);
            actual.RssRefreshInterval.Should().Be(expected.RssRefreshInterval);
            actual.RssSmartEpisodeFilters.Should().Be(expected.RssSmartEpisodeFilters);
            actual.SavePath.Should().Be(expected.SavePath);
            actual.SavePathChangedTmmEnabled.Should().Be(expected.SavePathChangedTmmEnabled);
            actual.SaveResumeDataInterval.Should().Be(expected.SaveResumeDataInterval);
            actual.SaveStatisticsInterval.Should().Be(expected.SaveStatisticsInterval);

            if (expected.ScanDirs is null)
            {
                actual.ScanDirs.Should().BeNull();
            }
            else
            {
                actual.ScanDirs.Should().NotBeNull();
                actual.ScanDirs!.Count.Should().Be(expected.ScanDirs.Count);
                foreach (var kv in expected.ScanDirs)
                {
                    actual.ScanDirs.Should().ContainKey(kv.Key);
                    var act = actual.ScanDirs[kv.Key];
                    var exp = kv.Value;
                    act.IsWatchedFolder.Should().Be(exp.IsWatchedFolder);
                    act.IsDefaultFolder.Should().Be(exp.IsDefaultFolder);
                    act.SavePath.Should().Be(exp.SavePath);
                }
            }

            actual.ScheduleFromHour.Should().Be(expected.ScheduleFromHour);
            actual.ScheduleFromMin.Should().Be(expected.ScheduleFromMin);
            actual.ScheduleToHour.Should().Be(expected.ScheduleToHour);
            actual.ScheduleToMin.Should().Be(expected.ScheduleToMin);
            actual.SchedulerDays.Should().Be(expected.SchedulerDays);
            actual.SchedulerEnabled.Should().Be(expected.SchedulerEnabled);
            actual.SendBufferLowWatermark.Should().Be(expected.SendBufferLowWatermark);
            actual.SendBufferWatermark.Should().Be(expected.SendBufferWatermark);
            actual.SendBufferWatermarkFactor.Should().Be(expected.SendBufferWatermarkFactor);
            actual.SlowTorrentDlRateThreshold.Should().Be(expected.SlowTorrentDlRateThreshold);
            actual.SlowTorrentInactiveTimer.Should().Be(expected.SlowTorrentInactiveTimer);
            actual.SlowTorrentUlRateThreshold.Should().Be(expected.SlowTorrentUlRateThreshold);
            actual.SocketBacklogSize.Should().Be(expected.SocketBacklogSize);
            actual.SocketReceiveBufferSize.Should().Be(expected.SocketReceiveBufferSize);
            actual.SocketSendBufferSize.Should().Be(expected.SocketSendBufferSize);
            actual.SsrfMitigation.Should().Be(expected.SsrfMitigation);
            actual.StopTrackerTimeout.Should().Be(expected.StopTrackerTimeout);
            actual.TempPath.Should().Be(expected.TempPath);
            actual.TempPathEnabled.Should().Be(expected.TempPathEnabled);
            actual.TorrentChangedTmmEnabled.Should().Be(expected.TorrentChangedTmmEnabled);
            actual.TorrentContentLayout.Should().Be(expected.TorrentContentLayout);
            actual.TorrentContentRemoveOption.Should().Be(expected.TorrentContentRemoveOption);
            actual.TorrentFileSizeLimit.Should().Be(expected.TorrentFileSizeLimit);
            actual.TorrentStopCondition.Should().Be(expected.TorrentStopCondition);
            actual.UpLimit.Should().Be(expected.UpLimit);
            actual.UploadChokingAlgorithm.Should().Be(expected.UploadChokingAlgorithm);
            actual.UploadSlotsBehavior.Should().Be(expected.UploadSlotsBehavior);
            actual.Upnp.Should().Be(expected.Upnp);
            actual.UpnpLeaseDuration.Should().Be(expected.UpnpLeaseDuration);
            actual.UseCategoryPathsInManualMode.Should().Be(expected.UseCategoryPathsInManualMode);
            actual.UseHttps.Should().Be(expected.UseHttps);
            actual.IgnoreSslErrors.Should().Be(expected.IgnoreSslErrors);
            actual.UseSubcategories.Should().Be(expected.UseSubcategories);
            actual.UtpTcpMixedMode.Should().Be(expected.UtpTcpMixedMode);
            actual.ValidateHttpsTrackerCertificate.Should().Be(expected.ValidateHttpsTrackerCertificate);
            actual.WebUiAddress.Should().Be(expected.WebUiAddress);
            actual.WebUiApiKey.Should().Be(expected.WebUiApiKey);
            actual.WebUiBanDuration.Should().Be(expected.WebUiBanDuration);
            actual.WebUiClickjackingProtectionEnabled.Should().Be(expected.WebUiClickjackingProtectionEnabled);
            actual.WebUiCsrfProtectionEnabled.Should().Be(expected.WebUiCsrfProtectionEnabled);
            actual.WebUiCustomHttpHeaders.Should().Be(expected.WebUiCustomHttpHeaders);
            actual.WebUiDomainList.Should().Be(expected.WebUiDomainList);
            actual.WebUiHostHeaderValidationEnabled.Should().Be(expected.WebUiHostHeaderValidationEnabled);
            actual.WebUiHttpsCertPath.Should().Be(expected.WebUiHttpsCertPath);
            actual.WebUiHttpsKeyPath.Should().Be(expected.WebUiHttpsKeyPath);
            actual.WebUiMaxAuthFailCount.Should().Be(expected.WebUiMaxAuthFailCount);
            actual.WebUiPort.Should().Be(expected.WebUiPort);
            actual.WebUiReverseProxiesList.Should().Be(expected.WebUiReverseProxiesList);
            actual.WebUiReverseProxyEnabled.Should().Be(expected.WebUiReverseProxyEnabled);
            actual.WebUiSecureCookieEnabled.Should().Be(expected.WebUiSecureCookieEnabled);
            actual.WebUiSessionTimeout.Should().Be(expected.WebUiSessionTimeout);
            actual.WebUiUpnp.Should().Be(expected.WebUiUpnp);
            actual.WebUiUseCustomHttpHeadersEnabled.Should().Be(expected.WebUiUseCustomHttpHeadersEnabled);
            actual.WebUiUsername.Should().Be(expected.WebUiUsername);
            actual.WebUiPassword.Should().Be(expected.WebUiPassword);
            actual.ConfirmTorrentDeletion.Should().Be(expected.ConfirmTorrentDeletion);
            actual.ConfirmTorrentRecheck.Should().Be(expected.ConfirmTorrentRecheck);
            actual.StatusBarExternalIp.Should().Be(expected.StatusBarExternalIp);
        }

        // ---------- Tests ----------

        [Fact]
        public void GIVEN_NullOriginal_AND_ChangedHasAllNonNullValues_WHEN_MergePreferences_THEN_AllFieldsCopied()
        {
            var changed = BuildAllSetB_AllNonNull();

            var result = _target.MergePreferences(null, changed);

            AssertAllEqual(result, changed);
        }

        [Fact]
        public void GIVEN_OriginalExists_AND_ChangedHasAllNonNullValues_WHEN_MergePreferences_THEN_AllFieldsOverwritten()
        {
            var original = BuildAllSetA();
            var changed = BuildAllSetB_AllNonNull();

            var result = _target.MergePreferences(original, changed);

            AssertAllEqual(result, changed);
            result.ScanDirs.Should().BeSameAs(changed.ScanDirs);
        }

        [Fact]
        public void GIVEN_OriginalExists_AND_ChangedHasAllNullValues_WHEN_MergePreferences_THEN_AllOriginalRetained()
        {
            var original = BuildAllSetA();
            var changed = new UpdatePreferences(); // everything null

            var result = _target.MergePreferences(original, changed);

            AssertAllEqual(result, original);
        }

        [Fact]
        public void GIVEN_OriginalExists_AND_ChangedHasMixOfNullAndNonNull_WHEN_MergePreferences_THEN_OnlyNonNullOverwrite()
        {
            var original = BuildAllSetA();
            var changed = BuildPartialChange();

            var result = _target.MergePreferences(original, changed);

            // Build expected by starting from A, then applying only the non-null fields we set in BuildPartialChange()
            var expected = BuildAllSetA();
            expected.AddToTopOfQueue = false;
            expected.AltDlLimit = 222;
            expected.SavePath = "/save-partial";
            expected.MaxRatio = 3.3f;
            // NOTE: MaxRatioEnabled remains as in A (null) because changed.MaxRatioEnabled is null.
            expected.ProxyIp = "proxy-new";
            expected.TempPathEnabled = false;
            expected.WebUiPort = 9090;
            expected.RssFetchDelay = 7777L;
            expected.ScanDirs = new Dictionary<string, SaveLocation>
            {
                ["watch"] = SaveLocation.Create(0),
                ["custom"] = SaveLocation.Create("/new/custom")
            };

            AssertAllEqual(result, expected);
        }

        // ---------- Validate() rule tests ----------

        [Fact]
        public void GIVEN_MaxRatioAndMaxRatioEnabledBothSet_WHEN_Validate_THEN_Throws()
        {
            var p = new UpdatePreferences { MaxRatio = 1.0f, MaxRatioEnabled = true };
            Action act = () => p.Validate();
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*max_ratio or max_ratio_enabled*");
        }

        [Fact]
        public void GIVEN_MaxSeedingTimeAndMaxSeedingTimeEnabledBothSet_WHEN_Validate_THEN_Throws()
        {
            var p = new UpdatePreferences { MaxSeedingTime = 10, MaxSeedingTimeEnabled = true };
            Action act = () => p.Validate();
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*max_seeding_time or max_seeding_time_enabled*");
        }

        [Fact]
        public void GIVEN_MaxInactiveSeedingTimeAndEnabledBothSet_WHEN_Validate_THEN_Throws()
        {
            var p = new UpdatePreferences { MaxInactiveSeedingTime = 10, MaxInactiveSeedingTimeEnabled = true };
            Action act = () => p.Validate();
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*max_inactive_seeding_time or max_inactive_seeding_time_enabled*");
        }
    }
}