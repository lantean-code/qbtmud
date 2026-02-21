namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Stores persisted welcome wizard progress.
    /// </summary>
    public sealed class WelcomeWizardState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeWizardState"/> class.
        /// </summary>
        public WelcomeWizardState()
        {
            AcknowledgedStepIds = new HashSet<string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets or sets the acknowledged step identifiers.
        /// </summary>
        public HashSet<string> AcknowledgedStepIds { get; set; }

        /// <summary>
        /// Gets or sets the last time the wizard was shown.
        /// </summary>
        public DateTime? LastShownUtc { get; set; }

        /// <summary>
        /// Gets or sets the last time wizard progress was acknowledged.
        /// </summary>
        public DateTime? LastCompletedUtc { get; set; }

        /// <summary>
        /// Creates a deep copy of the current state.
        /// </summary>
        /// <returns>A copied instance.</returns>
        public WelcomeWizardState Clone()
        {
            return new WelcomeWizardState
            {
                AcknowledgedStepIds = new HashSet<string>(AcknowledgedStepIds, StringComparer.Ordinal),
                LastShownUtc = LastShownUtc,
                LastCompletedUtc = LastCompletedUtc
            };
        }
    }
}
