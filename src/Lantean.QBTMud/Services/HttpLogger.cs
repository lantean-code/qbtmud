using Microsoft.Extensions.Http.Logging;

namespace Lantean.QBTMud.Services
{
    public class HttpLogger : IHttpClientLogger
    {
        private readonly ILogger<HttpLogger> _logger;

        public HttpLogger(ILogger<HttpLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public object? LogRequestStart(HttpRequestMessage request)
        {
            //#if DEBUG
            //            _logger.LogInformation(
            //                "Sending '{Request.Method}' to '{Request.Host}{Request.Path}'",
            //                request.Method,
            //                request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            //                request.RequestUri!.PathAndQuery);
            //#endif
            return null;
        }

        public void LogRequestStop(
            object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
        {
            //#if DEBUG
            //            _logger.LogInformation(
            //                "Received '{Response.StatusCodeInt} {Response.StatusCodeString}' after {Response.ElapsedMilliseconds}ms",
            //                (int)response.StatusCode,
            //                response.StatusCode,
            //                elapsed.TotalMilliseconds.ToString("F1"));
            //#endif
        }

        public void LogRequestFailed(
            object? context,
            HttpRequestMessage request,
            HttpResponseMessage? response,
            Exception exception,
            TimeSpan elapsed)
        {
            _logger.LogError(
                exception,
                "Request towards '{Request.Host}{Request.Path}' failed after {Response.ElapsedMilliseconds}ms",
                request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
                request.RequestUri!.PathAndQuery,
                elapsed.TotalMilliseconds.ToString("F1"));
        }
    }
}