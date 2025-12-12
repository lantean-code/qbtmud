using Lantean.QBTMud.Models;
using MudBlazor;

namespace Lantean.QBTMud.Services
{
    internal sealed class SpeedSeriesBuilder
    {
        public List<List<TimeSeriesChartSeries.TimeValue>> BuildSegments(IReadOnlyList<SpeedPoint> samples, DateTime windowStart, DateTime windowEnd, TimeSpan bucketSize)
        {
            var alignedStart = AlignTimestamp(windowStart, bucketSize);
            var alignedEnd = AlignTimestamp(windowEnd, bucketSize);

            var buckets = new Dictionary<DateTime, List<SpeedPoint>>();
            foreach (var sample in samples.Where(p => p.TimestampUtc >= windowStart && p.TimestampUtc <= windowEnd))
            {
                var key = AlignTimestamp(sample.TimestampUtc, bucketSize);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<SpeedPoint>();
                    buckets[key] = list;
                }

                list.Add(sample);
            }

            var segments = new List<List<TimeSeriesChartSeries.TimeValue>>();
            List<TimeSeriesChartSeries.TimeValue>? currentSegment = null;

            for (var cursor = alignedStart; cursor <= alignedEnd; cursor = cursor.Add(bucketSize))
            {
                if (buckets.TryGetValue(cursor, out var points) && points.Count > 0)
                {
                    currentSegment ??= new List<TimeSeriesChartSeries.TimeValue>();
                    currentSegment.Add(new TimeSeriesChartSeries.TimeValue(cursor, points.Average(p => p.BytesPerSecond)));
                }
                else if (currentSegment is not null && currentSegment.Count > 0)
                {
                    segments.Add(currentSegment);
                    currentSegment = null;
                }
            }

            if (currentSegment is not null && currentSegment.Count > 0)
            {
                segments.Add(currentSegment);
            }

            return segments;
        }

        private static DateTime AlignTimestamp(DateTime timestampUtc, TimeSpan bucketSize)
        {
            var ticks = bucketSize.Ticks;
            var alignedTicks = timestampUtc.Ticks - (timestampUtc.Ticks % ticks);
            return new DateTime(alignedTicks, DateTimeKind.Utc);
        }
    }
}
