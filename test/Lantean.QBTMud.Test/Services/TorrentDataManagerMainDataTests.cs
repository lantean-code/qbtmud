using AwesomeAssertions;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Client = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Services
{
    public class TorrentDataManagerMainDataTests
    {
        private readonly TorrentDataManager _target = new TorrentDataManager();

        // -------------------- CreateMainData --------------------

        [Fact]
        public void GIVEN_EmptyInput_WHEN_CreateMainData_THEN_EmptyCollections_And_DefaultStates()
        {
            var client = new Client.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: null,
                serverState: null);

            var result = _target.CreateMainData(client);

            result.Torrents.Should().BeEmpty();
            result.Tags.Should().BeEmpty();
            result.Categories.Should().BeEmpty();
            result.Trackers.Should().BeEmpty();

            // Default from app GlobalTransferInfo() is "Unknown"
            result.ServerState.ConnectionStatus.Should().Be("Unknown");
            result.ServerState.UseSubcategories.Should().BeFalse();

            result.TagState.Should().ContainKeys(FilterHelper.TAG_ALL, FilterHelper.TAG_UNTAGGED);
            result.TagState[FilterHelper.TAG_ALL].Should().BeEmpty();
            result.TagState[FilterHelper.TAG_UNTAGGED].Should().BeEmpty();

            result.CategoriesState.Should().ContainKeys(FilterHelper.CATEGORY_ALL, FilterHelper.CATEGORY_UNCATEGORIZED);
            result.CategoriesState[FilterHelper.CATEGORY_ALL].Should().BeEmpty();
            result.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Should().BeEmpty();

            foreach (var s in Enum.GetValues<Status>())
            {
                result.StatusState.Should().ContainKey(s.ToString());
                result.StatusState[s.ToString()].Should().BeEmpty();
            }

            result.TrackersState.Should().ContainKeys(FilterHelper.TRACKER_ALL, FilterHelper.TRACKER_TRACKERLESS);
            result.TrackersState[FilterHelper.TRACKER_ALL].Should().BeEmpty();
            result.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_PopulatedInput_WHEN_CreateMainData_THEN_Maps_All_And_Builds_FilterStates()
        {
            var hash = "abc123";
            var clientTorrent = new Client.Torrent
            {
                Name = "Movie A",
                State = "downloading",
                UploadSpeed = 0,
                Category = "Movies/HD",
                Tags = new[] { " tagA\tignored", "", "tagB" },
                Tracker = "udp://tracker1",
                AddedOn = 111,
                AmountLeft = 1,
                AutomaticTorrentManagement = true,
                Availability = 1.0f,
                Completed = 0,
                CompletionOn = 0,
                ContentPath = "/content",
                DownloadLimit = 0,
                DownloadSpeed = 1000,
                Downloaded = 200,
                DownloadedSession = 50,
                EstimatedTimeOfArrival = 100,
                FirstLastPiecePriority = false,
                ForceStart = false,
                InfoHashV1 = "v1",
                InfoHashV2 = "v2",
                LastActivity = 1,
                MagnetUri = "magnet:?xt",
                MaxRatio = 9.9f,
                MaxSeedingTime = 0,
                NumberComplete = 1,
                NumberIncomplete = 1,
                NumberLeeches = 0,
                NumberSeeds = 5,
                Priority = 1,
                Progress = 0.2f,
                Ratio = 0.1f,
                RatioLimit = 0,
                SavePath = "/save",
                SeedingTime = 0,
                SeedingTimeLimit = 0,
                SeenComplete = 0,
                SequentialDownload = false,
                Size = 1000,
                SuperSeeding = false,
                TimeActive = 10,
                TotalSize = 1000,
                UploadLimit = 0,
                Uploaded = 0,
                UploadedSession = 0,
                Reannounce = 0,
                InactiveSeedingTimeLimit = 0,
                MaxInactiveSeedingTime = 0,
                Popularity = 0,
                DownloadPath = "/dl",
                RootPath = "/root",
                IsPrivate = false,
                ShareLimitAction = Client.ShareLimitAction.Default,
                Comment = "c"
            };

            var torrents = new Dictionary<string, Client.Torrent> { [hash] = clientTorrent };

            var categories = new Dictionary<string, Client.Category>
            {
                ["Movies"] = new Client.Category("Movies", "/movies", downloadPath: null),
                ["Movies/HD"] = new Client.Category("Movies/HD", "/movies/hd", downloadPath: null)
            };

            var trackers = new Dictionary<string, IReadOnlyList<string>>
            {
                ["udp://tracker1"] = new List<string> { hash }
            };

            var clientServer = new Client.ServerState(
                allTimeDownloaded: 10,
                allTimeUploaded: 20,
                averageTimeQueue: 30,
                connectionStatus: "connected",
                dHTNodes: 2,
                downloadInfoData: 3,
                downloadInfoSpeed: 4,
                downloadRateLimit: 5,
                freeSpaceOnDisk: 6,
                globalRatio: 7.5f,
                queuedIOJobs: 8,
                queuing: true,
                readCacheHits: 9.1f,
                readCacheOverload: 10.2f,
                refreshInterval: 11,
                totalBuffersSize: 12,
                totalPeerConnections: 13,
                totalQueuedSize: 14,
                totalWastedSession: 15,
                uploadInfoData: 16,
                uploadInfoSpeed: 17,
                uploadRateLimit: 18,
                useAltSpeedLimits: false,
                useSubcategories: true,
                writeCacheOverload: 19.3f,
                lastExternalAddressV4: "1.2.3.4",
                lastExternalAddressV6: "2001::1");

            var client = new Client.MainData(
                responseId: 2,
                fullUpdate: true,
                torrents: torrents,
                torrentsRemoved: null,
                categories: categories,
                categoriesRemoved: null,
                tags: new[] { " tagA", "tagA", "tagB", "" },
                tagsRemoved: null,
                trackers: trackers,
                trackersRemoved: null,
                serverState: clientServer);

            var result = _target.CreateMainData(client);

            result.Torrents.Should().ContainKey(hash);
            var mapped = result.Torrents[hash];
            mapped.Name.Should().Be("Movie A");
            mapped.Category.Should().Be("Movies/HD");
            mapped.Tags.Should().BeEquivalentTo(new[] { "tagA", "tagB" }, o => o.WithoutStrictOrdering());
            mapped.Tracker.Should().Be("udp://tracker1");

            result.TagState[FilterHelper.TAG_ALL].Should().Contain(hash);
            result.TagState[FilterHelper.TAG_UNTAGGED].Should().NotContain(hash);
            result.TagState["tagA"].Should().Contain(hash);
            result.TagState["tagB"].Should().Contain(hash);

            result.CategoriesState[FilterHelper.CATEGORY_ALL].Should().Contain(hash);
            result.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Should().NotContain(hash);
            result.CategoriesState["Movies/HD"].Should().Contain(hash);
            result.CategoriesState["Movies"].Should().Contain(hash);

            result.TrackersState[FilterHelper.TRACKER_ALL].Should().Contain(hash);
            result.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Should().NotContain(hash);
            result.TrackersState["udp://tracker1"].Should().Contain(hash);

            result.StatusState[nameof(Status.Downloading)].Should().Contain(hash);
            result.StatusState[nameof(Status.Active)].Should().Contain(hash);
            result.StatusState[nameof(Status.Inactive)].Should().NotContain(hash);
            result.StatusState[nameof(Status.Stalled)].Should().NotContain(hash);

            result.ServerState.ConnectionStatus.Should().Be("connected");
            result.ServerState.UseSubcategories.Should().BeTrue();
            result.ServerState.LastExternalAddressV4.Should().Be("1.2.3.4");
            result.ServerState.LastExternalAddressV6.Should().Be("2001::1");
            result.ServerState.GlobalRatio.Should().Be(7.5f);
        }

        // -------------------- MergeMainData: removals --------------------

        [Fact]
        public void GIVEN_ExistingData_WHEN_Merge_Removals_THEN_ItemsAndStateAreRemoved_And_Flagged()
        {
            var hash = "h1";
            var clientTorrent = new Client.Torrent
            {
                Name = "T1",
                State = "downloading",
                UploadSpeed = 0,
                Category = "Cat/Sub",
                Tags = new[] { "x", "y" },
                Tracker = "udp://t1"
            };
            var client = new Client.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, Client.Torrent> { [hash] = clientTorrent },
                torrentsRemoved: null,
                categories: new Dictionary<string, Client.Category>
                {
                    ["Cat/Sub"] = new Client.Category("Cat/Sub", "/cat/sub", null)
                },
                categoriesRemoved: null,
                tags: new[] { "x", "y" },
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://t1"] = new List<string> { hash } },
                trackersRemoved: null,
                serverState: new Client.ServerState(
                    allTimeDownloaded: 0,
                    allTimeUploaded: 0,
                    averageTimeQueue: 0,
                    connectionStatus: "connected",
                    dHTNodes: 0,
                    downloadInfoData: 0,
                    downloadInfoSpeed: 0,
                    downloadRateLimit: 0,
                    freeSpaceOnDisk: 0,
                    globalRatio: 0f,
                    queuedIOJobs: 0,
                    queuing: false,
                    readCacheHits: 0f,
                    readCacheOverload: 0f,
                    refreshInterval: 0,
                    totalBuffersSize: 0,
                    totalPeerConnections: 0,
                    totalQueuedSize: 0,
                    totalWastedSession: 0,
                    uploadInfoData: 0,
                    uploadInfoSpeed: 0,
                    uploadRateLimit: 0,
                    useAltSpeedLimits: false,
                    useSubcategories: true,
                    writeCacheOverload: 0f,
                    lastExternalAddressV4: "4",
                    lastExternalAddressV6: "6"));
            var existing = _target.CreateMainData(client);

            var delta = new Client.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: new[] { hash },
                categories: null,
                categoriesRemoved: new[] { "Cat/Sub" },
                tags: null,
                tagsRemoved: new[] { "x" },
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: new[] { "udp://t1" },
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();

            existing.Torrents.Should().NotContainKey(hash);
            existing.Categories.Should().NotContainKey("Cat/Sub");
            existing.Trackers.Should().NotContainKey("udp://t1");
            existing.TagState.Should().NotContainKey("x");

            existing.TagState[FilterHelper.TAG_ALL].Should().NotContain(hash);
            existing.CategoriesState[FilterHelper.CATEGORY_ALL].Should().NotContain(hash);
            foreach (var kv in existing.StatusState)
            {
                kv.Value.Should().NotContain(hash);
            }

            existing.TrackersState[FilterHelper.TRACKER_ALL].Should().NotContain(hash);
        }

        // -------------------- MergeMainData: additions (from empty) --------------------

        [Fact]
        public void GIVEN_EmptyExisting_WHEN_Merge_Additions_THEN_TorrentAndStatesAdded_And_Flagged()
        {
            var existing = _target.CreateMainData(
                new Client.MainData(0, true, null, null, null, null, null, null,
                    new Dictionary<string, IReadOnlyList<string>>(), null, null));

            var hash = "z1";
            var addTorrent = new Client.Torrent
            {
                Name = "Zed",
                State = "downloading",
                UploadSpeed = 0,
                Category = "",
                Tags = Array.Empty<string>(),
                Tracker = ""
            };

            var delta = new Client.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: new Dictionary<string, Client.Torrent> { [hash] = addTorrent },
                torrentsRemoved: null,
                categories: new Dictionary<string, Client.Category>(),
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();

            existing.Torrents.Should().ContainKey(hash);

            existing.TagState[FilterHelper.TAG_ALL].Should().Contain(hash);
            existing.TagState[FilterHelper.TAG_UNTAGGED].Should().Contain(hash);

            existing.CategoriesState[FilterHelper.CATEGORY_ALL].Should().Contain(hash);
            existing.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Should().Contain(hash);

            existing.TrackersState[FilterHelper.TRACKER_ALL].Should().Contain(hash);
            existing.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Should().Contain(hash);

            existing.StatusState[nameof(Status.Downloading)].Should().Contain(hash);
            existing.StatusState[nameof(Status.Active)].Should().Contain(hash);
        }

        // -------------------- MergeMainData: updating an existing torrent (filter-affecting changes) --------------------

        [Fact]
        public void GIVEN_ExistingTorrent_WHEN_UpdateCategoryTagsStateTrackerAndSpeed_THEN_FilterSetsAdjusted()
        {
            var hash = "h2";
            var start = new Client.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, Client.Torrent>
                {
                    [hash] = new Client.Torrent
                    {
                        Name = "A",
                        State = "stalledDL",
                        UploadSpeed = 0,
                        Category = "",
                        Tags = Array.Empty<string>(),
                        Tracker = ""
                    }
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, Client.Category>(),
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: null,
                serverState: new Client.ServerState(
                    allTimeDownloaded: 0,
                    allTimeUploaded: 0,
                    averageTimeQueue: 0,
                    connectionStatus: "connected",
                    dHTNodes: 0,
                    downloadInfoData: 0,
                    downloadInfoSpeed: 0,
                    downloadRateLimit: 0,
                    freeSpaceOnDisk: 0,
                    globalRatio: 0f,
                    queuedIOJobs: 0,
                    queuing: false,
                    readCacheHits: 0f,
                    readCacheOverload: 0f,
                    refreshInterval: 0,
                    totalBuffersSize: 0,
                    totalPeerConnections: 0,
                    totalQueuedSize: 0,
                    totalWastedSession: 0,
                    uploadInfoData: 0,
                    uploadInfoSpeed: 0,
                    uploadRateLimit: 0,
                    useAltSpeedLimits: false,
                    useSubcategories: true,
                    writeCacheOverload: 0f));
            var list = _target.CreateMainData(start);

            list.ServerState.UseSubcategories = true;

            var update = new Client.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: new Dictionary<string, Client.Torrent>
                {
                    [hash] = new Client.Torrent
                    {
                        Name = "A",
                        State = "stalledDL",
                        UploadSpeed = 10,
                        Category = "Cat/Sub",
                        Tags = new[] { " x\tid " },
                        Tracker = "udp://zzz"
                    }
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, Client.Category>(),
                categoriesRemoved: null,
                tags: new[] { " x\tgarbage " },
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://zzz"] = new List<string> { hash } },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(update, list, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();

            list.TagState[FilterHelper.TAG_UNTAGGED].Should().NotContain(hash);
            list.TagState["x"].Should().Contain(hash);

            list.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Should().NotContain(hash);
            list.CategoriesState["Cat/Sub"].Should().Contain(hash);
            list.CategoriesState["Cat"].Should().Contain(hash);

            list.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Should().NotContain(hash);
            list.TrackersState["udp://zzz"].Should().Contain(hash);

            list.StatusState[nameof(Status.Inactive)].Should().NotContain(hash);
            list.StatusState[nameof(Status.Active)].Should().Contain(hash);
        }

        // -------------------- MergeMainData: trackers & categories dictionary update paths --------------------

        [Fact]
        public void GIVEN_ExistingTrackersAndCategories_WHEN_SequenceChangesOrSavePathChanges_THEN_DataChangedTrue()
        {
            var h = "a";
            var start = new Client.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, Client.Torrent> { [h] = new Client.Torrent { Name = "N", State = "downloading", UploadSpeed = 0, Category = "C", Tags = Array.Empty<string>(), Tracker = "t1" } },
                torrentsRemoved: null,
                categories: new Dictionary<string, Client.Category> { ["C"] = new Client.Category("C", "/a", null) },
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["t1"] = new List<string> { h } },
                trackersRemoved: null,
                serverState: null);
            var list = _target.CreateMainData(start);

            var delta = new Client.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: new Dictionary<string, Client.Category> { ["C"] = new Client.Category("C", "/b", null) },
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["t1"] = new List<string> { h, "other" } },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, list, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeFalse();

            list.Trackers["t1"].Should().Equal(new[] { h, "other" });
            list.Categories["C"].SavePath.Should().Be("/b");
        }

        // -------------------- MergeMainData: ServerState update --------------------

        [Fact]
        public void GIVEN_Existing_WHEN_ServerStateFieldsChange_THEN_DataChangedTrue_And_ValuesUpdated()
        {
            var existing = _target.CreateMainData(
                new Client.MainData(0, true, null, null, null, null, null, null,
                    new Dictionary<string, IReadOnlyList<string>>(), null,
                    new Client.ServerState(
                        allTimeDownloaded: 1, allTimeUploaded: 2, averageTimeQueue: 3,
                        connectionStatus: "connected", dHTNodes: 4, downloadInfoData: 5,
                        downloadInfoSpeed: 6, downloadRateLimit: 7, freeSpaceOnDisk: 8, globalRatio: 9.0f,
                        queuedIOJobs: 10, queuing: true, readCacheHits: 11.0f, readCacheOverload: 12.0f,
                        refreshInterval: 13, totalBuffersSize: 14, totalPeerConnections: 15, totalQueuedSize: 16,
                        totalWastedSession: 17, uploadInfoData: 18, uploadInfoSpeed: 19, uploadRateLimit: 20,
                        useAltSpeedLimits: false, useSubcategories: false, writeCacheOverload: 21.0f,
                        lastExternalAddressV4: "4", lastExternalAddressV6: "6")));

            var delta = new Client.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: null, torrentsRemoved: null,
                categories: null, categoriesRemoved: null,
                tags: null, tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(), trackersRemoved: null,
                serverState: new Client.ServerState(
                    allTimeDownloaded: 100, allTimeUploaded: 200, averageTimeQueue: 300,
                    connectionStatus: "stopped", dHTNodes: 40, downloadInfoData: 50,
                    downloadInfoSpeed: 60, downloadRateLimit: 70, freeSpaceOnDisk: 80, globalRatio: 1.5f,
                    queuedIOJobs: 1000, queuing: false, readCacheHits: 0.5f, readCacheOverload: 0.2f,
                    refreshInterval: 99, totalBuffersSize: 77, totalPeerConnections: 88, totalQueuedSize: 66,
                    totalWastedSession: 55, uploadInfoData: 44, uploadInfoSpeed: 33, uploadRateLimit: 22,
                    useAltSpeedLimits: true, useSubcategories: true, writeCacheOverload: 0.1f,
                    lastExternalAddressV4: "8.8.8.8", lastExternalAddressV6: "fe80::1"));

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeFalse();

            var s = existing.ServerState;
            s.ConnectionStatus.Should().Be("stopped");
            s.DHTNodes.Should().Be(40);
            s.DownloadInfoSpeed.Should().Be(60);
            s.UploadRateLimit.Should().Be(22);
            s.UseSubcategories.Should().BeTrue();
            s.LastExternalAddressV4.Should().Be("8.8.8.8");
            s.LastExternalAddressV6.Should().Be("fe80::1");
        }
    }
}
