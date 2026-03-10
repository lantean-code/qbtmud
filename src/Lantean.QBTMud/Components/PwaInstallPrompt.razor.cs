using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class PwaInstallPrompt : IAsyncDisposable
    {
        private const string _dismissedStorageKey = "PwaInstallPrompt.Dismissed.v1";
        private const string _installSnackbarKey = "pwa-install-prompt";
        private const string _installSnackbarCssClass = "pwa-install-snackbar";

        private DotNetObjectReference<PwaInstallPrompt>? _dotNetObjectReference;
        private long _subscriptionId;
        private bool _hiddenForSession;
        private bool _dismissedPermanently;
        private bool _promptInProgress;
        private bool _snackbarShown;
        private bool _snackbarCanPromptInstall;
        private bool _snackbarShowsIosInstructions;

        [Inject]
        protected ISettingsStorageService SettingsStorage { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected IPwaInstallPromptService PwaInstallPromptService { get; set; } = default!;

        protected PwaInstallPromptState InstallPromptState { get; set; } = new();

        protected bool CanPromptInstall =>
            InstallPromptState.CanPrompt && !InstallPromptState.IsInstalled;

        protected bool ShowIosInstructions =>
            InstallPromptState.IsIos && !CanPromptInstall;

        protected bool ShouldShowPrompt =>
            !_hiddenForSession
            && !_dismissedPermanently
            && !InstallPromptState.IsInstalled
            && (InstallPromptState.CanPrompt || InstallPromptState.IsIos);

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            var persistedDismissal = await SettingsStorage.GetItemAsync<bool?>(_dismissedStorageKey);
            _dismissedPermanently = persistedDismissal.GetValueOrDefault();

            if (_dismissedPermanently)
            {
                return;
            }

            _dotNetObjectReference = DotNetObjectReference.Create(this);
            _subscriptionId = await PwaInstallPromptService.SubscribeInstallPromptStateAsync(_dotNetObjectReference);
        }

        /// <summary>
        /// Receives browser install prompt state updates from JavaScript.
        /// </summary>
        /// <param name="state">The latest install prompt state.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [JSInvokable]
        public Task OnInstallPromptStateChanged(PwaInstallPromptState state)
        {
            InstallPromptState = state ?? new PwaInstallPromptState();
            RefreshPromptSnackbar();
            return InvokeAsync(StateHasChanged);
        }

        protected Task HideForSession()
        {
            _hiddenForSession = true;
            RemovePromptSnackbar();
            return InvokeAsync(StateHasChanged);
        }

        protected async Task DismissForever()
        {
            _dismissedPermanently = true;
            _hiddenForSession = true;
            _snackbarShown = false;
            await SettingsStorage.SetItemAsync(_dismissedStorageKey, true);
            RemovePromptSnackbar();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task PromptInstall()
        {
            if (_promptInProgress)
            {
                return;
            }

            _snackbarShown = false;
            _promptInProgress = true;
            try
            {
                var outcome = await PwaInstallPromptService.RequestInstallPromptAsync();
                if (string.Equals(outcome, "accepted", StringComparison.Ordinal))
                {
                    await HideForSession();
                    return;
                }
            }
            finally
            {
                _promptInProgress = false;
            }

            RefreshPromptSnackbar();
            await InvokeAsync(StateHasChanged);
        }

        private void RefreshPromptSnackbar()
        {
            if (!ShouldShowPrompt)
            {
                RemovePromptSnackbar();
                return;
            }

            if (_snackbarShown
                && (_snackbarCanPromptInstall == CanPromptInstall)
                && (_snackbarShowsIosInstructions == ShowIosInstructions))
            {
                return;
            }

            ShowPromptSnackbar();
        }

        private void ShowPromptSnackbar()
        {
            var componentParameters = new Dictionary<string, object>
            {
                [nameof(PwaInstallPromptSnackbarContent.CanPromptInstall)] = CanPromptInstall,
                [nameof(PwaInstallPromptSnackbarContent.ShowIosInstructions)] = ShowIosInstructions,
                [nameof(PwaInstallPromptSnackbarContent.OnInstallClicked)] = EventCallback.Factory.Create<MouseEventArgs>(this, PromptInstall),
                [nameof(PwaInstallPromptSnackbarContent.OnDismissClicked)] = EventCallback.Factory.Create<MouseEventArgs>(this, DismissForever)
            };

            SnackbarWorkflow.ShowComponent<PwaInstallPromptSnackbarContent>(
                componentParameters,
                Severity.Normal,
                options =>
                {
                    options.RequireInteraction = true;
                    options.ShowCloseIcon = false;
                    options.HideIcon = true;
                    options.SnackbarVariant = Variant.Outlined;
                    options.SnackbarTypeClass = _installSnackbarCssClass;
                },
                _installSnackbarKey);

            _snackbarShown = true;
            _snackbarCanPromptInstall = CanPromptInstall;
            _snackbarShowsIosInstructions = ShowIosInstructions;
        }

        private void RemovePromptSnackbar()
        {
            SnackbarWorkflow.Hide(_installSnackbarKey);
            _snackbarShown = false;
        }

        /// <summary>
        /// Releases JS interop subscriptions used by the component.
        /// </summary>
        /// <returns>A task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            RemovePromptSnackbar();

            if (_subscriptionId > 0)
            {
                try
                {
                    await PwaInstallPromptService.UnsubscribeInstallPromptStateAsync(_subscriptionId);
                }
                catch (JSException)
                {
                }
            }

            _dotNetObjectReference?.Dispose();
        }
    }
}
