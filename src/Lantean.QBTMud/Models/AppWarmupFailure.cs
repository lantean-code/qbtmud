namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents a captured warmup failure for a single initialization step.
    /// </summary>
    /// <param name="Step">The warmup step that failed.</param>
    /// <param name="Message">The failure message.</param>
    public sealed record AppWarmupFailure(AppWarmupStep Step, string Message);
}
