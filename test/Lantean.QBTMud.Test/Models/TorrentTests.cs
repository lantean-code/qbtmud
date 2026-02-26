using AwesomeAssertions;
using Lantean.QBTMud.Models;
using ShareLimitAction = Lantean.QBitTorrentClient.Models.ShareLimitAction;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class TorrentTests
    {
        private readonly Torrent _target;

        public TorrentTests()
        {
            _target = CreateTorrent();
        }

        [Fact]
        public void GIVEN_ConstructorParameters_WHEN_Created_THEN_AssignsValuesAndCopiesTags()
        {
            var tags = new List<string> { "TagA", "TagB" };
            var torrent = CreateTorrent(tags: tags);

            tags.Add("TagC");

            torrent.Hash.Should().Be("Hash");
            torrent.Category.Should().Be("Category");
            torrent.AutomaticTorrentManagement.Should().BeTrue();
            torrent.DownloadPath.Should().Be("DownloadPath");
            torrent.RootPath.Should().Be("RootPath");
            torrent.ShareLimitAction.Should().Be(ShareLimitAction.Stop);
            torrent.Comment.Should().Be("Comment");
            torrent.Tags.Should().Equal("TagA", "TagB");
        }

        [Fact]
        public void GIVEN_ProtectedDefaultConstructor_WHEN_DerivedTypeCreated_THEN_InitializesDefaults()
        {
            var torrent = new TestTorrent();

            torrent.Hash.Should().Be(string.Empty);
            torrent.Category.Should().Be(string.Empty);
            torrent.ContentPath.Should().Be(string.Empty);
            torrent.InfoHashV1.Should().Be(string.Empty);
            torrent.InfoHashV2.Should().Be(string.Empty);
            torrent.MagnetUri.Should().Be(string.Empty);
            torrent.Name.Should().Be(string.Empty);
            torrent.SavePath.Should().Be(string.Empty);
            torrent.DownloadPath.Should().Be(string.Empty);
            torrent.RootPath.Should().Be(string.Empty);
            torrent.State.Should().Be(string.Empty);
            torrent.Tracker.Should().Be(string.Empty);
            torrent.Tags.Should().BeEmpty();
            torrent.TrackersCount.Should().Be(0);
            torrent.HasTrackerError.Should().BeFalse();
            torrent.HasTrackerWarning.Should().BeFalse();
            torrent.HasOtherAnnounceError.Should().BeFalse();
            torrent.ShareLimitAction.Should().Be(ShareLimitAction.Default);
            torrent.Comment.Should().Be(string.Empty);
        }

        [Fact]
        public void GIVEN_SameHashTorrents_WHEN_EqualsCalled_THEN_ReturnsTrue()
        {
            var sameHash = CreateTorrent();

            _target.Equals(sameHash).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NullOrDifferentHash_WHEN_EqualsCalled_THEN_ReturnsFalse()
        {
            var differentHash = CreateTorrent(hash: "OtherHash");

#pragma warning disable CS8602
            _target.Equals(null).Should().BeFalse();
            _target.Equals(differentHash!).Should().BeFalse();
#pragma warning restore CS8602
            var differentTypeCheck = () => _target.Equals("Hash");
            differentTypeCheck.Should().Throw<InvalidCastException>();
        }

        [Fact]
        public void GIVEN_HashValue_WHEN_GetHashCodeAndToStringCalled_THEN_UseHash()
        {
            _target.GetHashCode().Should().Be("Hash".GetHashCode());
            _target.ToString().Should().Be("Hash");
        }

        private static Torrent CreateTorrent(string hash = "Hash", IEnumerable<string>? tags = null)
        {
            return new Torrent(
                hash,
                1,
                2,
                true,
                0.5f,
                "Category",
                3,
                4,
                "ContentPath",
                5,
                6,
                7,
                8,
                9,
                true,
                true,
                "InfoHashV1",
                "InfoHashV2",
                10,
                "MagnetUri",
                1.2f,
                11,
                "Name",
                12,
                13,
                14,
                15,
                16,
                0.7f,
                1.1f,
                1.3f,
                "SavePath",
                17,
                18,
                19,
                true,
                20,
                "State",
                true,
                tags ?? new[] { "TagA", "TagB" },
                21,
                22,
                "Tracker",
                23,
                true,
                true,
                true,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
                "DownloadPath",
                "RootPath",
                true,
                ShareLimitAction.Stop,
                "Comment");
        }

        private sealed class TestTorrent : Torrent
        {
            public TestTorrent()
            {
            }
        }
    }
}
