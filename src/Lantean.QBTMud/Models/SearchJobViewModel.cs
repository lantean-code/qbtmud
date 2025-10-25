using System;
using System.Collections.Generic;
using System.Linq;
using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Models
{
    public class SearchJobViewModel
    {
        private readonly List<SearchResult> _results = new();
        private readonly List<string> _plugins;

        public SearchJobViewModel(int id, string pattern, IReadOnlyCollection<string> plugins, string category)
        {
            Id = id;
            Pattern = pattern;
            var comparer = StringComparer.OrdinalIgnoreCase;
            _plugins = plugins.Select(p => p).OrderBy(p => p, comparer).ToList();
            Plugins = _plugins.AsReadOnly();
            Category = category;
            CreatedOn = DateTimeOffset.UtcNow;
            Status = "Running";
        }

        public int Id { get; }

        public string Pattern { get; }

        public IReadOnlyCollection<string> Plugins { get; }

        public string Category { get; }

        public string Status { get; private set; }

        public int Total { get; private set; }

        public DateTimeOffset CreatedOn { get; }

        public DateTimeOffset? CompletedOn { get; private set; }

        public string? ErrorMessage { get; private set; }

        public IReadOnlyList<SearchResult> Results => _results;

        public bool IsRunning => string.Equals(Status, "Running", StringComparison.OrdinalIgnoreCase);

        public bool IsStopped => string.Equals(Status, "Stopped", StringComparison.OrdinalIgnoreCase);

        public bool IsErrored => string.Equals(Status, "Error", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(ErrorMessage);

        public int CurrentOffset => _results.Count;

        public void ResetResults(bool clearError = true)
        {
            _results.Clear();
            Total = 0;
            if (clearError)
            {
                ErrorMessage = null;
            }
        }

        public void UpdateStatus(string status, int total)
        {
            Status = status;
            Total = total;
            if (!IsRunning)
            {
                CompletedOn ??= DateTimeOffset.UtcNow;
            }
        }

        public void AppendResults(IEnumerable<SearchResult> results)
        {
            foreach (var result in results)
            {
                _results.Add(result);
            }
        }

        public void SetError(string message)
        {
            Status = "Error";
            ErrorMessage = message;
            CompletedOn ??= DateTimeOffset.UtcNow;
        }

        public bool Matches(string pattern, string category, IReadOnlyCollection<string> plugins)
        {
            if (!string.Equals(Pattern, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(Category, category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (plugins.Count != Plugins.Count)
            {
                return false;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;
            return !_plugins.Except(plugins, comparer).Any()
                   && !plugins.Except(_plugins, comparer).Any();
        }
    }
}
