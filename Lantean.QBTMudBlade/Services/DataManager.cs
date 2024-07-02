using Lantean.QBTMudBlade.Models;

namespace Lantean.QBTMudBlade.Services
{
    public class DataManager : IDataManager
    {
        private static readonly Status[] _statuses = Enum.GetValues<Status>();

        public PeerList CreatePeerList(QBitTorrentClient.Models.TorrentPeers torrentPeers)
        {
            var peers = new Dictionary<string, Peer>();
            if (torrentPeers.Peers is not null)
            {
                foreach (var (key, peer) in torrentPeers.Peers)
                {
                    var newPeer = CreatePeer(key, peer);

                    peers[key] = newPeer;
                }
            }

            var peerList = new PeerList(peers);

            return peerList;
        }

        public MainData CreateMainData(QBitTorrentClient.Models.MainData mainData)
        {
            var torrents = new Dictionary<string, Torrent>(mainData.Torrents?.Count ?? 0);
            if (mainData.Torrents is not null)
            {
                foreach (var (hash, torrent) in mainData.Torrents)
                {
                    var newTorrent = CreateTorrent(hash, torrent);

                    torrents[hash] = newTorrent;
                }
            }

            var tags = new List<string>(mainData.Tags?.Count ?? 0);
            if (mainData.Tags is not null)
            {
                foreach (var tag in mainData.Tags)
                {
                    tags.Add(tag);
                }
            }

            var categories = new Dictionary<string, Category>(mainData.Categories?.Count ?? 0);
            if (mainData.Categories is not null)
            {
                foreach (var (name, category) in mainData.Categories)
                {
                    var newCategory = CreateCategory(category);

                    categories[name] = newCategory;
                }
            }

            var trackers = new Dictionary<string, IReadOnlyList<string>>(mainData.Trackers?.Count ?? 0);
            if (mainData.Trackers is not null)
            {
                foreach (var (url, hashes) in mainData.Trackers)
                {
                    trackers[url] = hashes;
                }
            }

            var serverState = CreateServerState(mainData.ServerState);

            var tagState = new Dictionary<string, HashSet<string>>(tags.Count + 2);
            tagState.Add(FilterHelper.TAG_ALL, torrents.Keys.ToHashSet());
            tagState.Add(FilterHelper.TAG_UNTAGGED, torrents.Values.Where(t => FilterHelper.FilterTag(t, FilterHelper.TAG_UNTAGGED)).ToHashesHashSet());
            foreach (var tag in tags)
            {
                tagState.Add(tag, torrents.Values.Where(t => FilterHelper.FilterTag(t, tag)).ToHashesHashSet());
            }

            var categoriesState = new Dictionary<string, HashSet<string>>(categories.Count + 2);
            categoriesState.Add(FilterHelper.CATEGORY_ALL, torrents.Keys.ToHashSet());
            categoriesState.Add(FilterHelper.CATEGORY_UNCATEGORIZED, torrents.Values.Where(t => FilterHelper.FilterCategory(t, FilterHelper.CATEGORY_UNCATEGORIZED, serverState.UseSubcategories)).ToHashesHashSet());
            foreach (var category in categories.Keys)
            {
                categoriesState.Add(category, torrents.Values.Where(t => FilterHelper.FilterCategory(t, category, serverState.UseSubcategories)).ToHashesHashSet());
            }

            var statusState = new Dictionary<string, HashSet<string>>(_statuses.Length + 2);
            foreach (var status in _statuses)
            {
                statusState.Add(status.ToString(), torrents.Values.Where(t => FilterHelper.FilterStatus(t, status)).ToHashesHashSet());
            }

            var trackersState = new Dictionary<string, HashSet<string>>(trackers.Count + 2);
            trackersState.Add(FilterHelper.TRACKER_ALL, torrents.Keys.ToHashSet());
            trackersState.Add(FilterHelper.TRACKER_TRACKERLESS, torrents.Values.Where(t => FilterHelper.FilterTracker(t, FilterHelper.TRACKER_TRACKERLESS)).ToHashesHashSet());
            foreach (var tracker in trackers.Keys)
            {
                trackersState.Add(tracker, torrents.Values.Where(t => FilterHelper.FilterTracker(t, tracker)).ToHashesHashSet());
            }

            var torrentList = new MainData(torrents, tags, categories, trackers, serverState, tagState, categoriesState, statusState, trackersState);

            return torrentList;
        }

        private static ServerState CreateServerState(QBitTorrentClient.Models.ServerState? serverState)
        {
            if (serverState is null)
            {
                return new ServerState();
            }
            return new ServerState(
                serverState.AllTimeDownloaded!.Value,
                serverState.AllTimeUploaded!.Value,
                serverState.AverageTimeQueue!.Value,
                serverState.ConnectionStatus!,
                serverState.DHTNodes!.Value,
                serverState.DownloadInfoData!.Value,
                serverState.DownloadInfoSpeed!.Value,
                serverState.DownloadRateLimit!.Value,
                serverState.FreeSpaceOnDisk!.Value,
                serverState.GlobalRatio!.Value,
                serverState.QueuedIOJobs!.Value,
                serverState.Queuing!.Value,
                serverState.ReadCacheHits!.Value,
                serverState.ReadCacheOverload!.Value,
                serverState.RefreshInterval!.Value,
                serverState.TotalBuffersSize!.Value,
                serverState.TotalPeerConnections!.Value,
                serverState.TotalQueuedSize!.Value,
                serverState.TotalWastedSession!.Value,
                serverState.UploadInfoData!.Value,
                serverState.UploadInfoSpeed!.Value,
                serverState.UploadRateLimit!.Value,
                serverState.UseAltSpeedLimits!.Value,
                serverState.UseSubcategories!.Value,
                serverState.WriteCacheOverload!.Value);
        }

