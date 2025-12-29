using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using ShareLimitAction = Lantean.QBitTorrentClient.Models.ShareLimitAction;

namespace Lantean.QBTMud.Services
{
    public class TorrentDataManager : ITorrentDataManager
    {
        private static Status[]? _statusArray = null;

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

            var trackersState = new Dictionary<string, HashSet<string>>(trackers.Count + 5)
            {
                { FilterHelper.TRACKER_ALL, torrents.Keys.ToHashSet() },
                { FilterHelper.TRACKER_TRACKERLESS, torrents.Values.Where(t => FilterHelper.FilterTracker(t, FilterHelper.TRACKER_TRACKERLESS)).ToHashesHashSet() },
                { FilterHelper.TRACKER_ERROR, torrents.Values.Where(t => FilterHelper.FilterTracker(t, FilterHelper.TRACKER_ERROR)).ToHashesHashSet() },
                { FilterHelper.TRACKER_WARNING, torrents.Values.Where(t => FilterHelper.FilterTracker(t, FilterHelper.TRACKER_WARNING)).ToHashesHashSet() },
                { FilterHelper.TRACKER_ANNOUNCE_ERROR, torrents.Values.Where(t => FilterHelper.FilterTracker(t, FilterHelper.TRACKER_ANNOUNCE_ERROR)).ToHashesHashSet() }
            };

            foreach (var (tracker, hashes) in trackers)
            {
                trackersState[tracker] = hashes.Where(torrents.ContainsKey).ToHashSet();
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
                serverState.ConnectionStatus ?? string.Empty,
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
                serverState.WriteCacheOverload.GetValueOrDefault(),
                serverState.LastExternalAddressV4 ?? string.Empty,
                serverState.LastExternalAddressV6 ?? string.Empty);
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

                    var removedFromTags = torrentList.Tags.Remove(normalizedTag);
                    var removedFromState = torrentList.TagState.Remove(normalizedTag);

                    var affectedHashes = new List<string>();
                    foreach (var (hash, torrent) in torrentList.Torrents)
                    {
                        if (!torrent.Tags.Remove(normalizedTag))
                        {
                            continue;
                        }

                        affectedHashes.Add(hash);
                        if (torrent.Tags.Count == 0)
                        {
                            torrentList.TagState[FilterHelper.TAG_UNTAGGED].Add(hash);
                        }
                    }

                    if (removedFromTags || affectedHashes.Count != 0)
                    {
                        dataChanged = true;
                    }

                    if (removedFromState || affectedHashes.Count != 0)
                    {
                        filterChanged = true;
                    }
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
                    if (torrentList.Torrents.TryGetValue(hash, out var existing))
                    {
                        var snapshot = CreateSnapshot(existing);
                        torrentList.Torrents.Remove(hash);

                        // remove from all filter sets using the captured snapshot
                        RemoveTorrentFromStates(torrentList, hash, snapshot);

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
                        filterChanged = true;

                        if (!torrentList.TrackersState.TryGetValue(url, out var trackerSet))
                        {
                            trackerSet = new HashSet<string>(StringComparer.Ordinal);
                            torrentList.TrackersState[url] = trackerSet;
                        }
                        else
                        {
                            trackerSet.Clear();
                        }

                        foreach (var hash in hashes.Where(torrentList.Torrents.ContainsKey))
                        {
                            trackerSet.Add(hash);
                        }
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
            UpdateTrackerState(torrentList, torrent, hash, previousSnapshot: null);
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
            UpdateTrackerState(torrentList, updatedTorrent, hash, previousSnapshot);
        }

        private static void RemoveTorrentFromStates(MainData torrentList, string hash, TorrentSnapshot snapshot)
        {
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
            UpdateTrackerStateForRemoval(torrentList, hash, snapshot);
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

            if (serverState.LastExternalAddressV4 is not null && existingServerState.LastExternalAddressV4 != serverState.LastExternalAddressV4)
            {
                existingServerState.LastExternalAddressV4 = serverState.LastExternalAddressV4;
                changed = true;
            }

            if (serverState.LastExternalAddressV6 is not null && existingServerState.LastExternalAddressV6 != serverState.LastExternalAddressV6)
            {
                existingServerState.LastExternalAddressV6 = serverState.LastExternalAddressV6;
                changed = true;
            }

            return changed;
        }

