using Lantean.QBitTorrentClient.Models;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Lantean.QBitTorrentClient
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _options = SerializerOptions.Options;

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        #region Authentication

        public async Task<bool> CheckAuthState()
        {
            try
            {
                var response = await _httpClient.GetAsync("app/version");
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        public async Task Login(string username, string password)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("username", username)
                .Add("password", password)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("auth/login", content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            if (responseContent == "Fails.")
            {
                throw new HttpRequestException(null, null, HttpStatusCode.BadRequest);
            }
        }

        public async Task Logout()
        {
            var response = await _httpClient.PostAsync("auth/logout", null);

            response.EnsureSuccessStatusCode();
        }

        #endregion Authentication

        #region Application

        public async Task<string> GetApplicationVersion()
        {
            var response = await _httpClient.GetAsync("app/version");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAPIVersion()
        {
            var response = await _httpClient.GetAsync("app/webapiVersion");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<BuildInfo> GetBuildInfo()
        {
            var response = await _httpClient.GetAsync("app/buildInfo");

            response.EnsureSuccessStatusCode();

            return await GetJson<BuildInfo>(response.Content);
        }

        public async Task Shutdown()
        {
            var response = await _httpClient.PostAsync("app/shutdown", null);

            response.EnsureSuccessStatusCode();
        }

        public async Task<Preferences> GetApplicationPreferences()
        {
            var response = await _httpClient.GetAsync("app/preferences");

            response.EnsureSuccessStatusCode();

            return await GetJson<Preferences>(response.Content);
        }

        public async Task SetApplicationPreferences(UpdatePreferences preferences)
        {
            var json = JsonSerializer.Serialize(preferences, _options);

            var content = new FormUrlEncodedBuilder()
                .Add("json", json)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("app/setPreferences", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task<string> GetDefaultSavePath()
        {
            var response = await _httpClient.GetAsync("app/defaultSavePath");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfaces()
        {
            var response = await _httpClient.GetAsync("app/networkInterfaceList");

            response.EnsureSuccessStatusCode();

            return await GetJsonList<NetworkInterface>(response.Content);
        }

        public async Task<IReadOnlyList<string>> GetNetworkInterfaceAddressList(string @interface)
        {
            var response = await _httpClient.GetAsync($"app/networkInterfaceAddressList?iface={@interface}");

            response.EnsureSuccessStatusCode();

            return await GetJsonList<string>(response.Content);
        }

        #endregion Application

        #region Log

        public async Task<IReadOnlyList<Log>> GetLog(bool? normal = null, bool? info = null, bool? warning = null, bool? critical = null, int? lastKnownId = null)
        {
            var query = new QueryBuilder();
            if (normal is not null)
            {
                query.Add("normal", normal.Value);
            }
            if (info is not null)
            {
                query.Add("info", info.Value);
            }
            if (warning is not null)
            {
                query.Add("warning", warning.Value);
            }
            if (critical is not null)
            {
                query.Add("critical", critical.Value);
            }
            if (lastKnownId is not null)
            {
                query.Add("last_known_id", lastKnownId.Value);
            }

            var response = await _httpClient.GetAsync($"log/main", query);

            response.EnsureSuccessStatusCode();

            return await GetJsonList<Log>(response.Content);
        }

        public async Task<IReadOnlyList<PeerLog>> GetPeerLog(int? lastKnownId = null)
        {
            var query = new QueryBuilder();
            if (lastKnownId is not null)
            {
                query.Add("last_known_id", lastKnownId.Value);
            }

            var response = await _httpClient.GetAsync($"log/peers", query);

            response.EnsureSuccessStatusCode();

            return await GetJsonList<PeerLog>(response.Content);
        }

        #endregion Log

        #region Sync

        public async Task<MainData> GetMainData(int requestId)
        {
            var response = await _httpClient.GetAsync($"sync/maindata?rid={requestId}");

            response.EnsureSuccessStatusCode();

            return await GetJson<MainData>(response.Content);
        }

        public async Task<TorrentPeers> GetTorrentPeersData(string hash, int requestId)
        {
            var response = await _httpClient.GetAsync($"sync/torrentPeers?hash={hash}&rid={requestId}");

            response.EnsureSuccessStatusCode();

            return await GetJson<TorrentPeers>(response.Content);
        }

        #endregion Sync

        #region Transfer info

        public async Task<GlobalTransferInfo> GetGlobalTransferInfo()
        {
            var response = await _httpClient.GetAsync("transfer/info");

            response.EnsureSuccessStatusCode();

            return await GetJson<GlobalTransferInfo>(response.Content);
        }

        public async Task<bool> GetAlternativeSpeedLimitsState()
        {
            var response = await _httpClient.GetAsync("transfer/speedLimitsMode");

            response.EnsureSuccessStatusCode();

            var value = await response.Content.ReadAsStringAsync();

            return value == "1";
        }

        public async Task ToggleAlternativeSpeedLimits()
        {
            var response = await _httpClient.PostAsync("transfer/toggleSpeedLimitsMode", null);

            response.EnsureSuccessStatusCode();
        }

        public async Task<long> GetGlobalDownloadLimit()
        {
            var response = await _httpClient.GetAsync("transfer/downloadLimit");

            response.EnsureSuccessStatusCode();

            var value = await response.Content.ReadAsStringAsync();

            return long.Parse(value);
        }

        public async Task SetGlobalDownloadLimit(long limit)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("transfer/setDownloadLimit", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task<long> GetGlobalUploadLimit()
        {
            var response = await _httpClient.GetAsync("transfer/uploadLimit");

            response.EnsureSuccessStatusCode();

            var value = await response.Content.ReadAsStringAsync();

            return long.Parse(value);
        }

        public async Task SetGlobalUploadLimit(long limit)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("transfer/setUploadLimit", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task BanPeers(IEnumerable<PeerId> peers)
        {
            var content = new FormUrlEncodedBuilder()
                .AddPipeSeparated("peers", peers)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("transfer/banPeers", content);

            response.EnsureSuccessStatusCode();
        }

        #endregion Transfer info

        #region Torrent management

        public async Task<IReadOnlyList<Torrent>> GetTorrentList(string? filter = null, string? category = null, string? tag = null, string? sort = null, bool? reverse = null, int? limit = null, int? offset = null, params string[] hashes)
        {
            var query = new QueryBuilder();
            if (filter is not null)
            {
                query.Add("filter", filter);
            }
            if (category is not null)
            {
                query.Add("category", category);
            }
            if (tag is not null)
            {
                query.Add("tag", tag);
            }
            if (sort is not null)
            {
                query.Add("sort", sort);
            }
            if (reverse is not null)
            {
                query.Add("reverse", reverse.Value);
            }
            if (limit is not null)
            {
                query.Add("limit", limit.Value);
            }
            if (offset is not null)
            {
                query.Add("offset", offset.Value);
            }
            if (hashes.Length > 0)
            {
                query.Add("hashes", string.Join('|', hashes));
            }

            var response = await _httpClient.GetAsync("torrents/info", query);

            response.EnsureSuccessStatusCode();

            return await GetJsonList<Torrent>(response.Content);
        }

        public async Task<TorrentProperties> GetTorrentProperties(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/properties?hash={hash}");

            response.EnsureSuccessStatusCode();

            return await GetJson<TorrentProperties>(response.Content);
        }

        public async Task<IReadOnlyList<TorrentTrackers>> GetTorrentTrackers(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/trackers?hash={hash}");

            response.EnsureSuccessStatusCode();

            return await GetJsonList<TorrentTrackers>(response.Content);
        }

        public async Task<IReadOnlyList<WebSeed>> GetTorrentWebSeeds(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/webseeds?hash={hash}");

            response.EnsureSuccessStatusCode();

            return await GetJsonList<WebSeed>(response.Content);
        }

        public async Task<IReadOnlyList<FileData>> GetTorrentContents(string hash, params int[] indexes)
        {
            var query = new QueryBuilder();
            query.Add("hash", hash);
            if (indexes.Length > 0)
            {
                query.Add("indexes", string.Join('|', indexes));
            }
            var response = await _httpClient.GetAsync("torrents/files", query);

            response.EnsureSuccessStatusCode();

            return await GetJsonList<FileData>(response.Content);
        }

        public async Task<IReadOnlyList<PieceState>> GetTorrentPieceStates(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/pieceStates?hash={hash}");

            response.EnsureSuccessStatusCode();

            return await GetJsonList<PieceState>(response.Content);
        }

        public async Task<IReadOnlyList<string>> GetTorrentPieceHashes(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/pieceHashes?hash={hash}");

            response.EnsureSuccessStatusCode();

            return await GetJsonList<string>(response.Content);
        }

        public async Task PauseTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/pause", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task ResumeTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/resume", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteTorrents(bool? all = null, bool deleteFiles = false, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("deleteFiles", deleteFiles)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/delete", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task RecheckTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/recheck", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task ReannounceTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/reannounce", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task AddTorrent(IEnumerable<string>? urls = null, Dictionary<string, Stream>? torrents = null, string? savePath = null, string? cookie = null, string? category = null, IEnumerable<string>? tags = null, bool? skipChecking = null, bool? paused = null, string? contentLayout = null, string? renameTorrent = null, long? uploadLimit = null, long? downloadLimit = null, float? ratioLimit = null, int? seedingTimeLimit = null, bool? autoTorrentManagement = null, bool? sequentialDownload = null, bool? firstLastPiecePriority = null)
        {
            var content = new MultipartFormDataContent();
            if (urls is not null)
            {
                content.AddString("urls", string.Join('\n', urls));
            }
            if (torrents is not null)
            {
                foreach (var (name, stream) in torrents)
                {
                    content.Add(new StreamContent(stream), "torrents", name);
                }
            }
            if (savePath is not null)
            {
                content.AddString("savepath", savePath);
            }
            if (cookie is not null)
            {
                content.AddString("cookie", cookie);
            }
            if (category is not null)
            {
                content.AddString("category", category);
            }
            if (tags is not null)
            {
                content.AddString("tags", string.Join(',', tags));
            }
            if (skipChecking is not null)
            {
                content.AddString("skip_checking", skipChecking.Value);
            }
            if (paused is not null)
            {
                content.AddString("paused", paused.Value);
            }
            if (contentLayout is not null)
            {
                content.AddString("contentLayout", contentLayout);
            }
            if (renameTorrent is not null)
            {
                content.AddString("rename", renameTorrent);
            }
            if (uploadLimit is not null)
            {
                content.AddString("upLimit", uploadLimit.Value);
            }
            if (downloadLimit is not null)
            {
                content.AddString("dlLimit", downloadLimit.Value);
            }
            if (ratioLimit is not null)
            {
                content.AddString("ratioLimit", ratioLimit.Value);
            }
            if (seedingTimeLimit is not null)
            {
                content.AddString("seedingTimeLimit", seedingTimeLimit.Value);
            }
            if (autoTorrentManagement is not null)
            {
                content.AddString("autoTMM", autoTorrentManagement.Value);
            }
            if (sequentialDownload is not null)
            {
                content.AddString("sequentialDownload", sequentialDownload.Value);
            }
            if (firstLastPiecePriority is not null)
            {
                content.AddString("firstLastPiecePrio", firstLastPiecePriority.Value);
            }

            var response = await _httpClient.PostAsync("torrents/add", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task AddTrackersToTorrent(string hash, IEnumerable<string> urls)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("urls", string.Join('\n', urls))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addTrackers", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task EditTracker(string hash, string originalUrl, string newUrl)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("originalUrl", originalUrl)
                .Add("newUrl", newUrl)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/editTracker", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveTrackers(string hash, IEnumerable<string> urls)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .AddPipeSeparated("urls", urls)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeTrackers", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task AddPeers(IEnumerable<string> hashes, IEnumerable<PeerId> peers)
        {
            var content = new FormUrlEncodedBuilder()
                .AddPipeSeparated("hash", hashes)
                .AddPipeSeparated("urls", peers)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addPeers", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task IncreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/increasePrio", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task DecreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/decreasePrio", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task MaximalTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/topPrio", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task MinimalTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/bottomPrio", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetFilePriority(string hash, IEnumerable<int> id, Priority priority)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .AddPipeSeparated("id", id)
                .Add("priority", priority)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/filePrio", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyDictionary<string, long>> GetTorrentDownloadLimit(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/downloadLimit", content);

            response.EnsureSuccessStatusCode();

            return await GetJsonDictionary<string, long>(response.Content);
        }

        public async Task SetTorrentDownloadLimit(long limit, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setDownloadLimit", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetTorrentShareLimit(float ratioLimit, float seedingTimeLimit, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("ratioLimit", ratioLimit)
                .Add("seedingTimeLimit", seedingTimeLimit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setShareLimits", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyDictionary<string, long>> GetTorrentUploadLimit(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/uploadLimit", content);

            response.EnsureSuccessStatusCode();

            return await GetJsonDictionary<string, long>(response.Content);
        }

        public async Task SetTorrentUploadLimit(long limit, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setUploadLimit", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetTorrentLocation(string location, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("location", location)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setLocation", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetTorrentName(string name, string hash)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("name", name)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/rename", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetTorrentCategory(string category, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("category", category)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setCategory", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyDictionary<string, Category>> GetAllCategories()
        {
            var response = await _httpClient.GetAsync("torrents/categories");

            response.EnsureSuccessStatusCode();

            return await GetJsonDictionary<string, Category>(response.Content);
        }

        public async Task AddCategory(string category, string savePath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("category", category)
                .Add("savePath", savePath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/createCategory", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task EditCategory(string category, string savePath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("category", category)
                .Add("savePath", savePath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/editCategory", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveCategories(params string[] categories)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("categories", string.Join('\n', categories))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeCategories", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task AddTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addTags", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeTags", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<string>> GetAllTags()
        {
            var response = await _httpClient.GetAsync("torrents/tags");

            response.EnsureSuccessStatusCode();

            return await GetJsonList<string>(response.Content);
        }

        public async Task CreateTags(IEnumerable<string> tags)
        {
            var content = new FormUrlEncodedBuilder()
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/createTags", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteTags(IEnumerable<string> tags)
        {
            var content = new FormUrlEncodedBuilder()
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/deleteTags", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetAutomaticTorrentManagement(bool enable, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("enable", enable)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setAutoManagement", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task ToggleSequentialDownload(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/toggleSequentialDownload", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetFirstLastPiecePriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/toggleFirstLastPiecePrio", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetForceStart(bool value, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("enable", value)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setFOrceStart", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task SetSuperSeeding(bool value, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("enable", value)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setSuperSeeding", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task RenameFile(string hash, string oldPath, string newPath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("oldPath", oldPath)
                .Add("newPath", newPath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/renameFile", content);

            response.EnsureSuccessStatusCode();
        }

        public async Task RenameFolder(string hash, string oldPath, string newPath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("oldPath", oldPath)
                .Add("newPath", newPath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/renameFolder", content);

            response.EnsureSuccessStatusCode();
        }

        public Task<string> GetExportUrl(string hash)
        {
            return Task.FromResult($"{_httpClient.BaseAddress}torrents/export?hash={hash}");
        }

        #endregion Torrent management

        #region RSS

        // not implementing RSS right now

        #endregion RSS

        #region Search

        // not implementing Search right now

        #endregion Search

        private async Task<T> GetJson<T>(HttpContent content)
        {
            return await content.ReadFromJsonAsync<T>(_options) ?? throw new InvalidOperationException($"Unable to deserialize response as {typeof(T).Name}");
        }

        private async Task<IReadOnlyList<T>> GetJsonList<T>(HttpContent content)
        {
            var items = await GetJson<IEnumerable<T>>(content);

            return items.ToList().AsReadOnly();
        }

        private async Task<IReadOnlyDictionary<TKey, TValue>> GetJsonDictionary<TKey, TValue>(HttpContent content) where TKey : notnull
        {
            var items = await GetJson<IDictionary<TKey, TValue>>(content);

            return items.AsReadOnly();
        }
    }
}