using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using System.Diagnostics;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Coordinates application warmup tasks and captures any failures.
    /// </summary>
    public sealed class AppWarmupService : IAppWarmupService
    {
        private readonly SemaphoreSlim _warmupLock = new SemaphoreSlim(1, 1);
        private readonly IWebUiLocalizer _webUiLocalizer;
        private readonly IWebUiLanguageCatalog _webUiLanguageCatalog;
        private readonly IThemeManagerService _themeManagerService;
        private readonly ILogger<AppWarmupService> _logger;
        private List<AppWarmupFailure> _failures = [];
        private bool _completed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppWarmupService"/> class.
        /// </summary>
        /// <param name="webUiLocalizer">The WebUI localizer.</param>
        /// <param name="webUiLanguageCatalog">The WebUI language catalog.</param>
        /// <param name="themeManagerService">The theme manager service.</param>
        /// <param name="logger">The logger instance.</param>
        public AppWarmupService(
            IWebUiLocalizer webUiLocalizer,
            IWebUiLanguageCatalog webUiLanguageCatalog,
            IThemeManagerService themeManagerService,
            ILogger<AppWarmupService> logger)
        {
            _webUiLocalizer = webUiLocalizer;
            _webUiLanguageCatalog = webUiLanguageCatalog;
            _themeManagerService = themeManagerService;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool IsCompleted
        {
            get { return _completed; }
        }

        /// <inheritdoc />
        public IReadOnlyList<AppWarmupFailure> Failures
        {
            get { return _failures; }
        }

        /// <inheritdoc />
        public async Task WarmupAsync(CancellationToken cancellationToken = default)
        {
            if (_completed)
            {
                return;
            }

            await _warmupLock.WaitAsync(cancellationToken);
            try
            {
                if (_completed)
                {
                    return;
                }

                _failures = [];

                await RunStep(
                    AppWarmupStep.WebUiLocalizer,
                    () => _webUiLocalizer.InitializeAsync(cancellationToken),
                    cancellationToken);

                await RunStep(
                    AppWarmupStep.WebUiLanguageCatalog,
                    () => _webUiLanguageCatalog.EnsureInitialized(cancellationToken),
                    cancellationToken);

                await RunStep(
                    AppWarmupStep.ThemeManager,
                    () => _themeManagerService.EnsureInitialized(),
                    cancellationToken);

                _completed = true;
            }
            finally
            {
                _warmupLock.Release();
            }
        }

        private async Task RunStep(AppWarmupStep step, Func<ValueTask> action, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await action();
                _logger.LogDebug("Warmup step {Step} completed in {ElapsedMilliseconds}ms.", step, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _failures.Add(new AppWarmupFailure(step, ex.Message));
                _logger.LogWarning(ex, "Warmup step {Step} failed after {ElapsedMilliseconds}ms.", step, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task RunStep(AppWarmupStep step, Func<Task> action, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await action();
                _logger.LogDebug("Warmup step {Step} completed in {ElapsedMilliseconds}ms.", step, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _failures.Add(new AppWarmupFailure(step, ex.Message));
                _logger.LogWarning(ex, "Warmup step {Step} failed after {ElapsedMilliseconds}ms.", step, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
