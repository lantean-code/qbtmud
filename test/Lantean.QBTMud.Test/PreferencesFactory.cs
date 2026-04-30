using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Test
{
    internal static class PreferencesFactory
    {
        internal static Lantean.QBTMud.Models.QBittorrentPreferences CreateQBittorrentPreferences(Action<PreferencesSpec> configure)
        {
            var spec = new PreferencesSpec();

            configure.Invoke(spec);

            return new Lantean.QBTMud.Models.QBittorrentPreferences
            {
                Locale = spec.Locale,
                AutoTmmEnabled = spec.AutoTmmEnabled,
                SavePath = spec.SavePath,
                TempPath = spec.TempPath,
                TempPathEnabled = spec.TempPathEnabled,
                AddStoppedEnabled = spec.AddStoppedEnabled,
                AddToTopOfQueue = spec.AddToTopOfQueue,
                TorrentStopCondition = spec.TorrentStopCondition,
                TorrentContentLayout = spec.TorrentContentLayout,
                MaxRatioEnabled = spec.MaxRatioEnabled,
                MaxRatio = spec.MaxRatio,
                MaxSeedingTimeEnabled = spec.MaxSeedingTimeEnabled,
                MaxSeedingTime = spec.MaxSeedingTime,
                MaxInactiveSeedingTimeEnabled = spec.MaxInactiveSeedingTimeEnabled,
                MaxInactiveSeedingTime = spec.MaxInactiveSeedingTime,
                QueueingEnabled = spec.QueueingEnabled,
                ConfirmTorrentDeletion = spec.ConfirmTorrentDeletion,
                DeleteTorrentContentFiles = spec.DeleteTorrentContentFiles,
                ConfirmTorrentRecheck = spec.ConfirmTorrentRecheck,
                StatusBarExternalIp = spec.StatusBarExternalIp,
                RssProcessingEnabled = spec.RssProcessingEnabled,
                UseSubcategories = spec.UseSubcategories,
                ResolvePeerCountries = spec.ResolvePeerCountries,
                RefreshInterval = spec.RefreshInterval
            };
        }

        internal static Preferences CreatePreferences(Action<PreferencesSpec> configure)
        {
            var spec = new PreferencesSpec();

            configure.Invoke(spec);

            return new Preferences(
                addToTopOfQueue: spec.AddToTopOfQueue,
                addStoppedEnabled: spec.AddStoppedEnabled,
                addTrackers: spec.AddTrackers,
                addTrackersEnabled: spec.AddTrackersEnabled,
                addTrackersFromUrlEnabled: spec.AddTrackersFromUrlEnabled,
                addTrackersUrl: spec.AddTrackersUrl,
                addTrackersUrlList: spec.AddTrackersUrlList,
                altDlLimit: spec.AltDlLimit,
                altUpLimit: spec.AltUpLimit,
                alternativeWebuiEnabled: spec.AlternativeWebuiEnabled,
                alternativeWebuiPath: spec.AlternativeWebuiPath,
                announceIp: spec.AnnounceIp,
                announcePort: spec.AnnouncePort,
                announceToAllTiers: spec.AnnounceToAllTiers,
                announceToAllTrackers: spec.AnnounceToAllTrackers,
                anonymousMode: spec.AnonymousMode,
                appInstanceName: spec.AppInstanceName,
                asyncIoThreads: spec.AsyncIoThreads,
                autoDeleteMode: spec.AutoDeleteMode,
                autoTmmEnabled: spec.AutoTmmEnabled,
                autorunEnabled: spec.AutorunEnabled,
                autorunOnTorrentAddedEnabled: spec.AutorunOnTorrentAddedEnabled,
                autorunOnTorrentAddedProgram: spec.AutorunOnTorrentAddedProgram,
                autorunProgram: spec.AutorunProgram,
                deleteTorrentContentFiles: spec.DeleteTorrentContentFiles,
                bannedIPs: spec.BannedIPs,
                bdecodeDepthLimit: spec.BdecodeDepthLimit,
                bdecodeTokenLimit: spec.BdecodeTokenLimit,
                bittorrentProtocol: spec.BittorrentProtocol,
                blockPeersOnPrivilegedPorts: spec.BlockPeersOnPrivilegedPorts,
                bypassAuthSubnetWhitelist: spec.BypassAuthSubnetWhitelist,
                bypassAuthSubnetWhitelistEnabled: spec.BypassAuthSubnetWhitelistEnabled,
                bypassLocalAuth: spec.BypassLocalAuth,
                categoryChangedTmmEnabled: spec.CategoryChangedTmmEnabled,
                checkingMemoryUse: spec.CheckingMemoryUse,
                connectionSpeed: spec.ConnectionSpeed,
                currentInterfaceAddress: spec.CurrentInterfaceAddress,
                currentInterfaceName: spec.CurrentInterfaceName,
                currentNetworkInterface: spec.CurrentNetworkInterface,
                dht: spec.Dht,
                dhtBootstrapNodes: spec.DhtBootstrapNodes,
                diskCache: spec.DiskCache,
                diskCacheTtl: spec.DiskCacheTtl,
                diskIoReadMode: spec.DiskIoReadMode,
                diskIoType: spec.DiskIoType,
                diskIoWriteMode: spec.DiskIoWriteMode,
                diskQueueSize: spec.DiskQueueSize,
                dlLimit: spec.DlLimit,
                dontCountSlowTorrents: spec.DontCountSlowTorrents,
                dyndnsDomain: spec.DyndnsDomain,
                dyndnsEnabled: spec.DyndnsEnabled,
                dyndnsPassword: spec.DyndnsPassword,
                dyndnsService: spec.DyndnsService,
                dyndnsUsername: spec.DyndnsUsername,
                embeddedTrackerPort: spec.EmbeddedTrackerPort,
                embeddedTrackerPortForwarding: spec.EmbeddedTrackerPortForwarding,
                enableCoalesceReadWrite: spec.EnableCoalesceReadWrite,
                enableEmbeddedTracker: spec.EnableEmbeddedTracker,
                enableMultiConnectionsFromSameIp: spec.EnableMultiConnectionsFromSameIp,
                enablePieceExtentAffinity: spec.EnablePieceExtentAffinity,
                enableUploadSuggestions: spec.EnableUploadSuggestions,
                encryption: spec.Encryption,
                excludedFileNames: spec.ExcludedFileNames,
                excludedFileNamesEnabled: spec.ExcludedFileNamesEnabled,
                exportDir: spec.ExportDir,
                exportDirFin: spec.ExportDirFin,
                fileLogAge: spec.FileLogAge,
                fileLogAgeType: spec.FileLogAgeType,
                fileLogBackupEnabled: spec.FileLogBackupEnabled,
                fileLogDeleteOld: spec.FileLogDeleteOld,
                fileLogEnabled: spec.FileLogEnabled,
                fileLogMaxSize: spec.FileLogMaxSize,
                fileLogPath: spec.FileLogPath,
                filePoolSize: spec.FilePoolSize,
                hashingThreads: spec.HashingThreads,
                i2pAddress: spec.I2pAddress,
                i2pEnabled: spec.I2pEnabled,
                i2pInboundLength: spec.I2pInboundLength,
                i2pInboundQuantity: spec.I2pInboundQuantity,
                i2pMixedMode: spec.I2pMixedMode,
                i2pOutboundLength: spec.I2pOutboundLength,
                i2pOutboundQuantity: spec.I2pOutboundQuantity,
                i2pPort: spec.I2pPort,
                idnSupportEnabled: spec.IdnSupportEnabled,
                incompleteFilesExt: spec.IncompleteFilesExt,
                useUnwantedFolder: spec.UseUnwantedFolder,
                ipFilterEnabled: spec.IpFilterEnabled,
                ipFilterPath: spec.IpFilterPath,
                ipFilterTrackers: spec.IpFilterTrackers,
                limitLanPeers: spec.LimitLanPeers,
                limitTcpOverhead: spec.LimitTcpOverhead,
                limitUtpRate: spec.LimitUtpRate,
                listenPort: spec.ListenPort,
                sslEnabled: spec.SslEnabled,
                sslListenPort: spec.SslListenPort,
                locale: spec.Locale,
                lsd: spec.Lsd,
                mailNotificationAuthEnabled: spec.MailNotificationAuthEnabled,
                mailNotificationEmail: spec.MailNotificationEmail,
                mailNotificationEnabled: spec.MailNotificationEnabled,
                mailNotificationPassword: spec.MailNotificationPassword,
                mailNotificationSender: spec.MailNotificationSender,
                mailNotificationSmtp: spec.MailNotificationSmtp,
                mailNotificationSslEnabled: spec.MailNotificationSslEnabled,
                mailNotificationUsername: spec.MailNotificationUsername,
                markOfTheWeb: spec.MarkOfTheWeb,
                maxActiveCheckingTorrents: spec.MaxActiveCheckingTorrents,
                maxActiveDownloads: spec.MaxActiveDownloads,
                maxActiveTorrents: spec.MaxActiveTorrents,
                maxActiveUploads: spec.MaxActiveUploads,
                maxConcurrentHttpAnnounces: spec.MaxConcurrentHttpAnnounces,
                maxConnec: spec.MaxConnec,
                maxConnecPerTorrent: spec.MaxConnecPerTorrent,
                maxInactiveSeedingTime: spec.MaxInactiveSeedingTime,
                maxInactiveSeedingTimeEnabled: spec.MaxInactiveSeedingTimeEnabled,
                maxRatio: spec.MaxRatio,
                maxRatioAct: spec.MaxRatioAct,
                maxRatioEnabled: spec.MaxRatioEnabled,
                maxSeedingTime: spec.MaxSeedingTime,
                maxSeedingTimeEnabled: spec.MaxSeedingTimeEnabled,
                maxUploads: spec.MaxUploads,
                maxUploadsPerTorrent: spec.MaxUploadsPerTorrent,
                memoryWorkingSetLimit: spec.MemoryWorkingSetLimit,
                mergeTrackers: spec.MergeTrackers,
                outgoingPortsMax: spec.OutgoingPortsMax,
                outgoingPortsMin: spec.OutgoingPortsMin,
                peerTos: spec.PeerTos,
                peerTurnover: spec.PeerTurnover,
                peerTurnoverCutoff: spec.PeerTurnoverCutoff,
                peerTurnoverInterval: spec.PeerTurnoverInterval,
                performanceWarning: spec.PerformanceWarning,
                pex: spec.Pex,
                preallocateAll: spec.PreallocateAll,
                proxyAuthEnabled: spec.ProxyAuthEnabled,
                proxyBittorrent: spec.ProxyBittorrent,
                proxyHostnameLookup: spec.ProxyHostnameLookup,
                proxyIp: spec.ProxyIp,
                proxyMisc: spec.ProxyMisc,
                proxyPassword: spec.ProxyPassword,
                proxyPeerConnections: spec.ProxyPeerConnections,
                proxyPort: spec.ProxyPort,
                proxyRss: spec.ProxyRss,
                proxyType: spec.ProxyType,
                proxyUsername: spec.ProxyUsername,
                pythonExecutablePath: spec.PythonExecutablePath,
                queueingEnabled: spec.QueueingEnabled,
                randomPort: spec.RandomPort,
                reannounceWhenAddressChanged: spec.ReannounceWhenAddressChanged,
                recheckCompletedTorrents: spec.RecheckCompletedTorrents,
                refreshInterval: spec.RefreshInterval,
                requestQueueSize: spec.RequestQueueSize,
                resolvePeerHostNames: spec.ResolvePeerHostNames,
                resolvePeerCountries: spec.ResolvePeerCountries,
                resumeDataStorageType: spec.ResumeDataStorageType,
                rssAutoDownloadingEnabled: spec.RssAutoDownloadingEnabled,
                rssFetchDelay: spec.RssFetchDelay,
                rssDownloadRepackProperEpisodes: spec.RssDownloadRepackProperEpisodes,
                rssMaxArticlesPerFeed: spec.RssMaxArticlesPerFeed,
                rssProcessingEnabled: spec.RssProcessingEnabled,
                rssRefreshInterval: spec.RssRefreshInterval,
                rssSmartEpisodeFilters: spec.RssSmartEpisodeFilters,
                savePath: spec.SavePath,
                savePathChangedTmmEnabled: spec.SavePathChangedTmmEnabled,
                saveResumeDataInterval: spec.SaveResumeDataInterval,
                saveStatisticsInterval: spec.SaveStatisticsInterval,
                scanDirs: spec.ScanDirs,
                scheduleFromHour: spec.ScheduleFromHour,
                scheduleFromMin: spec.ScheduleFromMin,
                scheduleToHour: spec.ScheduleToHour,
                scheduleToMin: spec.ScheduleToMin,
                schedulerDays: spec.SchedulerDays,
                schedulerEnabled: spec.SchedulerEnabled,
                sendBufferLowWatermark: spec.SendBufferLowWatermark,
                sendBufferWatermark: spec.SendBufferWatermark,
                sendBufferWatermarkFactor: spec.SendBufferWatermarkFactor,
                slowTorrentDlRateThreshold: spec.SlowTorrentDlRateThreshold,
                slowTorrentInactiveTimer: spec.SlowTorrentInactiveTimer,
                slowTorrentUlRateThreshold: spec.SlowTorrentUlRateThreshold,
                socketBacklogSize: spec.SocketBacklogSize,
                socketReceiveBufferSize: spec.SocketReceiveBufferSize,
                socketSendBufferSize: spec.SocketSendBufferSize,
                ssrfMitigation: spec.SsrfMitigation,
                stopTrackerTimeout: spec.StopTrackerTimeout,
                tempPath: spec.TempPath,
                tempPathEnabled: spec.TempPathEnabled,
                torrentChangedTmmEnabled: spec.TorrentChangedTmmEnabled,
                torrentContentLayout: spec.TorrentContentLayout,
                torrentContentRemoveOption: spec.TorrentContentRemoveOption,
                torrentFileSizeLimit: spec.TorrentFileSizeLimit,
                torrentStopCondition: spec.TorrentStopCondition,
                upLimit: spec.UpLimit,
                uploadChokingAlgorithm: spec.UploadChokingAlgorithm,
                uploadSlotsBehavior: spec.UploadSlotsBehavior,
                upnp: spec.Upnp,
                upnpLeaseDuration: spec.UpnpLeaseDuration,
                useCategoryPathsInManualMode: spec.UseCategoryPathsInManualMode,
                useHttps: spec.UseHttps,
                ignoreSslErrors: spec.IgnoreSslErrors,
                useSubcategories: spec.UseSubcategories,
                utpTcpMixedMode: spec.UtpTcpMixedMode,
                validateHttpsTrackerCertificate: spec.ValidateHttpsTrackerCertificate,
                hostnameCacheTtl: spec.HostnameCacheTtl,
                webUiAddress: spec.WebUiAddress,
                webUiApiKey: spec.WebUiApiKey,
                webUiBanDuration: spec.WebUiBanDuration,
                webUiClickjackingProtectionEnabled: spec.WebUiClickjackingProtectionEnabled,
                webUiCsrfProtectionEnabled: spec.WebUiCsrfProtectionEnabled,
                webUiCustomHttpHeaders: spec.WebUiCustomHttpHeaders,
                webUiDomainList: spec.WebUiDomainList,
                webUiHostHeaderValidationEnabled: spec.WebUiHostHeaderValidationEnabled,
                webUiHttpsCertPath: spec.WebUiHttpsCertPath,
                webUiHttpsKeyPath: spec.WebUiHttpsKeyPath,
                webUiMaxAuthFailCount: spec.WebUiMaxAuthFailCount,
                webUiPort: spec.WebUiPort,
                webUiReverseProxiesList: spec.WebUiReverseProxiesList,
                webUiReverseProxyEnabled: spec.WebUiReverseProxyEnabled,
                webUiSecureCookieEnabled: spec.WebUiSecureCookieEnabled,
                webUiSessionTimeout: spec.WebUiSessionTimeout,
                webUiUpnp: spec.WebUiUpnp,
                webUiUseCustomHttpHeadersEnabled: spec.WebUiUseCustomHttpHeadersEnabled,
                webUiUsername: spec.WebUiUsername,
                confirmTorrentDeletion: spec.ConfirmTorrentDeletion,
                confirmTorrentRecheck: spec.ConfirmTorrentRecheck,
                statusBarExternalIp: spec.StatusBarExternalIp);
        }

        internal sealed class PreferencesSpec
        {
            public bool AddToTopOfQueue { get; set; }
            public bool AddStoppedEnabled { get; set; }
            public string AddTrackers { get; set; } = string.Empty;
            public bool AddTrackersEnabled { get; set; }
            public bool AddTrackersFromUrlEnabled { get; set; }
            public string AddTrackersUrl { get; set; } = string.Empty;
            public string AddTrackersUrlList { get; set; } = string.Empty;
            public int AltDlLimit { get; set; }
            public int AltUpLimit { get; set; }
            public bool AlternativeWebuiEnabled { get; set; }
            public string AlternativeWebuiPath { get; set; } = string.Empty;
            public string AnnounceIp { get; set; } = string.Empty;
            public int AnnouncePort { get; set; }
            public bool AnnounceToAllTiers { get; set; }
            public bool AnnounceToAllTrackers { get; set; }
            public bool AnonymousMode { get; set; }
            public string AppInstanceName { get; set; } = string.Empty;
            public int AsyncIoThreads { get; set; }
            public AutoDeleteMode AutoDeleteMode { get; set; }
            public bool AutoTmmEnabled { get; set; }
            public bool AutorunEnabled { get; set; }
            public bool AutorunOnTorrentAddedEnabled { get; set; }
            public string AutorunOnTorrentAddedProgram { get; set; } = string.Empty;
            public string AutorunProgram { get; set; } = string.Empty;
            public bool DeleteTorrentContentFiles { get; set; }
            public string BannedIPs { get; set; } = string.Empty;
            public int BdecodeDepthLimit { get; set; }
            public int BdecodeTokenLimit { get; set; }
            public BittorrentProtocol BittorrentProtocol { get; set; }
            public bool BlockPeersOnPrivilegedPorts { get; set; }
            public string BypassAuthSubnetWhitelist { get; set; } = string.Empty;
            public bool BypassAuthSubnetWhitelistEnabled { get; set; }
            public bool BypassLocalAuth { get; set; }
            public bool CategoryChangedTmmEnabled { get; set; }
            public int CheckingMemoryUse { get; set; }
            public int ConnectionSpeed { get; set; }
            public string CurrentInterfaceAddress { get; set; } = string.Empty;
            public string CurrentInterfaceName { get; set; } = string.Empty;
            public string CurrentNetworkInterface { get; set; } = string.Empty;
            public bool Dht { get; set; }
            public string DhtBootstrapNodes { get; set; } = string.Empty;
            public int DiskCache { get; set; }
            public int DiskCacheTtl { get; set; }
            public DiskIoReadMode DiskIoReadMode { get; set; }
            public DiskIoType DiskIoType { get; set; }
            public DiskIoWriteMode DiskIoWriteMode { get; set; }
            public int DiskQueueSize { get; set; }
            public int DlLimit { get; set; }
            public bool DontCountSlowTorrents { get; set; }
            public string DyndnsDomain { get; set; } = string.Empty;
            public bool DyndnsEnabled { get; set; }
            public string DyndnsPassword { get; set; } = string.Empty;
            public DyndnsService DyndnsService { get; set; }
            public string DyndnsUsername { get; set; } = string.Empty;
            public int EmbeddedTrackerPort { get; set; }
            public bool EmbeddedTrackerPortForwarding { get; set; }
            public bool EnableCoalesceReadWrite { get; set; }
            public bool EnableEmbeddedTracker { get; set; }
            public bool EnableMultiConnectionsFromSameIp { get; set; }
            public bool EnablePieceExtentAffinity { get; set; }
            public bool EnableUploadSuggestions { get; set; }
            public EncryptionMode Encryption { get; set; }
            public string ExcludedFileNames { get; set; } = string.Empty;
            public bool ExcludedFileNamesEnabled { get; set; }
            public string ExportDir { get; set; } = string.Empty;
            public string ExportDirFin { get; set; } = string.Empty;
            public int FileLogAge { get; set; }
            public int FileLogAgeType { get; set; }
            public bool FileLogBackupEnabled { get; set; }
            public bool FileLogDeleteOld { get; set; }
            public bool FileLogEnabled { get; set; }
            public int FileLogMaxSize { get; set; }
            public string FileLogPath { get; set; } = string.Empty;
            public int FilePoolSize { get; set; }
            public int HashingThreads { get; set; }
            public string I2pAddress { get; set; } = string.Empty;
            public bool I2pEnabled { get; set; }
            public int I2pInboundLength { get; set; }
            public int I2pInboundQuantity { get; set; }
            public bool I2pMixedMode { get; set; }
            public int I2pOutboundLength { get; set; }
            public int I2pOutboundQuantity { get; set; }
            public int I2pPort { get; set; }
            public bool IdnSupportEnabled { get; set; }
            public bool IncompleteFilesExt { get; set; }
            public bool UseUnwantedFolder { get; set; }
            public bool IpFilterEnabled { get; set; }
            public string IpFilterPath { get; set; } = string.Empty;
            public bool IpFilterTrackers { get; set; }
            public bool LimitLanPeers { get; set; }
            public bool LimitTcpOverhead { get; set; }
            public bool LimitUtpRate { get; set; }
            public int ListenPort { get; set; }
            public bool SslEnabled { get; set; }
            public int SslListenPort { get; set; }
            public string Locale { get; set; } = string.Empty;
            public bool Lsd { get; set; }
            public bool MailNotificationAuthEnabled { get; set; }
            public string MailNotificationEmail { get; set; } = string.Empty;
            public bool MailNotificationEnabled { get; set; }
            public string MailNotificationPassword { get; set; } = string.Empty;
            public string MailNotificationSender { get; set; } = string.Empty;
            public string MailNotificationSmtp { get; set; } = string.Empty;
            public bool MailNotificationSslEnabled { get; set; }
            public string MailNotificationUsername { get; set; } = string.Empty;
            public bool MarkOfTheWeb { get; set; }
            public int MaxActiveCheckingTorrents { get; set; }
            public int MaxActiveDownloads { get; set; }
            public int MaxActiveTorrents { get; set; }
            public int MaxActiveUploads { get; set; }
            public int MaxConcurrentHttpAnnounces { get; set; }
            public int MaxConnec { get; set; }
            public int MaxConnecPerTorrent { get; set; }
            public int MaxInactiveSeedingTime { get; set; }
            public bool MaxInactiveSeedingTimeEnabled { get; set; }
            public float MaxRatio { get; set; }
            public MaxRatioAction MaxRatioAct { get; set; }
            public bool MaxRatioEnabled { get; set; }
            public int MaxSeedingTime { get; set; }
            public bool MaxSeedingTimeEnabled { get; set; }
            public int MaxUploads { get; set; }
            public int MaxUploadsPerTorrent { get; set; }
            public int MemoryWorkingSetLimit { get; set; }
            public bool MergeTrackers { get; set; }
            public int OutgoingPortsMax { get; set; }
            public int OutgoingPortsMin { get; set; }
            public int PeerTos { get; set; }
            public int PeerTurnover { get; set; }
            public int PeerTurnoverCutoff { get; set; }
            public int PeerTurnoverInterval { get; set; }
            public bool PerformanceWarning { get; set; }
            public bool Pex { get; set; }
            public bool PreallocateAll { get; set; }
            public bool ProxyAuthEnabled { get; set; }
            public bool ProxyBittorrent { get; set; }
            public bool ProxyHostnameLookup { get; set; }
            public string ProxyIp { get; set; } = string.Empty;
            public bool ProxyMisc { get; set; }
            public string ProxyPassword { get; set; } = string.Empty;
            public bool ProxyPeerConnections { get; set; }
            public int ProxyPort { get; set; }
            public bool ProxyRss { get; set; }
            public ProxyType ProxyType { get; set; }
            public string ProxyUsername { get; set; } = string.Empty;
            public string PythonExecutablePath { get; set; } = string.Empty;
            public bool QueueingEnabled { get; set; }
            public bool RandomPort { get; set; }
            public bool ReannounceWhenAddressChanged { get; set; }
            public bool RecheckCompletedTorrents { get; set; }
            public int RefreshInterval { get; set; }
            public int RequestQueueSize { get; set; }
            public bool? ResolvePeerHostNames { get; set; }
            public bool ResolvePeerCountries { get; set; }
            public ResumeDataStorageType ResumeDataStorageType { get; set; }
            public bool RssAutoDownloadingEnabled { get; set; }
            public long RssFetchDelay { get; set; }
            public bool RssDownloadRepackProperEpisodes { get; set; }
            public int RssMaxArticlesPerFeed { get; set; }
            public bool RssProcessingEnabled { get; set; }
            public int RssRefreshInterval { get; set; }
            public string RssSmartEpisodeFilters { get; set; } = string.Empty;
            public string SavePath { get; set; } = string.Empty;
            public bool SavePathChangedTmmEnabled { get; set; }
            public int SaveResumeDataInterval { get; set; }
            public int SaveStatisticsInterval { get; set; }
            public Dictionary<string, SaveLocation> ScanDirs { get; set; } = [];
            public int ScheduleFromHour { get; set; }
            public int ScheduleFromMin { get; set; }
            public int ScheduleToHour { get; set; }
            public int ScheduleToMin { get; set; }
            public SchedulerDays SchedulerDays { get; set; }
            public bool SchedulerEnabled { get; set; }
            public int SendBufferLowWatermark { get; set; }
            public int SendBufferWatermark { get; set; }
            public int SendBufferWatermarkFactor { get; set; }
            public int SlowTorrentDlRateThreshold { get; set; }
            public int SlowTorrentInactiveTimer { get; set; }
            public int SlowTorrentUlRateThreshold { get; set; }
            public int SocketBacklogSize { get; set; }
            public int SocketReceiveBufferSize { get; set; }
            public int SocketSendBufferSize { get; set; }
            public bool SsrfMitigation { get; set; }
            public int StopTrackerTimeout { get; set; }
            public string TempPath { get; set; } = string.Empty;
            public bool TempPathEnabled { get; set; }
            public bool TorrentChangedTmmEnabled { get; set; }
            public TorrentContentLayout TorrentContentLayout { get; set; }
            public TorrentContentRemoveOption TorrentContentRemoveOption { get; set; }
            public int TorrentFileSizeLimit { get; set; }
            public StopCondition TorrentStopCondition { get; set; }
            public int UpLimit { get; set; }
            public UploadChokingAlgorithm UploadChokingAlgorithm { get; set; }
            public UploadSlotsBehavior UploadSlotsBehavior { get; set; }
            public bool Upnp { get; set; }
            public int UpnpLeaseDuration { get; set; }
            public bool UseCategoryPathsInManualMode { get; set; }
            public bool UseHttps { get; set; }
            public bool IgnoreSslErrors { get; set; }
            public bool UseSubcategories { get; set; }
            public UtpTcpMixedMode UtpTcpMixedMode { get; set; }
            public bool ValidateHttpsTrackerCertificate { get; set; }
            public int? HostnameCacheTtl { get; set; }
            public string WebUiAddress { get; set; } = string.Empty;
            public string WebUiApiKey { get; set; } = string.Empty;
            public int WebUiBanDuration { get; set; }
            public bool WebUiClickjackingProtectionEnabled { get; set; }
            public bool WebUiCsrfProtectionEnabled { get; set; }
            public string WebUiCustomHttpHeaders { get; set; } = string.Empty;
            public string WebUiDomainList { get; set; } = string.Empty;
            public bool WebUiHostHeaderValidationEnabled { get; set; }
            public string WebUiHttpsCertPath { get; set; } = string.Empty;
            public string WebUiHttpsKeyPath { get; set; } = string.Empty;
            public int WebUiMaxAuthFailCount { get; set; }
            public int WebUiPort { get; set; }
            public string WebUiReverseProxiesList { get; set; } = string.Empty;
            public bool WebUiReverseProxyEnabled { get; set; }
            public bool WebUiSecureCookieEnabled { get; set; }
            public int WebUiSessionTimeout { get; set; }
            public bool WebUiUpnp { get; set; }
            public bool WebUiUseCustomHttpHeadersEnabled { get; set; }
            public string WebUiUsername { get; set; } = string.Empty;
            public string WebUiPassword { get; set; } = string.Empty;
            public bool ConfirmTorrentDeletion { get; set; }
            public bool ConfirmTorrentRecheck { get; set; }
            public bool StatusBarExternalIp { get; set; }
        }
    }
}
