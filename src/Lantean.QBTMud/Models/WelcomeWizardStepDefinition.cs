namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Describes a welcome wizard step definition.
    /// </summary>
    public sealed record WelcomeWizardStepDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeWizardStepDefinition"/> class.
        /// </summary>
        /// <param name="id">The stable step identifier.</param>
        /// <param name="order">The step order in the wizard flow.</param>
        public WelcomeWizardStepDefinition(string id, int order)
        {
            Id = id;
            Order = order;
        }

        /// <summary>
        /// Gets the stable step identifier.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// Gets the step order in the wizard flow.
        /// </summary>
        public int Order { get; init; }
    }
}
