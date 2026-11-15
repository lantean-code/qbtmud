using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Components.UI
{
    public partial class TorrentSplitPanel : IAsyncDisposable
    {
        private const string _sizeStorageKey = "TorrentList.DetailsRatio";
        private ElementReference _element;
        private IJSObjectReference? _module;
        private DotNetObjectReference<TorrentSplitPanel>? _reference;
        private bool _available;
        private bool _hidden;
        private bool _disposed;
        private int _ratio = 40;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public bool Enabled { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        public bool IsOpen => Enabled && _available && !_hidden && Hash is not null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Enabled && _module is null)
            {
                _ratio = Math.Clamp(await LocalStorage.GetItemAsync<int?>(_sizeStorageKey) ?? 40, 30, 60);
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Lantean.QBTMud.Presentation/torrent-split-panel.module.js");
                _reference = DotNetObjectReference.Create(this);
                await _module.InvokeVoidAsync("initialize", _element, _reference, _ratio);
            }
        }

        /// <summary>
        /// Updates whether the viewport and workspace can accommodate both panels.
        /// </summary>
        /// <param name="available">Whether sufficient space is available.</param>
        /// <returns>A task representing the render update.</returns>
        [JSInvokable]
        public Task SetAvailability(bool available)
        {
            if (_disposed || _available == available)
            {
                return Task.CompletedTask;
            }

            _available = available;
            return InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Remembers the details panel's preferred share of the workspace.
        /// </summary>
        /// <param name="ratio">The preferred percentage occupied by details.</param>
        /// <returns>A task representing persistence and the render update.</returns>
        [JSInvokable]
        public async Task ChangeRatio(int ratio)
        {
            _ratio = Math.Clamp(ratio, 30, 60);
            await LocalStorage.SetItemAsync(_sizeStorageKey, _ratio);
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("setRatio", _element, _ratio);
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Hides or restores the details panel without changing the selection.
        /// </summary>
        /// <returns>A task representing the render update.</returns>
        public Task ToggleDetails()
        {
            _hidden = !_hidden;
            return InvokeAsync(StateHasChanged);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _disposed = true;
            _reference?.Dispose();
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }

        private string Translate(string source)
        {
            return LanguageLocalizer.Translate("AppTorrentSplit", source);
        }
    }
}
