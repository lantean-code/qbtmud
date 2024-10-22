using Lantean.QBitTorrentClient.Models;
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

            await ThrowIfNotSuccessfulStatusCode(response);

            var responseContent = await response.Content.ReadAsStringAsync();
            if (responseContent == "Fails.")
            {
                throw new HttpRequestException(null, null, HttpStatusCode.BadRequest);
            }
        }

        public async Task Logout()
        {
            var response = await _httpClient.PostAsync("auth/logout", null);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        #endregion Authentication

        #region Application

        public async Task<string> GetApplicationVersion()
        {
            var response = await _httpClient.GetAsync("app/version");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAPIVersion()
        {
            var response = await _httpClient.GetAsync("app/webapiVersion");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<BuildInfo> GetBuildInfo()
        {
            var response = await _httpClient.GetAsync("app/buildInfo");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<BuildInfo>(response.Content);
        }

        public async Task Shutdown()
        {
            var response = await _httpClient.PostAsync("app/shutdown", null);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<Preferences> GetApplicationPreferences()
        {
            var response = await _httpClient.GetAsync("app/preferences");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<Preferences>(response.Content);
        }

        public async Task SetApplicationPreferences(UpdatePreferences preferences)
        {
            var json = JsonSerializer.Serialize(preferences, _options);

            var content = new FormUrlEncodedBuilder()
                .Add("json", json)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("app/setPreferences", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<string> GetDefaultSavePath()
        {
            var response = await _httpClient.GetAsync("app/defaultSavePath");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfaces()
        {
            var response = await _httpClient.GetAsync("app/networkInterfaceList");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<NetworkInterface>(response.Content);
        }

        public async Task<IReadOnlyList<string>> GetNetworkInterfaceAddressList(string @interface)
        {
            var response = await _httpClient.GetAsync($"app/networkInterfaceAddressList?iface={@interface}");

            await ThrowIfNotSuccessfulStatusCode(response);

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

            await ThrowIfNotSuccessfulStatusCode(response);

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

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<PeerLog>(response.Content);
        }

        #endregion Log

        #region Sync

        public async Task<MainData> GetMainData(int requestId)
        {
            var response = await _httpClient.GetAsync($"sync/maindata?rid={requestId}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<MainData>(response.Content);
        }

        public async Task<TorrentPeers> GetTorrentPeersData(string hash, int requestId)
        {
            var response = await _httpClient.GetAsync($"sync/torrentPeers?hash={hash}&rid={requestId}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<TorrentPeers>(response.Content);
        }

        #endregion Sync

        #region Transfer info

        public async Task<GlobalTransferInfo> GetGlobalTransferInfo()
        {
            var response = await _httpClient.GetAsync("transfer/info");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<GlobalTransferInfo>(response.Content);
        }

        public async Task<bool> GetAlternativeSpeedLimitsState()
        {
            var response = await _httpClient.GetAsync("transfer/speedLimitsMode");

            await ThrowIfNotSuccessfulStatusCode(response);

            var value = await response.Content.ReadAsStringAsync();

            return value == "1";
        }

        public async Task ToggleAlternativeSpeedLimits()
        {
            var response = await _httpClient.PostAsync("transfer/toggleSpeedLimitsMode", null);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<long> GetGlobalDownloadLimit()
        {
            var response = await _httpClient.GetAsync("transfer/downloadLimit");

            await ThrowIfNotSuccessfulStatusCode(response);

            var value = await response.Content.ReadAsStringAsync();

            return long.Parse(value);
        }

        public async Task SetGlobalDownloadLimit(long limit)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("transfer/setDownloadLimit", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<long> GetGlobalUploadLimit()
        {
            var response = await _httpClient.GetAsync("transfer/uploadLimit");

            await ThrowIfNotSuccessfulStatusCode(response);

            var value = await response.Content.ReadAsStringAsync();

            return long.Parse(value);
        }

        public async Task SetGlobalUploadLimit(long limit)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("transfer/setUploadLimit", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task BanPeers(IEnumerable<PeerId> peers)
        {
            var content = new FormUrlEncodedBuilder()
                .AddPipeSeparated("peers", peers)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("transfer/banPeers", content);

            await ThrowIfNotSuccessfulStatusCode(response);
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

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<Torrent>(response.Content);
        }

        public async Task<TorrentProperties> GetTorrentProperties(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/properties?hash={hash}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<TorrentProperties>(response.Content);
        }

        public async Task<IReadOnlyList<TorrentTracker>> GetTorrentTrackers(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/trackers?hash={hash}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<TorrentTracker>(response.Content);
        }

        public async Task<IReadOnlyList<WebSeed>> GetTorrentWebSeeds(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/webseeds?hash={hash}");

            await ThrowIfNotSuccessfulStatusCode(response);

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

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<FileData>(response.Content);
        }

        public async Task<IReadOnlyList<PieceState>> GetTorrentPieceStates(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/pieceStates?hash={hash}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<PieceState>(response.Content);
        }

        public async Task<IReadOnlyList<string>> GetTorrentPieceHashes(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/pieceHashes?hash={hash}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<string>(response.Content);
        }

        public async Task PauseTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/pause", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task StopTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/stop", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task ResumeTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/resume", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task StartTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/start", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task DeleteTorrents(bool? all = null, bool deleteFiles = false, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("deleteFiles", deleteFiles)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/delete", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RecheckTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/recheck", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task ReannounceTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/reannounce", content);

            await ThrowIfNotSuccessfulStatusCode(response);
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

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task AddTrackersToTorrent(string hash, IEnumerable<string> urls)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("urls", string.Join('\n', urls))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addTrackers", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task EditTracker(string hash, string originalUrl, string newUrl)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("originalUrl", originalUrl)
                .Add("newUrl", newUrl)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/editTracker", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RemoveTrackers(string hash, IEnumerable<string> urls)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .AddPipeSeparated("urls", urls)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeTrackers", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task AddPeers(IEnumerable<string> hashes, IEnumerable<PeerId> peers)
        {
            var content = new FormUrlEncodedBuilder()
                .AddPipeSeparated("hash", hashes)
                .AddPipeSeparated("urls", peers)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addPeers", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task IncreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/increasePrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task DecreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/decreasePrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task MaximalTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/topPrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task MinimalTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/bottomPrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetFilePriority(string hash, IEnumerable<int> id, Priority priority)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .AddPipeSeparated("id", id)
                .Add("priority", priority)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/filePrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyDictionary<string, long>> GetTorrentDownloadLimit(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/downloadLimit", content);

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonDictionary<string, long>(response.Content);
        }

        public async Task SetTorrentDownloadLimit(long limit, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setDownloadLimit", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetTorrentShareLimit(float ratioLimit, float seedingTimeLimit, float inactiveSeedingTimeLimit, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("ratioLimit", ratioLimit)
                .Add("seedingTimeLimit", seedingTimeLimit)
                .Add("inactiveSeedingTimeLimit", inactiveSeedingTimeLimit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setShareLimits", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyDictionary<string, long>> GetTorrentUploadLimit(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/uploadLimit", content);

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonDictionary<string, long>(response.Content);
        }

        public async Task SetTorrentUploadLimit(long limit, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("limit", limit)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setUploadLimit", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetTorrentLocation(string location, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("location", location)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setLocation", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetTorrentName(string name, string hash)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("name", name)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/rename", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetTorrentCategory(string category, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("category", category)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setCategory", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyDictionary<string, Category>> GetAllCategories()
        {
            var response = await _httpClient.GetAsync("torrents/categories");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonDictionary<string, Category>(response.Content);
        }

        public async Task AddCategory(string category, string savePath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("category", category)
                .Add("savePath", savePath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/createCategory", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task EditCategory(string category, string savePath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("category", category)
                .Add("savePath", savePath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/editCategory", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RemoveCategories(params string[] categories)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("categories", string.Join('\n', categories))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeCategories", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task AddTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addTags", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RemoveTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeTags", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyList<string>> GetAllTags()
        {
            var response = await _httpClient.GetAsync("torrents/tags");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<string>(response.Content);
        }

        public async Task CreateTags(IEnumerable<string> tags)
        {
            var content = new FormUrlEncodedBuilder()
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/createTags", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task DeleteTags(params string[] tags)
        {
            var content = new FormUrlEncodedBuilder()
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/deleteTags", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetAutomaticTorrentManagement(bool enable, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("enable", enable)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setAutoManagement", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task ToggleSequentialDownload(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/toggleSequentialDownload", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetFirstLastPiecePriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/toggleFirstLastPiecePrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetForceStart(bool value, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("value", value)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setForceStart", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetSuperSeeding(bool value, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .Add("value", value)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setSuperSeeding", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RenameFile(string hash, string oldPath, string newPath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("oldPath", oldPath)
                .Add("newPath", newPath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/renameFile", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RenameFolder(string hash, string oldPath, string newPath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("oldPath", oldPath)
                .Add("newPath", newPath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/renameFolder", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public Task<string> GetExportUrl(string hash)
        {
            return Task.FromResult($"{_httpClient.BaseAddress}torrents/export?hash={hash}");
        }

        #endregion Torrent management

        #region RSS

        public async Task AddRssFolder(string path)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("path", path)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/addFolder", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task AddRssFeed(string url, string? path = null)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("url", url)
                .AddIfNotNullOrEmpty("path", path)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/addFeed", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RemoveRssItem(string path)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("path", path)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/removeItem", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task MoveRssItem(string itemPath, string destPath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("itemPath", itemPath)
                .Add("destPath", destPath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/moveItem", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyDictionary<string, RssItem>> GetAllRssItems(bool? withData = null)
        {
            var content = new QueryBuilder()
                .AddIfNotNullOrEmpty("withData", withData);

            var response = await _httpClient.GetAsync("rss/items", content);

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonDictionary<string, RssItem>(response.Content);
        }

        public async Task MarkRssItemAsRead(string itemPath, string? articleId = null)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("itemPath", itemPath)
                .AddIfNotNullOrEmpty("articleId", articleId)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/markAsRead", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RefreshRssItem(string itemPath)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("itemPath", itemPath)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/refreshItem", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetRssAutoDownloadingRule(string ruleName, AutoDownloadingRule ruleDef)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("ruleName", ruleName)
                .Add("ruleDef", JsonSerializer.Serialize(ruleDef))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/setRule", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RenameRssAutoDownloadingRule(string ruleName, string newRuleName)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("ruleName", ruleName)
                .Add("newRuleName", newRuleName)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/renameRule", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RemoveRssAutoDownloadingRule(string ruleName)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("ruleName", ruleName)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/removeRule", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyDictionary<string, AutoDownloadingRule>> GetAllRssAutoDownloadingRules()
        {
            var response = await _httpClient.GetAsync("rss/rules");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonDictionary<string, AutoDownloadingRule>(response.Content);
        }

        public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetRssMatchingArticles(string ruleName)
        {
            var query = new QueryBuilder()
                .Add("ruleName", ruleName);

            var response = await _httpClient.GetAsync($"rss/matchingArticles{query}");

            await ThrowIfNotSuccessfulStatusCode(response);

            var dictionary = await GetJsonDictionary<string, IEnumerable<string>>(response.Content);

            return dictionary.ToDictionary(d => d.Key, d => (IReadOnlyList<string>)d.Value.ToList().AsReadOnly()).AsReadOnly();
        }

        #endregion RSS

        #region Search

        public async Task<int> StartSearch(string pattern, IEnumerable<string> plugins, string category = "all")
        {
            var content = new FormUrlEncodedBuilder()
                .Add("pattern", pattern)
                .AddPipeSeparated("plugins", plugins)
                .Add("category", category)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/start", content);

            await ThrowIfNotSuccessfulStatusCode(response);

            var obj = await GetJson<Dictionary<string, JsonElement>>(response.Content);

            return obj["id"].GetInt32();
        }

        public async Task StopSearch(int id)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("id", id)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/stop", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<SearchStatus?> GetSearchStatus(int id)
        {
            var query = new QueryBuilder();
            query.Add("id", id);

            var response = await _httpClient.GetAsync($"search/status{query}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await ThrowIfNotSuccessfulStatusCode(response);

            return (await GetJsonList<SearchStatus>(response.Content)).FirstOrDefault();
        }

        public async Task<IReadOnlyList<SearchStatus>> GetSearchesStatus()
        {
            var response = await _httpClient.GetAsync($"search/status");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<SearchStatus>(response.Content);
        }

        public async Task<SearchResults> GetSearchResults(int id, int? limit = null, int? offset = null)
        {
            var query = new QueryBuilder();
            query.Add("id", id);
            if (limit is not null)
            {
                query.Add("limit", limit.Value);
            }
            if (offset is not null)
            {
                query.Add("offset", offset.Value);
            }

            var response = await _httpClient.GetAsync($"search/results{query}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<SearchResults>(response.Content);
        }

        public async Task DeleteSearch(int id)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("id", id)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/delete", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyList<SearchPlugin>> GetSearchPlugins()
        {
            var response = await _httpClient.GetAsync($"search/plugins");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<SearchPlugin>(response.Content);
        }

        public async Task InstallSearchPlugins(params string[] sources)
        {
            var content = new FormUrlEncodedBuilder()
                .AddPipeSeparated("sources", sources)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/installPlugin", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task UninstallSearchPlugins(params string[] names)
        {
            var content = new FormUrlEncodedBuilder()
                .AddPipeSeparated("names", names)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/uninstallPlugin", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task EnableSearchPlugins(params string[] names)
        {
            var content = new FormUrlEncodedBuilder()
               .AddPipeSeparated("names", names)
               .Add("enable", true)
               .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/enablePlugin", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task DisableSearchPlugins(params string[] names)
        {
            var content = new FormUrlEncodedBuilder()
               .AddPipeSeparated("names", names)
               .Add("enable", false)
               .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/enablePlugin", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task UpdateSearchPlugins()
        {
            var response = await _httpClient.PostAsync("search/updatePlugins", null);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        #endregion Search

        private async Task<T> GetJson<T>(HttpContent content)
        {
            return await content.ReadFromJsonAsync<T>(_options) ?? throw new InvalidOperationException($"Unable to deserialize response as {typeof(T).Name}");
        }

        private async Task<IReadOnlyList<T>> GetJsonList<T>(HttpContent content)
        {
            try
            {
                var items = await GetJson<IEnumerable<T>>(content);

                return items.ToList().AsReadOnly();
            }
            catch
            {
                return [];
            }
        }

        private async Task<IReadOnlyDictionary<TKey, TValue>> GetJsonDictionary<TKey, TValue>(HttpContent content) where TKey : notnull
        {
            try
            {
                var items = await GetJson<IDictionary<TKey, TValue>>(content);

                return items.AsReadOnly();
            }
            catch
            {
                return new Dictionary<TKey, TValue>().AsReadOnly();
            }
        }

        private async Task<HttpResponseMessage> ThrowIfNotSuccessfulStatusCode(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }
    }
}