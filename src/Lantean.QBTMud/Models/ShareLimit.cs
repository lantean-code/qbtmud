using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Models
{
    public record ShareLimit
    {
        public float RatioLimit { get; set; }
        public int SeedingTimeLimit { get; set; }
        public int InactiveSeedingTimeLimit { get; set; }
        public ShareLimitAction? ShareLimitAction { get; set; }
    }

    public record ShareLimitMax : ShareLimit
    {
        public float MaxRatio { get; set; }
        public int MaxSeedingTime { get; set; }
        public int MaxInactiveSeedingTime { get; set; }
    }
}
