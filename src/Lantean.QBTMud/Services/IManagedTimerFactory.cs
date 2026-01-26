namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates instances of <see cref="IManagedTimer"/>.
    /// </summary>
    public interface IManagedTimerFactory
    {
        /// <summary>
        /// Creates a managed timer with the specified name and interval.
        /// </summary>
        /// <param name="name">The timer name.</param>
        /// <param name="interval">The timer interval.</param>
        /// <returns>A new <see cref="IManagedTimer"/> instance.</returns>
        IManagedTimer Create(string name, TimeSpan interval);
    }
}
