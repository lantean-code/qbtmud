using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components
{
    public partial class TorrentInfo
    {
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
    }
}