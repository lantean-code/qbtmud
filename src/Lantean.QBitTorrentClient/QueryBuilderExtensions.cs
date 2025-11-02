using System.Globalization;
using System.Linq;

namespace Lantean.QBitTorrentClient
{
    public static class QueryBuilderExtensions
    {
        public static QueryBuilder Add(this QueryBuilder builder, string key, bool value)
        {
            return builder.Add(key, value ? "true" : "false");
        }

        public static QueryBuilder Add(this QueryBuilder builder, string key, int value)
        {
            return builder.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public static QueryBuilder Add(this QueryBuilder builder, string key, long value)
        {
            return builder.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public static QueryBuilder Add(this QueryBuilder builder, string key, DateTimeOffset value, bool useSeconds = true)
        {
            return builder.Add(key, useSeconds ? value.ToUnixTimeSeconds() : value.ToUnixTimeMilliseconds());
        }

        public static QueryBuilder Add(this QueryBuilder builder, string key, Enum value)
        {
            return builder.Add(key, value.ToString());
        }

        public static QueryBuilder AddPipeSeparated<T>(this QueryBuilder builder, string key, IEnumerable<T> values)
        {
            return builder.Add(key, JoinWithInvariant(values, '|'));
        }

        public static QueryBuilder AddCommaSeparated<T>(this QueryBuilder builder, string key, IEnumerable<T> values)
        {
            return builder.Add(key, JoinWithInvariant(values, ','));
        }

        private static string JoinWithInvariant<T>(IEnumerable<T> values, char separator)
        {
            return string.Join(separator, values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty));
        }
    }
}
