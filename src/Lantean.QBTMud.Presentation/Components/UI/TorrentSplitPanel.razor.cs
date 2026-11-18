using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

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
        private bool _initializing;
        private int _ratio = 40;
        private int _sizeSelection = 40;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Parameter]
        public bool Enabled { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        public bool IsOpen => Enabled && _available && !_hidden && Hash is not null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!Enabled || _disposed || _initializing || _module is not null)
            {
                return;
            }

            _initializing = true;
            try
            {
                _ratio = Math.Clamp(await LocalStorage.GetItemAsync<int?>(_sizeStorageKey) ?? 40, 0, 100);
                _sizeSelection = _ratio;
                if (_disposed || !Enabled)
                {
                    return;
                }

                var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Lantean.QBTMud.Presentation/torrent-split-panel.module.js");
                if (_disposed || !Enabled)
                {
                    await module.DisposeAsync();
                    return;
                }

                _module = module;
                _reference = DotNetObjectReference.Create(this);
                await _module.InvokeVoidAsync("initialize", _element, _reference, _ratio);
            }
            finally
            {
                _initializing = false;
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
            if (_disposed)
            {
                return;
            }

            _ratio = Math.Clamp(ratio, 0, 100);
            _sizeSelection = _ratio;
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
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _reference?.Dispose();
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }

        private async Task OnSizeChanged(int ratio)
        {
            if (ratio != -1)
            {
                await ChangeRatio(ratio);
                return;
            }

            _sizeSelection = -1;
            await InvokeAsync(StateHasChanged);
            try
            {
                var parameters = new DialogParameters<SliderFieldDialog<int>>
                {
                    { dialog => dialog.Label, Translate("Details size") },
                    { dialog => dialog.Min, 0 },
                    { dialog => dialog.Max, 100 },
                    { dialog => dialog.Value, _ratio },
                    { dialog => dialog.ValueTryGetFunc, TryParseRatio },
                    { dialog => dialog.InputErrorText, Translate("Enter a percentage from 0 to 100.") },
                    { dialog => dialog.Adornment, Adornment.End },
                    { dialog => dialog.AdornmentText, "%" }
                };
                var reference = await DialogService.ShowAsync<SliderFieldDialog<int>>(
                    Translate("Details size"), parameters, DialogWorkflow.FormDialogOptions with { MaxWidth = MaxWidth.Small });
                var result = await reference.Result;
                if (result is { Canceled: false, Data: int value })
                {
                    await ChangeRatio(value);
                }
            }
            finally
            {
                _sizeSelection = _ratio;
                if (!_disposed)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private string Translate(string source)
        {
            return LanguageLocalizer.Translate("AppTorrentSplit", source);
        }

        private static int? TryParseRatio(string value)
        {
            if (int.TryParse(value, out var ratio) && ratio >= 0 && ratio <= 100)
            {
                return ratio;
            }

            return null;
        }
    }
}
