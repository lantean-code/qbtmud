namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Initializes language resources and applies the active culture for the current session.
    /// </summary>
    public interface ILanguageInitializationService
    {
        /// <summary>
        /// Ensures language resources are initialized for the preferred locale.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask EnsureLanguageResourcesInitialized(CancellationToken cancellationToken = default);
    }
}
