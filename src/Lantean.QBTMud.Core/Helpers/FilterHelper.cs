using System.Text.RegularExpressions;
using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient.Models;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Core.Helpers
{
    public static class FilterHelper
    {
        // NUL separates built-in filters from valid user-defined names.
        public const string TAG_ALL = "\0All";
        public const string TAG_UNTAGGED = "\0Untagged";
        public const string CATEGORY_ALL = "\0All";
        public const string CATEGORY_UNCATEGORIZED = "\0Uncategorized";
        public const string TRACKER_ALL = "All";
        public const string TRACKER_TRACKERLESS = "Trackerless";
        public const string TRACKER_ERROR = "Error";
        public const string TRACKER_WARNING = "Warning";
        public const string TRACKER_ANNOUNCE_ERROR = "Announce error";

        private static readonly TimeSpan _regexMatchTimeout = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Filters torrents using their current tracker when no tracker mapping is available.
        /// </summary>
        /// <param name="torrents">The torrents to filter.</param>
        /// <param name="filterState">The selected filters.</param>
        /// <returns>The matching torrents.</returns>
        public static IEnumerable<MudTorrent> Filter(this IEnumerable<MudTorrent> torrents, FilterState filterState)
        {
            return torrents.Filter(filterState, null);
        }

        /// <summary>
        /// Filters torrents using the complete tracker membership supplied by synchronisation.
        /// </summary>
        /// <param name="torrents">The torrents to filter.</param>
        /// <param name="filterState">The selected filters.</param>
        /// <param name="trackers">The tracker-to-hash mapping, or null to use current trackers.</param>
        /// <returns>The matching torrents.</returns>
        public static IEnumerable<MudTorrent> Filter(this IEnumerable<MudTorrent> torrents, FilterState filterState, IReadOnlyDictionary<string, IReadOnlyList<string>>? trackers)
        {
            return torrents.Where(t => FilterStatus(t, filterState.Status))
                .Where(t => FilterTag(t, filterState.Tag))
                .Where(t => FilterCategory(t, filterState.Category, filterState.UseSubcategories))
                .FilterTracker(filterState.Tracker, trackers)
                .Where(t => FilterTerms(t, filterState));
        }

        public static HashSet<string> ToHashesHashSet(this IEnumerable<MudTorrent> torrents)
        {
            return torrents.Select(t => t.Hash).ToHashSet();
        }

        public static bool AddIfTrue(this HashSet<string> hashSet, string value, bool condition)
        {
            if (condition)
            {
                return hashSet.Add(value);
            }

            return false;
        }

        public static bool RemoveIfTrue(this HashSet<string> hashSet, string value, bool condition)
        {
            if (condition)
            {
                return hashSet.Remove(value);
            }

            return false;
        }

        public static bool AddIfTrueOrRemove(this HashSet<string> hashSet, string value, bool condition)
        {
            if (condition)
            {
                return hashSet.Add(value);
            }
            else
            {
                return hashSet.Remove(value);
            }
        }

        public static bool ContainsAllTerms(string text, IEnumerable<string> terms, bool useRegex)
        {
            var target = text ?? string.Empty;

            foreach (var rawTerm in terms)
            {
                if (string.IsNullOrEmpty(rawTerm))
                {
                    continue;
                }

                var term = rawTerm;
                var isExclude = false;
                if (term[0] == '+' || term[0] == '-')
                {
                    isExclude = term[0] == '-';
                    if (term.Length == 1)
                    {
                        continue;
                    }

                    term = term[1..];
                }

                if (isExclude)
                {
                    if (MatchesTerm(target, term, useRegex))
                    {
                        return false;
                    }

                    continue;
                }

                if (!MatchesTerm(target, term, useRegex))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool FilterTerms(string field, string? terms)
        {
            return FilterTerms(field, terms, useRegex: false, isRegexValid: true);
        }

        public static bool FilterTerms(string field, string? terms, bool useRegex, bool isRegexValid)
        {
            if (string.IsNullOrWhiteSpace(terms))
            {
                return true;
            }

            if (useRegex && !isRegexValid)
            {
                return true;
            }

            var value = field ?? string.Empty;
            var tokens = terms.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return ContainsAllTerms(value, tokens, useRegex);
        }

        public static bool FilterTerms(MudTorrent torrent, FilterState filterState)
        {
            return FilterTerms(GetFilterFieldValue(torrent, filterState.FilterField), filterState.Terms, filterState.UseRegex, filterState.IsRegexValid);
        }

        public static bool FilterTerms(MudTorrent torrent, string? terms)
        {
            return FilterTerms(torrent.Name, terms, useRegex: false, isRegexValid: true);
        }

        private static string GetFilterFieldValue(MudTorrent torrent, TorrentFilterField field)
        {
            return field switch
            {
                TorrentFilterField.SavePath => torrent.SavePath ?? string.Empty,
                _ => torrent.Name ?? string.Empty,
            };
        }

        private static bool MatchesTerm(string text, string term, bool useRegex)
        {
            if (!useRegex)
            {
                return text.Contains(term, StringComparison.OrdinalIgnoreCase);
            }

            try
            {
                return Regex.IsMatch(text, term, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, _regexMatchTimeout);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Tests a tracker filter against the torrent's current tracker and synthetic states.
        /// </summary>
        /// <param name="torrent">The torrent to test.</param>
        /// <param name="tracker">The selected tracker URL, host or synthetic filter.</param>
        /// <returns>Whether the torrent matches.</returns>
        public static bool FilterTracker(MudTorrent torrent, string tracker)
        {
            if (tracker == TRACKER_ALL)
            {
                return true;
            }

            if (tracker == TRACKER_TRACKERLESS)
            {
                if (torrent.TrackersCount == 0)
                {
                    return true;
                }

                return string.IsNullOrEmpty(torrent.Tracker);
            }

            if (tracker == TRACKER_ERROR)
            {
                return torrent.HasTrackerError;
            }

            if (tracker == TRACKER_WARNING)
            {
                return torrent.HasTrackerWarning;
            }

            if (tracker == TRACKER_ANNOUNCE_ERROR)
            {
                return torrent.HasOtherAnnounceError;
            }

            if (string.IsNullOrEmpty(tracker))
            {
                return string.IsNullOrEmpty(torrent.Tracker);
            }

            return MatchesTracker(torrent.Tracker ?? string.Empty, tracker);
        }

        /// <summary>
        /// Filters by complete tracker membership, building the matching hash set once per query.
        /// </summary>
        /// <param name="torrents">The torrents to filter.</param>
        /// <param name="tracker">The selected tracker URL, host or synthetic filter.</param>
        /// <param name="trackers">The tracker-to-hash mapping, or null to use current trackers.</param>
        /// <returns>The matching torrents.</returns>
        public static IEnumerable<MudTorrent> FilterTracker(this IEnumerable<MudTorrent> torrents, string tracker, IReadOnlyDictionary<string, IReadOnlyList<string>>? trackers)
        {
            if (trackers is null || string.IsNullOrEmpty(tracker)
                || tracker is TRACKER_ALL or TRACKER_TRACKERLESS or TRACKER_ERROR or TRACKER_WARNING or TRACKER_ANNOUNCE_ERROR)
            {
                return torrents.Where(torrent => FilterTracker(torrent, tracker));
            }

            var hashes = trackers.Where(entry => MatchesTracker(entry.Key, tracker))
                .SelectMany(entry => entry.Value).ToHashSet(StringComparer.Ordinal);
            return torrents.Where(torrent => hashes.Contains(torrent.Hash));
        }

        private static bool MatchesTracker(string torrentTracker, string tracker)
        {
            if (IsTrackerUrl(tracker))
            {
                return string.Equals(NormalizeTrackerKey(torrentTracker), NormalizeTrackerKey(tracker), StringComparison.OrdinalIgnoreCase);
            }

            var torrentHost = GetTrackerHost(torrentTracker);
            return string.Equals(torrentHost, tracker, StringComparison.OrdinalIgnoreCase);
        }

        public static bool FilterCategory(MudTorrent torrent, string category, bool useSubcategories)
        {
            switch (category)
            {
                case CATEGORY_ALL:
                    return true;

                case CATEGORY_UNCATEGORIZED:
                    if (!string.IsNullOrEmpty(torrent.Category))
                    {
                        return false;
                    }

                    return true;

                default:
                    if (string.IsNullOrEmpty(torrent.Category))
                    {
                        return false;
                    }

                    if (!useSubcategories)
                    {
                        return string.Equals(torrent.Category, category, StringComparison.Ordinal);
                    }

                    if (string.Equals(torrent.Category, category, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    var prefix = string.Concat(category, "/");
                    return torrent.Category.StartsWith(prefix, StringComparison.Ordinal);
            }
        }

        public static bool FilterTag(MudTorrent torrent, string tag)
        {
            if (tag == TAG_ALL)
            {
                return true;
            }

            if (tag == TAG_UNTAGGED)
            {
                return torrent.Tags.Count == 0;
            }

            return torrent.Tags.Contains(tag);
        }

        public static bool FilterStatus(MudTorrent torrent, Status status)
        {
            return FilterStatus(torrent.State, torrent.UploadSpeed, status);
        }

        public static bool FilterStatus(TorrentState? state, long uploadSpeed, Status status)
        {
            bool inactive = false;
            switch (status)
            {
                case Status.All:
                    return true;

                case Status.Downloading:
                    if (state is not TorrentState.Downloading
                        and not TorrentState.ForcedDownloading
                        and not TorrentState.DownloadingMetadata
                        and not TorrentState.ForcedDownloadingMetadata
                        and not TorrentState.StalledDownloading
                        and not TorrentState.QueuedDownloading
                        and not TorrentState.CheckingDownloading
                        and not TorrentState.StoppedDownloading)
                    {
                        return false;
                    }
                    break;

                case Status.Seeding:
                    if (state is not TorrentState.Uploading
                        and not TorrentState.ForcedUploading
                        and not TorrentState.StalledUploading
                        and not TorrentState.QueuedUploading
                        and not TorrentState.CheckingUploading)
                    {
                        return false;
                    }
                    break;

                case Status.Completed:
                    if (state is not TorrentState.Uploading
                        and not TorrentState.ForcedUploading
                        and not TorrentState.StalledUploading
                        and not TorrentState.QueuedUploading
                        and not TorrentState.CheckingUploading
                        and not TorrentState.StoppedUploading)
                    {
                        return false;
                    }

                    break;

                case Status.Stopped:
                    if (state is not TorrentState.StoppedDownloading and not TorrentState.StoppedUploading)
                    {
                        return false;
                    }
                    break;

                case Status.Inactive:
                case Status.Active:
                    if (status == Status.Inactive)
                    {
                        inactive = true;
                    }
                    bool check;
                    if (state == TorrentState.StalledDownloading)
                    {
                        check = uploadSpeed > 0;
                    }
                    else
                    {
                        check = state is TorrentState.DownloadingMetadata
                            or TorrentState.ForcedDownloadingMetadata
                            or TorrentState.Downloading
                            or TorrentState.ForcedDownloading
                            or TorrentState.Uploading
                            or TorrentState.ForcedUploading;
                    }

                    if (check == inactive)
                    {
                        return false;
                    }
                    break;

                case Status.Stalled:
                    if (state is not TorrentState.StalledUploading and not TorrentState.StalledDownloading)
                    {
                        return false;
                    }
                    break;

                case Status.StalledUploading:
                    if (state != TorrentState.StalledUploading)
                    {
                        return false;
                    }
                    break;

                case Status.StalledDownloading:
                    if (state != TorrentState.StalledDownloading)
                    {
                        return false;
                    }
                    break;

                case Status.Checking:
                    if (state is not TorrentState.CheckingUploading
                        and not TorrentState.CheckingDownloading
                        and not TorrentState.CheckingResumeData)
                    {
                        return false;
                    }
                    break;

                case Status.Errored:
                    if (state is not TorrentState.Error
                        and not TorrentState.Unknown
                        and not TorrentState.MissingFiles)
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        private static string NormalizeTrackerKey(string tracker)
        {
            return tracker.TrimEnd('/');
        }

        private static string GetTrackerHost(string tracker)
        {
            if (Uri.TryCreate(tracker, UriKind.Absolute, out var trackerUri))
            {
                return trackerUri.Host;
            }

            if (Uri.TryCreate(string.Concat("http://", tracker), UriKind.Absolute, out var fallbackUri))
            {
                return fallbackUri.Host;
            }

            return tracker;
        }

        private static bool IsTrackerUrl(string tracker)
        {
            return tracker.Contains("://", StringComparison.Ordinal);
        }

        public static string GetStatusName(this string status)
        {
            return status switch
            {
                nameof(Status.StalledUploading) => "Stalled Uploading",
                nameof(Status.StalledDownloading) => "Stalled Downloading",
                _ => status,
            };
        }
    }
}
