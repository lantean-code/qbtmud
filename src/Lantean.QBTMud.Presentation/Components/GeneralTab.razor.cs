using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components
{
    public partial class GeneralTab : IAsyncDisposable
    {
        private const string _propertiesContext = "PropertiesWidget";

        private readonly bool _refreshEnabled = true;

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _disposedValue;
        private bool _piecesLoaded;
        private bool _piecesLoading = true;
        private bool _piecesFailed;
        private string? _requestedHash;
        private string? _displayedHash;
        private double? _displayedProgress;
        private int _requestVersion;
        private bool _selectionLoading;

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [Parameter]
        public double? TorrentProgress { get; set; }

        [Parameter]
        public bool? PiecesExpanded { get; set; }

        [Parameter]
        public EventCallback<bool> PiecesExpandedChanged { get; set; }

        [Parameter]
        public EventCallback<string> DisplayedHashChanged { get; set; }

        [CascadingParameter]
        public PiecesProgressLayoutState? PiecesLayoutState { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        protected IReadOnlyList<PieceState> Pieces { get; set; } = [];

        protected TorrentProperties Properties { get; set; } = default!;

        protected bool PiecesLoading => _piecesLoading;

        protected bool PiecesFailed => _piecesFailed;

        protected string? DisplayedHash => _displayedHash ?? Hash;

        protected double? DisplayedProgress => _displayedHash is null || _displayedHash == Hash ? TorrentProgress : _displayedProgress;

        protected override async Task OnParametersSetAsync()
        {
            if (_disposedValue || Hash is null)
            {
                return;
            }

            if (!Active || _requestedHash == Hash)
            {
                return;
            }

            await LoadSelectionAsync(Hash);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (firstRender)
            {
                _refreshTimer ??= ManagedTimerFactory.Create("GeneralTabRefresh", TimeSpan.FromMilliseconds(RefreshInterval));
                await _refreshTimer.StartAsync(RefreshTickAsync, _timerCancellationToken.Token);
            }
        }

        private async Task LoadSelectionAsync(string hash)
        {
            _requestedHash = hash;
            var version = ++_requestVersion;
            var progress = TorrentProgress;
            _selectionLoading = true;
            try
            {
                if (!_piecesLoaded)
                {
                    _piecesLoading = true;
                    _piecesFailed = false;
                }

                var propertiesResult = await ApiClient.GetTorrentPropertiesAsync(hash);
                if (!IsCurrentRequest(hash, version))
                {
                    return;
                }
                if (propertiesResult.IsFailure)
                {
                    if (_displayedHash is null)
                    {
                        MarkPiecesFailed();
                    }
                    if (propertiesResult.Failure?.Kind == ApiFailureKind.NotFound)
                    {
                        _timerCancellationToken.CancelIfNotDisposed();
                    }
                    else
                    {
                        await ApiFeedbackWorkflow.HandleFailureAsync(propertiesResult);
                    }

                    await InvokeAsync(StateHasChanged);
                    return;
                }

                var piecesResult = await ApiClient.GetTorrentPieceStatesAsync(hash);
                if (!IsCurrentRequest(hash, version))
                {
                    return;
                }
                if (piecesResult.IsFailure)
                {
                    if (piecesResult.Failure?.Kind == ApiFailureKind.NotFound)
                    {
                        if (_displayedHash is null)
                        {
                            MarkPiecesFailed();
                        }
                    }
                    else
                    {
                        await ApiFeedbackWorkflow.HandleFailureAsync(piecesResult);
                    }
                    if (_displayedHash is null)
                    {
                        Properties = propertiesResult.Value;
                    }
                }
                else
                {
                    Properties = propertiesResult.Value;
                    Pieces = piecesResult.Value;
                    MarkPiecesLoaded();
                    _displayedHash = hash;
                    _displayedProgress = progress;
                    await DisplayedHashChanged.InvokeAsync(hash);
                }

                await InvokeAsync(StateHasChanged);
            }
            finally
            {
                if (version == _requestVersion)
                {
                    _selectionLoading = false;
                }
            }
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            if (_disposedValue || cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            if (_selectionLoading)
            {
                return ManagedTimerTickResult.Continue;
            }

            if (Active && Hash is not null)
            {
                if (_displayedHash is not null && _displayedHash != Hash)
                {
                    await LoadSelectionAsync(Hash);
                    return ManagedTimerTickResult.Continue;
                }

                var hash = Hash;
                var version = _requestVersion;
                var propertiesResult = await ApiClient.GetTorrentPropertiesAsync(hash);
                if (!IsCurrentRequest(hash, version))
                {
                    return ManagedTimerTickResult.Continue;
                }
                if (propertiesResult.IsFailure)
                {
                    MarkPiecesFailed();
                    await InvokeAsync(StateHasChanged);
                    var failureKind = propertiesResult.Failure?.Kind;
                    await ApiFeedbackWorkflow.HandleFailureAsync(propertiesResult, failure =>
                    {
                        if (failure.Kind is ApiFailureKind.NotFound or ApiFailureKind.AuthenticationRequired)
                        {
                            _timerCancellationToken.CancelIfNotDisposed();
                        }

                        return failure.Kind == ApiFailureKind.NotFound
                            ? ApiFeedbackCustomFailureResult.StopHandling
                            : ApiFeedbackCustomFailureResult.ContinueWithWorkflow;
                    });

                    if (failureKind is ApiFailureKind.NotFound or ApiFailureKind.AuthenticationRequired)
                    {
                        return ManagedTimerTickResult.Stop;
                    }

                    return ManagedTimerTickResult.Continue;
                }

                var piecesResult = await ApiClient.GetTorrentPieceStatesAsync(hash);
                if (!IsCurrentRequest(hash, version))
                {
                    return ManagedTimerTickResult.Continue;
                }
                if (piecesResult.IsFailure)
                {
                    var failureKind = piecesResult.Failure?.Kind;
                    await ApiFeedbackWorkflow.HandleFailureAsync(piecesResult, failure =>
                    {
                        if (failure.Kind == ApiFailureKind.NotFound)
                        {
                            MarkPiecesFailed();
                            return ApiFeedbackCustomFailureResult.StopHandling;
                        }

                        return ApiFeedbackCustomFailureResult.ContinueWithWorkflow;
                    });

                    if (failureKind == ApiFailureKind.NotFound)
                    {
                        await InvokeAsync(StateHasChanged);
                        return ManagedTimerTickResult.Stop;
                    }
                }
                else
                {
                    Properties = propertiesResult.Value;
                    Pieces = piecesResult.Value;
                    MarkPiecesLoaded();
                    if (_displayedHash != hash)
                    {
                        _displayedHash = hash;
                        _displayedProgress = TorrentProgress;
                        await DisplayedHashChanged.InvokeAsync(hash);
                    }
                }

                await InvokeAsync(StateHasChanged);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            return ManagedTimerTickResult.Continue;
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();
                    if (_refreshTimer is not null)
                    {
                        await _refreshTimer.DisposeAsync();
                    }

                    await Task.CompletedTask;
                }

                _disposedValue = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void MarkPiecesLoaded()
        {
            _piecesLoaded = true;
            _piecesLoading = false;
            _piecesFailed = false;
        }

        private bool IsCurrentRequest(string hash, int version)
        {
            return !_disposedValue && Hash == hash && version == _requestVersion;
        }

        private void MarkPiecesFailed()
        {
            _piecesLoaded = true;
            _piecesLoading = false;
            _piecesFailed = true;
            Pieces = [];
        }

        private string TranslateProperties(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_propertiesContext, source, arguments);
        }

        private string FormatWithDetail(string? value, string? detail, string formatKey)
        {
            var valueText = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(valueText))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                return valueText;
            }

            return TranslateProperties(formatKey, valueText, detail);
        }
    }
}