        public void MergeMainData(QBitTorrentClient.Models.MainData mainData, MainData torrentList)
        {
            if (mainData.CategoriesRemoved is not null)
            {
                foreach (var category in mainData.CategoriesRemoved)
                {
                    torrentList.Categories.Remove(category);
                    torrentList.CategoriesState.Remove(category);
                }
            }

            if (mainData.TagsRemoved is not null)
            {
                foreach (var tag in mainData.TagsRemoved)
                {
                    torrentList.Tags.Remove(tag);
                    torrentList.TagState.Remove(tag);
                }
            }

            if (mainData.TrackersRemoved is not null)
            {
                foreach (var tracker in mainData.TrackersRemoved)
                {
                    torrentList.Trackers.Remove(tracker);
                    torrentList.TrackersState.Remove(tracker);
                }
            }

            if (mainData.TorrentsRemoved is not null)
            {
                foreach (var hash in mainData.TorrentsRemoved)
                {
                    RemoveTorrentFromStates(torrentList, hash);
                    torrentList.Torrents.Remove(hash);
                }
            }

            if (mainData.Categories is not null)
            {
                foreach (var (name, category) in mainData.Categories)
                {
                    if (!torrentList.Categories.TryGetValue(name, out var existingCategory))
                    {
                        var newCategory = CreateCategory(category);
                        torrentList.Categories.Add(name, newCategory);
                    }
                    else
                    {
                        UpdateCategory(existingCategory, category);
                    }
                }
            }

            if (mainData.Tags is not null)
            {
                foreach (var tag in mainData.Tags)
                {
                    torrentList.Tags.Add(tag);
                }
            }

            if (mainData.Trackers is not null)
            {
                foreach (var (url, hashes) in mainData.Trackers)
                {
                    if (!torrentList.Trackers.TryGetValue(url, out var existingHashes))
                    {
                        torrentList.Trackers.Add(url, hashes);
                    }
                    else
                    {
                        torrentList.Trackers[url] = hashes;
                    }
                }
            }

            if (mainData.Torrents is not null)
            {
                foreach (var (hash, torrent) in mainData.Torrents)
                {
                    if (!torrentList.Torrents.TryGetValue(hash, out var existingTorrent))
                    {
                        var newTorrent = CreateTorrent(hash, torrent);
                        torrentList.Torrents.Add(hash, newTorrent);
                        AddTorrentToStates(torrentList, hash);
                    }
                    else
                    {
                        UpdateTorrentStates(torrentList, hash);
                        UpdateTorrent(existingTorrent, torrent);
                    }
                }
            }

            if (mainData.ServerState is not null)
            {
                UpdateServerState(torrentList.ServerState, mainData.ServerState);
            }
        }

        private static void AddTorrentToStates(MainData torrentList, string hash)
        {
            var torrent = torrentList.Torrents[hash];

            torrentList.TagState[FilterHelper.TAG_ALL].Add(hash);
            torrentList.TagState[FilterHelper.TAG_UNTAGGED].AddIfTrue(hash, FilterHelper.FilterTag(torrent, FilterHelper.TAG_UNTAGGED));
            foreach (var tag in torrentList.Tags)
            {
                torrentList.TagState[tag].AddIfTrue(hash, FilterHelper.FilterTag(torrent, tag));
            }

            torrentList.CategoriesState[FilterHelper.CATEGORY_ALL].Add(hash);
            torrentList.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].AddIfTrue(hash, FilterHelper.FilterCategory(torrent, FilterHelper.CATEGORY_UNCATEGORIZED, torrentList.ServerState.UseSubcategories));
            foreach (var category in torrentList.Categories.Keys)
            {
                torrentList.CategoriesState[category].AddIfTrue(hash, FilterHelper.FilterCategory(torrent, category, torrentList.ServerState.UseSubcategories));
            }

            foreach (var status in _statuses)
            {
                torrentList.StatusState[status.ToString()].AddIfTrue(hash, FilterHelper.FilterStatus(torrent, status));
            }

            torrentList.TrackersState[FilterHelper.TRACKER_ALL].Add(hash);
            torrentList.TrackersState[FilterHelper.TRACKER_TRACKERLESS].AddIfTrue(hash, FilterHelper.FilterTracker(torrent, FilterHelper.TRACKER_TRACKERLESS));
            foreach (var tracker in torrentList.Trackers.Keys)
            {
                torrentList.TrackersState[tracker].AddIfTrue(hash, FilterHelper.FilterTracker(torrent, tracker));
            }
        }

