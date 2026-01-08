using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class Details : IAsyncDisposable
    {
        private const int TabCount = 5;

        private static readonly KeyboardEvent _backspaceKey = new("Backspace");
        private static readonly KeyboardEvent _altOneKey = new("1") { AltKey = true };
        private static readonly KeyboardEvent _altTwoKey = new("2") { AltKey = true };
        private static readonly KeyboardEvent _altThreeKey = new("3") { AltKey = true };
        private static readonly KeyboardEvent _altFourKey = new("4") { AltKey = true };
        private static readonly KeyboardEvent _altFiveKey = new("5") { AltKey = true };
        private static readonly KeyboardEvent _ctrlArrowLeftKey = new("ArrowLeft") { CtrlKey = true };
        private static readonly KeyboardEvent _ctrlArrowRightKey = new("ArrowRight") { CtrlKey = true };

        private bool _disposedValue;

        private bool _shortcutsRegistered;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences Preferences { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        protected int ActiveTab { get; set; } = 0;

        protected int RefreshInterval => MainData?.ServerState.RefreshInterval ?? 1500;

        protected string Name => GetName();

        protected bool ShowTabs { get; set; } = true;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!IsTorrentAvailable())
            {
                ShowTabs = false;
                NavigationManager.NavigateToHome(forceLoad: true);
                return;
            }

            ShowTabs = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await RegisterShortcutAsync();
            }
        }

        private string GetName()
        {
            if (Hash is null || MainData is null)
            {
                return "";
            }

            if (!MainData.Torrents.TryGetValue(Hash, out var torrent))
            {
                return "";
            }

            return torrent.Name;
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await UnregisterShortcutAsync();
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Releases the resources used by the component.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool IsTorrentAvailable()
        {
            if (MainData is null || string.IsNullOrWhiteSpace(Hash))
            {
                return false;
            }

            return MainData.Torrents.ContainsKey(Hash);
        }

        private async Task RegisterShortcutAsync()
        {
            if (_shortcutsRegistered)
            {
                return;
            }

            await KeyboardService.RegisterKeypressEvent(_backspaceKey, HandleBackspace);
            await KeyboardService.RegisterKeypressEvent(_altOneKey, _ => SetActiveTabAsync(0));
            await KeyboardService.RegisterKeypressEvent(_altTwoKey, _ => SetActiveTabAsync(1));
            await KeyboardService.RegisterKeypressEvent(_altThreeKey, _ => SetActiveTabAsync(2));
            await KeyboardService.RegisterKeypressEvent(_altFourKey, _ => SetActiveTabAsync(3));
            await KeyboardService.RegisterKeypressEvent(_altFiveKey, _ => SetActiveTabAsync(4));
            await KeyboardService.RegisterKeypressEvent(_ctrlArrowLeftKey, _ => MoveActiveTabAsync(-1));
            await KeyboardService.RegisterKeypressEvent(_ctrlArrowRightKey, _ => MoveActiveTabAsync(1));
            _shortcutsRegistered = true;
        }

        private async Task UnregisterShortcutAsync()
        {
            if (!_shortcutsRegistered)
            {
                return;
            }

            await KeyboardService.UnregisterKeypressEvent(_backspaceKey);
            await KeyboardService.UnregisterKeypressEvent(_altOneKey);
            await KeyboardService.UnregisterKeypressEvent(_altTwoKey);
            await KeyboardService.UnregisterKeypressEvent(_altThreeKey);
            await KeyboardService.UnregisterKeypressEvent(_altFourKey);
            await KeyboardService.UnregisterKeypressEvent(_altFiveKey);
            await KeyboardService.UnregisterKeypressEvent(_ctrlArrowLeftKey);
            await KeyboardService.UnregisterKeypressEvent(_ctrlArrowRightKey);
            _shortcutsRegistered = false;
        }

        private Task HandleBackspace(KeyboardEvent keyboardEvent)
        {
            NavigateBack();
            return Task.CompletedTask;
        }

        private Task SetActiveTabAsync(int index)
        {
            ActiveTab = Math.Clamp(index, 0, TabCount - 1);
            return InvokeAsync(StateHasChanged);
        }

        private Task MoveActiveTabAsync(int delta)
        {
            return SetActiveTabAsync(ActiveTab + delta);
        }
    }
}
