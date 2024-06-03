using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBitTorrentClient
{
    public static class ApiClientExtensions
    {
        public static Task PauseTorrent(this IApiClient apiClient, string hash)
        {
            return apiClient.PauseTorrents(null, hash);
        }

        public static Task PauseTorrents(this IApiClient apiClient, IEnumerable<string> hashes)
        {
            return apiClient.PauseTorrents(null, hashes.ToArray());
        }

        public static Task PauseAllTorrents(this IApiClient apiClient)
        {
            return apiClient.PauseTorrents(true);
        }

        public static Task ResumeTorrent(this IApiClient apiClient, string hash)
        {
            return apiClient.ResumeTorrents(null, hash);
        }

        public static Task ResumeTorrents(this IApiClient apiClient, IEnumerable<string> hashes)
        {
            return apiClient.ResumeTorrents(null, hashes.ToArray());
        }

        public static Task ResumeAllTorrents(this IApiClient apiClient)
        {
            return apiClient.ResumeTorrents(true);
        }

        public static Task DeleteTorrent(this IApiClient apiClient, string hash, bool deleteFiles)
        {
            return apiClient.DeleteTorrents(null, deleteFiles, hash);
        }

        public static Task DeleteTorrents(this IApiClient apiClient, IEnumerable<string> hashes, bool deleteFiles)
        {
            return apiClient.DeleteTorrents(null, deleteFiles, hashes.ToArray());
        }

        public static Task DeleteAllTorrents(this IApiClient apiClient, bool deleteFiles)
        {
            return apiClient.DeleteTorrents(true, deleteFiles);
        }

        public static async Task<Torrent?> GetTorrent(this IApiClient apiClient, string hash)
        {
            var torrents = await apiClient.GetTorrentList(hashes: hash);

            if (torrents.Count == 0)
            {
                return null;
            }

            return torrents[0];
        }

        public static Task SetTorrentCategory(this IApiClient apiClient, string category, string hash)
        {
            return apiClient.SetTorrentCategory(category, null, hash);
        }

        public static Task SetTorrentCategory(this IApiClient apiClient, string category, IEnumerable<string> hashes)
        {
            return apiClient.SetTorrentCategory(category, null, hashes.ToArray());
        }

        public static Task RemoveTorrentCategory(this IApiClient apiClient, string hash)
        {
            return apiClient.SetTorrentCategory(string.Empty, null, hash);
        }

        public static Task RemoveTorrentCategory(this IApiClient apiClient, IEnumerable<string> hashes)
        {
            return apiClient.SetTorrentCategory(string.Empty, null, hashes.ToArray());
        }

        public static Task RemoveTorrentTags(this IApiClient apiClient, IEnumerable<string> tags, string hash)
        {
            return apiClient.RemoveTorrentTags(tags, null, hash);
        }

        public static Task RemoveTorrentTags(this IApiClient apiClient, IEnumerable<string> tags, IEnumerable<string> hashes)
        {
            return apiClient.RemoveTorrentTags(tags, null, hashes.ToArray());
        }

        public static Task RemoveTorrentTag(this IApiClient apiClient, string tag, string hash)
        {
            return apiClient.RemoveTorrentTags([tag], hash);
        }

        public static Task RemoveTorrentTag(this IApiClient apiClient, string tag, IEnumerable<string> hashes)
        {
            return apiClient.RemoveTorrentTags([tag], null, hashes.ToArray());
        }

        public static Task AddTorrentTags(this IApiClient apiClient, IEnumerable<string> tags, string hash)
        {
            return apiClient.AddTorrentTags(tags, null, hash);
        }

        public static Task AddTorrentTags(this IApiClient apiClient, IEnumerable<string> tags, IEnumerable<string> hashes)
        {
            return apiClient.AddTorrentTags(tags, null, hashes.ToArray());
        }

        public static Task AddTorrentTag(this IApiClient apiClient, string tag, string hash)
        {
            return apiClient.AddTorrentTags([tag], hash);
        }

        public static Task AddTorrentTag(this IApiClient apiClient, string tag, IEnumerable<string> hashes)
        {
            return apiClient.AddTorrentTags([tag], null, hashes.ToArray());
        }

        public static Task RecheckTorrent(this IApiClient apiClient, string hash)
        {
            return apiClient.RecheckTorrents(null, hash);
        }

        public static Task ReannounceTorrent(this IApiClient apiClient, string hash)
        {
            return apiClient.ReannounceTorrents(null, hash);
        }
    }
}