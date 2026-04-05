using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Models
{
    public class Torrent
    {
        public Torrent(
            string hash,
            long addedOn,
            long amountLeft,
            bool automaticTorrentManagement,
            double availability,
            string category,
            long completed,
            long completionOn,
            string contentPath,
            int downloadLimit,
            int downloadSpeed,
            long downloaded,
            long downloadedSession,
            long estimatedTimeOfArrival,
            bool firstLastPiecePriority,
            bool forceStart,
            string infoHashV1,
            string infoHashV2,
            long lastActivity,
            string magnetUri,
            double maxRatio,
            int maxSeedingTime,
            string name,
            int numberComplete,
            int numberIncomplete,
            int numberLeeches,
            int numberSeeds,
            int priority,
            double progress,
            double ratio,
            double ratioLimit,
            string savePath,
            long seedingTime,
            int seedingTimeLimit,
            long seenComplete,
            bool sequentialDownload,
            long size,
            TorrentState? state,
            bool superSeeding,
            IEnumerable<string> tags,
            long timeActive,
            long totalSize,
            string tracker,
            int trackersCount,
            bool hasTrackerError,
            bool hasTrackerWarning,
            bool hasOtherAnnounceError,
            int uploadLimit,
            long uploaded,
            long uploadedSession,
            int uploadSpeed,
            long reannounce,
            int inactiveSeedingTimeLimit,
            int maxInactiveSeedingTime,
            double popularity,
            string downloadPath,
            string rootPath,
            bool isPrivate,
            ShareLimitAction shareLimitAction,
            string comment)
        {
            Hash = hash;
            AddedOn = addedOn;
            AmountLeft = amountLeft;
            AutomaticTorrentManagement = automaticTorrentManagement;
            Availability = availability;
            Category = category;
            Completed = completed;
            CompletionOn = completionOn;
            ContentPath = contentPath;
            DownloadLimit = downloadLimit;
            DownloadSpeed = downloadSpeed;
            Downloaded = downloaded;
            DownloadedSession = downloadedSession;
            EstimatedTimeOfArrival = estimatedTimeOfArrival;
            FirstLastPiecePriority = firstLastPiecePriority;
            ForceStart = forceStart;
            InfoHashV1 = infoHashV1;
            InfoHashV2 = infoHashV2;
            LastActivity = lastActivity;
            MagnetUri = magnetUri;
            MaxRatio = maxRatio;
            MaxSeedingTime = maxSeedingTime;
            Name = name;
            NumberComplete = numberComplete;
            NumberIncomplete = numberIncomplete;
            NumberLeeches = numberLeeches;
            NumberSeeds = numberSeeds;
            Priority = priority;
            Progress = progress;
            Ratio = ratio;
            RatioLimit = ratioLimit;
            SavePath = savePath;
            SeedingTime = seedingTime;
            SeedingTimeLimit = seedingTimeLimit;
            SeenComplete = seenComplete;
            SequentialDownload = sequentialDownload;
            Size = size;
            State = state;
            SuperSeeding = superSeeding;
            Tags = tags.ToList();
            TimeActive = timeActive;
            TotalSize = totalSize;
            Tracker = tracker;
            TrackersCount = trackersCount;
            HasTrackerError = hasTrackerError;
            HasTrackerWarning = hasTrackerWarning;
            HasOtherAnnounceError = hasOtherAnnounceError;
            UploadLimit = uploadLimit;
            Uploaded = uploaded;
            UploadedSession = uploadedSession;
            UploadSpeed = uploadSpeed;
            Reannounce = reannounce;
            InactiveSeedingTimeLimit = inactiveSeedingTimeLimit;
            MaxInactiveSeedingTime = maxInactiveSeedingTime;
            Popularity = popularity;
            DownloadPath = downloadPath;
            RootPath = rootPath;
            IsPrivate = isPrivate;
            ShareLimitAction = shareLimitAction;
            Comment = comment;
        }

        protected Torrent()
        {
            Hash = string.Empty;
            Category = string.Empty;
            ContentPath = string.Empty;
            InfoHashV1 = string.Empty;
            InfoHashV2 = string.Empty;
            MagnetUri = string.Empty;
            Name = string.Empty;
            SavePath = string.Empty;
            DownloadPath = string.Empty;
            RootPath = string.Empty;
            Tags = new List<string>();
            Tracker = string.Empty;
            TrackersCount = 0;
            HasTrackerError = false;
            HasTrackerWarning = false;
            HasOtherAnnounceError = false;
            ShareLimitAction = ShareLimitAction.Default;
            Comment = string.Empty;
        }

        public string Hash { get; }

        public long AddedOn { get; set; }

        public long AmountLeft { get; set; }

        public bool AutomaticTorrentManagement { get; set; }

        public double Availability { get; set; }

        public string Category { get; set; }

        public long Completed { get; set; }

        public long CompletionOn { get; set; }

        public string ContentPath { get; set; }

        public int DownloadLimit { get; set; }

        public int DownloadSpeed { get; set; }

        public long Downloaded { get; set; }

        public long DownloadedSession { get; set; }

        public long EstimatedTimeOfArrival { get; set; }

        public bool FirstLastPiecePriority { get; set; }

        public bool ForceStart { get; set; }

        public string InfoHashV1 { get; set; }

        public string InfoHashV2 { get; set; }

        public long LastActivity { get; set; }

        public string MagnetUri { get; set; }

        public double MaxRatio { get; set; }

        public int MaxSeedingTime { get; set; }

        public string Name { get; set; }

        public int NumberComplete { get; set; }

        public int NumberIncomplete { get; set; }

        public int NumberLeeches { get; set; }

        public int NumberSeeds { get; set; }

        public int Priority { get; set; }

        public double Progress { get; set; }

        public double Ratio { get; set; }

        public double RatioLimit { get; set; }

        public double Popularity { get; set; }

        public string SavePath { get; set; }

        public string DownloadPath { get; set; }

        public string RootPath { get; set; }

        public long SeedingTime { get; set; }

        public int SeedingTimeLimit { get; set; }

        public long SeenComplete { get; set; }

        public bool SequentialDownload { get; set; }

        public long Size { get; set; }

        public TorrentState? State { get; set; }

        public bool SuperSeeding { get; set; }

        public List<string> Tags { get; set; }

        public long TimeActive { get; set; }

        public long TotalSize { get; set; }

        public string Tracker { get; set; }

        public int TrackersCount { get; set; }

        public bool HasTrackerError { get; set; }

        public bool HasTrackerWarning { get; set; }

        public bool HasOtherAnnounceError { get; set; }

        public int UploadLimit { get; set; }

        public long Uploaded { get; set; }

        public long UploadedSession { get; set; }

        public int UploadSpeed { get; set; }

        public long Reannounce { get; set; }

        public int InactiveSeedingTimeLimit { get; set; }

        public int MaxInactiveSeedingTime { get; set; }

        public bool IsPrivate { get; set; }

        public ShareLimitAction ShareLimitAction { get; set; }

        public string Comment { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            return ((Torrent)obj).Hash == Hash;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override string ToString()
        {
            return Hash;
        }
    }
}
