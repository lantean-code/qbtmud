using AwesomeAssertions;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Services
{
    public class TorrentDataManagerMainDataTests
    {
        private readonly TorrentDataManager _target;

        public TorrentDataManagerMainDataTests()
        {
            _target = new TorrentDataManager();
        }

        // -------------------- CreateMainData --------------------

        [Fact]
        public void GIVEN_EmptyInput_WHEN_CreateMainData_THEN_EmptyCollections_And_DefaultStates()
        {
            var client = new ClientModels.MainData(
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
        public void GIVEN_NullTrackers_WHEN_CreateMainData_THEN_TrackersAreInitializedToEmpty()
        {
            var hash = "hash-null-trackers";
            var client = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent
                    {
                        Name = "Name",
                        State = "downloading",
                        UploadSpeed = 0,
                        Category = string.Empty,
                        Tags = Array.Empty<string>(),
                        Tracker = string.Empty,
                        TrackersCount = 0
                    }
                },
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var result = _target.CreateMainData(client);

            result.Trackers.Should().BeEmpty();
            result.TrackersState[FilterHelper.TRACKER_ALL].Should().Contain(hash);
            result.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Should().Contain(hash);
        }

        [Fact]
        public void GIVEN_ServerStateWithNullStrings_WHEN_CreateMainData_THEN_NullsAreMappedToEmptyStrings()
        {
            var client = new ClientModels.MainData(
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
                serverState: new ClientModels.ServerState(
                    allTimeDownloaded: 1,
                    allTimeUploaded: 2,
                    averageTimeQueue: 3,
                    connectionStatus: null,
                    dHTNodes: 4,
                    downloadInfoData: 5,
                    downloadInfoSpeed: 6,
                    downloadRateLimit: 7,
                    freeSpaceOnDisk: 8,
                    globalRatio: 9.5f,
                    queuedIOJobs: 10,
                    queuing: false,
                    readCacheHits: 11,
                    readCacheOverload: 12,
                    refreshInterval: 13,
                    totalBuffersSize: 14,
                    totalPeerConnections: 15,
                    totalQueuedSize: 16,
                    totalWastedSession: 17,
                    uploadInfoData: 18,
                    uploadInfoSpeed: 19,
                    uploadRateLimit: 20,
                    useAltSpeedLimits: false,
                    useSubcategories: false,
                    writeCacheOverload: 21,
                    lastExternalAddressV4: null,
                    lastExternalAddressV6: null));

            var result = _target.CreateMainData(client);

            result.ServerState.ConnectionStatus.Should().Be(string.Empty);
            result.ServerState.LastExternalAddressV4.Should().Be(string.Empty);
            result.ServerState.LastExternalAddressV6.Should().Be(string.Empty);
        }

        [Fact]
        public void GIVEN_PopulatedInput_WHEN_CreateMainData_THEN_Maps_All_And_Builds_FilterStates()
        {
            var hash = "abc123";
            var clientTorrent = new ClientModels.Torrent
            {
                Name = "Movie A",
                State = "downloading",
                UploadSpeed = 0,
                Category = "Movies/HD",
                Tags = new[] { " tagA\tignored", "", "tagB" },
                Tracker = "udp://tracker1",
                TrackersCount = 1,
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
                ShareLimitAction = ClientModels.ShareLimitAction.Default,
                Comment = "c"
            };

            var torrents = new Dictionary<string, ClientModels.Torrent> { [hash] = clientTorrent };

            var categories = new Dictionary<string, ClientModels.Category>
            {
                ["Movies"] = new ClientModels.Category("Movies", "/movies", downloadPath: null),
                ["Movies/HD"] = new ClientModels.Category("Movies/HD", "/movies/hd", downloadPath: null)
            };

            var trackers = new Dictionary<string, IReadOnlyList<string>>
            {
                ["udp://tracker1"] = new List<string> { hash }
            };

            var clientServer = new ClientModels.ServerState(
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

            var client = new ClientModels.MainData(
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

        [Fact]
        public void GIVEN_TorrentWithNullStrings_WHEN_CreateMainData_THEN_UsesEmptyStrings()
        {
            var hash = "nulls";
            var clientTorrent = new ClientModels.Torrent
            {
                Hash = hash,
                Name = null,
                Category = null,
                ContentPath = null,
                SavePath = null,
                DownloadPath = null,
                RootPath = null,
                MagnetUri = null,
                InfoHashV1 = null,
                InfoHashV2 = null,
                State = null,
                Tracker = null,
                Tags = null,
                TrackersCount = 0
            };

            var client = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent> { [hash] = clientTorrent },
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: null,
                serverState: null);

            var result = _target.CreateMainData(client);

            var torrent = result.Torrents[hash];
            torrent.Name.Should().Be(string.Empty);
            torrent.Category.Should().Be(string.Empty);
            torrent.ContentPath.Should().Be(string.Empty);
            torrent.SavePath.Should().Be(string.Empty);
            torrent.DownloadPath.Should().Be(string.Empty);
            torrent.RootPath.Should().Be(string.Empty);
            torrent.MagnetUri.Should().Be(string.Empty);
            torrent.InfoHashV1.Should().Be(string.Empty);
            torrent.InfoHashV2.Should().Be(string.Empty);
            torrent.State.Should().Be(string.Empty);
            torrent.Tracker.Should().Be(string.Empty);
            torrent.Tags.Should().BeEmpty();
        }

        // -------------------- MergeMainData: removals --------------------

        [Fact]
        public void GIVEN_ExistingData_WHEN_Merge_Removals_THEN_ItemsAndStateAreRemoved_And_Flagged()
        {
            var hash = "h1";
            var clientTorrent = new ClientModels.Torrent
            {
                Name = "T1",
                State = "downloading",
                UploadSpeed = 0,
                Category = "Cat/Sub",
                Tags = new[] { "x", "y" },
                Tracker = "udp://t1",
                TrackersCount = 1
            };
            var client = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent> { [hash] = clientTorrent },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>
                {
                    ["Cat/Sub"] = new ClientModels.Category("Cat/Sub", "/cat/sub", null)
                },
                categoriesRemoved: null,
                tags: new[] { "x", "y" },
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://t1"] = new List<string> { hash } },
                trackersRemoved: null,
                serverState: new ClientModels.ServerState(
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

            var delta = new ClientModels.MainData(
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

        [Fact]
        public void GIVEN_TrackerMembershipChanges_WHEN_Merge_THEN_TrackersStateIsRebuilt_And_Flagged()
        {
            var hash = "h1";
            var client = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent { Name = "T1", State = "downloading", UploadSpeed = 0, Tags = Array.Empty<string>(), Tracker = "udp://t1", TrackersCount = 1, Category = string.Empty }
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>(),
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://t1"] = new[] { hash } },
                trackersRemoved: null,
                serverState: null);
            var existing = _target.CreateMainData(client);

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://t1"] = Array.Empty<string>() },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();

            existing.Trackers["udp://t1"].Should().BeEmpty();
            existing.TrackersState["udp://t1"].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_TrackerIncludesUnknownHash_WHEN_Merge_THEN_StateExcludesUnknownHash()
        {
            var knownHash = "h1";
            var client = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [knownHash] = new ClientModels.Torrent { Name = "T1", State = "downloading", UploadSpeed = 0, Tags = Array.Empty<string>(), Tracker = "udp://t1", TrackersCount = 1, Category = string.Empty }
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>(),
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://t1"] = new[] { knownHash } },
                trackersRemoved: null,
                serverState: null);
            var existing = _target.CreateMainData(client);

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://t1"] = new[] { knownHash, "ghost" } },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            existing.TrackersState["udp://t1"].Should().ContainSingle().Which.Should().Be(knownHash);
        }

        [Fact]
        public void GIVEN_TagsRemovedContainsEmpty_WHEN_Merge_THEN_NoChanges()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(0, true, null, null, null, null, null, null, new Dictionary<string, IReadOnlyList<string>>(), null, null));

            var delta = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: new[] { "   " },
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeFalse();
            filterChanged.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_TagRemovedNotPresentOnTorrent_WHEN_Merge_THEN_NoTagStateChange()
        {
            var hash = "h1";
            var clientTorrent = new ClientModels.Torrent
            {
                Name = "T1",
                State = "downloading",
                UploadSpeed = 0,
                Tags = new[] { "keep" },
                Tracker = string.Empty,
                TrackersCount = 0,
                Category = string.Empty
            };

            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent> { [hash] = clientTorrent },
                    torrentsRemoved: null,
                    categories: new Dictionary<string, ClientModels.Category>(),
                    categoriesRemoved: null,
                    tags: new[] { "keep" },
                    tagsRemoved: null,
                    trackers: new Dictionary<string, IReadOnlyList<string>>(),
                    trackersRemoved: null,
                    serverState: null));

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: new[] { "missing" },
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeFalse();
            filterChanged.Should().BeFalse();
            existing.Torrents[hash].Tags.Should().ContainSingle().Which.Should().Be("keep");
            existing.TagState.Should().ContainKey("keep");
        }

        [Fact]
        public void GIVEN_NewCategory_WHEN_Merge_THEN_CategoryAddedAndFlagged()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(0, true, null, null, null, null, null, null, new Dictionary<string, IReadOnlyList<string>>(), null, null));

            var delta = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category> { ["New"] = new ClientModels.Category("New", "/new", null) },
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            existing.Categories.Should().ContainKey("New");
        }

        [Fact]
        public void GIVEN_NewTrackerUrl_WHEN_Merge_THEN_TrackerStateCreated()
        {
            var hash = "h1";
            var clientTorrent = new ClientModels.Torrent
            {
                Name = "T1",
                State = "downloading",
                UploadSpeed = 0,
                Tags = Array.Empty<string>(),
                Tracker = string.Empty,
                TrackersCount = 0,
                Category = string.Empty
            };

            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent> { [hash] = clientTorrent },
                    torrentsRemoved: null,
                    categories: new Dictionary<string, ClientModels.Category>(),
                    categoriesRemoved: null,
                    tags: Array.Empty<string>(),
                    tagsRemoved: null,
                    trackers: new Dictionary<string, IReadOnlyList<string>>(),
                    trackersRemoved: null,
                    serverState: null));

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://new"] = new[] { hash } },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            existing.Trackers.Should().ContainKey("udp://new");
        }

        [Fact]
        public void GIVEN_MissingStatusEntries_WHEN_TorrentRemoved_THEN_RemovalContinuesGracefully()
        {
            var hash = "h1";
            var torrents = new Dictionary<string, Torrent> { [hash] = BuildTorrent(hash) };
            var tags = new HashSet<string>();
            var categories = new Dictionary<string, Category>();
            var trackers = new Dictionary<string, IReadOnlyList<string>>();
            var tagState = new Dictionary<string, HashSet<string>> { [FilterHelper.TAG_ALL] = new HashSet<string> { hash }, [FilterHelper.TAG_UNTAGGED] = new HashSet<string>() };
            var categoriesState = new Dictionary<string, HashSet<string>> { [FilterHelper.CATEGORY_ALL] = new HashSet<string>(), [FilterHelper.CATEGORY_UNCATEGORIZED] = new HashSet<string>() };
            var statusState = new Dictionary<string, HashSet<string>> { { nameof(Status.Downloading), new HashSet<string> { hash } } };
            var trackersState = new Dictionary<string, HashSet<string>> { { FilterHelper.TRACKER_ALL, new HashSet<string> { hash } } };
            var main = new MainData(torrents, tags, categories, trackers, new ServerState(), tagState, categoriesState, statusState, trackersState);

            var delta = new ClientModels.MainData(1, false, null, new[] { hash }, null, null, null, null, null, null, null);

            var changed = _target.MergeMainData(delta, main, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            main.Torrents.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_EmptyTags_WHEN_UpdateTagStateForAddition_THEN_UntaggedUpdated()
        {
            var tagState = new Dictionary<string, HashSet<string>> { { FilterHelper.TAG_UNTAGGED, new HashSet<string>() } };
            var categoriesState = new Dictionary<string, HashSet<string>>();
            var statusState = new Dictionary<string, HashSet<string>>();
            var trackersState = new Dictionary<string, HashSet<string>>();
            var main = new MainData(new Dictionary<string, Torrent>(), Array.Empty<string>(), new Dictionary<string, Category>(), new Dictionary<string, IReadOnlyList<string>>(), new ServerState(), tagState, categoriesState, statusState, trackersState);
            TorrentDataManager.UpdateTagStateForAddition(main, BuildTorrent("h1"), "h1");

            main.TagState[FilterHelper.TAG_UNTAGGED].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_NewTags_WHEN_UpdateTagStateForUpdate_THEN_UnTaggedCleared_And_TagsAdded()
        {
            var tagState = new Dictionary<string, HashSet<string>> { { FilterHelper.TAG_UNTAGGED, new HashSet<string> { "h1" } } };
            var main = new MainData(new Dictionary<string, Torrent>(), Array.Empty<string>(), new Dictionary<string, Category>(), new Dictionary<string, IReadOnlyList<string>>(), new ServerState(), tagState, new Dictionary<string, HashSet<string>>(), new Dictionary<string, HashSet<string>>(), new Dictionary<string, HashSet<string>>());
            var newTags = new List<string> { string.Empty, "x" };

            TorrentDataManager.UpdateTagStateForUpdate(main, "h1", new List<string>(), newTags);

            main.TagState[FilterHelper.TAG_UNTAGGED].Should().BeEmpty();
            main.TagState["x"].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_PreviousTags_WHEN_UpdateTagStateForRemoval_THEN_RemovedFromSets()
        {
            var tagState = new Dictionary<string, HashSet<string>>
            {
                { FilterHelper.TAG_UNTAGGED, new HashSet<string>() },
                { "x", new HashSet<string> { "h1" } }
            };
            var main = new MainData(new Dictionary<string, Torrent>(), Array.Empty<string>(), new Dictionary<string, Category>(), new Dictionary<string, IReadOnlyList<string>>(), new ServerState(), tagState, new Dictionary<string, HashSet<string>>(), new Dictionary<string, HashSet<string>>(), new Dictionary<string, HashSet<string>>());

            TorrentDataManager.UpdateTagStateForRemoval(main, "h1", new List<string> { "x", string.Empty });

            main.TagState["x"].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_PreviousCategoryAndSubcategoriesEnabled_WHEN_UpdateCategoryState_THEN_RemovedFromPreviousKeys()
        {
            var categoriesState = new Dictionary<string, HashSet<string>>
            {
                { FilterHelper.CATEGORY_UNCATEGORIZED, new HashSet<string>() },
                { "A/B", new HashSet<string> { "h1" } },
                { "A", new HashSet<string> { "h1" } }
            };
            var main = new MainData(new Dictionary<string, Torrent>(), Array.Empty<string>(), new Dictionary<string, Category>(), new Dictionary<string, IReadOnlyList<string>>(), new ServerState { UseSubcategories = true }, new Dictionary<string, HashSet<string>>(), categoriesState, new Dictionary<string, HashSet<string>>(), new Dictionary<string, HashSet<string>>());
            TorrentDataManager.UpdateCategoryState(main, BuildTorrent("h1", "New", new[] { "x" }), "h1", "A/B");

            main.CategoriesState["A/B"].Should().BeEmpty();
            main.CategoriesState["A"].Should().BeEmpty();
            main.CategoriesState["New"].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_NoPreviousCategory_WHEN_UpdateCategoryStateForRemoval_THEN_RemovedFromUncategorized()
        {
            var categoriesState = new Dictionary<string, HashSet<string>>
            {
                { FilterHelper.CATEGORY_UNCATEGORIZED, new HashSet<string> { "h1" } }
            };
            var main = new MainData(new Dictionary<string, Torrent>(), Array.Empty<string>(), new Dictionary<string, Category>(), new Dictionary<string, IReadOnlyList<string>>(), new ServerState(), new Dictionary<string, HashSet<string>>(), categoriesState, new Dictionary<string, HashSet<string>>(), new Dictionary<string, HashSet<string>>());

            TorrentDataManager.UpdateCategoryStateForRemoval(main, "h1", null);

            main.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Should().BeEmpty();
        }

        private Torrent BuildTorrent(string hash, string category = "", IEnumerable<string>? tags = null, string tracker = "", string state = "downloading")
        {
            return _target.CreateTorrent(
                hash,
                new ClientModels.Torrent
                {
                    Name = "Name",
                    Category = category,
                    Tags = tags?.ToArray(),
                    Tracker = tracker,
                    State = state,
                    UploadSpeed = 0,
                    TrackersCount = 0
                });
        }

        [Fact]
        public void GIVEN_TagRemoved_WHEN_Merge_THEN_TagDroppedFromLists_And_UntaggedUpdated()
        {
            var hash1 = "h1";
            var hash2 = "h2";
            var client = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash1] = new ClientModels.Torrent { Name = "T1", State = "downloading", UploadSpeed = 0, Tags = new[] { "x" }, Tracker = string.Empty, TrackersCount = 0, Category = string.Empty },
                    [hash2] = new ClientModels.Torrent { Name = "T2", State = "downloading", UploadSpeed = 0, Tags = new[] { "x", "y" }, Tracker = string.Empty, TrackersCount = 0, Category = string.Empty }
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>(),
                categoriesRemoved: null,
                tags: new[] { "x", "y" },
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: null,
                serverState: null);
            var existing = _target.CreateMainData(client);

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: new[] { "x" },
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();

            existing.Tags.Should().ContainSingle().Which.Should().Be("y");
            existing.TagState.Should().NotContainKey("x");

            existing.Torrents[hash1].Tags.Should().BeEmpty();
            existing.Torrents[hash2].Tags.Should().ContainSingle().Which.Should().Be("y");

            existing.TagState[FilterHelper.TAG_UNTAGGED].Should().Contain(hash1);
            existing.TagState[FilterHelper.TAG_UNTAGGED].Should().NotContain(hash2);
        }

        // -------------------- MergeMainData: additions (from empty) --------------------

        [Fact]
        public void GIVEN_EmptyExisting_WHEN_Merge_Additions_THEN_TorrentAndStatesAdded_And_Flagged()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(0, true, null, null, null, null, null, null,
                    new Dictionary<string, IReadOnlyList<string>>(), null, null));

            var hash = "z1";
            var addTorrent = new ClientModels.Torrent
            {
                Name = "Zed",
                State = "downloading",
                UploadSpeed = 0,
                Category = "",
                Tags = Array.Empty<string>(),
                Tracker = "",
                TrackersCount = 0
            };

            var delta = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: new Dictionary<string, ClientModels.Torrent> { [hash] = addTorrent },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>(),
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
            var start = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent
                    {
                        Name = "A",
                        State = "stalledDL",
                        UploadSpeed = 0,
                        Category = "",
                        Tags = Array.Empty<string>(),
                        Tracker = "",
                        TrackersCount = 0
                    }
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>(),
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: null,
                serverState: new ClientModels.ServerState(
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

            var update = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent
                    {
                        Name = "A",
                        State = "stalledDL",
                        UploadSpeed = 10,
                        Category = "Cat/Sub",
                        Tags = new[] { " x\tid " },
                        Tracker = "udp://zzz",
                        TrackersCount = 1
                    }
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>(),
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
            var start = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: true,
                torrents: new Dictionary<string, ClientModels.Torrent> { [h] = new ClientModels.Torrent { Name = "N", State = "downloading", UploadSpeed = 0, Category = "C", Tags = Array.Empty<string>(), Tracker = "t1", TrackersCount = 1 } },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category> { ["C"] = new ClientModels.Category("C", "/a", null) },
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["t1"] = new List<string> { h } },
                trackersRemoved: null,
                serverState: null);
            var list = _target.CreateMainData(start);

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category> { ["C"] = new ClientModels.Category("C", "/b", null) },
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["t1"] = new List<string> { h, "other" } },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, list, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();

            list.Trackers["t1"].Should().Equal(h, "other");
            list.Categories["C"].SavePath.Should().Be("/b");
        }

        // -------------------- MergeMainData: ServerState update --------------------

        [Fact]
        public void GIVEN_Existing_WHEN_ServerStateFieldsChange_THEN_DataChangedTrue_And_ValuesUpdated()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(0, true, null, null, null, null, null, null,
                    new Dictionary<string, IReadOnlyList<string>>(), null,
                    new ClientModels.ServerState(
                        allTimeDownloaded: 1, allTimeUploaded: 2, averageTimeQueue: 3,
                        connectionStatus: "connected", dHTNodes: 4, downloadInfoData: 5,
                        downloadInfoSpeed: 6, downloadRateLimit: 7, freeSpaceOnDisk: 8, globalRatio: 9.0f,
                        queuedIOJobs: 10, queuing: true, readCacheHits: 11.0f, readCacheOverload: 12.0f,
                        refreshInterval: 13, totalBuffersSize: 14, totalPeerConnections: 15, totalQueuedSize: 16,
                        totalWastedSession: 17, uploadInfoData: 18, uploadInfoSpeed: 19, uploadRateLimit: 20,
                        useAltSpeedLimits: false, useSubcategories: false, writeCacheOverload: 21.0f,
                        lastExternalAddressV4: "4", lastExternalAddressV6: "6")));

            var delta = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: null, torrentsRemoved: null,
                categories: null, categoriesRemoved: null,
                tags: null, tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(), trackersRemoved: null,
                serverState: new ClientModels.ServerState(
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

        [Fact]
        public void GIVEN_Existing_WHEN_ServerStateFieldsAreProvidedWithSameValues_THEN_DataChangedIsFalse()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(0, true, null, null, null, null, null, null,
                    new Dictionary<string, IReadOnlyList<string>>(), null,
                    new ClientModels.ServerState(
                        allTimeDownloaded: 1, allTimeUploaded: 2, averageTimeQueue: 3,
                        connectionStatus: "connected", dHTNodes: 4, downloadInfoData: 5,
                        downloadInfoSpeed: 6, downloadRateLimit: 7, freeSpaceOnDisk: 8, globalRatio: 9.0f,
                        queuedIOJobs: 10, queuing: true, readCacheHits: 11.0f, readCacheOverload: 12.0f,
                        refreshInterval: 13, totalBuffersSize: 14, totalPeerConnections: 15, totalQueuedSize: 16,
                        totalWastedSession: 17, uploadInfoData: 18, uploadInfoSpeed: 19, uploadRateLimit: 20,
                        useAltSpeedLimits: false, useSubcategories: false, writeCacheOverload: 21.0f,
                        lastExternalAddressV4: "4", lastExternalAddressV6: "6")));

            var delta = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: new ClientModels.ServerState(
                    allTimeDownloaded: 1, allTimeUploaded: 2, averageTimeQueue: 3,
                    connectionStatus: "connected", dHTNodes: 4, downloadInfoData: 5,
                    downloadInfoSpeed: 6, downloadRateLimit: 7, freeSpaceOnDisk: 8, globalRatio: 9.0f,
                    queuedIOJobs: 10, queuing: true, readCacheHits: 11.0f, readCacheOverload: 12.0f,
                    refreshInterval: 13, totalBuffersSize: 14, totalPeerConnections: 15, totalQueuedSize: 16,
                    totalWastedSession: 17, uploadInfoData: 18, uploadInfoSpeed: 19, uploadRateLimit: 20,
                    useAltSpeedLimits: false, useSubcategories: false, writeCacheOverload: 21.0f,
                    lastExternalAddressV4: "4", lastExternalAddressV6: "6"));

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeFalse();
            filterChanged.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Existing_WHEN_ServerStateFieldsAreAllNull_THEN_DataChangedIsFalse()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(0, true, null, null, null, null, null, null,
                    new Dictionary<string, IReadOnlyList<string>>(), null,
                    new ClientModels.ServerState(
                        allTimeDownloaded: 1, allTimeUploaded: 2, averageTimeQueue: 3,
                        connectionStatus: "connected", dHTNodes: 4, downloadInfoData: 5,
                        downloadInfoSpeed: 6, downloadRateLimit: 7, freeSpaceOnDisk: 8, globalRatio: 9.0f,
                        queuedIOJobs: 10, queuing: true, readCacheHits: 11.0f, readCacheOverload: 12.0f,
                        refreshInterval: 13, totalBuffersSize: 14, totalPeerConnections: 15, totalQueuedSize: 16,
                        totalWastedSession: 17, uploadInfoData: 18, uploadInfoSpeed: 19, uploadRateLimit: 20,
                        useAltSpeedLimits: false, useSubcategories: false, writeCacheOverload: 21.0f,
                        lastExternalAddressV4: "4", lastExternalAddressV6: "6")));

            var delta = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: new ClientModels.ServerState(
                    allTimeDownloaded: null,
                    allTimeUploaded: null,
                    averageTimeQueue: null,
                    connectionStatus: null,
                    dHTNodes: null,
                    downloadInfoData: null,
                    downloadInfoSpeed: null,
                    downloadRateLimit: null,
                    freeSpaceOnDisk: null,
                    globalRatio: null,
                    queuedIOJobs: null,
                    queuing: null,
                    readCacheHits: null,
                    readCacheOverload: null,
                    refreshInterval: null,
                    totalBuffersSize: null,
                    totalPeerConnections: null,
                    totalQueuedSize: null,
                    totalWastedSession: null,
                    uploadInfoData: null,
                    uploadInfoSpeed: null,
                    uploadRateLimit: null,
                    useAltSpeedLimits: null,
                    useSubcategories: null,
                    writeCacheOverload: null,
                    lastExternalAddressV4: null,
                    lastExternalAddressV6: null));

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeFalse();
            filterChanged.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NewTorrentWithNonBlankName_WHEN_MergeMainDataAddsTorrent_THEN_TransitionUsesTorrentName()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>(),
                    torrentsRemoved: null,
                    categories: null,
                    categoriesRemoved: null,
                    tags: null,
                    tagsRemoved: null,
                    trackers: new Dictionary<string, IReadOnlyList<string>>(),
                    trackersRemoved: null,
                    serverState: null));

            const string hash = "named-hash";
            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent
                    {
                        Name = "Torrent Name",
                        State = "downloading",
                        Downloaded = 0,
                        TotalSize = 100,
                        UploadSpeed = 0,
                        Category = string.Empty,
                        Tags = Array.Empty<string>(),
                        Tracker = string.Empty,
                        TrackersCount = 0
                    }
                },
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged, out var transitions);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            transitions.Should().ContainSingle();
            transitions[0].Name.Should().Be("Torrent Name");
            transitions[0].IsAdded.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ExistingTorrentWithName_WHEN_FinishedStateChanges_THEN_TransitionUsesTorrentName()
        {
            const string hash = "finished-name-hash";
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>
                    {
                        [hash] = new ClientModels.Torrent
                        {
                            Name = "Torrent Name",
                            State = "downloading",
                            Downloaded = 10,
                            TotalSize = 100,
                            UploadSpeed = 0,
                            Category = string.Empty,
                            Tags = Array.Empty<string>(),
                            Tracker = string.Empty,
                            TrackersCount = 0
                        }
                    },
                    torrentsRemoved: null,
                    categories: null,
                    categoriesRemoved: null,
                    tags: null,
                    tagsRemoved: null,
                    trackers: new Dictionary<string, IReadOnlyList<string>>(),
                    trackersRemoved: null,
                    serverState: null));

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent
                    {
                        Name = "Torrent Name",
                        State = "downloading",
                        Downloaded = 100,
                        TotalSize = 100,
                        UploadSpeed = 0
                    }
                },
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged, out var transitions);

            changed.Should().BeTrue();
            filterChanged.Should().BeFalse();
            transitions.Should().ContainSingle();
            transitions[0].Hash.Should().Be(hash);
            transitions[0].Name.Should().Be("Torrent Name");
            transitions[0].IsAdded.Should().BeFalse();
            transitions[0].PreviousIsFinished.Should().BeFalse();
            transitions[0].CurrentIsFinished.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NewTorrentWithBlankName_WHEN_MergeMainDataAddsTorrent_THEN_TransitionUsesHashDisplayName()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>(),
                    torrentsRemoved: null,
                    categories: null,
                    categoriesRemoved: null,
                    tags: null,
                    tagsRemoved: null,
                    trackers: new Dictionary<string, IReadOnlyList<string>>(),
                    trackersRemoved: null,
                    serverState: null));

            const string hash = "added-hash";
            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent
                    {
                        Name = "  ",
                        State = "downloading",
                        Downloaded = 0,
                        TotalSize = 100,
                        UploadSpeed = 0,
                        Category = string.Empty,
                        Tags = Array.Empty<string>(),
                        Tracker = string.Empty,
                        TrackersCount = 0
                    }
                },
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged, out var transitions);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            transitions.Should().ContainSingle();
            transitions[0].Hash.Should().Be(hash);
            transitions[0].Name.Should().Be(hash);
            transitions[0].IsAdded.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ExistingTorrentWithBlankName_WHEN_FinishedStateChanges_THEN_TransitionUsesHashDisplayName()
        {
            const string hash = "finished-hash";
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>
                    {
                        [hash] = new ClientModels.Torrent
                        {
                            Name = " ",
                            State = "downloading",
                            Downloaded = 10,
                            TotalSize = 100,
                            UploadSpeed = 0,
                            Category = string.Empty,
                            Tags = Array.Empty<string>(),
                            Tracker = string.Empty,
                            TrackersCount = 0
                        }
                    },
                    torrentsRemoved: null,
                    categories: null,
                    categoriesRemoved: null,
                    tags: null,
                    tagsRemoved: null,
                    trackers: new Dictionary<string, IReadOnlyList<string>>(),
                    trackersRemoved: null,
                    serverState: null));

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    [hash] = new ClientModels.Torrent
                    {
                        Name = " ",
                        State = "downloading",
                        Downloaded = 100,
                        TotalSize = 100,
                        UploadSpeed = 0
                    }
                },
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged, out var transitions);

            changed.Should().BeTrue();
            filterChanged.Should().BeFalse();
            transitions.Should().ContainSingle();
            transitions[0].Hash.Should().Be(hash);
            transitions[0].Name.Should().Be(hash);
            transitions[0].IsAdded.Should().BeFalse();
            transitions[0].PreviousIsFinished.Should().BeFalse();
            transitions[0].CurrentIsFinished.Should().BeTrue();
        }
    }
}
