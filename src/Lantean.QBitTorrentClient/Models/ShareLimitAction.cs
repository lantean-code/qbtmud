namespace Lantean.QBitTorrentClient.Models
{
    public enum ShareLimitAction
    {
        Default = -1, // special value

        Stop = 0,
        Remove = 1,
        RemoveWithContent = 3,
        EnableSuperSeeding = 2
    }
}