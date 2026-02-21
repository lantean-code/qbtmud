using Lantean.QBTMud.Models;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides persistence and migration helpers for welcome wizard progress state.
    /// </summary>
    public sealed class WelcomeWizardStateService : IWelcomeWizardStateService
    {
        private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
        private readonly ILocalStorageService _localStorageService;
        private WelcomeWizardState? _cachedState;

        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeWizardStateService"/> class.
        /// </summary>
        /// <param name="localStorageService">The local storage service.</param>
        public WelcomeWizardStateService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        /// <inheritdoc />
        public async Task<WelcomeWizardState> GetStateAsync(CancellationToken cancellationToken = default)
        {
            if (_cachedState is not null)
            {
                return _cachedState.Clone();
            }

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_cachedState is not null)
                {
                    return _cachedState.Clone();
                }

                WelcomeWizardState? loadedState = null;
                try
                {
                    loadedState = await _localStorageService.GetItemAsync<WelcomeWizardState>(WelcomeWizardStorageKeys.State, cancellationToken);
                }
                catch (JsonException)
                {
                    loadedState = null;
                }

                if (loadedState is not null)
                {
                    _cachedState = Normalize(loadedState);
                    return _cachedState.Clone();
                }

                var legacyCompleted = await _localStorageService.GetItemAsync<bool?>(WelcomeWizardStorageKeys.Completed, cancellationToken);
                var migratedState = BuildMigratedState(legacyCompleted.GetValueOrDefault());
                _cachedState = Normalize(migratedState);
                await _localStorageService.SetItemAsync(WelcomeWizardStorageKeys.State, _cachedState, cancellationToken);

                return _cachedState.Clone();
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<WelcomeWizardState> SaveStateAsync(WelcomeWizardState state, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);

            var normalized = Normalize(state);
            _cachedState = normalized.Clone();

            await _localStorageService.SetItemAsync(WelcomeWizardStorageKeys.State, normalized, cancellationToken);
            return _cachedState.Clone();
        }

        /// <inheritdoc />
        public async Task<WelcomeWizardState> MarkShownAsync(CancellationToken cancellationToken = default)
        {
            var state = await GetStateAsync(cancellationToken);
            state.LastShownUtc = DateTime.UtcNow;
            return await SaveStateAsync(state, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<WelcomeWizardState> AcknowledgeStepsAsync(IEnumerable<string> stepIds, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stepIds);

            var state = await GetStateAsync(cancellationToken);
            foreach (var stepId in stepIds)
            {
                var normalizedStepId = NormalizeStepId(stepId);
                if (normalizedStepId is null)
                {
                    continue;
                }

                state.AcknowledgedStepIds.Add(normalizedStepId);
            }

            state.LastCompletedUtc = DateTime.UtcNow;
            return await SaveStateAsync(state, cancellationToken);
        }

        private static WelcomeWizardState BuildMigratedState(bool legacyCompleted)
        {
            var state = new WelcomeWizardState();
            if (!legacyCompleted)
            {
                return state;
            }

            foreach (var stepId in WelcomeWizardStepCatalog.LegacyAcknowledgedStepIds)
            {
                state.AcknowledgedStepIds.Add(stepId);
            }

            state.LastCompletedUtc = DateTime.UtcNow;
            return state;
        }

        private static WelcomeWizardState Normalize(WelcomeWizardState state)
        {
            var normalizedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stepId in state.AcknowledgedStepIds)
            {
                var normalizedStepId = NormalizeStepId(stepId);
                if (normalizedStepId is null)
                {
                    continue;
                }

                normalizedIds.Add(normalizedStepId);
            }

            return new WelcomeWizardState
            {
                AcknowledgedStepIds = normalizedIds,
                LastShownUtc = state.LastShownUtc,
                LastCompletedUtc = state.LastCompletedUtc
            };
        }

        private static string? NormalizeStepId(string? stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                return null;
            }

            return stepId.Trim();
        }
    }
}
