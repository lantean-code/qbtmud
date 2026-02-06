using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components
{
    public partial class TorrentInfo
    {
        private const string _transferListModelContext = "TransferListModel";

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public string Hash { get; set; } = default!;

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        protected Torrent? Torrent => GetTorrent();

        private Torrent? GetTorrent()
        {
            if (Hash is null || !MainData.Torrents.TryGetValue(Hash, out var torrent))
            {
                return null;
            }

            return torrent;
        }

        private string TranslateTransferListModel(string source, params object[] arguments)
        {
            return WebUiLocalizer.Translate(_transferListModelContext, source, arguments);
        }
    }
}