        private static void UpdateTorrentStates(MainData torrentList, string hash)
        {
            var torrent = torrentList.Torrents[hash];

            torrentList.TagState[FilterHelper.TAG_UNTAGGED].AddIfTrueOrRemove(hash, FilterHelper.FilterTag(torrent, FilterHelper.TAG_UNTAGGED));
            foreach (var tag in torrentList.Tags)
            {
                if (!torrentList.TagState.TryGetValue(tag, out HashSet<string>? value))
                {
                    value = [];
                    torrentList.TagState.Add(tag, value);
                }

                value.AddIfTrueOrRemove(hash, FilterHelper.FilterTag(torrent, tag));
            }

            torrentList.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].AddIfTrueOrRemove(hash, FilterHelper.FilterCategory(torrent, FilterHelper.CATEGORY_UNCATEGORIZED, torrentList.ServerState.UseSubcategories));
            foreach (var category in torrentList.Categories.Keys)
            {
                if (!torrentList.CategoriesState.TryGetValue(category, out HashSet<string>? value))
                {
                    value = [];
                    torrentList.CategoriesState.Add(category, value);
                }

                value.AddIfTrueOrRemove(hash, FilterHelper.FilterCategory(torrent, category, torrentList.ServerState.UseSubcategories));
            }

            foreach (var status in _statuses)
            {
                torrentList.StatusState[status.ToString()].AddIfTrueOrRemove(hash, FilterHelper.FilterStatus(torrent, status));
            }

