using System.Text;

namespace Lantean.QBitTorrentClient
{
    public class QueryBuilder
    {
        private readonly IList<KeyValuePair<string, string>> _parameters;

        public QueryBuilder()
        {
            _parameters = [];
        }

        public QueryBuilder(IList<KeyValuePair<string, string>> parameters)
        {
            _parameters = parameters;
        }

        public QueryBuilder Add(string key, string value)
        {
            _parameters.Add(new KeyValuePair<string, string>(key, value));
            return this;
        }

        public QueryBuilder AddIfNotNullOrEmpty(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _parameters.Add(new KeyValuePair<string, string>(key, value));
            }

            return this;
        }

        public string ToQueryString()
        {
            if (_parameters.Count == 0)
            {
                return string.Empty;
            }

            var queryString = new StringBuilder();
            for (int i = 0; i < _parameters.Count; i++)
            {
                var kvp = _parameters[i];
                if (i == 0)
                {
                    queryString.Append('?');
                }
                else
                {
                    queryString.Append('&');
                }
                queryString.Append(Uri.EscapeDataString(kvp.Key));
                queryString.Append('=');
                queryString.Append(Uri.EscapeDataString(kvp.Value));
            }

            return queryString.ToString();
        }

        public override string ToString()
        {
            return ToQueryString();
        }
    }
}