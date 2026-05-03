using AwesomeAssertions;
using Lantean.QBTMud.Core.Helpers;
using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient.Models;

using ClientModels = QBittorrent.ApiClient.Models;

using MudCategory = Lantean.QBTMud.Core.Models.Category;
using MudMainData = Lantean.QBTMud.Core.Models.MainData;
using MudServerState = Lantean.QBTMud.Core.Models.ServerState;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Application.Test.Services
{
    public class TorrentDataManagerHelperTests
    {
        private readonly TorrentDataManager _target;

        public TorrentDataManagerHelperTests()
        {
            _target = new TorrentDataManager();
        }

        [Fact]
        public void GIVEN_MixedTagsOnTorrents_WHEN_RemovingTags_THEN_OnlyTaggedTorrentIsChanged()
        {
            var hashWithTag = "h1";
            var hashWithoutTag = "h2";
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>
                    {
                        [hashWithTag] = ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: new[] { "tagX" }, category: string.Empty, tracker: string.Empty, trackersCount: 0),
                        [hashWithoutTag] = ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: string.Empty, trackersCount: 0)
                    },
                    torrentsRemoved: null,
                    categories: new Dictionary<string, ClientModels.Category>(),
                    categoriesRemoved: null,
                    tags: new[] { "tagX" },
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
                tagsRemoved: new[] { "tagX", " " },
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            existing.Torrents[hashWithTag].Tags.Should().BeEmpty();
            existing.Torrents[hashWithoutTag].Tags.Should().BeEmpty();
            existing.TagState[FilterHelper.TAG_UNTAGGED].Should().Contain(hashWithTag);
        }

        [Fact]
        public void GIVEN_EmptyAndValidTags_WHEN_MergeMainDataAddsTags_THEN_EmptyTagsAreIgnored()
        {
            var hash = "h1";
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>
                    {
                        [hash] = ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: string.Empty, trackersCount: 0)
                    },
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
                tags: new[] { " new ", "trimmed\tignored", string.Empty },
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            existing.Tags.Should().Contain("new");
            existing.TagState.Should().ContainKey("new");
            existing.TagState.Should().ContainKey("trimmed");
        }

        [Fact]
        public void GIVEN_ExistingTrackerState_WHEN_TrackerHashesChange_THEN_SetIsClearedAndRebuilt()
        {
            var hash = "h1";
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>
                    {
                        [hash] = ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: "udp://old", trackersCount: 1)
                    },
                    torrentsRemoved: null,
                    categories: new Dictionary<string, ClientModels.Category>(),
                    categoriesRemoved: null,
                    tags: Array.Empty<string>(),
                    tagsRemoved: null,
                    trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://old"] = new[] { hash } },
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
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://old"] = new[] { "ghost" } },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            existing.TrackersState["udp://old"].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_CustomStatesMissingStatuses_WHEN_AddingTorrent_THEN_MissingSetsAreSkipped()
        {
            var main = CreateManualMainData();
            var delta = new ClientModels.MainData(
                responseId: 1,
                fullUpdate: false,
                torrents: new Dictionary<string, ClientModels.Torrent>
                {
                    ["h1"] = ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: string.Empty, trackersCount: 0)
                },
                torrentsRemoved: null,
                categories: new Dictionary<string, ClientModels.Category>(),
                categoriesRemoved: null,
                tags: Array.Empty<string>(),
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, main, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            main.TagState[FilterHelper.TAG_ALL].Should().Contain("h1");
            main.CategoriesState[FilterHelper.CATEGORY_ALL].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_RemovalWithMissingStatusSets_WHEN_RemovingTorrent_THEN_RemovalSkipsMissingStatuses()
        {
            var main = CreateManualMainData();
            var torrent = _target.CreateTorrent(
                "h1",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: "udp://t", trackersCount: 1));
            main.Torrents["h1"] = torrent;
            main.TagState[FilterHelper.TAG_ALL].Add("h1");
            main.CategoriesState[FilterHelper.CATEGORY_UNCATEGORIZED].Add("h1");
            main.TrackersState[FilterHelper.TRACKER_ALL].Add("h1");
            main.TrackersState["udp://t"] = new HashSet<string> { "h1" };
            main.TrackersState[FilterHelper.TRACKER_TRACKERLESS].Add("h1");
            main.TrackersState[FilterHelper.TRACKER_ERROR].Add("h1");
            main.TrackersState[FilterHelper.TRACKER_WARNING].Add("h1");
            main.TrackersState[FilterHelper.TRACKER_ANNOUNCE_ERROR].Add("h1");

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: new[] { "h1" },
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, main, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            main.Torrents.Should().BeEmpty();
            main.TrackersState[FilterHelper.TRACKER_ALL].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_TorrentWithTags_WHEN_UpdateTagStateForAddition_THEN_TagSetsArePopulated()
        {
            var main = CreateManualMainData();
            var torrent = _target.CreateTorrent(
                "h1",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: new[] { string.Empty, "tag1" }, category: string.Empty, tracker: string.Empty, trackersCount: 0));
            main.TagState[FilterHelper.TAG_UNTAGGED].Add("h1");
            torrent.Tags.Add(string.Empty);

            TorrentDataManager.UpdateTagStateForAddition(main, torrent, "h1");

            main.TagState[FilterHelper.TAG_UNTAGGED].Should().BeEmpty();
            main.TagState["tag1"].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_TagAlreadyPresent_WHEN_TagsIncludedInDelta_THEN_TagStateIsRebuilt()
        {
            var hash = "h1";
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
                    responseId: 1,
                    fullUpdate: true,
                    torrents: new Dictionary<string, ClientModels.Torrent>
                    {
                        [hash] = ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: new[] { "keep" }, category: string.Empty, tracker: string.Empty, trackersCount: 0)
                    },
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
                tags: new[] { "keep" },
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeFalse();
            filterChanged.Should().BeFalse();
            existing.TagState["keep"].Should().Contain(hash);
        }

        [Fact]
        public void GIVEN_TrackerStateMissingEntry_WHEN_TrackerHashesChange_THEN_NewSetIsCreated()
        {
            var hash = "h1";
            var main = new MudMainData(
                new Dictionary<string, MudTorrent> { [hash] = _target.CreateTorrent(hash, ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: "udp://t", trackersCount: 1)) },
                Array.Empty<string>(),
                new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>> { ["udp://t"] = new List<string> { hash } },
                new MudServerState(),
                new Dictionary<string, HashSet<string>>
                {
                    { FilterHelper.TAG_ALL, new HashSet<string>() },
                    { FilterHelper.TAG_UNTAGGED, new HashSet<string>() }
                },
                new Dictionary<string, HashSet<string>>
                {
                    { FilterHelper.CATEGORY_ALL, new HashSet<string>() },
                    { FilterHelper.CATEGORY_UNCATEGORIZED, new HashSet<string>() }
                },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>
                {
                    { FilterHelper.TRACKER_ALL, new HashSet<string>() },
                    { FilterHelper.TRACKER_TRACKERLESS, new HashSet<string>() },
                    { FilterHelper.TRACKER_ERROR, new HashSet<string>() },
                    { FilterHelper.TRACKER_WARNING, new HashSet<string>() },
                    { FilterHelper.TRACKER_ANNOUNCE_ERROR, new HashSet<string>() }
                });

            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: new Dictionary<string, IReadOnlyList<string>> { ["udp://t"] = new List<string> { hash, "ghost" } },
                trackersRemoved: null,
                serverState: null);

            var changed = _target.MergeMainData(delta, main, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeTrue();
            main.TrackersState["udp://t"].Should().ContainSingle().Which.Should().Be(hash);
        }

        [Fact]
        public void GIVEN_NoNewTags_WHEN_UpdateTagStateForUpdate_THEN_UnTaggedContainsHash()
        {
            var main = CreateManualMainData();
            main.TagState[FilterHelper.TAG_UNTAGGED].Clear();

            TorrentDataManager.UpdateTagStateForUpdate(main, "h1", new List<string> { "old" }, new List<string>());

            main.TagState[FilterHelper.TAG_UNTAGGED].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_PreviousCategory_WHEN_UpdateCategoryStateForRemoval_THEN_AllKeysAreCleared()
        {
            var main = CreateManualMainData();
            main.ServerState.UseSubcategories = true;
            main.CategoriesState["A/B"] = new HashSet<string> { "h1" };
            main.CategoriesState["A"] = new HashSet<string> { "h1" };

            TorrentDataManager.UpdateCategoryStateForRemoval(main, "h1", "A/B");

            main.CategoriesState["A/B"].Should().BeEmpty();
            main.CategoriesState["A"].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_StatusTransition_WHEN_UpdateStatusState_THEN_RemovedAndAdded()
        {
            var statuses = new Dictionary<string, HashSet<string>>
            {
                { nameof(Status.Active), new HashSet<string> { "h1" } },
                { nameof(Status.Inactive), new HashSet<string>() }
            };
            TorrentDataManager.UpdateStatusState(
                new MudMainData(
                    new Dictionary<string, MudTorrent>(),
                    Array.Empty<string>(),
                    new Dictionary<string, MudCategory>(),
                    new Dictionary<string, IReadOnlyList<string>>(),
                    new MudServerState(),
                    new Dictionary<string, HashSet<string>>(),
                    new Dictionary<string, HashSet<string>>(),
                    statuses,
                    new Dictionary<string, HashSet<string>>()),
                "h1",
                TorrentState.Downloading,
                1,
                TorrentState.StalledDownloading,
                0);

            statuses[nameof(Status.Active)].Should().BeEmpty();
            statuses[nameof(Status.Inactive)].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_TrackerChanges_WHEN_UpdateTrackerState_THEN_OldEntriesRemovedAndBucketsUpdated()
        {
            var trackersState = new Dictionary<string, HashSet<string>>
            {
                { "udp://old", new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_ALL, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_TRACKERLESS, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_ERROR, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_WARNING, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_ANNOUNCE_ERROR, new HashSet<string> { "h1" } }
            };
            var main = new MudMainData(
                new Dictionary<string, MudTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new MudServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                trackersState);

            var previousSnapshot = new TorrentDataManager.TorrentSnapshot("Cat", new List<string>(), "udp://old", TorrentState.Downloading, 0, 1, true, true, true);
            var updatedTorrent = _target.CreateTorrent(
                "h1",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: "udp://new", trackersCount: 0, hasTrackerError: false, hasTrackerWarning: true, hasOtherAnnounceError: false));

            TorrentDataManager.UpdateTrackerState(main, updatedTorrent, "h1", previousSnapshot);

            trackersState["udp://old"].Should().BeEmpty();
            trackersState["udp://new"].Should().Contain("h1");
            trackersState[FilterHelper.TRACKER_TRACKERLESS].Should().Contain("h1");
            trackersState[FilterHelper.TRACKER_ERROR].Should().BeEmpty();
            trackersState[FilterHelper.TRACKER_WARNING].Should().Contain("h1");
            trackersState[FilterHelper.TRACKER_ANNOUNCE_ERROR].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_TrackerSnapshot_WHEN_UpdateTrackerStateForRemoval_THEN_AllBucketsDropHash()
        {
            var trackersState = new Dictionary<string, HashSet<string>>
            {
                { "udp://t", new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_TRACKERLESS, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_ERROR, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_WARNING, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_ANNOUNCE_ERROR, new HashSet<string> { "h1" } }
            };
            var main = new MudMainData(
                new Dictionary<string, MudTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new MudServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                trackersState);
            var snapshot = new TorrentDataManager.TorrentSnapshot("Cat", new List<string>(), "udp://t", TorrentState.Unknown, 0, 0, true, true, true);

            TorrentDataManager.UpdateTrackerStateForRemoval(main, "h1", snapshot);

            trackersState["udp://t"].Should().BeEmpty();
            trackersState[FilterHelper.TRACKER_TRACKERLESS].Should().BeEmpty();
            trackersState[FilterHelper.TRACKER_ERROR].Should().BeEmpty();
            trackersState[FilterHelper.TRACKER_WARNING].Should().BeEmpty();
            trackersState[FilterHelper.TRACKER_ANNOUNCE_ERROR].Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_SubcategoryPath_WHEN_EnumerateCategoryKeys_THEN_ReturnsAllSegments()
        {
            var keys = TorrentDataManager.EnumerateCategoryKeys("Movies/HD/1080p", true).ToList();

            keys.Should().ContainInOrder(new[] { "Movies/HD/1080p", "Movies/HD", "Movies" });
        }

        [Fact]
        public void GIVEN_EmptyCategory_WHEN_EnumerateCategoryKeys_THEN_ReturnsNothing()
        {
            var keys = TorrentDataManager.EnumerateCategoryKeys(string.Empty, true).ToList();

            keys.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_SubcategoriesDisabled_WHEN_EnumerateCategoryKeys_THEN_ReturnsOnlyProvidedCategory()
        {
            var keys = TorrentDataManager.EnumerateCategoryKeys("Movies/HD/1080p", false).ToList();

            keys.Should().Equal("Movies/HD/1080p");
        }

        [Fact]
        public void GIVEN_SameCategorySavePath_WHEN_UpdateCategory_THEN_ReturnsFalse()
        {
            var category = new MudCategory("Cat", "/path");
            var changed = TorrentDataManager.UpdateCategory(category, new ClientModels.Category("Cat", "/path", null));

            changed.Should().BeFalse();
            category.SavePath.Should().Be("/path");
        }

        [Fact]
        public void GIVEN_NullCategorySavePath_WHEN_UpdateCategory_THEN_ReturnsFalse()
        {
            var category = new MudCategory("Cat", "/path");
            var changed = TorrentDataManager.UpdateCategory(category, new ClientModels.Category("Cat", null, null));

            changed.Should().BeFalse();
            category.SavePath.Should().Be("/path");
        }

        [Fact]
        public void GIVEN_TrackerSetToNull_WHEN_UpdateTrackerState_THEN_TrackerFallsBackToEmptyString()
        {
            var trackersState = new Dictionary<string, HashSet<string>>
            {
                { "udp://old", new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_ALL, new HashSet<string> { "h1" } },
                { FilterHelper.TRACKER_TRACKERLESS, new HashSet<string>() },
                { FilterHelper.TRACKER_ERROR, new HashSet<string>() },
                { FilterHelper.TRACKER_WARNING, new HashSet<string>() },
                { FilterHelper.TRACKER_ANNOUNCE_ERROR, new HashSet<string>() }
            };
            var main = new MudMainData(
                new Dictionary<string, MudTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new MudServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                trackersState);

            var previousSnapshot = new TorrentDataManager.TorrentSnapshot("Cat", new List<string>(), "udp://old", TorrentState.Downloading, 0, 1, false, false, false);
            var updatedTorrent = _target.CreateTorrent(
                "h1",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, tags: Array.Empty<string>(), category: string.Empty, tracker: "udp://new", trackersCount: 0));
            updatedTorrent.Tracker = null!;

            TorrentDataManager.UpdateTrackerState(main, updatedTorrent, "h1", previousSnapshot);

            trackersState["udp://old"].Should().BeEmpty();
            trackersState.ContainsKey("udp://new").Should().BeFalse();
            trackersState[FilterHelper.TRACKER_TRACKERLESS].Should().Contain("h1");
        }

        [Fact]
        public void GIVEN_TorrentDeltaWithAllFields_WHEN_UpdateTorrent_THEN_DataAndFilterFlagsAreTrue()
        {
            var existing = _target.CreateTorrent("hash", new ClientModels.Torrent());
            var delta = ClientTorrentFactory.Create(addedOn: 1, amountLeft: 2, automaticTorrentManagement: true, availability: 1.5f, category: "Cat", completed: 3, completionOn: 4, contentPath: "/content", downloaded: 5, downloadedSession: 6, downloadLimit: 7, downloadSpeed: 8, estimatedTimeOfArrival: 9, firstLastPiecePriority: true, forceStart: true, infoHashV1: "InfoHashV1", infoHashV2: "InfoHashV2", lastActivity: 10, magnetUri: "MagnetUri", maxRatio: 0.5f, maxSeedingTime: 11, name: "Name", numberComplete: 12, numberIncomplete: 13, numberLeeches: 14, numberSeeds: 15, priority: 1, progress: 0.2f, ratio: 0.3f, ratioLimit: 0.4f, savePath: "/save", seedingTime: 16, seedingTimeLimit: 17, seenComplete: 18, sequentialDownload: true, size: 19, state: TorrentState.Uploading, superSeeding: true, tags: new[] { " t1", "t2\tjunk" }, timeActive: 20, totalSize: 21, tracker: "udp://tracker", trackersCount: 2, hasTrackerError: true, hasTrackerWarning: true, hasOtherAnnounceError: true, uploadLimit: 22, uploaded: 23, uploadedSession: 24, uploadSpeed: 25, reannounce: 26, inactiveSeedingTimeLimit: 27, maxInactiveSeedingTime: 28, popularity: 29, downloadPath: "/dl", rootPath: "/root", isPrivate: true, shareLimitAction: ShareLimitAction.Stop, comment: "Comment");

            var result = TorrentDataManager.UpdateTorrent(existing, delta);

            result.DataChanged.Should().BeTrue();
            result.FilterChanged.Should().BeTrue();
            existing.Category.Should().Be("Cat");
            existing.Tags.Should().BeEquivalentTo(new[] { "t1", "t2" });
            existing.UploadSpeed.Should().Be(25);
            existing.Comment.Should().Be("Comment");
            existing.ShareLimitAction.Should().Be(ShareLimitAction.Stop);
        }

        [Fact]
        public void GIVEN_ExistingNameStateAndUploadSpeed_WHEN_UpdateTorrentHasSameValues_THEN_NoChangesAreReported()
        {
            var existing = _target.CreateTorrent(
                "hash",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 25, category: string.Empty, tags: Array.Empty<string>(), tracker: string.Empty, trackersCount: 0));

            var delta = ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 25);

            var result = TorrentDataManager.UpdateTorrent(existing, delta);

            result.DataChanged.Should().BeFalse();
            result.FilterChanged.Should().BeFalse();
            existing.Name.Should().Be("Name");
            existing.State.Should().Be(TorrentState.Downloading);
            existing.UploadSpeed.Should().Be(25);
        }

        [Fact]
        public void GIVEN_EmptyAndNonEmptyCategories_WHEN_CreateSnapshot_THEN_EmptyCategoryMapsToNull()
        {
            var emptyCategoryTorrent = _target.CreateTorrent(
                "hash-1",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, category: string.Empty, tags: Array.Empty<string>(), tracker: string.Empty, trackersCount: 0));

            var nonEmptyCategoryTorrent = _target.CreateTorrent(
                "hash-2",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, category: "Movies", tags: Array.Empty<string>(), tracker: string.Empty, trackersCount: 0));

            var emptySnapshot = TorrentDataManager.CreateSnapshot(emptyCategoryTorrent);
            var nonEmptySnapshot = TorrentDataManager.CreateSnapshot(nonEmptyCategoryTorrent);

            emptySnapshot.Category.Should().BeNull();
            nonEmptySnapshot.Category.Should().Be("Movies");
        }

        [Fact]
        public void GIVEN_NullCategory_WHEN_CreateSnapshot_THEN_CategoryIsNull()
        {
            var torrent = _target.CreateTorrent(
                "hash-3",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, category: "Movies", tags: Array.Empty<string>(), tracker: string.Empty, trackersCount: 0));
            torrent.Category = null!;

            var snapshot = TorrentDataManager.CreateSnapshot(torrent);

            snapshot.Category.Should().BeNull();
        }

        [Fact]
        public void GIVEN_NullTrackerAndState_WHEN_CreateSnapshot_THEN_TrackerAndStateFallbackToEmptyStrings()
        {
            var torrent = _target.CreateTorrent(
                "hash-4",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 0, category: string.Empty, tags: Array.Empty<string>(), tracker: "udp://tracker", trackersCount: 1));
            torrent.Tracker = null!;
            torrent.State = null!;

            var snapshot = TorrentDataManager.CreateSnapshot(torrent);

            snapshot.Tracker.Should().Be(string.Empty);
            snapshot.State.Should().BeNull();
        }

        [Fact]
        public void GIVEN_NullNameStateAndUploadSpeed_WHEN_UpdateTorrent_THEN_NoChangesAreReported()
        {
            var existing = _target.CreateTorrent(
                "hash",
                ClientTorrentFactory.Create(name: "Name", state: TorrentState.Downloading, uploadSpeed: 25, category: string.Empty, tags: Array.Empty<string>(), tracker: string.Empty, trackersCount: 0));

            var delta = new ClientModels.Torrent();

            var result = TorrentDataManager.UpdateTorrent(existing, delta);

            result.DataChanged.Should().BeFalse();
            result.FilterChanged.Should().BeFalse();
            existing.Name.Should().Be("Name");
            existing.State.Should().Be(TorrentState.Downloading);
            existing.UploadSpeed.Should().Be(25);
        }

        [Fact]
        public void GIVEN_ServerStateDelta_WHEN_MergeMainData_THEN_AllServerFieldsAreUpdated()
        {
            var existing = _target.CreateMainData(
                new ClientModels.MainData(
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
                    serverState: null));
            var deltaServerState = new ClientModels.ServerState(
                allTimeDownloaded: 10,
                allTimeUploaded: 20,
                averageTimeQueue: 30,
                connectionStatus: ConnectionStatus.Connected,
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
                lastExternalAddressV4: "LastExternalAddressV4",
                lastExternalAddressV6: "LastExternalAddressV6");
            var delta = new ClientModels.MainData(
                responseId: 2,
                fullUpdate: false,
                torrents: null,
                torrentsRemoved: null,
                categories: null,
                categoriesRemoved: null,
                tags: null,
                tagsRemoved: null,
                trackers: null,
                trackersRemoved: null,
                serverState: deltaServerState);

            var changed = _target.MergeMainData(delta, existing, out var filterChanged);

            changed.Should().BeTrue();
            filterChanged.Should().BeFalse();
            existing.ServerState.ConnectionStatus.Should().Be(ConnectionStatus.Connected);
            existing.ServerState.AllTimeUploaded.Should().Be(20);
            existing.ServerState.LastExternalAddressV4.Should().Be("LastExternalAddressV4");
            existing.ServerState.LastExternalAddressV6.Should().Be("LastExternalAddressV6");
            existing.ServerState.GlobalRatio.Should().Be(7.5f);
            existing.ServerState.UseSubcategories.Should().BeTrue();
        }

        private MudMainData CreateManualMainData()
        {
            return new MudMainData(
                new Dictionary<string, MudTorrent>(),
                Array.Empty<string>(),
                new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new MudServerState(),
                new Dictionary<string, HashSet<string>>
                {
                    { FilterHelper.TAG_ALL, new HashSet<string>() },
                    { FilterHelper.TAG_UNTAGGED, new HashSet<string>() }
                },
                new Dictionary<string, HashSet<string>>
                {
                    { FilterHelper.CATEGORY_ALL, new HashSet<string>() },
                    { FilterHelper.CATEGORY_UNCATEGORIZED, new HashSet<string>() }
                },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>
                {
                    { FilterHelper.TRACKER_ALL, new HashSet<string>() },
                    { FilterHelper.TRACKER_TRACKERLESS, new HashSet<string>() },
                    { FilterHelper.TRACKER_ERROR, new HashSet<string>() },
                    { FilterHelper.TRACKER_WARNING, new HashSet<string>() },
                    { FilterHelper.TRACKER_ANNOUNCE_ERROR, new HashSet<string>() }
                });
        }
    }
}
