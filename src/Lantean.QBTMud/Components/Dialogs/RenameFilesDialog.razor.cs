using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using System.Collections.ObjectModel;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class RenameFilesDialog
    {
        private const string _preferencesStorageKey = "RenameFilesDialog.MultiRenamePreferences";

        protected static readonly Dictionary<AppliesTo, string> AppliesToItems = Enum.GetValues<AppliesTo>().ToDictionary(v => v, v => v.GetDescriptionAttributeOrDefault());

        private string? _sortColumn;
        private SortDirection _sortDirection;
        private List<ColumnDefinition<FileRow>>? _columnDefinitions;
        private List<FileRow> _sourceFileList = [];

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected ISettingsStorageService SettingsStorage { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Hash { get; set; }

        protected HashSet<FileRow> SelectedItems { get; set; } = [];

        protected List<FileRow> FileList { get; private set; } = [];

        protected IEnumerable<FileRow> Files => GetFiles();

        protected HashSet<string> ExpandedNodes { get; set; } = [];

        private ReadOnlyCollection<FileRow> GetFiles()
        {
            if (!FileList.Any())
            {
                return new ReadOnlyCollection<FileRow>([]);
            }

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

        private List<FileRow> GetRenamedItems()
        {
            var selectedNames = SelectedItems.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                _sourceFileList
                    .Where(item => selectedNames.Contains(item.Name))
                    .Select(CloneFileRow)
                    .ToList(),
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

            var previewRows = new List<FileRow>(_sourceFileList.Count);
            foreach (var item in _sourceFileList)
            {
                var fileRow = CloneFileRow(item);
                var renamedRow = renamedFiles.FirstOrDefault(r => r.Name == fileRow.Name);
                if (renamedRow is not null)
                {
                    previewRows.Add(renamedRow);
                }
                else
                {
                    previewRows.Add(fileRow);
                }
            }

            SelectedItems = previewRows
                .Where(item => selectedNames.Contains(item.Name))
                .ToHashSet();

            return previewRows;
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

            ApiResult renameResult;
            if (match.IsFolder)
            {
                renameResult = await ApiClient.RenameFolderAsync(hash, oldPath, newPath);
            }
            else
            {
                renameResult = await ApiClient.RenameFileAsync(hash, oldPath, newPath);
            }

            if (renameResult.IsSuccess)
            {
                return (true, null);
            }

            return (false, GetFailureMessage(renameResult.Failure));
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
            var sortSelector = GetColumnDefinitions().FirstOrDefault(c => c.Id == _sortColumn)?.SortSelector;

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
            RefreshPreview();
        }

        protected string Search { get; set; } = "";

        protected void SearchChanged(string value)
        {
            Search = value;
            RefreshPreview();
        }

        protected bool UseRegex { get; set; }

        protected async Task UseRegexChanged(bool value)
        {
            UseRegex = value;
            RefreshPreview();

            await UpdatePreferences(p => p.UseRegex = value);
        }

        protected bool MatchAllOccurrences { get; set; }

        protected async Task MatchAllOccurrencesChanged(bool value)
        {
            MatchAllOccurrences = value;
            RefreshPreview();

            await UpdatePreferences(p => p.MatchAllOccurrences = value);
        }

        protected bool CaseSensitive { get; set; }

        protected async Task CaseSensitiveChanged(bool value)
        {
            CaseSensitive = value;
            RefreshPreview();

            await UpdatePreferences(p => p.CaseSensitive = value);
        }

        protected string Replacement { get; set; } = "";

        protected async Task ReplacementChanged(string value)
        {
            Replacement = value;
            RefreshPreview();

            await UpdatePreferences(p => p.Replace = value);
        }

        protected AppliesTo AppliesToValue { get; set; } = AppliesTo.FilenameExtension;

        protected async Task AppliesToChanged(AppliesTo value)
        {
            AppliesToValue = value;
            RefreshPreview();

            await UpdatePreferences(p => p.AppliesTo = value);
        }

        protected bool IncludeFiles { get; set; } = true;

        protected async Task IncludeFilesChanged(bool value)
        {
            IncludeFiles = value;
            RefreshPreview();

            await UpdatePreferences(p => p.IncludeFiles = value);
        }

        protected bool IncludeFolders { get; set; }

        protected async Task IncludeFoldersChanged(bool value)
        {
            IncludeFolders = value;
            RefreshPreview();

            await UpdatePreferences(p => p.IncludeFolders = value);
        }

        protected int FileEnumerationStart { get; set; }

        protected async Task FileEnumerationStartChanged(int value)
        {
            FileEnumerationStart = value;
            RefreshPreview();

            await UpdatePreferences(p => p.FileEnumerationStart = value);
        }

        protected bool ReplaceAll { get; set; }

        protected async Task ReplaceAllChanged(bool value)
        {
            ReplaceAll = value;
            RefreshPreview();

            await UpdatePreferences(p => p.ReplaceAll = value);
        }

        protected bool RememberMultiRenameSettings { get; set; }

        protected async Task RememberMultiRenameSettingsChanged(bool value)
        {
            RememberMultiRenameSettings = value;

            await UpdatePreferences(p => p.RememberPreferences = value);
        }

        private async Task UpdatePreferences(Action<MultiRenamePreferences> updateAction)
        {
            var preferences = await SettingsStorage.GetItemAsync<MultiRenamePreferences>(_preferencesStorageKey) ?? new();
            updateAction(preferences);
            if (preferences.RememberPreferences)
            {
                await SettingsStorage.SetItemAsync(_preferencesStorageKey, preferences);
            }
            else
            {
                await SettingsStorage.RemoveItemAsync(_preferencesStorageKey);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var preferences = await SettingsStorage.GetItemAsync<MultiRenamePreferences>(_preferencesStorageKey) ?? new();

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

            var contentsResult = await ApiClient.GetTorrentContentsAsync(Hash);
            if (!contentsResult.TryGetValue(out var fileContents))
            {
                FileList = [];
                await ApiFeedbackWorkflow.HandleFailureAsync(contentsResult);
                return;
            }

            _sourceFileList = DataManager.CreateContentsList(fileContents).Values.Select(CreateFileRow).ToList();
            RefreshPreview();
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected async Task Submit()
        {
            if (Hash is null)
            {
                MudDialog.Close();

                return;
            }

            var previewRows = Files.ToList();
            var renamedFiles = previewRows
                .Where(file => !string.Equals(file.NewName, file.OriginalName, StringComparison.Ordinal))
                .ToList();

            var hasFailure = false;

            if (ReplaceAll)
            {
                foreach (var renamedFile in renamedFiles.Where(f => !f.IsFolder))
                {
                    var (success, errorMessage) = await RenameItem(Hash, renamedFile);
                    renamedFile.Renamed = success;
                    renamedFile.ErrorMessage = errorMessage;
                    hasFailure |= !success && !string.IsNullOrWhiteSpace(errorMessage);
                }

                foreach (var renamedFile in renamedFiles.Where(f => f.IsFolder).OrderBy(f => f.Path.Split(Extensions.DirectorySeparator)))
                {
                    var (success, errorMessage) = await RenameItem(Hash, renamedFile);
                    renamedFile.Renamed = success;
                    renamedFile.ErrorMessage = errorMessage;
                    hasFailure |= !success && !string.IsNullOrWhiteSpace(errorMessage);
                }
            }
            else
            {
                var first = renamedFiles.FirstOrDefault();
                if (first is not null)
                {
                    var (success, errorMessage) = await RenameItem(Hash, first);
                    first.Renamed = success;
                    first.ErrorMessage = errorMessage;
                    hasFailure = !success && !string.IsNullOrWhiteSpace(errorMessage);
                }
            }

            if (hasFailure)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            MudDialog.Close();
        }

        protected IEnumerable<ColumnDefinition<FileRow>> Columns => GetColumnDefinitions();

        private List<ColumnDefinition<FileRow>> GetColumnDefinitions()
        {
            _columnDefinitions ??= BuildColumnDefinitions();

            return _columnDefinitions;
        }

        private List<ColumnDefinition<FileRow>> BuildColumnDefinitions()
        {
            return
            [
                ColumnDefinitionHelper.CreateColumnDefinition(
                    LanguageLocalizer.Translate("TrackerListWidget", "Original"),
                    c => c.Name,
                    NameColumn,
                    width: 400,
                    initialDirection: SortDirection.Ascending,
                    classFunc: c => c.IsFolder ? "px-0 pt-0 pb-2" : "pa-2",
                    id: "original"),
                ColumnDefinitionHelper.CreateColumnDefinition<FileRow>(
                    LanguageLocalizer.Translate("TrackerListWidget", "Renamed"),
                    c => c.NewName,
                    id: "renamed"),
            ];
        }

        private string GetFailureMessage(ApiFailure? failure)
        {
            if (!string.IsNullOrWhiteSpace(failure?.UserMessage))
            {
                return failure.UserMessage;
            }

            return LanguageLocalizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.");
        }

        private void RefreshPreview()
        {
            if (_sourceFileList.Count == 0)
            {
                FileList = [];
                return;
            }

            FileList = GetRenamedItems();
        }

        private static FileRow CloneFileRow(FileRow row)
        {
            return new FileRow
            {
                ErrorMessage = row.ErrorMessage,
                IsFolder = row.IsFolder,
                Level = row.Level,
                Name = row.Name,
                NewName = row.NewName,
                OriginalName = row.OriginalName,
                Path = row.Path,
                Renamed = row.Renamed,
            };
        }

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
