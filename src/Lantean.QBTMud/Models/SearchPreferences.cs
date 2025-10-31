namespace Lantean.QBTMud.Models
{
    public class SearchPreferences
    {
        public string SelectedCategory { get; set; } = SearchForm.AllCategoryId;

        public HashSet<string> SelectedPlugins { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public SearchInScope SearchIn { get; set; } = SearchInScope.Everywhere;

        public string? FilterText { get; set; }

        public int? MinimumSeeds { get; set; }

        public int? MaximumSeeds { get; set; }

        public double? MinimumSize { get; set; }

        public SearchSizeUnit MinimumSizeUnit { get; set; } = SearchSizeUnit.Mebibytes;

        public double? MaximumSize { get; set; }

        public SearchSizeUnit MaximumSizeUnit { get; set; } = SearchSizeUnit.Gibibytes;
    }
}