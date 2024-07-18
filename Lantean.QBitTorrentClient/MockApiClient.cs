using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBitTorrentClient
{
    public class MockApiClient : IApiClient
    {
        private readonly ApiClient _apiClient;

        public MockApiClient(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task AddCategory(string category, string savePath)
        {
            return _apiClient.AddCategory(category, savePath);
        }

        public Task AddPeers(IEnumerable<string> hashes, IEnumerable<PeerId> peers)
        {
            return _apiClient.AddPeers(hashes, peers);
        }

        public Task AddTorrent(IEnumerable<string>? urls = null, Dictionary<string, Stream>? torrents = null, string? savePath = null, string? cookie = null, string? category = null, IEnumerable<string>? tags = null, bool? skipChecking = null, bool? paused = null, string? contentLayout = null, string? renameTorrent = null, long? uploadLimit = null, long? downloadLimit = null, float? ratioLimit = null, int? seedingTimeLimit = null, bool? autoTorrentManagement = null, bool? sequentialDownload = null, bool? firstLastPiecePriority = null)
        {
            return _apiClient.AddTorrent(urls, torrents, savePath, cookie, category, tags, skipChecking, paused, contentLayout, renameTorrent, uploadLimit, downloadLimit, ratioLimit, seedingTimeLimit, autoTorrentManagement, sequentialDownload, firstLastPiecePriority);
        }

        public Task AddTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes)
        {
            return _apiClient.AddTorrentTags(tags, all, hashes);
        }

        public Task AddTrackersToTorrent(string hash, IEnumerable<string> urls)
        {
            return _apiClient.AddTrackersToTorrent(hash, urls);
        }

        public Task BanPeers(IEnumerable<PeerId> peers)
        {
            return _apiClient.BanPeers(peers);
        }

        public Task<bool> CheckAuthState()
        {
            return _apiClient.CheckAuthState();
        }

        public Task CreateTags(IEnumerable<string> tags)
        {
            return _apiClient.CreateTags(tags);
        }

        public Task DecreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            return _apiClient.DecreaseTorrentPriority(all, hashes);
        }

        public Task DeleteTags(params string[] tags)
        {
            return _apiClient.DeleteTags(tags);
        }

        public Task DeleteTorrents(bool? all = null, bool deleteFiles = false, params string[] hashes)
        {
            return _apiClient.DeleteTorrents(all, deleteFiles, hashes);
        }

        public Task EditCategory(string category, string savePath)
        {
            return _apiClient.EditCategory(category, savePath);
        }

        public Task EditTracker(string hash, string originalUrl, string newUrl)
        {
            return _apiClient.EditTracker(hash, originalUrl, newUrl);
        }

        public Task<string> GetExportUrl(string hash)
        {
            return _apiClient.GetExportUrl(hash);
        }

        public Task<IReadOnlyDictionary<string, Category>> GetAllCategories()
        {
            return _apiClient.GetAllCategories();
        }

        public Task<IReadOnlyList<string>> GetAllTags()
        {
            return _apiClient.GetAllTags();
        }

        public Task<bool> GetAlternativeSpeedLimitsState()
        {
            return _apiClient.GetAlternativeSpeedLimitsState();
        }

        public Task<string> GetAPIVersion()
        {
            return _apiClient.GetAPIVersion();
        }

        public Task<Preferences> GetApplicationPreferences()
        {
            return _apiClient.GetApplicationPreferences();
        }

        public Task<string> GetApplicationVersion()
        {
            return _apiClient.GetApplicationVersion();
        }

        public Task<BuildInfo> GetBuildInfo()
        {
            return _apiClient.GetBuildInfo();
        }

        public Task<string> GetDefaultSavePath()
        {
            return _apiClient.GetDefaultSavePath();
        }

        public Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfaces()
        {
            return _apiClient.GetNetworkInterfaces();
        }

        public Task<IReadOnlyList<string>> GetNetworkInterfaceAddressList(string @interface)
        {
            return _apiClient.GetNetworkInterfaceAddressList(@interface);
        }

        public Task<long> GetGlobalDownloadLimit()
        {
            return _apiClient.GetGlobalDownloadLimit();
        }

        public Task<GlobalTransferInfo> GetGlobalTransferInfo()
        {
            return _apiClient.GetGlobalTransferInfo();
        }

        public Task<long> GetGlobalUploadLimit()
        {
            return _apiClient.GetGlobalUploadLimit();
        }

        public Task<IReadOnlyList<Log>> GetLog(bool? normal = null, bool? info = null, bool? warning = null, bool? critical = null, int? lastKnownId = null)
        {
            return _apiClient.GetLog(normal, info, warning, critical, lastKnownId);
        }

        public Task<MainData> GetMainData(int requestId)
        {
            return _apiClient.GetMainData(requestId);
        }

        public Task<IReadOnlyList<PeerLog>> GetPeerLog(int? lastKnownId = null)
        {
            return _apiClient.GetPeerLog(lastKnownId);
        }

        public Task<IReadOnlyList<FileData>> GetTorrentContents(string hash, params int[] indexes)
        {
            var list = new List<FileData>();
            list.Add(new FileData(2, "slackware-14.2-iso/slackware-14.2-source-d6.iso", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(3, "slackware-14.2-iso/slackware-14.2-source-d6.iso.asc", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(4, "slackware-14.2-iso/slackware-14.2-source-d6.iso.md5", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(5, "slackware-14.2-iso/slackware-14.2-source-d6.iso.txt", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(6, "slackware-14.2-iso/temp/slackware-14.2-source-d6.iso.md5", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(7, "slackware-14.2-iso/temp/slackware-14.2-source-d6.iso.txt", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(8, "slackware-14.2-iso2/slackware-14.2-source-d6.iso2", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(9, "slackware-14.2-iso2/slackware-14.2-source-d6.iso2.asc", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(10, "slackware-14.2-iso2/slackware-14.2-source-d6.iso2.md5", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(11, "slackware-14.2-iso2/slackware-14.2-source-d6.iso2.txt", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(12, "really/long/directory/path/is/here/file.txt", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            list.Add(new FileData(13, "other.txt", 500, 0f, Priority.Normal, false, [1, 2], 0f));
            return Task.FromResult<IReadOnlyList<FileData>>(list);
        }

        public Task<IReadOnlyDictionary<string, long>> GetTorrentDownloadLimit(bool? all = null, params string[] hashes)
        {
            return _apiClient.GetTorrentDownloadLimit(all, hashes);
        }

        public Task<IReadOnlyList<Torrent>> GetTorrentList(string? filter = null, string? category = null, string? tag = null, string? sort = null, bool? reverse = null, int? limit = null, int? offset = null, params string[] hashes)
        {
            return _apiClient.GetTorrentList(filter, category, tag, sort, reverse, limit, offset, hashes);
        }

        public Task<TorrentPeers> GetTorrentPeersData(string hash, int requestId)
        {
            return _apiClient.GetTorrentPeersData(hash, requestId);
        }

        public Task<IReadOnlyList<string>> GetTorrentPieceHashes(string hash)
        {
            return _apiClient.GetTorrentPieceHashes(hash);
        }

        public Task<IReadOnlyList<PieceState>> GetTorrentPieceStates(string hash)
        {
            return _apiClient.GetTorrentPieceStates(hash);
        }

        public Task<TorrentProperties> GetTorrentProperties(string hash)
        {
            return _apiClient.GetTorrentProperties(hash);
        }

        public Task<IReadOnlyList<TorrentTrackers>> GetTorrentTrackers(string hash)
        {
            return _apiClient.GetTorrentTrackers(hash);
        }

        public Task<IReadOnlyDictionary<string, long>> GetTorrentUploadLimit(bool? all = null, params string[] hashes)
        {
            return _apiClient.GetTorrentUploadLimit(all, hashes);
        }

        public Task<IReadOnlyList<WebSeed>> GetTorrentWebSeeds(string hash)
        {
            return _apiClient.GetTorrentWebSeeds(hash);
        }

        public Task IncreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            return _apiClient.IncreaseTorrentPriority(all, hashes);
        }

        public Task Login(string username, string password)
        {
            return _apiClient.Login(username, password);
        }

        public Task Logout()
        {
            return _apiClient.Logout();
        }

        public Task MaximalTorrentPriority(bool? all = null, params string[] hashes)
        {
            return _apiClient.MaximalTorrentPriority(all, hashes);
        }

        public Task MinimalTorrentPriority(bool? all = null, params string[] hashes)
        {
            return _apiClient.MinimalTorrentPriority(all, hashes);
        }

        public Task PauseTorrents(bool? all = null, params string[] hashes)
        {
            return _apiClient.PauseTorrents(all, hashes);
        }

        public Task ReannounceTorrents(bool? all = null, params string[] hashes)
        {
            return _apiClient.ReannounceTorrents(all, hashes);
        }

        public Task RecheckTorrents(bool? all = null, params string[] hashes)
        {
            return _apiClient.ReannounceTorrents(all, hashes);
        }

        public Task RemoveCategories(params string[] categories)
        {
            return _apiClient.RemoveCategories(categories);
        }

        public Task RemoveTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes)
        {
            return _apiClient.RemoveTorrentTags(tags, all, hashes);
        }

        public Task RemoveTrackers(string hash, IEnumerable<string> urls)
        {
            return _apiClient.RemoveTrackers(hash, urls);
        }

        public Task RenameFile(string hash, string oldPath, string newPath)
        {
            return _apiClient.RenameFile(hash, oldPath, newPath);
        }

        public Task RenameFolder(string hash, string oldPath, string newPath)
        {
            return _apiClient.RenameFolder(hash, oldPath, newPath);
        }

        public Task ResumeTorrents(bool? all = null, params string[] hashes)
        {
            return _apiClient.ResumeTorrents(all, hashes);
        }

        public Task SetApplicationPreferences(UpdatePreferences preferences)
        {
            return _apiClient.SetApplicationPreferences(preferences);
        }

        public Task SetAutomaticTorrentManagement(bool enable, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetAutomaticTorrentManagement(enable, all, hashes);
        }

        public Task SetFilePriority(string hash, IEnumerable<int> id, Priority priority)
        {
            return _apiClient.SetFilePriority(hash, id, priority);
        }

        public Task SetFirstLastPiecePriority(bool? all = null, params string[] hashes)
        {
            return _apiClient.SetFirstLastPiecePriority(all, hashes);
        }

        public Task SetForceStart(bool value, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetForceStart(value, all, hashes);
        }

        public Task SetGlobalDownloadLimit(long limit)
        {
            return _apiClient.SetGlobalDownloadLimit(limit);
        }

        public Task SetGlobalUploadLimit(long limit)
        {
            return _apiClient.SetGlobalDownloadLimit(limit);
        }

        public Task SetSuperSeeding(bool value, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetSuperSeeding(value, all, hashes);
        }

        public Task SetTorrentCategory(string category, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetTorrentCategory(category, all, hashes);
        }

        public Task SetTorrentDownloadLimit(long limit, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetTorrentDownloadLimit(limit, all, hashes);
        }

        public Task SetTorrentLocation(string location, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetTorrentLocation(location, all, hashes);
        }

        public Task SetTorrentName(string name, string hash)
        {
            return _apiClient.SetTorrentName(name, hash);
        }

        public Task SetTorrentShareLimit(float ratioLimit, float seedingTimeLimit, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetTorrentShareLimit(ratioLimit, seedingTimeLimit, all, hashes);
        }

        public Task SetTorrentUploadLimit(long limit, bool? all = null, params string[] hashes)
        {
            return _apiClient.SetTorrentUploadLimit(limit, all, hashes);
        }

        public Task Shutdown()
        {
            return _apiClient.Shutdown();
        }

        public Task ToggleAlternativeSpeedLimits()
        {
            return _apiClient.ToggleAlternativeSpeedLimits();
        }

        public Task ToggleSequentialDownload(bool? all = null, params string[] hashes)
        {
            return _apiClient.ToggleSequentialDownload(all, hashes);
        }

        public Task<int> StartSearch(string pattern, IEnumerable<string> plugins, string category = "all")
        {
            return _apiClient.StartSearch(pattern, plugins, category);
        }

        public Task StopSearch(int id)
        {
            return _apiClient.StopSearch(id);
        }

        public Task<SearchStatus?> GetSearchStatus(int id)
        {
            return _apiClient.GetSearchStatus(id);
        }

        public Task<IReadOnlyList<SearchStatus>> GetSearchesStatus()
        {
            return _apiClient.GetSearchesStatus();
        }

        public Task<SearchResults> GetSearchResults(int id, int? limit = null, int? offset = null)
        {
            return _apiClient.GetSearchResults(id, limit, offset);
        }

        public Task DeleteSearch(int id)
        {
            return _apiClient.DeleteSearch(id);
        }

        public Task<IReadOnlyList<SearchPlugin>> GetSearchPlugins()
        {
            return _apiClient.GetSearchPlugins();
        }

        public Task InstallSearchPlugins(params string[] sources)
        {
            return _apiClient.InstallSearchPlugins(sources);
        }

        public Task UninstallSearchPlugins(params string[] names)
        {
            return _apiClient.UninstallSearchPlugins(names);
        }

        public Task EnableSearchPlugins(params string[] names)
        {
            return _apiClient.EnableSearchPlugins(names);
        }

        public Task DisableSearchPlugins(params string[] names)
        {
            return _apiClient.DisableSearchPlugins(names);
        }

        public Task UpdateSearchPlugins()
        {
            return _apiClient.UpdateSearchPlugins();
        }
    }
}