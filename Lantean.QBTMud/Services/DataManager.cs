using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public class DataManager : IDataManager
    {
        private static Status[]? _statusArray = null;

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

            var tags = new List<string>();
            if (mainData.Tags is not null)
            {
                var seenTags = new HashSet<string>(StringComparer.Ordinal);
                foreach (var tag in mainData.Tags)
                {
                    var normalizedTag = NormalizeTag(tag);
                    if (string.IsNullOrEmpty(normalizedTag) || !seenTags.Add(normalizedTag))
                    {
                        continue;
                    }

                    tags.Add(normalizedTag);
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

            var tagState = new Dictionary<string, HashSet<string>>(tags.Count + 2)
            {
                { FilterHelper.TAG_ALL, torrents.Keys.ToHashSet() },
                { FilterHelper.TAG_UNTAGGED, torrents.Values.Where(t => FilterHelper.FilterTag(t, FilterHelper.TAG_UNTAGGED)).ToHashesHashSet() }
            };
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

            var statuses = GetStatuses().ToArray();
            var statusState = new Dictionary<string, HashSet<string>>(statuses.Length + 2);
            foreach (var status in statuses)
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
                serverState.AllTimeDownloaded.GetValueOrDefault(),
                serverState.AllTimeUploaded.GetValueOrDefault(),
                serverState.AverageTimeQueue.GetValueOrDefault(),
                serverState.ConnectionStatus!,
                serverState.DHTNodes.GetValueOrDefault(),
                serverState.DownloadInfoData.GetValueOrDefault(),
                serverState.DownloadInfoSpeed.GetValueOrDefault(),
                serverState.DownloadRateLimit.GetValueOrDefault(),
                serverState.FreeSpaceOnDisk.GetValueOrDefault(),
                serverState.GlobalRatio.GetValueOrDefault(),
                serverState.QueuedIOJobs.GetValueOrDefault(),
                serverState.Queuing.GetValueOrDefault(),
                serverState.ReadCacheHits.GetValueOrDefault(),
                serverState.ReadCacheOverload.GetValueOrDefault(),
                serverState.RefreshInterval.GetValueOrDefault(),
                serverState.TotalBuffersSize.GetValueOrDefault(),
                serverState.TotalPeerConnections.GetValueOrDefault(),
                serverState.TotalQueuedSize.GetValueOrDefault(),
                serverState.TotalWastedSession.GetValueOrDefault(),
                serverState.UploadInfoData.GetValueOrDefault(),
                serverState.UploadInfoSpeed.GetValueOrDefault(),
                serverState.UploadRateLimit.GetValueOrDefault(),
                serverState.UseAltSpeedLimits.GetValueOrDefault(),
                serverState.UseSubcategories.GetValueOrDefault(),
                serverState.WriteCacheOverload.GetValueOrDefault());
        }

        public bool MergeMainData(QBitTorrentClient.Models.MainData mainData, MainData torrentList, out bool filterChanged)
        {
            filterChanged = false;
            var dataChanged = false;

            if (mainData.CategoriesRemoved is not null)
            {
                foreach (var category in mainData.CategoriesRemoved)
                {
                    if (torrentList.Categories.Remove(category))
                    {
                        dataChanged = true;
                        filterChanged = true;
                    }
                    if (torrentList.CategoriesState.Remove(category))
                    {
                        filterChanged = true;
                    }
                }
            }

            if (mainData.TagsRemoved is not null)
            {
                foreach (var tag in mainData.TagsRemoved)
                {
                    var normalizedTag = NormalizeTag(tag);
                    if (string.IsNullOrEmpty(normalizedTag))
                    {
                        continue;
                    }

                    if (torrentList.TagState.Remove(normalizedTag))
                    {
                        filterChanged = true;
                    }
                    torrentList.TagState.Remove(normalizedTag);
                }
            }

            if (mainData.TrackersRemoved is not null)
            {
                foreach (var tracker in mainData.TrackersRemoved)
                {
                    if (torrentList.Trackers.Remove(tracker))
                    {
                        dataChanged = true;
                        filterChanged = true;
                    }
                    if (torrentList.TrackersState.Remove(tracker))
                    {
                        filterChanged = true;
                    }
                }
            }

            if (mainData.TorrentsRemoved is not null)
            {
                foreach (var hash in mainData.TorrentsRemoved)
                {
                    if (torrentList.Torrents.Remove(hash))
                    {
                        RemoveTorrentFromStates(torrentList, hash);
                        dataChanged = true;
                        filterChanged = true;
                    }
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
                        dataChanged = true;
                        filterChanged = true;
                    }
                    else if (UpdateCategory(existingCategory, category))
                    {
                        dataChanged = true;
                    }
                }
            }

            if (mainData.Tags is not null)
            {
                foreach (var tag in mainData.Tags)
                {
                    var normalizedTag = NormalizeTag(tag);
                    if (string.IsNullOrEmpty(normalizedTag))
                    {
                        continue;
                    }

                    if (torrentList.Tags.Add(normalizedTag))
                    {
                        dataChanged = true;
                        filterChanged = true;
                    }
                    var matchingHashes = torrentList.Torrents
                        .Where(pair => FilterHelper.FilterTag(pair.Value, normalizedTag))
                        .Select(pair => pair.Key)
                        .ToHashSet();
                    torrentList.TagState[normalizedTag] = matchingHashes;
                }
            }

            if (mainData.Trackers is not null)
            {
                foreach (var (url, hashes) in mainData.Trackers)
                {
                    if (!torrentList.Trackers.TryGetValue(url, out var existingHashes))
                    {
                        torrentList.Trackers.Add(url, hashes);
                        dataChanged = true;
                        filterChanged = true;
                    }
                    else if (!existingHashes.SequenceEqual(hashes))
                    {
                        torrentList.Trackers[url] = hashes;
                        dataChanged = true;
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
                        dataChanged = true;
                        filterChanged = true;
                    }
                    else
                    {
                        var previousSnapshot = CreateSnapshot(existingTorrent);
                        var updateResult = UpdateTorrent(existingTorrent, torrent);
                        if (updateResult.FilterChanged)
                        {
                            UpdateTorrentStates(torrentList, hash, previousSnapshot, existingTorrent);
                            filterChanged = true;
                        }
                        if (updateResult.DataChanged)
                        {
                            dataChanged = true;
                        }
                    }
                }
            }

            if (mainData.ServerState is not null)
            {
                if (UpdateServerState(torrentList.ServerState, mainData.ServerState))
                {
                    dataChanged = true;
                }
            }

            return dataChanged;
        }

        private static void AddTorrentToStates(MainData torrentList, string hash)
        {
            if (!torrentList.Torrents.TryGetValue(hash, out var torrent))
            {
                return;
            }

            torrentList.TagState[FilterHelper.TAG_ALL].Add(hash);
            UpdateTagStateForAddition(torrentList, torrent, hash);

            torrentList.CategoriesState[FilterHelper.CATEGORY_ALL].Add(hash);
            UpdateCategoryState(torrentList, torrent, hash, previousCategory: null);

            foreach (var status in GetStatuses())
            {
                if (!torrentList.StatusState.TryGetValue(status.ToString(), out var statusSet))
                {
                    continue;
                }

                if (FilterHelper.FilterStatus(torrent, status))
                {
                    statusSet.Add(hash);
                }
            }

            torrentList.TrackersState[FilterHelper.TRACKER_ALL].Add(hash);
            UpdateTrackerState(torrentList, torrent, hash, previousTracker: null);
        }

        private static Status[] GetStatuses()
        {
            if (_statusArray is not null)
            {
                return _statusArray;
            }

            _statusArray = Enum.GetValues<Status>();

            return _statusArray;
        }

        private static void UpdateTorrentStates(MainData torrentList, string hash, TorrentSnapshot previousSnapshot, Torrent updatedTorrent)
        {
            UpdateTagStateForUpdate(torrentList, hash, previousSnapshot.Tags, updatedTorrent.Tags);
            UpdateCategoryState(torrentList, updatedTorrent, hash, previousSnapshot.Category);
            UpdateStatusState(torrentList, hash, previousSnapshot.State, previousSnapshot.UploadSpeed, updatedTorrent.State, updatedTorrent.UploadSpeed);
            UpdateTrackerState(torrentList, updatedTorrent, hash, previousSnapshot.Tracker);
        }

        private static void RemoveTorrentFromStates(MainData torrentList, string hash)
        {
            if (!torrentList.Torrents.TryGetValue(hash, out var torrent))
            {
                return;
            }

            var snapshot = CreateSnapshot(torrent);

            torrentList.TagState[FilterHelper.TAG_ALL].Remove(hash);
            UpdateTagStateForRemoval(torrentList, hash, snapshot.Tags);

            torrentList.CategoriesState[FilterHelper.CATEGORY_ALL].Remove(hash);
            UpdateCategoryStateForRemoval(torrentList, hash, snapshot.Category);

            foreach (var status in GetStatuses())
            {
                if (!torrentList.StatusState.TryGetValue(status.ToString(), out var statusState))
                {
                    continue;
                }

                if (FilterHelper.FilterStatus(snapshot.State, snapshot.UploadSpeed, status))
                {
                    statusState.Remove(hash);
                }
            }

            torrentList.TrackersState[FilterHelper.TRACKER_ALL].Remove(hash);
            UpdateTrackerStateForRemoval(torrentList, hash, snapshot.Tracker);
        }

        private static bool UpdateServerState(ServerState existingServerState, QBitTorrentClient.Models.ServerState serverState)
        {
            var changed = false;

            if (serverState.AllTimeDownloaded.HasValue && existingServerState.AllTimeDownloaded != serverState.AllTimeDownloaded.Value)
            {
                existingServerState.AllTimeDownloaded = serverState.AllTimeDownloaded.Value;
                changed = true;
            }

            if (serverState.AllTimeUploaded.HasValue && existingServerState.AllTimeUploaded != serverState.AllTimeUploaded.Value)
            {
                existingServerState.AllTimeUploaded = serverState.AllTimeUploaded.Value;
                changed = true;
            }

            if (serverState.AverageTimeQueue.HasValue && existingServerState.AverageTimeQueue != serverState.AverageTimeQueue.Value)
            {
                existingServerState.AverageTimeQueue = serverState.AverageTimeQueue.Value;
                changed = true;
            }

            if (serverState.ConnectionStatus is not null && existingServerState.ConnectionStatus != serverState.ConnectionStatus)
            {
                existingServerState.ConnectionStatus = serverState.ConnectionStatus;
                changed = true;
            }

            if (serverState.DHTNodes.HasValue && existingServerState.DHTNodes != serverState.DHTNodes.Value)
            {
                existingServerState.DHTNodes = serverState.DHTNodes.Value;
                changed = true;
            }

            if (serverState.DownloadInfoData.HasValue && existingServerState.DownloadInfoData != serverState.DownloadInfoData.Value)
            {
                existingServerState.DownloadInfoData = serverState.DownloadInfoData.Value;
                changed = true;
            }

            if (serverState.DownloadInfoSpeed.HasValue && existingServerState.DownloadInfoSpeed != serverState.DownloadInfoSpeed.Value)
            {
                existingServerState.DownloadInfoSpeed = serverState.DownloadInfoSpeed.Value;
                changed = true;
            }

            if (serverState.DownloadRateLimit.HasValue && existingServerState.DownloadRateLimit != serverState.DownloadRateLimit.Value)
            {
                existingServerState.DownloadRateLimit = serverState.DownloadRateLimit.Value;
                changed = true;
            }

            if (serverState.FreeSpaceOnDisk.HasValue && existingServerState.FreeSpaceOnDisk != serverState.FreeSpaceOnDisk.Value)
            {
                existingServerState.FreeSpaceOnDisk = serverState.FreeSpaceOnDisk.Value;
                changed = true;
            }

            if (serverState.GlobalRatio.HasValue && existingServerState.GlobalRatio != serverState.GlobalRatio.Value)
            {
                existingServerState.GlobalRatio = serverState.GlobalRatio.Value;
                changed = true;
            }

            if (serverState.QueuedIOJobs.HasValue && existingServerState.QueuedIOJobs != serverState.QueuedIOJobs.Value)
            {
                existingServerState.QueuedIOJobs = serverState.QueuedIOJobs.Value;
                changed = true;
            }

            if (serverState.Queuing.HasValue && existingServerState.Queuing != serverState.Queuing.Value)
            {
                existingServerState.Queuing = serverState.Queuing.Value;
                changed = true;
            }

            if (serverState.ReadCacheHits.HasValue && existingServerState.ReadCacheHits != serverState.ReadCacheHits.Value)
            {
                existingServerState.ReadCacheHits = serverState.ReadCacheHits.Value;
                changed = true;
            }

            if (serverState.ReadCacheOverload.HasValue && existingServerState.ReadCacheOverload != serverState.ReadCacheOverload.Value)
            {
                existingServerState.ReadCacheOverload = serverState.ReadCacheOverload.Value;
                changed = true;
            }

            if (serverState.RefreshInterval.HasValue && existingServerState.RefreshInterval != serverState.RefreshInterval.Value)
            {
                existingServerState.RefreshInterval = serverState.RefreshInterval.Value;
                changed = true;
            }

            if (serverState.TotalBuffersSize.HasValue && existingServerState.TotalBuffersSize != serverState.TotalBuffersSize.Value)
            {
                existingServerState.TotalBuffersSize = serverState.TotalBuffersSize.Value;
                changed = true;
            }

            if (serverState.TotalPeerConnections.HasValue && existingServerState.TotalPeerConnections != serverState.TotalPeerConnections.Value)
            {
                existingServerState.TotalPeerConnections = serverState.TotalPeerConnections.Value;
                changed = true;
            }

            if (serverState.TotalQueuedSize.HasValue && existingServerState.TotalQueuedSize != serverState.TotalQueuedSize.Value)
            {
                existingServerState.TotalQueuedSize = serverState.TotalQueuedSize.Value;
                changed = true;
            }

            if (serverState.TotalWastedSession.HasValue && existingServerState.TotalWastedSession != serverState.TotalWastedSession.Value)
            {
                existingServerState.TotalWastedSession = serverState.TotalWastedSession.Value;
                changed = true;
            }

            if (serverState.UploadInfoData.HasValue && existingServerState.UploadInfoData != serverState.UploadInfoData.Value)
            {
                existingServerState.UploadInfoData = serverState.UploadInfoData.Value;
                changed = true;
            }

            if (serverState.UploadInfoSpeed.HasValue && existingServerState.UploadInfoSpeed != serverState.UploadInfoSpeed.Value)
            {
                existingServerState.UploadInfoSpeed = serverState.UploadInfoSpeed.Value;
                changed = true;
            }

            if (serverState.UploadRateLimit.HasValue && existingServerState.UploadRateLimit != serverState.UploadRateLimit.Value)
            {
                existingServerState.UploadRateLimit = serverState.UploadRateLimit.Value;
                changed = true;
            }

            if (serverState.UseAltSpeedLimits.HasValue && existingServerState.UseAltSpeedLimits != serverState.UseAltSpeedLimits.Value)
            {
                existingServerState.UseAltSpeedLimits = serverState.UseAltSpeedLimits.Value;
                changed = true;
            }

            if (serverState.UseSubcategories.HasValue && existingServerState.UseSubcategories != serverState.UseSubcategories.Value)
            {
                existingServerState.UseSubcategories = serverState.UseSubcategories.Value;
                changed = true;
            }

            if (serverState.WriteCacheOverload.HasValue && existingServerState.WriteCacheOverload != serverState.WriteCacheOverload.Value)
            {
                existingServerState.WriteCacheOverload = serverState.WriteCacheOverload.Value;
                changed = true;
            }

            return changed;
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
                peer.Downloaded.GetValueOrDefault(),
                peer.DownloadSpeed.GetValueOrDefault(),
                peer.Files!,
                peer.Flags!,
                peer.FlagsDescription!,
                peer.IPAddress!,
                peer.Port.GetValueOrDefault(),
                peer.Progress.GetValueOrDefault(),
                peer.Relevance.GetValueOrDefault(),
                peer.Uploaded.GetValueOrDefault(),
                peer.UploadSpeed.GetValueOrDefault());
        }

        public Torrent CreateTorrent(string hash, QBitTorrentClient.Models.Torrent torrent)
        {
            var normalizedTags = torrent.Tags?
                .Select(NormalizeTag)
                .Where(static tag => !string.IsNullOrEmpty(tag))
                .ToList()
                ?? new List<string>();

            return new Torrent(
                hash,
                torrent.AddedOn.GetValueOrDefault(),
                torrent.AmountLeft.GetValueOrDefault(),
                torrent.AutomaticTorrentManagement.GetValueOrDefault(),
                torrent.Availability.GetValueOrDefault(),
                torrent.Category!,
                torrent.Completed.GetValueOrDefault(),
                torrent.CompletionOn.GetValueOrDefault(),
                torrent.ContentPath!,
                torrent.DownloadLimit.GetValueOrDefault(),
                torrent.DownloadSpeed.GetValueOrDefault(),
                torrent.Downloaded.GetValueOrDefault(),
                torrent.DownloadedSession.GetValueOrDefault(),
                torrent.EstimatedTimeOfArrival.GetValueOrDefault(),
                torrent.FirstLastPiecePriority.GetValueOrDefault(),
                torrent.ForceStart.GetValueOrDefault(),
                torrent.InfoHashV1!,
                torrent.InfoHashV2!,
                torrent.LastActivity.GetValueOrDefault(),
                torrent.MagnetUri!,
                torrent.MaxRatio.GetValueOrDefault(),
                torrent.MaxSeedingTime.GetValueOrDefault(),
                torrent.Name!,
                torrent.NumberComplete.GetValueOrDefault(),
                torrent.NumberIncomplete.GetValueOrDefault(),
                torrent.NumberLeeches.GetValueOrDefault(),
                torrent.NumberSeeds.GetValueOrDefault(),
                torrent.Priority.GetValueOrDefault(),
                torrent.Progress.GetValueOrDefault(),
                torrent.Ratio.GetValueOrDefault(),
                torrent.RatioLimit.GetValueOrDefault(),
                torrent.SavePath!,
                torrent.SeedingTime.GetValueOrDefault(),
                torrent.SeedingTimeLimit.GetValueOrDefault(),
                torrent.SeenComplete.GetValueOrDefault(),
                torrent.SequentialDownload.GetValueOrDefault(),
                torrent.Size.GetValueOrDefault(),
                torrent.State!,
                torrent.SuperSeeding.GetValueOrDefault(),
                normalizedTags,
                torrent.TimeActive.GetValueOrDefault(),
                torrent.TotalSize.GetValueOrDefault(),
                torrent.Tracker!,
                torrent.UploadLimit.GetValueOrDefault(),
                torrent.Uploaded.GetValueOrDefault(),
                torrent.UploadedSession.GetValueOrDefault(),
                torrent.UploadSpeed.GetValueOrDefault(),
                torrent.Reannounce ?? 0,
                torrent.InactiveSeedingTimeLimit.GetValueOrDefault(),
                torrent.MaxInactiveSeedingTime.GetValueOrDefault());
        }

        private static string NormalizeTag(string? tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return string.Empty;
            }

            var separatorIndex = tag.IndexOf('\t');
            var normalized = (separatorIndex >= 0) ? tag[..separatorIndex] : tag;

            return normalized.Trim();
        }

        private static TorrentSnapshot CreateSnapshot(Torrent torrent)
        {
            return new TorrentSnapshot(
                string.IsNullOrEmpty(torrent.Category) ? null : torrent.Category,
                torrent.Tags.ToList(),
                torrent.Tracker ?? string.Empty,
                torrent.State ?? string.Empty,
                torrent.UploadSpeed);
        }

        private readonly struct TorrentSnapshot
        {
            public TorrentSnapshot(string? category, List<string> tags, string tracker, string state, long uploadSpeed)
            {
                Category = category;
                Tags = tags;
                Tracker = tracker;
                State = state;
                UploadSpeed = uploadSpeed;
            }

            public string? Category { get; }

            public IReadOnlyList<string> Tags { get; }

            public string Tracker { get; }

            public string State { get; }

            public long UploadSpeed { get; }
        }

        private static void UpdateTagStateForAddition(MainData torrentList, Torrent torrent, string hash)
        {
            if (torrent.Tags.Count == 0)
            {
                torrentList.TagState[FilterHelper.TAG_UNTAGGED].Add(hash);
                return;
            }

            torrentList.TagState[FilterHelper.TAG_UNTAGGED].Remove(hash);
            foreach (var tag in torrent.Tags)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                GetOrCreateTagSet(torrentList, tag).Add(hash);
            }
        }

        private static void UpdateTagStateForUpdate(MainData torrentList, string hash, IReadOnlyList<string> previousTags, IList<string> newTags)
        {
            UpdateTagStateForRemoval(torrentList, hash, previousTags);

            if (newTags.Count == 0)
            {
                torrentList.TagState[FilterHelper.TAG_UNTAGGED].Add(hash);
                return;
            }

            torrentList.TagState[FilterHelper.TAG_UNTAGGED].Remove(hash);
            foreach (var tag in newTags)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                GetOrCreateTagSet(torrentList, tag).Add(hash);
            }
        }

        private static void UpdateTagStateForRemoval(MainData torrentList, string hash, IReadOnlyList<string> previousTags)
        {
            torrentList.TagState[FilterHelper.TAG_UNTAGGED].Remove(hash);

            foreach (var tag in previousTags)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                if (torrentList.TagState.TryGetValue(tag, out var set))
                {
                    set.Remove(hash);
                }
            }
        }

        private static void UpdateCategoryState(MainData torrentList, Torrent updatedTorrent, string hash, string? previousCategory)
        {
            var useSubcategories = torrentList.ServerState.UseSubcategories;

            if (!string.IsNullOrEmpty(previousCategory))
            {
                foreach (var categoryKey in EnumerateCategoryKeys(previousCategory, useSubcategories))
                {
                    if (torrentList.CategoriesState.TryGetValue(categoryKey, out var set))
                    {
                        set.Remove(hash);
                    }
                }
            }
            else
            {
                torrentList.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Remove(hash);
            }

            if (string.IsNullOrEmpty(updatedTorrent.Category))
            {
                torrentList.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Add(hash);
                return;
            }

            foreach (var categoryKey in EnumerateCategoryKeys(updatedTorrent.Category, useSubcategories))
            {
                GetOrCreateCategorySet(torrentList, categoryKey).Add(hash);
            }
        }

        private static void UpdateCategoryStateForRemoval(MainData torrentList, string hash, string? previousCategory)
        {
            if (string.IsNullOrEmpty(previousCategory))
            {
                torrentList.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Remove(hash);
                return;
            }

            foreach (var categoryKey in EnumerateCategoryKeys(previousCategory, torrentList.ServerState.UseSubcategories))
            {
                if (torrentList.CategoriesState.TryGetValue(categoryKey, out var set))
                {
                    set.Remove(hash);
                }
            }
        }

        private static void UpdateStatusState(MainData torrentList, string hash, string previousState, long previousUploadSpeed, string newState, long newUploadSpeed)
        {
            foreach (var status in GetStatuses())
            {
                if (!torrentList.StatusState.TryGetValue(status.ToString(), out var statusSet))
                {
                    continue;
                }

                var wasMatch = FilterHelper.FilterStatus(previousState, previousUploadSpeed, status);
                var isMatch = FilterHelper.FilterStatus(newState, newUploadSpeed, status);

                if (wasMatch == isMatch)
                {
                    continue;
                }

                if (wasMatch)
                {
                    statusSet.Remove(hash);
                }

                if (isMatch)
                {
                    statusSet.Add(hash);
                }
            }
        }

        private static void UpdateTrackerState(MainData torrentList, Torrent updatedTorrent, string hash, string? previousTracker)
        {
            if (!string.IsNullOrEmpty(previousTracker))
            {
                if (torrentList.TrackersState.TryGetValue(previousTracker, out var oldSet))
                {
                    oldSet.Remove(hash);
                }
            }
            else
            {
                torrentList.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Remove(hash);
            }

            var tracker = updatedTorrent.Tracker ?? string.Empty;
            if (string.IsNullOrEmpty(tracker))
            {
                torrentList.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Add(hash);
                return;
            }

            torrentList.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Remove(hash);
            GetOrCreateTrackerSet(torrentList, tracker).Add(hash);
        }

        private static void UpdateTrackerStateForRemoval(MainData torrentList, string hash, string? previousTracker)
        {
            if (string.IsNullOrEmpty(previousTracker))
            {
                torrentList.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Remove(hash);
                return;
            }

            if (torrentList.TrackersState.TryGetValue(previousTracker, out var trackerSet))
            {
                trackerSet.Remove(hash);
            }
        }

        private static IEnumerable<string> EnumerateCategoryKeys(string category, bool useSubcategories)
        {
            if (string.IsNullOrEmpty(category))
            {
                yield break;
            }

            yield return category;

            if (!useSubcategories)
            {
                yield break;
            }

            var current = category;
            while (true)
            {
                var separatorIndex = current.LastIndexOf('/');
                if (separatorIndex < 0)
                {
                    yield break;
                }

                current = current[..separatorIndex];
                yield return current;
            }
        }

        private static HashSet<string> GetOrCreateTagSet(MainData torrentList, string tag)
        {
            if (!torrentList.TagState.TryGetValue(tag, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                torrentList.TagState[tag] = set;
            }

            return set;
        }

        private static HashSet<string> GetOrCreateCategorySet(MainData torrentList, string category)
        {
            if (!torrentList.CategoriesState.TryGetValue(category, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                torrentList.CategoriesState[category] = set;
            }

            return set;
        }

        private static HashSet<string> GetOrCreateTrackerSet(MainData torrentList, string tracker)
        {
            if (!torrentList.TrackersState.TryGetValue(tracker, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                torrentList.TrackersState[tracker] = set;
            }

            return set;
        }

        private static bool UpdateCategory(Category existingCategory, QBitTorrentClient.Models.Category category)
        {
            if (category.SavePath is not null && existingCategory.SavePath != category.SavePath)
            {
                existingCategory.SavePath = category.SavePath;
                return true;
            }

            return false;
        }

        private readonly struct TorrentUpdateResult
        {
            public TorrentUpdateResult(bool dataChanged, bool filterChanged)
            {
                DataChanged = dataChanged;
                FilterChanged = filterChanged;
            }

            public bool DataChanged { get; }

            public bool FilterChanged { get; }
        }

        private static TorrentUpdateResult UpdateTorrent(Torrent existingTorrent, QBitTorrentClient.Models.Torrent torrent)
        {
            var dataChanged = false;
            var filterChanged = false;

            if (torrent.AddedOn.HasValue && existingTorrent.AddedOn != torrent.AddedOn.Value)
            {
                existingTorrent.AddedOn = torrent.AddedOn.Value;
                dataChanged = true;
            }

            if (torrent.AmountLeft.HasValue && existingTorrent.AmountLeft != torrent.AmountLeft.Value)
            {
                existingTorrent.AmountLeft = torrent.AmountLeft.Value;
                dataChanged = true;
            }

            if (torrent.AutomaticTorrentManagement.HasValue && existingTorrent.AutomaticTorrentManagement != torrent.AutomaticTorrentManagement.Value)
            {
                existingTorrent.AutomaticTorrentManagement = torrent.AutomaticTorrentManagement.Value;
                dataChanged = true;
            }

            if (torrent.Availability.HasValue && existingTorrent.Availability != torrent.Availability.Value)
            {
                existingTorrent.Availability = torrent.Availability.Value;
                dataChanged = true;
            }

            if (torrent.Category is not null && existingTorrent.Category != torrent.Category)
            {
                existingTorrent.Category = torrent.Category;
                dataChanged = true;
                filterChanged = true;
            }

            if (torrent.Completed.HasValue && existingTorrent.Completed != torrent.Completed.Value)
            {
                existingTorrent.Completed = torrent.Completed.Value;
                dataChanged = true;
            }

            if (torrent.CompletionOn.HasValue && existingTorrent.CompletionOn != torrent.CompletionOn.Value)
            {
                existingTorrent.CompletionOn = torrent.CompletionOn.Value;
                dataChanged = true;
            }

            if (torrent.ContentPath is not null && existingTorrent.ContentPath != torrent.ContentPath)
            {
                existingTorrent.ContentPath = torrent.ContentPath;
                dataChanged = true;
            }

            if (torrent.Downloaded.HasValue && existingTorrent.Downloaded != torrent.Downloaded.Value)
            {
                existingTorrent.Downloaded = torrent.Downloaded.Value;
                dataChanged = true;
            }

            if (torrent.DownloadedSession.HasValue && existingTorrent.DownloadedSession != torrent.DownloadedSession.Value)
            {
                existingTorrent.DownloadedSession = torrent.DownloadedSession.Value;
                dataChanged = true;
            }

            if (torrent.DownloadLimit.HasValue && existingTorrent.DownloadLimit != torrent.DownloadLimit.Value)
            {
                existingTorrent.DownloadLimit = torrent.DownloadLimit.Value;
                dataChanged = true;
            }

            if (torrent.DownloadSpeed.HasValue && existingTorrent.DownloadSpeed != torrent.DownloadSpeed.Value)
            {
                existingTorrent.DownloadSpeed = torrent.DownloadSpeed.Value;
                dataChanged = true;
            }

            if (torrent.EstimatedTimeOfArrival.HasValue && existingTorrent.EstimatedTimeOfArrival != torrent.EstimatedTimeOfArrival.Value)
            {
                existingTorrent.EstimatedTimeOfArrival = torrent.EstimatedTimeOfArrival.Value;
                dataChanged = true;
            }

            if (torrent.FirstLastPiecePriority.HasValue && existingTorrent.FirstLastPiecePriority != torrent.FirstLastPiecePriority.Value)
            {
                existingTorrent.FirstLastPiecePriority = torrent.FirstLastPiecePriority.Value;
                dataChanged = true;
            }

            if (torrent.ForceStart.HasValue && existingTorrent.ForceStart != torrent.ForceStart.Value)
            {
                existingTorrent.ForceStart = torrent.ForceStart.Value;
                dataChanged = true;
            }

            if (torrent.InfoHashV1 is not null && existingTorrent.InfoHashV1 != torrent.InfoHashV1)
            {
                existingTorrent.InfoHashV1 = torrent.InfoHashV1;
                dataChanged = true;
            }

            if (torrent.InfoHashV2 is not null && existingTorrent.InfoHashV2 != torrent.InfoHashV2)
            {
                existingTorrent.InfoHashV2 = torrent.InfoHashV2;
                dataChanged = true;
            }

            if (torrent.LastActivity.HasValue && existingTorrent.LastActivity != torrent.LastActivity.Value)
            {
                existingTorrent.LastActivity = torrent.LastActivity.Value;
                dataChanged = true;
            }

            if (torrent.MagnetUri is not null && existingTorrent.MagnetUri != torrent.MagnetUri)
            {
                existingTorrent.MagnetUri = torrent.MagnetUri;
                dataChanged = true;
            }

            if (torrent.MaxRatio.HasValue && existingTorrent.MaxRatio != torrent.MaxRatio.Value)
            {
                existingTorrent.MaxRatio = torrent.MaxRatio.Value;
                dataChanged = true;
            }

            if (torrent.MaxSeedingTime.HasValue && existingTorrent.MaxSeedingTime != torrent.MaxSeedingTime.Value)
            {
                existingTorrent.MaxSeedingTime = torrent.MaxSeedingTime.Value;
                dataChanged = true;
            }

            if (torrent.Name is not null && existingTorrent.Name != torrent.Name)
            {
                existingTorrent.Name = torrent.Name;
                dataChanged = true;
                filterChanged = true;
            }

            if (torrent.NumberComplete.HasValue && existingTorrent.NumberComplete != torrent.NumberComplete.Value)
            {
                existingTorrent.NumberComplete = torrent.NumberComplete.Value;
                dataChanged = true;
            }

            if (torrent.NumberIncomplete.HasValue && existingTorrent.NumberIncomplete != torrent.NumberIncomplete.Value)
            {
                existingTorrent.NumberIncomplete = torrent.NumberIncomplete.Value;
                dataChanged = true;
            }

            if (torrent.NumberLeeches.HasValue && existingTorrent.NumberLeeches != torrent.NumberLeeches.Value)
            {
                existingTorrent.NumberLeeches = torrent.NumberLeeches.Value;
                dataChanged = true;
            }

            if (torrent.NumberSeeds.HasValue && existingTorrent.NumberSeeds != torrent.NumberSeeds.Value)
            {
                existingTorrent.NumberSeeds = torrent.NumberSeeds.Value;
                dataChanged = true;
            }

            if (torrent.Priority.HasValue && existingTorrent.Priority != torrent.Priority.Value)
            {
                existingTorrent.Priority = torrent.Priority.Value;
                dataChanged = true;
            }

            if (torrent.Progress.HasValue && existingTorrent.Progress != torrent.Progress.Value)
            {
                existingTorrent.Progress = torrent.Progress.Value;
                dataChanged = true;
            }

            if (torrent.Ratio.HasValue && existingTorrent.Ratio != torrent.Ratio.Value)
            {
                existingTorrent.Ratio = torrent.Ratio.Value;
                dataChanged = true;
            }

            if (torrent.RatioLimit.HasValue && existingTorrent.RatioLimit != torrent.RatioLimit.Value)
            {
                existingTorrent.RatioLimit = torrent.RatioLimit.Value;
                dataChanged = true;
            }

            if (torrent.SavePath is not null && existingTorrent.SavePath != torrent.SavePath)
            {
                existingTorrent.SavePath = torrent.SavePath;
                dataChanged = true;
            }

            if (torrent.SeedingTime.HasValue && existingTorrent.SeedingTime != torrent.SeedingTime.Value)
            {
                existingTorrent.SeedingTime = torrent.SeedingTime.Value;
                dataChanged = true;
            }

            if (torrent.SeedingTimeLimit.HasValue && existingTorrent.SeedingTimeLimit != torrent.SeedingTimeLimit.Value)
            {
                existingTorrent.SeedingTimeLimit = torrent.SeedingTimeLimit.Value;
                dataChanged = true;
            }

            if (torrent.SeenComplete.HasValue && existingTorrent.SeenComplete != torrent.SeenComplete.Value)
            {
                existingTorrent.SeenComplete = torrent.SeenComplete.Value;
                dataChanged = true;
            }

            if (torrent.SequentialDownload.HasValue && existingTorrent.SequentialDownload != torrent.SequentialDownload.Value)
            {
                existingTorrent.SequentialDownload = torrent.SequentialDownload.Value;
                dataChanged = true;
            }

            if (torrent.Size.HasValue && existingTorrent.Size != torrent.Size.Value)
            {
                existingTorrent.Size = torrent.Size.Value;
                dataChanged = true;
            }

            if (torrent.State is not null && existingTorrent.State != torrent.State)
            {
                existingTorrent.State = torrent.State;
                dataChanged = true;
                filterChanged = true;
            }

            if (torrent.SuperSeeding.HasValue && existingTorrent.SuperSeeding != torrent.SuperSeeding.Value)
            {
                existingTorrent.SuperSeeding = torrent.SuperSeeding.Value;
                dataChanged = true;
            }

            if (torrent.Tags is not null)
            {
                var normalizedTags = torrent.Tags.Select(NormalizeTag)
                                    .Where(static tag => !string.IsNullOrEmpty(tag))
                                    .ToList();

                if (!existingTorrent.Tags.SequenceEqual(normalizedTags))
                {
                    existingTorrent.Tags.Clear();
                    existingTorrent.Tags.AddRange(normalizedTags);
                    dataChanged = true;
                    filterChanged = true;
                }
            }

            if (torrent.TimeActive.HasValue && existingTorrent.TimeActive != torrent.TimeActive.Value)
            {
                existingTorrent.TimeActive = torrent.TimeActive.Value;
                dataChanged = true;
            }

            if (torrent.TotalSize.HasValue && existingTorrent.TotalSize != torrent.TotalSize.Value)
            {
                existingTorrent.TotalSize = torrent.TotalSize.Value;
                dataChanged = true;
            }

            if (torrent.Tracker is not null && existingTorrent.Tracker != torrent.Tracker)
            {
                existingTorrent.Tracker = torrent.Tracker;
                dataChanged = true;
                filterChanged = true;
            }

            if (torrent.UploadLimit.HasValue && existingTorrent.UploadLimit != torrent.UploadLimit.Value)
            {
                existingTorrent.UploadLimit = torrent.UploadLimit.Value;
                dataChanged = true;
            }

            if (torrent.Uploaded.HasValue && existingTorrent.Uploaded != torrent.Uploaded.Value)
            {
                existingTorrent.Uploaded = torrent.Uploaded.Value;
                dataChanged = true;
            }

            if (torrent.UploadedSession.HasValue && existingTorrent.UploadedSession != torrent.UploadedSession.Value)
            {
                existingTorrent.UploadedSession = torrent.UploadedSession.Value;
                dataChanged = true;
            }

            var previousUploadSpeed = existingTorrent.UploadSpeed;
            if (torrent.UploadSpeed.HasValue && previousUploadSpeed != torrent.UploadSpeed.Value)
            {
                existingTorrent.UploadSpeed = torrent.UploadSpeed.Value;
                dataChanged = true;
                if ((previousUploadSpeed > 0) != (torrent.UploadSpeed.Value > 0))
                {
                    filterChanged = true;
                }
            }

            if (torrent.Reannounce.HasValue && existingTorrent.Reannounce != torrent.Reannounce.Value)
            {
                existingTorrent.Reannounce = torrent.Reannounce.Value;
                dataChanged = true;
            }

            if (torrent.InactiveSeedingTimeLimit.HasValue && existingTorrent.InactiveSeedingTimeLimit != torrent.InactiveSeedingTimeLimit.Value)
            {
                existingTorrent.InactiveSeedingTimeLimit = torrent.InactiveSeedingTimeLimit.Value;
                dataChanged = true;
            }

            if (torrent.MaxInactiveSeedingTime.HasValue && existingTorrent.MaxInactiveSeedingTime != torrent.MaxInactiveSeedingTime.Value)
            {
                existingTorrent.MaxInactiveSeedingTime = torrent.MaxInactiveSeedingTime.Value;
                dataChanged = true;
            }

            return new TorrentUpdateResult(dataChanged, filterChanged);
        }

        public Dictionary<string, ContentItem> CreateContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files)
        {
            return BuildContentsTree(files);
        }

        private static Dictionary<string, ContentItem> BuildContentsTree(IReadOnlyList<QBitTorrentClient.Models.FileData> files)
        {
            var result = new Dictionary<string, ContentItem>();
            if (files.Count == 0)
            {
                return result;
            }

            var folderIndex = files.Min(f => f.Index) - 1;
            var nodes = new Dictionary<string, ContentTreeNode>(files.Count * 2);
            var root = new ContentTreeNode(null, null);

            foreach (var file in files)
            {
                var parent = root;
                string? parentPath = parent.Item?.Name;

                var segments = file.Name.Split(Extensions.DirectorySeparator);
                var directoriesLength = segments.Length - 1;

                for (var i = 0; i < directoriesLength; i++)
                {
                    var folderName = segments[i];
                    if (folderName == ".unwanted")
                    {
                        continue;
                    }

                    var folderPath = string.IsNullOrEmpty(parentPath)
                        ? folderName
                        : string.Concat(parentPath, Extensions.DirectorySeparator, folderName);

                    if (!nodes.TryGetValue(folderPath, out var folderNode))
                    {
                        var level = (parent.Item?.Level ?? -1) + 1;
                        var folderItem = new ContentItem(folderPath, folderName, folderIndex--, Priority.Normal, 0, 0, 0, true, level);
                        folderNode = new ContentTreeNode(folderItem, parent);
                        nodes[folderPath] = folderNode;
                        parent.Children[folderPath] = folderNode;
                    }

                    parent = folderNode;
                    parentPath = parent.Item!.Name;
                }

                var displayName = segments[^1];
                var fileLevel = (parent.Item?.Level ?? -1) + 1;
                var fileItem = new ContentItem(file.Name, displayName, file.Index, (Priority)(int)file.Priority, file.Progress, file.Size, file.Availability, false, fileLevel);
                var fileNode = new ContentTreeNode(fileItem, parent);
                nodes[file.Name] = fileNode;
                parent.Children[fileItem.Name] = fileNode;
            }

            var folders = nodes.Values
                .Where(n => n.Item is not null && n.Item.IsFolder)
                .OrderByDescending(n => n.Item!.Level)
                .ToList();

            foreach (var folder in folders)
            {
                var folderItem = folder.Item!;
                if (folder.Children.Count == 0)
                {
                    folderItem.Size = 0;
                    folderItem.Progress = 0;
                    folderItem.Availability = 0;
                    folderItem.Priority = Priority.Normal;
                    continue;
                }

                long sizeSum = 0;
                double progressSum = 0;
                double availabilitySum = 0;
                var firstChild = true;
                var aggregatedPriority = Priority.Normal;

                foreach (var child in folder.Children.Values)
                {
                    var childItem = child.Item!;
                    sizeSum += childItem.Size;

                    if (firstChild)
                    {
                        aggregatedPriority = childItem.Priority;
                        firstChild = false;
                    }
                    else if (aggregatedPriority != childItem.Priority)
                    {
                        aggregatedPriority = Priority.Mixed;
                    }

                    if (childItem.Priority != Priority.DoNotDownload)
                    {
                        progressSum += childItem.Progress * childItem.Size;
                        availabilitySum += childItem.Availability * childItem.Size;
                    }
                }

                folderItem.Size = sizeSum;
                folderItem.Progress = sizeSum > 0 ? (float)(progressSum / sizeSum) : 0;
                folderItem.Availability = sizeSum > 0 ? (float)(availabilitySum / sizeSum) : 0;
                folderItem.Priority = firstChild ? Priority.Normal : aggregatedPriority;
            }

            foreach (var node in nodes.Values)
            {
                if (node.Item is null)
                {
                    continue;
                }

                result[node.Item.Name] = node.Item;
            }

            return result;
        }

        private static bool UpdateContentItem(ContentItem destination, ContentItem source)
        {
            const float floatTolerance = 0.0001f;
            var changed = false;

            if (destination.Priority != source.Priority)
            {
                destination.Priority = source.Priority;
                changed = true;
            }

            if (System.Math.Abs(destination.Progress - source.Progress) > floatTolerance)
            {
                destination.Progress = source.Progress;
                changed = true;
            }

            if (destination.Size != source.Size)
            {
                destination.Size = source.Size;
                changed = true;
            }

            if (System.Math.Abs(destination.Availability - source.Availability) > floatTolerance)
            {
                destination.Availability = source.Availability;
                changed = true;
            }

            return changed;
        }

        private struct DirectoryAccumulator
        {
            public long TotalSize { get; private set; }

            private long _activeSize;
            private double _progressSum;
            private double _availabilitySum;
            private Priority? _priority;
            private bool _mixedPriority;

            public void Add(Priority priority, float progress, long size, float availability)
            {
                TotalSize += size;

                if (priority != Priority.DoNotDownload)
                {
                    _activeSize += size;
                    _progressSum += progress * size;
                    _availabilitySum += availability * size;
                }

                if (!_priority.HasValue)
                {
                    _priority = priority;
                }
                else if (_priority.Value != priority)
                {
                    _mixedPriority = true;
                }
            }

            public Priority ResolvePriority()
            {
                if (_mixedPriority)
                {
                    return Priority.Mixed;
                }

                return _priority ?? Priority.Normal;
            }

            public float ResolveProgress()
            {
                if (_activeSize == 0 || TotalSize == 0)
                {
                    return 0f;
                }

                var value = _progressSum / _activeSize;
                if (value < 0)
                {
                    return 0f;
                }

                if (value > 1)
                {
                    return 1f;
                }

                return (float)value;
            }

            public float ResolveAvailability()
            {
                if (_activeSize == 0 || TotalSize == 0)
                {
                    return 0f;
                }

                return (float)(_availabilitySum / _activeSize);
            }
        }

        private sealed class ContentTreeNode
        {
            public ContentTreeNode(ContentItem? item, ContentTreeNode? parent)
            {
                Item = item;
                Parent = parent;
                Children = new Dictionary<string, ContentTreeNode>();
            }

            public ContentItem? Item { get; }

            public ContentTreeNode? Parent { get; }

            public Dictionary<string, ContentTreeNode> Children { get; }
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
                    AddStoppedEnabled = changed.AddStoppedEnabled,
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
                original.AddStoppedEnabled = changed.AddStoppedEnabled ?? original.AddStoppedEnabled;
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

        public bool MergeContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files, Dictionary<string, ContentItem> contents)
        {
            if (files.Count == 0)
            {
                if (contents.Count == 0)
                {
                    return false;
                }

                contents.Clear();
                return true;
            }

            var hasChanges = false;
            var seenPaths = new HashSet<string>(files.Count * 2);
            var directoryAccumulators = new Dictionary<string, DirectoryAccumulator>();

            var minExistingIndex = contents.Count == 0
                ? int.MaxValue
                : contents.Values.Min(c => c.Index);
            var minFileIndex = files.Min(f => f.Index);
            var nextFolderIndex = System.Math.Min(minExistingIndex, minFileIndex) - 1;

            foreach (var file in files)
            {
                var priority = (Priority)(int)file.Priority;
                var pathSegments = file.Name.Split(Extensions.DirectorySeparator);
                var level = pathSegments.Length - 1;
                var displayName = pathSegments[^1];
                var filePath = file.Name;
                seenPaths.Add(filePath);

                if (contents.TryGetValue(filePath, out var existingFile))
                {
                    var updatedFile = new ContentItem(filePath, displayName, file.Index, priority, file.Progress, file.Size, file.Availability, false, level);
                    if (UpdateContentItem(existingFile, updatedFile))
                    {
                        hasChanges = true;
                    }
                }
                else
                {
                    var newFile = new ContentItem(filePath, displayName, file.Index, priority, file.Progress, file.Size, file.Availability, false, level);
                    contents[filePath] = newFile;
                    hasChanges = true;
                }

                string directoryPath = string.Empty;
                for (var i = 0; i < level; i++)
                {
                    var segment = pathSegments[i];
                    if (segment == ".unwanted")
                    {
                        continue;
                    }

                    directoryPath = string.IsNullOrEmpty(directoryPath)
                        ? segment
                        : string.Concat(directoryPath, Extensions.DirectorySeparator, segment);

                    seenPaths.Add(directoryPath);

                    if (!contents.TryGetValue(directoryPath, out var directoryItem))
                    {
                        var newDirectory = new ContentItem(directoryPath, segment, nextFolderIndex--, Priority.Normal, 0, 0, 0, true, i);
                        contents[directoryPath] = newDirectory;
                        hasChanges = true;
                    }

                    if (!directoryAccumulators.TryGetValue(directoryPath, out var accumulator))
                    {
                        accumulator = new DirectoryAccumulator();
                    }

                    accumulator.Add(priority, file.Progress, file.Size, file.Availability);
                    directoryAccumulators[directoryPath] = accumulator;
                }
            }

            var keysToRemove = contents.Keys.Where(key => !seenPaths.Contains(key)).ToList();
            if (keysToRemove.Count != 0)
            {
                hasChanges = true;
                foreach (var key in keysToRemove)
                {
                    contents.Remove(key);
                }
            }

            foreach (var (directoryPath, accumulator) in directoryAccumulators)
            {
                if (!contents.TryGetValue(directoryPath, out var directoryItem))
                {
                    continue;
                }

                var updatedDirectory = new ContentItem(
                    directoryPath,
                    directoryItem.DisplayName,
                    directoryItem.Index,
                    accumulator.ResolvePriority(),
                    accumulator.ResolveProgress(),
                    accumulator.TotalSize,
                    accumulator.ResolveAvailability(),
                    true,
                    directoryItem.Level);

                if (UpdateContentItem(directoryItem, updatedDirectory))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        public RssList CreateRssList(IReadOnlyDictionary<string, QBitTorrentClient.Models.RssItem> rssItems)
        {
            var articles = new List<RssArticle>();
            var feeds = new Dictionary<string, RssFeed>();
            foreach (var (key, rssItem) in rssItems)
            {
                feeds.Add(key, new RssFeed(rssItem.HasError, rssItem.IsLoading, rssItem.LastBuildDate, rssItem.Title, rssItem.Uid, rssItem.Url));
                if (rssItem.Articles is null)
                {
                    continue;
                }
                foreach (var rssArticle in rssItem.Articles)
                {
                    var article = new RssArticle(
                        key,
                        rssArticle.Category,
                        rssArticle.Comments,
                        rssArticle.Date!,
                        rssArticle.Description,
                        rssArticle.Id!,
                        rssArticle.Link,
                        rssArticle.Thumbnail,
                        rssArticle.Title!,
                        rssArticle.TorrentURL!,
                        rssArticle.IsRead);

                    articles.Add(article);
                }
            }

            return new RssList(feeds, articles);
        }
    }
}
