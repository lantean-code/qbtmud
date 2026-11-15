using AwesomeAssertions;
using Lantean.QBTMud.Core.Helpers;
using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient.Models;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Core.Test.Helpers
{
    public sealed class FilterHelperTests
    {
        [Theory]
        [InlineData("All")]
        [InlineData("Untagged")]
        [InlineData("Uncategorized")]
        public void GIVEN_UserDefinedReservedNames_WHEN_Filtering_THEN_ShouldKeepSyntheticFiltersDistinct(string name)
        {
            var named = CreateTorrent("Named", category: name, tags: [name]);
            var empty = CreateTorrent("Empty");

            FilterHelper.FilterTag(named, name).Should().BeTrue();
            FilterHelper.FilterTag(empty, name).Should().BeFalse();
            FilterHelper.FilterCategory(named, name, false).Should().BeTrue();
            FilterHelper.FilterCategory(empty, name, false).Should().BeFalse();
            FilterHelper.FilterTag(empty, FilterHelper.TAG_ALL).Should().BeTrue();
            FilterHelper.FilterCategory(empty, FilterHelper.CATEGORY_ALL, false).Should().BeTrue();
            FilterHelper.FilterTag(named, FilterHelper.TAG_UNTAGGED).Should().BeFalse();
            FilterHelper.FilterCategory(named, FilterHelper.CATEGORY_UNCATEGORIZED, false).Should().BeFalse();
        }

        [Theory]
        [InlineData("secondary.example.com")]
        [InlineData("udp://secondary.example.com/announce/")]
        public void GIVEN_SecondaryTrackerMembership_WHEN_Filtering_THEN_ShouldIncludeEveryAssociatedTorrent(string tracker)
        {
            var torrent = CreateTorrent("Hash", tracker: "https://primary.example.com/announce", trackersCount: 2);
            var unrelated = CreateTorrent("Unrelated", tracker: "udp://secondary.example.com/announce", trackersCount: 1);
            var trackers = new Dictionary<string, IReadOnlyList<string>>
            {
                ["https://primary.example.com/announce"] = ["Hash"],
                ["udp://secondary.example.com/announce"] = ["Hash", "Missing"],
                ["https://secondary.example.com/other"] = ["Hash"]
            };
            var state = new FilterState(FilterHelper.CATEGORY_ALL, Status.All, FilterHelper.TAG_ALL,
                tracker, false, null, TorrentFilterField.Name, false, true);

            new[] { torrent, unrelated }.Filter(state, trackers).Should().ContainSingle().Which.Should().Be(torrent);
            new[] { torrent }.FilterTracker(tracker, trackers).Should().ContainSingle();
            new[] { torrent }.FilterTracker(FilterHelper.TRACKER_ALL, trackers).Should().ContainSingle();
            new[] { torrent }.FilterTracker(tracker, new Dictionary<string, IReadOnlyList<string>>()).Should().BeEmpty();
        }

        [Theory]
        [InlineData(FilterHelper.TRACKER_ALL)]
        [InlineData(FilterHelper.TRACKER_TRACKERLESS)]
        [InlineData(FilterHelper.TRACKER_ERROR)]
        [InlineData(FilterHelper.TRACKER_WARNING)]
        [InlineData(FilterHelper.TRACKER_ANNOUNCE_ERROR)]
        [InlineData("")]
        public void GIVEN_SyntheticTrackerSelection_WHEN_MembershipIsAvailable_THEN_ShouldUseTorrentState(string tracker)
        {
            var torrent = CreateTorrent("Hash", hasTrackerError: true, hasTrackerWarning: true, hasOtherAnnounceError: true);

            new[] { torrent }.FilterTracker(tracker, new Dictionary<string, IReadOnlyList<string>>())
                .Should().ContainSingle().Which.Should().Be(torrent);
        }

        [Fact]
        public void GIVEN_NullCurrentTracker_WHEN_MembershipIsUnavailable_THEN_ShouldReturnNoMatch()
        {
            var torrent = CreateTorrent("Hash", tracker: null!);

            FilterHelper.FilterTracker(torrent, "tracker.example.com").Should().BeFalse();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_ExcessiveBacktracking_WHEN_FilteringRegex_THEN_ShouldHandleTimeout(bool exclude)
        {
            var input = new string('a', 10000) + "!";
            var pattern = (exclude ? "-" : string.Empty) + "(a+)+$";

            var result = FilterHelper.FilterTerms(input, pattern, useRegex: true, isRegexValid: true);

            result.Should().Be(exclude);
        }

        [Fact]
        public void GIVEN_Torrents_WHEN_FilterApplied_THEN_ShouldReturnMatches()
        {
            var torrents = new[]
            {
                CreateTorrent(
                    hash: "1",
                    name: "Match Name",
                    category: "Movies/Action",
                    tags: new[] { "tag1" },
                    tracker: "http://tracker.example.com/announce",
                    trackersCount: 1,
                    state: TorrentState.Downloading,
                    uploadSpeed: 100,
                    savePath: "/data/movies"),
                CreateTorrent(hash: "2", name: "Other", category: "Movies", tags: new[] { "tag1" }, tracker: "tracker.example.com", trackersCount: 1, state: TorrentState.Uploading),
                CreateTorrent(hash: "3", name: "Match Name", category: string.Empty, tags: Array.Empty<string>(), tracker: string.Empty, trackersCount: 0, state: TorrentState.QueuedUploading)
            };

            var filterState = new FilterState(
                category: "Movies",
                status: Status.Downloading,
                tag: "tag1",
                tracker: "tracker.example.com",
                useSubcategories: true,
                terms: "Match",
                filterField: TorrentFilterField.Name,
                useRegex: false,
                isRegexValid: true);

            var result = torrents.Filter(filterState);

            result.Should().ContainSingle().Which.Hash.Should().Be("1");
        }

        [Fact]
        public void GIVEN_Torrents_WHEN_ToHashesHashSet_THEN_ShouldDeduplicate()
        {
            var torrents = new[]
            {
                CreateTorrent(hash: "A"),
                CreateTorrent(hash: "A"),
                CreateTorrent(hash: "B")
            };

            torrents.ToHashesHashSet().Should().BeEquivalentTo(new HashSet<string> { "A", "B" });
        }

        [Fact]
        public void GIVEN_AddIfTrue_WHEN_ConditionMet_THEN_ShouldAdd()
        {
            var set = new HashSet<string>();
            set.AddIfTrue("value", true).Should().BeTrue();
            set.Contains("value").Should().BeTrue();
            set.AddIfTrue("value", false).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_RemoveIfTrue_WHEN_ConditionMet_THEN_ShouldRemove()
        {
            var set = new HashSet<string> { "value" };
            set.RemoveIfTrue("value", true).Should().BeTrue();
            set.Contains("value").Should().BeFalse();
            set.RemoveIfTrue("value", false).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_AddIfTrueOrRemove_WHEN_ConditionToggles_THEN_ShouldModifySet()
        {
            var set = new HashSet<string>();
            set.AddIfTrueOrRemove("value", true).Should().BeTrue();
            set.Contains("value").Should().BeTrue();
            set.AddIfTrueOrRemove("value", false).Should().BeTrue();
            set.Contains("value").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Terms_WHEN_ContainsAllTermsPlain_THEN_ShouldRespectIncludeAndExclude()
        {
            var terms = new[] { "match", "-skip", "+also" };
            FilterHelper.ContainsAllTerms("This will Match and Also item", terms, false).Should().BeTrue();
            FilterHelper.ContainsAllTerms("Skip this match", terms, false).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_TermsWithEmptyAndSignOnly_WHEN_ContainsAllTerms_THEN_ShouldSkipAndEvaluateRemaining()
        {
            var terms = new[] { "", "+", "-", "+include" };

            FilterHelper.ContainsAllTerms("include me", terms, false).Should().BeTrue();
            FilterHelper.ContainsAllTerms("something else", terms, false).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Terms_WHEN_ContainsAllTermsRegex_THEN_ShouldHandleInvalidPattern()
        {
            FilterHelper.ContainsAllTerms("123 value", new[] { "\\d+" }, true).Should().BeTrue();
            FilterHelper.ContainsAllTerms("value", new[] { "(" }, true).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_TextNull_WHEN_ContainsAllTerms_THEN_ShouldTreatAsEmpty()
        {
            var terms = new[] { "missing" };

            FilterHelper.ContainsAllTerms(null!, terms, false).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Field_WHEN_FilterTermsSimple_THEN_ShouldReturnExpected()
        {
            FilterHelper.FilterTerms("Sample text", null).Should().BeTrue();
            FilterHelper.FilterTerms("Sample text", "Sample").Should().BeTrue();
            FilterHelper.FilterTerms("Sample text", "Missing").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Field_WHEN_FilterTermsRegexInvalid_THEN_ShouldIgnore()
        {
            FilterHelper.FilterTerms("value", "(", useRegex: true, isRegexValid: false).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_RegexTerm_WHEN_FilterTermsRegexValid_THEN_ShouldEvaluate()
        {
            FilterHelper.FilterTerms("value123", "\\d+", useRegex: true, isRegexValid: true).Should().BeTrue();
            FilterHelper.FilterTerms("value", "\\d+", useRegex: true, isRegexValid: true).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Torrent_WHEN_FilterTermsUsesField_THEN_ShouldSelectProperField()
        {
            var torrent = CreateTorrent(hash: "1", name: "Movie", savePath: "/downloads");
            var state = new FilterState(FilterHelper.CATEGORY_ALL, Status.All, FilterHelper.TAG_ALL, FilterHelper.TRACKER_ALL, false, "/downloads", TorrentFilterField.SavePath, false, true);
            FilterHelper.FilterTerms(torrent, state).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_TorrentWithNullSavePath_WHEN_FilterTermsUsesSavePath_THEN_ShouldUseEmpty()
        {
            var torrent = CreateTorrent(hash: "1", name: "Movie", savePath: null!);
            var state = new FilterState(FilterHelper.CATEGORY_ALL, Status.All, FilterHelper.TAG_ALL, FilterHelper.TRACKER_ALL, false, "x", TorrentFilterField.SavePath, false, true);

            FilterHelper.FilterTerms(torrent, state).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Torrent_WHEN_FilterTermsWithTerms_THEN_ShouldEvaluateName()
        {
            var torrent = CreateTorrent(hash: "2", name: "Example Name");
            FilterHelper.FilterTerms(torrent, "Example").Should().BeTrue();
            FilterHelper.FilterTerms(torrent, "Missing").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Torrent_WHEN_FilterTrackerSpecialCases_THEN_ShouldBehave()
        {
            FilterHelper.FilterTracker(CreateTorrent(hash: "AllTest"), FilterHelper.TRACKER_ALL).Should().BeTrue();

            var trackerless = CreateTorrent(hash: "1", tracker: string.Empty, trackersCount: 0);
            FilterHelper.FilterTracker(trackerless, FilterHelper.TRACKER_TRACKERLESS).Should().BeTrue();
            var trackerlessWithCount = CreateTorrent(hash: "1c", tracker: string.Empty, trackersCount: 1);
            FilterHelper.FilterTracker(trackerlessWithCount, FilterHelper.TRACKER_TRACKERLESS).Should().BeTrue();

            var errorTorrent = CreateTorrent(hash: "2", hasTrackerError: true);
            FilterHelper.FilterTracker(errorTorrent, FilterHelper.TRACKER_ERROR).Should().BeTrue();

            var warningTorrent = CreateTorrent(hash: "3", hasTrackerWarning: true);
            FilterHelper.FilterTracker(warningTorrent, FilterHelper.TRACKER_WARNING).Should().BeTrue();

            var announceErrorTorrent = CreateTorrent(hash: "4", hasOtherAnnounceError: true);
            FilterHelper.FilterTracker(announceErrorTorrent, FilterHelper.TRACKER_ANNOUNCE_ERROR).Should().BeTrue();

            var hostTorrent = CreateTorrent(hash: "5", tracker: "udp://tracker.example.com:8080/announce", trackersCount: 1);
            FilterHelper.FilterTracker(hostTorrent, "tracker.example.com").Should().BeTrue();

            var urlTorrent = CreateTorrent(hash: "6", tracker: "http://tracker.example.com/announce", trackersCount: 1);
            FilterHelper.FilterTracker(urlTorrent, "http://tracker.example.com/announce/").Should().BeTrue();

            var trackerlessNonEmpty = CreateTorrent(hash: "7", tracker: "udp://tracker", trackersCount: 1);
            FilterHelper.FilterTracker(trackerlessNonEmpty, FilterHelper.TRACKER_TRACKERLESS).Should().BeFalse();
            FilterHelper.FilterTracker(trackerlessNonEmpty, string.Empty).Should().BeFalse();

            FilterHelper.FilterTracker(urlTorrent, string.Empty).Should().BeFalse();
            FilterHelper.FilterTracker(trackerless, string.Empty).Should().BeTrue();
            FilterHelper.FilterTracker(CreateTorrent(hash: "8", tracker: "udp://tracker"), "tracker").Should().BeTrue();
            FilterHelper.FilterTracker(CreateTorrent(hash: "9", tracker: string.Empty), "host").Should().BeFalse();
            FilterHelper.FilterTracker(CreateTorrent(hash: "10", tracker: "::::"), "::::").Should().BeTrue();
            FilterHelper.FilterTracker(CreateTorrent(hash: "11", tracker: "tracker.example.com"), "tracker.example.com").Should().BeTrue();
        }

        [Fact]
        public void GIVEN_Torrent_WHEN_FilterCategoryApplied_THEN_ShouldHandleScenarios()
        {
            var torrent = CreateTorrent(hash: "1", category: "Movies/Action");
            FilterHelper.FilterCategory(torrent, FilterHelper.CATEGORY_ALL, false).Should().BeTrue();
            FilterHelper.FilterCategory(torrent, FilterHelper.CATEGORY_UNCATEGORIZED, false).Should().BeFalse();
            FilterHelper.FilterCategory(CreateTorrent(hash: "2", category: string.Empty), FilterHelper.CATEGORY_UNCATEGORIZED, false).Should().BeTrue();
            FilterHelper.FilterCategory(torrent, "Movies", false).Should().BeFalse();
            FilterHelper.FilterCategory(torrent, "Movies/Action", false).Should().BeTrue();
            FilterHelper.FilterCategory(torrent, "Movies", true).Should().BeTrue();
            FilterHelper.FilterCategory(CreateTorrent(hash: "3", category: string.Empty), "Movies", false).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_Torrent_WHEN_FilterTagApplied_THEN_ShouldMatchRules()
        {
            var taggedTorrent = CreateTorrent(hash: "1", tags: new[] { "Tag1", "Tag2" });
            var untaggedTorrent = CreateTorrent(hash: "2", tags: Array.Empty<string>());

            FilterHelper.FilterTag(taggedTorrent, FilterHelper.TAG_ALL).Should().BeTrue();
            FilterHelper.FilterTag(untaggedTorrent, FilterHelper.TAG_UNTAGGED).Should().BeTrue();
            FilterHelper.FilterTag(taggedTorrent, "Tag2").Should().BeTrue();
            FilterHelper.FilterTag(taggedTorrent, "Missing").Should().BeFalse();
        }

        [Theory]
        [InlineData(TorrentState.Downloading, 0, Status.Downloading, true)]
        [InlineData(TorrentState.QueuedDownloading, 0, Status.Downloading, true)]
        [InlineData(TorrentState.Uploading, 0, Status.Downloading, false)]
        [InlineData(TorrentState.Uploading, 0, Status.Seeding, true)]
        [InlineData(TorrentState.Downloading, 0, Status.Seeding, false)]
        [InlineData(TorrentState.ForcedUploading, 0, Status.Completed, true)]
        [InlineData(TorrentState.Downloading, 0, Status.Completed, false)]
        [InlineData(TorrentState.StoppedDownloading, 0, Status.Stopped, true)]
        [InlineData(TorrentState.Downloading, 0, Status.Stopped, false)]
        [InlineData(TorrentState.StalledDownloading, 0, Status.Stalled, true)]
        [InlineData(TorrentState.Downloading, 0, Status.Stalled, false)]
        [InlineData(TorrentState.StalledUploading, 0, Status.StalledUploading, true)]
        [InlineData(TorrentState.Downloading, 0, Status.StalledUploading, false)]
        [InlineData(TorrentState.StalledDownloading, 0, Status.StalledDownloading, true)]
        [InlineData(TorrentState.StalledUploading, 0, Status.StalledDownloading, false)]
        [InlineData(TorrentState.CheckingResumeData, 0, Status.Checking, true)]
        [InlineData(TorrentState.Downloading, 0, Status.Checking, false)]
        [InlineData(TorrentState.Error, 0, Status.Errored, true)]
        [InlineData(TorrentState.Downloading, 0, Status.Errored, false)]
        public void GIVEN_State_WHEN_FilterStatusInvoked_THEN_ShouldReturnResult(TorrentState state, int uploadSpeed, Status status, bool expected)
        {
            FilterHelper.FilterStatus(state, uploadSpeed, status).Should().Be(expected);
        }

        [Fact]
        public void GIVEN_State_WHEN_FilterStatusActiveInactive_THEN_ShouldDistinguish()
        {
            FilterHelper.FilterStatus(TorrentState.Downloading, 0, Status.Active).Should().BeTrue();
            FilterHelper.FilterStatus(TorrentState.StalledDownloading, 0, Status.Active).Should().BeFalse();
            FilterHelper.FilterStatus(TorrentState.StalledDownloading, 0, Status.Inactive).Should().BeTrue();
            FilterHelper.FilterStatus(TorrentState.Downloading, 0, Status.Inactive).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_StatusString_WHEN_GetStatusName_THEN_ShouldFormat()
        {
            "StalledUploading".GetStatusName().Should().Be("Stalled Uploading");
            "StalledDownloading".GetStatusName().Should().Be("Stalled Downloading");
            "Other".GetStatusName().Should().Be("Other");
        }

        private static MudTorrent CreateTorrent(
            string hash,
            string name = "Name",
            string category = "",
            IEnumerable<string>? tags = null,
            string tracker = "",
            int trackersCount = 0,
            bool hasTrackerError = false,
            bool hasTrackerWarning = false,
            bool hasOtherAnnounceError = false,
            TorrentState? state = TorrentState.Downloading,
            int uploadSpeed = 0,
            string? savePath = "/downloads")
        {
            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                availability: 1,
                category,
                completed: 0,
                completionOn: 0,
                contentPath: string.Empty,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: string.Empty,
                infoHashV2: string.Empty,
                lastActivity: 0,
                magnetUri: string.Empty,
                maxRatio: 1,
                maxSeedingTime: 0,
                name,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath ?? string.Empty,
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state,
                superSeeding: false,
                tags ?? Array.Empty<string>(),
                timeActive: 0,
                totalSize: 0,
                tracker,
                trackersCount,
                hasTrackerError,
                hasTrackerWarning,
                hasOtherAnnounceError,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed,
                reannounce: 0,
                inactiveSeedingTimeLimit: 0,
                maxInactiveSeedingTime: 0,
                popularity: 0,
                downloadPath: string.Empty,
                rootPath: string.Empty,
                isPrivate: false,
                ShareLimitAction.Default,
                comment: string.Empty);
        }
    }
}
