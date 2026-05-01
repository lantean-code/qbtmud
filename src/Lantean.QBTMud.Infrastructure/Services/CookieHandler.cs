using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Lantean.QBTMud.Infrastructure.Services
{
    public class CookieHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
