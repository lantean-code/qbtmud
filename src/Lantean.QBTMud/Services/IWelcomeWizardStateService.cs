using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides persistence and migration helpers for welcome wizard progress state.
    /// </summary>
    public interface IWelcomeWizardStateService
    {
        /// <summary>
        /// Gets the current wizard progress state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The current wizard progress state.</returns>
        Task<WelcomeWizardState> GetStateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists wizard progress state.
        /// </summary>
        /// <param name="state">The state to persist.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The persisted normalized state.</returns>
        Task<WelcomeWizardState> SaveStateAsync(WelcomeWizardState state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the wizard as shown.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The persisted normalized state.</returns>
        Task<WelcomeWizardState> MarkShownAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges the supplied step identifiers.
        /// </summary>
        /// <param name="stepIds">The step identifiers to acknowledge.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The persisted normalized state.</returns>
        Task<WelcomeWizardState> AcknowledgeStepsAsync(IEnumerable<string> stepIds, CancellationToken cancellationToken = default);
    }
}
