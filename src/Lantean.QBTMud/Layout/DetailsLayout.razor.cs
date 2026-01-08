using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Layout
{
    public partial class DetailsLayout : IAsyncDisposable
    {
        private static readonly KeyboardEvent _altArrowUpKey = new("ArrowUp") { AltKey = true };
        private static readonly KeyboardEvent _altArrowDownKey = new("ArrowDown") { AltKey = true };

        private bool _disposedValue;
        private bool _shortcutsRegistered;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "DrawerOpenChanged")]
        public EventCallback<bool> DrawerOpenChanged { get; set; }

        [CascadingParameter]
        public IEnumerable<Torrent>? Torrents { get; set; }

        [CascadingParameter(Name = "SortColumn")]
        public string? SortColumn { get; set; }

        [CascadingParameter(Name = "SortDirection")]
        public SortDirection SortDirection { get; set; }

        protected string? SelectedTorrent { get; set; }

        protected IReadOnlyList<Torrent> OrderedTorrents => GetOrderedTorrents();

        protected override void OnParametersSet()
        {
            var selectedHash = GetSelectedHash();
            if (string.IsNullOrWhiteSpace(selectedHash))
            {
                return;
            }

            SelectedTorrent = selectedHash;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await RegisterShortcutsAsync();
            }
        }

        protected async Task OnDrawerOpenChanged(bool value)
        {
            DrawerOpen = value;
            if (DrawerOpenChanged.HasDelegate)
            {
                await DrawerOpenChanged.InvokeAsync(value);
            }
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await UnregisterShortcutsAsync();
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Releases the resources used by the layout.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        private async Task RegisterShortcutsAsync()
        {
            if (_shortcutsRegistered)
            {
                return;
            }

            await KeyboardService.RegisterKeypressEvent(_altArrowUpKey, _ => NavigateAdjacentTorrent(-1));
            await KeyboardService.RegisterKeypressEvent(_altArrowDownKey, _ => NavigateAdjacentTorrent(1));
            _shortcutsRegistered = true;
        }

        private async Task UnregisterShortcutsAsync()
        {
            if (!_shortcutsRegistered)
            {
                return;
            }

            await KeyboardService.UnregisterKeypressEvent(_altArrowUpKey);
            await KeyboardService.UnregisterKeypressEvent(_altArrowDownKey);
            _shortcutsRegistered = false;
        }

        private Task NavigateAdjacentTorrent(int offset)
        {
            var orderedTorrents = OrderedTorrents;
            if (orderedTorrents.Count == 0 || string.IsNullOrWhiteSpace(SelectedTorrent))
            {
                return Task.CompletedTask;
            }

            var currentIndex = -1;
            for (var i = 0; i < orderedTorrents.Count; i++)
            {
                if (orderedTorrents[i].Hash == SelectedTorrent)
                {
                    currentIndex = i;
                    break;
                }
            }
            if (currentIndex < 0)
            {
                return Task.CompletedTask;
            }

            var nextIndex = currentIndex + offset;
            if (nextIndex < 0 || nextIndex >= orderedTorrents.Count)
            {
                return Task.CompletedTask;
            }

            NavigationManager.NavigateTo($"./details/{orderedTorrents[nextIndex].Hash}");
            return Task.CompletedTask;
        }

        private string? GetSelectedHash()
        {
            var path = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            if (!path.StartsWith("details/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var hashSegment = path["details/".Length..];
            var queryIndex = hashSegment.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                hashSegment = hashSegment[..queryIndex];
            }

            return string.IsNullOrWhiteSpace(hashSegment) ? null : hashSegment;
        }

        private List<Torrent> GetOrderedTorrents()
        {
            if (Torrents is null)
            {
                return [];
            }

            var sortSelector = TorrentList.ColumnsDefinitions.Find(t => t.Id == SortColumn)?.SortSelector ?? (t => t.Name);
            return Torrents.OrderByDirection(SortDirection, sortSelector).ToList();
        }
    }
}
