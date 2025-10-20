namespace Lantean.QBTMud.Models
{
    public class FileRow
    {
        public string OriginalName { get; set; }
        public string? NewName { get; set; }
        public bool IsFolder { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public bool Renamed { get; set; }
        public string? ErrorMessage { get; set; }
        public string Path { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            return ((FileRow)obj).Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}