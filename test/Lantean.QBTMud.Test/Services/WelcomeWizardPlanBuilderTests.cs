using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class WelcomeWizardPlanBuilderTests
    {
        private readonly IWelcomeWizardStateService _welcomeWizardStateService;
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly WelcomeWizardPlanBuilder _target;

        public WelcomeWizardPlanBuilderTests()
        {
            _welcomeWizardStateService = Mock.Of<IWelcomeWizardStateService>();
            _webApiCapabilityService = Mock.Of<IWebApiCapabilityService>();
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            _target = new WelcomeWizardPlanBuilder(_welcomeWizardStateService, _webApiCapabilityService);
        }

        [Fact]
        public async Task GIVEN_NewUserWithoutAcknowledgedSteps_WHEN_BuildPlanInvoked_THEN_ReturnsAllStepsInCatalogOrder()
        {
            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.GetStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardState());

            var plan = await _target.BuildPlanAsync(TestContext.Current.CancellationToken);

            plan.IsReturningUser.Should().BeFalse();
            plan.ShouldShowWizard.Should().BeTrue();
            plan.PendingSteps.Select(step => step.Id).Should().ContainInOrder(WelcomeWizardStepCatalog.GetSteps(includeStorageStep: true).Select(step => step.Id));
        }

        [Fact]
        public async Task GIVEN_PartiallyAcknowledgedUser_WHEN_BuildPlanInvoked_THEN_ReturnsOnlyUnseenSteps()
        {
            var state = new WelcomeWizardState
            {
                AcknowledgedStepIds = new HashSet<string>(StringComparer.Ordinal)
                {
                    WelcomeWizardStepCatalog.LanguageStepId,
                    WelcomeWizardStepCatalog.ThemeStepId
                }
            };

            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.GetStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(state);

            var plan = await _target.BuildPlanAsync(TestContext.Current.CancellationToken);

            plan.IsReturningUser.Should().BeTrue();
            plan.PendingSteps.Should().HaveCount(2);
            plan.PendingSteps[0].Id.Should().Be(WelcomeWizardStepCatalog.NotificationsStepId);
            plan.PendingSteps[1].Id.Should().Be(WelcomeWizardStepCatalog.StorageStepId);
        }

        [Fact]
        public async Task GIVEN_AllCatalogStepsAcknowledged_WHEN_BuildPlanInvoked_THEN_ReturnsEmptyPlan()
        {
            var state = new WelcomeWizardState
            {
                AcknowledgedStepIds = new HashSet<string>(WelcomeWizardStepCatalog.KnownStepIds, StringComparer.Ordinal)
            };

            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.GetStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(state);

            var plan = await _target.BuildPlanAsync(TestContext.Current.CancellationToken);

            plan.IsReturningUser.Should().BeTrue();
            plan.PendingSteps.Should().BeEmpty();
            plan.ShouldShowWizard.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_UserWithUnknownAcknowledgedStep_WHEN_BuildPlanInvoked_THEN_StillReturnsKnownPendingSteps()
        {
            var state = new WelcomeWizardState
            {
                AcknowledgedStepIds = new HashSet<string>(StringComparer.Ordinal)
                {
                    "welcome.unknown.v1"
                }
            };

            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.GetStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(state);

            var plan = await _target.BuildPlanAsync(TestContext.Current.CancellationToken);

            plan.IsReturningUser.Should().BeTrue();
            plan.PendingSteps.Select(step => step.Id).Should().ContainInOrder(WelcomeWizardStepCatalog.GetSteps(includeStorageStep: true).Select(step => step.Id));
        }

        [Fact]
        public async Task GIVEN_ClientDataUnsupported_WHEN_BuildPlanInvoked_THEN_ShouldExcludeStorageStep()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));
            Mock.Get(_welcomeWizardStateService)
                .Setup(service => service.GetStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardState());

            var plan = await _target.BuildPlanAsync(TestContext.Current.CancellationToken);

            plan.PendingSteps.Select(step => step.Id).Should().ContainInOrder(WelcomeWizardStepCatalog.GetSteps(includeStorageStep: false).Select(step => step.Id));
            plan.PendingSteps.Select(step => step.Id).Should().NotContain(WelcomeWizardStepCatalog.StorageStepId);
        }
    }
}
