using Lantean.QBTMud.Application.Services.Localization;
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
        private int _loadVersion;

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
            _loadVersion++;
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
            var version = ++_loadVersion;
            var path = _currentPath;
            var mode = Mode;
            if (string.IsNullOrWhiteSpace(path))
            {
                _entries.Clear();
                _loadError = Translate("Enter a valid path.");
                _isLoading = false;
                return;
            }

            _isLoading = true;
            _loadError = null;
            await InvokeAsync(StateHasChanged);

            try
            {
                var directoriesResult = await ApiClient.GetDirectoryContentAsync(path, DirectoryContentMode.Directories);
                if (!IsCurrentLoad(version))
                {
                    return;
                }

                if (directoriesResult.IsFailure)
                {
                    _entries.Clear();
                    _loadError = Translate("Unable to load directory content.");
                    return;
                }

                var directoryPaths = directoriesResult.Value;
                var directoryEntries = directoryPaths
                    .Select(path => new PathBrowseEntry(path, GetTailSegment(path), true))
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var fileEntries = new List<PathBrowseEntry>();
                if (mode != DirectoryContentMode.Directories)
                {
                    var filesResult = await ApiClient.GetDirectoryContentAsync(path, DirectoryContentMode.Files);
                    if (!IsCurrentLoad(version))
                    {
                        return;
                    }

                    if (filesResult.IsFailure)
                    {
                        _entries.Clear();
                        _loadError = Translate("Unable to load directory content.");
                        return;
                    }

                    var filePaths = filesResult.Value;
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
                if (IsCurrentLoad(version))
                {
                    _entries.Clear();
                    _loadError = Translate("Unable to load directory content: %1", exception.Message);
                }
            }
            finally
            {
                if (IsCurrentLoad(version))
                {
                    _isLoading = false;
                }
            }
        }

        private bool IsCurrentLoad(int version)
        {
            return version == _loadVersion;
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
            if (defaultPathResult.IsFailure)
            {
                return string.Empty;
            }

            var path = defaultPathResult.Value;
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path;
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
