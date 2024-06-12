using Lantean.QBTMudBlade.Models;

namespace Lantean.QBTMudBlade
{
    public static class Extensions
    {
        public const char DirectorySeparator = '/';

        public static string GetDirectoryPath(this string pathAndFileName)
        {
            return string.Join(DirectorySeparator, pathAndFileName.Split(DirectorySeparator)[..^1]);
        }

        public static string GetDirectoryPath(this ContentItem contentItem)
        {
            return contentItem.Name.GetDirectoryPath();
        }

        public static string GetFileName(this string pathAndFileName)
        {
            return pathAndFileName.Split(DirectorySeparator)[^1];
        }

        public static string GetFileName(this ContentItem contentItem)
        {
            return contentItem.Name.GetFileName();
        }

        public static string GetDescendantsKey(this string pathAndFileName, int? level = null)
        {
            var paths = pathAndFileName.Split(DirectorySeparator);
            var index = level is null ? new Index(1, true) : new Index(level.Value);
            return string.Join(DirectorySeparator, paths[0..index]) + DirectorySeparator;
        }

        public static string GetDescendantsKey(this ContentItem contentItem, int? level = null)
        {
            return contentItem.Name.GetDescendantsKey(level);
        }

        public static void CancelIfNotDisposed(this CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // disposed
            }
        }

        public static bool IsFinished(this Torrent torrent)
        {
            return torrent.TotalSize == torrent.Downloaded;
        }

        public static bool MetaDownloaded(this Torrent torrent)
        {
            return !(torrent.State == "metaDL" || torrent.State == "forcedMetaDL" || torrent.TotalSize == -1);
        }
    }
}