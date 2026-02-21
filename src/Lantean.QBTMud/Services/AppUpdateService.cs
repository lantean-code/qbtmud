using Lantean.QBTMud.Models;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides qbtmud update information based on GitHub releases.
    /// </summary>
    public sealed class AppUpdateService : IAppUpdateService
    {
        private const string LatestReleasePath = "repos/lantean-code/qbtmud/releases/latest";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly SemaphoreSlim _cacheSemaphore = new SemaphoreSlim(1, 1);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAppBuildInfoService _appBuildInfoService;
        private readonly ILogger<AppUpdateService> _logger;
        private AppUpdateStatus? _cachedStatus;
        private DateTime _cacheTimestampUtc;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppUpdateService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="appBuildInfoService">The build info service.</param>
        /// <param name="logger">The logger instance.</param>
        public AppUpdateService(
            IHttpClientFactory httpClientFactory,
            IAppBuildInfoService appBuildInfoService,
            ILogger<AppUpdateService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _appBuildInfoService = appBuildInfoService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<AppUpdateStatus> GetUpdateStatusAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && _cachedStatus is not null && (DateTime.UtcNow - _cacheTimestampUtc) < CacheDuration)
            {
                return _cachedStatus;
            }

            await _cacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!forceRefresh && _cachedStatus is not null && (DateTime.UtcNow - _cacheTimestampUtc) < CacheDuration)
                {
                    return _cachedStatus;
                }

                var currentBuild = _appBuildInfoService.GetCurrentBuildInfo();
                var latestRelease = await TryGetLatestReleaseAsync(cancellationToken);
                var status = BuildStatus(currentBuild, latestRelease);

                _cachedStatus = status;
                _cacheTimestampUtc = DateTime.UtcNow;
                return status;
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        private async Task<AppReleaseInfo?> TryGetLatestReleaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GitHubReleases");
                using var response = await client.GetAsync(LatestReleasePath, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<GitHubReleasePayload>(cancellationToken);
                if (payload is null || string.IsNullOrWhiteSpace(payload.TagName) || string.IsNullOrWhiteSpace(payload.HtmlUrl))
                {
                    return null;
                }

                var publishedAtUtc = payload.PublishedAt?.UtcDateTime;
                var releaseName = string.IsNullOrWhiteSpace(payload.Name) ? payload.TagName : payload.Name;
                return new AppReleaseInfo(payload.TagName.Trim(), releaseName.Trim(), payload.HtmlUrl.Trim(), publishedAtUtc);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is NotSupportedException || ex is FormatException || ex is JsonException)
            {
                _logger.LogDebug(ex, "Unable to load qbtmud latest release information.");
                return null;
            }
        }

        private static AppUpdateStatus BuildStatus(AppBuildInfo currentBuild, AppReleaseInfo? latestRelease)
        {
            var checkedAtUtc = DateTime.UtcNow;
            if (latestRelease is null)
            {
                return new AppUpdateStatus(currentBuild, null, false, false, checkedAtUtc);
            }

            var canCompareCurrent = SemanticVersionInfo.TryParse(currentBuild.Version, out var currentVersion);
            var canCompareLatest = SemanticVersionInfo.TryParse(latestRelease.TagName, out var latestVersion);
            var canCompare = canCompareCurrent && canCompareLatest;
            if (!canCompare)
            {
                return new AppUpdateStatus(currentBuild, latestRelease, false, false, checkedAtUtc);
            }

            var comparison = currentVersion.CompareTo(latestVersion);
            var updateAvailable = comparison < 0;

            return new AppUpdateStatus(currentBuild, latestRelease, updateAvailable, true, checkedAtUtc);
        }

        private sealed class GitHubReleasePayload
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("published_at")]
            public DateTimeOffset? PublishedAt { get; set; }
        }

        private readonly struct SemanticVersionInfo
        {
            public SemanticVersionInfo(int major, int minor, int patch, int revision, bool isPreRelease)
            {
                Major = major;
                Minor = minor;
                Patch = patch;
                Revision = revision;
                IsPreRelease = isPreRelease;
            }

            public int Major { get; }

            public int Minor { get; }

            public int Patch { get; }

            public int Revision { get; }

            public bool IsPreRelease { get; }

            public int CompareTo(SemanticVersionInfo other)
            {
                var majorComparison = Major.CompareTo(other.Major);
                if (majorComparison != 0)
                {
                    return majorComparison;
                }

                var minorComparison = Minor.CompareTo(other.Minor);
                if (minorComparison != 0)
                {
                    return minorComparison;
                }

                var patchComparison = Patch.CompareTo(other.Patch);
                if (patchComparison != 0)
                {
                    return patchComparison;
                }

                var revisionComparison = Revision.CompareTo(other.Revision);
                if (revisionComparison != 0)
                {
                    return revisionComparison;
                }

                if (IsPreRelease == other.IsPreRelease)
                {
                    return 0;
                }

                return IsPreRelease ? -1 : 1;
            }

            public static bool TryParse(string value, out SemanticVersionInfo version)
            {
                version = default;
                if (string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }

                var normalizedValue = value.Trim();
                if (normalizedValue.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedValue = normalizedValue[1..];
                }

                var metadataSeparatorIndex = normalizedValue.IndexOf('+');
                if (metadataSeparatorIndex >= 0)
                {
                    normalizedValue = normalizedValue[..metadataSeparatorIndex];
                }

                var preReleaseSeparatorIndex = normalizedValue.IndexOf('-');
                var isPreRelease = preReleaseSeparatorIndex >= 0;
                if (isPreRelease)
                {
                    normalizedValue = normalizedValue[..preReleaseSeparatorIndex];
                }

                var segments = normalizedValue.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length is < 2 or > 4)
                {
                    return false;
                }

                var values = new[] { 0, 0, 0, 0 };
                for (var i = 0; i < segments.Length; i++)
                {
                    if (!int.TryParse(segments[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i]))
                    {
                        return false;
                    }
                }

                version = new SemanticVersionInfo(values[0], values[1], values[2], values[3], isPreRelease);
                return true;
            }
        }
    }
}