        private static Category CreateCategory(QBitTorrentClient.Models.Category category)
        {
            return new Category(category.Name, category.SavePath!);
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
                torrent.Category ?? string.Empty,
                torrent.Completed.GetValueOrDefault(),
                torrent.CompletionOn.GetValueOrDefault(),
                torrent.ContentPath ?? string.Empty,
                torrent.DownloadLimit.GetValueOrDefault(),
                torrent.DownloadSpeed.GetValueOrDefault(),
                torrent.Downloaded.GetValueOrDefault(),
                torrent.DownloadedSession.GetValueOrDefault(),
                torrent.EstimatedTimeOfArrival.GetValueOrDefault(),
                torrent.FirstLastPiecePriority.GetValueOrDefault(),
                torrent.ForceStart.GetValueOrDefault(),
                torrent.InfoHashV1 ?? string.Empty,
                torrent.InfoHashV2 ?? string.Empty,
                torrent.LastActivity.GetValueOrDefault(),
                torrent.MagnetUri ?? string.Empty,
                torrent.MaxRatio.GetValueOrDefault(),
                torrent.MaxSeedingTime.GetValueOrDefault(),
                torrent.Name ?? string.Empty,
                torrent.NumberComplete.GetValueOrDefault(),
                torrent.NumberIncomplete.GetValueOrDefault(),
                torrent.NumberLeeches.GetValueOrDefault(),
                torrent.NumberSeeds.GetValueOrDefault(),
                torrent.Priority.GetValueOrDefault(),
                torrent.Progress.GetValueOrDefault(),
                torrent.Ratio.GetValueOrDefault(),
                torrent.RatioLimit.GetValueOrDefault(),
                torrent.SavePath ?? string.Empty,
                torrent.SeedingTime.GetValueOrDefault(),
                torrent.SeedingTimeLimit.GetValueOrDefault(),
                torrent.SeenComplete.GetValueOrDefault(),
                torrent.SequentialDownload.GetValueOrDefault(),
                torrent.Size.GetValueOrDefault(),
                torrent.State ?? string.Empty,
                torrent.SuperSeeding.GetValueOrDefault(),
                normalizedTags,
                torrent.TimeActive.GetValueOrDefault(),
                torrent.TotalSize.GetValueOrDefault(),
                torrent.Tracker ?? string.Empty,
                torrent.TrackersCount.GetValueOrDefault(),
                torrent.HasTrackerError.GetValueOrDefault(),
                torrent.HasTrackerWarning.GetValueOrDefault(),
                torrent.HasOtherAnnounceError.GetValueOrDefault(),
                torrent.UploadLimit.GetValueOrDefault(),
                torrent.Uploaded.GetValueOrDefault(),
                torrent.UploadedSession.GetValueOrDefault(),
                torrent.UploadSpeed.GetValueOrDefault(),
                torrent.Reannounce ?? 0,
                torrent.InactiveSeedingTimeLimit.GetValueOrDefault(),
                torrent.MaxInactiveSeedingTime.GetValueOrDefault(),
                torrent.Popularity.GetValueOrDefault(),
                torrent.DownloadPath ?? string.Empty,
                torrent.RootPath ?? string.Empty,
                torrent.IsPrivate.GetValueOrDefault(),
                torrent.ShareLimitAction ?? ShareLimitAction.Default,
                torrent.Comment ?? string.Empty);
        }

        internal static string NormalizeTag(string? tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return string.Empty;
            }

            var separatorIndex = tag.IndexOf('\t');
            var normalized = (separatorIndex >= 0) ? tag[..separatorIndex] : tag;

