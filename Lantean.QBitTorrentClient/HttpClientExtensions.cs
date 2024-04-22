namespace Lantean.QBitTorrentClient
{
    internal static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, string requestUrl, FormUrlEncodedBuilder builder)
        {
            return httpClient.PostAsync(requestUrl, builder.ToFormUrlEncodedContent());
        }

        public static Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, string requestUrl, QueryBuilder builder)
        {
            return httpClient.GetAsync($"{requestUrl}{builder.ToQueryString()}");
        }
    }
}
