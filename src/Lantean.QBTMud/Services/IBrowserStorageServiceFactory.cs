namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates browser storage service instances for supported browser storage scopes.
    /// </summary>
    public interface IBrowserStorageServiceFactory
    {
        /// <summary>
        /// Creates a browser storage service for local storage.
        /// </summary>
        /// <returns>A browser storage service targeting local storage.</returns>
        IBrowserStorageService CreateLocalStorageService();

        /// <summary>
        /// Creates a browser storage service for session storage.
        /// </summary>
        /// <returns>A browser storage service targeting session storage.</returns>
        IBrowserStorageService CreateSessionStorageService();
    }
}
