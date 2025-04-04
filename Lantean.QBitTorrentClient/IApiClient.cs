﻿using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBitTorrentClient
{
    public interface IApiClient
    {
        #region Authentication

        Task<bool> CheckAuthState();

        Task Login(string username, string password);

        Task Logout();

        #endregion Authentication

        #region Application

        Task<string> GetApplicationVersion();

        Task<string> GetAPIVersion();

        Task<BuildInfo> GetBuildInfo();

        Task Shutdown();

        Task<Preferences> GetApplicationPreferences();

        Task SetApplicationPreferences(UpdatePreferences preferences);

        Task<string> GetDefaultSavePath();

        Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfaces();

        Task<IReadOnlyList<string>> GetNetworkInterfaceAddressList(string @interface);

        #endregion Application

        #region Log

        Task<IReadOnlyList<Log>> GetLog(bool? normal = null, bool? info = null, bool? warning = null, bool? critical = null, int? lastKnownId = null);

        Task<IReadOnlyList<PeerLog>> GetPeerLog(int? lastKnownId = null);

        #endregion Log

        #region Sync

        Task<MainData> GetMainData(int requestId);

        Task<TorrentPeers> GetTorrentPeersData(string hash, int requestId);

        #endregion Sync

        #region Transfer info

        Task<GlobalTransferInfo> GetGlobalTransferInfo();

        Task<bool> GetAlternativeSpeedLimitsState();

        Task ToggleAlternativeSpeedLimits();

        Task<long> GetGlobalDownloadLimit();

        Task SetGlobalDownloadLimit(long limit);

        Task<long> GetGlobalUploadLimit();

        Task SetGlobalUploadLimit(long limit);

        Task BanPeers(IEnumerable<PeerId> peers);

        #endregion Transfer info

        #region Torrent management

        Task<IReadOnlyList<Torrent>> GetTorrentList(string? filter = null, string? category = null, string? tag = null, string? sort = null, bool? reverse = null, int? limit = null, int? offset = null, bool? isPrivate = null, params string[] hashes);

        Task<TorrentProperties> GetTorrentProperties(string hash);

        Task<IReadOnlyList<TorrentTracker>> GetTorrentTrackers(string hash);

        Task<IReadOnlyList<WebSeed>> GetTorrentWebSeeds(string hash);

        Task<IReadOnlyList<FileData>> GetTorrentContents(string hash, params int[] indexes);

        Task<IReadOnlyList<PieceState>> GetTorrentPieceStates(string hash);

        Task<IReadOnlyList<string>> GetTorrentPieceHashes(string hash);

        Task PauseTorrents(bool? all = null, params string[] hashes);

        Task ResumeTorrents(bool? all = null, params string[] hashes);

        Task StartTorrents(bool? all = null, params string[] hashes);

        Task StopTorrents(bool? all = null, params string[] hashes);

        Task DeleteTorrents(bool? all = null, bool deleteFiles = false, params string[] hashes);

        Task RecheckTorrents(bool? all = null, params string[] hashes);

        Task ReannounceTorrents(bool? all = null, params string[] hashes);

        Task AddTorrent(AddTorrentParams addTorrentParams);

        Task AddTrackersToTorrent(string hash, IEnumerable<string> urls);

        Task EditTracker(string hash, string originalUrl, string newUrl);

        Task RemoveTrackers(string hash, IEnumerable<string> urls);

        Task AddPeers(IEnumerable<string> hashes, IEnumerable<PeerId> peers);

        Task IncreaseTorrentPriority(bool? all = null, params string[] hashes);

        Task DecreaseTorrentPriority(bool? all = null, params string[] hashes);

        Task MaxTorrentPriority(bool? all = null, params string[] hashes);

        Task MinTorrentPriority(bool? all = null, params string[] hashes);

        Task SetFilePriority(string hash, IEnumerable<int> id, Priority priority);

        Task<IReadOnlyDictionary<string, long>> GetTorrentDownloadLimit(bool? all = null, params string[] hashes);

        Task SetTorrentDownloadLimit(long limit, bool? all = null, params string[] hashes);

        Task SetTorrentShareLimit(float ratioLimit, float seedingTimeLimit, float inactiveSeedingTimeLimit, bool? all = null, params string[] hashes);

        Task<IReadOnlyDictionary<string, long>> GetTorrentUploadLimit(bool? all = null, params string[] hashes);

        Task SetTorrentUploadLimit(long limit, bool? all = null, params string[] hashes);

        Task SetTorrentLocation(string location, bool? all = null, params string[] hashes);

        Task SetTorrentName(string name, string hash);

        Task SetTorrentCategory(string category, bool? all = null, params string[] hashes);

        Task<IReadOnlyDictionary<string, Category>> GetAllCategories();

        Task AddCategory(string category, string savePath);

        Task EditCategory(string category, string savePath);

        Task RemoveCategories(params string[] categories);

        Task AddTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes);

        Task RemoveTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes);

        Task<IReadOnlyList<string>> GetAllTags();

        Task CreateTags(IEnumerable<string> tags);

        Task DeleteTags(params string[] tags);

        Task SetAutomaticTorrentManagement(bool enable, bool? all = null, params string[] hashes);

        Task ToggleSequentialDownload(bool? all = null, params string[] hashes);

        Task SetFirstLastPiecePriority(bool? all = null, params string[] hashes);

        Task SetForceStart(bool value, bool? all = null, params string[] hashes);

        Task SetSuperSeeding(bool value, bool? all = null, params string[] hashes);

        Task RenameFile(string hash, string oldPath, string newPath);

        Task RenameFolder(string hash, string oldPath, string newPath);

        Task<string> GetExportUrl(string hash);

        #endregion Torrent management

        #region RSS

        Task AddRssFolder(string path);

        Task AddRssFeed(string url, string? path = null);

        Task RemoveRssItem(string path);

        Task MoveRssItem(string itemPath, string destPath);

        Task<IReadOnlyDictionary<string, RssItem>> GetAllRssItems(bool? withData = null);

        Task MarkRssItemAsRead(string itemPath, string? articleId = null);

        Task RefreshRssItem(string itemPath);

        Task SetRssAutoDownloadingRule(string ruleName, AutoDownloadingRule ruleDef);

        Task RenameRssAutoDownloadingRule(string ruleName, string newRuleName);

        Task RemoveRssAutoDownloadingRule(string ruleName);

        Task<IReadOnlyDictionary<string, AutoDownloadingRule>> GetAllRssAutoDownloadingRules();

        Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetRssMatchingArticles(string ruleName);

        #endregion RSS

        #region Search

        Task<int> StartSearch(string pattern, IEnumerable<string> plugins, string category = "all");

        Task StopSearch(int id);

        Task<SearchStatus?> GetSearchStatus(int id);

        Task<IReadOnlyList<SearchStatus>> GetSearchesStatus();

        Task<SearchResults> GetSearchResults(int id, int? limit = null, int? offset = null);

        Task DeleteSearch(int id);

        Task<IReadOnlyList<SearchPlugin>> GetSearchPlugins();

        Task InstallSearchPlugins(params string[] sources);

        Task UninstallSearchPlugins(params string[] names);

        Task EnableSearchPlugins(params string[] names);

        Task DisableSearchPlugins(params string[] names);

        Task UpdateSearchPlugins();

        #endregion Search
    }
}