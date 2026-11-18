using System.Text;
using System.Text.RegularExpressions;
using Lantean.QBTMud.Core.Models;

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
        private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Creates rename previews by expanding replacement tokens once for each match.
        /// </summary>
        /// <param name="files">The files and folders to preview.</param>
        /// <param name="search">The search text or regular expression.</param>
        /// <param name="useRegex">Whether the search is a regular expression.</param>
        /// <param name="replacement">The replacement text and tokens.</param>
        /// <param name="matchAllOccurrences">Whether every occurrence is replaced.</param>
        /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
        /// <param name="appliesToOption">The part of each name to match.</param>
        /// <param name="includeFiles">Whether to include files.</param>
        /// <param name="includeFolders">Whether to include folders.</param>
        /// <param name="replaceAll">The selected submission mode.</param>
        /// <param name="fileEnumerationStart">The first enumeration value.</param>
        /// <returns>The matching rows with their preview names.</returns>
        public static IReadOnlyList<FileRow> GetRenamedFiles(
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
            var matchedFiles = new List<FileRow>();

            if (string.IsNullOrEmpty(search))
            {
                return matchedFiles;
            }

            // Setup regex options
            var options = RegexOptions.None;
            if (!caseSensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }

            // Build regex pattern
            var pattern = useRegex ? search : Regex.Escape(search);
            Regex regex;
            try
            {
                regex = new Regex(pattern, options, _regexTimeout);
            }
            catch (ArgumentException)
            {
                return matchedFiles;
            }

            var fileEnumeration = fileEnumerationStart;
            var groupNames = regex.GetGroupNames();
            var groupNumbers = regex.GetGroupNumbers();
            var namedGroups = groupNames.Where(name => !int.TryParse(name, out _)).OrderByDescending(name => name.Length).ToArray();

            try
            {
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
                        var replacementValue = ExpandReplacement(replacement, match, groupNumbers, namedGroups, fileEnumeration);

                        var matchIndex = match.Index;
                        var matchLength = match.Length;
                        var startIndex = matchIndex + offset;
                        var endIndex = startIndex + matchLength;

                        renamed = ReplaceBetween(renamed, startIndex, endIndex, replacementValue);
                    }

                    row.NewName = renamed;
                    fileEnumeration++;
                    matchedFiles.Add(row);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return matchedFiles;
            }

            return matchedFiles;
        }

        private static string ExpandReplacement(string replacement, Match match, IReadOnlyCollection<int> groupNumbers, IReadOnlyList<string> namedGroups, int fileEnumeration)
        {
            var result = new StringBuilder();
            for (var index = 0; index < replacement.Length; index++)
            {
                if (replacement[index] == '\\' && index + 1 < replacement.Length && replacement[index + 1] == '$')
                {
                    result.Append('$');
                    index++;
                    continue;
                }

                if (replacement[index] != '$')
                {
                    result.Append(replacement[index]);
                    continue;
                }

                var tokenStart = index + 1;
                var tokenEnd = tokenStart;
                while (tokenEnd < replacement.Length && char.IsAsciiDigit(replacement[tokenEnd]))
                {
                    tokenEnd++;
                }
                if (tokenEnd > tokenStart)
                {
                    var number = replacement[tokenStart..tokenEnd];
                    result.Append(int.TryParse(number, out var groupNumber) && groupNumbers.Contains(groupNumber) ? match.Groups[groupNumber].Value : number);
                    index = tokenEnd - 1;
                    continue;
                }

                var namedGroup = namedGroups.FirstOrDefault(name => replacement.AsSpan(tokenStart).StartsWith(name, StringComparison.Ordinal));
                if (namedGroup is not null)
                {
                    result.Append(match.Groups[namedGroup].Value);
                    index += namedGroup.Length;
                    continue;
                }

                while (tokenEnd < replacement.Length && tokenEnd - tokenStart < 8 && replacement[tokenEnd] == 'd')
                {
                    tokenEnd++;
                }
                if (tokenEnd > tokenStart)
                {
                    result.Append(fileEnumeration.ToString().PadLeft(tokenEnd - tokenStart, '0'));
                    index = tokenEnd - 1;
                }
            }

            return result.ToString();
        }

        private static string ReplaceBetween(string input, int start, int end, string replacement)
        {
            return string.Concat(input.AsSpan(0, start), replacement, input.AsSpan(end));
        }
    }
}
