using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record UpdatePreferences
    {
        [JsonPropertyName("add_to_top_of_queue")]
        public bool? AddToTopOfQueue { get; set; }

        [JsonPropertyName("add_trackers")]
        public string? AddTrackers { get; set; }

        [JsonPropertyName("add_trackers_enabled")]
        public bool? AddTrackersEnabled { get; set; }

        [JsonPropertyName("alt_dl_limit")]
        public int? AltDlLimit { get; set; }

        [JsonPropertyName("alt_up_limit")]
        public int? AltUpLimit { get; set; }

        [JsonPropertyName("alternative_webui_enabled")]
        public bool? AlternativeWebuiEnabled { get; set; }

        [JsonPropertyName("alternative_webui_path")]
        public string? AlternativeWebuiPath { get; set; }

        [JsonPropertyName("announce_ip")]
        public string? AnnounceIp { get; set; }

        [JsonPropertyName("announce_to_all_tiers")]
        public bool? AnnounceToAllTiers { get; set; }

        [JsonPropertyName("announce_to_all_trackers")]
        public bool? AnnounceToAllTrackers { get; set; }

        [JsonPropertyName("anonymous_mode")]
        public bool? AnonymousMode { get; set; }

        [JsonPropertyName("app_instance_name")]
        public string? AppInstanceName { get; set; }

        [JsonPropertyName("async_io_threads")]
        public int? AsyncIoThreads { get; set; }

        [JsonPropertyName("auto_delete_mode")]
        public int? AutoDeleteMode { get; set; }

        [JsonPropertyName("auto_tmm_enabled")]
        public bool? AutoTmmEnabled { get; set; }

        [JsonPropertyName("autorun_enabled")]
        public bool? AutorunEnabled { get; set; }

        [JsonPropertyName("autorun_on_torrent_added_enabled")]
        public bool? AutorunOnTorrentAddedEnabled { get; set; }

        [JsonPropertyName("autorun_on_torrent_added_program")]
        public string? AutorunOnTorrentAddedProgram { get; set; }

        [JsonPropertyName("autorun_program")]
        public string? AutorunProgram { get; set; }

        [JsonPropertyName("banned_IPs")]
        public string? BannedIPs { get; set; }

        [JsonPropertyName("bdecode_depth_limit")]
        public int? BdecodeDepthLimit { get; set; }

        [JsonPropertyName("bdecode_token_limit")]
        public int? BdecodeTokenLimit { get; set; }

        [JsonPropertyName("bittorrent_protocol")]
        public int? BittorrentProtocol { get; set; }

        [JsonPropertyName("block_peers_on_privileged_ports")]
        public bool? BlockPeersOnPrivilegedPorts { get; set; }

        [JsonPropertyName("bypass_auth_subnet_whitelist")]
        public string? BypassAuthSubnetWhitelist { get; set; }

        [JsonPropertyName("bypass_auth_subnet_whitelist_enabled")]
        public bool? BypassAuthSubnetWhitelistEnabled { get; set; }

        [JsonPropertyName("bypass_local_auth")]
        public bool? BypassLocalAuth { get; set; }

        [JsonPropertyName("category_changed_tmm_enabled")]
        public bool? CategoryChangedTmmEnabled { get; set; }

        [JsonPropertyName("checking_memory_use")]
        public int? CheckingMemoryUse { get; set; }

        [JsonPropertyName("connection_speed")]
        public int? ConnectionSpeed { get; set; }

        [JsonPropertyName("current_interface_address")]
        public string? CurrentInterfaceAddress { get; set; }

        [JsonPropertyName("current_interface_name")]
        public string? CurrentInterfaceName { get; set; }

        [JsonPropertyName("current_network_interface")]
        public string? CurrentNetworkInterface { get; set; }

        [JsonPropertyName("dht")]
        public bool? Dht { get; set; }

        [JsonPropertyName("dht_bootstrap_nodes")]
        public string? DhtBootstrapNodes { get; set; }

        [JsonPropertyName("disk_cache")]
        public int? DiskCache { get; set; }

        [JsonPropertyName("disk_cache_ttl")]
        public int? DiskCacheTtl { get; set; }

        [JsonPropertyName("disk_io_read_mode")]
        public int? DiskIoReadMode { get; set; }

        [JsonPropertyName("disk_io_type")]
        public int? DiskIoType { get; set; }

        [JsonPropertyName("disk_io_write_mode")]
        public int? DiskIoWriteMode { get; set; }

        [JsonPropertyName("disk_queue_size")]
        public int? DiskQueueSize { get; set; }

        [JsonPropertyName("dl_limit")]
        public int? DlLimit { get; set; }

        [JsonPropertyName("dont_count_slow_torrents")]
        public bool? DontCountSlowTorrents { get; set; }

        [JsonPropertyName("dyndns_domain")]
        public string? DyndnsDomain { get; set; }

        [JsonPropertyName("dyndns_enabled")]
        public bool? DyndnsEnabled { get; set; }

        [JsonPropertyName("dyndns_password")]
        public string? DyndnsPassword { get; set; }

        [JsonPropertyName("dyndns_service")]
        public int? DyndnsService { get; set; }

        [JsonPropertyName("dyndns_username")]
        public string? DyndnsUsername { get; set; }

        [JsonPropertyName("embedded_tracker_port")]
        public int? EmbeddedTrackerPort { get; set; }

        [JsonPropertyName("embedded_tracker_port_forwarding")]
        public bool? EmbeddedTrackerPortForwarding { get; set; }

        [JsonPropertyName("enable_coalesce_read_write")]
        public bool? EnableCoalesceReadWrite { get; set; }

        [JsonPropertyName("enable_embedded_tracker")]
        public bool? EnableEmbeddedTracker { get; set; }

        [JsonPropertyName("enable_multi_connections_from_same_ip")]
        public bool? EnableMultiConnectionsFromSameIp { get; set; }

        [JsonPropertyName("enable_piece_extent_affinity")]
        public bool? EnablePieceExtentAffinity { get; set; }

        [JsonPropertyName("enable_upload_suggestions")]
        public bool? EnableUploadSuggestions { get; set; }

        [JsonPropertyName("encryption")]
        public int? Encryption { get; set; }

        [JsonPropertyName("excluded_file_names")]
        public string? ExcludedFileNames { get; set; }

        [JsonPropertyName("excluded_file_names_enabled")]
        public bool? ExcludedFileNamesEnabled { get; set; }

        [JsonPropertyName("export_dir")]
        public string? ExportDir { get; set; }

        [JsonPropertyName("export_dir_fin")]
        public string? ExportDirFin { get; set; }

        [JsonPropertyName("file_log_age")]
        public int? FileLogAge { get; set; }

        [JsonPropertyName("file_log_age_type")]
        public int? FileLogAgeType { get; set; }

        [JsonPropertyName("file_log_backup_enabled")]
        public bool? FileLogBackupEnabled { get; set; }

        [JsonPropertyName("file_log_delete_old")]
        public bool? FileLogDeleteOld { get; set; }

        [JsonPropertyName("file_log_enabled")]
        public bool? FileLogEnabled { get; set; }

        [JsonPropertyName("file_log_max_size")]
        public int? FileLogMaxSize { get; set; }

        [JsonPropertyName("file_log_path")]
        public string? FileLogPath { get; set; }

        [JsonPropertyName("file_pool_size")]
        public int? FilePoolSize { get; set; }

        [JsonPropertyName("hashing_threads")]
        public int? HashingThreads { get; set; }

        [JsonPropertyName("i2p_address")]
        public string? I2pAddress { get; set; }

        [JsonPropertyName("i2p_enabled")]
        public bool? I2pEnabled { get; set; }

        [JsonPropertyName("i2p_inbound_length")]
        public int? I2pInboundLength { get; set; }

        [JsonPropertyName("i2p_inbound_quantity")]
        public int? I2pInboundQuantity { get; set; }

        [JsonPropertyName("i2p_mixed_mode")]
        public bool? I2pMixedMode { get; set; }

        [JsonPropertyName("i2p_outbound_length")]
        public int? I2pOutboundLength { get; set; }

        [JsonPropertyName("i2p_outbound_quantity")]
        public int? I2pOutboundQuantity { get; set; }

        [JsonPropertyName("i2p_port")]
        public int? I2pPort { get; set; }

        [JsonPropertyName("idn_support_enabled")]
        public bool? IdnSupportEnabled { get; set; }

        [JsonPropertyName("incomplete_files_ext")]
        public bool? IncompleteFilesExt { get; set; }

        [JsonPropertyName("ip_filter_enabled")]
        public bool? IpFilterEnabled { get; set; }

        [JsonPropertyName("ip_filter_path")]
        public string? IpFilterPath { get; set; }

        [JsonPropertyName("ip_filter_trackers")]
        public bool? IpFilterTrackers { get; set; }

        [JsonPropertyName("limit_lan_peers")]
        public bool? LimitLanPeers { get; set; }

        [JsonPropertyName("limit_tcp_overhead")]
        public bool? LimitTcpOverhead { get; set; }

        [JsonPropertyName("limit_utp_rate")]
        public bool? LimitUtpRate { get; set; }

        [JsonPropertyName("listen_port")]
        public int? ListenPort { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("lsd")]
        public bool? Lsd { get; set; }

        [JsonPropertyName("mail_notification_auth_enabled")]
        public bool? MailNotificationAuthEnabled { get; set; }

        [JsonPropertyName("mail_notification_email")]
        public string? MailNotificationEmail { get; set; }

        [JsonPropertyName("mail_notification_enabled")]
        public bool? MailNotificationEnabled { get; set; }

        [JsonPropertyName("mail_notification_password")]
        public string? MailNotificationPassword { get; set; }

        [JsonPropertyName("mail_notification_sender")]
        public string? MailNotificationSender { get; set; }

        [JsonPropertyName("mail_notification_smtp")]
        public string? MailNotificationSmtp { get; set; }

        [JsonPropertyName("mail_notification_ssl_enabled")]
        public bool? MailNotificationSslEnabled { get; set; }

        [JsonPropertyName("mail_notification_username")]
        public string? MailNotificationUsername { get; set; }

        [JsonPropertyName("mark_of_the_web")]
        public bool? MarkOfTheWeb { get; set; }

        [JsonPropertyName("max_active_checking_torrents")]
        public int? MaxActiveCheckingTorrents { get; set; }

        [JsonPropertyName("max_active_downloads")]
        public int? MaxActiveDownloads { get; set; }

        [JsonPropertyName("max_active_torrents")]
        public int? MaxActiveTorrents { get; set; }

        [JsonPropertyName("max_active_uploads")]
        public int? MaxActiveUploads { get; set; }

        [JsonPropertyName("max_concurrent_http_announces")]
        public int? MaxConcurrentHttpAnnounces { get; set; }

        [JsonPropertyName("max_connec")]
        public int? MaxConnec { get; set; }

        [JsonPropertyName("max_connec_per_torrent")]
        public int? MaxConnecPerTorrent { get; set; }

        [JsonPropertyName("max_inactive_seeding_time")]
        public int? MaxInactiveSeedingTime { get; set; }

        [JsonPropertyName("max_inactive_seeding_time_enabled")]
        public bool? MaxInactiveSeedingTimeEnabled { get; set; }

        [JsonPropertyName("max_ratio")]
        public float? MaxRatio { get; set; }

        [JsonPropertyName("max_ratio_act")]
        public int? MaxRatioAct { get; set; }

        [JsonPropertyName("max_ratio_enabled")]
        public bool? MaxRatioEnabled { get; set; }

        [JsonPropertyName("max_seeding_time")]
        public int? MaxSeedingTime { get; set; }

        [JsonPropertyName("max_seeding_time_enabled")]
        public bool? MaxSeedingTimeEnabled { get; set; }

        [JsonPropertyName("max_uploads")]
        public int? MaxUploads { get; set; }

        [JsonPropertyName("max_uploads_per_torrent")]
        public int? MaxUploadsPerTorrent { get; set; }

        [JsonPropertyName("memory_working_set_limit")]
        public int? MemoryWorkingSetLimit { get; set; }

        [JsonPropertyName("merge_trackers")]
        public bool? MergeTrackers { get; set; }

        [JsonPropertyName("outgoing_ports_max")]
        public int? OutgoingPortsMax { get; set; }

        [JsonPropertyName("outgoing_ports_min")]
        public int? OutgoingPortsMin { get; set; }

        [JsonPropertyName("peer_tos")]
        public int? PeerTos { get; set; }

        [JsonPropertyName("peer_turnover")]
        public int? PeerTurnover { get; set; }

        [JsonPropertyName("peer_turnover_cutoff")]
        public int? PeerTurnoverCutoff { get; set; }

        [JsonPropertyName("peer_turnover_interval")]
        public int? PeerTurnoverInterval { get; set; }

        [JsonPropertyName("performance_warning")]
        public bool? PerformanceWarning { get; set; }

        [JsonPropertyName("pex")]
        public bool? Pex { get; set; }

        [JsonPropertyName("preallocate_all")]
        public bool? PreallocateAll { get; set; }

        [JsonPropertyName("proxy_auth_enabled")]
        public bool? ProxyAuthEnabled { get; set; }

        [JsonPropertyName("proxy_bittorrent")]
        public bool? ProxyBittorrent { get; set; }

        [JsonPropertyName("proxy_hostname_lookup")]
        public bool? ProxyHostnameLookup { get; set; }

        [JsonPropertyName("proxy_ip")]
        public string? ProxyIp { get; set; }

        [JsonPropertyName("proxy_misc")]
        public bool? ProxyMisc { get; set; }

        [JsonPropertyName("proxy_password")]
        public string? ProxyPassword { get; set; }

        [JsonPropertyName("proxy_peer_connections")]
        public bool? ProxyPeerConnections { get; set; }

        [JsonPropertyName("proxy_port")]
        public int? ProxyPort { get; set; }

        [JsonPropertyName("proxy_rss")]
        public bool? ProxyRss { get; set; }

        [JsonPropertyName("proxy_type")]
        public string? ProxyType { get; set; }

        [JsonPropertyName("proxy_username")]
        public string? ProxyUsername { get; set; }

        [JsonPropertyName("python_executable_path")]
        public string? PythonExecutablePath { get; set; }

        [JsonPropertyName("queueing_enabled")]
        public bool? QueueingEnabled { get; set; }

        [JsonPropertyName("random_port")]
        public bool? RandomPort { get; set; }

        [JsonPropertyName("reannounce_when_address_changed")]
        public bool? ReannounceWhenAddressChanged { get; set; }

        [JsonPropertyName("recheck_completed_torrents")]
        public bool? RecheckCompletedTorrents { get; set; }

        [JsonPropertyName("refresh_interval")]
        public int? RefreshInterval { get; set; }

        [JsonPropertyName("request_queue_size")]
        public int? RequestQueueSize { get; set; }

        [JsonPropertyName("resolve_peer_countries")]
        public bool? ResolvePeerCountries { get; set; }

        [JsonPropertyName("resume_data_storage_type")]
        public string? ResumeDataStorageType { get; set; }

        [JsonPropertyName("rss_auto_downloading_enabled")]
        public bool? RssAutoDownloadingEnabled { get; set; }

        [JsonPropertyName("rss_download_repack_proper_episodes")]
        public bool? RssDownloadRepackProperEpisodes { get; set; }

        [JsonPropertyName("rss_fetch_delay")]
        public long? RssFetchDelay { get; set; }

        [JsonPropertyName("rss_max_articles_per_feed")]
        public int? RssMaxArticlesPerFeed { get; set; }

        [JsonPropertyName("rss_processing_enabled")]
        public bool? RssProcessingEnabled { get; set; }

        [JsonPropertyName("rss_refresh_interval")]
        public int? RssRefreshInterval { get; set; }

        [JsonPropertyName("rss_smart_episode_filters")]
        public string? RssSmartEpisodeFilters { get; set; }

        [JsonPropertyName("save_path")]
        public string? SavePath { get; set; }

        [JsonPropertyName("save_path_changed_tmm_enabled")]
        public bool? SavePathChangedTmmEnabled { get; set; }

        [JsonPropertyName("save_resume_data_interval")]
        public int? SaveResumeDataInterval { get; set; }

        [JsonPropertyName("scan_dirs")]
        public Dictionary<string, SaveLocation>? ScanDirs { get; set; }

        [JsonPropertyName("schedule_from_hour")]
        public int? ScheduleFromHour { get; set; }

        [JsonPropertyName("schedule_from_min")]
        public int? ScheduleFromMin { get; set; }

        [JsonPropertyName("schedule_to_hour")]
        public int? ScheduleToHour { get; set; }

        [JsonPropertyName("schedule_to_min")]
        public int? ScheduleToMin { get; set; }

        [JsonPropertyName("scheduler_days")]
        public int? SchedulerDays { get; set; }

        [JsonPropertyName("scheduler_enabled")]
        public bool? SchedulerEnabled { get; set; }

        [JsonPropertyName("send_buffer_low_watermark")]
        public int? SendBufferLowWatermark { get; set; }

        [JsonPropertyName("send_buffer_watermark")]
        public int? SendBufferWatermark { get; set; }

        [JsonPropertyName("send_buffer_watermark_factor")]
        public int? SendBufferWatermarkFactor { get; set; }

        [JsonPropertyName("slow_torrent_dl_rate_threshold")]
        public int? SlowTorrentDlRateThreshold { get; set; }

        [JsonPropertyName("slow_torrent_inactive_timer")]
        public int? SlowTorrentInactiveTimer { get; set; }

        [JsonPropertyName("slow_torrent_ul_rate_threshold")]
        public int? SlowTorrentUlRateThreshold { get; set; }

        [JsonPropertyName("socket_backlog_size")]
        public int? SocketBacklogSize { get; set; }

        [JsonPropertyName("socket_receive_buffer_size")]
        public int? SocketReceiveBufferSize { get; set; }

        [JsonPropertyName("socket_send_buffer_size")]
        public int? SocketSendBufferSize { get; set; }

        [JsonPropertyName("ssrf_mitigation")]
        public bool? SsrfMitigation { get; set; }

        [JsonPropertyName("start_paused_enabled")]
        public bool? StartPausedEnabled { get; set; }

        [JsonPropertyName("stop_tracker_timeout")]
        public int? StopTrackerTimeout { get; set; }

        [JsonPropertyName("temp_path")]
        public string? TempPath { get; set; }

        [JsonPropertyName("temp_path_enabled")]
        public bool? TempPathEnabled { get; set; }

        [JsonPropertyName("torrent_changed_tmm_enabled")]
        public bool? TorrentChangedTmmEnabled { get; set; }

        [JsonPropertyName("torrent_content_layout")]
        public string? TorrentContentLayout { get; set; }

        [JsonPropertyName("torrent_file_size_limit")]
        public int? TorrentFileSizeLimit { get; set; }

        [JsonPropertyName("torrent_stop_condition")]
        public string? TorrentStopCondition { get; set; }

        [JsonPropertyName("up_limit")]
        public int? UpLimit { get; set; }

        [JsonPropertyName("upload_choking_algorithm")]
        public int? UploadChokingAlgorithm { get; set; }

        [JsonPropertyName("upload_slots_behavior")]
        public int? UploadSlotsBehavior { get; set; }

        [JsonPropertyName("upnp")]
        public bool? Upnp { get; set; }

        [JsonPropertyName("upnp_lease_duration")]
        public int? UpnpLeaseDuration { get; set; }

        [JsonPropertyName("use_category_paths_in_manual_mode")]
        public bool? UseCategoryPathsInManualMode { get; set; }

        [JsonPropertyName("use_https")]
        public bool? UseHttps { get; set; }

        [JsonPropertyName("use_subcategories")]
        public bool? UseSubcategories { get; set; }

        [JsonPropertyName("utp_tcp_mixed_mode")]
        public int? UtpTcpMixedMode { get; set; }

        [JsonPropertyName("validate_https_tracker_certificate")]
        public bool? ValidateHttpsTrackerCertificate { get; set; }

        [JsonPropertyName("web_ui_address")]
        public string? WebUiAddress { get; set; }

        [JsonPropertyName("web_ui_ban_duration")]
        public int? WebUiBanDuration { get; set; }

        [JsonPropertyName("web_ui_clickjacking_protection_enabled")]
        public bool? WebUiClickjackingProtectionEnabled { get; set; }

        [JsonPropertyName("web_ui_csrf_protection_enabled")]
        public bool? WebUiCsrfProtectionEnabled { get; set; }

        [JsonPropertyName("web_ui_custom_http_headers")]
        public string? WebUiCustomHttpHeaders { get; set; }

        [JsonPropertyName("web_ui_domain_list")]
        public string? WebUiDomainList { get; set; }

        [JsonPropertyName("web_ui_host_header_validation_enabled")]
        public bool? WebUiHostHeaderValidationEnabled { get; set; }

        [JsonPropertyName("web_ui_https_cert_path")]
        public string? WebUiHttpsCertPath { get; set; }

        [JsonPropertyName("web_ui_https_key_path")]
        public string? WebUiHttpsKeyPath { get; set; }

        [JsonPropertyName("web_ui_max_auth_fail_count")]
        public int? WebUiMaxAuthFailCount { get; set; }

        [JsonPropertyName("web_ui_port")]
        public int? WebUiPort { get; set; }

        [JsonPropertyName("web_ui_reverse_proxies_list")]
        public string? WebUiReverseProxiesList { get; set; }

        [JsonPropertyName("web_ui_reverse_proxy_enabled")]
        public bool? WebUiReverseProxyEnabled { get; set; }

        [JsonPropertyName("web_ui_secure_cookie_enabled")]
        public bool? WebUiSecureCookieEnabled { get; set; }

        [JsonPropertyName("web_ui_session_timeout")]
        public int? WebUiSessionTimeout { get; set; }

        [JsonPropertyName("web_ui_upnp")]
        public bool? WebUiUpnp { get; set; }

        [JsonPropertyName("web_ui_use_custom_http_headers_enabled")]
        public bool? WebUiUseCustomHttpHeadersEnabled { get; set; }

        [JsonPropertyName("web_ui_username")]
        public string? WebUiUsername { get; set; }

        [JsonPropertyName("web_ui_password")]
        public string? WebUiPassword { get; set; }
    }
}