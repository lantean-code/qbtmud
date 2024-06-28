namespace Lantean.QBTMudBlade.Models
{
    public class LogForm
    {
        public bool Normal => SelectedTypes.Contains("Normal");
        public bool Info => SelectedTypes.Contains("Info");
        public bool Warning => SelectedTypes.Contains("Warning");
        public bool Critical => SelectedTypes.Contains("Critical");

        public int? LastKnownId { get; set; }

        public IEnumerable<string> SelectedTypes { get; set; } = new HashSet<string>();

        public string? Criteria { get; set; }
    }
}