using System.Net;

namespace QbtMudTranslations
{
    internal sealed class TranslationBackendRejectedException : InvalidOperationException
    {
        public TranslationBackendRejectedException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
