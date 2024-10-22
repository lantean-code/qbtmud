namespace Lantean.QBTMud.Models
{
    public class SearchForm
    {
        public string? SearchText { get; set; }

        public string SelectedPlugin { get; set; } = "all";

        public string SelectedCategory { get; set; } = "all";
    }
}