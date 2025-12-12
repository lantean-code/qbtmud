using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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
            await _target.InitializeAsync();

            var series = _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            series.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_MultipleSamplesInSameBucket_WHEN_GetSeries_THEN_ReturnsAveragedBucket()
        {
            await _target.InitializeAsync();
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await _target.PushSampleAsync(baseTime, 10, 20);
            await _target.PushSampleAsync(baseTime.AddSeconds(3), 20, 30);

            var downloadSeries = _target.GetSeries(SpeedPeriod.Min5, SpeedDirection.Download);
            downloadSeries.Should().HaveCount(1);
            downloadSeries[0].BytesPerSecond.Should().BeApproximately(15, 0.0001);
        }

        [Fact]
        public async Task GIVEN_ManyBuckets_WHEN_ExceedingWindow_THEN_TrimsOldestBuckets()
        {
            await _target.InitializeAsync();
            var baseTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 40; ++i)
            {
                var sampleTime = baseTime.AddSeconds(i * 2);
                await _target.PushSampleAsync(sampleTime, 100, 200);
            }

            var downloadSeries = _target.GetSeries(SpeedPeriod.Min1, SpeedDirection.Download);

            downloadSeries.Count.Should().BeLessThanOrEqualTo(31);
            var duration = downloadSeries.Last().TimestampUtc - downloadSeries.First().TimestampUtc;
            duration.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(64));
        }

        [Fact]
        public async Task GIVEN_PersistedData_WHEN_Reinitialized_THEN_RestoresBuckets()
        {
            await _target.InitializeAsync();
            var baseTime = DateTime.UtcNow;

            await _target.PushSampleAsync(baseTime, 50, 60);
            await _target.PersistAsync();

            var reloaded = new SpeedHistoryService(_localStorage);
            await reloaded.InitializeAsync();

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
            await _target.InitializeAsync();

            var series = _target.GetSeries((SpeedPeriod)(-1), SpeedDirection.Download);

            series.Should().BeEmpty();
        }
    }
}
