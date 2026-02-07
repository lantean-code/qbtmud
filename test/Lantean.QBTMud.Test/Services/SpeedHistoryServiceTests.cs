using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;

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
            await (_target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));

            var series = _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            series.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_MultipleSamplesInSameBucket_WHEN_GetSeries_THEN_ReturnsAveragedBucket()
        {
            await (_target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await (_target.PushSampleAsync(baseTime, 10, 20, Xunit.TestContext.Current.CancellationToken));
            await (_target.PushSampleAsync(baseTime.AddSeconds(3), 20, 30, Xunit.TestContext.Current.CancellationToken));

            var downloadSeries = _target.GetSeries(SpeedPeriod.Min5, SpeedDirection.Download);
            downloadSeries.Should().HaveCount(1);
            downloadSeries[0].BytesPerSecond.Should().BeApproximately(15, 0.0001);
        }

        [Fact]
        public async Task GIVEN_ManyBuckets_WHEN_ExceedingWindow_THEN_TrimsOldestBuckets()
        {
            await (_target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 40; ++i)
            {
                var sampleTime = baseTime.AddSeconds(i * 2);
                await (_target.PushSampleAsync(sampleTime, 100, 200, Xunit.TestContext.Current.CancellationToken));
            }

            var downloadSeries = _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            downloadSeries.Count.Should().BeLessThanOrEqualTo(31);
            var duration = downloadSeries.Last().TimestampUtc - downloadSeries.First().TimestampUtc;
            duration.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(64));
        }

        [Fact]
        public async Task GIVEN_PersistedData_WHEN_Reinitialized_THEN_RestoresBuckets()
        {
            await (_target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));
            var baseTime = DateTime.UtcNow;

            await (_target.PushSampleAsync(baseTime, 50, 60, Xunit.TestContext.Current.CancellationToken));
            await (_target.PersistAsync(Xunit.TestContext.Current.CancellationToken));

            var reloaded = new SpeedHistoryService(_localStorage);
            await (reloaded.InitializeAsync(Xunit.TestContext.Current.CancellationToken));

            var downloadSeries = reloaded.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            downloadSeries.Should().HaveCount(1);
            (downloadSeries[0].TimestampUtc - baseTime).Duration().Should().BeLessThan(TimeSpan.FromSeconds(3));
            downloadSeries[0].BytesPerSecond.Should().Be(50);
            reloaded.LastUpdatedUtc.Should().NotBeNull();
            (reloaded.LastUpdatedUtc!.Value - baseTime).Duration().Should().BeLessThan(TimeSpan.FromSeconds(3));
        }

        [Fact]
        public async Task GIVEN_UnknownPeriod_WHEN_GetSeries_THEN_ReturnsEmpty()
        {
            await (_target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));

            var series = _target.GetSeries((SpeedPeriod)(-1), SpeedDirection.Download);

            series.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_SamplesWithinFlushInterval_WHEN_PushingSamples_THEN_PersistsOnce()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage, TimeSpan.FromSeconds(1));
            await (target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await (target.PushSampleAsync(baseTime, 10, 10, Xunit.TestContext.Current.CancellationToken));
            await (target.PushSampleAsync(baseTime.AddMilliseconds(500), 20, 20, Xunit.TestContext.Current.CancellationToken));
            await (target.PushSampleAsync(baseTime.AddMilliseconds(900), 30, 30, Xunit.TestContext.Current.CancellationToken));

            localStorage.WriteCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_BucketRollover_WHEN_PushingSamples_THEN_PersistsCompletedBucket()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage, TimeSpan.FromHours(1));
            await (target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await (target.PushSampleAsync(baseTime, 10, 10, Xunit.TestContext.Current.CancellationToken));
            await (target.PushSampleAsync(baseTime.AddSeconds(3), 20, 20, Xunit.TestContext.Current.CancellationToken));

            localStorage.WriteCount.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_FlushIntervalElapsedWithoutRollover_WHEN_PushingSamples_THEN_PersistsBuilder()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage, TimeSpan.FromSeconds(1));
            await (target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await (target.PushSampleAsync(baseTime, 10, 10, Xunit.TestContext.Current.CancellationToken));
            await (target.PushSampleAsync(baseTime.AddSeconds(1.5), 20, 20, Xunit.TestContext.Current.CancellationToken));

            localStorage.WriteCount.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_PersistedStateAndOtherKeys_WHEN_Clear_THEN_RemovesStateAndResetsHistory()
        {
            var localStorage = new TestLocalStorageService();
            var target = new SpeedHistoryService(localStorage);
            await (target.InitializeAsync(Xunit.TestContext.Current.CancellationToken));
            var baseTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await (target.PushSampleAsync(baseTime, 10, 20, Xunit.TestContext.Current.CancellationToken));
            await (target.PersistAsync(Xunit.TestContext.Current.CancellationToken));
            await localStorage.SetItemAsStringAsync("OtherKey", "OtherValue", Xunit.TestContext.Current.CancellationToken);

            target.LastUpdatedUtc.Should().NotBeNull();
            localStorage.Snapshot().Keys.Should().Contain("qbtmud.speedhistory.v1");

            await (target.ClearAsync(Xunit.TestContext.Current.CancellationToken));

            target.LastUpdatedUtc.Should().BeNull();
            target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download).Should().BeEmpty();
            localStorage.Snapshot().Keys.Should().NotContain("qbtmud.speedhistory.v1");
            localStorage.Snapshot().Keys.Should().Contain("OtherKey");
        }
    }
}
