using System.Net;
using Lantean.QBTMud.Interop;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Services
{
    internal sealed class MagnetLinkService : IMagnetLinkService
    {
        private const int _maxDownloadLength = 8 * 1024;

        private readonly IJSRuntime _jsRuntime;
        private readonly IInternalUrlProvider _internalUrlProvider;

        public MagnetLinkService(IJSRuntime jsRuntime, IInternalUrlProvider internalUrlProvider)
        {
            _jsRuntime = jsRuntime;
            _internalUrlProvider = internalUrlProvider;
        }

        public async Task<MagnetHandlerRegistrationResult> RegisterHandler(string handlerName)
        {
            var templateUrl = _internalUrlProvider.GetAbsoluteUrl(query: "download=%s");

            try
            {
                var result = await _jsRuntime.RegisterMagnetHandler(templateUrl, handlerName);

                return new MagnetHandlerRegistrationResult
                {
                    Status = MapStatus(result.Status),
                    Message = result.Message,
                };
            }
            catch (JSException exception)
            {
                return new MagnetHandlerRegistrationResult
                {
                    Status = MagnetHandlerRegistrationStatus.Unknown,
                    Message = exception.Message,
                };
            }
        }

        public string? ExtractDownloadLink(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return null;
            }

            if (!Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri))
            {
                return null;
            }

            var downloadValue = ExtractDownloadParameterFromComponent(absoluteUri.Fragment);
            if (string.IsNullOrWhiteSpace(downloadValue))
            {
                downloadValue = ExtractDownloadParameterFromComponent(absoluteUri.Query);
            }

            if (string.IsNullOrWhiteSpace(downloadValue))
            {
                return null;
            }

            var decoded = WebUtility.UrlDecode(downloadValue);
            if (!IsSupportedDownloadLink(decoded))
            {
                return null;
            }

            return decoded;
        }

        public bool IsSupportedDownloadLink(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Length > _maxDownloadLength)
            {
                return false;
            }

            if (value.IndexOfAny(['\r', '\n']) >= 0)
            {
                return false;
            }

            if (value.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                return value.Contains("xt=urn:btih", StringComparison.OrdinalIgnoreCase);
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return uri.AbsolutePath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ExtractDownloadParameterFromComponent(string component)
        {
            if (string.IsNullOrEmpty(component))
            {
                return null;
            }

            var trimmed = component;
            if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("?", StringComparison.Ordinal))
            {
                trimmed = trimmed[1..];
            }

            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            if (trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                var routeQueryIndex = trimmed.IndexOf('?', StringComparison.Ordinal);
                if (routeQueryIndex < 0 || routeQueryIndex >= trimmed.Length - 1)
                {
                    return null;
                }

                trimmed = trimmed[(routeQueryIndex + 1)..];
            }

            foreach (var segment in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = segment.IndexOf('=');
                if (separatorIndex < 0)
                {
                    if (string.Equals(segment, "download", StringComparison.OrdinalIgnoreCase))
                    {
                        return string.Empty;
                    }

                    continue;
                }

                var key = segment[..separatorIndex];
                if (!string.Equals(key, "download", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (separatorIndex >= segment.Length - 1)
                {
                    return string.Empty;
                }

                return segment[(separatorIndex + 1)..];
            }

            return null;
        }

        private static MagnetHandlerRegistrationStatus MapStatus(string? status)
        {
            var normalizedStatus = status?.ToLowerInvariant();
            return normalizedStatus switch
            {
                "success" => MagnetHandlerRegistrationStatus.Success,
                "insecure" => MagnetHandlerRegistrationStatus.Insecure,
                "unsupported" => MagnetHandlerRegistrationStatus.Unsupported,
                _ => MagnetHandlerRegistrationStatus.Unknown,
            };
        }
    }
}
