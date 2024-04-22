namespace Lantean.QBTMudBlade.Models
{
    public class Torrent
    {
        public Torrent(
            string hash,
            long addedOn,
            long amountLeft,
            bool automaticTorrentManagement,
            float aavailability,
            string category,
            long completed,
            long completionOn,
            string contentPath,
            long downloadLimit,
            long downloadSpeed,
            long downloaded,
            long downloadedSession,
            long estimatedTimeOfArrival,
            bool firstLastPiecePriority,
            bool forceStart,
            string infoHashV1,
            string infoHashV2,
            long lastActivity,
            string magnetUri,
            float maxRatio,
            int maxSeedingTime,
            string name,
            int numberComplete,
            int numberIncomplete,
            int numberLeeches,
            int numberSeeds,
            int priority,
            float progress,
            float ratio,
            float ratioLimit,
            string savePath,
            long seedingTime,
            int seedingTimeLimit,
            long seenComplete,
            bool sequentialDownload,
            long size,
            string state,
            bool superSeeding,
            IEnumerable<string> tags,
            int timeActive,
            long totalSize,
            string tracker,
            long uploadLimit,
            long uploaded,
            long uploadedSession,
            long uploadSpeed,
            long reannounce)
        {
            Hash = hash;
            AddedOn = addedOn;
            AmountLeft = amountLeft;
            AutomaticTorrentManagement = automaticTorrentManagement;
            Availability = aavailability;
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
            UploadLimit = uploadLimit;
            Uploaded = uploaded;
            UploadedSession = uploadedSession;
            UploadSpeed = uploadSpeed;
            Reannounce = reannounce;
        }

        protected Torrent()
        {
            Hash = "";
            Category = "";
            ContentPath = "";
            InfoHashV1 = "";
            InfoHashV2 = "";
            MagnetUri = "";
            Name = "";
            SavePath = "";
            State = "";
            Tags = [];
            Tracker = "";
        }

        public string Hash { get; }

        public long AddedOn { get; set; }

        public long AmountLeft { get; set; }

        public bool AutomaticTorrentManagement { get; set; }

        public float Availability { get; set; }

        public string Category { get; set; }

        public long Completed { get; set; }

        public long CompletionOn { get; set; }

        public string ContentPath { get; set; }

        public long DownloadLimit { get; set; }

        public long DownloadSpeed { get; set; }

        public long Downloaded { get; set; }

        public long DownloadedSession { get; set; }

        public long EstimatedTimeOfArrival { get; set; }

        public bool FirstLastPiecePriority { get; set; }

        public bool ForceStart { get; set; }

        public string InfoHashV1 { get; set; }

        public string InfoHashV2 { get; set; }

        public long LastActivity { get; set; }

        public string MagnetUri { get; set; }

        public float MaxRatio { get; set; }

        public int MaxSeedingTime { get; set; }

        public string Name { get; set; }

        public int NumberComplete { get; set; }

        public int NumberIncomplete { get; set; }

        public int NumberLeeches { get; set; }

        public int NumberSeeds { get; set; }

        public int Priority { get; set; }

        public float Progress { get; set; }

        public float Ratio { get; set; }

        public float RatioLimit { get; set; }

        public string SavePath { get; set; }

        public long SeedingTime { get; set; }

        public int SeedingTimeLimit { get; set; }

        public long SeenComplete { get; set; }

        public bool SequentialDownload { get; set; }

        public long Size { get; set; }

        public string State { get; set; }

        public bool SuperSeeding { get; set; }

        public List<string> Tags { get; set; }

        public int TimeActive { get; set; }

        public long TotalSize { get; set; }

        public string Tracker { get; set; }

        public long UploadLimit { get; set; }

        public long Uploaded { get; set; }

        public long UploadedSession { get; set; }

        public long UploadSpeed { get; set; }

        public long Reannounce { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
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