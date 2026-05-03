using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Tracks torrent state transitions and shows browser notifications based on user preferences.
    /// </summary>
    public sealed class TorrentCompletionNotificationService : ITorrentCompletionNotificationService
    {
        private const string _appNotificationsContext = "AppNotifications";

        private readonly IBrowserNotificationService _browserNotificationService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILanguageLocalizer _languageLocalizer;
        private Dictionary<string, TorrentSnapshotState> _knownStates = new Dictionary<string, TorrentSnapshotState>(StringComparer.Ordinal);
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="TorrentCompletionNotificationService"/> class.
        /// </summary>
        /// <param name="browserNotificationService">The browser notification service.</param>
        /// <param name="appSettingsService">The app settings service.</param>
        /// <param name="languageLocalizer">The language localizer.</param>
        public TorrentCompletionNotificationService(
            IBrowserNotificationService browserNotificationService,
            IAppSettingsService appSettingsService,
            ILanguageLocalizer languageLocalizer)
        {
            _browserNotificationService = browserNotificationService;
            _appSettingsService = appSettingsService;
            _languageLocalizer = languageLocalizer;
        }

        /// <inheritdoc />
        public Task InitializeAsync(IReadOnlyDictionary<string, Torrent> torrents, CancellationToken cancellationToken = default)
        {
            _knownStates = BuildSnapshot(torrents);
            _initialized = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ProcessAsync(IReadOnlyDictionary<string, Torrent> torrents, CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                await InitializeAsync(torrents, cancellationToken);
                return;
            }

            var currentSnapshot = BuildSnapshot(torrents);
            var appSettings = await _appSettingsService.GetSettingsAsync(cancellationToken);

            if (appSettings.NotificationsEnabled && await CanShowNotifications(cancellationToken))
            {
                foreach (var (hash, torrent) in torrents)
                {
                    var displayName = string.IsNullOrWhiteSpace(torrent.Name) ? torrent.Hash : torrent.Name;
                    if (!_knownStates.TryGetValue(hash, out var previousState))
                    {
                        if (appSettings.TorrentAddedNotificationsEnabled)
                        {
                            await ShowAddedNotificationAsync(displayName, cancellationToken);
                        }

                        continue;
                    }

                    if (!currentSnapshot.TryGetValue(hash, out var currentState))
                    {
                        continue;
                    }

                    if (!appSettings.DownloadFinishedNotificationsEnabled || !ShouldShowFinishedNotification(previousState, currentState))
                    {
                        continue;
                    }

                    await ShowFinishedNotificationAsync(displayName, cancellationToken);
                }
            }

            _knownStates = currentSnapshot;
        }

        /// <inheritdoc />
        public async Task ProcessTransitionsAsync(IReadOnlyList<TorrentTransition> transitions, CancellationToken cancellationToken = default)
        {
            if (transitions.Count == 0)
            {
                return;
            }

            var appSettings = await _appSettingsService.GetSettingsAsync(cancellationToken);
            if (!appSettings.NotificationsEnabled || !await CanShowNotifications(cancellationToken))
            {
                return;
            }

            foreach (var transition in transitions)
            {
                if (transition.IsAdded)
                {
                    if (appSettings.TorrentAddedNotificationsEnabled)
                    {
                        await ShowAddedNotificationAsync(transition.Name, cancellationToken);
                    }

                    continue;
                }

                if (!appSettings.DownloadFinishedNotificationsEnabled || !ShouldShowFinishedNotification(transition))
                {
                    continue;
                }

                await ShowFinishedNotificationAsync(transition.Name, cancellationToken);
            }
        }

        private async Task<bool> CanShowNotifications(CancellationToken cancellationToken)
        {
            if (!await _browserNotificationService.IsSupportedAsync(cancellationToken))
            {
                return false;
            }

            var permission = await _browserNotificationService.GetPermissionAsync(cancellationToken);
            return permission == BrowserNotificationPermission.Granted;
        }

        private async Task ShowFinishedNotificationAsync(string displayName, CancellationToken cancellationToken)
        {
            var title = Translate("Download completed");
            var body = Translate("'%1' has finished downloading.", displayName);
            await TryShowNotificationAsync(title, body, cancellationToken);
        }

        private async Task ShowAddedNotificationAsync(string displayName, CancellationToken cancellationToken)
        {
            var title = Translate("Torrent added");
            var body = Translate("'%1' was added.", displayName);
            await TryShowNotificationAsync(title, body, cancellationToken);
        }

        private static Dictionary<string, TorrentSnapshotState> BuildSnapshot(IReadOnlyDictionary<string, Torrent> torrents)
        {
            var snapshot = new Dictionary<string, TorrentSnapshotState>(StringComparer.Ordinal);
            foreach (var (hash, torrent) in torrents)
            {
                snapshot[hash] = new TorrentSnapshotState(IsFinished(torrent));
            }

            return snapshot;
        }

        private static bool ShouldShowFinishedNotification(TorrentSnapshotState previousState, TorrentSnapshotState currentState)
        {
            return !previousState.IsFinished && currentState.IsFinished;
        }

        private static bool ShouldShowFinishedNotification(TorrentTransition transition)
        {
            return !transition.PreviousIsFinished && transition.CurrentIsFinished;
        }

        private static bool IsFinished(Torrent torrent)
        {
            return torrent.IsFinished();
        }

        private string Translate(string source, params object[] arguments)
        {
            return _languageLocalizer.Translate(_appNotificationsContext, source, arguments);
        }

        private async Task TryShowNotificationAsync(string title, string body, CancellationToken cancellationToken)
        {
            await _browserNotificationService.ShowNotificationAsync(title, body, cancellationToken);
        }

        private readonly struct TorrentSnapshotState
        {
            public TorrentSnapshotState(bool isFinished)
            {
                IsFinished = isFinished;
            }

            public bool IsFinished { get; }
        }
    }
}
