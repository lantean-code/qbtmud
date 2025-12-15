using System.Diagnostics;

namespace Lantean.QBTMud.Models
{
    [DebuggerDisplay("{Name}")]
    public class ContentItem
    {
        public ContentItem(
            string name,
            string displayName,
            int index,
            Priority priority,
            float progress,
            long size,
            float availability,
            bool isFolder = false,
            int level = 0,
            long? downloadSize = null)
        {
            Name = name;
            DisplayName = displayName;
            Index = index;
            Priority = priority;
            Progress = progress;
            Size = size;
            DownloadSize = downloadSize ?? size;
            Availability = availability;
            IsFolder = isFolder;
            Level = level;
        }

        public string Name { get; }

        public string Path => IsFolder ? Name : Name.GetDirectoryPath();

        public string DisplayName { get; }

        public int Index { get; }

        public Priority Priority { get; set; }

        public float Progress { get; set; }

        public long Size { get; set; }

        public long DownloadSize { get; set; }

        public float Availability { get; set; }

        public long Downloaded => (long)Math.Round(DownloadSize * (double)Progress, 0);

        public long Remaining => Progress == 1 || Priority == Priority.DoNotDownload ? 0 : DownloadSize - Downloaded;

        public bool IsFolder { get; }

        public int Level { get; }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            return ((ContentItem)obj).Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
