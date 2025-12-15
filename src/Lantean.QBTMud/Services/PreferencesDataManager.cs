namespace Lantean.QBTMud.Services
{
    public class PreferencesDataManager : IPreferencesDataManager
    {
        public QBitTorrentClient.Models.UpdatePreferences MergePreferences(
            QBitTorrentClient.Models.UpdatePreferences? original,
            QBitTorrentClient.Models.UpdatePreferences changed)
        {
            if (original is null)
            {
                original = new QBitTorrentClient.Models.UpdatePreferences
                {
                    AddToTopOfQueue = changed.AddToTopOfQueue,
                    AddStoppedEnabled = changed.AddStoppedEnabled,
                    AddTrackers = changed.AddTrackers,
                    AddTrackersEnabled = changed.AddTrackersEnabled,
                    AddTrackersFromUrlEnabled = changed.AddTrackersFromUrlEnabled,
                    AddTrackersUrl = changed.AddTrackersUrl,
                    AddTrackersUrlList = changed.AddTrackersUrlList,
                    AltDlLimit = changed.AltDlLimit,
                    AltUpLimit = changed.AltUpLimit,
                    AlternativeWebuiEnabled = changed.AlternativeWebuiEnabled,
                    AlternativeWebuiPath = changed.AlternativeWebuiPath,
                    AnnounceIp = changed.AnnounceIp,
                    AnnouncePort = changed.AnnouncePort,
                    AnnounceToAllTiers = changed.AnnounceToAllTiers,
                    AnnounceToAllTrackers = changed.AnnounceToAllTrackers,
                    AnonymousMode = changed.AnonymousMode,
                    AppInstanceName = changed.AppInstanceName,
                    AsyncIoThreads = changed.AsyncIoThreads,
                    AutoDeleteMode = changed.AutoDeleteMode,
                    AutoTmmEnabled = changed.AutoTmmEnabled,
                    AutorunEnabled = changed.AutorunEnabled,
                    AutorunOnTorrentAddedEnabled = changed.AutorunOnTorrentAddedEnabled,
                    AutorunOnTorrentAddedProgram = changed.AutorunOnTorrentAddedProgram,
                    AutorunProgram = changed.AutorunProgram,
                    DeleteTorrentContentFiles = changed.DeleteTorrentContentFiles,
                    BannedIPs = changed.BannedIPs,
                    BdecodeDepthLimit = changed.BdecodeDepthLimit,
                    BdecodeTokenLimit = changed.BdecodeTokenLimit,
                    BittorrentProtocol = changed.BittorrentProtocol,
                    BlockPeersOnPrivilegedPorts = changed.BlockPeersOnPrivilegedPorts,
                    BypassAuthSubnetWhitelist = changed.BypassAuthSubnetWhitelist,
                    BypassAuthSubnetWhitelistEnabled = changed.BypassAuthSubnetWhitelistEnabled,
                    BypassLocalAuth = changed.BypassLocalAuth,
                    CategoryChangedTmmEnabled = changed.CategoryChangedTmmEnabled,
                    CheckingMemoryUse = changed.CheckingMemoryUse,
                    ConnectionSpeed = changed.ConnectionSpeed,
                    CurrentInterfaceAddress = changed.CurrentInterfaceAddress,
                    CurrentInterfaceName = changed.CurrentInterfaceName,
                    CurrentNetworkInterface = changed.CurrentNetworkInterface,
                    Dht = changed.Dht,
                    DhtBootstrapNodes = changed.DhtBootstrapNodes,
                    DiskCache = changed.DiskCache,
                    DiskCacheTtl = changed.DiskCacheTtl,
                    DiskIoReadMode = changed.DiskIoReadMode,
                    DiskIoType = changed.DiskIoType,
                    DiskIoWriteMode = changed.DiskIoWriteMode,
                    DiskQueueSize = changed.DiskQueueSize,
                    DlLimit = changed.DlLimit,
                    DontCountSlowTorrents = changed.DontCountSlowTorrents,
                    DyndnsDomain = changed.DyndnsDomain,
                    DyndnsEnabled = changed.DyndnsEnabled,
                    DyndnsPassword = changed.DyndnsPassword,
                    DyndnsService = changed.DyndnsService,
                    DyndnsUsername = changed.DyndnsUsername,
                    EmbeddedTrackerPort = changed.EmbeddedTrackerPort,
                    EmbeddedTrackerPortForwarding = changed.EmbeddedTrackerPortForwarding,
                    EnableCoalesceReadWrite = changed.EnableCoalesceReadWrite,
                    EnableEmbeddedTracker = changed.EnableEmbeddedTracker,
                    EnableMultiConnectionsFromSameIp = changed.EnableMultiConnectionsFromSameIp,
                    EnablePieceExtentAffinity = changed.EnablePieceExtentAffinity,
                    EnableUploadSuggestions = changed.EnableUploadSuggestions,
                    Encryption = changed.Encryption,
                    ExcludedFileNames = changed.ExcludedFileNames,
                    ExcludedFileNamesEnabled = changed.ExcludedFileNamesEnabled,
                    ExportDir = changed.ExportDir,
                    ExportDirFin = changed.ExportDirFin,
                    FileLogAge = changed.FileLogAge,
                    FileLogAgeType = changed.FileLogAgeType,
                    FileLogBackupEnabled = changed.FileLogBackupEnabled,
                    FileLogDeleteOld = changed.FileLogDeleteOld,
                    FileLogEnabled = changed.FileLogEnabled,
                    FileLogMaxSize = changed.FileLogMaxSize,
                    FileLogPath = changed.FileLogPath,
                    FilePoolSize = changed.FilePoolSize,
                    HashingThreads = changed.HashingThreads,
                    I2pAddress = changed.I2pAddress,
                    I2pEnabled = changed.I2pEnabled,
                    I2pInboundLength = changed.I2pInboundLength,
                    I2pInboundQuantity = changed.I2pInboundQuantity,
                    I2pMixedMode = changed.I2pMixedMode,
                    I2pOutboundLength = changed.I2pOutboundLength,
                    I2pOutboundQuantity = changed.I2pOutboundQuantity,
                    I2pPort = changed.I2pPort,
                    IdnSupportEnabled = changed.IdnSupportEnabled,
                    IncompleteFilesExt = changed.IncompleteFilesExt,
                    UseUnwantedFolder = changed.UseUnwantedFolder,
                    IpFilterEnabled = changed.IpFilterEnabled,
                    IpFilterPath = changed.IpFilterPath,
                    IpFilterTrackers = changed.IpFilterTrackers,
                    LimitLanPeers = changed.LimitLanPeers,
                    LimitTcpOverhead = changed.LimitTcpOverhead,
                    LimitUtpRate = changed.LimitUtpRate,
                    ListenPort = changed.ListenPort,
                    SslEnabled = changed.SslEnabled,
                    SslListenPort = changed.SslListenPort,
                    Locale = changed.Locale,
                    Lsd = changed.Lsd,
                    MailNotificationAuthEnabled = changed.MailNotificationAuthEnabled,
                    MailNotificationEmail = changed.MailNotificationEmail,
                    MailNotificationEnabled = changed.MailNotificationEnabled,
                    MailNotificationPassword = changed.MailNotificationPassword,
                    MailNotificationSender = changed.MailNotificationSender,
                    MailNotificationSmtp = changed.MailNotificationSmtp,
                    MailNotificationSslEnabled = changed.MailNotificationSslEnabled,
                    MailNotificationUsername = changed.MailNotificationUsername,
                    MarkOfTheWeb = changed.MarkOfTheWeb,
                    MaxActiveCheckingTorrents = changed.MaxActiveCheckingTorrents,
                    MaxActiveDownloads = changed.MaxActiveDownloads,
                    MaxActiveTorrents = changed.MaxActiveTorrents,
                    MaxActiveUploads = changed.MaxActiveUploads,
                    MaxConcurrentHttpAnnounces = changed.MaxConcurrentHttpAnnounces,
                    MaxConnec = changed.MaxConnec,
                    MaxConnecPerTorrent = changed.MaxConnecPerTorrent,
                    MaxInactiveSeedingTime = changed.MaxInactiveSeedingTime,
                    MaxInactiveSeedingTimeEnabled = changed.MaxInactiveSeedingTimeEnabled,
                    MaxRatio = changed.MaxRatio,
                    MaxRatioAct = changed.MaxRatioAct,
                    MaxRatioEnabled = changed.MaxRatioEnabled,
                    MaxSeedingTime = changed.MaxSeedingTime,
                    MaxSeedingTimeEnabled = changed.MaxSeedingTimeEnabled,
                    MaxUploads = changed.MaxUploads,
                    MaxUploadsPerTorrent = changed.MaxUploadsPerTorrent,
                    MemoryWorkingSetLimit = changed.MemoryWorkingSetLimit,
                    MergeTrackers = changed.MergeTrackers,
                    OutgoingPortsMax = changed.OutgoingPortsMax,
                    OutgoingPortsMin = changed.OutgoingPortsMin,
                    PeerTos = changed.PeerTos,
                    PeerTurnover = changed.PeerTurnover,
                    PeerTurnoverCutoff = changed.PeerTurnoverCutoff,
                    PeerTurnoverInterval = changed.PeerTurnoverInterval,
                    PerformanceWarning = changed.PerformanceWarning,
                    Pex = changed.Pex,
                    PreallocateAll = changed.PreallocateAll,
                    ProxyAuthEnabled = changed.ProxyAuthEnabled,
                    ProxyBittorrent = changed.ProxyBittorrent,
                    ProxyHostnameLookup = changed.ProxyHostnameLookup,
                    ProxyIp = changed.ProxyIp,
                    ProxyMisc = changed.ProxyMisc,
                    ProxyPassword = changed.ProxyPassword,
                    ProxyPeerConnections = changed.ProxyPeerConnections,
                    ProxyPort = changed.ProxyPort,
                    ProxyRss = changed.ProxyRss,
                    ProxyType = changed.ProxyType,
                    ProxyUsername = changed.ProxyUsername,
                    PythonExecutablePath = changed.PythonExecutablePath,
                    QueueingEnabled = changed.QueueingEnabled,
                    RandomPort = changed.RandomPort,
                    ReannounceWhenAddressChanged = changed.ReannounceWhenAddressChanged,
                    RecheckCompletedTorrents = changed.RecheckCompletedTorrents,
                    RefreshInterval = changed.RefreshInterval,
                    RequestQueueSize = changed.RequestQueueSize,
                    ResolvePeerCountries = changed.ResolvePeerCountries,
                    ResumeDataStorageType = changed.ResumeDataStorageType,
                    RssAutoDownloadingEnabled = changed.RssAutoDownloadingEnabled,
                    RssDownloadRepackProperEpisodes = changed.RssDownloadRepackProperEpisodes,
                    RssFetchDelay = changed.RssFetchDelay,
                    RssMaxArticlesPerFeed = changed.RssMaxArticlesPerFeed,
                    RssProcessingEnabled = changed.RssProcessingEnabled,
                    RssRefreshInterval = changed.RssRefreshInterval,
                    RssSmartEpisodeFilters = changed.RssSmartEpisodeFilters,
                    SavePath = changed.SavePath,
                    SavePathChangedTmmEnabled = changed.SavePathChangedTmmEnabled,
                    SaveResumeDataInterval = changed.SaveResumeDataInterval,
                    SaveStatisticsInterval = changed.SaveStatisticsInterval,
                    ScanDirs = changed.ScanDirs,
                    ScheduleFromHour = changed.ScheduleFromHour,
                    ScheduleFromMin = changed.ScheduleFromMin,
                    ScheduleToHour = changed.ScheduleToHour,
                    ScheduleToMin = changed.ScheduleToMin,
                    SchedulerDays = changed.SchedulerDays,
                    SchedulerEnabled = changed.SchedulerEnabled,
                    SendBufferLowWatermark = changed.SendBufferLowWatermark,
                    SendBufferWatermark = changed.SendBufferWatermark,
                    SendBufferWatermarkFactor = changed.SendBufferWatermarkFactor,
                    SlowTorrentDlRateThreshold = changed.SlowTorrentDlRateThreshold,
                    SlowTorrentInactiveTimer = changed.SlowTorrentInactiveTimer,
                    SlowTorrentUlRateThreshold = changed.SlowTorrentUlRateThreshold,
                    SocketBacklogSize = changed.SocketBacklogSize,
                    SocketReceiveBufferSize = changed.SocketReceiveBufferSize,
                    SocketSendBufferSize = changed.SocketSendBufferSize,
                    SsrfMitigation = changed.SsrfMitigation,
                    StopTrackerTimeout = changed.StopTrackerTimeout,
                    TempPath = changed.TempPath,
                    TempPathEnabled = changed.TempPathEnabled,
                    TorrentChangedTmmEnabled = changed.TorrentChangedTmmEnabled,
                    TorrentContentLayout = changed.TorrentContentLayout,
                    TorrentContentRemoveOption = changed.TorrentContentRemoveOption,
                    TorrentFileSizeLimit = changed.TorrentFileSizeLimit,
                    TorrentStopCondition = changed.TorrentStopCondition,
                    UpLimit = changed.UpLimit,
                    UploadChokingAlgorithm = changed.UploadChokingAlgorithm,
                    UploadSlotsBehavior = changed.UploadSlotsBehavior,
                    Upnp = changed.Upnp,
                    UpnpLeaseDuration = changed.UpnpLeaseDuration,
                    UseCategoryPathsInManualMode = changed.UseCategoryPathsInManualMode,
                    UseHttps = changed.UseHttps,
                    IgnoreSslErrors = changed.IgnoreSslErrors,
                    UseSubcategories = changed.UseSubcategories,
                    UtpTcpMixedMode = changed.UtpTcpMixedMode,
                    ValidateHttpsTrackerCertificate = changed.ValidateHttpsTrackerCertificate,
                    WebUiAddress = changed.WebUiAddress,
                    WebUiApiKey = changed.WebUiApiKey,
                    WebUiBanDuration = changed.WebUiBanDuration,
                    WebUiClickjackingProtectionEnabled = changed.WebUiClickjackingProtectionEnabled,
                    WebUiCsrfProtectionEnabled = changed.WebUiCsrfProtectionEnabled,
                    WebUiCustomHttpHeaders = changed.WebUiCustomHttpHeaders,
                    WebUiDomainList = changed.WebUiDomainList,
                    WebUiHostHeaderValidationEnabled = changed.WebUiHostHeaderValidationEnabled,
                    WebUiHttpsCertPath = changed.WebUiHttpsCertPath,
                    WebUiHttpsKeyPath = changed.WebUiHttpsKeyPath,
                    WebUiMaxAuthFailCount = changed.WebUiMaxAuthFailCount,
                    WebUiPort = changed.WebUiPort,
                    WebUiReverseProxiesList = changed.WebUiReverseProxiesList,
                    WebUiReverseProxyEnabled = changed.WebUiReverseProxyEnabled,
                    WebUiSecureCookieEnabled = changed.WebUiSecureCookieEnabled,
                    WebUiSessionTimeout = changed.WebUiSessionTimeout,
                    WebUiUpnp = changed.WebUiUpnp,
                    WebUiUseCustomHttpHeadersEnabled = changed.WebUiUseCustomHttpHeadersEnabled,
                    WebUiUsername = changed.WebUiUsername,
                    WebUiPassword = changed.WebUiPassword,
                    ConfirmTorrentDeletion = changed.ConfirmTorrentDeletion,
                    ConfirmTorrentRecheck = changed.ConfirmTorrentRecheck,
                    StatusBarExternalIp = changed.StatusBarExternalIp
                };

                ApplyMutuallyExclusiveLimits(original, changed);

                return original;
            }

            original.AddToTopOfQueue = changed.AddToTopOfQueue ?? original.AddToTopOfQueue;
            original.AddStoppedEnabled = changed.AddStoppedEnabled ?? original.AddStoppedEnabled;
            original.AddTrackers = changed.AddTrackers ?? original.AddTrackers;
            original.AddTrackersEnabled = changed.AddTrackersEnabled ?? original.AddTrackersEnabled;
            original.AddTrackersFromUrlEnabled = changed.AddTrackersFromUrlEnabled ?? original.AddTrackersFromUrlEnabled;
            original.AddTrackersUrl = changed.AddTrackersUrl ?? original.AddTrackersUrl;
            original.AddTrackersUrlList = changed.AddTrackersUrlList ?? original.AddTrackersUrlList;
            original.AltDlLimit = changed.AltDlLimit ?? original.AltDlLimit;
            original.AltUpLimit = changed.AltUpLimit ?? original.AltUpLimit;
            original.AlternativeWebuiEnabled = changed.AlternativeWebuiEnabled ?? original.AlternativeWebuiEnabled;
            original.AlternativeWebuiPath = changed.AlternativeWebuiPath ?? original.AlternativeWebuiPath;
            original.AnnounceIp = changed.AnnounceIp ?? original.AnnounceIp;
            original.AnnouncePort = changed.AnnouncePort ?? original.AnnouncePort;
            original.AnnounceToAllTiers = changed.AnnounceToAllTiers ?? original.AnnounceToAllTiers;
            original.AnnounceToAllTrackers = changed.AnnounceToAllTrackers ?? original.AnnounceToAllTrackers;
            original.AnonymousMode = changed.AnonymousMode ?? original.AnonymousMode;
            original.AppInstanceName = changed.AppInstanceName ?? original.AppInstanceName;
            original.AsyncIoThreads = changed.AsyncIoThreads ?? original.AsyncIoThreads;
            original.AutoDeleteMode = changed.AutoDeleteMode ?? original.AutoDeleteMode;
            original.AutoTmmEnabled = changed.AutoTmmEnabled ?? original.AutoTmmEnabled;
            original.AutorunEnabled = changed.AutorunEnabled ?? original.AutorunEnabled;
            original.AutorunOnTorrentAddedEnabled = changed.AutorunOnTorrentAddedEnabled ?? original.AutorunOnTorrentAddedEnabled;
            original.AutorunOnTorrentAddedProgram = changed.AutorunOnTorrentAddedProgram ?? original.AutorunOnTorrentAddedProgram;
            original.AutorunProgram = changed.AutorunProgram ?? original.AutorunProgram;
            original.DeleteTorrentContentFiles = changed.DeleteTorrentContentFiles ?? original.DeleteTorrentContentFiles;
            original.BannedIPs = changed.BannedIPs ?? original.BannedIPs;
            original.BdecodeDepthLimit = changed.BdecodeDepthLimit ?? original.BdecodeDepthLimit;
            original.BdecodeTokenLimit = changed.BdecodeTokenLimit ?? original.BdecodeTokenLimit;
            original.BittorrentProtocol = changed.BittorrentProtocol ?? original.BittorrentProtocol;
            original.BlockPeersOnPrivilegedPorts = changed.BlockPeersOnPrivilegedPorts ?? original.BlockPeersOnPrivilegedPorts;
            original.BypassAuthSubnetWhitelist = changed.BypassAuthSubnetWhitelist ?? original.BypassAuthSubnetWhitelist;
            original.BypassAuthSubnetWhitelistEnabled = changed.BypassAuthSubnetWhitelistEnabled ?? original.BypassAuthSubnetWhitelistEnabled;
            original.BypassLocalAuth = changed.BypassLocalAuth ?? original.BypassLocalAuth;
            original.CategoryChangedTmmEnabled = changed.CategoryChangedTmmEnabled ?? original.CategoryChangedTmmEnabled;
            original.CheckingMemoryUse = changed.CheckingMemoryUse ?? original.CheckingMemoryUse;
            original.ConnectionSpeed = changed.ConnectionSpeed ?? original.ConnectionSpeed;
            original.CurrentInterfaceAddress = changed.CurrentInterfaceAddress ?? original.CurrentInterfaceAddress;
            original.CurrentInterfaceName = changed.CurrentInterfaceName ?? original.CurrentInterfaceName;
            original.CurrentNetworkInterface = changed.CurrentNetworkInterface ?? original.CurrentNetworkInterface;
            original.Dht = changed.Dht ?? original.Dht;
            original.DhtBootstrapNodes = changed.DhtBootstrapNodes ?? original.DhtBootstrapNodes;
            original.DiskCache = changed.DiskCache ?? original.DiskCache;
            original.DiskCacheTtl = changed.DiskCacheTtl ?? original.DiskCacheTtl;
            original.DiskIoReadMode = changed.DiskIoReadMode ?? original.DiskIoReadMode;
            original.DiskIoType = changed.DiskIoType ?? original.DiskIoType;
            original.DiskIoWriteMode = changed.DiskIoWriteMode ?? original.DiskIoWriteMode;
            original.DiskQueueSize = changed.DiskQueueSize ?? original.DiskQueueSize;
            original.DlLimit = changed.DlLimit ?? original.DlLimit;
            original.DontCountSlowTorrents = changed.DontCountSlowTorrents ?? original.DontCountSlowTorrents;
            original.DyndnsDomain = changed.DyndnsDomain ?? original.DyndnsDomain;
            original.DyndnsEnabled = changed.DyndnsEnabled ?? original.DyndnsEnabled;
            original.DyndnsPassword = changed.DyndnsPassword ?? original.DyndnsPassword;
            original.DyndnsService = changed.DyndnsService ?? original.DyndnsService;
            original.DyndnsUsername = changed.DyndnsUsername ?? original.DyndnsUsername;
            original.EmbeddedTrackerPort = changed.EmbeddedTrackerPort ?? original.EmbeddedTrackerPort;
            original.EmbeddedTrackerPortForwarding = changed.EmbeddedTrackerPortForwarding ?? original.EmbeddedTrackerPortForwarding;
            original.EnableCoalesceReadWrite = changed.EnableCoalesceReadWrite ?? original.EnableCoalesceReadWrite;
            original.EnableEmbeddedTracker = changed.EnableEmbeddedTracker ?? original.EnableEmbeddedTracker;
            original.EnableMultiConnectionsFromSameIp = changed.EnableMultiConnectionsFromSameIp ?? original.EnableMultiConnectionsFromSameIp;
            original.EnablePieceExtentAffinity = changed.EnablePieceExtentAffinity ?? original.EnablePieceExtentAffinity;
            original.EnableUploadSuggestions = changed.EnableUploadSuggestions ?? original.EnableUploadSuggestions;
            original.Encryption = changed.Encryption ?? original.Encryption;
            original.ExcludedFileNames = changed.ExcludedFileNames ?? original.ExcludedFileNames;
            original.ExcludedFileNamesEnabled = changed.ExcludedFileNamesEnabled ?? original.ExcludedFileNamesEnabled;
            original.ExportDir = changed.ExportDir ?? original.ExportDir;
            original.ExportDirFin = changed.ExportDirFin ?? original.ExportDirFin;
            original.FileLogAge = changed.FileLogAge ?? original.FileLogAge;
            original.FileLogAgeType = changed.FileLogAgeType ?? original.FileLogAgeType;
            original.FileLogBackupEnabled = changed.FileLogBackupEnabled ?? original.FileLogBackupEnabled;
            original.FileLogDeleteOld = changed.FileLogDeleteOld ?? original.FileLogDeleteOld;
            original.FileLogEnabled = changed.FileLogEnabled ?? original.FileLogEnabled;
            original.FileLogMaxSize = changed.FileLogMaxSize ?? original.FileLogMaxSize;
            original.FileLogPath = changed.FileLogPath ?? original.FileLogPath;
            original.FilePoolSize = changed.FilePoolSize ?? original.FilePoolSize;
            original.HashingThreads = changed.HashingThreads ?? original.HashingThreads;
            original.I2pAddress = changed.I2pAddress ?? original.I2pAddress;
            original.I2pEnabled = changed.I2pEnabled ?? original.I2pEnabled;
            original.I2pInboundLength = changed.I2pInboundLength ?? original.I2pInboundLength;
            original.I2pInboundQuantity = changed.I2pInboundQuantity ?? original.I2pInboundQuantity;
            original.I2pMixedMode = changed.I2pMixedMode ?? original.I2pMixedMode;
            original.I2pOutboundLength = changed.I2pOutboundLength ?? original.I2pOutboundLength;
            original.I2pOutboundQuantity = changed.I2pOutboundQuantity ?? original.I2pOutboundQuantity;
            original.I2pPort = changed.I2pPort ?? original.I2pPort;
            original.IdnSupportEnabled = changed.IdnSupportEnabled ?? original.IdnSupportEnabled;
            original.IncompleteFilesExt = changed.IncompleteFilesExt ?? original.IncompleteFilesExt;
            original.UseUnwantedFolder = changed.UseUnwantedFolder ?? original.UseUnwantedFolder;
            original.IpFilterEnabled = changed.IpFilterEnabled ?? original.IpFilterEnabled;
            original.IpFilterPath = changed.IpFilterPath ?? original.IpFilterPath;
            original.IpFilterTrackers = changed.IpFilterTrackers ?? original.IpFilterTrackers;
            original.LimitLanPeers = changed.LimitLanPeers ?? original.LimitLanPeers;
            original.LimitTcpOverhead = changed.LimitTcpOverhead ?? original.LimitTcpOverhead;
            original.LimitUtpRate = changed.LimitUtpRate ?? original.LimitUtpRate;
            original.ListenPort = changed.ListenPort ?? original.ListenPort;
            original.SslEnabled = changed.SslEnabled ?? original.SslEnabled;
            original.SslListenPort = changed.SslListenPort ?? original.SslListenPort;
            original.Locale = changed.Locale ?? original.Locale;
            original.Lsd = changed.Lsd ?? original.Lsd;
            original.MailNotificationAuthEnabled = changed.MailNotificationAuthEnabled ?? original.MailNotificationAuthEnabled;
            original.MailNotificationEmail = changed.MailNotificationEmail ?? original.MailNotificationEmail;
            original.MailNotificationEnabled = changed.MailNotificationEnabled ?? original.MailNotificationEnabled;
            original.MailNotificationPassword = changed.MailNotificationPassword ?? original.MailNotificationPassword;
            original.MailNotificationSender = changed.MailNotificationSender ?? original.MailNotificationSender;
            original.MailNotificationSmtp = changed.MailNotificationSmtp ?? original.MailNotificationSmtp;
            original.MailNotificationSslEnabled = changed.MailNotificationSslEnabled ?? original.MailNotificationSslEnabled;
            original.MailNotificationUsername = changed.MailNotificationUsername ?? original.MailNotificationUsername;
            original.MarkOfTheWeb = changed.MarkOfTheWeb ?? original.MarkOfTheWeb;
            original.MaxActiveCheckingTorrents = changed.MaxActiveCheckingTorrents ?? original.MaxActiveCheckingTorrents;
            original.MaxActiveDownloads = changed.MaxActiveDownloads ?? original.MaxActiveDownloads;
            original.MaxActiveTorrents = changed.MaxActiveTorrents ?? original.MaxActiveTorrents;
            original.MaxActiveUploads = changed.MaxActiveUploads ?? original.MaxActiveUploads;
            original.MaxConcurrentHttpAnnounces = changed.MaxConcurrentHttpAnnounces ?? original.MaxConcurrentHttpAnnounces;
            original.MaxConnec = changed.MaxConnec ?? original.MaxConnec;
            original.MaxConnecPerTorrent = changed.MaxConnecPerTorrent ?? original.MaxConnecPerTorrent;
            original.MaxRatioAct = changed.MaxRatioAct ?? original.MaxRatioAct;
            original.MaxUploads = changed.MaxUploads ?? original.MaxUploads;
            original.MaxUploadsPerTorrent = changed.MaxUploadsPerTorrent ?? original.MaxUploadsPerTorrent;
            original.MemoryWorkingSetLimit = changed.MemoryWorkingSetLimit ?? original.MemoryWorkingSetLimit;
            original.MergeTrackers = changed.MergeTrackers ?? original.MergeTrackers;
            original.OutgoingPortsMax = changed.OutgoingPortsMax ?? original.OutgoingPortsMax;
            original.OutgoingPortsMin = changed.OutgoingPortsMin ?? original.OutgoingPortsMin;
            original.PeerTos = changed.PeerTos ?? original.PeerTos;
            original.PeerTurnover = changed.PeerTurnover ?? original.PeerTurnover;
            original.PeerTurnoverCutoff = changed.PeerTurnoverCutoff ?? original.PeerTurnoverCutoff;
            original.PeerTurnoverInterval = changed.PeerTurnoverInterval ?? original.PeerTurnoverInterval;
            original.PerformanceWarning = changed.PerformanceWarning ?? original.PerformanceWarning;
            original.Pex = changed.Pex ?? original.Pex;
            original.PreallocateAll = changed.PreallocateAll ?? original.PreallocateAll;
            original.ProxyAuthEnabled = changed.ProxyAuthEnabled ?? original.ProxyAuthEnabled;
            original.ProxyBittorrent = changed.ProxyBittorrent ?? original.ProxyBittorrent;
            original.ProxyHostnameLookup = changed.ProxyHostnameLookup ?? original.ProxyHostnameLookup;
            original.ProxyIp = changed.ProxyIp ?? original.ProxyIp;
            original.ProxyMisc = changed.ProxyMisc ?? original.ProxyMisc;
            original.ProxyPassword = changed.ProxyPassword ?? original.ProxyPassword;
            original.ProxyPeerConnections = changed.ProxyPeerConnections ?? original.ProxyPeerConnections;
            original.ProxyPort = changed.ProxyPort ?? original.ProxyPort;
            original.ProxyRss = changed.ProxyRss ?? original.ProxyRss;
            original.ProxyType = changed.ProxyType ?? original.ProxyType;
            original.ProxyUsername = changed.ProxyUsername ?? original.ProxyUsername;
            original.PythonExecutablePath = changed.PythonExecutablePath ?? original.PythonExecutablePath;
            original.QueueingEnabled = changed.QueueingEnabled ?? original.QueueingEnabled;
            original.RandomPort = changed.RandomPort ?? original.RandomPort;
            original.ReannounceWhenAddressChanged = changed.ReannounceWhenAddressChanged ?? original.ReannounceWhenAddressChanged;
            original.RecheckCompletedTorrents = changed.RecheckCompletedTorrents ?? original.RecheckCompletedTorrents;
            original.RefreshInterval = changed.RefreshInterval ?? original.RefreshInterval;
            original.RequestQueueSize = changed.RequestQueueSize ?? original.RequestQueueSize;
            original.ResolvePeerCountries = changed.ResolvePeerCountries ?? original.ResolvePeerCountries;
            original.ResumeDataStorageType = changed.ResumeDataStorageType ?? original.ResumeDataStorageType;
            original.RssAutoDownloadingEnabled = changed.RssAutoDownloadingEnabled ?? original.RssAutoDownloadingEnabled;
            original.RssDownloadRepackProperEpisodes = changed.RssDownloadRepackProperEpisodes ?? original.RssDownloadRepackProperEpisodes;
            original.RssFetchDelay = changed.RssFetchDelay ?? original.RssFetchDelay;
            original.RssMaxArticlesPerFeed = changed.RssMaxArticlesPerFeed ?? original.RssMaxArticlesPerFeed;
            original.RssProcessingEnabled = changed.RssProcessingEnabled ?? original.RssProcessingEnabled;
            original.RssRefreshInterval = changed.RssRefreshInterval ?? original.RssRefreshInterval;
            original.RssSmartEpisodeFilters = changed.RssSmartEpisodeFilters ?? original.RssSmartEpisodeFilters;
            original.SavePath = changed.SavePath ?? original.SavePath;
            original.SavePathChangedTmmEnabled = changed.SavePathChangedTmmEnabled ?? original.SavePathChangedTmmEnabled;
            original.SaveResumeDataInterval = changed.SaveResumeDataInterval ?? original.SaveResumeDataInterval;
            original.SaveStatisticsInterval = changed.SaveStatisticsInterval ?? original.SaveStatisticsInterval;
            original.ScanDirs = changed.ScanDirs ?? original.ScanDirs;
            original.ScheduleFromHour = changed.ScheduleFromHour ?? original.ScheduleFromHour;
            original.ScheduleFromMin = changed.ScheduleFromMin ?? original.ScheduleFromMin;
            original.ScheduleToHour = changed.ScheduleToHour ?? original.ScheduleToHour;
            original.ScheduleToMin = changed.ScheduleToMin ?? original.ScheduleToMin;
            original.SchedulerDays = changed.SchedulerDays ?? original.SchedulerDays;
            original.SchedulerEnabled = changed.SchedulerEnabled ?? original.SchedulerEnabled;
            original.SendBufferLowWatermark = changed.SendBufferLowWatermark ?? original.SendBufferLowWatermark;
            original.SendBufferWatermark = changed.SendBufferWatermark ?? original.SendBufferWatermark;
            original.SendBufferWatermarkFactor = changed.SendBufferWatermarkFactor ?? original.SendBufferWatermarkFactor;
            original.SlowTorrentDlRateThreshold = changed.SlowTorrentDlRateThreshold ?? original.SlowTorrentDlRateThreshold;
            original.SlowTorrentInactiveTimer = changed.SlowTorrentInactiveTimer ?? original.SlowTorrentInactiveTimer;
            original.SlowTorrentUlRateThreshold = changed.SlowTorrentUlRateThreshold ?? original.SlowTorrentUlRateThreshold;
            original.SocketBacklogSize = changed.SocketBacklogSize ?? original.SocketBacklogSize;
            original.SocketReceiveBufferSize = changed.SocketReceiveBufferSize ?? original.SocketReceiveBufferSize;
            original.SocketSendBufferSize = changed.SocketSendBufferSize ?? original.SocketSendBufferSize;
            original.SsrfMitigation = changed.SsrfMitigation ?? original.SsrfMitigation;
            original.StopTrackerTimeout = changed.StopTrackerTimeout ?? original.StopTrackerTimeout;
            original.TempPath = changed.TempPath ?? original.TempPath;
            original.TempPathEnabled = changed.TempPathEnabled ?? original.TempPathEnabled;
            original.TorrentChangedTmmEnabled = changed.TorrentChangedTmmEnabled ?? original.TorrentChangedTmmEnabled;
            original.TorrentContentLayout = changed.TorrentContentLayout ?? original.TorrentContentLayout;
            original.TorrentContentRemoveOption = changed.TorrentContentRemoveOption ?? original.TorrentContentRemoveOption;
            original.TorrentFileSizeLimit = changed.TorrentFileSizeLimit ?? original.TorrentFileSizeLimit;
            original.TorrentStopCondition = changed.TorrentStopCondition ?? original.TorrentStopCondition;
            original.UpLimit = changed.UpLimit ?? original.UpLimit;
            original.UploadChokingAlgorithm = changed.UploadChokingAlgorithm ?? original.UploadChokingAlgorithm;
            original.UploadSlotsBehavior = changed.UploadSlotsBehavior ?? original.UploadSlotsBehavior;
            original.Upnp = changed.Upnp ?? original.Upnp;
            original.UpnpLeaseDuration = changed.UpnpLeaseDuration ?? original.UpnpLeaseDuration;
            original.UseCategoryPathsInManualMode = changed.UseCategoryPathsInManualMode ?? original.UseCategoryPathsInManualMode;
            original.UseHttps = changed.UseHttps ?? original.UseHttps;
            original.IgnoreSslErrors = changed.IgnoreSslErrors ?? original.IgnoreSslErrors;
            original.UseSubcategories = changed.UseSubcategories ?? original.UseSubcategories;
            original.UtpTcpMixedMode = changed.UtpTcpMixedMode ?? original.UtpTcpMixedMode;
            original.ValidateHttpsTrackerCertificate = changed.ValidateHttpsTrackerCertificate ?? original.ValidateHttpsTrackerCertificate;
            original.WebUiAddress = changed.WebUiAddress ?? original.WebUiAddress;
            original.WebUiApiKey = changed.WebUiApiKey ?? original.WebUiApiKey;
            original.WebUiBanDuration = changed.WebUiBanDuration ?? original.WebUiBanDuration;
            original.WebUiClickjackingProtectionEnabled = changed.WebUiClickjackingProtectionEnabled ?? original.WebUiClickjackingProtectionEnabled;
            original.WebUiCsrfProtectionEnabled = changed.WebUiCsrfProtectionEnabled ?? original.WebUiCsrfProtectionEnabled;
            original.WebUiCustomHttpHeaders = changed.WebUiCustomHttpHeaders ?? original.WebUiCustomHttpHeaders;
            original.WebUiDomainList = changed.WebUiDomainList ?? original.WebUiDomainList;
            original.WebUiHostHeaderValidationEnabled = changed.WebUiHostHeaderValidationEnabled ?? original.WebUiHostHeaderValidationEnabled;
            original.WebUiHttpsCertPath = changed.WebUiHttpsCertPath ?? original.WebUiHttpsCertPath;
            original.WebUiHttpsKeyPath = changed.WebUiHttpsKeyPath ?? original.WebUiHttpsKeyPath;
            original.WebUiMaxAuthFailCount = changed.WebUiMaxAuthFailCount ?? original.WebUiMaxAuthFailCount;
            original.WebUiPort = changed.WebUiPort ?? original.WebUiPort;
            original.WebUiReverseProxiesList = changed.WebUiReverseProxiesList ?? original.WebUiReverseProxiesList;
            original.WebUiReverseProxyEnabled = changed.WebUiReverseProxyEnabled ?? original.WebUiReverseProxyEnabled;
            original.WebUiSecureCookieEnabled = changed.WebUiSecureCookieEnabled ?? original.WebUiSecureCookieEnabled;
            original.WebUiSessionTimeout = changed.WebUiSessionTimeout ?? original.WebUiSessionTimeout;
            original.WebUiUpnp = changed.WebUiUpnp ?? original.WebUiUpnp;
            original.WebUiUseCustomHttpHeadersEnabled = changed.WebUiUseCustomHttpHeadersEnabled ?? original.WebUiUseCustomHttpHeadersEnabled;
            original.WebUiUsername = changed.WebUiUsername ?? original.WebUiUsername;
            original.WebUiPassword = changed.WebUiPassword ?? original.WebUiPassword;
            original.ConfirmTorrentDeletion = changed.ConfirmTorrentDeletion ?? original.ConfirmTorrentDeletion;
            original.ConfirmTorrentRecheck = changed.ConfirmTorrentRecheck ?? original.ConfirmTorrentRecheck;
            original.StatusBarExternalIp = changed.StatusBarExternalIp ?? original.StatusBarExternalIp;

            ApplyMutuallyExclusiveLimits(original, changed);

            return original;
        }

        private static void ApplyMutuallyExclusiveLimits(
            QBitTorrentClient.Models.UpdatePreferences target,
            QBitTorrentClient.Models.UpdatePreferences changed)
        {
            if (changed.MaxRatio.HasValue)
            {
                target.MaxRatio = changed.MaxRatio;
                target.MaxRatioEnabled = null;
            }
            else if (changed.MaxRatioEnabled.HasValue)
            {
                target.MaxRatioEnabled = changed.MaxRatioEnabled;
                target.MaxRatio = null;
            }

            if (changed.MaxSeedingTime.HasValue)
            {
                target.MaxSeedingTime = changed.MaxSeedingTime;
                target.MaxSeedingTimeEnabled = null;
            }
            else if (changed.MaxSeedingTimeEnabled.HasValue)
            {
                target.MaxSeedingTimeEnabled = changed.MaxSeedingTimeEnabled;
                target.MaxSeedingTime = null;
            }

            if (changed.MaxInactiveSeedingTime.HasValue)
            {
                target.MaxInactiveSeedingTime = changed.MaxInactiveSeedingTime;
                target.MaxInactiveSeedingTimeEnabled = null;
            }
            else if (changed.MaxInactiveSeedingTimeEnabled.HasValue)
            {
                target.MaxInactiveSeedingTimeEnabled = changed.MaxInactiveSeedingTimeEnabled;
                target.MaxInactiveSeedingTime = null;
            }
        }
    }
}
