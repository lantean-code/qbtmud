using System.Threading;
using System.Threading.Tasks;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents a timer that signals at a fixed interval.
    /// </summary>
    public interface IPeriodicTimer : IAsyncDisposable
    {
        /// <summary>
        /// Waits asynchronously for the next tick of the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        /// <returns><see langword="true"/> if the timer ticked; otherwise <see langword="false"/> when the timer is disposed.</returns>
        Task<bool> WaitForNextTickAsync(CancellationToken cancellationToken);
    }
}
