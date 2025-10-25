namespace Lantean.QBTMud.Models
{
    public class LogForm
    {
        public bool Normal => SelectedTypes.Contains("Normal");
        public bool Info => SelectedTypes.Contains("Info");
        public bool Warning => SelectedTypes.Contains("Warning");
        public bool Critical => SelectedTypes.Contains("Critical");

        public int? LastKnownId { get; set; }

#pragma warning disable IDE0028 // Simplify collection initialization - the SelectedValues of MudSelect has issues with the type being HashSet<string> but it needs to be.
        public IEnumerable<string> SelectedTypes { get; set; } = new HashSet<string>();
#pragma warning restore IDE0028 // Simplify collection initialization

        public string? Criteria { get; set; }
    }
}