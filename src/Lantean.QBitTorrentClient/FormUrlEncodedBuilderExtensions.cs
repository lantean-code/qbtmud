using System.Globalization;

namespace Lantean.QBitTorrentClient
{
    public static class FormUrlEncodedBuilderExtensions
    {
        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, bool value)
        {
            return builder.Add(key, value ? "true" : "false");
        }

        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, int value)
        {
            return builder.Add(key, value.ToString());
        }

        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, long value)
        {
            return builder.Add(key, value.ToString());
        }

        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, DateTimeOffset value, bool useSeconds = true)
        {
            return builder.Add(key, useSeconds ? value.ToUnixTimeSeconds() : value.ToUnixTimeMilliseconds());
        }

        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, float value)
        {
            return builder.Add(key, value.ToString());
        }

        public static FormUrlEncodedBuilder Add<T>(this FormUrlEncodedBuilder builder, string key, T value) where T : struct, IConvertible
        {
            return builder.Add(key, value.ToInt32(CultureInfo.InvariantCulture).ToString());
        }

        public static FormUrlEncodedBuilder AddAllOrPipeSeparated(this FormUrlEncodedBuilder builder, string key, bool? all = null, params string[] values)
        {
            return builder.Add(key, all.GetValueOrDefault() ? "all" : string.Join('|', values));
        }

        public static FormUrlEncodedBuilder AddPipeSeparated<T>(this FormUrlEncodedBuilder builder, string key, IEnumerable<T> values)
        {
            return builder.Add(key, string.Join('|', values));
        }

        public static FormUrlEncodedBuilder AddCommaSeparated<T>(this FormUrlEncodedBuilder builder, string key, IEnumerable<T> values)
        {
            return builder.Add(key, string.Join(',', values));
        }
    }
}