using Lantean.QBTMud.Models;
using System.Text.Json.Serialization;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Aggregates transfer speed samples into time buckets and persists them locally.
    /// </summary>
    public class SpeedHistoryService : ISpeedHistoryService
    {
        private const string StorageKey = "qbtmud.speedhistory.v1";
        private const int SchemaVersion = 1;
        private static readonly TimeSpan DefaultFlushInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan MaximumRetention = TimeSpan.FromHours(24);

        private static readonly IReadOnlyDictionary<SpeedPeriod, BucketConfiguration> BucketConfigurations = new Dictionary<SpeedPeriod, BucketConfiguration>
        {
            { SpeedPeriod.Min1, new BucketConfiguration(TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1)) },
            { SpeedPeriod.Min5, new BucketConfiguration(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5)) },
            { SpeedPeriod.Min30, new BucketConfiguration(TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(30)) },
            { SpeedPeriod.Hour3, new BucketConfiguration(TimeSpan.FromSeconds(60), TimeSpan.FromHours(3)) },
            { SpeedPeriod.Hour6, new BucketConfiguration(TimeSpan.FromSeconds(120), TimeSpan.FromHours(6)) },
            { SpeedPeriod.Hour12, new BucketConfiguration(TimeSpan.FromSeconds(240), TimeSpan.FromHours(12)) },
            { SpeedPeriod.Hour24, new BucketConfiguration(TimeSpan.FromSeconds(480), TimeSpan.FromHours(24)) }
        };

        private readonly ILocalStorageService _localStorage;
        private readonly Dictionary<SpeedPeriod, Bucketizer> _bucketizers;
        private bool _isInitialized;
        private bool _stateDirty;
        private readonly TimeSpan _flushInterval;
        private DateTime? _lastPersistUtc;

        public SpeedHistoryService(ILocalStorageService localStorage, TimeSpan? flushInterval = null)
        {
            _localStorage = localStorage;
            _bucketizers = BucketConfigurations.ToDictionary(
                kvp => kvp.Key,
                kvp => new Bucketizer(kvp.Key, kvp.Value.BucketSize, kvp.Value.MaxDuration));
            _flushInterval = flushInterval ?? DefaultFlushInterval;
        }

        /// <summary>
        /// The timestamp of the most recent recorded sample.
        /// </summary>
        public DateTime? LastUpdatedUtc { get; private set; }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return;
            }

            var persistedState = await _localStorage.GetItemAsync<PersistedState>(StorageKey, cancellationToken);
            if (persistedState is null || persistedState.SchemaVersion != SchemaVersion)
            {
                _isInitialized = true;
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var bucketizer in _bucketizers.Values)
            {
                if (!persistedState.Buckets.TryGetValue(bucketizer.Period, out var persistedBuckets) || persistedBuckets is null)
                {
                    continue;
                }

                foreach (var bucket in persistedBuckets.OrderBy(b => b.StartUtc))
                {
                    if ((now - bucket.StartUtc) > MaximumRetention)
                    {
                        continue;
                    }

                    bucketizer.Buckets.Add(bucket);
                    bucketizer.TrimToDuration(now);
                }
            }

            LastUpdatedUtc = persistedState.LastUpdatedUtc;
            _isInitialized = true;
        }

        public async Task PushSampleAsync(DateTime timestampUtc, long downloadBytesPerSecond, long uploadBytesPerSecond, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                await InitializeAsync(cancellationToken);
            }

            var bucketsChanged = false;
            var bucketRolled = false;
            foreach (var bucketizer in _bucketizers.Values)
            {
                var result = bucketizer.PushSample(timestampUtc, downloadBytesPerSecond, uploadBytesPerSecond);
                bucketsChanged |= result.DataChanged;
                bucketRolled |= result.BucketRolled;
            }

            if (bucketsChanged)
            {
                _stateDirty = true;
                LastUpdatedUtc = timestampUtc;
                await MaybePersistAsync(bucketRolled, timestampUtc, cancellationToken);
            }
        }

        public async Task PersistAsync(CancellationToken cancellationToken = default, DateTime? timestampUtc = null)
        {
            if (!_isInitialized || !_stateDirty)
            {
                return;
            }

            var buckets = new Dictionary<SpeedPeriod, List<SpeedBucket>>();
            foreach (var bucketizer in _bucketizers)
            {
                var snapshot = bucketizer.Value.Snapshot(includeBuilder: true);
                buckets[bucketizer.Key] = snapshot;
            }

            var state = new PersistedState(SchemaVersion, buckets, LastUpdatedUtc);
            await _localStorage.SetItemAsync(StorageKey, state, cancellationToken);
            _stateDirty = false;
            _lastPersistUtc = timestampUtc ?? DateTime.UtcNow;
        }

        private Task MaybePersistAsync(bool bucketRolled, DateTime timestampUtc, CancellationToken cancellationToken)
        {
            if (!_stateDirty)
            {
                return Task.CompletedTask;
            }

            if (bucketRolled || ShouldFlush(timestampUtc))
            {
                return PersistAsync(cancellationToken, timestampUtc);
            }

            return Task.CompletedTask;
        }

        private bool ShouldFlush(DateTime timestampUtc)
        {
            if (_lastPersistUtc is null)
            {
                return true;
            }

            return (timestampUtc - _lastPersistUtc.Value) >= _flushInterval;
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            foreach (var bucketizer in _bucketizers.Values)
            {
                bucketizer.Clear();
            }

            LastUpdatedUtc = null;
            _stateDirty = false;
            await _localStorage.RemoveItemAsync(StorageKey, cancellationToken);
        }

        public IReadOnlyList<SpeedPoint> GetSeries(SpeedPeriod period, SpeedDirection direction)
        {
            if (!_bucketizers.TryGetValue(period, out var bucketizer))
            {
                return Array.Empty<SpeedPoint>();
            }

            var buckets = bucketizer.Snapshot(includeBuilder: true);
            return buckets
                .Select(b => new SpeedPoint(b.StartUtc, direction == SpeedDirection.Download ? b.AverageDownloadBytesPerSecond : b.AverageUploadBytesPerSecond))
                .ToList();
        }

        private sealed record BucketConfiguration(TimeSpan BucketSize, TimeSpan MaxDuration);

        private sealed class Bucketizer
        {
            public Bucketizer(SpeedPeriod period, TimeSpan bucketSize, TimeSpan maxDuration)
            {
                Period = period;
                BucketSize = bucketSize;
                MaxDuration = maxDuration;
            }

            public SpeedPeriod Period { get; }

            public TimeSpan BucketSize { get; }

            public TimeSpan MaxDuration { get; }

            public List<SpeedBucket> Buckets { get; } = new();

            private BucketBuilder? Builder { get; set; }

            public BucketPushResult PushSample(DateTime timestampUtc, long downloadBytesPerSecond, long uploadBytesPerSecond)
            {
                var bucketStart = AlignTimestamp(timestampUtc);
                var bucketRolled = Builder is not null && Builder.StartUtc != bucketStart;
                if (Builder is null || bucketRolled)
                {
                    CompleteCurrentBucket();
                    Builder = new BucketBuilder(bucketStart, BucketSize);
                }

                Builder.AddSample(downloadBytesPerSecond, uploadBytesPerSecond);
                return new BucketPushResult(bucketRolled);
            }

            public List<SpeedBucket> Snapshot(bool includeBuilder)
            {
                var snapshot = new List<SpeedBucket>(Buckets.Count + (includeBuilder ? 1 : 0));
                snapshot.AddRange(Buckets);
                if (includeBuilder && Builder is not null && Builder.SampleCount > 0)
                {
                    snapshot.Add(Builder.ToBucket());
                }

                return snapshot;
            }

            public void Clear()
            {
                Buckets.Clear();
                Builder = null;
            }

            public void TrimToDuration(DateTime? newestTimestampUtc = null)
            {
                if (newestTimestampUtc.HasValue)
                {
                    while (Buckets.Count > 0 && ((newestTimestampUtc.Value - Buckets[0].StartUtc) > MaxDuration))
                    {
                        Buckets.RemoveAt(0);
                    }
                }

                var coverage = TimeSpan.Zero;
                for (var i = Buckets.Count - 1; i >= 0; --i)
                {
                    coverage += TimeSpan.FromMilliseconds(Buckets[i].DurationMilliseconds);
                    if (coverage > MaxDuration)
                    {
                        Buckets.RemoveRange(0, i + 1);
                        return;
                    }
                }
            }

            private void CompleteCurrentBucket()
            {
                if (Builder is null || Builder.SampleCount == 0)
                {
                    return;
                }

                Buckets.Add(Builder.ToBucket());
                TrimToDuration(Builder.StartUtc);
            }

            private DateTime AlignTimestamp(DateTime timestampUtc)
            {
                var bucketTicks = BucketSize.Ticks;
                var alignedTicks = timestampUtc.Ticks - (timestampUtc.Ticks % bucketTicks);
                return new DateTime(alignedTicks, DateTimeKind.Utc);
            }
        }

        private sealed class BucketBuilder
        {
            public BucketBuilder(DateTime startUtc, TimeSpan duration)
            {
                StartUtc = startUtc;
                Duration = duration;
            }

            public DateTime StartUtc { get; }

            public TimeSpan Duration { get; }

            public int SampleCount { get; private set; }

            public double DownloadAccumulator { get; private set; }

            public double UploadAccumulator { get; private set; }

            public void AddSample(long downloadBytesPerSecond, long uploadBytesPerSecond)
            {
                ++SampleCount;
                DownloadAccumulator += downloadBytesPerSecond;
                UploadAccumulator += uploadBytesPerSecond;
            }

            public SpeedBucket ToBucket()
            {
                var downloadAverage = DownloadAccumulator / SampleCount;
                var uploadAverage = UploadAccumulator / SampleCount;
                return new SpeedBucket(StartUtc, (int)Duration.TotalMilliseconds, downloadAverage, uploadAverage);
            }
        }

        private readonly record struct BucketPushResult(bool BucketRolled)
        {
            public bool DataChanged => true;
        }

        private sealed class SpeedBucket
        {
            [JsonConstructor]
            public SpeedBucket(DateTime startUtc, int durationMilliseconds, double averageDownloadBytesPerSecond, double averageUploadBytesPerSecond)
            {
                StartUtc = startUtc;
                DurationMilliseconds = durationMilliseconds;
                AverageDownloadBytesPerSecond = averageDownloadBytesPerSecond;
                AverageUploadBytesPerSecond = averageUploadBytesPerSecond;
            }

            public DateTime StartUtc { get; }

            public int DurationMilliseconds { get; }

            public double AverageDownloadBytesPerSecond { get; }

            public double AverageUploadBytesPerSecond { get; }
        }

        private sealed class PersistedState
        {
            [JsonConstructor]
            public PersistedState(int schemaVersion, Dictionary<SpeedPeriod, List<SpeedBucket>> buckets, DateTime? lastUpdatedUtc)
            {
                SchemaVersion = schemaVersion;
                Buckets = buckets;
                LastUpdatedUtc = lastUpdatedUtc;
            }

            public int SchemaVersion { get; }

            public Dictionary<SpeedPeriod, List<SpeedBucket>> Buckets { get; }

            public DateTime? LastUpdatedUtc { get; }
        }
    }
}
