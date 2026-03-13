using Lantean.QBTMud.Configuration;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates absolute URLs for qbtmud routes based on the configured routing mode.
    /// </summary>
    internal sealed class InternalUrlProvider : IInternalUrlProvider
    {
        private readonly NavigationManager _navigationManager;
        private readonly RoutingMode _routingMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalUrlProvider"/> class.
        /// </summary>
        /// <param name="navigationManager">The navigation manager.</param>
        /// <param name="routingMode">The configured routing mode.</param>
        public InternalUrlProvider(NavigationManager navigationManager, RoutingMode routingMode)
        {
            ArgumentNullException.ThrowIfNull(navigationManager);

            _navigationManager = navigationManager;
            _routingMode = routingMode;
        }

        /// <inheritdoc />
        public string GetAbsoluteUrl(string? path = null, string? query = null)
        {
            var normalizedPath = NormalizePath(path);
            var normalizedQuery = NormalizeQuery(query);
            var trimmedBaseUri = _navigationManager.BaseUri.TrimEnd('/');

            if (_routingMode == RoutingMode.Hash)
            {
                var hashPath = normalizedPath.Length == 0
                    ? "#/"
                    : string.Concat("#/", normalizedPath);

                return BuildUrl(trimmedBaseUri, hashPath, normalizedQuery);
            }

            return BuildUrl(trimmedBaseUri, normalizedPath, normalizedQuery);
        }

        private static string BuildUrl(string trimmedBaseUri, string normalizedPath, string normalizedQuery)
        {
            var url = string.IsNullOrEmpty(normalizedPath)
                ? string.Concat(trimmedBaseUri, "/")
                : string.Concat(trimmedBaseUri, "/", normalizedPath);

            if (string.IsNullOrEmpty(normalizedQuery))
            {
                return url;
            }

            return string.Concat(url, "?", normalizedQuery);
        }

        private static string NormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Trim().TrimStart('/');
        }

        private static string NormalizeQuery(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return string.Empty;
            }

            return query.Trim().TrimStart('?');
        }
    }
}
