namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents the computed welcome wizard plan for the current user.
    /// </summary>
    public sealed class WelcomeWizardPlan
    {
        /// <summary>
        /// Gets an empty plan.
        /// </summary>
        public static WelcomeWizardPlan Empty
        {
            get
            {
                return new WelcomeWizardPlan(false, Array.Empty<WelcomeWizardStepDefinition>());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeWizardPlan"/> class.
        /// </summary>
        /// <param name="isReturningUser">A value indicating whether the user has previously acknowledged any wizard steps.</param>
        /// <param name="pendingSteps">The pending wizard steps in display order.</param>
        public WelcomeWizardPlan(bool isReturningUser, IReadOnlyList<WelcomeWizardStepDefinition> pendingSteps)
        {
            IsReturningUser = isReturningUser;
            PendingSteps = pendingSteps ?? Array.Empty<WelcomeWizardStepDefinition>();
        }

        /// <summary>
        /// Gets a value indicating whether the user has previously acknowledged wizard steps.
        /// </summary>
        public bool IsReturningUser { get; }

        /// <summary>
        /// Gets the pending wizard steps in display order.
        /// </summary>
        public IReadOnlyList<WelcomeWizardStepDefinition> PendingSteps { get; }

        /// <summary>
        /// Gets a value indicating whether the wizard should be shown.
        /// </summary>
        public bool ShouldShowWizard
        {
            get
            {
                return PendingSteps.Count > 0;
            }
        }
    }
}
