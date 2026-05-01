using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class StorageCatalogServiceTests
    {
        private readonly StorageCatalogService _target;

        public StorageCatalogServiceTests()
        {
            _target = new StorageCatalogService();
        }

        [Fact]
        public void GIVEN_ServiceConstructed_WHEN_GroupsAndItemsRead_THEN_ReturnsCatalogData()
        {
            _target.Groups.Should().NotBeEmpty();
            _target.Items.Should().NotBeEmpty();
            _target.Items.Should().Contain(item => item.MatchMode == StorageCatalogItemMatchMode.PrefixPattern);
        }

        [Fact]
        public void GIVEN_WhitespaceKey_WHEN_MatchItemByKeyInvoked_THEN_ReturnsNull()
        {
            var result = _target.MatchItemByKey("   ");

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_ExactKeyWithPadding_WHEN_MatchItemByKeyInvoked_THEN_ReturnsExactItem()
        {
            var result = _target.MatchItemByKey("  ThemeManager.LocalThemes  ");

            result.Should().NotBeNull();
            result!.Id.Should().Be("themes.local-themes");
            result.MatchMode.Should().Be(StorageCatalogItemMatchMode.ExactKey);
        }

        [Fact]
        public void GIVEN_PrefixKey_WHEN_MatchItemByKeyInvoked_THEN_ReturnsPrefixItem()
        {
            var result = _target.MatchItemByKey("DynamicTableTorrent.ColumnWidths.Main");

            result.Should().NotBeNull();
            result!.Id.Should().Be("tables.dynamic");
            result.MatchMode.Should().Be(StorageCatalogItemMatchMode.PrefixPattern);
        }

        [Fact]
        public void GIVEN_UnknownKey_WHEN_MatchItemByKeyInvoked_THEN_ReturnsNull()
        {
            var result = _target.MatchItemByKey("Unknown.Storage.Key");

            result.Should().BeNull();
        }
    }
}
