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
            return builder.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, long value)
        {
            return builder.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, DateTimeOffset value, bool useSeconds = true)
        {
            return builder.Add(key, useSeconds ? value.ToUnixTimeSeconds() : value.ToUnixTimeMilliseconds());
        }

        public static FormUrlEncodedBuilder Add(this FormUrlEncodedBuilder builder, string key, float value)
        {
            return builder.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public static FormUrlEncodedBuilder Add<T>(this FormUrlEncodedBuilder builder, string key, T value) where T : struct, IConvertible
        {
            return builder.Add(key, value.ToInt32(CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
        }

        public static FormUrlEncodedBuilder AddAllOrPipeSeparated(this FormUrlEncodedBuilder builder, string key, bool? all = null, params string[] values)
        {
            if (all.GetValueOrDefault())
            {
                return builder.Add(key, "all");
            }

            return builder.Add(key, JoinWithInvariant(values, '|'));
        }

        public static FormUrlEncodedBuilder AddPipeSeparated<T>(this FormUrlEncodedBuilder builder, string key, IEnumerable<T> values)
        {
            return builder.Add(key, JoinWithInvariant(values, '|'));
        }

        public static FormUrlEncodedBuilder AddCommaSeparated<T>(this FormUrlEncodedBuilder builder, string key, IEnumerable<T> values)
        {
            return builder.Add(key, JoinWithInvariant(values, ','));
        }

        private static string JoinWithInvariant<T>(IEnumerable<T> values, char separator)
        {
            return string.Join(separator, values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty));
        }
    }
}
