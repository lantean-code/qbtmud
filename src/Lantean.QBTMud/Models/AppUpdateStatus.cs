namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents update-check state for qbtmud.
    /// </summary>
    public sealed record AppUpdateStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppUpdateStatus"/> class.
        /// </summary>
        /// <param name="currentBuild">The current local build information.</param>
        /// <param name="latestRelease">The latest release information, if available.</param>
        /// <param name="isUpdateAvailable">A value indicating whether a newer release is available.</param>
        /// <param name="canCompareVersions">A value indicating whether the version comparison result is reliable.</param>
        /// <param name="checkedAtUtc">The UTC timestamp when the status was generated.</param>
        public AppUpdateStatus(AppBuildInfo currentBuild, AppReleaseInfo? latestRelease, bool isUpdateAvailable, bool canCompareVersions, DateTime checkedAtUtc)
        {
            CurrentBuild = currentBuild;
            LatestRelease = latestRelease;
            IsUpdateAvailable = isUpdateAvailable;
            CanCompareVersions = canCompareVersions;
            CheckedAtUtc = checkedAtUtc;
        }

        /// <summary>
        /// Gets the current local build information.
        /// </summary>
        public AppBuildInfo CurrentBuild { get; init; }

        /// <summary>
        /// Gets the latest release information, if available.
        /// </summary>
        public AppReleaseInfo? LatestRelease { get; init; }

        /// <summary>
        /// Gets a value indicating whether a newer release is available.
        /// </summary>
        public bool IsUpdateAvailable { get; init; }

        /// <summary>
        /// Gets a value indicating whether the version comparison result is reliable.
        /// </summary>
        public bool CanCompareVersions { get; init; }

        /// <summary>
        /// Gets the UTC timestamp when the status was generated.
        /// </summary>
        public DateTime CheckedAtUtc { get; init; }
    }
}
