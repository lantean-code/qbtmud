namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents a single data point on a speed chart.
    /// </summary>
    public record SpeedPoint(DateTime TimestampUtc, double BytesPerSecond);
}
