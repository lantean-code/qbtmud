using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class SpeedSeriesBuilderTests
    {
        private readonly SpeedSeriesBuilder _target;

        public SpeedSeriesBuilderTests()
        {
            _target = new SpeedSeriesBuilder();
        }

        [Fact]
        public void GIVEN_ContinuousSamples_WHEN_BuildSegmentsInvoked_THEN_ShouldReturnSingleSegment()
        {
            var windowStart = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var windowEnd = windowStart.AddMinutes(6);
            var bucketSize = TimeSpan.FromMinutes(2);
            var samples = new List<SpeedPoint>
            {
                new SpeedPoint(windowStart, 100),
                new SpeedPoint(windowStart.AddSeconds(30), 300),
                new SpeedPoint(windowStart.AddMinutes(2), 400),
                new SpeedPoint(windowStart.AddMinutes(4), 500)
            };

            var segments = _target.BuildSegments(samples, windowStart, windowEnd, bucketSize);

            segments.Count.Should().Be(1);
            segments[0].Count.Should().Be(3);
            segments[0][0].Value.Should().Be(200);
            segments[0][1].Value.Should().Be(400);
            segments[0][2].Value.Should().Be(500);
            segments[0].Select(p => p.DateTime).Should().BeEquivalentTo(new[]
            {
                windowStart,
                windowStart.AddMinutes(2),
                windowStart.AddMinutes(4)
            });
        }

        [Fact]
        public void GIVEN_SamplesWithGaps_WHEN_BuildSegmentsInvoked_THEN_ShouldSplitSegments()
        {
            var windowStart = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var windowEnd = windowStart.AddMinutes(12);
            var bucketSize = TimeSpan.FromMinutes(2);
            var samples = new List<SpeedPoint>
            {
                new SpeedPoint(windowStart, 100),
                new SpeedPoint(windowStart.AddMinutes(2), 200),
                new SpeedPoint(windowStart.AddMinutes(6), 300),
                new SpeedPoint(windowStart.AddMinutes(10), 400)
            };

            var segments = _target.BuildSegments(samples, windowStart, windowEnd, bucketSize);

            segments.Count.Should().Be(3);
            segments[0].Select(p => p.DateTime).Should().BeEquivalentTo(new[]
            {
                windowStart,
                windowStart.AddMinutes(2)
            });
            segments[1].Select(p => p.DateTime).Should().BeEquivalentTo(new[]
            {
                windowStart.AddMinutes(6)
            });
            segments[2].Select(p => p.DateTime).Should().BeEquivalentTo(new[]
            {
                windowStart.AddMinutes(10)
            });
        }
    }
}
