using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class AppWarmupServiceTests
    {
        private readonly ILanguageInitializationService _languageInitializationService;
        private readonly ILanguageCatalog _languageCatalog;
        private readonly IThemeManagerService _themeManagerService;
        private readonly ILogger<AppWarmupService> _logger;
        private readonly AppWarmupService _target;

        public AppWarmupServiceTests()
        {
            _languageInitializationService = Mock.Of<ILanguageInitializationService>();
            _languageCatalog = Mock.Of<ILanguageCatalog>();
            _themeManagerService = Mock.Of<IThemeManagerService>();
            _logger = Mock.Of<ILogger<AppWarmupService>>();

            Mock.Get(_languageInitializationService)
                .Setup(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            Mock.Get(_languageCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Mock.Get(_themeManagerService)
                .Setup(service => service.EnsureInitialized())
                .Returns(Task.CompletedTask);

            _target = new AppWarmupService(_languageInitializationService, _languageCatalog, _themeManagerService, _logger);
        }

        [Fact]
        public async Task GIVEN_WarmupNotRun_WHEN_Invoked_THEN_CompletesAndCallsDependencies()
        {
            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            _target.IsCompleted.Should().BeTrue();
            _target.Failures.Should().BeEmpty();

            Mock.Get(_languageInitializationService).Verify(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_languageCatalog).Verify(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_WarmupCompleted_WHEN_InvokedAgain_THEN_DoesNotRunTwice()
        {
            await _target.WarmupAsync(TestContext.Current.CancellationToken);
            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            Mock.Get(_languageInitializationService).Verify(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_languageCatalog).Verify(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ConcurrentWarmup_WHEN_InvokedTwice_THEN_RunsOnce()
        {
            var gate = new TaskCompletionSource<bool>();
            var callCount = 0;
            Mock.Get(_languageInitializationService)
                .Setup(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Interlocked.Increment(ref callCount);
                    return new ValueTask(gate.Task);
                });

            var cancellationToken = TestContext.Current.CancellationToken;
            var first = _target.WarmupAsync(cancellationToken);
            var second = _target.WarmupAsync(cancellationToken);

            callCount.Should().Be(1);

            gate.SetResult(true);
            await Task.WhenAll(first, second);

            Mock.Get(_languageInitializationService).Verify(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageInitializationThrows_WHEN_WarmupInvoked_THEN_RecordsFailureAndContinues()
        {
            Mock.Get(_languageInitializationService)
                .Setup(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            _target.IsCompleted.Should().BeTrue();
            _target.Failures.Should().ContainSingle(failure => failure.Step == AppWarmupStep.LanguageLocalizer && failure.Message == "Failure");

            Mock.Get(_languageCatalog).Verify(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageInitializationCanceledWithRequestedToken_WHEN_WarmupInvoked_THEN_ShouldPropagateOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            Mock.Get(_languageInitializationService)
                .Setup(service => service.EnsureLanguageResourcesInitialized(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(_ =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException(cancellationTokenSource.Token);
                });

            Func<Task> action = async () =>
            {
                await _target.WarmupAsync(cancellationTokenSource.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_LanguageCatalogInitCanceledWithRequestedToken_WHEN_WarmupInvoked_THEN_ShouldPropagateOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            Mock.Get(_languageCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(_ =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException(cancellationTokenSource.Token);
                });

            Func<Task> action = async () =>
            {
                await _target.WarmupAsync(cancellationTokenSource.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_LanguageCatalogInitThrows_WHEN_WarmupInvoked_THEN_ShouldRecordFailureAndContinue()
        {
            Mock.Get(_languageCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("CatalogFailure"));

            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            _target.IsCompleted.Should().BeTrue();
            _target.Failures.Should().ContainSingle(failure => failure.Step == AppWarmupStep.LanguageCatalog && failure.Message == "CatalogFailure");
            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeManagerInitThrows_WHEN_WarmupInvoked_THEN_ShouldRecordFailure()
        {
            Mock.Get(_themeManagerService)
                .Setup(service => service.EnsureInitialized())
                .ThrowsAsync(new InvalidOperationException("ThemeFailure"));

            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            _target.IsCompleted.Should().BeTrue();
            _target.Failures.Should().ContainSingle(failure => failure.Step == AppWarmupStep.ThemeManager && failure.Message == "ThemeFailure");
        }
    }
}
