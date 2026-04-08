using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class PathBrowserDialog
    {
        private const int _pathChangeDebounceMs = 300;
        private readonly List<PathBrowseEntry> _entries = [];
        private string _currentPath = string.Empty;
        private bool _isLoading;
        private string? _loadError;
        private int _pathChangeVersion;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? InitialPath { get; set; }

        [Parameter]
        public DirectoryContentMode Mode { get; set; } = DirectoryContentMode.All;

        [Parameter]
        public bool AllowFolderSelection { get; set; } = true;

        protected bool IsLoading
        {
            get { return _isLoading; }
        }

        protected string? LoadError
        {
            get { return _loadError; }
        }

        protected bool CanNavigateUp
        {
            get { return !string.IsNullOrWhiteSpace(GetParentPath(_currentPath)); }
        }

        protected bool CanSelectFolder
        {
            get { return AllowFolderSelection && !string.IsNullOrWhiteSpace(_currentPath); }
        }

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(InitialPath))
            {
                _currentPath = InitialPath!;
            }
            else
            {
                _currentPath = await GetDefaultPathAsync();
            }

            await LoadEntriesAsync();
        }

        protected async Task Reload()
        {
            await LoadEntriesAsync();
        }

        protected async Task PathChanged(string value)
        {
            _currentPath = value;
            await DebounceLoadEntriesAsync();
        }

        protected async Task NavigateUp()
        {
            var parent = GetParentPath(_currentPath);
            if (string.IsNullOrWhiteSpace(parent))
            {
                return;
            }

            _currentPath = parent;
            await LoadEntriesAsync();
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void SelectCurrentFolder()
        {
            if (!CanSelectFolder)
            {
                return;
            }

            MudDialog.Close(DialogResult.Ok(_currentPath));
        }

        private async Task EntryClicked(PathBrowseEntry entry)
        {
            if (entry.IsDirectory)
            {
                _currentPath = entry.Path;
                await LoadEntriesAsync();
                return;
            }

            if (AllowsFileSelection())
            {
                MudDialog.Close(DialogResult.Ok(entry.Path));
            }
        }

        private async Task LoadEntriesAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentPath))
            {
                _entries.Clear();
                _loadError = Translate("Enter a valid path.");
                return;
            }

            _isLoading = true;
            _loadError = null;
            await InvokeAsync(StateHasChanged);

            try
            {
                var directoriesResult = await ApiClient.GetDirectoryContentAsync(_currentPath, DirectoryContentMode.Directories);
                if (!directoriesResult.TryGetValue(out var directoryPaths))
                {
                    _entries.Clear();
                    _loadError = Translate("Unable to load directory content.");
                    return;
                }

                var directoryEntries = directoryPaths
                    .Select(path => new PathBrowseEntry(path, GetTailSegment(path), true))
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var fileEntries = new List<PathBrowseEntry>();
                if (Mode != DirectoryContentMode.Directories)
                {
                    var filesResult = await ApiClient.GetDirectoryContentAsync(_currentPath, DirectoryContentMode.Files);
                    if (!filesResult.TryGetValue(out var filePaths))
                    {
                        _entries.Clear();
                        _loadError = Translate("Unable to load directory content.");
                        return;
                    }

                    fileEntries = filePaths
                        .Select(path => new PathBrowseEntry(path, GetTailSegment(path), false))
                        .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                _entries.Clear();
                _entries.AddRange(directoryEntries);
                _entries.AddRange(fileEntries);
            }
            catch (Exception exception)
            {
                _entries.Clear();
                _loadError = Translate("Unable to load directory content: %1", exception.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task DebounceLoadEntriesAsync()
        {
            var version = Interlocked.Increment(ref _pathChangeVersion);
            await Task.Delay(_pathChangeDebounceMs);
            if (version != _pathChangeVersion)
            {
                return;
            }

            await LoadEntriesAsync();
        }

        private async Task<string> GetDefaultPathAsync()
        {
            var defaultPathResult = await ApiClient.GetDefaultSavePathAsync();
            if (defaultPathResult.TryGetValue(out var path))
            {
                return string.IsNullOrWhiteSpace(path) ? string.Empty : path;
            }

            return string.Empty;
        }

        private bool AllowsFileSelection()
        {
            return Mode != DirectoryContentMode.Directories;
        }

        private static string? GetParentPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var trimmed = TrimTrailingSeparators(path);
            if (trimmed.Length == 0)
            {
                return null;
            }

            var index = trimmed.LastIndexOfAny(['/', '\\']);
            if (index < 0)
            {
                return null;
            }

            return trimmed[..(index + 1)];
        }

        private static string TrimTrailingSeparators(string path)
        {
            if (path.Length == 1 && (path[0] == '/' || path[0] == '\\'))
            {
                return path;
            }

            return path.TrimEnd('/', '\\');
        }

        private static string GetTailSegment(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var trimmed = TrimTrailingSeparators(path);
            var index = trimmed.LastIndexOfAny(['/', '\\']);
            return index < 0 ? trimmed : trimmed[(index + 1)..];
        }

        private sealed record PathBrowseEntry(string Path, string Name, bool IsDirectory);

        private string Translate(string value, params object[] args)
        {
            return LanguageLocalizer.Translate("AppPathBrowserDialog", value, args);
        }
    }
}
