using Moq;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal static class TorrentSelectorTestHelper
    {
        internal static TorrentSelector All()
        {
            return It.Is<TorrentSelector>(selector => IsAll(selector));
        }

        internal static TorrentSelector FromHashes(IEnumerable<string> hashes)
        {
            return It.Is<TorrentSelector>(selector => HasHashes(selector, hashes));
        }

        internal static TorrentSelector FromHashes(params string[] hashes)
        {
            return It.Is<TorrentSelector>(selector => HasHashes(selector, hashes));
        }

        internal static TorrentSelector FromHash(string hash)
        {
            return It.Is<TorrentSelector>(selector => HasHashes(selector, new[] { hash }));
        }

        internal static TorrentSelector? OptionalFromHashes(IEnumerable<string> hashes)
        {
            return It.Is<TorrentSelector?>(selector => selector != null && HasHashes(selector, hashes));
        }

        internal static TorrentSelector? OptionalFromHash(string hash)
        {
            return It.Is<TorrentSelector?>(selector => selector != null && HasHashes(selector, new[] { hash }));
        }

        internal static bool IsAll(TorrentSelector selector)
        {
            return selector.All
                && (selector.Hashes is null || !selector.Hashes.Any());
        }

        internal static bool HasHashes(TorrentSelector selector, IEnumerable<string> hashes)
        {
            return !selector.All
                && selector.Hashes is not null
                && selector.Hashes.SequenceEqual(hashes);
        }
    }
}
