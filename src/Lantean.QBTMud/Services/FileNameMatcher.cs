using Lantean.QBTMud.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Lantean.QBTMud.Services
{
    public record MatchOptions
    {
        public bool MatchAllOccurrences { get; set; } = false;

        public bool CaseSensitive { get; set; } = false;
    }

    public record ReplaceOptions
    {
        public AppliesTo AppliesToOption { get; set; } = AppliesTo.FilenameExtension;
        public bool IncludeFiles { get; set; } = true;
        public bool IncludeFolders { get; set; } = false;
        public bool ReplaceAll { get; set; } = false;
        public int FileEnumerationStart { get; set; } = 0;
    }

    public static class FileNameMatcher
    {
        private const int _maxMatchesPerFile = 250;

        public static Dictionary<string, FileRow> GetRenamedFiles(
            IEnumerable<FileRow> files,
            string search,
            bool useRegex,
            string replacement,
            bool matchAllOccurrences,
            bool caseSensitive,
            AppliesTo appliesToOption,
            bool includeFiles,
            bool includeFolders,
            bool replaceAll,
            int fileEnumerationStart)
        {
            var matchedFiles = new Dictionary<string, FileRow>();

            if (string.IsNullOrEmpty(search))
            {
                return [];
            }

            // Setup regex options
            var options = RegexOptions.None;
            if (!caseSensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }

            // Build regex pattern
            var pattern = useRegex ? search : Regex.Escape(search);
            var regex = new Regex(pattern, options);

            var fileEnumeration = fileEnumerationStart;

            foreach (var row in files)
            {
                // Filter files and folders
                if (!row.IsFolder && !includeFiles)
                {
                    continue;
                }
                if (row.IsFolder && !includeFolders)
                {
                    continue;
                }

                // Extract file name and extension
                var fileExtension = Path.GetExtension(row.OriginalName);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(row.OriginalName);

                var targetString = string.Empty;
                var offset = 0;

                switch (appliesToOption)
                {
                    case AppliesTo.FilenameExtension:
                        targetString = fileNameWithoutExt + fileExtension;
                        break;

                    case AppliesTo.Filename:
                        targetString = fileNameWithoutExt;
                        break;

                    case AppliesTo.Extension:
                        targetString = fileExtension;
                        offset = fileNameWithoutExt.Length;
                        break;
                }

                // Find matches based on MatchAllOccurrences
                var matches = new List<Match>();
                var matchCount = 0;

                if (matchAllOccurrences)
                {
                    foreach (Match match in regex.Matches(targetString))
                    {
                        matches.Add(match);
                        matchCount++;
                        if (matchCount >= _maxMatchesPerFile)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    var match = regex.Match(targetString);
                    if (match.Success)
                    {
                        matches.Add(match);
                    }
                }

                if (matches.Count == 0)
                {
                    continue;
                }

                var renamed = row.OriginalName;

                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    var match = matches[i];
                    var replacementValue = replacement;

                    // Replace numerical groups
                    for (var g = 0; g < match.Groups.Count; g++)
                    {
                        var groupValue = match.Groups[g].Value;
                        if (string.IsNullOrEmpty(groupValue))
                        {
                            continue;
                        }

                        replacementValue = ReplaceGroup(replacementValue, $"${g}", groupValue, "\\", false);
                    }

                    // Replace named groups
                    foreach (var groupName in regex.GetGroupNames())
                    {
                        if (int.TryParse(groupName, out _))
                        {
                            continue; // Skip numerical group names
                        }

                        var groupValue = match.Groups[groupName].Value;
                        replacementValue = ReplaceGroup(replacementValue, $"${groupName}", groupValue, "\\", false);
                    }

                    // Replace auxiliary variables (e.g., $d, $dd, $ddd, etc.)
                    var v = new string('d', 8);
                    while (v.Length > 0)
                    {
                        var fileCount = fileEnumeration.ToString().PadLeft(v.Length, '0');
                        replacementValue = ReplaceGroup(replacementValue, $"${v}", fileCount, "\\", false);
                        v = v.Substring(1);
                    }

                    // Remove empty $ variable
                    replacementValue = ReplaceGroup(replacementValue, "$", string.Empty, "\\");

                    var matchIndex = match.Index;
                    var matchLength = match.Length;
                    var startIndex = matchIndex + offset;
                    var endIndex = startIndex + matchLength;

                    renamed = ReplaceBetween(renamed, startIndex, endIndex, replacementValue);
                }

                row.NewName = renamed;
                fileEnumeration++;
                matchedFiles.Add(row.Name, row);
            }

            return matchedFiles;
        }

        // Helper methods
        private static string ReplaceGroup(string input, string search, string replacement, string escape, bool stripEscape = true)
        {
            var result = new StringBuilder();
            var i = 0;
            var inputLength = input.Length;
            var escapeLength = escape.Length;
            var searchLength = search.Length;

            while (i < inputLength)
            {
                // Check if the current index contains the escape string
                if (IsSubstringAt(input, i, escape))
                {
                    // Check if the escape is followed by the search string
                    if (IsSubstringAt(input, i + escapeLength, search))
                    {
                        if (stripEscape)
                        {
                            result.Append(search);
                            i += escapeLength + searchLength;
                        }
                        else
                        {
                            result.Append(escape + search);
                            i += escapeLength + searchLength;
                        }
                    }
                    else
                    {
                        result.Append(escape);
                        i += escapeLength;
                    }
                }
                else if (IsSubstringAt(input, i, search))
                {
                    result.Append(replacement);
                    i += searchLength;
                }
                else
                {
                    result.Append(input[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        private static string ReplaceBetween(string input, int start, int end, string replacement)
        {
            if (start < 0 || end > input.Length || start > end)
            {
                return input;
            }

            return string.Concat(input.AsSpan(0, start), replacement, input.AsSpan(end));
        }

        private static bool IsSubstringAt(string input, int index, string substring)
        {
            if (index + substring.Length > input.Length)
            {
                return false;
            }

            return input.Substring(index, substring.Length) == substring;
        }
    }
}