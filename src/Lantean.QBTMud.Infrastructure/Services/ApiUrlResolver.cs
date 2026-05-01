using Lantean.QBTMud.Application.Services;
using Microsoft.Extensions.Options;

namespace Lantean.QBTMud.Infrastructure.Services
{
    /// <summary>
    /// Resolves absolute URLs for the configured qBittorrent Web API endpoint.
    /// </summary>
    public sealed class ApiUrlResolver : IApiUrlResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiUrlResolver"/> class.
        /// </summary>
        /// <param name="options">The API URL resolver configuration.</param>
        public ApiUrlResolver(IOptions<ApiUrlResolverOptions> options)
            : this(GetApiBaseAddress(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiUrlResolver"/> class.
        /// </summary>
        /// <param name="apiBaseAddress">The qBittorrent Web API base address.</param>
        public ApiUrlResolver(Uri apiBaseAddress)
        {
            ArgumentNullException.ThrowIfNull(apiBaseAddress);

            if (!apiBaseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException("The API base address must be absolute.", nameof(apiBaseAddress));
            }

            ApiBaseAddress = EnsureTrailingSlash(apiBaseAddress);
        }

        /// <inheritdoc />
        public Uri ApiBaseAddress { get; }

        /// <inheritdoc />
        public string BuildAbsoluteUrl(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            var trimmedPath = relativePath.Trim();
            trimmedPath = trimmedPath.TrimStart('/');
            if (trimmedPath.Length == 0)
            {
                throw new ArgumentException("The relative path must not be empty.", nameof(relativePath));
            }

            return new Uri(ApiBaseAddress, trimmedPath).AbsoluteUri;
        }

        private static Uri EnsureTrailingSlash(Uri uri)
        {
            var builder = new UriBuilder(uri);
            if (!builder.Path.EndsWith('/'))
            {
                builder.Path = string.Concat(builder.Path, "/");
            }

            return builder.Uri;
        }

        private static Uri GetApiBaseAddress(IOptions<ApiUrlResolverOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.Value.ApiBaseAddress
                ?? throw new InvalidOperationException("The API base address option must be configured.");
        }
    }
}
