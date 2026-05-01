using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IPendingDownloadWorkflow"/>.
    /// </summary>
    public sealed class PendingDownloadWorkflow : IPendingDownloadWorkflow
    {
        private const string _pendingDownloadStorageKey = "LoggedInLayout.PendingDownload";
        private const string _lastProcessedDownloadStorageKey = "LoggedInLayout.LastProcessedDownload";

        private readonly ISessionStorageService _sessionStorageService;
        private readonly IMagnetLinkService _magnetLinkService;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly NavigationManager _navigationManager;
        private string? _lastProcessedDownloadToken;
        private string? _pendingDownloadLink;

        /// <summary>
        /// Initializes a new instance of the <see cref="PendingDownloadWorkflow"/> class.
        /// </summary>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="magnetLinkService">The magnet link service.</param>
        /// <param name="dialogWorkflow">The dialog workflow.</param>
        /// <param name="navigationManager">The navigation manager.</param>
        public PendingDownloadWorkflow(
            ISessionStorageService sessionStorageService,
            IMagnetLinkService magnetLinkService,
            IDialogWorkflow dialogWorkflow,
            NavigationManager navigationManager)
        {
            _sessionStorageService = sessionStorageService;
            _magnetLinkService = magnetLinkService;
            _dialogWorkflow = dialogWorkflow;
            _navigationManager = navigationManager;
        }

        /// <inheritdoc />
        public async Task RestoreAsync(CancellationToken cancellationToken = default)
        {
            if (_pendingDownloadLink is null)
            {
                var pendingDownload = await _sessionStorageService.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
                if (_magnetLinkService.IsSupportedDownloadLink(pendingDownload))
                {
                    _pendingDownloadLink = pendingDownload;
                }
                else if (pendingDownload is not null)
                {
                    await _sessionStorageService.RemoveItemAsync(_pendingDownloadStorageKey, cancellationToken);
                }
            }

            var processedDownload = await _sessionStorageService.GetItemAsync<string>(_lastProcessedDownloadStorageKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(processedDownload))
            {
                _lastProcessedDownloadToken = processedDownload;
            }
        }

        /// <inheritdoc />
        public async Task CaptureFromUriAsync(string? uri, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return;
            }

            var downloadValue = _magnetLinkService.ExtractDownloadLink(uri);
            if (string.IsNullOrWhiteSpace(downloadValue) || HasAlreadyProcessed(downloadValue))
            {
                return;
            }

            _pendingDownloadLink = downloadValue;
            await _sessionStorageService.SetItemAsync(_pendingDownloadStorageKey, _pendingDownloadLink, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_pendingDownloadLink))
            {
                return;
            }

            var magnet = _pendingDownloadLink;
            if (string.Equals(_lastProcessedDownloadToken, magnet, StringComparison.Ordinal))
            {
                await ClearAsync(cancellationToken);
                _navigationManager.NavigateToHome(forceLoad: true);
                return;
            }

            try
            {
                await _dialogWorkflow.InvokeAddTorrentLinkDialog(magnet);
                await SaveLastProcessedDownloadAsync(magnet, cancellationToken);
                await ClearAsync(cancellationToken);
                _navigationManager.NavigateToHome(forceLoad: true);
            }
            catch (Exception)
            {
                _pendingDownloadLink = magnet;
                await _sessionStorageService.SetItemAsync(_pendingDownloadStorageKey, _pendingDownloadLink, cancellationToken);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _pendingDownloadLink = null;
            await _sessionStorageService.RemoveItemAsync(_pendingDownloadStorageKey, cancellationToken);
        }

        private bool HasAlreadyProcessed(string download)
        {
            return string.Equals(_lastProcessedDownloadToken, download, StringComparison.Ordinal);
        }

        private async Task SaveLastProcessedDownloadAsync(string download, CancellationToken cancellationToken)
        {
            _lastProcessedDownloadToken = download;
            await _sessionStorageService.SetItemAsync(_lastProcessedDownloadStorageKey, download, cancellationToken);
        }
    }
}
