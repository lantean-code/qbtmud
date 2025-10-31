using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Models
{
    public record ShareRatio
    {
        public float RatioLimit { get; set; }
        public float SeedingTimeLimit { get; set; }
        public float InactiveSeedingTimeLimit { get; set; }
        public ShareLimitAction? ShareLimitAction { get; set; }
    }

    public record ShareRatioMax : ShareRatio
    {
        public float MaxRatio { get; set; }
        public float MaxSeedingTime { get; set; }
        public float MaxInactiveSeedingTime { get; set; }
    }
}