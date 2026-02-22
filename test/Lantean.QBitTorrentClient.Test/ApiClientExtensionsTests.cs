using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Moq;

namespace Lantean.QBitTorrentClient.Test
{
    public sealed class ApiClientExtensionsTests
    {
        private readonly IApiClient _target;

        public ApiClientExtensionsTests()
        {
            _target = Mock.Of<IApiClient>();
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_StopTorrent_THEN_ShouldCallStopTorrentsWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.StopTorrents(null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.StopTorrent("Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.StopTorrents(null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_StopTorrents_THEN_ShouldCallStopTorrentsWithArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.StopTorrents(null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.StopTorrents(new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.StopTorrents(null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_StopAllTorrents_THEN_ShouldCallStopTorrentsWithAll()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.StopTorrents(true, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            await _target.StopAllTorrents();

            Mock.Get(_target).Verify(apiClient => apiClient.StopTorrents(true, It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_StartTorrent_THEN_ShouldCallStartTorrentsWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.StartTorrents(null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.StartTorrent("Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.StartTorrents(null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_StartTorrents_THEN_ShouldCallStartTorrentsWithArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.StartTorrents(null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.StartTorrents(new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.StartTorrents(null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_StartAllTorrents_THEN_ShouldCallStartTorrentsWithAll()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.StartTorrents(true, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            await _target.StartAllTorrents();

            Mock.Get(_target).Verify(apiClient => apiClient.StartTorrents(true, It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_HashAndDeleteFiles_WHEN_DeleteTorrent_THEN_ShouldCallDeleteTorrentsWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.DeleteTorrents(null, true, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.DeleteTorrent("Hash", true);

            Mock.Get(_target).Verify(apiClient => apiClient.DeleteTorrents(null, true, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_HashesAndDeleteFiles_WHEN_DeleteTorrents_THEN_ShouldCallDeleteTorrentsWithArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.DeleteTorrents(null, false, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.DeleteTorrents(new[] { "Hash1", "Hash2" }, false);

            Mock.Get(_target).Verify(apiClient => apiClient.DeleteTorrents(null, false, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DeleteFiles_WHEN_DeleteAllTorrents_THEN_ShouldCallDeleteTorrentsWithAll()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.DeleteTorrents(true, true, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            await _target.DeleteAllTorrents(true);

            Mock.Get(_target).Verify(apiClient => apiClient.DeleteTorrents(true, true, It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_HashAndNoMatchingTorrent_WHEN_GetTorrent_THEN_ShouldReturnNull()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.GetTorrentList(null, null, null, null, null, null, null, null, null, null, "Hash"))
                .ReturnsAsync(new List<Torrent>());

            var result = await _target.GetTorrent("Hash");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_HashAndMatchingTorrent_WHEN_GetTorrent_THEN_ShouldReturnFirstTorrent()
        {
            var expectedTorrent = new Torrent
            {
                Hash = "Hash",
                Name = "Name"
            };

            Mock.Get(_target)
                .Setup(apiClient => apiClient.GetTorrentList(null, null, null, null, null, null, null, null, null, null, "Hash"))
                .ReturnsAsync(new List<Torrent> { expectedTorrent });

            var result = await _target.GetTorrent("Hash");

            result.Should().NotBeNull();
            result.Should().BeSameAs(expectedTorrent);
        }

        [Fact]
        public async Task GIVEN_CategoryAndHash_WHEN_SetTorrentCategory_THEN_ShouldCallSetTorrentCategoryWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.SetTorrentCategory("Category", null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.SetTorrentCategory("Category", "Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.SetTorrentCategory("Category", null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategoryAndHashes_WHEN_SetTorrentCategory_THEN_ShouldCallSetTorrentCategoryWithArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.SetTorrentCategory("Category", null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.SetTorrentCategory("Category", new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.SetTorrentCategory("Category", null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_RemoveTorrentCategory_THEN_ShouldCallSetTorrentCategoryWithEmptyCategory()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.SetTorrentCategory(string.Empty, null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.RemoveTorrentCategory("Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.SetTorrentCategory(string.Empty, null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_RemoveTorrentCategory_THEN_ShouldCallSetTorrentCategoryWithEmptyCategoryAndArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.SetTorrentCategory(string.Empty, null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.RemoveTorrentCategory(new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.SetTorrentCategory(string.Empty, null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagsAndHash_WHEN_RemoveTorrentTags_THEN_ShouldCallRemoveTorrentTagsWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.RemoveTorrentTags(new[] { "Tag1", "Tag2" }, "Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagsAndHashes_WHEN_RemoveTorrentTags_THEN_ShouldCallRemoveTorrentTagsWithArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.RemoveTorrentTags(new[] { "Tag1", "Tag2" }, new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagAndHash_WHEN_RemoveTorrentTag_THEN_ShouldCallRemoveTorrentTagsWithSingleTag()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.RemoveTorrentTag("Tag", "Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagAndHashes_WHEN_RemoveTorrentTag_THEN_ShouldCallRemoveTorrentTagsWithSingleTagAndArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.RemoveTorrentTag("Tag", new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.RemoveTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagsAndHash_WHEN_AddTorrentTags_THEN_ShouldCallAddTorrentTagsWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.AddTorrentTags(new[] { "Tag1", "Tag2" }, "Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagsAndHashes_WHEN_AddTorrentTags_THEN_ShouldCallAddTorrentTagsWithArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.AddTorrentTags(new[] { "Tag1", "Tag2" }, new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag1", "Tag2" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagAndHash_WHEN_AddTorrentTag_THEN_ShouldCallAddTorrentTagsWithSingleTag()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.AddTorrentTag("Tag", "Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagAndHashes_WHEN_AddTorrentTag_THEN_ShouldCallAddTorrentTagsWithSingleTagAndArray()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))))
                .Returns(Task.CompletedTask);

            await _target.AddTorrentTag("Tag", new[] { "Hash1", "Hash2" });

            Mock.Get(_target).Verify(apiClient => apiClient.AddTorrentTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })), null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash1", "Hash2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_RecheckTorrent_THEN_ShouldCallRecheckTorrentsWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.RecheckTorrents(null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.RecheckTorrent("Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.RecheckTorrents(null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_ReannounceTorrent_THEN_ShouldCallReannounceTorrentsWithHash()
        {
            Mock.Get(_target)
                .Setup(apiClient => apiClient.ReannounceTorrents(null, null, "Hash"))
                .Returns(Task.CompletedTask);

            await _target.ReannounceTorrent("Hash");

            Mock.Get(_target).Verify(apiClient => apiClient.ReannounceTorrents(null, null, "Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_UsedAndUnusedCategories_WHEN_RemoveUnusedCategories_THEN_ShouldDeleteOnlyUnusedCategoryNames()
        {
            var torrents = new List<Torrent>
            {
                new Torrent { Category = "UsedCategory" },
                new Torrent { Category = "UsedCategory" },
                new Torrent { Category = null }
            };

            var categories = new Dictionary<string, Category>
            {
                ["UsedCategory"] = new Category("UsedCategory", "SavePath", null),
                ["UnusedCategory"] = new Category("UnusedCategory", "SavePath", null),
                ["NullName"] = new Category(null!, "SavePath", null)
            };

            Mock.Get(_target)
                .Setup(apiClient => apiClient.GetTorrentList(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .ReturnsAsync(torrents);
            Mock.Get(_target)
                .Setup(apiClient => apiClient.GetAllCategories())
                .ReturnsAsync(categories);
            Mock.Get(_target)
                .Setup(apiClient => apiClient.RemoveCategories(It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var result = await _target.RemoveUnusedCategories();

            result.Should().Equal("UnusedCategory");
            Mock.Get(_target).Verify(apiClient => apiClient.RemoveCategories(It.Is<string[]>(categoryNames => categoryNames.SequenceEqual(new[] { "UnusedCategory" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_UsedAndUnusedTags_WHEN_RemoveUnusedTags_THEN_ShouldDeleteOnlyUnusedTags()
        {
            var torrents = new List<Torrent>
            {
                new Torrent { Tags = new List<string> { "UsedTag", "OtherUsedTag", "UsedTag" } },
                new Torrent { Tags = null }
            };

            var tags = new List<string>
            {
                "UsedTag",
                "UnusedTag",
                "OtherUsedTag",
                "UnusedTag"
            };

            Mock.Get(_target)
                .Setup(apiClient => apiClient.GetTorrentList(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .ReturnsAsync(torrents);
            Mock.Get(_target)
                .Setup(apiClient => apiClient.GetAllTags())
                .ReturnsAsync(tags);
            Mock.Get(_target)
                .Setup(apiClient => apiClient.DeleteTags(It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var result = await _target.RemoveUnusedTags();

            result.Should().Equal("UnusedTag");
            Mock.Get(_target).Verify(apiClient => apiClient.DeleteTags(It.Is<string[]>(tagNames => tagNames.SequenceEqual(new[] { "UnusedTag" }))), Times.Once);
        }
    }
}
