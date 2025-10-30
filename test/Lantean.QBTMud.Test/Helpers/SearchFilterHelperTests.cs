using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Xunit;

namespace Lantean.QBTMud.Test.Helpers
{
    public sealed class SearchFilterHelperTests
    {
        [Fact]
        public void GIVEN_NullResults_WHEN_ApplyFilters_THEN_ShouldThrow()
        {
            var options = CreateOptions();
            Action act = () => SearchFilterHelper.ApplyFilters(null!, options);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_NullOptions_WHEN_ApplyFilters_THEN_ShouldThrow()
        {
            var act = () => SearchFilterHelper.ApplyFilters(Array.Empty<SearchResult>(), null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_Filters_WHEN_ApplyFilters_THEN_ShouldReturnMatchingEntries()
        {
            var results = new[]
            {
                CreateResult("First match", seeders: 10, fileSize: 2L << 20, engine: "EngineA", site: "siteA"),
                CreateResult("Second", seeders: 3, fileSize: 1000, engine: "EngineB", site: "siteB")
            };

            var options = new SearchFilterOptions(
                FilterText: "match",
                SearchIn: SearchInScope.Names,
                MinimumSeeds: 5,
                MaximumSeeds: 20,
                MinimumSize: 1,
                MinimumSizeUnit: SearchSizeUnit.Mebibytes,
                MaximumSize: 3,
                MaximumSizeUnit: SearchSizeUnit.Mebibytes);

            var filtered = SearchFilterHelper.ApplyFilters(results, options);
            filtered.Should().ContainSingle().Which.FileName.Should().Be("First match");
        }

        [Fact]
        public void GIVEN_Filters_WHEN_CountVisible_THEN_ShouldMatchApplyFilters()
        {
            var results = new[]
            {
                CreateResult("keep", seeders: 5, fileSize: 1L << 20),
                CreateResult("discard", seeders: 1, fileSize: 1L << 19)
            };

            var options = new SearchFilterOptions(
                FilterText: "keep",
                SearchIn: SearchInScope.Everywhere,
                MinimumSeeds: 5,
                MaximumSeeds: null,
                MinimumSize: 0.5,
                MinimumSizeUnit: SearchSizeUnit.Mebibytes,
                MaximumSize: null,
                MaximumSizeUnit: SearchSizeUnit.Bytes);

            var count = SearchFilterHelper.CountVisible(results, options);

            count.Should().Be(1);
        }

        [Fact]
        public void GIVEN_NullInputs_WHEN_CountVisible_THEN_ShouldThrow()
        {
            var options = CreateOptions();
            Action actResults = () => SearchFilterHelper.CountVisible(null!, options);
            actResults.Should().Throw<ArgumentNullException>();

            Action actOptions = () => SearchFilterHelper.CountVisible(Array.Empty<SearchResult>(), null!);
            actOptions.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_FilterTextWithWhitespace_WHEN_Matches_THEN_ShouldTrimBeforeMatching()
        {
            var result = CreateResult("Example Name", engine: "EngineA");
            var options = new SearchFilterOptions("  Name  ", SearchInScope.Names, null, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            SearchFilterHelper.ApplyFilters(new[] { result }, options).Should().ContainSingle();
        }

        [Fact]
        public void GIVEN_SearchScopeNames_WHEN_NoMatchInName_THEN_ShouldExclude()
        {
            var result = CreateResult("Different", engine: "EngineMatch");
            var options = new SearchFilterOptions("Match", SearchInScope.Names, null, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            SearchFilterHelper.ApplyFilters(new[] { result }, options).Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_SearchScopeEverywhere_WHEN_MatchInOtherField_THEN_ShouldInclude()
        {
            var result = CreateResult("filename", engine: "EngineMatch");
            var options = new SearchFilterOptions("match", SearchInScope.Everywhere, null, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            SearchFilterHelper.ApplyFilters(new[] { result }, options).Should().ContainSingle();
        }

        [Fact]
        public void GIVEN_SeedFilters_WHEN_NormalizedSeedersApplied_THEN_ShouldRespectBounds()
        {
            var results = new[]
            {
                CreateResult("valid", seeders: 10),
                CreateResult("low", seeders: 2),
                CreateResult("negative", seeders: -5)
            };

            var options = new SearchFilterOptions(null, SearchInScope.Everywhere, 1, 9, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);

            var filtered = SearchFilterHelper.ApplyFilters(results, options);
            filtered.Should().ContainSingle().Which.FileName.Should().Be("low");
        }

        [Fact]
        public void GIVEN_SizeFilters_WHEN_ValuesAdjusted_THEN_ShouldFilterProperly()
        {
            var results = new[]
            {
                CreateResult("Small", fileSize: 500_000),
                CreateResult("Medium", fileSize: 2L << 20),
                CreateResult("Large", fileSize: 10L << 30)
            };

            var options = new SearchFilterOptions(
                FilterText: null,
                SearchIn: SearchInScope.Everywhere,
                MinimumSeeds: null,
                MaximumSeeds: null,
                MinimumSize: 1,
                MinimumSizeUnit: SearchSizeUnit.Mebibytes,
                MaximumSize: 3,
                MaximumSizeUnit: SearchSizeUnit.Gibibytes);

            var filtered = SearchFilterHelper.ApplyFilters(results, options);
            filtered.Should().ContainSingle().Which.FileName.Should().Be("Medium");
        }

        [Fact]
        public void GIVEN_SizeFilterMaxExtreme_WHEN_ApplyFilters_THEN_ShouldNotOverflow()
        {
            var result = CreateResult("Huge", fileSize: long.MaxValue);

            var options = new SearchFilterOptions(
                FilterText: null,
                SearchIn: SearchInScope.Everywhere,
                MinimumSeeds: null,
                MaximumSeeds: null,
                MinimumSize: null,
                MinimumSizeUnit: SearchSizeUnit.Bytes,
                MaximumSize: double.MaxValue,
                MaximumSizeUnit: SearchSizeUnit.Exbibytes);

            SearchFilterHelper.ApplyFilters(new[] { result }, options).Should().ContainSingle();
        }

        private static SearchFilterOptions CreateOptions()
        {
            return new SearchFilterOptions(null, SearchInScope.Everywhere, null, null, null, SearchSizeUnit.Bytes, null, SearchSizeUnit.Bytes);
        }

        private static SearchResult CreateResult(
            string name,
            int seeders = 0,
            long fileSize = 0,
            string engine = "",
            string site = "")
        {
            return new SearchResult(
                descriptionLink: "description",
                fileName: name,
                fileSize: fileSize,
                fileUrl: "http://example.com/file",
                leechers: 0,
                seeders: seeders,
                siteUrl: site,
                engineName: engine,
                publishedOn: null);
        }
    }
}
