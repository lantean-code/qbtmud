using Lantean.QBTMud.Models;
using QBittorrent.ApiClient.Models;
using MudMainData = Lantean.QBTMud.Models.MainData;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the result of a shell session load attempt.
    /// </summary>
    public sealed record ShellSessionLoadResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShellSessionLoadResult"/> class.
        /// </summary>
        /// <param name="outcome">The load outcome.</param>
        /// <param name="appSettings">The loaded app settings.</param>
        /// <param name="preferences">The loaded qBittorrent preferences.</param>
        /// <param name="version">The loaded qBittorrent version string.</param>
        /// <param name="mainData">The loaded shell main-data snapshot.</param>
        /// <param name="requestId">The next qBittorrent request identifier.</param>
        public ShellSessionLoadResult(
            ShellSessionLoadOutcome outcome,
            AppSettings? appSettings = null,
            Preferences? preferences = null,
            string? version = null,
            MudMainData? mainData = null,
            int requestId = 0)
        {
            Outcome = outcome;
            AppSettings = appSettings;
            Preferences = preferences;
            Version = version;
            MainData = mainData;
            RequestId = requestId;
        }

        /// <summary>
        /// Gets the load outcome.
        /// </summary>
        public ShellSessionLoadOutcome Outcome { get; init; }

        /// <summary>
        /// Gets the loaded app settings.
        /// </summary>
        public AppSettings? AppSettings { get; init; }

        /// <summary>
        /// Gets the loaded qBittorrent preferences.
        /// </summary>
        public Preferences? Preferences { get; init; }

        /// <summary>
        /// Gets the loaded qBittorrent version string.
        /// </summary>
        public string? Version { get; init; }

        /// <summary>
        /// Gets the loaded shell main-data snapshot.
        /// </summary>
        public MudMainData? MainData { get; init; }

        /// <summary>
        /// Gets the next qBittorrent request identifier.
        /// </summary>
        public int RequestId { get; init; }
    }
}
