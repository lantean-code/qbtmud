namespace Lantean.QBitTorrentClient
{
    public static class MultipartFormDataContentExtensions
    {
        public static void AddString(this MultipartFormDataContent content, string name, string value)
        {
            content.Add(new StringContent(value), name);
        }

        public static void AddString(this MultipartFormDataContent content, string name, bool value)
        {
            content.AddString(name, value ? "true" : "false");
        }

        public static void AddString(this MultipartFormDataContent content, string name, int value)
        {
            content.AddString(name, value.ToString());
        }

        public static void AddString(this MultipartFormDataContent content, string name, long value)
        {
            content.AddString(name, value.ToString());
        }

        public static void AddString(this MultipartFormDataContent content, string name, float value)
        {
            content.AddString(name, value.ToString());
        }

        public static void AddString(this MultipartFormDataContent content, string name, Enum value)
        {
            content.AddString(name, value.ToString());
        }

        public static void AddString(this MultipartFormDataContent content, string name, DateTimeOffset value, bool useSeconds = true)
        {
            content.AddString(name, useSeconds ? value.ToUnixTimeSeconds() : value.ToUnixTimeMilliseconds());
        }
    }
}