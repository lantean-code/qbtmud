namespace Lantean.QBTMud.Configuration
{
    internal static class ApiUrlConfiguration
    {
        private const string _apiBaseUrlConfigurationKey = "Api:BaseUrl";
        private const string _apiPathSuffix = "api/v2/";

        public static Uri GetApiBaseAddress(
            IConfiguration configuration,
            Uri applicationBaseAddress,
            Uri defaultApiHostBaseAddress)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(applicationBaseAddress);
            ArgumentNullException.ThrowIfNull(defaultApiHostBaseAddress);

            if (!applicationBaseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException("The application base address must be absolute.", nameof(applicationBaseAddress));
            }

            if (!defaultApiHostBaseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException("The default API host base address must be absolute.", nameof(defaultApiHostBaseAddress));
            }

            var configuredValue = configuration[_apiBaseUrlConfigurationKey];
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                return AppendApiPath(defaultApiHostBaseAddress);
            }

            var trimmedValue = configuredValue.Trim();
            if (!TryGetConfiguredBaseAddress(applicationBaseAddress, trimmedValue, out var configuredBaseAddress)
                || configuredBaseAddress is null)
            {
                return AppendApiPath(defaultApiHostBaseAddress);
            }

            configuredBaseAddress = RemoveQueryAndFragment(configuredBaseAddress);
            if (HasApiPath(configuredBaseAddress))
            {
                return EnsureTrailingSlash(configuredBaseAddress);
            }

            return AppendApiPath(configuredBaseAddress);
        }

        private static bool HasApiPath(Uri uri)
        {
            var path = uri.AbsolutePath;
            return path.EndsWith("/api/v2", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/api/v2/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetConfiguredBaseAddress(Uri applicationBaseAddress, string trimmedValue, out Uri? configuredBaseAddress)
        {
            configuredBaseAddress = null;

            if (trimmedValue.Contains("://", StringComparison.Ordinal))
            {
                if (!Uri.TryCreate(trimmedValue, UriKind.Absolute, out var absoluteUri) ||
                    !absoluteUri.IsAbsoluteUri ||
                    !IsSupportedScheme(absoluteUri))
                {
                    return false;
                }

                configuredBaseAddress = absoluteUri;
                return true;
            }

            if (!IsSupportedRelativeValue(trimmedValue))
            {
                return false;
            }

            if (!Uri.TryCreate(applicationBaseAddress, trimmedValue, out var relativeUri) ||
                relativeUri is null ||
                !relativeUri.IsAbsoluteUri ||
                !IsSupportedScheme(relativeUri))
            {
                return false;
            }

            configuredBaseAddress = relativeUri;
            return true;
        }

        private static bool IsSupportedRelativeValue(string value)
        {
            return value.StartsWith("/", StringComparison.Ordinal) ||
                value.StartsWith("./", StringComparison.Ordinal);
        }

        private static bool IsSupportedScheme(Uri uri)
        {
            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private static Uri AppendApiPath(Uri baseAddress)
        {
            return new Uri(EnsureTrailingSlash(baseAddress), _apiPathSuffix);
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

        private static Uri RemoveQueryAndFragment(Uri uri)
        {
            var builder = new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty
            };

            return builder.Uri;
        }
    }
}
