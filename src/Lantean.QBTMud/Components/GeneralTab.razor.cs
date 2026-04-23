using Lantean.QBTMud.Services;
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

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected Lantean.QBTMud.Services.Localization.ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        protected IReadOnlyList<PieceState> Pieces { get; set; } = [];

        protected TorrentProperties Properties { get; set; } = default!;

        protected bool PiecesLoading => _piecesLoading;

        protected bool PiecesFailed => _piecesFailed;

        protected override async Task OnParametersSetAsync()
        {
            if (Hash is null)
            {
                return;
            }

            if (!Active)
            {
                return;
            }

            if (!_piecesLoaded)
            {
                _piecesLoading = true;
                _piecesFailed = false;
            }

            var propertiesResult = await ApiClient.GetTorrentPropertiesAsync(Hash);
            if (propertiesResult.IsFailure)
            {
                MarkPiecesFailed();
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

            Properties = propertiesResult.Value;

            var piecesResult = await ApiClient.GetTorrentPieceStatesAsync(Hash);
            if (piecesResult.IsFailure)
            {
                if (piecesResult.Failure?.Kind == ApiFailureKind.NotFound)
                {
                    MarkPiecesFailed();
                }
                else
                {
                    await ApiFeedbackWorkflow.HandleFailureAsync(piecesResult);
                }
            }
            else
            {
                Pieces = piecesResult.Value;
                MarkPiecesLoaded();
            }

            await InvokeAsync(StateHasChanged);
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

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            if (Active && Hash is not null)
            {
                var propertiesResult = await ApiClient.GetTorrentPropertiesAsync(Hash);
                if (propertiesResult.IsFailure)
                {
                    MarkPiecesFailed();
                    await InvokeAsync(StateHasChanged);
                    if (propertiesResult.Failure?.Kind is ApiFailureKind.NotFound or ApiFailureKind.AuthenticationRequired)
                    {
                        _timerCancellationToken.CancelIfNotDisposed();
                        return ManagedTimerTickResult.Stop;
                    }

                    await ApiFeedbackWorkflow.HandleFailureAsync(propertiesResult);
                    return ManagedTimerTickResult.Continue;
                }

                Properties = propertiesResult.Value;

                var piecesResult = await ApiClient.GetTorrentPieceStatesAsync(Hash);
                if (piecesResult.IsFailure)
                {
                    if (piecesResult.Failure?.Kind == ApiFailureKind.NotFound)
                    {
                        MarkPiecesFailed();
                        await InvokeAsync(StateHasChanged);
                        return ManagedTimerTickResult.Stop;
                    }

                    await ApiFeedbackWorkflow.HandleFailureAsync(piecesResult);
                }
                else
                {
                    Pieces = piecesResult.Value;
                    MarkPiecesLoaded();
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
