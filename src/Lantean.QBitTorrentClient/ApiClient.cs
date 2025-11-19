using Lantean.QBitTorrentClient.Models;
using System.Globalization;
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
            preferences.Validate();

            var json = JsonSerializer.Serialize(preferences, _options);

            var content = new FormUrlEncodedBuilder()
                .Add("json", json)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("app/setPreferences", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyList<ApplicationCookie>> GetApplicationCookies()
        {
            var response = await _httpClient.GetAsync("app/cookies");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<ApplicationCookie>(response.Content);
        }

        public async Task SetApplicationCookies(IEnumerable<ApplicationCookie> cookies)
        {
            var json = JsonSerializer.Serialize(cookies, _options);

            var content = new FormUrlEncodedBuilder()
                .Add("cookies", json)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("app/setCookies", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SendTestEmail()
        {
            var response = await _httpClient.PostAsync("app/sendTestEmail", null);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<IReadOnlyList<string>> GetDirectoryContent(string directoryPath, DirectoryContentMode mode = DirectoryContentMode.All)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

            var query = new QueryBuilder()
                .Add("dirPath", directoryPath)
                .Add("mode", mode switch
                {
                    DirectoryContentMode.Directories => "dirs",
                    DirectoryContentMode.Files => "files",
                    _ => "all"
                });

            var response = await _httpClient.GetAsync("app/getDirectoryContent", query);

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<string>(response.Content);
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
            var query = new QueryBuilder()
                .Add("iface", @interface);

            var response = await _httpClient.GetAsync("app/networkInterfaceAddressList", query);

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

        public async Task SetAlternativeSpeedLimitsState(bool enabled)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("mode", enabled ? 1 : 0)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("transfer/setSpeedLimitsMode", content);

            await ThrowIfNotSuccessfulStatusCode(response);
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

        public async Task<IReadOnlyList<Torrent>> GetTorrentList(string? filter = null, string? category = null, string? tag = null, string? sort = null, bool? reverse = null, int? limit = null, int? offset = null, bool? isPrivate = null, bool? includeFiles = null, bool? includeTrackers = null, params string[] hashes)
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
            if (isPrivate is not null)
            {
                query.Add("private", isPrivate.Value ? "true" : "false");
            }
            if (includeFiles is not null)
            {
                query.Add("includeFiles", includeFiles.Value ? "true" : "false");
            }
            if (includeTrackers is not null)
            {
                query.Add("includeTrackers", includeTrackers.Value ? "true" : "false");
            }

            var response = await _httpClient.GetAsync("torrents/info", query);

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<Torrent>(response.Content);
        }

        public async Task<int> GetTorrentCount()
        {
            var response = await _httpClient.GetAsync("torrents/count");

            await ThrowIfNotSuccessfulStatusCode(response);

            var payload = await response.Content.ReadAsStringAsync();
            return int.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0;
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

        public async Task AddTorrentWebSeeds(string hash, IEnumerable<string> urls)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("urls", string.Join('|', urls))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addWebSeeds", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task EditTorrentWebSeed(string hash, string originalUrl, string newUrl)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("origUrl", originalUrl)
                .Add("newUrl", newUrl)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/editWebSeed", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RemoveTorrentWebSeeds(string hash, IEnumerable<string> urls)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("urls", string.Join('|', urls))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeWebSeeds", content);

            await ThrowIfNotSuccessfulStatusCode(response);
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

        public async Task StopTorrents(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/stop", content);

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

        public async Task ReannounceTorrents(bool? all = null, IEnumerable<string>? trackers = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/reannounce", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task<AddTorrentResult> AddTorrent(AddTorrentParams addTorrentParams)
        {
            var content = new MultipartFormDataContent();

            if (addTorrentParams.Urls?.Any() == true)
            {
                content.AddString("urls", string.Join('\n', addTorrentParams.Urls));
            }

            if (addTorrentParams.Torrents is not null)
            {
                foreach (var (name, stream) in addTorrentParams.Torrents)
                {
                    content.Add(new StreamContent(stream), "torrents", name);
                }
            }

            if (addTorrentParams.SkipChecking is not null)
            {
                content.AddString("skip_checking", addTorrentParams.SkipChecking.Value);
            }
            if (addTorrentParams.SequentialDownload is not null)
            {
                content.AddString("sequentialDownload", addTorrentParams.SequentialDownload.Value);
            }
            if (addTorrentParams.FirstLastPiecePriority is not null)
            {
                content.AddString("firstLastPiecePrio", addTorrentParams.FirstLastPiecePriority.Value);
            }
            if (addTorrentParams.AddToTopOfQueue is not null)
            {
                content.AddString("addToTopOfQueue", addTorrentParams.AddToTopOfQueue.Value);
            }
            if (addTorrentParams.Forced is not null)
            {
                content.AddString("forced", addTorrentParams.Forced.Value);
            }
            if (addTorrentParams.Stopped is not null)
            {
                content.AddString("stopped", addTorrentParams.Stopped.Value);
            }
            if (addTorrentParams.SavePath is not null)
            {
                content.AddString("savepath", addTorrentParams.SavePath);
            }
            if (addTorrentParams.DownloadPath is not null)
            {
                content.AddString("downloadPath", addTorrentParams.DownloadPath);
            }
            if (addTorrentParams.UseDownloadPath is not null)
            {
                content.AddString("useDownloadPath", addTorrentParams.UseDownloadPath.Value);
            }
            if (addTorrentParams.Category is not null)
            {
                content.AddString("category", addTorrentParams.Category);
            }
            if (addTorrentParams.Tags is not null)
            {
                content.AddString("tags", string.Join(',', addTorrentParams.Tags));
            }
            if (addTorrentParams.RenameTorrent is not null)
            {
                content.AddString("rename", addTorrentParams.RenameTorrent);
            }
            if (addTorrentParams.UploadLimit is not null)
            {
                content.AddString("upLimit", addTorrentParams.UploadLimit.Value);
            }
            if (addTorrentParams.DownloadLimit is not null)
            {
                content.AddString("dlLimit", addTorrentParams.DownloadLimit.Value);
            }
            if (addTorrentParams.RatioLimit is not null)
            {
                content.AddString("ratioLimit", addTorrentParams.RatioLimit.Value);
            }
            if (addTorrentParams.SeedingTimeLimit is not null)
            {
                content.AddString("seedingTimeLimit", addTorrentParams.SeedingTimeLimit.Value);
            }
            if (addTorrentParams.InactiveSeedingTimeLimit is not null)
            {
                content.AddString("inactiveSeedingTimeLimit", addTorrentParams.InactiveSeedingTimeLimit.Value);
            }
            if (addTorrentParams.ShareLimitAction is not null)
            {
                content.AddString("shareLimitAction", addTorrentParams.ShareLimitAction.Value);
            }
            if (addTorrentParams.AutoTorrentManagement is not null)
            {
                content.AddString("autoTMM", addTorrentParams.AutoTorrentManagement.Value);
            }
            if (addTorrentParams.StopCondition is not null)
            {
                content.AddString("stopCondition", addTorrentParams.StopCondition.Value);
            }
            if (addTorrentParams.ContentLayout is not null)
            {
                content.AddString("contentLayout", addTorrentParams.ContentLayout.Value);
            }
            if (addTorrentParams.Downloader is not null)
            {
                content.AddString("downloader", addTorrentParams.Downloader);
            }
            if (addTorrentParams.FilePriorities is not null)
            {
                var priorities = string.Join(',', addTorrentParams.FilePriorities.Select(priority => ((int)priority).ToString(CultureInfo.InvariantCulture)));
                content.AddString("filePriorities", priorities);
            }
            if (!string.IsNullOrWhiteSpace(addTorrentParams.SslCertificate))
            {
                content.AddString("ssl_certificate", addTorrentParams.SslCertificate!);
            }
            if (!string.IsNullOrWhiteSpace(addTorrentParams.SslPrivateKey))
            {
                content.AddString("ssl_private_key", addTorrentParams.SslPrivateKey!);
            }
            if (!string.IsNullOrWhiteSpace(addTorrentParams.SslDhParams))
            {
                content.AddString("ssl_dh_params", addTorrentParams.SslDhParams!);
            }

            var response = await _httpClient.PostAsync("torrents/add", content);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var conflictMessage = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(conflictMessage))
                {
                    conflictMessage = "All torrents failed to add.";
                }

                throw new HttpRequestException(conflictMessage, null, response.StatusCode);
            }

            await ThrowIfNotSuccessfulStatusCode(response);

            var payload = await response.Content.ReadAsStringAsync();

            // 5.1.x and earlier return plain text responses
            switch (payload)
            {
                case "Fails.":
                    return new AddTorrentResult(0, 1);

                case "Ok.":
                    return new AddTorrentResult(1, 0);

                case null:
                    return new AddTorrentResult(0, 0);

                case "":
                    return new AddTorrentResult(0, 0);
            }

            var result = JsonSerializer.Deserialize<AddTorrentResult>(payload, _options);
            if (result is null)
            {
                var count = (addTorrentParams.Torrents?.Count ?? 0) + (addTorrentParams.Urls?.Count() ?? 0);
                return new AddTorrentResult(0, count);
            }
            else
            {
                return result;
            }
        }

        public async Task AddTrackersToTorrent(IEnumerable<string> urls, bool? all = null, params string[] hashes)
        {
            if (all is not true && (hashes is null || hashes.Length == 0))
            {
                throw new ArgumentException("Specify at least one torrent hash or set all=true.", nameof(hashes));
            }

            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes ?? Array.Empty<string>())
                .Add("urls", string.Join('\n', urls))
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addTrackers", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task EditTracker(string hash, string url, string? newUrl = null, int? tier = null)
        {
            if ((newUrl is null) && (tier is null))
            {
                throw new ArgumentException("Must specify at least one of newUrl or tier.");
            }

            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("url", url);

            if (!string.IsNullOrEmpty(newUrl))
            {
                content.Add("newUrl", newUrl!);
            }
            if (tier is not null)
            {
                content.Add("tier", tier.Value);
            }

            var form = content.ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/editTracker", form);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task RemoveTrackers(IEnumerable<string> urls, bool? all = null, params string[] hashes)
        {
            if (all is not true && (hashes is null || hashes.Length == 0))
            {
                throw new ArgumentException("Specify at least one torrent hash or set all=true.", nameof(hashes));
            }

            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hash", all, hashes ?? Array.Empty<string>())
                .AddPipeSeparated("urls", urls)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/removeTrackers", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task AddPeers(IEnumerable<string> hashes, IEnumerable<PeerId> peers)
        {
            var content = new FormUrlEncodedBuilder()
                .AddPipeSeparated("hashes", hashes)
                .AddPipeSeparated("urls", peers)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/addPeers", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task IncreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/increasePrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task DecreaseTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/decreasePrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task MaxTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/topPrio", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task MinTorrentPriority(bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
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

        public async Task SetTorrentShareLimit(float ratioLimit, float seedingTimeLimit, float inactiveSeedingTimeLimit, ShareLimitAction? shareLimitAction = null, bool? all = null, params string[] hashes)
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

        public async Task SetTorrentSavePath(IEnumerable<string> hashes, string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var hashArray = hashes?.Where(h => !string.IsNullOrWhiteSpace(h)).ToArray() ?? Array.Empty<string>();
            if (hashArray.Length == 0)
            {
                throw new ArgumentException("Specify at least one torrent hash.", nameof(hashes));
            }

            var content = new FormUrlEncodedBuilder()
                .Add("id", string.Join('|', hashArray))
                .Add("path", path)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setSavePath", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task SetTorrentDownloadPath(IEnumerable<string> hashes, string? path)
        {
            var hashArray = hashes?.Where(h => !string.IsNullOrWhiteSpace(h)).ToArray() ?? Array.Empty<string>();
            if (hashArray.Length == 0)
            {
                throw new ArgumentException("Specify at least one torrent hash.", nameof(hashes));
            }

            var content = new FormUrlEncodedBuilder()
                .Add("id", string.Join('|', hashArray))
                .Add("path", path ?? string.Empty)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setDownloadPath", content);

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

        public async Task AddCategory(string category, string savePath, DownloadPathOption? downloadPathOption = null)
        {
            var builder = new FormUrlEncodedBuilder()
                .Add("category", category)
                .Add("savePath", savePath);

            if (downloadPathOption is not null)
            {
                builder.Add("downloadPathEnabled", downloadPathOption.Enabled);
                if (!string.IsNullOrWhiteSpace(downloadPathOption.Path))
                {
                    builder.Add("downloadPath", downloadPathOption.Path!);
                }
            }

            var response = await _httpClient.PostAsync("torrents/createCategory", builder.ToFormUrlEncodedContent());

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        public async Task EditCategory(string category, string savePath, DownloadPathOption? downloadPathOption = null)
        {
            var builder = new FormUrlEncodedBuilder()
                .Add("category", category)
                .Add("savePath", savePath);

            if (downloadPathOption is not null)
            {
                builder.Add("downloadPathEnabled", downloadPathOption.Enabled);
                if (!string.IsNullOrWhiteSpace(downloadPathOption.Path))
                {
                    builder.Add("downloadPath", downloadPathOption.Path!);
                }
            }

            var response = await _httpClient.PostAsync("torrents/editCategory", builder.ToFormUrlEncodedContent());

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

        public async Task SetTorrentTags(IEnumerable<string> tags, bool? all = null, params string[] hashes)
        {
            var content = new FormUrlEncodedBuilder()
                .AddAllOrPipeSeparated("hashes", all, hashes)
                .AddCommaSeparated("tags", tags)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setTags", content);

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

        public async Task<SslParameters> GetTorrentSslParameters(string hash)
        {
            var response = await _httpClient.GetAsync($"torrents/SSLParameters?hash={hash}");

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJson<SslParameters>(response.Content);
        }

        public async Task SetTorrentSslParameters(string hash, SslParameters parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            if (string.IsNullOrWhiteSpace(parameters.Certificate) || string.IsNullOrWhiteSpace(parameters.PrivateKey) || string.IsNullOrWhiteSpace(parameters.DhParams))
            {
                throw new ArgumentException("Certificate, private key, and DH params are required.", nameof(parameters));
            }

            var content = new FormUrlEncodedBuilder()
                .Add("hash", hash)
                .Add("ssl_certificate", parameters.Certificate!)
                .Add("ssl_private_key", parameters.PrivateKey!)
                .Add("ssl_dh_params", parameters.DhParams!)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrents/setSSLParameters", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        #endregion Torrent management

        #region Torrent creator

        public async Task<string> AddTorrentCreationTask(TorrentCreationTaskRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.SourcePath))
            {
                throw new ArgumentException("SourcePath is required.", nameof(request));
            }

            var builder = new FormUrlEncodedBuilder()
                .Add("sourcePath", request.SourcePath);

            if (!string.IsNullOrWhiteSpace(request.TorrentFilePath))
            {
                builder.Add("torrentFilePath", request.TorrentFilePath!);
            }
            if (request.PieceSize.HasValue)
            {
                builder.Add("pieceSize", request.PieceSize.Value);
            }
            if (request.Private.HasValue)
            {
                builder.Add("private", request.Private.Value);
            }
            if (request.StartSeeding.HasValue)
            {
                builder.Add("startSeeding", request.StartSeeding.Value);
            }
            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                builder.Add("comment", request.Comment!);
            }
            if (!string.IsNullOrWhiteSpace(request.Source))
            {
                builder.Add("source", request.Source!);
            }
            if (request.Trackers is not null)
            {
                builder.Add("trackers", string.Join('|', request.Trackers));
            }
            if (request.UrlSeeds is not null)
            {
                builder.Add("urlSeeds", string.Join('|', request.UrlSeeds));
            }
            if (!string.IsNullOrWhiteSpace(request.Format))
            {
                builder.Add("format", request.Format!);
            }
            if (request.OptimizeAlignment.HasValue)
            {
                builder.Add("optimizeAlignment", request.OptimizeAlignment.Value);
            }
            if (request.PaddedFileSizeLimit.HasValue)
            {
                builder.Add("paddedFileSizeLimit", request.PaddedFileSizeLimit.Value);
            }

            var response = await _httpClient.PostAsync("torrentcreator/addTask", builder.ToFormUrlEncodedContent());

            await ThrowIfNotSuccessfulStatusCode(response);

            var payload = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return string.Empty;
            }

            var json = JsonSerializer.Deserialize<JsonElement>(payload, _options);
            if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("taskID", out var idElement))
            {
                return idElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        public async Task<IReadOnlyList<TorrentCreationTaskStatus>> GetTorrentCreationTasks(string? taskId = null)
        {
            HttpResponseMessage response;
            if (string.IsNullOrWhiteSpace(taskId))
            {
                response = await _httpClient.GetAsync("torrentcreator/status");
            }
            else
            {
                var query = new QueryBuilder()
                    .Add("taskID", taskId);

                response = await _httpClient.GetAsync("torrentcreator/status", query);
            }

            await ThrowIfNotSuccessfulStatusCode(response);

            return await GetJsonList<TorrentCreationTaskStatus>(response.Content);
        }

        public async Task<byte[]> GetTorrentCreationTaskFile(string taskId)
        {
            var query = new QueryBuilder()
                .Add("taskID", taskId);

            var response = await _httpClient.GetAsync("torrentcreator/torrentFile", query);

            await ThrowIfNotSuccessfulStatusCode(response);

            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task DeleteTorrentCreationTask(string taskId)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("taskID", taskId)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("torrentcreator/deleteTask", content);

            await ThrowIfNotSuccessfulStatusCode(response);
        }

        #endregion Torrent creator

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
                .Add("path", path ?? string.Empty)
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

        public async Task SetRssFeedUrl(string path, string url)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("path", path)
                .Add("url", url)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("rss/setFeedURL", content);

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

        public async Task DownloadSearchResult(string pluginName, string torrentUrl)
        {
            var content = new FormUrlEncodedBuilder()
                .Add("pluginName", pluginName)
                .Add("torrentUrl", torrentUrl)
                .ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync("search/downloadTorrent", content);

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
