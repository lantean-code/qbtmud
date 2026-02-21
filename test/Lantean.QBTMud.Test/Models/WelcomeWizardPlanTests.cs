using AwesomeAssertions;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class WelcomeWizardPlanTests
    {
        [Fact]
        public void GIVEN_EmptyPlan_WHEN_Accessed_THEN_ReturnsNoPendingStepsAndDoesNotShowWizard()
        {
            var result = WelcomeWizardPlan.Empty;

            result.IsReturningUser.Should().BeFalse();
            result.PendingSteps.Should().BeEmpty();
            result.ShouldShowWizard.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NullPendingSteps_WHEN_Constructed_THEN_UsesEmptyPendingStepsAndDoesNotShowWizard()
        {
            var result = new WelcomeWizardPlan(isReturningUser: true, pendingSteps: null!);

            result.IsReturningUser.Should().BeTrue();
            result.PendingSteps.Should().BeEmpty();
            result.ShouldShowWizard.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_PendingSteps_WHEN_Constructed_THEN_ShowsWizard()
        {
            var pendingSteps = new List<WelcomeWizardStepDefinition>
            {
                new WelcomeWizardStepDefinition("notifications", 3)
            };

            var result = new WelcomeWizardPlan(isReturningUser: true, pendingSteps);

            result.IsReturningUser.Should().BeTrue();
            result.PendingSteps.Should().BeEquivalentTo(pendingSteps);
            result.ShouldShowWizard.Should().BeTrue();
        }
    }
}
