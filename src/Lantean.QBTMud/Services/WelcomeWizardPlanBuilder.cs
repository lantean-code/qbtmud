using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IWelcomeWizardPlanBuilder"/>.
    /// </summary>
    public sealed class WelcomeWizardPlanBuilder : IWelcomeWizardPlanBuilder
    {
        private readonly IWelcomeWizardStateService _welcomeWizardStateService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeWizardPlanBuilder"/> class.
        /// </summary>
        /// <param name="welcomeWizardStateService">The wizard state service.</param>
        public WelcomeWizardPlanBuilder(IWelcomeWizardStateService welcomeWizardStateService)
        {
            _welcomeWizardStateService = welcomeWizardStateService;
        }

        /// <inheritdoc />
        public async Task<WelcomeWizardPlan> BuildPlanAsync(CancellationToken cancellationToken = default)
        {
            var state = await _welcomeWizardStateService.GetStateAsync(cancellationToken);
            var acknowledgedIds = state.AcknowledgedStepIds;

            var pendingSteps = WelcomeWizardStepCatalog.Steps
                .Where(step => !acknowledgedIds.Contains(step.Id))
                .OrderBy(step => step.Order)
                .ToList();

            var isReturningUser = acknowledgedIds.Count > 0;
            return new WelcomeWizardPlan(isReturningUser, pendingSteps);
        }
    }
}
