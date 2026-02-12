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
        private readonly IWebUiLocalizer _webUiLocalizer;
        private readonly IWebUiLanguageCatalog _webUiLanguageCatalog;
        private readonly IThemeManagerService _themeManagerService;
        private readonly ILogger<AppWarmupService> _logger;
        private readonly AppWarmupService _target;

        public AppWarmupServiceTests()
        {
            _webUiLocalizer = Mock.Of<IWebUiLocalizer>();
            _webUiLanguageCatalog = Mock.Of<IWebUiLanguageCatalog>();
            _themeManagerService = Mock.Of<IThemeManagerService>();
            _logger = Mock.Of<ILogger<AppWarmupService>>();

            Mock.Get(_webUiLocalizer)
                .Setup(localizer => localizer.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            Mock.Get(_webUiLanguageCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Mock.Get(_themeManagerService)
                .Setup(service => service.EnsureInitialized())
                .Returns(Task.CompletedTask);

            _target = new AppWarmupService(_webUiLocalizer, _webUiLanguageCatalog, _themeManagerService, _logger);
        }

        [Fact]
        public async Task GIVEN_WarmupNotRun_WHEN_Invoked_THEN_CompletesAndCallsDependencies()
        {
            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            _target.IsCompleted.Should().BeTrue();
            _target.Failures.Should().BeEmpty();

            Mock.Get(_webUiLocalizer).Verify(localizer => localizer.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_webUiLanguageCatalog).Verify(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_WarmupCompleted_WHEN_InvokedAgain_THEN_DoesNotRunTwice()
        {
            await _target.WarmupAsync(TestContext.Current.CancellationToken);
            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            Mock.Get(_webUiLocalizer).Verify(localizer => localizer.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_webUiLanguageCatalog).Verify(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ConcurrentWarmup_WHEN_InvokedTwice_THEN_RunsOnce()
        {
            var gate = new TaskCompletionSource<bool>();
            var callCount = 0;
            Mock.Get(_webUiLocalizer)
                .Setup(localizer => localizer.InitializeAsync(It.IsAny<CancellationToken>()))
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

            Mock.Get(_webUiLocalizer).Verify(localizer => localizer.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LocalizerThrows_WHEN_WarmupInvoked_THEN_RecordsFailureAndContinues()
        {
            Mock.Get(_webUiLocalizer)
                .Setup(localizer => localizer.InitializeAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            await _target.WarmupAsync(TestContext.Current.CancellationToken);

            _target.IsCompleted.Should().BeTrue();
            _target.Failures.Should().ContainSingle(failure => failure.Step == AppWarmupStep.WebUiLocalizer && failure.Message == "Failure");

            Mock.Get(_webUiLanguageCatalog).Verify(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }
    }
}