            torrentList.TrackersState[FilterHelper.TRACKER_TRACKERLESS].AddIfTrueOrRemove(hash, FilterHelper.FilterTracker(torrent, FilterHelper.TRACKER_TRACKERLESS));
            foreach (var tracker in torrentList.Trackers.Keys)
            {
                if (!torrentList.TrackersState.TryGetValue(tracker, out HashSet<string>? value))
                {
                    value = [];
                    torrentList.TrackersState.Add(tracker, value);
                }

                value.AddIfTrueOrRemove(hash, FilterHelper.FilterTracker(torrent, tracker));
            }
        }

        private static void RemoveTorrentFromStates(MainData torrentList, string hash)
        {
            var torrent = torrentList.Torrents[hash];

            torrentList.TagState[FilterHelper.TAG_ALL].Remove(hash);
            torrentList.TagState[FilterHelper.TAG_UNTAGGED].RemoveIfTrue(hash, FilterHelper.FilterTag(torrent, FilterHelper.TAG_UNTAGGED));
            foreach (var tag in torrentList.Tags)
            {
                if (!torrentList.TagState.TryGetValue(tag, out var tagState))
                {
                    continue;
                }
                tagState.RemoveIfTrue(hash, FilterHelper.FilterTag(torrent, tag));
            }

            torrentList.CategoriesState[FilterHelper.CATEGORY_ALL].Remove(hash);
            torrentList.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].RemoveIfTrue(hash, FilterHelper.FilterCategory(torrent, FilterHelper.CATEGORY_UNCATEGORIZED, torrentList.ServerState.UseSubcategories));
            foreach (var category in torrentList.Categories.Keys)
            {
                if (!torrentList.CategoriesState.TryGetValue(category, out var categoryState))
                {
                    continue;
                }
                categoryState.RemoveIfTrue(hash, FilterHelper.FilterCategory(torrent, category, torrentList.ServerState.UseSubcategories));
            }

            foreach (var status in _statuses)
            {
                if (!torrentList.StatusState.TryGetValue(status.ToString(), out var statusState))
                {
                    continue;
                }
                statusState.RemoveIfTrue(hash, FilterHelper.FilterStatus(torrent, status));
            }

            torrentList.TrackersState[FilterHelper.TRACKER_ALL].Remove(hash);
            torrentList.TrackersState[FilterHelper.TRACKER_TRACKERLESS].RemoveIfTrue(hash, FilterHelper.FilterTracker(torrent, FilterHelper.TRACKER_TRACKERLESS));
            foreach (var tracker in torrentList.Trackers.Keys)
            {
                if (!torrentList.TrackersState.TryGetValue(tracker, out var trackerState))
                {
                    continue;
                }
                trackerState.RemoveIfTrue(hash, FilterHelper.FilterTracker(torrent, tracker));
            }
        }

        private static void UpdateServerState(ServerState existingServerState, QBitTorrentClient.Models.ServerState serverState)
        {
            existingServerState.AllTimeDownloaded = serverState.AllTimeDownloaded ?? existingServerState.AllTimeDownloaded;
            existingServerState.AllTimeUploaded = serverState.AllTimeUploaded ?? existingServerState.AllTimeUploaded;
            existingServerState.AverageTimeQueue = serverState.AverageTimeQueue ?? existingServerState.AverageTimeQueue;
            existingServerState.ConnectionStatus = serverState.ConnectionStatus ?? existingServerState.ConnectionStatus;
            existingServerState.DHTNodes = serverState.DHTNodes ?? existingServerState.DHTNodes;
            existingServerState.DownloadInfoData = serverState.DownloadInfoData ?? existingServerState.DownloadInfoData;
            existingServerState.DownloadInfoSpeed = serverState.DownloadInfoSpeed ?? existingServerState.DownloadInfoSpeed;
            existingServerState.DownloadRateLimit = serverState.DownloadRateLimit ?? existingServerState.DownloadRateLimit;
            existingServerState.FreeSpaceOnDisk = serverState.FreeSpaceOnDisk ?? existingServerState.FreeSpaceOnDisk;
            existingServerState.GlobalRatio = serverState.GlobalRatio ?? existingServerState.GlobalRatio;
            existingServerState.QueuedIOJobs = serverState.QueuedIOJobs ?? existingServerState.QueuedIOJobs;
            existingServerState.Queuing = serverState.Queuing ?? existingServerState.Queuing;
            existingServerState.ReadCacheHits = serverState.ReadCacheHits ?? existingServerState.ReadCacheHits;
            existingServerState.ReadCacheOverload = serverState.ReadCacheOverload ?? existingServerState.ReadCacheOverload;
            existingServerState.RefreshInterval = serverState.RefreshInterval ?? existingServerState.RefreshInterval;
            existingServerState.TotalBuffersSize = serverState.TotalBuffersSize ?? existingServerState.TotalBuffersSize;
            existingServerState.TotalPeerConnections = serverState.TotalPeerConnections ?? existingServerState.TotalPeerConnections;
            existingServerState.TotalQueuedSize = serverState.TotalQueuedSize ?? existingServerState.TotalQueuedSize;
            existingServerState.TotalWastedSession = serverState.TotalWastedSession ?? existingServerState.TotalWastedSession;
            existingServerState.UploadInfoData = serverState.UploadInfoData ?? existingServerState.UploadInfoData;
            existingServerState.UploadInfoSpeed = serverState.UploadInfoSpeed ?? existingServerState.UploadInfoSpeed;
            existingServerState.UploadRateLimit = serverState.UploadRateLimit ?? existingServerState.UploadRateLimit;
            existingServerState.UseAltSpeedLimits = serverState.UseAltSpeedLimits ?? existingServerState.UseAltSpeedLimits;
            existingServerState.UseSubcategories = serverState.UseSubcategories ?? existingServerState.UseSubcategories;
            existingServerState.WriteCacheOverload = serverState.WriteCacheOverload ?? existingServerState.WriteCacheOverload;
        }

        public void MergeTorrentPeers(QBitTorrentClient.Models.TorrentPeers torrentPeers, PeerList peerList)
        {
            if (torrentPeers.PeersRemoved is not null)
            {
                foreach (var key in torrentPeers.PeersRemoved)
                {
                    peerList.Peers.Remove(key);
                }
            }

            if (torrentPeers.Peers is not null)
            {
                foreach (var (key, peer) in torrentPeers.Peers)
                {
                    if (!peerList.Peers.TryGetValue(key, out var existingPeer))
                    {
                        var newPeer = CreatePeer(key, peer);
                        peerList.Peers.Add(key, newPeer);
                    }
                    else
                    {
                        UpdatePeer(existingPeer, peer);
                    }
                }
            }
        }

        private static void UpdatePeer(Peer existingPeer, QBitTorrentClient.Models.Peer peer)
        {
            existingPeer.Client = peer.Client ?? existingPeer.Client;
            existingPeer.ClientId = peer.ClientId ?? existingPeer.ClientId;
            existingPeer.Connection = peer.Connection ?? existingPeer.Connection;
            existingPeer.Country = peer.Country ?? existingPeer.Country;
            existingPeer.CountryCode = peer.CountryCode ?? existingPeer.CountryCode;
            existingPeer.Downloaded = peer.Downloaded ?? existingPeer.Downloaded;
            existingPeer.DownloadSpeed = peer.DownloadSpeed ?? existingPeer.DownloadSpeed;
            existingPeer.Files = peer.Files ?? existingPeer.Files;
            existingPeer.Flags = peer.Flags ?? existingPeer.Flags;
            existingPeer.FlagsDescription = peer.FlagsDescription ?? existingPeer.FlagsDescription;
            existingPeer.IPAddress = peer.IPAddress ?? existingPeer.IPAddress;
            existingPeer.Port = peer.Port ?? existingPeer.Port;
            existingPeer.Progress = peer.Progress ?? existingPeer.Progress;
            existingPeer.Relevance = peer.Relevance ?? existingPeer.Relevance;
            existingPeer.Uploaded = peer.Uploaded ?? existingPeer.Uploaded;
            existingPeer.UploadSpeed = peer.UploadSpeed ?? existingPeer.UploadSpeed;
        }

        private static Category CreateCategory(QBitTorrentClient.Models.Category category)
        {
            return new Category(category.Name, category.SavePath!);
        }

        private static Peer CreatePeer(string key, QBitTorrentClient.Models.Peer peer)
        {
            return new Peer(
                key,
                peer.Client!,
                peer.ClientId!,
                peer.Connection!,
                peer.Country,
                peer.CountryCode,
                peer.Downloaded!.Value,
                peer.DownloadSpeed!.Value,
                peer.Files!,
                peer.Flags!,
                peer.FlagsDescription!,
                peer.IPAddress!,
                peer.Port!.Value,
                peer.Progress!.Value,
                peer.Relevance!.Value,
                peer.Uploaded!.Value,
                peer.UploadSpeed!.Value);
        }

        public Torrent CreateTorrent(string hash, QBitTorrentClient.Models.Torrent torrent)
        {
            return new Torrent(
                hash,
                torrent.AddedOn!.Value,
                torrent.AmountLeft!.Value,
                torrent.AutomaticTorrentManagement!.Value,
                torrent.Availability!.Value,
                torrent.Category!,
                torrent.Completed!.Value,
                torrent.CompletionOn!.Value,
                torrent.ContentPath!,
                torrent.DownloadLimit!.Value,
                torrent.DownloadSpeed!.Value,
                torrent.Downloaded!.Value,
                torrent.DownloadedSession!.Value,
                torrent.EstimatedTimeOfArrival!.Value,
                torrent.FirstLastPiecePriority!.Value,
                torrent.ForceStart!.Value,
                torrent.InfoHashV1!,
                torrent.InfoHashV2!,
                torrent.LastActivity!.Value,
                torrent.MagnetUri!,
                torrent.MaxRatio!.Value,
                torrent.MaxSeedingTime!.Value,
                torrent.Name!,
                torrent.NumberComplete!.Value,
                torrent.NumberIncomplete!.Value,
                torrent.NumberLeeches!.Value,
                torrent.NumberSeeds!.Value,
                torrent.Priority!.Value,
                torrent.Progress!.Value,
                torrent.Ratio!.Value,
                torrent.RatioLimit!.Value,
                torrent.SavePath!,
                torrent.SeedingTime!.Value,
                torrent.SeedingTimeLimit!.Value,
                torrent.SeenComplete!.Value,
                torrent.SequentialDownload!.Value,
                torrent.Size!.Value,
                torrent.State!,
                torrent.SuperSeeding!.Value,
                torrent.Tags!,
                torrent.TimeActive!.Value,
                torrent.TotalSize!.Value,
                torrent.Tracker!,
                torrent.UploadLimit!.Value,
                torrent.Uploaded!.Value,
                torrent.UploadedSession!.Value,
                torrent.UploadSpeed!.Value,
                torrent.Reannounce ?? 0);
        }

        private static void UpdateCategory(Category existingCategory, QBitTorrentClient.Models.Category category)
        {
            existingCategory.SavePath = category.SavePath ?? existingCategory.SavePath;
        }

        private static void UpdateTorrent(Torrent existingTorrent, QBitTorrentClient.Models.Torrent torrent)
        {
            existingTorrent.AddedOn = torrent.AddedOn ?? existingTorrent.AddedOn;
            existingTorrent.AmountLeft = torrent.AmountLeft ?? existingTorrent.AmountLeft;
            existingTorrent.AutomaticTorrentManagement = torrent.AutomaticTorrentManagement ?? existingTorrent.AutomaticTorrentManagement;
            existingTorrent.Availability = torrent.Availability ?? existingTorrent.Availability;
            existingTorrent.Category = torrent.Category ?? existingTorrent.Category;
            existingTorrent.Completed = torrent.Completed ?? existingTorrent.Completed;
            existingTorrent.CompletionOn = torrent.CompletionOn ?? existingTorrent.CompletionOn;
            existingTorrent.ContentPath = torrent.ContentPath ?? existingTorrent.ContentPath;
            existingTorrent.Downloaded = torrent.Downloaded ?? existingTorrent.Downloaded;
            existingTorrent.DownloadedSession = torrent.DownloadedSession ?? existingTorrent.DownloadedSession;
            existingTorrent.DownloadLimit = torrent.DownloadLimit ?? existingTorrent.DownloadLimit;
            existingTorrent.DownloadSpeed = torrent.DownloadSpeed ?? existingTorrent.DownloadSpeed;
            existingTorrent.EstimatedTimeOfArrival = torrent.EstimatedTimeOfArrival ?? existingTorrent.EstimatedTimeOfArrival;
            existingTorrent.FirstLastPiecePriority = torrent.FirstLastPiecePriority ?? existingTorrent.FirstLastPiecePriority;
            existingTorrent.ForceStart = torrent.ForceStart ?? existingTorrent.ForceStart;
            existingTorrent.InfoHashV1 = torrent.InfoHashV1 ?? existingTorrent.InfoHashV1;
            existingTorrent.InfoHashV2 = torrent.InfoHashV2 ?? existingTorrent.InfoHashV2;
            existingTorrent.LastActivity = torrent.LastActivity ?? existingTorrent.LastActivity;
            existingTorrent.MagnetUri = torrent.MagnetUri ?? existingTorrent.MagnetUri;
            existingTorrent.MaxRatio = torrent.MaxRatio ?? existingTorrent.MaxRatio;
            existingTorrent.MaxSeedingTime = torrent.MaxSeedingTime ?? existingTorrent.MaxSeedingTime;
            existingTorrent.Name = torrent.Name ?? existingTorrent.Name;
            existingTorrent.NumberComplete = torrent.NumberComplete ?? existingTorrent.NumberComplete;
            existingTorrent.NumberIncomplete = torrent.NumberIncomplete ?? existingTorrent.NumberIncomplete;
            existingTorrent.NumberLeeches = torrent.NumberLeeches ?? existingTorrent.NumberLeeches;
            existingTorrent.NumberSeeds = torrent.NumberSeeds ?? existingTorrent.NumberSeeds;
            existingTorrent.Priority = torrent.Priority ?? existingTorrent.Priority;
            existingTorrent.Progress = torrent.Progress ?? existingTorrent.Progress;
            existingTorrent.Ratio = torrent.Ratio ?? existingTorrent.Ratio;
            existingTorrent.RatioLimit = torrent.RatioLimit ?? existingTorrent.RatioLimit;
            existingTorrent.SavePath = torrent.SavePath ?? existingTorrent.SavePath;
            existingTorrent.SeedingTime = torrent.SeedingTime ?? existingTorrent.SeedingTime;
            existingTorrent.SeedingTimeLimit = torrent.SeedingTimeLimit ?? existingTorrent.SeedingTimeLimit;
            existingTorrent.SeenComplete = torrent.SeenComplete ?? existingTorrent.SeenComplete;
            existingTorrent.SequentialDownload = torrent.SequentialDownload ?? existingTorrent.SequentialDownload;
            existingTorrent.Size = torrent.Size ?? existingTorrent.Size;
            existingTorrent.State = torrent.State ?? existingTorrent.State;
            existingTorrent.SuperSeeding = torrent.SuperSeeding ?? existingTorrent.SuperSeeding;
            if (torrent.Tags is not null)
            {
                existingTorrent.Tags.Clear();
                existingTorrent.Tags.AddRange(torrent.Tags);
            }
            existingTorrent.TimeActive = torrent.TimeActive ?? existingTorrent.TimeActive;
            existingTorrent.TotalSize = torrent.TotalSize ?? existingTorrent.TotalSize;
            existingTorrent.Tracker = torrent.Tracker ?? existingTorrent.Tracker;
            existingTorrent.UploadLimit = torrent.UploadLimit ?? existingTorrent.UploadLimit;
            existingTorrent.Uploaded = torrent.Uploaded ?? existingTorrent.Uploaded;
            existingTorrent.UploadedSession = torrent.UploadedSession ?? existingTorrent.UploadedSession;
            existingTorrent.UploadSpeed = torrent.UploadSpeed ?? existingTorrent.UploadSpeed;
            existingTorrent.Reannounce = torrent.Reannounce ?? existingTorrent.Reannounce;
        }

        public Dictionary<string, ContentItem> CreateContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files)
        {
            var contents = new Dictionary<string, ContentItem>();
            if (files.Count == 0)
            {
                return contents;
            }

            var folderIndex = files.Min(f => f.Index) - 1;

            foreach (var file in files)
            {
                if (!file.Name.Contains(Extensions.DirectorySeparator))
                {
                    contents.Add(file.Name, new ContentItem(file.Name, file.Name, file.Index, (Priority)(int)file.Priority, file.Progress, file.Size, file.Availability));
                }
                else
                {
                    var nameAndPath = file.Name.Split(Extensions.DirectorySeparator);
                    var paths = nameAndPath[..^1];
                    for (var i = 0; i < paths.Length; i++)
                    {
                        var directoryName = paths[i];
                        var directoryPath = string.Join(Extensions.DirectorySeparator, paths[0..(i + 1)]);
                        if (!contents.ContainsKey(directoryPath))
                        {
                            contents.Add(directoryPath, new ContentItem(directoryPath, directoryName, folderIndex--, Priority.Normal, 0, 0, 0, true, i));
                        }
                    }

                    var displayName = nameAndPath[^1];

                    contents.Add(file.Name, new ContentItem(file.Name, displayName, file.Index, (Priority)(int)file.Priority, file.Progress, file.Size, file.Availability, false, paths.Length));
                }
            }

            var directories = contents.Where(c => c.Value.IsFolder).OrderByDescending(c => c.Value.Level);

            foreach (var directory in directories)
            {
                var key = directory.Key;
                var level = directory.Value.Level;
                var filesContents = contents.Where(c => c.Value.Name.StartsWith(key + Extensions.DirectorySeparator) && !c.Value.IsFolder).ToList();
                var directoriesContents = contents.Where(c => c.Value.Name.StartsWith(key + Extensions.DirectorySeparator) && c.Value.IsFolder && c.Value.Level == level + 1).ToList();
                var allContents = filesContents.Concat(directoriesContents);
                var priorities = allContents.Select(d => d.Value.Priority).Distinct();
                var downloadingContents = allContents.Where(c => c.Value.Priority != Priority.DoNotDownload).ToList();

                long size = 0;
                float availability = 0;
                long downloaded = 0;
                float progress = 0;
                if (downloadingContents.Count != 0)
                {
                    size = downloadingContents.Sum(c => c.Value.Size);
                    availability = downloadingContents.Average(c => c.Value.Availability);
                    downloaded = downloadingContents.Sum(c => c.Value.Downloaded);
                    progress = (float)downloaded / size;
                }

                if (!contents.TryGetValue(key, out var dir))
                {
                    continue;
                }
                dir.Availability = availability;
                dir.Size = size;
                dir.Progress = progress;
                if (priorities.Count() == 1)
                {
                    dir.Priority = priorities.First();
                }
                else
                {
                    dir.Priority = Priority.Mixed;
                }
            }

            return contents;
        }

        public QBitTorrentClient.Models.UpdatePreferences MergePreferences(QBitTorrentClient.Models.UpdatePreferences? original, QBitTorrentClient.Models.UpdatePreferences changed)
        {
            if (original is null)
            {
                original = new QBitTorrentClient.Models.UpdatePreferences
                {
                    AddToTopOfQueue = changed.AddToTopOfQueue,
                    AddTrackers = changed.AddTrackers,
                    AddTrackersEnabled = changed.AddTrackersEnabled,
                    AltDlLimit = changed.AltDlLimit,
                    AltUpLimit = changed.AltUpLimit,
                    AlternativeWebuiEnabled = changed.AlternativeWebuiEnabled,
                    AlternativeWebuiPath = changed.AlternativeWebuiPath,
                    AnnounceIp = changed.AnnounceIp,
                    AnnounceToAllTiers = changed.AnnounceToAllTiers,
                    AnnounceToAllTrackers = changed.AnnounceToAllTrackers,
                    AnonymousMode = changed.AnonymousMode,
                    AsyncIoThreads = changed.AsyncIoThreads,
                    AutoDeleteMode = changed.AutoDeleteMode,
                    AutoTmmEnabled = changed.AutoTmmEnabled,
                    AutorunEnabled = changed.AutorunEnabled,
                    AutorunOnTorrentAddedEnabled = changed.AutorunOnTorrentAddedEnabled,
                    AutorunOnTorrentAddedProgram = changed.AutorunOnTorrentAddedProgram,
                    AutorunProgram = changed.AutorunProgram,
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
                    IpFilterEnabled = changed.IpFilterEnabled,
                    IpFilterPath = changed.IpFilterPath,
                    IpFilterTrackers = changed.IpFilterTrackers,
                    LimitLanPeers = changed.LimitLanPeers,
                    LimitTcpOverhead = changed.LimitTcpOverhead,
                    LimitUtpRate = changed.LimitUtpRate,
                    ListenPort = changed.ListenPort,
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
                    RssMaxArticlesPerFeed = changed.RssMaxArticlesPerFeed,
                    RssProcessingEnabled = changed.RssProcessingEnabled,
                    RssRefreshInterval = changed.RssRefreshInterval,
                    RssSmartEpisodeFilters = changed.RssSmartEpisodeFilters,
                    SavePath = changed.SavePath,
                    SavePathChangedTmmEnabled = changed.SavePathChangedTmmEnabled,
                    SaveResumeDataInterval = changed.SaveResumeDataInterval,
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
                    StartPausedEnabled = changed.StartPausedEnabled,
                    StopTrackerTimeout = changed.StopTrackerTimeout,
                    TempPath = changed.TempPath,
                    TempPathEnabled = changed.TempPathEnabled,
                    TorrentChangedTmmEnabled = changed.TorrentChangedTmmEnabled,
                    TorrentContentLayout = changed.TorrentContentLayout,
                    TorrentFileSizeLimit = changed.TorrentFileSizeLimit,
                    TorrentStopCondition = changed.TorrentStopCondition,
                    UpLimit = changed.UpLimit,
                    UploadChokingAlgorithm = changed.UploadChokingAlgorithm,
                    UploadSlotsBehavior = changed.UploadSlotsBehavior,
                    Upnp = changed.Upnp,
                    UpnpLeaseDuration = changed.UpnpLeaseDuration,
                    UseCategoryPathsInManualMode = changed.UseCategoryPathsInManualMode,
                    UseHttps = changed.UseHttps,
                    UseSubcategories = changed.UseSubcategories,
                    UtpTcpMixedMode = changed.UtpTcpMixedMode,
                    ValidateHttpsTrackerCertificate = changed.ValidateHttpsTrackerCertificate,
                    WebUiAddress = changed.WebUiAddress,
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
                    WebUiUsername = changed.WebUiUsername
                };
            }
            else
            {
                original.AddToTopOfQueue = changed.AddToTopOfQueue ?? original.AddToTopOfQueue;
                original.AddTrackers = changed.AddTrackers ?? original.AddTrackers;
                original.AddTrackersEnabled = changed.AddTrackersEnabled ?? original.AddTrackersEnabled;
                original.AltDlLimit = changed.AltDlLimit ?? original.AltDlLimit;
                original.AltUpLimit = changed.AltUpLimit ?? original.AltUpLimit;
                original.AlternativeWebuiEnabled = changed.AlternativeWebuiEnabled ?? original.AlternativeWebuiEnabled;
                original.AlternativeWebuiPath = changed.AlternativeWebuiPath ?? original.AlternativeWebuiPath;
                original.AnnounceIp = changed.AnnounceIp ?? original.AnnounceIp;
                original.AnnounceToAllTiers = changed.AnnounceToAllTiers ?? original.AnnounceToAllTiers;
                original.AnnounceToAllTrackers = changed.AnnounceToAllTrackers ?? original.AnnounceToAllTrackers;
                original.AnonymousMode = changed.AnonymousMode ?? original.AnonymousMode;
                original.AsyncIoThreads = changed.AsyncIoThreads ?? original.AsyncIoThreads;
                original.AutoDeleteMode = changed.AutoDeleteMode ?? original.AutoDeleteMode;
                original.AutoTmmEnabled = changed.AutoTmmEnabled ?? original.AutoTmmEnabled;
                original.AutorunEnabled = changed.AutorunEnabled ?? original.AutorunEnabled;
                original.AutorunOnTorrentAddedEnabled = changed.AutorunOnTorrentAddedEnabled ?? original.AutorunOnTorrentAddedEnabled;
                original.AutorunOnTorrentAddedProgram = changed.AutorunOnTorrentAddedProgram ?? original.AutorunOnTorrentAddedProgram;
                original.AutorunProgram = changed.AutorunProgram ?? original.AutorunProgram;
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
                original.IpFilterEnabled = changed.IpFilterEnabled ?? original.IpFilterEnabled;
                original.IpFilterPath = changed.IpFilterPath ?? original.IpFilterPath;
                original.IpFilterTrackers = changed.IpFilterTrackers ?? original.IpFilterTrackers;
                original.LimitLanPeers = changed.LimitLanPeers ?? original.LimitLanPeers;
                original.LimitTcpOverhead = changed.LimitTcpOverhead ?? original.LimitTcpOverhead;
                original.LimitUtpRate = changed.LimitUtpRate ?? original.LimitUtpRate;
                original.ListenPort = changed.ListenPort ?? original.ListenPort;
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
                original.MaxActiveCheckingTorrents = changed.MaxActiveCheckingTorrents ?? original.MaxActiveCheckingTorrents;
                original.MaxActiveDownloads = changed.MaxActiveDownloads ?? original.MaxActiveDownloads;
                original.MaxActiveTorrents = changed.MaxActiveTorrents ?? original.MaxActiveTorrents;
                original.MaxActiveUploads = changed.MaxActiveUploads ?? original.MaxActiveUploads;
                original.MaxConcurrentHttpAnnounces = changed.MaxConcurrentHttpAnnounces ?? original.MaxConcurrentHttpAnnounces;
                original.MaxConnec = changed.MaxConnec ?? original.MaxConnec;
                original.MaxConnecPerTorrent = changed.MaxConnecPerTorrent ?? original.MaxConnecPerTorrent;
                original.MaxInactiveSeedingTime = changed.MaxInactiveSeedingTime ?? original.MaxInactiveSeedingTime;
                original.MaxInactiveSeedingTimeEnabled = changed.MaxInactiveSeedingTimeEnabled ?? original.MaxInactiveSeedingTimeEnabled;
                original.MaxRatio = changed.MaxRatio ?? original.MaxRatio;
                original.MaxRatioAct = changed.MaxRatioAct ?? original.MaxRatioAct;
                original.MaxRatioEnabled = changed.MaxRatioEnabled ?? original.MaxRatioEnabled;
                original.MaxSeedingTime = changed.MaxSeedingTime ?? original.MaxSeedingTime;
                original.MaxSeedingTimeEnabled = changed.MaxSeedingTimeEnabled ?? original.MaxSeedingTimeEnabled;
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
                original.RssMaxArticlesPerFeed = changed.RssMaxArticlesPerFeed ?? original.RssMaxArticlesPerFeed;
                original.RssProcessingEnabled = changed.RssProcessingEnabled ?? original.RssProcessingEnabled;
                original.RssRefreshInterval = changed.RssRefreshInterval ?? original.RssRefreshInterval;
                original.RssSmartEpisodeFilters = changed.RssSmartEpisodeFilters ?? original.RssSmartEpisodeFilters;
                original.SavePath = changed.SavePath ?? original.SavePath;
                original.SavePathChangedTmmEnabled = changed.SavePathChangedTmmEnabled ?? original.SavePathChangedTmmEnabled;
                original.SaveResumeDataInterval = changed.SaveResumeDataInterval ?? original.SaveResumeDataInterval;
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
                original.StartPausedEnabled = changed.StartPausedEnabled ?? original.StartPausedEnabled;
                original.StopTrackerTimeout = changed.StopTrackerTimeout ?? original.StopTrackerTimeout;
                original.TempPath = changed.TempPath ?? original.TempPath;
                original.TempPathEnabled = changed.TempPathEnabled ?? original.TempPathEnabled;
                original.TorrentChangedTmmEnabled = changed.TorrentChangedTmmEnabled ?? original.TorrentChangedTmmEnabled;
                original.TorrentContentLayout = changed.TorrentContentLayout ?? original.TorrentContentLayout;
                original.TorrentFileSizeLimit = changed.TorrentFileSizeLimit ?? original.TorrentFileSizeLimit;
                original.TorrentStopCondition = changed.TorrentStopCondition ?? original.TorrentStopCondition;
                original.UpLimit = changed.UpLimit ?? original.UpLimit;
                original.UploadChokingAlgorithm = changed.UploadChokingAlgorithm ?? original.UploadChokingAlgorithm;
                original.UploadSlotsBehavior = changed.UploadSlotsBehavior ?? original.UploadSlotsBehavior;
                original.Upnp = changed.Upnp ?? original.Upnp;
                original.UpnpLeaseDuration = changed.UpnpLeaseDuration ?? original.UpnpLeaseDuration;
                original.UseCategoryPathsInManualMode = changed.UseCategoryPathsInManualMode ?? original.UseCategoryPathsInManualMode;
                original.UseHttps = changed.UseHttps ?? original.UseHttps;
                original.UseSubcategories = changed.UseSubcategories ?? original.UseSubcategories;
                original.UtpTcpMixedMode = changed.UtpTcpMixedMode ?? original.UtpTcpMixedMode;
                original.ValidateHttpsTrackerCertificate = changed.ValidateHttpsTrackerCertificate ?? original.ValidateHttpsTrackerCertificate;
                original.WebUiAddress = changed.WebUiAddress ?? original.WebUiAddress;
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
            }

            return original;
        }

        public void MergeContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files, Dictionary<string, ContentItem> contents)
        {
            var contentsList = CreateContentsList(files);

            foreach (var (key, value) in contentsList)
            {
                if (contents.TryGetValue(key, out var content))
                {
                    content.Availability = value.Availability;
                    content.Priority = value.Priority;
                    content.Progress = value.Progress;
                    content.Size = value.Size;
                }
                else
                {
                    contents[key] = value;
                }
            }
        }
    }
}