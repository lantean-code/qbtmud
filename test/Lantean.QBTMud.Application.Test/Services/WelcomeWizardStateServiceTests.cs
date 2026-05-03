using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Core.Models;
using Moq;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class WelcomeWizardStateServiceTests
    {
        private readonly TestLocalStorageService _settingsStorageService;
        private readonly WelcomeWizardStateService _target;

        public WelcomeWizardStateServiceTests()
        {
            _settingsStorageService = new TestLocalStorageService();
            _target = new WelcomeWizardStateService(_settingsStorageService);
        }

        [Fact]
        public async Task GIVEN_NoPersistedStateOrLegacyCompletion_WHEN_GetStateInvoked_THEN_ReturnsEmptyStateAndPersistsVersionedKey()
        {
            var state = await _target.GetStateAsync(TestContext.Current.CancellationToken);
            var persisted = await _settingsStorageService.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, TestContext.Current.CancellationToken);

            state.AcknowledgedStepIds.Should().BeEmpty();
            state.LastShownUtc.Should().BeNull();
            state.LastCompletedUtc.Should().BeNull();
            persisted.Should().NotBeNull();
            persisted!.AcknowledgedStepIds.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_LegacyCompletedUserWithoutVersionedState_WHEN_GetStateInvoked_THEN_MigratesLegacyStepIdsOnly()
        {
            await _settingsStorageService.SetItemAsync(WelcomeWizardStorageKeys.Completed, true, TestContext.Current.CancellationToken);

            var state = await _target.GetStateAsync(TestContext.Current.CancellationToken);

            state.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.LanguageStepId);
            state.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.ThemeStepId);
            state.AcknowledgedStepIds.Should().NotContain(WelcomeWizardStepCatalog.NotificationsStepId);
            state.LastCompletedUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_AcknowledgeStepsWithDuplicatesAndWhitespace_WHEN_Invoked_THEN_PersistsDistinctNormalizedStepIds()
        {
            var state = await _target.AcknowledgeStepsAsync(
            [
                $" {WelcomeWizardStepCatalog.NotificationsStepId} ",
                WelcomeWizardStepCatalog.NotificationsStepId,
                string.Empty
            ],
            TestContext.Current.CancellationToken);

            state.AcknowledgedStepIds.Should().ContainSingle();
            state.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.NotificationsStepId);
            state.LastCompletedUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_MarkShownInvoked_WHEN_Completed_THEN_UpdatesLastShownUtc()
        {
            var state = await _target.MarkShownAsync(TestContext.Current.CancellationToken);
            var persisted = await _settingsStorageService.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, TestContext.Current.CancellationToken);

            state.LastShownUtc.Should().NotBeNull();
            persisted.Should().NotBeNull();
            persisted!.LastShownUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_VersionedStateExists_WHEN_GetStateInvoked_THEN_DoesNotUseLegacyCompletionKey()
        {
            var existing = new WelcomeWizardState
            {
                AcknowledgedStepIds = new HashSet<string>(StringComparer.Ordinal)
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                }
            };

            await _settingsStorageService.SetItemAsync(WelcomeWizardStorageKeys.Completed, true, TestContext.Current.CancellationToken);
            await _settingsStorageService.SetItemAsync(WelcomeWizardStorageKeys.State, existing, TestContext.Current.CancellationToken);

            var state = await _target.GetStateAsync(TestContext.Current.CancellationToken);

            state.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.NotificationsStepId);
            state.AcknowledgedStepIds.Should().NotContain(WelcomeWizardStepCatalog.LanguageStepId);
            state.AcknowledgedStepIds.Should().NotContain(WelcomeWizardStepCatalog.ThemeStepId);
        }

        [Fact]
        public async Task GIVEN_VersionedStateContainsInvalidStepIds_WHEN_GetStateInvoked_THEN_FiltersAndNormalizesIds()
        {
            var existing = new WelcomeWizardState
            {
                AcknowledgedStepIds = new HashSet<string>(StringComparer.Ordinal)
                {
                    $" {WelcomeWizardStepCatalog.NotificationsStepId} ",
                    " ",
                    string.Empty
                }
            };

            await _settingsStorageService.SetItemAsync(WelcomeWizardStorageKeys.State, existing, TestContext.Current.CancellationToken);

            var state = await _target.GetStateAsync(TestContext.Current.CancellationToken);

            state.AcknowledgedStepIds.Should().ContainSingle();
            state.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.NotificationsStepId);
        }

        [Fact]
        public async Task GIVEN_StateReadThrowsJsonException_WHEN_GetStateInvoked_THEN_UsesLegacyMigrationFallback()
        {
            var settingsStorageService = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            settingsStorageService
                .Setup(service => service.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("invalid"));
            settingsStorageService
                .Setup(service => service.GetItemAsync<bool?>(WelcomeWizardStorageKeys.Completed, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            settingsStorageService
                .Setup(service => service.SetItemAsync(WelcomeWizardStorageKeys.State, It.IsAny<WelcomeWizardState>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var target = new WelcomeWizardStateService(settingsStorageService.Object);

            var state = await target.GetStateAsync(TestContext.Current.CancellationToken);

            state.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.LanguageStepId);
            state.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.ThemeStepId);
            state.LastCompletedUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_CachedState_WHEN_GetStateInvokedAgain_THEN_ReadsStorageOnce()
        {
            var settingsStorageService = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            settingsStorageService
                .Setup(service => service.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WelcomeWizardState());

            var target = new WelcomeWizardStateService(settingsStorageService.Object);

            _ = await target.GetStateAsync(TestContext.Current.CancellationToken);
            _ = await target.GetStateAsync(TestContext.Current.CancellationToken);

            settingsStorageService.Verify(
                service => service.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ConcurrentInitialStateReads_WHEN_FirstReadInitializesCache_THEN_SecondReadReturnsFromSemaphoreCache()
        {
            var settingsStorageService = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRead = new TaskCompletionSource<WelcomeWizardState>(TaskCreationOptions.RunContinuationsAsynchronously);

            settingsStorageService
                .Setup(service => service.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    readStarted.TrySetResult();
                    return await releaseRead.Task;
                });
            settingsStorageService
                .Setup(service => service.GetItemAsync<bool?>(WelcomeWizardStorageKeys.Completed, It.IsAny<CancellationToken>()))
                .ReturnsAsync((bool?)false);
            settingsStorageService
                .Setup(service => service.SetItemAsync(WelcomeWizardStorageKeys.State, It.IsAny<WelcomeWizardState>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var target = new WelcomeWizardStateService(settingsStorageService.Object);
            var firstReadTask = target.GetStateAsync(TestContext.Current.CancellationToken);
            await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            var secondReadTask = target.GetStateAsync(TestContext.Current.CancellationToken);

            releaseRead.TrySetResult(new WelcomeWizardState
            {
                AcknowledgedStepIds = new HashSet<string>(StringComparer.Ordinal)
                {
                    WelcomeWizardStepCatalog.NotificationsStepId
                }
            });

            var first = await firstReadTask;
            var second = await secondReadTask;

            first.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.NotificationsStepId);
            second.AcknowledgedStepIds.Should().Contain(WelcomeWizardStepCatalog.NotificationsStepId);
            settingsStorageService.Verify(
                service => service.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
