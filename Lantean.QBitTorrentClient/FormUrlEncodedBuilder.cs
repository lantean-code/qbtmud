namespace Lantean.QBitTorrentClient
{
    public class FormUrlEncodedBuilder
    {
        private readonly IList<KeyValuePair<string, string>> _parameters;

        public FormUrlEncodedBuilder()
        {
            _parameters = [];
        }

        public FormUrlEncodedBuilder(IList<KeyValuePair<string, string>> parameters)
        {
            _parameters = parameters;
        }

        public FormUrlEncodedBuilder Add(string key, string value)
        {
            _parameters.Add(new KeyValuePair<string, string>(key, value));
            return this;
        }

        public FormUrlEncodedBuilder AddIfNotNullOrEmpty(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _parameters.Add(new KeyValuePair<string, string>(key, value));
            }

            return this;
        }

        public FormUrlEncodedContent ToFormUrlEncodedContent()
        {
            return new FormUrlEncodedContent(_parameters);
        }
    }
}