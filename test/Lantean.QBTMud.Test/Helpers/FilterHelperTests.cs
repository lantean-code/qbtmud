using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using MudTorrent = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Test.Helpers
{
    public sealed class FilterHelperTests
    {
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
                    state: "downloading",
                    uploadSpeed: 100,
                    savePath: "/data/movies"),
                CreateTorrent(hash: "2", name: "Other", category: "Movies", tags: new[] { "tag1" }, tracker: "tracker.example.com", trackersCount: 1, state: "uploading"),
                CreateTorrent(hash: "3", name: "Match Name", category: string.Empty, tags: Array.Empty<string>(), tracker: string.Empty, trackersCount: 0, state: "queuedUP")
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
        public void GIVEN_Terms_WHEN_ContainsAllTermsRegex_THEN_ShouldHandleInvalidPattern()
        {
            FilterHelper.ContainsAllTerms("123 value", new[] { "\\d+" }, true).Should().BeTrue();
            FilterHelper.ContainsAllTerms("value", new[] { "(" }, true).Should().BeFalse();
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
        public void GIVEN_Torrent_WHEN_FilterTermsUsesField_THEN_ShouldSelectProperField()
        {
            var torrent = CreateTorrent(hash: "1", name: "Movie", savePath: "/downloads");
            var state = new FilterState(FilterHelper.CATEGORY_ALL, Status.All, FilterHelper.TAG_ALL, FilterHelper.TRACKER_ALL, false, "/downloads", TorrentFilterField.SavePath, false, true);
            FilterHelper.FilterTerms(torrent, state).Should().BeTrue();
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

            FilterHelper.FilterTracker(urlTorrent, string.Empty).Should().BeFalse();
            FilterHelper.FilterTracker(trackerless, string.Empty).Should().BeTrue();
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
        [InlineData("downloading", 0, Status.Downloading, true)]
        [InlineData("queuedDL", 0, Status.Downloading, true)]
        [InlineData("uploading", 0, Status.Downloading, false)]
        [InlineData("uploading", 0, Status.Seeding, true)]
        [InlineData("downloading", 0, Status.Seeding, false)]
        [InlineData("forcedUP", 0, Status.Completed, true)]
        [InlineData("downloading", 0, Status.Completed, false)]
        [InlineData("stoppedDL", 0, Status.Stopped, true)]
        [InlineData("downloading", 0, Status.Stopped, false)]
        [InlineData("stalledDL", 0, Status.Stalled, true)]
        [InlineData("downloading", 0, Status.Stalled, false)]
        [InlineData("stalledUP", 0, Status.StalledUploading, true)]
        [InlineData("downloading", 0, Status.StalledUploading, false)]
        [InlineData("stalledDL", 0, Status.StalledDownloading, true)]
        [InlineData("stalledUP", 0, Status.StalledDownloading, false)]
        [InlineData("checkingResumeData", 0, Status.Checking, true)]
        [InlineData("downloading", 0, Status.Checking, false)]
        [InlineData("error", 0, Status.Errored, true)]
        [InlineData("downloading", 0, Status.Errored, false)]
        public void GIVEN_State_WHEN_FilterStatusInvoked_THEN_ShouldReturnResult(string state, long uploadSpeed, Status status, bool expected)
        {
            FilterHelper.FilterStatus(state, uploadSpeed, status).Should().Be(expected);
        }

        [Fact]
        public void GIVEN_State_WHEN_FilterStatusActiveInactive_THEN_ShouldDistinguish()
        {
            FilterHelper.FilterStatus("downloading", 0, Status.Active).Should().BeTrue();
            FilterHelper.FilterStatus("stalledDL", 0, Status.Active).Should().BeFalse();
            FilterHelper.FilterStatus("stalledDL", 0, Status.Inactive).Should().BeTrue();
            FilterHelper.FilterStatus("downloading", 0, Status.Inactive).Should().BeFalse();
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
            string state = "downloading",
            long uploadSpeed = 0,
            string savePath = "/downloads")
        {
            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 1,
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
                savePath,
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