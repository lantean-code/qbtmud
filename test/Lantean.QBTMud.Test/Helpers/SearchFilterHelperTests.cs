using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Helpers
{
    public sealed class SearchFilterHelperTests
    {
        [Fact]
        public void GIVEN_SizeFilterBelowZero_WHEN_Convert_THEN_ReturnsZero()
        {
            var options = new SearchFilterOptions(null, SearchInScope.Everywhere, null, null, -1, SearchSizeUnit.Mebibytes, null, SearchSizeUnit.Gibibytes);
            var count = SearchFilterHelper.CountVisible(Array.Empty<SearchResult>(), options);
            count.Should().Be(0);
        }

        [Fact]
        public void GIVEN_ExtremelyLargeSize_WHEN_Filter_THEN_ValueClamped()
        {
            var result = new SearchResult("http://desc", "Huge", long.MaxValue, "http://files", 1, 1, "http://site", "movies", null);
            var options = new SearchFilterOptions(null, SearchInScope.Everywhere, null, null, null, SearchSizeUnit.Bytes, 9.22e18, SearchSizeUnit.Gibibytes);

            var visible = SearchFilterHelper.ApplyFilters(new[] { result }, options);
            visible.Should().ContainSingle();
        }

        [Fact]
        public void GIVEN_FilterTextEverywhere_WHEN_SearchableFieldsEvaluated_THEN_ResultMatched()
        {
            var result = new SearchResult("http://desc", "Filtered Name", 1_000, "http://files", 1, 1, "http://site", "movies", null);
            var options = new SearchFilterOptions("site", SearchInScope.Everywhere, null, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            var filtered = SearchFilterHelper.ApplyFilters(new[] { result }, options);
            filtered.Should().ContainSingle();

            var missing = SearchFilterHelper.ApplyFilters(new[] { result }, options with { FilterText = "missing" });
            missing.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_MaximumSeeds_WHEN_ResultExceedsLimit_THEN_ResultFilteredOut()
        {
            var result = new SearchResult("DescriptionLink", "FileName", 1_000, "FileUrl", 1, 20, "SiteUrl", "EngineName", null);
            var options = new SearchFilterOptions(null, SearchInScope.Everywhere, null, 10, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            var filtered = SearchFilterHelper.ApplyFilters(new[] { result }, options);

            filtered.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_MinimumSize_WHEN_ResultIsBelowThreshold_THEN_ResultFilteredOut()
        {
            var result = new SearchResult("DescriptionLink", "FileName", 1_023, "FileUrl", 1, 1, "SiteUrl", "EngineName", null);
            var options = new SearchFilterOptions(null, SearchInScope.Everywhere, null, null, 1, SearchSizeUnit.Kibibytes, null, SearchSizeUnit.Bytes);

            var filtered = SearchFilterHelper.ApplyFilters(new[] { result }, options);

            filtered.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_NegativeSeeders_WHEN_MinimumSeedFilterApplied_THEN_SeedersAreNormalizedToZero()
        {
            var result = new SearchResult("DescriptionLink", "FileName", 1_000, "FileUrl", 1, -5, "SiteUrl", "EngineName", null);
            var options = new SearchFilterOptions(null, SearchInScope.Everywhere, 1, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            var visible = SearchFilterHelper.CountVisible(new[] { result }, options);

            visible.Should().Be(0);
        }

        [Fact]
        public void GIVEN_NameSearchScopeAndMissingFileName_WHEN_FilterApplied_THEN_ResultFilteredOut()
        {
            var result = new SearchResult("DescriptionLink", string.Empty, 1_000, "FileUrl", 1, 1, "SiteUrl", "EngineName", null);
            var options = new SearchFilterOptions("file", SearchInScope.Names, null, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            var filtered = SearchFilterHelper.ApplyFilters(new[] { result }, options);

            filtered.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_EverywhereSearchScopeAndNoSearchableMatches_WHEN_FilterApplied_THEN_ResultFilteredOut()
        {
            var result = new SearchResult(string.Empty, "FileName", 1_000, "FileUrl", 1, 1, string.Empty, string.Empty, null);
            var options = new SearchFilterOptions("needle", SearchInScope.Everywhere, null, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            var filtered = SearchFilterHelper.ApplyFilters(new[] { result }, options);

            filtered.Should().BeEmpty();
        }
    }
}
