using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using System.Text.Json.Nodes;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class SpeedHistoryServiceTests
    {
        private readonly TestLocalStorageService _localStorage;
        private readonly SpeedHistoryService _target;

        public SpeedHistoryServiceTests()
        {
            _localStorage = new TestLocalStorageService();
            _target = new SpeedHistoryService(_localStorage);
        }

        [Fact]
        public async Task GIVEN_NoPersistedState_WHEN_Initialize_THEN_HistoryIsEmpty()
        {
            await _target.InitializeAsync(TestContext.Current.CancellationToken);

            var series = _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            series.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_MultipleSamplesInSameBucket_WHEN_GetSeries_THEN_ReturnsAveragedBucket()
        {
            await _target.InitializeAsync(TestContext.Current.CancellationToken);
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await _target.PushSampleAsync(baseTime, 10, 20, TestContext.Current.CancellationToken);
            await _target.PushSampleAsync(baseTime.AddSeconds(3), 20, 30, TestContext.Current.CancellationToken);

            var downloadSeries = _target.GetSeries(SpeedPeriod.Min5, SpeedDirection.Download);
            downloadSeries.Should().HaveCount(1);
            downloadSeries[0].BytesPerSecond.Should().BeApproximately(15, 0.0001);
        }

        [Fact]
        public async Task GIVEN_ManyBuckets_WHEN_ExceedingWindow_THEN_TrimsOldestBuckets()
        {
            await _target.InitializeAsync(TestContext.Current.CancellationToken);
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 40; ++i)
            {
                var sampleTime = baseTime.AddSeconds(i * 2);
                await _target.PushSampleAsync(sampleTime, 100, 200, TestContext.Current.CancellationToken);
            }

            var downloadSeries = _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            downloadSeries.Count.Should().BeLessThanOrEqualTo(31);
            var duration = downloadSeries.Last().TimestampUtc - downloadSeries.First().TimestampUtc;
            duration.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(64));
        }

        [Fact]
        public async Task GIVEN_PersistedData_WHEN_Reinitialized_THEN_RestoresBuckets()
        {
            await _target.InitializeAsync(TestContext.Current.CancellationToken);
            var baseTime = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await _target.PushSampleAsync(baseTime, 50, 60, TestContext.Current.CancellationToken);
            await _target.PersistAsync(TestContext.Current.CancellationToken);

            var reloaded = new SpeedHistoryService(_localStorage);
            await reloaded.InitializeAsync(TestContext.Current.CancellationToken);

            var downloadSeries = reloaded.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            downloadSeries.Should().HaveCount(1);
            downloadSeries[0].TimestampUtc.Should().Be(baseTime);
            downloadSeries[0].BytesPerSecond.Should().Be(50);
            reloaded.LastUpdatedUtc.Should().NotBeNull();
            reloaded.LastUpdatedUtc!.Value.Should().Be(baseTime);
        }

        [Fact]
        public async Task GIVEN_UnknownPeriod_WHEN_GetSeries_THEN_ReturnsEmpty()
        {
            await _target.InitializeAsync(TestContext.Current.CancellationToken);

            var series = _target.GetSeries((SpeedPeriod)(-1), SpeedDirection.Download);

            series.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_SamplesWithinFlushInterval_WHEN_PushingSamples_THEN_PersistsOnce()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage, TimeSpan.FromSeconds(1));
            await target.InitializeAsync(TestContext.Current.CancellationToken);
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await target.PushSampleAsync(baseTime, 10, 10, TestContext.Current.CancellationToken);
            await target.PushSampleAsync(baseTime.AddMilliseconds(500), 20, 20, TestContext.Current.CancellationToken);
            await target.PushSampleAsync(baseTime.AddMilliseconds(900), 30, 30, TestContext.Current.CancellationToken);

            localStorage.WriteCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_BucketRollover_WHEN_PushingSamples_THEN_PersistsCompletedBucket()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage, TimeSpan.FromHours(1));
            await target.InitializeAsync(TestContext.Current.CancellationToken);
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await target.PushSampleAsync(baseTime, 10, 10, TestContext.Current.CancellationToken);
            await target.PushSampleAsync(baseTime.AddSeconds(3), 20, 20, TestContext.Current.CancellationToken);

            localStorage.WriteCount.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_FlushIntervalElapsedWithoutRollover_WHEN_PushingSamples_THEN_PersistsBuilder()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage, TimeSpan.FromSeconds(1));
            await target.InitializeAsync(TestContext.Current.CancellationToken);
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await target.PushSampleAsync(baseTime, 10, 10, TestContext.Current.CancellationToken);
            await target.PushSampleAsync(baseTime.AddSeconds(1.5), 20, 20, TestContext.Current.CancellationToken);

            localStorage.WriteCount.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_PersistedStateAndOtherKeys_WHEN_Clear_THEN_RemovesStateAndResetsHistory()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage);
            await target.InitializeAsync(TestContext.Current.CancellationToken);
            var baseTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await target.PushSampleAsync(baseTime, 10, 20, TestContext.Current.CancellationToken);
            await target.PersistAsync(TestContext.Current.CancellationToken);
            await localStorage.SetItemAsStringAsync("OtherKey", "OtherValue", TestContext.Current.CancellationToken);

            target.LastUpdatedUtc.Should().NotBeNull();
            localStorage.Snapshot().Keys.Should().Contain("SpeedHistory.State");

            await target.ClearAsync(TestContext.Current.CancellationToken);

            target.LastUpdatedUtc.Should().BeNull();
            target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download).Should().BeEmpty();
            localStorage.Snapshot().Keys.Should().NotContain("SpeedHistory.State");
            localStorage.Snapshot().Keys.Should().Contain("OtherKey");
        }

        [Fact]
        public async Task GIVEN_AlreadyInitialized_WHEN_InitializeCalledAgain_THEN_DoesNotReloadState()
        {
            var now = DateTime.UtcNow;
            var stale = now.AddDays(-3);
            var initial = new SpeedHistoryService(_localStorage);
            await initial.InitializeAsync(TestContext.Current.CancellationToken);
            await initial.PushSampleAsync(stale, 111, 222, TestContext.Current.CancellationToken);
            await initial.PersistAsync(TestContext.Current.CancellationToken);

            await _target.InitializeAsync(TestContext.Current.CancellationToken);
            await _target.InitializeAsync(TestContext.Current.CancellationToken);

            _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download).Should().BeEmpty();
            _target.LastUpdatedUtc.Should().Be(stale);
        }

        [Fact]
        public async Task GIVEN_NotInitialized_WHEN_PushSampleCalled_THEN_InitializesAndStoresSample()
        {
            var sampleTime = new DateTime(2024, 1, 1, 1, 2, 3, DateTimeKind.Utc);

            await _target.PushSampleAsync(sampleTime, 321, 654, TestContext.Current.CancellationToken);

            _target.LastUpdatedUtc.Should().Be(sampleTime);
            _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download).Should().HaveCount(1);
        }

        [Fact]
        public async Task GIVEN_PersistedBucketsForSubsetOfPeriods_WHEN_Initialized_THEN_LoadsRecentAndSkipsExpiredAndMissing()
        {
            var now = DateTime.UtcNow;
            var stale = now.AddHours(-30);
            var recent = now;
            var initial = new SpeedHistoryService(_localStorage);
            await initial.InitializeAsync(TestContext.Current.CancellationToken);
            await initial.PushSampleAsync(stale, 100, 200, TestContext.Current.CancellationToken);
            await initial.PushSampleAsync(recent, 300, 400, TestContext.Current.CancellationToken);
            await initial.PersistAsync(TestContext.Current.CancellationToken);

            var persisted = await _localStorage.GetItemAsStringAsync("SpeedHistory.State", TestContext.Current.CancellationToken);
            persisted.Should().NotBeNull();

            var root = JsonNode.Parse(persisted!)!.AsObject();
            var buckets = root["buckets"]!.AsObject();
            var chosenProperty = buckets.First();
            var chosenPeriod = ParsePeriodKey(chosenProperty.Key);
            var chosenValue = chosenProperty.Value!.DeepClone();
            buckets.Clear();
            buckets.Add(chosenProperty.Key, chosenValue);
            await _localStorage.SetItemAsStringAsync("SpeedHistory.State", root.ToJsonString(), TestContext.Current.CancellationToken);

            var reloaded = new SpeedHistoryService(_localStorage);
            await reloaded.InitializeAsync(TestContext.Current.CancellationToken);

            var download = reloaded.GetSeries(chosenPeriod, SpeedDirection.Download);
            var min5Download = reloaded.GetSeries(SpeedPeriod.Min5, SpeedDirection.Download);

            download.Should().NotBeEmpty();
            download.Any(point => point.BytesPerSecond == 300).Should().BeTrue();
            min5Download.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_PersistedBuckets_WHEN_RequestingUploadSeries_THEN_ReturnsUploadAverages()
        {
            var now = DateTime.UtcNow;
            var recent = now;
            var initial = new SpeedHistoryService(_localStorage);
            await initial.InitializeAsync(TestContext.Current.CancellationToken);
            await initial.PushSampleAsync(recent, 100, 4321, TestContext.Current.CancellationToken);
            await initial.PersistAsync(TestContext.Current.CancellationToken);

            var reloaded = new SpeedHistoryService(_localStorage);
            await reloaded.InitializeAsync(TestContext.Current.CancellationToken);

            var upload = reloaded.GetSeries(SpeedPeriod.Min1, SpeedDirection.Upload);

            upload.Should().HaveCount(1);
            upload[0].BytesPerSecond.Should().Be(4321);
        }

        [Fact]
        public async Task GIVEN_PersistedSampleWithinRetentionButOutsidePeriodWindow_WHEN_Initialized_THEN_TrimsSampleByDuration()
        {
            var now = DateTime.UtcNow;
            var oldSample = now.AddHours(-2);
            var initial = new SpeedHistoryService(_localStorage);
            await initial.InitializeAsync(TestContext.Current.CancellationToken);
            await initial.PushSampleAsync(oldSample, 999, 111, TestContext.Current.CancellationToken);
            await initial.PersistAsync(TestContext.Current.CancellationToken);

            var reloaded = new SpeedHistoryService(_localStorage);
            await reloaded.InitializeAsync(TestContext.Current.CancellationToken);

            reloaded.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download).Should().BeEmpty();
        }

        private static SpeedPeriod ParsePeriodKey(string key)
        {
            if (int.TryParse(key, out var numeric))
            {
                return (SpeedPeriod)numeric;
            }

            return Enum.Parse<SpeedPeriod>(key);
        }
    }
}
