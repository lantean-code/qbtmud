using Lantean.QBTMud.Components.Dialogs;
using MudBlazor;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="ILostConnectionWorkflow"/>.
    /// </summary>
    public sealed class LostConnectionWorkflow : ILostConnectionWorkflow
    {
        private readonly IDialogService _dialogService;
        private readonly ILogger<LostConnectionWorkflow> _logger;
        private readonly SemaphoreSlim _stateLock = new(1, 1);
        private IDialogReference? _dialogReference;
        private bool _isShowing;

        /// <summary>
        /// Initializes a new instance of the <see cref="LostConnectionWorkflow"/> class.
        /// </summary>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="logger">The logger.</param>
        public LostConnectionWorkflow(
            IDialogService dialogService,
            ILogger<LostConnectionWorkflow> logger)
        {
            _dialogService = dialogService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task MarkLostConnectionAsync()
        {
            await _stateLock.WaitAsync();
            try
            {
                if (_dialogReference is not null || _isShowing)
                {
                    return;
                }

                _isShowing = true;
            }
            finally
            {
                _stateLock.Release();
            }

            await ShowDialogAsync();
        }

        private async Task ShowDialogAsync()
        {
            IDialogReference? dialogReference = null;

            try
            {
                var options = new DialogOptions
                {
                    CloseOnEscapeKey = false,
                    BackdropClick = false,
                    NoHeader = true,
                    FullWidth = true,
                    MaxWidth = MaxWidth.ExtraSmall,
                    BackgroundClass = "background-blur background-blur-strong"
                };

                dialogReference = await _dialogService.ShowAsync<LostConnectionDialog>(title: null, options);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to show the lost-connection dialog.");
            }
            finally
            {
                await _stateLock.WaitAsync();
                try
                {
                    _isShowing = false;
                    if (dialogReference is not null)
                    {
                        _dialogReference = dialogReference;
                    }
                }
                finally
                {
                    _stateLock.Release();
                }
            }
        }
    }
}
