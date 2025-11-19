namespace Lantean.QBitTorrentClient.Models
{
    public enum TrackerStatus
    {
        Disabled = 0,
        Uncontacted = 1,
        Working = 2,
        Updating = 3,
        NotWorking = 4,
        Error = 5,
        Unreachable = 6
    }
}