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
                if (intValue == 0)
                {
                    return new SaveLocation
                    {
                        IsWatchedFolder = true
                    };
                }
                else if (intValue == 1)
                {
                    return new SaveLocation
                    {
                        IsDefaultFolder = true
                    };
                }
            }
            else if (value is string stringValue)
            {
                if (stringValue == "0")
                {
                    return new SaveLocation
                    {
                        IsWatchedFolder = true
                    };
                }
                else if (stringValue == "1")
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
                        SavePath = stringValue
                    };
                }
            }

            throw new ArgumentOutOfRangeException(nameof(value));
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