namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Coordinates lost-connection behavior.
    /// </summary>
    public interface ILostConnectionWorkflow
    {
        /// <summary>
        /// Marks the application as disconnected and coordinates lost-connection handling.
        /// </summary>
        /// <returns>A task that completes when the transition has been processed.</returns>
        Task MarkLostConnectionAsync();
    }
}
