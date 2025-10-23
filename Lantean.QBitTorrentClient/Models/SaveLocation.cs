namespace Lantean.QBitTorrentClient.Models
{
    public class SaveLocation
    {
        public bool IsWatchedFolder { get; set; }

        public bool IsDefaultFolder { get; set; }

        public string? SavePath { get; set; }

        public static SaveLocation Create(object? value)
        {
            if (value is int intValue)
            {
                return Create(intValue);
            }
            else if (value is string stringValue)
            {
                return Create(stringValue);
            }

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        public static SaveLocation Create(int value)
        {
            if (value == 0)
            {
                return new SaveLocation
                {
                    IsWatchedFolder = true
                };
            }
            else if (value == 1)
            {
                return new SaveLocation
                {
                    IsDefaultFolder = true
                };
            }

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        public static SaveLocation Create(string? value)
        {
            if (value is null)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == "0")
            {
                return new SaveLocation
                {
                    IsWatchedFolder = true
                };
            }
            else if (value == "1")
            {
                return new SaveLocation
                {
                    IsDefaultFolder = true
                };
            }
            else
            {
                return new SaveLocation
                {
                    SavePath = value
                };
            }
        }

        public object ToValue()
        {
            if (IsWatchedFolder)
            {
                return 0;
            }
            else if (IsDefaultFolder)
            {
                return 1;
            }
            else if (SavePath is not null)
            {
                return SavePath;
            }

            throw new InvalidOperationException("Invalid value.");
        }

        public override string? ToString()
        {
            return ToValue().ToString();
        }
    }
}