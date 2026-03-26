using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Components
{
    public partial class PwaInstallPrompt : IAsyncDisposable
    {
        private static readonly TimeSpan _initialPopoverDisplayDelay = TimeSpan.FromMilliseconds(300);

        private DotNetObjectReference<PwaInstallPrompt>? _dotNetObjectReference;
        private long _subscriptionId;
        private bool _hiddenForSession;
        private bool _dismissedPermanently;
        private bool _promptInProgress;
        private bool _popoverOpen;
        private bool _displayedCanPromptInstall;
        private bool _displayedShowIosInstructions;
        private bool _disposeRequested;
        private int _promptStateVersion;

        [Inject]
        protected ISettingsStorageService SettingsStorage { get; set; } = default!;

        [Inject]
        protected IPwaInstallPromptService PwaInstallPromptService { get; set; } = default!;

        protected PwaInstallPromptState InstallPromptState { get; set; } = new();

        protected bool CanPromptInstall =>
            InstallPromptState.CanPrompt && !InstallPromptState.IsInstalled;

        protected bool ShowIosInstructions =>
            InstallPromptState.IsIos && !CanPromptInstall;

        protected bool DisplayedCanPromptInstall => _displayedCanPromptInstall;

        protected bool DisplayedShowIosInstructions => _displayedShowIosInstructions;

        protected bool IsInstallPromptBusy =>
            _promptInProgress || InstallPromptState.IsPromptInProgress;

        protected bool ShouldShowPrompt =>
            !_hiddenForSession
            && !_dismissedPermanently
            && !InstallPromptState.IsInstalled
            && (InstallPromptState.CanPrompt || InstallPromptState.IsIos);

        protected bool IsPopoverOpen =>
            _popoverOpen && ShouldKeepPromptVisible;

        private bool ShouldKeepPromptVisible =>
            !_hiddenForSession
            && !_dismissedPermanently
            && !InstallPromptState.IsInstalled
            && (InstallPromptState.CanPrompt || InstallPromptState.IsIos || IsInstallPromptBusy);

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            var persistedDismissal = await SettingsStorage.GetItemAsync<bool?>(PwaInstallPromptStorageKeys.Dismissed);
            _dismissedPermanently = persistedDismissal.GetValueOrDefault();

            _dotNetObjectReference = DotNetObjectReference.Create(this);
            _subscriptionId = await PwaInstallPromptService.SubscribeInstallPromptStateAsync(_dotNetObjectReference);
        }

        /// <summary>
        /// Receives browser install prompt state updates from JavaScript.
        /// </summary>
        /// <param name="state">The latest install prompt state.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [JSInvokable]
        public async Task OnInstallPromptStateChanged(PwaInstallPromptState state)
        {
            if (_dismissedPermanently)
            {
                var persistedDismissal = await SettingsStorage.GetItemAsync<bool?>(PwaInstallPromptStorageKeys.Dismissed);
                var isDismissedPermanently = persistedDismissal.GetValueOrDefault();

                if (!isDismissedPermanently)
                {
                    _hiddenForSession = false;
                }

                _dismissedPermanently = isDismissedPermanently;
            }

            InstallPromptState = state ?? new PwaInstallPromptState();
            _promptStateVersion++;
            var promptStateVersion = _promptStateVersion;

            if (!IsInstallPromptBusy)
            {
                UpdateDisplayedPromptMode();
                await RefreshPromptVisibilityAsync(promptStateVersion);
            }

            if (!_disposeRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        protected Task HideForSession()
        {
            _hiddenForSession = true;
            _promptStateVersion++;
            _popoverOpen = false;
            return InvokeAsync(StateHasChanged);
        }

        protected async Task DismissForever()
        {
            _dismissedPermanently = true;
            _hiddenForSession = true;
            _promptStateVersion++;
            _popoverOpen = false;
            await SettingsStorage.SetItemAsync(PwaInstallPromptStorageKeys.Dismissed, true);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task PromptInstall()
        {
            if (_promptInProgress)
            {
                return;
            }

            _promptInProgress = true;
            try
            {
                var outcome = await PwaInstallPromptService.RequestInstallPromptAsync();
                if (string.Equals(outcome, "accepted", StringComparison.Ordinal))
                {
                    await HideForSession();
                    return;
                }

                InstallPromptState = await PwaInstallPromptService.GetInstallPromptStateAsync();
                _promptStateVersion++;
            }
            finally
            {
                _promptInProgress = false;
            }

            UpdateDisplayedPromptMode();
            await RefreshPromptVisibilityAsync(_promptStateVersion);
            await InvokeAsync(StateHasChanged);
        }

        private async Task RefreshPromptVisibilityAsync(int promptStateVersion)
        {
            if (!ShouldKeepPromptVisible)
            {
                _popoverOpen = false;
                return;
            }

            if (_popoverOpen || !ShouldShowPrompt)
            {
                return;
            }

            await Task.Delay(_initialPopoverDisplayDelay);

            if (_disposeRequested
                || IsInstallPromptBusy
                || promptStateVersion != _promptStateVersion
                || !ShouldShowPrompt)
            {
                return;
            }

            _popoverOpen = true;
        }

        private void UpdateDisplayedPromptMode()
        {
            _displayedCanPromptInstall = CanPromptInstall;
            _displayedShowIosInstructions = ShowIosInstructions;
        }

        /// <summary>
        /// Releases JS interop subscriptions used by the component.
        /// </summary>
        /// <returns>A task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _disposeRequested = true;
            _popoverOpen = false;

            if (_subscriptionId > 0)
            {
                try
                {
                    await PwaInstallPromptService.UnsubscribeInstallPromptStateAsync(_subscriptionId);
                }
                catch (JSException)
                {
                    // Ignore JS teardown failures during disposal because the component is already being destroyed.
                }
            }

            _dotNetObjectReference?.Dispose();
        }
    }
}
