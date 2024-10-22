using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Collections.ObjectModel;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class RenameFilesDialog
    {
        private const string _preferencesStorageKey = "RenameFilesDialog.MultiRenamePreferences";

        protected static readonly Dictionary<AppliesTo, string> AppliesToItems = Enum.GetValues<AppliesTo>().ToDictionary(v => v, v => v.GetDescriptionAttributeOrDefault());

        private readonly Dictionary<string, RenderFragment<RowContext<FileRow>>> _columnRenderFragments = [];

        private string? _sortColumn;
        private SortDirection _sortDirection;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Hash { get; set; }

        protected HashSet<FileRow> SelectedItems { get; set; } = [];

        protected IEnumerable<FileRow> FileList { get; private set; } = [];

        protected IEnumerable<FileRow> Files => GetFiles();

        protected HashSet<string> ExpandedNodes { get; set; } = [];

        public RenameFilesDialog()
        {
            _columnRenderFragments.Add("Name", NameColumn);
            //_columnRenderFragments.Add("Replacement", ReplacementColumn);
        }

        private ReadOnlyCollection<FileRow> GetFiles()
        {
            var maxLevel = FileList.Max(f => f.Level);
            // this is a flat file structure
            if (maxLevel == 0)
            {
                return FileList.OrderByDirection(_sortDirection, GetSortSelector()).ToList().AsReadOnly();
            }

            var list = new List<FileRow>();

            var rootItems = FileList.Where(c => c.Level == 0).OrderByDirection(_sortDirection, GetSortSelector()).ToList();
            foreach (var item in rootItems)
            {
                list.Add(item);

                if (item.IsFolder)
                {
                    var level = 0;
                    var descendants = GetChildren(item, level);
                    foreach (var descendant in descendants)
                    {
                        list.Add(descendant);
                    }
                }
            }

            return list.AsReadOnly();
        }

        private IEnumerable<FileRow> GetRenamedItems(IEnumerable<ContentItem> items)
        {
            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                SelectedItems,
                Search,
                UseRegex,
                Replacement,
                MatchAllOccurrences,
                CaseSensitive,
                AppliesToValue,
                IncludeFiles,
                IncludeFolders,
                ReplaceAll,
                FileEnumerationStart);

            foreach (var item in items)
            {
                var fileRow = CreateFileRow(item);
                if (renamedFiles.TryGetValue(fileRow.Name, out var renamedRow))
                {
                    yield return renamedRow;
                }
                else
                {
                    yield return fileRow;
                }
            }
        }

        private static FileRow CreateFileRow(ContentItem item)
        {
            var fileRow = new FileRow
            {
                IsFolder = item.IsFolder,
                Level = item.Level,
                NewName = item.DisplayName,
                OriginalName = item.DisplayName,
                Name = item.Name,
                Path = item.Path,
            };

            return fileRow;
        }

        //private IEnumerable<(FileRow, string)> GetRenamedItemsOld(IEnumerable<FileRow> items)
        //{
        //    foreach (var item in items)
        //    {
        //        if (!SelectedItems.Contains(item))
        //        {
        //            yield return (item, "");
        //            continue;
        //        }

        //        if ((item.IsFolder && !IncludeFolders) || (!item.IsFolder && !IncludeFiles))
        //        {
        //            yield return (item, "");
        //            continue;
        //        }

        //        if (string.IsNullOrEmpty(Search) || string.IsNullOrEmpty(Replacement))
        //        {
        //            yield return (item, "");
        //            continue;
        //        }

        //        var newName = item.DisplayName;
        //        switch (AppliesToValue)
        //        {
        //            case AppliesTo.FilenameExtension:
        //                newName = ReplaceInString(item.DisplayName);
        //                break;

        //            case AppliesTo.Filename:
        //                var extension = Path.GetExtension(item.DisplayName);
        //                var filename = Path.GetFileNameWithoutExtension(item.DisplayName);
        //                filename = ReplaceInString(filename);
        //                newName = filename + extension;
        //                break;

        //            case AppliesTo.Extension:
        //                extension = Path.GetExtension(item.DisplayName);
        //                string newExtension = ReplaceInString(extension);
        //                newName = Path.GetFileNameWithoutExtension(item.DisplayName) + newExtension;
        //                break;
        //        }

        //        yield return (item, newName);
        //    }
        //}

        //private string ReplaceInString(string input)
        //{
        //    if (UseRegex)
        //    {
        //        var regex = new Regex(Search, CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
        //        return MatchAllOccurrences
        //            ? regex.Replace(input, Replacement)
        //            : regex.Replace(input, Replacement, 1);
        //    }
        //    else
        //    {
        //        var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        //        if (MatchAllOccurrences)
        //        {
        //            return input.Replace(Search, Replacement, comparison);
        //        }
        //        else
        //        {
        //            var index = input.IndexOf(Search, comparison);
        //            if (index == -1)
        //            {
        //                return input;
        //            }
        //            return input.Remove(index, Search.Length).Insert(index, Replacement);
        //        }
        //    }
        //}

        protected async Task DoRename()
        {
            if (Hash is null)
            {
                return;
            }

            await RenameAsync(Hash, Files.Where(f => f.Renamed).ToList());
        }

        private async Task RenameAsync(string hash, List<FileRow> matchedFiles)
        {
            if (matchedFiles == null || matchedFiles.Count == 0 || string.IsNullOrEmpty(hash))
            {
                return;
            }

            for (int i = matchedFiles.Count - 1; i >= 0; i--)
            {
                var file = matchedFiles[i];
                var (success, errorMessage) = await RenameItem(hash, file);
                file.Renamed = success;
                file.ErrorMessage = errorMessage;
            }
        }

        private async Task<(bool, string?)> RenameItem(string hash, FileRow match)
        {
            if (match.NewName == match.OriginalName)
            {
                // Original file name is identical to Renamed
                return (false, null);
            }

            var newName = match.NewName!;

            var parentPath = Path.GetDirectoryName(match.Name);
            var oldPath = string.IsNullOrEmpty(parentPath)
                ? match.OriginalName
                : Path.Combine(parentPath, match.OriginalName);
            var newPath = string.IsNullOrEmpty(parentPath)
                ? newName
                : Path.Combine(parentPath, newName);

            try
            {
                if (match.IsFolder)
                {
                    await ApiClient.RenameFile(hash, oldPath, newPath);
                }
                else
                {
                    await ApiClient.RenameFile(hash, oldPath, newPath);
                }
                return (true, null);
            }
            catch (HttpRequestException ex)
            {
                return (false, ex.Message != "" ? ex.Message : $"Error with request: {ex.StatusCode}.");
            }
        }

        private IEnumerable<FileRow> GetChildren(FileRow folder, int level)
        {
            level++;
            var descendantsKey = folder.Name.GetDescendantsKey(level);

            foreach (var item in FileList.Where(f => f.Name.StartsWith(descendantsKey) && f.Level == level).OrderByDirection(_sortDirection, GetSortSelector()))
            {
                if (item.IsFolder)
                {
                    var descendants = GetChildren(item, level);
                    // if the filter returns some results then show folder item
                    if (descendants.Any())
                    {
                        yield return item;
                    }

                    // then show children
                    foreach (var descendant in descendants)
                    {
                        yield return descendant;
                    }
                }
                else
                {
                    yield return item;
                }
            }
        }

        private Func<FileRow, object?> GetSortSelector()
        {
            var sortSelector = ColumnsDefinitions.Find(c => c.Id == _sortColumn)?.SortSelector;

            return sortSelector ?? (i => i.Name);
        }

        protected void SortColumnChanged(string sortColumn)
        {
            _sortColumn = sortColumn;
        }

        protected void SortDirectionChanged(SortDirection sortDirection)
        {
            _sortDirection = sortDirection;
        }

        protected void SelectedItemsChanged(HashSet<FileRow> selectedItems)
        {
            SelectedItems = selectedItems;
        }

        protected string Search { get; set; } = "";

        protected void SearchChanged(string value)
        {
            Search = value;
        }

        protected bool UseRegex { get; set; }

        protected void UseRegexChanged(bool value)
        {
            UseRegex = value;
        }

        protected bool MatchAllOccurrences { get; set; }

        protected void MatchAllOccurrencesChanged(bool value)
        {
            MatchAllOccurrences = value;
        }

        protected bool CaseSensitive { get; set; }

        protected void CaseSensitiveChanged(bool value)
        {
            CaseSensitive = value;
        }

        protected string Replacement { get; set; } = "";

        protected void ReplacementChanged(string value)
        {
            Replacement = value;
        }

        protected AppliesTo AppliesToValue { get; set; } = AppliesTo.FilenameExtension;

        protected void AppliesToChanged(AppliesTo value)
        {
            AppliesToValue = value;
        }

        protected bool IncludeFiles { get; set; } = true;

        protected void IncludeFilesChanged(bool value)
        {
            IncludeFiles = value;
        }

        protected bool IncludeFolders { get; set; }

        protected void IncludeFoldersChanged(bool value)
        {
            IncludeFolders = value;
        }

        protected int FileEnumerationStart { get; set; }

        protected void FileEnumerationStartChanged(int value)
        {
            FileEnumerationStart = value;
        }

        protected bool ReplaceAll { get; set; }

        protected void ReplaceAllChanged(bool value)
        {
            ReplaceAll = value;
        }

        protected override async Task OnInitializedAsync()
        {
            var preferences = await LocalStorage.GetItemAsync<MultiRenamePreferences>(_preferencesStorageKey) ?? new();

            if (preferences.RememberPreferences)
            {
                Search = preferences.Search;
                UseRegex = preferences.UseRegex;
                MatchAllOccurrences = preferences.MatchAllOccurrences;
                CaseSensitive = preferences.CaseSensitive;
                Replacement = preferences.Replace;
                AppliesToValue = preferences.AppliesTo;
                IncludeFiles = preferences.IncludeFiles;
                IncludeFolders = preferences.IncludeFolders;
                FileEnumerationStart = preferences.FileEnumerationStart;
                ReplaceAll = preferences.ReplaceAll;
            }

            if (Hash is null)
            {
                return;
            }

            var contents = await ApiClient.GetTorrentContents(Hash);
            FileList = GetRenamedItems(DataManager.CreateContentsList(contents).Values);
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close();
        }

        protected IEnumerable<ColumnDefinition<FileRow>> Columns => GetColumnDefinitions();

        private IEnumerable<ColumnDefinition<FileRow>> GetColumnDefinitions()
        {
            foreach (var columnDefinition in ColumnsDefinitions)
            {
                if (_columnRenderFragments.TryGetValue(columnDefinition.Header, out var fragment))
                {
                    columnDefinition.RowTemplate = fragment;
                }

                yield return columnDefinition;
            }
        }

        public static List<ColumnDefinition<FileRow>> ColumnsDefinitions { get; } =
        [
            ColumnDefinitionHelper.CreateColumnDefinition("Name", c => c.Name, NameColumn, width: 400, initialDirection: SortDirection.Ascending, classFunc: c => c.IsFolder ? "px-0 pt-0 pb-2" : "pa-2"),
            ColumnDefinitionHelper.CreateColumnDefinition<FileRow>("Replacement", c => c.NewName),
        ];

        private sealed class MultiRenamePreferences
        {
            public bool RememberPreferences { get; set; } = false;

            public string Search { get; set; } = "";

            public bool UseRegex { get; set; } = false;

            public bool MatchAllOccurrences { get; set; } = false;

            public bool CaseSensitive { get; set; } = false;

            public string Replace { get; set; } = "";

            public AppliesTo AppliesTo { get; set; } = AppliesTo.FilenameExtension;

            public bool IncludeFiles { get; set; } = true;

            public bool IncludeFolders { get; set; } = false;

            public int FileEnumerationStart { get; set; } = 0;

            public bool ReplaceAll { get; set; } = false;
        }
    }
}