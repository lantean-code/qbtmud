using AwesomeAssertions;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class AppUpdateStatusTests
    {
        [Fact]
        public void GIVEN_StatusWithoutLatestRelease_WHEN_Constructed_THEN_ExposesExpectedProperties()
        {
            var checkedAtUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var currentBuild = new AppBuildInfo("1.0.0", "AssemblyMetadata");

            var result = new AppUpdateStatus(
                currentBuild: currentBuild,
                latestRelease: null,
                isUpdateAvailable: false,
                canCompareVersions: true,
                checkedAtUtc: checkedAtUtc);

            result.CurrentBuild.Should().Be(currentBuild);
            result.LatestRelease.Should().BeNull();
            result.IsUpdateAvailable.Should().BeFalse();
            result.CanCompareVersions.Should().BeTrue();
            result.CheckedAtUtc.Should().Be(checkedAtUtc);
        }

        [Fact]
        public void GIVEN_StatusWithLatestRelease_WHEN_ToStringInvoked_THEN_IncludesKeyValues()
        {
            var checkedAtUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var currentBuild = new AppBuildInfo("1.0.0", "AssemblyMetadata");
            var latestRelease = new AppReleaseInfo("v2.0.0", "v2.0.0", "https://example.invalid", checkedAtUtc);

            var result = new AppUpdateStatus(
                currentBuild: currentBuild,
                latestRelease: latestRelease,
                isUpdateAvailable: true,
                canCompareVersions: true,
                checkedAtUtc: checkedAtUtc);

            result.ToString().Should().Contain("AppUpdateStatus");
            result.ToString().Should().Contain("CurrentBuild");
            result.ToString().Should().Contain("LatestRelease");
            result.ToString().Should().Contain("IsUpdateAvailable = True");
            result.ToString().Should().Contain("CanCompareVersions = True");
        }
    }
}
