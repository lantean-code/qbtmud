using System;
using System.Collections.Generic;
using System.Linq;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Helpers
{
    public static class SearchFilterHelper
    {
        public static IReadOnlyList<SearchResult> ApplyFilters(IEnumerable<SearchResult> results, SearchFilterOptions options)
        {
            ArgumentNullException.ThrowIfNull(results);
            ArgumentNullException.ThrowIfNull(options);

            return results.Where(result => Matches(result, options)).ToList();
        }

        public static int CountVisible(IEnumerable<SearchResult> results, SearchFilterOptions options)
        {
            ArgumentNullException.ThrowIfNull(results);
            ArgumentNullException.ThrowIfNull(options);

            return results.Count(result => Matches(result, options));
        }

        private static bool Matches(SearchResult result, SearchFilterOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.FilterText))
            {
                var filter = options.FilterText.Trim();
                if (filter.Length > 0 && !MatchesFilter(result, filter, options.SearchIn))
                {
                    return false;
                }
            }

            var normalizedSeeds = NormalizePeerCount(result.Seeders);
            if (options.MinimumSeeds is int minimumSeeds && normalizedSeeds < minimumSeeds)
            {
                return false;
            }

            if (options.MaximumSeeds is int maximumSeeds && normalizedSeeds > maximumSeeds)
            {
                return false;
            }

            if (options.MinimumSize is double minimumSize && minimumSize > 0)
            {
                if (result.FileSize < ConvertToBytes(minimumSize, options.MinimumSizeUnit))
                {
                    return false;
                }
            }

            if (options.MaximumSize is double maximumSize && maximumSize > 0)
            {
                if (result.FileSize > ConvertToBytes(maximumSize, options.MaximumSizeUnit))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesFilter(SearchResult result, string filter, SearchInScope scope)
        {
            var comparison = StringComparison.OrdinalIgnoreCase;

            if (scope == SearchInScope.Names)
            {
                return !string.IsNullOrWhiteSpace(result.FileName)
                       && result.FileName.IndexOf(filter, comparison) >= 0;
            }

            return SearchableFields(result).Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(filter, comparison) >= 0);
        }

        private static IEnumerable<string?> SearchableFields(SearchResult result)
        {
            yield return result.FileName;
            yield return result.EngineName;
            yield return result.SiteUrl;
            yield return result.DescriptionLink;
        }

        private static int NormalizePeerCount(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static long ConvertToBytes(double value, SearchSizeUnit unit)
        {
            if (value <= 0)
            {
                return 0;
            }

            var exponent = (int)unit;
            var bytes = value * Math.Pow(1024, exponent);
            if (bytes >= long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long)bytes;
        }
    }
}
