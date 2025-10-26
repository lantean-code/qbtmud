using System.Collections.Generic;

namespace Lantean.QBTMud.Models
{
    public class SearchForm
    {
        public const string AllCategoryId = "all";

        public string? SearchText { get; set; }

        public HashSet<string> SelectedPlugins { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string SelectedCategory { get; set; } = AllCategoryId;

        public string? FilterText { get; set; }

        public SearchInScope SearchIn { get; set; } = SearchInScope.Everywhere;

        public int? MinimumSeeds { get; set; }

        public int? MaximumSeeds { get; set; }

        public double? MinimumSize { get; set; }

        public SearchSizeUnit MinimumSizeUnit { get; set; } = SearchSizeUnit.Mebibytes;

        public double? MaximumSize { get; set; }

        public SearchSizeUnit MaximumSizeUnit { get; set; } = SearchSizeUnit.Gibibytes;
    }
}
