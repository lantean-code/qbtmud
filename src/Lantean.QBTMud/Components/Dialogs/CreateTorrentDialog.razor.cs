using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class CreateTorrentDialog : SubmittableDialog
    {
        private const int BytesPerKibibyte = 1024;
        private const string StorageKey = "TorrentCreator.FormState";

        private static readonly IReadOnlyList<int> _pieceSizeOptions =
        [
            16 * 1024,
            32 * 1024,
            64 * 1024,
            128 * 1024,
            256 * 1024,
            512 * 1024,
            1024 * 1024,
            2 * 1024 * 1024,
            4 * 1024 * 1024,
            8 * 1024 * 1024,
            16 * 1024 * 1024,
            32 * 1024 * 1024,
            64 * 1024 * 1024,
            128 * 1024 * 1024
        ];

        private bool _supportsTorrentFormat;
        private bool _forceSourcePathValidation;
        private string? _comment;
        private string? _source;
        private string? _sourcePath;
        private string? _torrentFilePath;
        private string? _trackers;
        private string? _urlSeeds;
        private string _torrentFormat = "hybrid";
        private int? _pieceSize;
        private int _paddedFileSizeLimitKiB = -1;
        private bool _isPrivate;
        private bool _optimizeAlignment = true;
        private bool _startSeeding = true;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        protected IReadOnlyList<int> PieceSizeOptions
        {
            get { return _pieceSizeOptions; }
        }

        protected bool SupportsTorrentFormat
        {
            get { return _supportsTorrentFormat; }
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadFormStateAsync();
            await LoadBuildInfoAsync();
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected async Task Submit()
        {
            var request = BuildRequest();
            if (request is null)
            {
                return;
            }

            await PersistFormStateAsync();
            MudDialog.Close(DialogResult.Ok(request));
        }

        protected override async Task Submit(KeyboardEvent keyboardEvent)
        {
            await Submit();
        }

        private TorrentCreationTaskRequest? BuildRequest()
        {
            var sourcePath = _sourcePath?.Trim();
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                _forceSourcePathValidation = true;
                Snackbar?.Add("Source path is required.", Severity.Warning);
                return null;
            }

            _forceSourcePathValidation = false;

            var request = new TorrentCreationTaskRequest
            {
                SourcePath = sourcePath,
                TorrentFilePath = string.IsNullOrWhiteSpace(_torrentFilePath) ? null : _torrentFilePath.Trim(),
                PieceSize = _pieceSize,
                Private = _isPrivate,
                StartSeeding = _startSeeding,
                Comment = string.IsNullOrWhiteSpace(_comment) ? null : _comment.Trim(),
                Source = string.IsNullOrWhiteSpace(_source) ? null : _source.Trim(),
                Trackers = ParseList(_trackers),
                UrlSeeds = ParseList(_urlSeeds)
            };

            if (_supportsTorrentFormat)
            {
                request.Format = _torrentFormat;
            }
            else
            {
                request.OptimizeAlignment = _optimizeAlignment;
                request.PaddedFileSizeLimit = BuildPaddedFileSizeLimit();
            }

            return request;
        }

        protected async Task OnSourcePathChanged(string? value)
        {
            _sourcePath = value;
            _forceSourcePathValidation = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadFormStateAsync()
        {
            TorrentCreationFormState? state;
            try
            {
                state = await LocalStorage.GetItemAsync<TorrentCreationFormState>(StorageKey);
            }
            catch (Exception exception)
            {
                Snackbar?.Add($"Unable to load saved torrent creator settings: {exception.Message}", Severity.Warning);
                return;
            }

            if (state is null)
            {
                return;
            }

            _sourcePath = state.SourcePath;
            _torrentFilePath = state.TorrentFilePath;
            _pieceSize = state.PieceSize;
            _isPrivate = state.Private;
            _startSeeding = state.StartSeeding;
            _trackers = state.Trackers;
            _urlSeeds = state.UrlSeeds;
            _comment = state.Comment;
            _source = state.Source;
            _torrentFormat = string.IsNullOrWhiteSpace(state.Format) ? _torrentFormat : state.Format;
            _optimizeAlignment = state.OptimizeAlignment;

            if (state.PaddedFileSizeLimit.HasValue)
            {
                _paddedFileSizeLimitKiB = state.PaddedFileSizeLimit.Value < 0
                    ? -1
                    : state.PaddedFileSizeLimit.Value / BytesPerKibibyte;
            }
        }

        private async Task LoadBuildInfoAsync()
        {
            try
            {
                var buildInfo = await ApiClient.GetBuildInfo();
                _supportsTorrentFormat = SupportsTorrentFormatForVersion(buildInfo.LibTorrentVersion);
            }
            catch
            {
                _supportsTorrentFormat = false;
            }
        }

        private async Task PersistFormStateAsync()
        {
            var state = new TorrentCreationFormState
            {
                SourcePath = _sourcePath?.Trim() ?? string.Empty,
                TorrentFilePath = _torrentFilePath?.Trim() ?? string.Empty,
                PieceSize = _pieceSize,
                Private = _isPrivate,
                StartSeeding = _startSeeding,
                Trackers = _trackers ?? string.Empty,
                UrlSeeds = _urlSeeds ?? string.Empty,
                Comment = _comment ?? string.Empty,
                Source = _source ?? string.Empty,
                Format = _torrentFormat,
                OptimizeAlignment = _optimizeAlignment,
                PaddedFileSizeLimit = ConvertPaddedFileSizeLimit()
            };

            await LocalStorage.SetItemAsync(StorageKey, state);
        }

        private int? BuildPaddedFileSizeLimit()
        {
            if (!_optimizeAlignment)
            {
                return null;
            }

            return ConvertPaddedFileSizeLimit();
        }

        private int? ConvertPaddedFileSizeLimit()
        {
            if (_paddedFileSizeLimitKiB < 0)
            {
                return -1;
            }

            if (_paddedFileSizeLimitKiB > int.MaxValue / BytesPerKibibyte)
            {
                return int.MaxValue;
            }

            return _paddedFileSizeLimitKiB * BytesPerKibibyte;
        }

        private static IReadOnlyList<string>? ParseList(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var items = value
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            return items.Length == 0 ? null : items;
        }

        private static bool SupportsTorrentFormatForVersion(string? libtorrentVersion)
        {
            if (!TryGetMajorVersion(libtorrentVersion, out var major))
            {
                return false;
            }

            return major >= 2;
        }

        private static bool TryGetMajorVersion(string? version, out int major)
        {
            major = 0;
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            var trimmed = version.Trim();
            var end = 0;
            while (end < trimmed.Length && (char.IsDigit(trimmed[end]) || trimmed[end] == '.'))
            {
                end++;
            }

            if (end == 0)
            {
                return false;
            }

            var numeric = trimmed[..end].TrimEnd('.');
            if (Version.TryParse(numeric, out var parsed))
            {
                major = parsed.Major;
                return true;
            }

            var firstSegment = numeric.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return int.TryParse(firstSegment, out major);
        }
    }
}
