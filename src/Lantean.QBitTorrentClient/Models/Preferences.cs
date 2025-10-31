using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record Preferences
    {
        [JsonConstructor]
        public Preferences(
            bool addToTopOfQueue,
            bool addStoppedEnabled,
            string addTrackers,
            bool addTrackersEnabled,
            int altDlLimit,
            int altUpLimit,
            bool alternativeWebuiEnabled,
            string alternativeWebuiPath,
            string announceIp,
            int announcePort,
            bool announceToAllTiers,
            bool announceToAllTrackers,
            bool anonymousMode,
            string appInstanceName,
            int asyncIoThreads,
            int autoDeleteMode,
            bool autoTmmEnabled,
            bool autorunEnabled,
            bool autorunOnTorrentAddedEnabled,
            string autorunOnTorrentAddedProgram,
            string autorunProgram,
            string bannedIPs,
            int bdecodeDepthLimit,
            int bdecodeTokenLimit,
            int bittorrentProtocol,
            bool blockPeersOnPrivilegedPorts,
            string bypassAuthSubnetWhitelist,
            bool bypassAuthSubnetWhitelistEnabled,
            bool bypassLocalAuth,
            bool categoryChangedTmmEnabled,
            int checkingMemoryUse,
            int connectionSpeed,
            string currentInterfaceAddress,
            string currentInterfaceName,
            string currentNetworkInterface,
            bool dht,
            string dhtBootstrapNodes,
            int diskCache,
            int diskCacheTtl,
            int diskIoReadMode,
            int diskIoType,
            int diskIoWriteMode,
            int diskQueueSize,
            int dlLimit,
            bool dontCountSlowTorrents,
            string dyndnsDomain,
            bool dyndnsEnabled,
            string dyndnsPassword,
            int dyndnsService,
            string dyndnsUsername,
            int embeddedTrackerPort,
            bool embeddedTrackerPortForwarding,
            bool enableCoalesceReadWrite,
            bool enableEmbeddedTracker,
            bool enableMultiConnectionsFromSameIp,
            bool enablePieceExtentAffinity,
            bool enableUploadSuggestions,
            int encryption,
            string excludedFileNames,
            bool excludedFileNamesEnabled,
            string exportDir,
            string exportDirFin,
            int fileLogAge,
            int fileLogAgeType,
            bool fileLogBackupEnabled,
            bool fileLogDeleteOld,
            bool fileLogEnabled,
            int fileLogMaxSize,
            string fileLogPath,
            int filePoolSize,
            int hashingThreads,
            string i2pAddress,
            bool i2pEnabled,
            int i2pInboundLength,
            int i2pInboundQuantity,
            bool i2pMixedMode,
            int i2pOutboundLength,
            int i2pOutboundQuantity,
            int i2pPort,
            bool idnSupportEnabled,
            bool incompleteFilesExt,
            bool useUnwantedFolder,
            bool ipFilterEnabled,
            string ipFilterPath,
            bool ipFilterTrackers,
            bool limitLanPeers,
            bool limitTcpOverhead,
            bool limitUtpRate,
            int listenPort,
            bool sslEnabled,
            int sslListenPort,
            string locale,
            bool lsd,
            bool mailNotificationAuthEnabled,
            string mailNotificationEmail,
            bool mailNotificationEnabled,
            string mailNotificationPassword,
            string mailNotificationSender,
            string mailNotificationSmtp,
            bool mailNotificationSslEnabled,
            string mailNotificationUsername,
            bool markOfTheWeb,
            int maxActiveCheckingTorrents,
            int maxActiveDownloads,
            int maxActiveTorrents,
            int maxActiveUploads,
            int maxConcurrentHttpAnnounces,
            int maxConnec,
            int maxConnecPerTorrent,
            int maxInactiveSeedingTime,
            bool maxInactiveSeedingTimeEnabled,
            float maxRatio,
            int maxRatioAct,
            bool maxRatioEnabled,
            int maxSeedingTime,
            bool maxSeedingTimeEnabled,
            int maxUploads,
            int maxUploadsPerTorrent,
            int memoryWorkingSetLimit,
            bool mergeTrackers,
            int outgoingPortsMax,
            int outgoingPortsMin,
            int peerTos,
            int peerTurnover,
            int peerTurnoverCutoff,
            int peerTurnoverInterval,
            bool performanceWarning,
            bool pex,
            bool preallocateAll,
            bool proxyAuthEnabled,
            bool proxyBittorrent,
            bool proxyHostnameLookup,
            string proxyIp,
            bool proxyMisc,
            string proxyPassword,
            bool proxyPeerConnections,
            int proxyPort,
            bool proxyRss,
            string proxyType,
            string proxyUsername,
            string pythonExecutablePath,
            bool queueingEnabled,
            bool randomPort,
            bool reannounceWhenAddressChanged,
            bool recheckCompletedTorrents,
            int refreshInterval,
            int requestQueueSize,
            bool resolvePeerCountries,
            string resumeDataStorageType,
            bool rssAutoDownloadingEnabled,
            long rssFetchDelay,
            bool rssDownloadRepackProperEpisodes,
            int rssMaxArticlesPerFeed,
            bool rssProcessingEnabled,
            int rssRefreshInterval,
            string rssSmartEpisodeFilters,
            string savePath,
            bool savePathChangedTmmEnabled,
            int saveResumeDataInterval,
            int saveStatisticsInterval,
            Dictionary<string, SaveLocation> scanDirs,
            int scheduleFromHour,
            int scheduleFromMin,
            int scheduleToHour,
            int scheduleToMin,
            int schedulerDays,
            bool schedulerEnabled,
            int sendBufferLowWatermark,
            int sendBufferWatermark,
            int sendBufferWatermarkFactor,
            int slowTorrentDlRateThreshold,
            int slowTorrentInactiveTimer,
            int slowTorrentUlRateThreshold,
            int socketBacklogSize,
            int socketReceiveBufferSize,
            int socketSendBufferSize,
            bool ssrfMitigation,
            int stopTrackerTimeout,
            string tempPath,
            bool tempPathEnabled,
            bool torrentChangedTmmEnabled,
            string torrentContentLayout,
            string torrentContentRemoveOption,
            int torrentFileSizeLimit,
            string torrentStopCondition,
            int upLimit,
            int uploadChokingAlgorithm,
            int uploadSlotsBehavior,
            bool upnp,
            int upnpLeaseDuration,
            bool useCategoryPathsInManualMode,
            bool useHttps,
            bool ignoreSslErrors,
            bool useSubcategories,
            int utpTcpMixedMode,
            bool validateHttpsTrackerCertificate,
            string webUiAddress,
            string webUiApiKey,
            int webUiBanDuration,
            bool webUiClickjackingProtectionEnabled,
            bool webUiCsrfProtectionEnabled,
            string webUiCustomHttpHeaders,
            string webUiDomainList,
            bool webUiHostHeaderValidationEnabled,
            string webUiHttpsCertPath,
            string webUiHttpsKeyPath,
            int webUiMaxAuthFailCount,
            int webUiPort,
            string webUiReverseProxiesList,
            bool webUiReverseProxyEnabled,
            bool webUiSecureCookieEnabled,
            int webUiSessionTimeout,
            bool webUiUpnp,
            bool webUiUseCustomHttpHeadersEnabled,
            string webUiUsername,
            string webUiPassword,
            bool confirmTorrentDeletion,
            bool confirmTorrentRecheck,
            bool statusBarExternalIp
        )
        {
            AddToTopOfQueue = addToTopOfQueue;
            AddStoppedEnabled = addStoppedEnabled;
            AddTrackers = addTrackers;
            AddTrackersEnabled = addTrackersEnabled;
            AltDlLimit = altDlLimit;
            AltUpLimit = altUpLimit;
            AlternativeWebuiEnabled = alternativeWebuiEnabled;
            AlternativeWebuiPath = alternativeWebuiPath;
            AnnounceIp = announceIp;
            AnnouncePort = announcePort;
            AnnounceToAllTiers = announceToAllTiers;
            AnnounceToAllTrackers = announceToAllTrackers;
            AnonymousMode = anonymousMode;
            AppInstanceName = appInstanceName;
            AsyncIoThreads = asyncIoThreads;
            AutoDeleteMode = autoDeleteMode;
            AutoTmmEnabled = autoTmmEnabled;
            AutorunEnabled = autorunEnabled;
            AutorunOnTorrentAddedEnabled = autorunOnTorrentAddedEnabled;
            AutorunOnTorrentAddedProgram = autorunOnTorrentAddedProgram;
            AutorunProgram = autorunProgram;
            BannedIPs = bannedIPs;
            BdecodeDepthLimit = bdecodeDepthLimit;
            BdecodeTokenLimit = bdecodeTokenLimit;
            BittorrentProtocol = bittorrentProtocol;
            BlockPeersOnPrivilegedPorts = blockPeersOnPrivilegedPorts;
            BypassAuthSubnetWhitelist = bypassAuthSubnetWhitelist;
            BypassAuthSubnetWhitelistEnabled = bypassAuthSubnetWhitelistEnabled;
            BypassLocalAuth = bypassLocalAuth;
            CategoryChangedTmmEnabled = categoryChangedTmmEnabled;
            CheckingMemoryUse = checkingMemoryUse;
            ConnectionSpeed = connectionSpeed;
            CurrentInterfaceAddress = currentInterfaceAddress;
            CurrentInterfaceName = currentInterfaceName;
            CurrentNetworkInterface = currentNetworkInterface;
            Dht = dht;
            DhtBootstrapNodes = dhtBootstrapNodes;
            DiskCache = diskCache;
            DiskCacheTtl = diskCacheTtl;
            DiskIoReadMode = diskIoReadMode;
            DiskIoType = diskIoType;
            DiskIoWriteMode = diskIoWriteMode;
            DiskQueueSize = diskQueueSize;
            DlLimit = dlLimit;
            DontCountSlowTorrents = dontCountSlowTorrents;
            DyndnsDomain = dyndnsDomain;
            DyndnsEnabled = dyndnsEnabled;
            DyndnsPassword = dyndnsPassword;
            DyndnsService = dyndnsService;
            DyndnsUsername = dyndnsUsername;
            EmbeddedTrackerPort = embeddedTrackerPort;
            EmbeddedTrackerPortForwarding = embeddedTrackerPortForwarding;
            EnableCoalesceReadWrite = enableCoalesceReadWrite;
            EnableEmbeddedTracker = enableEmbeddedTracker;
            EnableMultiConnectionsFromSameIp = enableMultiConnectionsFromSameIp;
            EnablePieceExtentAffinity = enablePieceExtentAffinity;
            EnableUploadSuggestions = enableUploadSuggestions;
            Encryption = encryption;
            ExcludedFileNames = excludedFileNames;
            ExcludedFileNamesEnabled = excludedFileNamesEnabled;
            ExportDir = exportDir;
            ExportDirFin = exportDirFin;
            FileLogAge = fileLogAge;
            FileLogAgeType = fileLogAgeType;
            FileLogBackupEnabled = fileLogBackupEnabled;
            FileLogDeleteOld = fileLogDeleteOld;
            FileLogEnabled = fileLogEnabled;
            FileLogMaxSize = fileLogMaxSize;
            FileLogPath = fileLogPath;
            FilePoolSize = filePoolSize;
            HashingThreads = hashingThreads;
            I2pAddress = i2pAddress;
            I2pEnabled = i2pEnabled;
            I2pInboundLength = i2pInboundLength;
            I2pInboundQuantity = i2pInboundQuantity;
            I2pMixedMode = i2pMixedMode;
            I2pOutboundLength = i2pOutboundLength;
            I2pOutboundQuantity = i2pOutboundQuantity;
            I2pPort = i2pPort;
            IdnSupportEnabled = idnSupportEnabled;
            IncompleteFilesExt = incompleteFilesExt;
            UseUnwantedFolder = useUnwantedFolder;
            IpFilterEnabled = ipFilterEnabled;
            IpFilterPath = ipFilterPath;
            IpFilterTrackers = ipFilterTrackers;
            LimitLanPeers = limitLanPeers;
            LimitTcpOverhead = limitTcpOverhead;
            LimitUtpRate = limitUtpRate;
            ListenPort = listenPort;
            SslEnabled = sslEnabled;
            SslListenPort = sslListenPort;
            Locale = locale;
            Lsd = lsd;
            MailNotificationAuthEnabled = mailNotificationAuthEnabled;
            MailNotificationEmail = mailNotificationEmail;
            MailNotificationEnabled = mailNotificationEnabled;
            MailNotificationPassword = mailNotificationPassword;
            MailNotificationSender = mailNotificationSender;
            MailNotificationSmtp = mailNotificationSmtp;
            MailNotificationSslEnabled = mailNotificationSslEnabled;
            MailNotificationUsername = mailNotificationUsername;
            MarkOfTheWeb = markOfTheWeb;
            MaxActiveCheckingTorrents = maxActiveCheckingTorrents;
            MaxActiveDownloads = maxActiveDownloads;
            MaxActiveTorrents = maxActiveTorrents;
            MaxActiveUploads = maxActiveUploads;
            MaxConcurrentHttpAnnounces = maxConcurrentHttpAnnounces;
            MaxConnec = maxConnec;
            MaxConnecPerTorrent = maxConnecPerTorrent;
            MaxInactiveSeedingTime = maxInactiveSeedingTime;
            MaxInactiveSeedingTimeEnabled = maxInactiveSeedingTimeEnabled;
            MaxRatio = maxRatio;
            MaxRatioAct = maxRatioAct;
            MaxRatioEnabled = maxRatioEnabled;
            MaxSeedingTime = maxSeedingTime;
            MaxSeedingTimeEnabled = maxSeedingTimeEnabled;
            MaxUploads = maxUploads;
            MaxUploadsPerTorrent = maxUploadsPerTorrent;
            MemoryWorkingSetLimit = memoryWorkingSetLimit;
            MergeTrackers = mergeTrackers;
            OutgoingPortsMax = outgoingPortsMax;
            OutgoingPortsMin = outgoingPortsMin;
            PeerTos = peerTos;
            PeerTurnover = peerTurnover;
            PeerTurnoverCutoff = peerTurnoverCutoff;
            PeerTurnoverInterval = peerTurnoverInterval;
            PerformanceWarning = performanceWarning;
            Pex = pex;
            PreallocateAll = preallocateAll;
            ProxyAuthEnabled = proxyAuthEnabled;
            ProxyBittorrent = proxyBittorrent;
            ProxyHostnameLookup = proxyHostnameLookup;
            ProxyIp = proxyIp;
            ProxyMisc = proxyMisc;
            ProxyPassword = proxyPassword;
            ProxyPeerConnections = proxyPeerConnections;
            ProxyPort = proxyPort;
            ProxyRss = proxyRss;
            ProxyType = proxyType;
            ProxyUsername = proxyUsername;
            PythonExecutablePath = pythonExecutablePath;
            QueueingEnabled = queueingEnabled;
            RandomPort = randomPort;
            ReannounceWhenAddressChanged = reannounceWhenAddressChanged;
            RecheckCompletedTorrents = recheckCompletedTorrents;
            RefreshInterval = refreshInterval;
            RequestQueueSize = requestQueueSize;
            ResolvePeerCountries = resolvePeerCountries;
            ResumeDataStorageType = resumeDataStorageType;
            RssAutoDownloadingEnabled = rssAutoDownloadingEnabled;
            RssDownloadRepackProperEpisodes = rssDownloadRepackProperEpisodes;
            RssFetchDelay = rssFetchDelay;
            RssMaxArticlesPerFeed = rssMaxArticlesPerFeed;
            RssProcessingEnabled = rssProcessingEnabled;
            RssRefreshInterval = rssRefreshInterval;
            RssSmartEpisodeFilters = rssSmartEpisodeFilters;
            SavePath = savePath;
            SavePathChangedTmmEnabled = savePathChangedTmmEnabled;
            SaveResumeDataInterval = saveResumeDataInterval;
            SaveStatisticsInterval = saveStatisticsInterval;
            ScanDirs = scanDirs;
            ScheduleFromHour = scheduleFromHour;
            ScheduleFromMin = scheduleFromMin;
            ScheduleToHour = scheduleToHour;
            ScheduleToMin = scheduleToMin;
            SchedulerDays = schedulerDays;
            SchedulerEnabled = schedulerEnabled;
            SendBufferLowWatermark = sendBufferLowWatermark;
            SendBufferWatermark = sendBufferWatermark;
            SendBufferWatermarkFactor = sendBufferWatermarkFactor;
            SlowTorrentDlRateThreshold = slowTorrentDlRateThreshold;
            SlowTorrentInactiveTimer = slowTorrentInactiveTimer;
            SlowTorrentUlRateThreshold = slowTorrentUlRateThreshold;
            SocketBacklogSize = socketBacklogSize;
            SocketReceiveBufferSize = socketReceiveBufferSize;
            SocketSendBufferSize = socketSendBufferSize;
            SsrfMitigation = ssrfMitigation;
            StopTrackerTimeout = stopTrackerTimeout;
            TempPath = tempPath;
            TempPathEnabled = tempPathEnabled;
            TorrentChangedTmmEnabled = torrentChangedTmmEnabled;
            TorrentContentLayout = torrentContentLayout;
            TorrentContentRemoveOption = torrentContentRemoveOption;
            TorrentFileSizeLimit = torrentFileSizeLimit;
            TorrentStopCondition = torrentStopCondition;
            UpLimit = upLimit;
            UploadChokingAlgorithm = uploadChokingAlgorithm;
            UploadSlotsBehavior = uploadSlotsBehavior;
            Upnp = upnp;
            UpnpLeaseDuration = upnpLeaseDuration;
            UseCategoryPathsInManualMode = useCategoryPathsInManualMode;
            UseHttps = useHttps;
            IgnoreSslErrors = ignoreSslErrors;
            UseSubcategories = useSubcategories;
            UtpTcpMixedMode = utpTcpMixedMode;
            ValidateHttpsTrackerCertificate = validateHttpsTrackerCertificate;
            WebUiAddress = webUiAddress;
            WebUiApiKey = webUiApiKey;
            WebUiBanDuration = webUiBanDuration;
            WebUiClickjackingProtectionEnabled = webUiClickjackingProtectionEnabled;
            WebUiCsrfProtectionEnabled = webUiCsrfProtectionEnabled;
            WebUiCustomHttpHeaders = webUiCustomHttpHeaders;
            WebUiDomainList = webUiDomainList;
            WebUiHostHeaderValidationEnabled = webUiHostHeaderValidationEnabled;
            WebUiHttpsCertPath = webUiHttpsCertPath;
            WebUiHttpsKeyPath = webUiHttpsKeyPath;
            WebUiMaxAuthFailCount = webUiMaxAuthFailCount;
            WebUiPort = webUiPort;
            WebUiReverseProxiesList = webUiReverseProxiesList;
            WebUiReverseProxyEnabled = webUiReverseProxyEnabled;
            WebUiSecureCookieEnabled = webUiSecureCookieEnabled;
            WebUiSessionTimeout = webUiSessionTimeout;
            WebUiUpnp = webUiUpnp;
            WebUiUseCustomHttpHeadersEnabled = webUiUseCustomHttpHeadersEnabled;
            WebUiUsername = webUiUsername;
            WebUiPassword = webUiPassword;
            ConfirmTorrentDeletion = confirmTorrentDeletion;
            ConfirmTorrentRecheck = confirmTorrentRecheck;
            StatusBarExternalIp = statusBarExternalIp;
        }

        [JsonPropertyName("add_to_top_of_queue")]
        public bool AddToTopOfQueue { get; }

        [JsonPropertyName("add_stopped_enabled")]
        public bool AddStoppedEnabled { get; }

        [JsonPropertyName("add_trackers")]
        public string AddTrackers { get; }

        [JsonPropertyName("add_trackers_enabled")]
        public bool AddTrackersEnabled { get; }

        [JsonPropertyName("alt_dl_limit")]
        public int AltDlLimit { get; }

        [JsonPropertyName("alt_up_limit")]
        public int AltUpLimit { get; }

        [JsonPropertyName("alternative_webui_enabled")]
        public bool AlternativeWebuiEnabled { get; }

        [JsonPropertyName("alternative_webui_path")]
        public string AlternativeWebuiPath { get; }

        [JsonPropertyName("announce_ip")]
        public string AnnounceIp { get; }

        [JsonPropertyName("announce_port")]
        public int AnnouncePort { get; }

        [JsonPropertyName("announce_to_all_tiers")]
        public bool AnnounceToAllTiers { get; }

        [JsonPropertyName("announce_to_all_trackers")]
        public bool AnnounceToAllTrackers { get; }

        [JsonPropertyName("anonymous_mode")]
        public bool AnonymousMode { get; }

        [JsonPropertyName("app_instance_name")]
        public string AppInstanceName { get; }

        [JsonPropertyName("async_io_threads")]
        public int AsyncIoThreads { get; }

        [JsonPropertyName("auto_delete_mode")]
        public int AutoDeleteMode { get; }

        [JsonPropertyName("auto_tmm_enabled")]
        public bool AutoTmmEnabled { get; }

        [JsonPropertyName("autorun_enabled")]
        public bool AutorunEnabled { get; }

        [JsonPropertyName("autorun_on_torrent_added_enabled")]
        public bool AutorunOnTorrentAddedEnabled { get; }

        [JsonPropertyName("autorun_on_torrent_added_program")]
        public string AutorunOnTorrentAddedProgram { get; }

        [JsonPropertyName("autorun_program")]
        public string AutorunProgram { get; }

        [JsonPropertyName("banned_IPs")]
        public string BannedIPs { get; }

        [JsonPropertyName("bdecode_depth_limit")]
        public int BdecodeDepthLimit { get; }

        [JsonPropertyName("bdecode_token_limit")]
        public int BdecodeTokenLimit { get; }

        [JsonPropertyName("bittorrent_protocol")]
        public int BittorrentProtocol { get; }

        [JsonPropertyName("block_peers_on_privileged_ports")]
        public bool BlockPeersOnPrivilegedPorts { get; }

        [JsonPropertyName("bypass_auth_subnet_whitelist")]
        public string BypassAuthSubnetWhitelist { get; }

        [JsonPropertyName("bypass_auth_subnet_whitelist_enabled")]
        public bool BypassAuthSubnetWhitelistEnabled { get; }

        [JsonPropertyName("bypass_local_auth")]
        public bool BypassLocalAuth { get; }

        [JsonPropertyName("category_changed_tmm_enabled")]
        public bool CategoryChangedTmmEnabled { get; }

        [JsonPropertyName("checking_memory_use")]
        public int CheckingMemoryUse { get; }

        [JsonPropertyName("connection_speed")]
        public int ConnectionSpeed { get; }

        [JsonPropertyName("current_interface_address")]
        public string CurrentInterfaceAddress { get; }

        [JsonPropertyName("current_interface_name")]
        public string CurrentInterfaceName { get; }

        [JsonPropertyName("current_network_interface")]
        public string CurrentNetworkInterface { get; }

        [JsonPropertyName("dht")]
        public bool Dht { get; }

        [JsonPropertyName("dht_bootstrap_nodes")]
        public string DhtBootstrapNodes { get; }

        [JsonPropertyName("disk_cache")]
        public int DiskCache { get; }

        [JsonPropertyName("disk_cache_ttl")]
        public int DiskCacheTtl { get; }

        [JsonPropertyName("disk_io_read_mode")]
        public int DiskIoReadMode { get; }

        [JsonPropertyName("disk_io_type")]
        public int DiskIoType { get; }

        [JsonPropertyName("disk_io_write_mode")]
        public int DiskIoWriteMode { get; }

        [JsonPropertyName("disk_queue_size")]
        public int DiskQueueSize { get; }

        [JsonPropertyName("dl_limit")]
        public int DlLimit { get; }

        [JsonPropertyName("dont_count_slow_torrents")]
        public bool DontCountSlowTorrents { get; }

        [JsonPropertyName("dyndns_domain")]
        public string DyndnsDomain { get; }

        [JsonPropertyName("dyndns_enabled")]
        public bool DyndnsEnabled { get; }

        [JsonPropertyName("dyndns_password")]
        public string DyndnsPassword { get; }

        [JsonPropertyName("dyndns_service")]
        public int DyndnsService { get; }

        [JsonPropertyName("dyndns_username")]
        public string DyndnsUsername { get; }

        [JsonPropertyName("embedded_tracker_port")]
        public int EmbeddedTrackerPort { get; }

        [JsonPropertyName("embedded_tracker_port_forwarding")]
        public bool EmbeddedTrackerPortForwarding { get; }

        [JsonPropertyName("enable_coalesce_read_write")]
        public bool EnableCoalesceReadWrite { get; }

        [JsonPropertyName("enable_embedded_tracker")]
        public bool EnableEmbeddedTracker { get; }

        [JsonPropertyName("enable_multi_connections_from_same_ip")]
        public bool EnableMultiConnectionsFromSameIp { get; }

        [JsonPropertyName("enable_piece_extent_affinity")]
        public bool EnablePieceExtentAffinity { get; }

        [JsonPropertyName("enable_upload_suggestions")]
        public bool EnableUploadSuggestions { get; }

        [JsonPropertyName("encryption")]
        public int Encryption { get; }

        [JsonPropertyName("excluded_file_names")]
        public string ExcludedFileNames { get; }

        [JsonPropertyName("excluded_file_names_enabled")]
        public bool ExcludedFileNamesEnabled { get; }

        [JsonPropertyName("export_dir")]
        public string ExportDir { get; }

        [JsonPropertyName("export_dir_fin")]
        public string ExportDirFin { get; }

        [JsonPropertyName("file_log_age")]
        public int FileLogAge { get; }

        [JsonPropertyName("file_log_age_type")]
        public int FileLogAgeType { get; }

        [JsonPropertyName("file_log_backup_enabled")]
        public bool FileLogBackupEnabled { get; }

        [JsonPropertyName("file_log_delete_old")]
        public bool FileLogDeleteOld { get; }

        [JsonPropertyName("file_log_enabled")]
        public bool FileLogEnabled { get; }

        [JsonPropertyName("file_log_max_size")]
        public int FileLogMaxSize { get; }

        [JsonPropertyName("file_log_path")]
        public string FileLogPath { get; }

        [JsonPropertyName("file_pool_size")]
        public int FilePoolSize { get; }

        [JsonPropertyName("hashing_threads")]
        public int HashingThreads { get; }

        [JsonPropertyName("i2p_address")]
        public string I2pAddress { get; }

        [JsonPropertyName("i2p_enabled")]
        public bool I2pEnabled { get; }

        [JsonPropertyName("i2p_inbound_length")]
        public int I2pInboundLength { get; }

        [JsonPropertyName("i2p_inbound_quantity")]
        public int I2pInboundQuantity { get; }

        [JsonPropertyName("i2p_mixed_mode")]
        public bool I2pMixedMode { get; }

        [JsonPropertyName("i2p_outbound_length")]
        public int I2pOutboundLength { get; }

        [JsonPropertyName("i2p_outbound_quantity")]
        public int I2pOutboundQuantity { get; }

        [JsonPropertyName("i2p_port")]
        public int I2pPort { get; }

        [JsonPropertyName("idn_support_enabled")]
        public bool IdnSupportEnabled { get; }

        [JsonPropertyName("incomplete_files_ext")]
        public bool IncompleteFilesExt { get; }

        [JsonPropertyName("use_unwanted_folder")]
        public bool UseUnwantedFolder { get; }

        [JsonPropertyName("ip_filter_enabled")]
        public bool IpFilterEnabled { get; }

        [JsonPropertyName("ip_filter_path")]
        public string IpFilterPath { get; }

        [JsonPropertyName("ip_filter_trackers")]
        public bool IpFilterTrackers { get; }

        [JsonPropertyName("limit_lan_peers")]
        public bool LimitLanPeers { get; }

        [JsonPropertyName("limit_tcp_overhead")]
        public bool LimitTcpOverhead { get; }

        [JsonPropertyName("limit_utp_rate")]
        public bool LimitUtpRate { get; }

        [JsonPropertyName("listen_port")]
        public int ListenPort { get; }

        [JsonPropertyName("ssl_enabled")]
        public bool SslEnabled { get; }

        [JsonPropertyName("ssl_listen_port")]
        public int SslListenPort { get; }

        [JsonPropertyName("locale")]
        public string Locale { get; }

        [JsonPropertyName("lsd")]
        public bool Lsd { get; }

        [JsonPropertyName("mail_notification_auth_enabled")]
        public bool MailNotificationAuthEnabled { get; }

        [JsonPropertyName("mail_notification_email")]
        public string MailNotificationEmail { get; }

        [JsonPropertyName("mail_notification_enabled")]
        public bool MailNotificationEnabled { get; }

        [JsonPropertyName("mail_notification_password")]
        public string MailNotificationPassword { get; }

        [JsonPropertyName("mail_notification_sender")]
        public string MailNotificationSender { get; }

        [JsonPropertyName("mail_notification_smtp")]
        public string MailNotificationSmtp { get; }

        [JsonPropertyName("mail_notification_ssl_enabled")]
        public bool MailNotificationSslEnabled { get; }

        [JsonPropertyName("mail_notification_username")]
        public string MailNotificationUsername { get; }

        [JsonPropertyName("mark_of_the_web")]
        public bool MarkOfTheWeb { get; }

        [JsonPropertyName("max_active_checking_torrents")]
        public int MaxActiveCheckingTorrents { get; }

        [JsonPropertyName("max_active_downloads")]
        public int MaxActiveDownloads { get; }

        [JsonPropertyName("max_active_torrents")]
        public int MaxActiveTorrents { get; }

        [JsonPropertyName("max_active_uploads")]
        public int MaxActiveUploads { get; }

        [JsonPropertyName("max_concurrent_http_announces")]
        public int MaxConcurrentHttpAnnounces { get; }

        [JsonPropertyName("max_connec")]
        public int MaxConnec { get; }

        [JsonPropertyName("max_connec_per_torrent")]
        public int MaxConnecPerTorrent { get; }

        [JsonPropertyName("max_inactive_seeding_time")]
        public int MaxInactiveSeedingTime { get; }

        [JsonPropertyName("max_inactive_seeding_time_enabled")]
        public bool MaxInactiveSeedingTimeEnabled { get; }

        [JsonPropertyName("max_ratio")]
        public float MaxRatio { get; }

        [JsonPropertyName("max_ratio_act")]
        public int MaxRatioAct { get; }

        [JsonPropertyName("max_ratio_enabled")]
        public bool MaxRatioEnabled { get; }

        [JsonPropertyName("max_seeding_time")]
        public int MaxSeedingTime { get; }

        [JsonPropertyName("max_seeding_time_enabled")]
        public bool MaxSeedingTimeEnabled { get; }

        [JsonPropertyName("max_uploads")]
        public int MaxUploads { get; }

        [JsonPropertyName("max_uploads_per_torrent")]
        public int MaxUploadsPerTorrent { get; }

        [JsonPropertyName("memory_working_set_limit")]
        public int MemoryWorkingSetLimit { get; }

        [JsonPropertyName("merge_trackers")]
        public bool MergeTrackers { get; }

        [JsonPropertyName("outgoing_ports_max")]
        public int OutgoingPortsMax { get; }

        [JsonPropertyName("outgoing_ports_min")]
        public int OutgoingPortsMin { get; }

        [JsonPropertyName("peer_tos")]
        public int PeerTos { get; }

        [JsonPropertyName("peer_turnover")]
        public int PeerTurnover { get; }

        [JsonPropertyName("peer_turnover_cutoff")]
        public int PeerTurnoverCutoff { get; }

        [JsonPropertyName("peer_turnover_interval")]
        public int PeerTurnoverInterval { get; }

        [JsonPropertyName("performance_warning")]
        public bool PerformanceWarning { get; }

        [JsonPropertyName("pex")]
        public bool Pex { get; }

        [JsonPropertyName("preallocate_all")]
        public bool PreallocateAll { get; }

        [JsonPropertyName("proxy_auth_enabled")]
        public bool ProxyAuthEnabled { get; }

        [JsonPropertyName("proxy_bittorrent")]
        public bool ProxyBittorrent { get; }

        [JsonPropertyName("proxy_hostname_lookup")]
        public bool ProxyHostnameLookup { get; }

        [JsonPropertyName("proxy_ip")]
        public string ProxyIp { get; }

        [JsonPropertyName("proxy_misc")]
        public bool ProxyMisc { get; }

        [JsonPropertyName("proxy_password")]
        public string ProxyPassword { get; }

        [JsonPropertyName("proxy_peer_connections")]
        public bool ProxyPeerConnections { get; }

        [JsonPropertyName("proxy_port")]
        public int ProxyPort { get; }

        [JsonPropertyName("proxy_rss")]
        public bool ProxyRss { get; }

        [JsonPropertyName("proxy_type")]
        public string ProxyType { get; }

        [JsonPropertyName("proxy_username")]
        public string ProxyUsername { get; }

        [JsonPropertyName("python_executable_path")]
        public string PythonExecutablePath { get; }

        [JsonPropertyName("queueing_enabled")]
        public bool QueueingEnabled { get; }

        [JsonPropertyName("random_port")]
        public bool RandomPort { get; }

        [JsonPropertyName("reannounce_when_address_changed")]
        public bool ReannounceWhenAddressChanged { get; }

        [JsonPropertyName("recheck_completed_torrents")]
        public bool RecheckCompletedTorrents { get; }

        [JsonPropertyName("refresh_interval")]
        public int RefreshInterval { get; }

        [JsonPropertyName("request_queue_size")]
        public int RequestQueueSize { get; }

        [JsonPropertyName("resolve_peer_countries")]
        public bool ResolvePeerCountries { get; }

        [JsonPropertyName("resume_data_storage_type")]
        public string ResumeDataStorageType { get; }

        [JsonPropertyName("rss_auto_downloading_enabled")]
        public bool RssAutoDownloadingEnabled { get; }

        [JsonPropertyName("rss_download_repack_proper_episodes")]
        public bool RssDownloadRepackProperEpisodes { get; }

        [JsonPropertyName("rss_fetch_delay")]
        public long RssFetchDelay { get; }

        [JsonPropertyName("rss_max_articles_per_feed")]
        public int RssMaxArticlesPerFeed { get; }

        [JsonPropertyName("rss_processing_enabled")]
        public bool RssProcessingEnabled { get; }

        [JsonPropertyName("rss_refresh_interval")]
        public int RssRefreshInterval { get; }

        [JsonPropertyName("rss_smart_episode_filters")]
        public string RssSmartEpisodeFilters { get; }

        [JsonPropertyName("save_path")]
        public string SavePath { get; }

        [JsonPropertyName("save_path_changed_tmm_enabled")]
        public bool SavePathChangedTmmEnabled { get; }

        [JsonPropertyName("save_resume_data_interval")]
        public int SaveResumeDataInterval { get; }

        [JsonPropertyName("save_statistics_interval")]
        public int SaveStatisticsInterval { get; }

        [JsonPropertyName("scan_dirs")]
        public Dictionary<string, SaveLocation> ScanDirs { get; }

        [JsonPropertyName("schedule_from_hour")]
        public int ScheduleFromHour { get; }

        [JsonPropertyName("schedule_from_min")]
        public int ScheduleFromMin { get; }

        [JsonPropertyName("schedule_to_hour")]
        public int ScheduleToHour { get; }

        [JsonPropertyName("schedule_to_min")]
        public int ScheduleToMin { get; }

        [JsonPropertyName("scheduler_days")]
        public int SchedulerDays { get; }

        [JsonPropertyName("scheduler_enabled")]
        public bool SchedulerEnabled { get; }

        [JsonPropertyName("send_buffer_low_watermark")]
        public int SendBufferLowWatermark { get; }

        [JsonPropertyName("send_buffer_watermark")]
        public int SendBufferWatermark { get; }

        [JsonPropertyName("send_buffer_watermark_factor")]
        public int SendBufferWatermarkFactor { get; }

        [JsonPropertyName("slow_torrent_dl_rate_threshold")]
        public int SlowTorrentDlRateThreshold { get; }

        [JsonPropertyName("slow_torrent_inactive_timer")]
        public int SlowTorrentInactiveTimer { get; }

        [JsonPropertyName("slow_torrent_ul_rate_threshold")]
        public int SlowTorrentUlRateThreshold { get; }

        [JsonPropertyName("socket_backlog_size")]
        public int SocketBacklogSize { get; }

        [JsonPropertyName("socket_receive_buffer_size")]
        public int SocketReceiveBufferSize { get; }

        [JsonPropertyName("socket_send_buffer_size")]
        public int SocketSendBufferSize { get; }

        [JsonPropertyName("ssrf_mitigation")]
        public bool SsrfMitigation { get; }

        [JsonPropertyName("stop_tracker_timeout")]
        public int StopTrackerTimeout { get; }

        [JsonPropertyName("temp_path")]
        public string TempPath { get; }

        [JsonPropertyName("temp_path_enabled")]
        public bool TempPathEnabled { get; }

        [JsonPropertyName("torrent_changed_tmm_enabled")]
        public bool TorrentChangedTmmEnabled { get; }

        [JsonPropertyName("torrent_content_layout")]
        public string TorrentContentLayout { get; }

        [JsonPropertyName("torrent_content_remove_option")]
        public string TorrentContentRemoveOption { get; }

        [JsonPropertyName("torrent_file_size_limit")]
        public int TorrentFileSizeLimit { get; }

        [JsonPropertyName("torrent_stop_condition")]
        public string TorrentStopCondition { get; }

        [JsonPropertyName("up_limit")]
        public int UpLimit { get; }

        [JsonPropertyName("upload_choking_algorithm")]
        public int UploadChokingAlgorithm { get; }

        [JsonPropertyName("upload_slots_behavior")]
        public int UploadSlotsBehavior { get; }

        [JsonPropertyName("upnp")]
        public bool Upnp { get; }

        [JsonPropertyName("upnp_lease_duration")]
        public int UpnpLeaseDuration { get; }

        [JsonPropertyName("use_category_paths_in_manual_mode")]
        public bool UseCategoryPathsInManualMode { get; }

        [JsonPropertyName("use_https")]
        public bool UseHttps { get; }

        [JsonPropertyName("ignore_ssl_errors")]
        public bool IgnoreSslErrors { get; }

        [JsonPropertyName("use_subcategories")]
        public bool UseSubcategories { get; }

        [JsonPropertyName("utp_tcp_mixed_mode")]
        public int UtpTcpMixedMode { get; }

        [JsonPropertyName("validate_https_tracker_certificate")]
        public bool ValidateHttpsTrackerCertificate { get; }

        [JsonPropertyName("web_ui_address")]
        public string WebUiAddress { get; }

        [JsonPropertyName("web_ui_api_key")]
        public string WebUiApiKey { get; }

        [JsonPropertyName("web_ui_ban_duration")]
        public int WebUiBanDuration { get; }

        [JsonPropertyName("web_ui_clickjacking_protection_enabled")]
        public bool WebUiClickjackingProtectionEnabled { get; }

        [JsonPropertyName("web_ui_csrf_protection_enabled")]
        public bool WebUiCsrfProtectionEnabled { get; }

        [JsonPropertyName("web_ui_custom_http_headers")]
        public string WebUiCustomHttpHeaders { get; }

        [JsonPropertyName("web_ui_domain_list")]
        public string WebUiDomainList { get; }

        [JsonPropertyName("web_ui_host_header_validation_enabled")]
        public bool WebUiHostHeaderValidationEnabled { get; }

        [JsonPropertyName("web_ui_https_cert_path")]
        public string WebUiHttpsCertPath { get; }

        [JsonPropertyName("web_ui_https_key_path")]
        public string WebUiHttpsKeyPath { get; }

        [JsonPropertyName("web_ui_max_auth_fail_count")]
        public int WebUiMaxAuthFailCount { get; }

        [JsonPropertyName("web_ui_port")]
        public int WebUiPort { get; }

        [JsonPropertyName("web_ui_reverse_proxies_list")]
        public string WebUiReverseProxiesList { get; }

        [JsonPropertyName("web_ui_reverse_proxy_enabled")]
        public bool WebUiReverseProxyEnabled { get; }

        [JsonPropertyName("web_ui_secure_cookie_enabled")]
        public bool WebUiSecureCookieEnabled { get; }

        [JsonPropertyName("web_ui_session_timeout")]
        public int WebUiSessionTimeout { get; }

        [JsonPropertyName("web_ui_upnp")]
        public bool WebUiUpnp { get; }

        [JsonPropertyName("web_ui_use_custom_http_headers_enabled")]
        public bool WebUiUseCustomHttpHeadersEnabled { get; }

        [JsonPropertyName("web_ui_username")]
        public string WebUiUsername { get; }

        [JsonPropertyName("web_ui_password")]
        public string WebUiPassword { get; }

        [JsonPropertyName("confirm_torrent_deletion")]
        public bool ConfirmTorrentDeletion { get; }

        [JsonPropertyName("confirm_torrent_recheck")]
        public bool ConfirmTorrentRecheck { get; }

        [JsonPropertyName("status_bar_external_ip")]
        public bool StatusBarExternalIp { get; }
    }
}