            return normalized.Trim();
        }

        internal static TorrentSnapshot CreateSnapshot(Torrent torrent)
        {
            return new TorrentSnapshot(
                string.IsNullOrEmpty(torrent.Category) ? null : torrent.Category,
                torrent.Tags.ToList(),
                torrent.Tracker ?? string.Empty,
                torrent.State ?? string.Empty,
                torrent.UploadSpeed,
                torrent.TrackersCount,
                torrent.HasTrackerError,
                torrent.HasTrackerWarning,
                torrent.HasOtherAnnounceError);
        }

        internal readonly struct TorrentSnapshot
        {
            public TorrentSnapshot(
                string? category,
                List<string> tags,
                string tracker,
                string state,
                long uploadSpeed,
                int trackersCount,
                bool hasTrackerError,
                bool hasTrackerWarning,
                bool hasOtherAnnounceError)
            {
                Category = category;
                Tags = tags;
                Tracker = tracker;
                State = state;
                UploadSpeed = uploadSpeed;
                TrackersCount = trackersCount;
                HasTrackerError = hasTrackerError;
                HasTrackerWarning = hasTrackerWarning;
                HasOtherAnnounceError = hasOtherAnnounceError;
            }

            public string? Category { get; }

            public IReadOnlyList<string> Tags { get; }

            public string Tracker { get; }

            public string State { get; }

            public long UploadSpeed { get; }

            public int TrackersCount { get; }

            public bool HasTrackerError { get; }

            public bool HasTrackerWarning { get; }

            public bool HasOtherAnnounceError { get; }
        }

        internal static void UpdateTagStateForAddition(MainData torrentList, Torrent torrent, string hash)
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

        internal static void UpdateTagStateForUpdate(MainData torrentList, string hash, IReadOnlyList<string> previousTags, IList<string> newTags)
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

        internal static void UpdateTagStateForRemoval(MainData torrentList, string hash, IReadOnlyList<string> previousTags)
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

        internal static void UpdateCategoryState(MainData torrentList, Torrent updatedTorrent, string hash, string? previousCategory)
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

        internal static void UpdateCategoryStateForRemoval(MainData torrentList, string hash, string? previousCategory)
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

        internal static void UpdateStatusState(MainData torrentList, string hash, string previousState, long previousUploadSpeed, string newState, long newUploadSpeed)
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

        internal static void UpdateTrackerState(MainData torrentList, Torrent updatedTorrent, string hash, TorrentSnapshot? previousSnapshot)
        {
            var previousTracker = previousSnapshot?.Tracker ?? string.Empty;
            var currentTracker = updatedTorrent.Tracker ?? string.Empty;

            if (!string.IsNullOrEmpty(previousTracker) && !string.Equals(previousTracker, currentTracker, StringComparison.Ordinal))
            {
                if (torrentList.TrackersState.TryGetValue(previousTracker, out var oldSet))
                {
                    oldSet.Remove(hash);
                }
            }

            if (!string.IsNullOrEmpty(currentTracker))
            {
                GetOrCreateTrackerSet(torrentList, currentTracker).Add(hash);
            }

            UpdateTrackerBucketState(torrentList, FilterHelper.TRACKER_TRACKERLESS, hash, updatedTorrent.TrackersCount == 0);
            UpdateTrackerBucketState(torrentList, FilterHelper.TRACKER_ERROR, hash, updatedTorrent.HasTrackerError);
            UpdateTrackerBucketState(torrentList, FilterHelper.TRACKER_WARNING, hash, updatedTorrent.HasTrackerWarning);
            UpdateTrackerBucketState(torrentList, FilterHelper.TRACKER_ANNOUNCE_ERROR, hash, updatedTorrent.HasOtherAnnounceError);
        }

        internal static void UpdateTrackerStateForRemoval(MainData torrentList, string hash, TorrentSnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(snapshot.Tracker) && torrentList.TrackersState.TryGetValue(snapshot.Tracker, out var trackerSet))
            {
                trackerSet.Remove(hash);
            }

            if (snapshot.TrackersCount == 0 && torrentList.TrackersState.TryGetValue(FilterHelper.TRACKER_TRACKERLESS, out var trackerlessSet))
            {
                trackerlessSet.Remove(hash);
            }

            if (snapshot.HasTrackerError && torrentList.TrackersState.TryGetValue(FilterHelper.TRACKER_ERROR, out var errorSet))
            {
                errorSet.Remove(hash);
            }

            if (snapshot.HasTrackerWarning && torrentList.TrackersState.TryGetValue(FilterHelper.TRACKER_WARNING, out var warningSet))
            {
                warningSet.Remove(hash);
            }

            if (snapshot.HasOtherAnnounceError && torrentList.TrackersState.TryGetValue(FilterHelper.TRACKER_ANNOUNCE_ERROR, out var announceErrorSet))
            {
                announceErrorSet.Remove(hash);
            }
        }

        internal static IEnumerable<string> EnumerateCategoryKeys(string category, bool useSubcategories)
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

        internal static HashSet<string> GetOrCreateTagSet(MainData torrentList, string tag)
        {
            if (!torrentList.TagState.TryGetValue(tag, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                torrentList.TagState[tag] = set;
            }

            return set;
        }

        internal static HashSet<string> GetOrCreateCategorySet(MainData torrentList, string category)
        {
            if (!torrentList.CategoriesState.TryGetValue(category, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                torrentList.CategoriesState[category] = set;
            }

            return set;
        }

        private static void UpdateTrackerBucketState(MainData torrentList, string key, string hash, bool include)
        {
            if (include)
            {
                GetOrCreateTrackerSet(torrentList, key).Add(hash);
                return;
            }

            if (torrentList.TrackersState.TryGetValue(key, out var set))
            {
                set.Remove(hash);
            }
        }

        internal static HashSet<string> GetOrCreateTrackerSet(MainData torrentList, string tracker)
        {
            if (!torrentList.TrackersState.TryGetValue(tracker, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                torrentList.TrackersState[tracker] = set;
            }

            return set;
        }

        internal static bool UpdateCategory(Category existingCategory, QBitTorrentClient.Models.Category category)
        {
            if (category.SavePath is not null && existingCategory.SavePath != category.SavePath)
            {
                existingCategory.SavePath = category.SavePath;
                return true;
            }

            return false;
        }

        internal readonly struct TorrentUpdateResult
        {
            public TorrentUpdateResult(bool dataChanged, bool filterChanged)
            {
                DataChanged = dataChanged;
                FilterChanged = filterChanged;
            }

            public bool DataChanged { get; }

            public bool FilterChanged { get; }
        }

        internal static TorrentUpdateResult UpdateTorrent(Torrent existingTorrent, QBitTorrentClient.Models.Torrent torrent)
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

            if (torrent.TrackersCount.HasValue && existingTorrent.TrackersCount != torrent.TrackersCount.Value)
            {
                existingTorrent.TrackersCount = torrent.TrackersCount.Value;
                dataChanged = true;
                filterChanged = true;
            }

            if (torrent.HasTrackerError.HasValue && existingTorrent.HasTrackerError != torrent.HasTrackerError.Value)
            {
                existingTorrent.HasTrackerError = torrent.HasTrackerError.Value;
                dataChanged = true;
                filterChanged = true;
            }

            if (torrent.HasTrackerWarning.HasValue && existingTorrent.HasTrackerWarning != torrent.HasTrackerWarning.Value)
            {
                existingTorrent.HasTrackerWarning = torrent.HasTrackerWarning.Value;
                dataChanged = true;
                filterChanged = true;
            }

            if (torrent.HasOtherAnnounceError.HasValue && existingTorrent.HasOtherAnnounceError != torrent.HasOtherAnnounceError.Value)
            {
                existingTorrent.HasOtherAnnounceError = torrent.HasOtherAnnounceError.Value;
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

            if (torrent.Popularity.HasValue && existingTorrent.Popularity != torrent.Popularity.Value)
            {
                existingTorrent.Popularity = torrent.Popularity.Value;
                dataChanged = true;
            }

            if (torrent.DownloadPath is not null && !string.Equals(existingTorrent.DownloadPath, torrent.DownloadPath, StringComparison.Ordinal))
            {
                existingTorrent.DownloadPath = torrent.DownloadPath;
                dataChanged = true;
            }

            if (torrent.RootPath is not null && !string.Equals(existingTorrent.RootPath, torrent.RootPath, StringComparison.Ordinal))
            {
                existingTorrent.RootPath = torrent.RootPath;
                dataChanged = true;
            }

            if (torrent.IsPrivate.HasValue && existingTorrent.IsPrivate != torrent.IsPrivate.Value)
            {
                existingTorrent.IsPrivate = torrent.IsPrivate.Value;
                dataChanged = true;
            }
            if (torrent.ShareLimitAction.HasValue && existingTorrent.ShareLimitAction != torrent.ShareLimitAction.Value)
            {
                existingTorrent.ShareLimitAction = torrent.ShareLimitAction.Value;
                dataChanged = true;
            }

            if (torrent.Comment is not null && !string.Equals(existingTorrent.Comment, torrent.Comment, StringComparison.Ordinal))
            {
                existingTorrent.Comment = torrent.Comment;
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

                var isDoNotDownload = file.Priority == QBitTorrentClient.Models.Priority.DoNotDownload;
                var downloadSize = isDoNotDownload ? 0 : file.Size;

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
                        var folderItem = new ContentItem(folderPath, folderName, folderIndex--, Priority.Normal, 0, 0, 0, true, level, 0);
                        folderNode = new ContentTreeNode(folderItem, parent);
                        nodes[folderPath] = folderNode;
                        parent.Children[folderPath] = folderNode;
                    }

                    parent = folderNode;
                    parentPath = parent.Item!.Name;
                }

                var displayName = segments[^1];
                var fileLevel = (parent.Item?.Level ?? -1) + 1;
                var fileItem = new ContentItem(file.Name, displayName, file.Index, (Priority)(int)file.Priority, file.Progress, file.Size, file.Availability, false, fileLevel, downloadSize);
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

                var accumulator = new DirectoryAccumulator();

                foreach (var child in folder.Children.Values)
                {
                    var childItem = child.Item!;
                    accumulator.Add(childItem.Priority, childItem.Progress, childItem.Size, childItem.Availability, childItem.DownloadSize);
                }

                folderItem.Size = accumulator.TotalSize;
                folderItem.DownloadSize = accumulator.DownloadSize;
                folderItem.Progress = accumulator.ResolveProgress();
                folderItem.Availability = accumulator.ResolveAvailability();
                folderItem.Priority = accumulator.ResolvePriority();
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

        internal static bool UpdateContentItem(ContentItem destination, ContentItem source)
        {
            const float floatTolerance = 0.0001f;
            var changed = false;

            if (destination.Priority != source.Priority)
            {
                destination.Priority = source.Priority;
                changed = true;
            }

            if (Math.Abs(destination.Progress - source.Progress) > floatTolerance)
            {
                destination.Progress = source.Progress;
                changed = true;
            }

            if (destination.Size != source.Size)
            {
                destination.Size = source.Size;
                changed = true;
            }

            if (destination.DownloadSize != source.DownloadSize)
            {
                destination.DownloadSize = source.DownloadSize;
                changed = true;
            }

            if (Math.Abs(destination.Availability - source.Availability) > floatTolerance)
            {
                destination.Availability = source.Availability;
                changed = true;
            }

            return changed;
        }

        private struct DirectoryAccumulator
        {
            public long TotalSize { get; private set; }

            public long DownloadSize { get; private set; }

            private double _downloadedDownloadSizeSum;
            private double _availabilitySum;
            private Priority? _priority;
            private bool _mixedPriority;

            public void Add(Priority priority, float progress, long size, float availability, long downloadSize)
            {
                TotalSize += size;

                if (downloadSize > 0)
                {
                    DownloadSize += downloadSize;
                    _downloadedDownloadSizeSum += downloadSize * progress;
                }

                if (priority != Priority.DoNotDownload)
                {
                    _availabilitySum += size * availability;
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
                if (DownloadSize == 0)
                {
                    return 0f;
                }

                var value = _downloadedDownloadSizeSum / DownloadSize;
                if (value > 0.999999f)
                {
                    return 1f;
                }
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
                if (TotalSize == 0)
                {
                    return 0f;
                }

                return (float)(_availabilitySum / TotalSize);
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
            var nextFolderIndex = Math.Min(minExistingIndex, minFileIndex) - 1;

            foreach (var file in files)
            {
                var priority = (Priority)(int)file.Priority;
                var isDoNotDownload = file.Priority == QBitTorrentClient.Models.Priority.DoNotDownload;
                var downloadSize = isDoNotDownload ? 0 : file.Size;
                var pathSegments = file.Name.Split(Extensions.DirectorySeparator);
                var level = pathSegments.Length - 1;
                var displayName = pathSegments[^1];
                var filePath = file.Name;
                seenPaths.Add(filePath);

                if (contents.TryGetValue(filePath, out var existingFile))
                {
                    var updatedFile = new ContentItem(filePath, displayName, file.Index, priority, file.Progress, file.Size, file.Availability, false, level, downloadSize);
                    if (UpdateContentItem(existingFile, updatedFile))
                    {
                        hasChanges = true;
                    }
                }
                else
                {
                    var newFile = new ContentItem(filePath, displayName, file.Index, priority, file.Progress, file.Size, file.Availability, false, level, downloadSize);
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
                        var newDirectory = new ContentItem(directoryPath, segment, nextFolderIndex--, Priority.Normal, 0, 0, 0, true, i, 0);
                        contents[directoryPath] = newDirectory;
                        hasChanges = true;
                    }

                    if (!directoryAccumulators.TryGetValue(directoryPath, out var accumulator))
                    {
                        accumulator = new DirectoryAccumulator();
                    }

                    accumulator.Add(priority, file.Progress, file.Size, file.Availability, downloadSize);
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
                    directoryItem.Level,
                    accumulator.DownloadSize);

                if (UpdateContentItem(directoryItem, updatedDirectory))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }
    }
}
