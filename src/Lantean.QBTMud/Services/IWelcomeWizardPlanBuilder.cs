using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Builds the welcome wizard plan for the current user.
    /// </summary>
    public interface IWelcomeWizardPlanBuilder
    {
        /// <summary>
        /// Builds the current wizard plan.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The computed wizard plan.</returns>
        Task<WelcomeWizardPlan> BuildPlanAsync(CancellationToken cancellationToken = default);
    }
}
