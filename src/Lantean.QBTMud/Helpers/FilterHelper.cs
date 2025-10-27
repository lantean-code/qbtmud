using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Helpers
{
    public static class FilterHelper
    {
        public const string TAG_ALL = "All";
        public const string TAG_UNTAGGED = "Untagged";
        public const string CATEGORY_ALL = "All";
        public const string CATEGORY_UNCATEGORIZED = "Uncategorized";
        public const string TRACKER_ALL = "All";
        public const string TRACKER_TRACKERLESS = "Trackerless";
        public const string TRACKER_ERROR = "Error";
        public const string TRACKER_WARNING = "Warning";
        public const string TRACKER_ANNOUNCE_ERROR = "Announce error";

        public static IEnumerable<Torrent> Filter(this IEnumerable<Torrent> torrents, FilterState filterState)
        {
            return torrents.Where(t => FilterStatus(t, filterState.Status))
                .Where(t => FilterTag(t, filterState.Tag))
                .Where(t => FilterCategory(t, filterState.Category, filterState.UseSubcategories))
                .Where(t => FilterTracker(t, filterState.Tracker))
                .Where(t => FilterTerms(t.Name, filterState.Terms));
        }

        public static HashSet<string> ToHashesHashSet(this IEnumerable<Torrent> torrents)
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

        public static bool ContainsAllTerms(string text, IEnumerable<string> terms)
        {
            return terms.Any(t =>
            {
                var term = t;
                var isTermRequired = term[0] == '+';
                var isTermExcluded = term[0] == '-';

                if (isTermRequired || isTermExcluded)
                {
                    if (term.Length == 1)
                    {
                        return true;
                    }
                    term = term[1..];
                }

                var textContainsTerm = text.Contains(term, StringComparison.OrdinalIgnoreCase);
                return isTermExcluded ? !textContainsTerm : textContainsTerm;
            });
        }

        public static bool FilterTerms(string field, string? terms)
        {
            if (terms is null || terms == "")
            {
                return true;
            }

            return ContainsAllTerms(field, terms.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        public static bool FilterTerms(Torrent torrent, string? terms)
        {
            if (terms is null || terms == "")
            {
                return true;
            }

            return ContainsAllTerms(torrent.Name, terms.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        public static bool FilterTracker(Torrent torrent, string tracker)
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

            var torrentTracker = torrent.Tracker ?? string.Empty;
            if (IsTrackerUrl(tracker))
            {
                return string.Equals(NormalizeTrackerKey(torrentTracker), NormalizeTrackerKey(tracker), StringComparison.OrdinalIgnoreCase);
            }

            var torrentHost = GetTrackerHost(torrentTracker);
            return string.Equals(torrentHost, tracker, StringComparison.OrdinalIgnoreCase);
        }

        public static bool FilterCategory(Torrent torrent, string category, bool useSubcategories)
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

        public static bool FilterTag(Torrent torrent, string tag)
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

        public static bool FilterStatus(Torrent torrent, Status status)
        {
            return FilterStatus(torrent.State, torrent.UploadSpeed, status);
        }

        public static bool FilterStatus(string state, long uploadSpeed, Status status)
        {
            bool inactive = false;
            switch (status)
            {
                case Status.All:
                    return true;

                case Status.Downloading:
                    if (state != "downloading" && !state.Contains("DL"))
                    {
                        return false;
                    }
                    break;

                case Status.Seeding:
                    if (state != "uploading" && state != "forcedUP" && state != "stalledUP" && state != "queuedUP" && state != "checkingUP")
                    {
                        return false;
                    }
                    break;

                case Status.Completed:
                    if (state != "uploading" && !state.Contains("UP"))
                    {
                        return false;
                    }

                    break;

                case Status.Stopped:
                    if (state != "stoppedDL" && state != "stoppedUP")
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
                    if (state == "stalledDL")
                    {
                        check = uploadSpeed > 0;
                    }
                    else
                    {
                        check = state == "metaDL" || state == "forcedMetaDL" || state == "downloading" || state == "forcedDL" || state == "uploading" || state == "forcedUP";
                    }

                    if (check == inactive)
                    {
                        return false;
                    }
                    break;

                case Status.Stalled:
                    if (state != "stalledUP" && state != "stalledDL")
                    {
                        return false;
                    }
                    break;

                case Status.StalledUploading:
                    if (state != "stalledUP")
                    {
                        return false;
                    }
                    break;

                case Status.StalledDownloading:
                    if (state != "stalledDL")
                    {
                        return false;
                    }
                    break;

                case Status.Checking:
                    if (state != "checkingUP" && state != "checkingDL" && state != "checkingResumeData")
                    {
                        return false;
                    }
                    break;

                case Status.Errored:
                    if (state != "error" && state != "unknown" && state != "missingFiles")
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        private static string NormalizeTrackerKey(string tracker)
        {
            if (string.IsNullOrEmpty(tracker))
            {
                return string.Empty;
            }

            return tracker.TrimEnd('/');
        }

        private static string GetTrackerHost(string tracker)
        {
            if (Uri.TryCreate(tracker, UriKind.Absolute, out var trackerUri))
            {
                return trackerUri.Host;
            }

            if (string.IsNullOrEmpty(tracker))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(string.Concat("http://", tracker), UriKind.Absolute, out var fallbackUri))
            {
                return fallbackUri.Host;
            }

            return tracker;
        }

        private static bool IsTrackerUrl(string tracker)
        {
            if (string.IsNullOrEmpty(tracker))
            {
                return false;
            }

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
