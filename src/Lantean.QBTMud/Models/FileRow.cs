namespace Lantean.QBTMud.Models
{
    public class FileRow
    {
        public required string OriginalName { get; set; }
        public string? NewName { get; set; }
        public bool IsFolder { get; set; }
        public required string Name { get; set; }
        public int Level { get; set; }
        public bool Renamed { get; set; }
        public string? ErrorMessage { get; set; }
        public required string Path { get; set; }

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
