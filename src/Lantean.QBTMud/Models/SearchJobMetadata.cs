namespace Lantean.QBTMud.Models
{
    public class SearchJobMetadata
    {
        public int Id { get; set; }

        public string Pattern { get; set; } = string.Empty;

        public string Category { get; set; } = SearchForm.AllCategoryId;

        public List<string> Plugins { get; set; } = [];
    }
}
