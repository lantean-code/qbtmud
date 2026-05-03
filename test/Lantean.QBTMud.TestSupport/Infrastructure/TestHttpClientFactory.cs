namespace Lantean.QBTMud.TestSupport.Infrastructure
{
    internal static class TestHttpClientFactory
    {
        public static HttpClient CreateClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
        }
    }
}
