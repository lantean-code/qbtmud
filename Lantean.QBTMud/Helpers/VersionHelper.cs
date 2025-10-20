namespace Lantean.QBTMud.Helpers
{
    internal static class VersionHelper
    {
        private static int? _version;

        private const int _defaultVersion = 5;

        public static int DefaultVersion => _defaultVersion;

        public static int GetMajorVersion(string? version)
        {
            if (_version is not null)
            {
                return _version.Value;
            }

            if (string.IsNullOrEmpty(version))
            {
                return _defaultVersion;
            }

            if (!Version.TryParse(version?.Replace("v", ""), out var theVersion))
            {
                return _defaultVersion;
            }

            _version = theVersion.Major;

            return _version.Value;
        }
    }
}