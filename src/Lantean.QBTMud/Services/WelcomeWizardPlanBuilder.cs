using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IWelcomeWizardPlanBuilder"/>.
    /// </summary>
    public sealed class WelcomeWizardPlanBuilder : IWelcomeWizardPlanBuilder
    {
        private readonly IWelcomeWizardStateService _welcomeWizardStateService;
        private readonly IWebApiCapabilityService _webApiCapabilityService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeWizardPlanBuilder"/> class.
        /// </summary>
        /// <param name="welcomeWizardStateService">The wizard state service.</param>
        /// <param name="webApiCapabilityService">The Web API capability service.</param>
        public WelcomeWizardPlanBuilder(IWelcomeWizardStateService welcomeWizardStateService, IWebApiCapabilityService webApiCapabilityService)
        {
            _welcomeWizardStateService = welcomeWizardStateService;
            _webApiCapabilityService = webApiCapabilityService;
        }

        /// <inheritdoc />
        public async Task<WelcomeWizardPlan> BuildPlanAsync(CancellationToken cancellationToken = default)
        {
            var state = await _welcomeWizardStateService.GetStateAsync(cancellationToken);
            var capabilityState = await _webApiCapabilityService.GetCapabilityStateAsync(cancellationToken);
            var acknowledgedIds = state.AcknowledgedStepIds;

            var pendingSteps = WelcomeWizardStepCatalog
                .GetSteps(includeStorageStep: capabilityState.SupportsClientData)
                .Where(step => !acknowledgedIds.Contains(step.Id))
                .OrderBy(step => step.Order)
                .ToList();

            var isReturningUser = acknowledgedIds.Count > 0;
            return new WelcomeWizardPlan(isReturningUser, pendingSteps);
        }
    }
}
