using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class PathBrowserDialog
    {
        private const int PathChangeDebounceMs = 300;
        private readonly List<PathBrowseEntry> _entries = [];
        private string _currentPath = string.Empty;
        private bool _isLoading;
        private string? _loadError;
        private int _pathChangeVersion;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

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
                _loadError = "Enter a valid path.";
                return;
            }

            _isLoading = true;
            _loadError = null;
            await InvokeAsync(StateHasChanged);

            try
            {
                var directories = await ApiClient.GetDirectoryContent(_currentPath, DirectoryContentMode.Directories);
                var directoryEntries = directories
                    .Select(path => new PathBrowseEntry(path, GetTailSegment(path), true))
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var fileEntries = new List<PathBrowseEntry>();
                if (Mode != DirectoryContentMode.Directories)
                {
                    var files = await ApiClient.GetDirectoryContent(_currentPath, DirectoryContentMode.Files);
                    fileEntries = files
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
                _loadError = $"Unable to load directory content: {exception.Message}";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task DebounceLoadEntriesAsync()
        {
            var version = Interlocked.Increment(ref _pathChangeVersion);
            await Task.Delay(PathChangeDebounceMs);
            if (version != _pathChangeVersion)
            {
                return;
            }

            await LoadEntriesAsync();
        }

        private async Task<string> GetDefaultPathAsync()
        {
            try
            {
                var path = await ApiClient.GetDefaultSavePath();
                return string.IsNullOrWhiteSpace(path) ? string.Empty : path;
            }
            catch
            {
                return string.Empty;
            }
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
    }
}
