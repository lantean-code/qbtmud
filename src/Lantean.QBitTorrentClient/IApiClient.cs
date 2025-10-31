using Lantean.QBitTorrentClient.Models;
using System.Text.Json;

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

        Task<IReadOnlyList<ApplicationCookie>> GetApplicationCookies();

        Task SetApplicationCookies(IEnumerable<ApplicationCookie> cookies);

        Task<string> RotateApiKey();

        Task<string> GetDefaultSavePath();

        Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfaces();

        Task<IReadOnlyList<string>> GetNetworkInterfaceAddressList(string @interface);

        #endregion Application

        #region Client data

        Task<IReadOnlyDictionary<string, JsonElement>> LoadClientData(IEnumerable<string>? keys = null);

        Task StoreClientData(IReadOnlyDictionary<string, JsonElement> data);

        #endregion Client data

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

        Task<IReadOnlyList<Torrent>> GetTorrentList(string? filter = null, string? category = null, string? tag = null, string? sort = null, bool? reverse = null, int? limit = null, int? offset = null, bool? isPrivate = null, bool? includeFiles = null, params string[] hashes);

        Task<TorrentProperties> GetTorrentProperties(string hash);

        Task<IReadOnlyList<TorrentTracker>> GetTorrentTrackers(string hash);

        Task<IReadOnlyList<WebSeed>> GetTorrentWebSeeds(string hash);

        Task AddTorrentWebSeeds(string hash, IEnumerable<string> urls);

        Task EditTorrentWebSeed(string hash, string originalUrl, string newUrl);

        Task RemoveTorrentWebSeeds(string hash, IEnumerable<string> urls);

        Task<IReadOnlyList<FileData>> GetTorrentContents(string hash, params int[] indexes);

        Task<IReadOnlyList<PieceState>> GetTorrentPieceStates(string hash);

        Task<IReadOnlyList<string>> GetTorrentPieceHashes(string hash);

        Task StartTorrents(bool? all = null, params string[] hashes);

        Task StopTorrents(bool? all = null, params string[] hashes);

        Task DeleteTorrents(bool? all = null, bool deleteFiles = false, params string[] hashes);

        Task RecheckTorrents(bool? all = null, params string[] hashes);

        Task ReannounceTorrents(bool? all = null, IEnumerable<string>? trackers = null, params string[] hashes);

        Task<AddTorrentResult> AddTorrent(AddTorrentParams addTorrentParams);

        Task AddTrackersToTorrent(IEnumerable<string> urls, bool? all = null, params string[] hashes);

        Task EditTracker(string hash, string url, string? newUrl = null, int? tier = null);

        Task RemoveTrackers(IEnumerable<string> urls, bool? all = null, params string[] hashes);

        Task AddPeers(IEnumerable<string> hashes, IEnumerable<PeerId> peers);

        Task IncreaseTorrentPriority(bool? all = null, params string[] hashes);

        Task DecreaseTorrentPriority(bool? all = null, params string[] hashes);

        Task MaxTorrentPriority(bool? all = null, params string[] hashes);

        Task MinTorrentPriority(bool? all = null, params string[] hashes);

        Task SetFilePriority(string hash, IEnumerable<int> id, Priority priority);

        Task<IReadOnlyDictionary<string, long>> GetTorrentDownloadLimit(bool? all = null, params string[] hashes);

        Task SetTorrentDownloadLimit(long limit, bool? all = null, params string[] hashes);

        Task SetTorrentShareLimit(float ratioLimit, float seedingTimeLimit, float inactiveSeedingTimeLimit, ShareLimitAction? shareLimitAction = null, bool? all = null, params string[] hashes);

        Task<IReadOnlyDictionary<string, long>> GetTorrentUploadLimit(bool? all = null, params string[] hashes);

        Task SetTorrentUploadLimit(long limit, bool? all = null, params string[] hashes);

        Task SetTorrentLocation(string location, bool? all = null, params string[] hashes);

        Task SetTorrentName(string name, string hash);

        Task SetTorrentComment(IEnumerable<string> hashes, string comment);

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

        Task<TorrentMetadata?> FetchMetadata(string source, string? downloader = null);

        Task<IReadOnlyList<TorrentMetadata>> ParseMetadata(IEnumerable<(string FileName, Stream Content)> torrents);

        Task<byte[]> SaveMetadata(string source);

        #endregion Torrent management

        #region Torrent creator

        Task<string> AddTorrentCreationTask(TorrentCreationTaskRequest request);

        Task<IReadOnlyList<TorrentCreationTaskStatus>> GetTorrentCreationTasks(string? taskId = null);

        Task<byte[]> GetTorrentCreationTaskFile(string taskId);

        Task DeleteTorrentCreationTask(string taskId);

        #endregion Torrent creator

        #region RSS

        Task AddRssFolder(string path);

        Task AddRssFeed(string url, string? path = null);

        Task RemoveRssItem(string path);

        Task MoveRssItem(string itemPath, string destPath);

        Task SetRssFeedUrl(string path, string url);

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
