using Lantean.QBTMud.Models;
using System.Reflection;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Resolves qbtmud build metadata embedded in the assembly.
    /// </summary>
    public sealed class AppBuildInfoService : IAppBuildInfoService
    {
        private const string BuildVersionMetadataKey = "QbtMudBuildVersion";
        private readonly Assembly _assembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuildInfoService"/> class.
        /// </summary>
        public AppBuildInfoService()
            : this(typeof(AppBuildInfoService).Assembly)
        {
        }

        internal AppBuildInfoService(Assembly assembly)
        {
            _assembly = assembly;
        }

        /// <inheritdoc />
        public AppBuildInfo GetCurrentBuildInfo()
        {
            var metadataVersion = _assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(attribute => string.Equals(attribute.Key, BuildVersionMetadataKey, StringComparison.Ordinal))
                .Select(attribute => attribute.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (!string.IsNullOrWhiteSpace(metadataVersion))
            {
                return new AppBuildInfo(metadataVersion.Trim(), "AssemblyMetadata");
            }

            var informationalVersion = _assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                return new AppBuildInfo(informationalVersion.Trim(), "InformationalVersion");
            }

            var version = _assembly.GetName().Version?.ToString();
            if (!string.IsNullOrWhiteSpace(version))
            {
                return new AppBuildInfo(version.Trim(), "AssemblyVersion");
            }

            return new AppBuildInfo("unknown", "Unavailable");
        }
    }
}
