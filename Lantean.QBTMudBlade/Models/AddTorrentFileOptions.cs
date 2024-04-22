using Microsoft.AspNetCore.Components.Forms;

namespace Lantean.QBTMudBlade.Models
{
    public record AddTorrentFileOptions : TorrentOptions
    {
        public AddTorrentFileOptions(IReadOnlyList<IBrowserFile> files, TorrentOptions options) : base(options)
        {
            Files = files;
        }

        public IReadOnlyList<IBrowserFile> Files { get; }
    }
}