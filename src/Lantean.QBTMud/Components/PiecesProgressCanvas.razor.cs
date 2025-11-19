using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using System.Globalization;

namespace Lantean.QBTMud.Components
{
    public partial class PiecesProgressCanvas : ComponentBase
    {
        private readonly string _canvasId = $"pieces-progress-canvas-{Guid.NewGuid():N}";

        private bool _showCanvas;
        private string _linearBarStyle = string.Empty;
        private string _linearSummary = "Pieces data unavailable";
        private string _linearTooltip = "Pieces data unavailable";
        private string _linearAriaLabel = string.Empty;
        private string _canvasEmptyText = "Pieces data unavailable";
        private string _canvasHiddenText = "Pieces visualisation is hidden on small screens.";
        private string _canvasAriaLabel = string.Empty;
        private int[] _pieceStates = Array.Empty<int>();
        private bool _shouldRedraw = true;

        [Parameter]
        [EditorRequired]
        public string Hash { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IReadOnlyList<PieceState> Pieces { get; set; } = Array.Empty<PieceState>();

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [CascadingParameter(Name = "IsDarkMode")]
        public bool IsDarkMode { get; set; }

        [CascadingParameter]
        public MudTheme Theme { get; set; } = default!;

        [CascadingParameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        protected bool HasPieceData => Pieces.Count > 0;

        protected string LinearBarStyle => _linearBarStyle;

        protected string LinearSummary => _linearSummary;

        protected string LinearTooltip => _linearTooltip;

        protected string LinearAriaLabel => _linearAriaLabel;

        protected string CanvasEmptyText => _canvasEmptyText;

        protected string CanvasHiddenText => _canvasHiddenText;

        protected string CanvasAriaLabel => _canvasAriaLabel;

        protected int ColumnsForCurrentBreakpoint => DetermineColumnCount();

        protected string ToggleIcon => _showCanvas ? Icons.Material.Filled.ExpandLess : Icons.Material.Filled.ExpandMore;

        protected override void OnInitialized()
        {
            _showCanvas = true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            BuildProgressSummary();
            BuildCanvasMetadata();
            _pieceStates = Pieces.Select(static piece => (int)piece).ToArray();
            _shouldRedraw = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!_showCanvas || ColumnsForCurrentBreakpoint == 0 || _pieceStates.Length == 0)
            {
                return;
            }

            if (!_shouldRedraw)
            {
                return;
            }

            var rect = await JSRuntime.GetBoundingClientRect($"#{_canvasId}");
            if (rect is null || rect.Width <= 0)
            {
                return;
            }

            var columns = Math.Max(1, ColumnsForCurrentBreakpoint);
            var width = rect.Width;
            var cellSize = Math.Max(1, width / columns);
            var actualWidth = width;
            var rows = (int)Math.Ceiling((double)_pieceStates.Length / columns);
            var height = Math.Max(cellSize, rows * cellSize);

            await JSRuntime.InvokeVoidAsync(
                "qbt.renderPiecesCanvas",
                _canvasId,
                actualWidth,
                height,
                columns,
                cellSize,
                _pieceStates,
                DownloadedColor,
                DownloadingColor,
                PendingColor);

            _shouldRedraw = false;
        }

        protected void ToggleCanvas()
        {
            _showCanvas = !_showCanvas;
            if (_showCanvas)
            {
                _shouldRedraw = true;
            }
        }

        protected void HandleLinearKeyDown(KeyboardEventArgs args)
        {
            if (args.Key is "Enter" or " " or "Space" or "Spacebar")
            {
                ToggleCanvas();
            }
        }

        private void BuildProgressSummary()
        {
            if (Pieces.Count == 0)
            {
                _linearBarStyle = $"background-color: {PendingColor};";
                _linearSummary = "Pieces data unavailable";
                _linearTooltip = "Pieces data unavailable";
                _linearAriaLabel = $"Pieces progress unavailable for torrent {Hash}.";
                return;
            }

            int downloadedCount = 0;
            int downloadingCount = 0;
            int pendingCount = 0;

            foreach (var piece in Pieces)
            {
                switch (piece)
                {
                    case PieceState.Downloaded:
                        downloadedCount++;
                        break;

                    case PieceState.Downloading:
                        downloadingCount++;
                        break;

                    default:
                        pendingCount++;
                        break;
                }
            }

            var gradient = BuildLinearGradient();
            _linearBarStyle = gradient;

            var percentComplete = Pieces.Count == 0
                ? 0
                : ((downloadedCount + (downloadingCount * 0.5)) / Pieces.Count) * 100.0;
            _linearSummary = CreateInvariant(
                "{0:0.#}% complete — {1} downloaded, {2} in progress",
                percentComplete,
                downloadedCount,
                downloadingCount);
            _linearTooltip = CreateInvariant(
                "Downloaded: {0}\nDownloading: {1}\nPending: {2}",
                downloadedCount,
                downloadingCount,
                pendingCount);
            _linearAriaLabel = CreateInvariant(
                "Pieces progress for torrent {0}: {1:0.#}% complete. {2} downloaded, {3} downloading, {4} pending. Toggle canvas view.",
                Hash,
                percentComplete,
                downloadedCount,
                downloadingCount,
                pendingCount);
        }

        private void BuildCanvasMetadata()
        {
            if (Pieces.Count == 0)
            {
                _canvasEmptyText = "Pieces data unavailable.";
                _canvasAriaLabel = $"Pieces canvas unavailable for torrent {Hash}.";
                return;
            }

            var columns = ColumnsForCurrentBreakpoint;
            if (columns == 0)
            {
                _canvasHiddenText = "Pieces canvas hidden on small screens.";
            }

            _canvasEmptyText = string.Empty;
            _canvasAriaLabel = CreateInvariant(
                "Pieces canvas for torrent {0}. Rendering {1} pieces with {2} columns.",
                Hash,
                Pieces.Count,
                Math.Max(1, columns));
        }

        private int DetermineColumnCount()
        {
            if (CurrentBreakpoint <= Breakpoint.Sm)
            {
                return 0;
            }

            if (CurrentBreakpoint <= Breakpoint.Md)
            {
                return 32;
            }

            return 64;
        }

        private string BuildLinearGradient()
        {
            if (Pieces.Count == 0)
            {
                return $"background-color: {PendingColor};";
            }

            var builder = new System.Text.StringBuilder();
            builder.Append("background-color: ").Append(PendingColor).Append(';');
            builder.Append("background-image: linear-gradient(to right");

            var totalPieces = Pieces.Count;
            var segmentStart = 0;
            var currentState = Pieces[0];
            for (var index = 1; index < totalPieces; index++)
            {
                if (Pieces[index] != currentState)
                {
                    AppendGradientSegment(builder, currentState, segmentStart, index, totalPieces);
                    segmentStart = index;
                    currentState = Pieces[index];
                }
            }

            AppendGradientSegment(builder, currentState, segmentStart, totalPieces, totalPieces);
            builder.Append(");");
            return builder.ToString();
        }

        private void AppendGradientSegment(System.Text.StringBuilder builder, PieceState state, int startIndex, int endIndex, int totalPieces)
        {
            var color = state switch
            {
                PieceState.Downloaded => DownloadedColor,
                PieceState.Downloading => DownloadingColor,
                _ => PendingColor
            };
            var startPercent = Percentage(startIndex, totalPieces);
            var endPercent = Percentage(endIndex, totalPieces);
            builder.Append(", ")
                .Append(color)
                .Append(' ')
                .Append(startPercent.ToString("0.###", CultureInfo.InvariantCulture))
                .Append("%, ")
                .Append(color)
                .Append(' ')
                .Append(endPercent.ToString("0.###", CultureInfo.InvariantCulture))
                .Append('%');
        }

        private string DownloadedColor => ToCssColor(IsDarkMode ? Theme.PaletteDark.Success : Theme.PaletteLight.Success);

        private string DownloadingColor => ToCssColor(IsDarkMode ? Theme.PaletteDark.Info : Theme.PaletteLight.Info);

        private string PendingColor => ToCssColor(IsDarkMode ? Theme.PaletteDark.Surface : Theme.PaletteLight.Surface);

        private static string ToCssColor(MudColor color)
        {
            return color.ToString(MudColorOutputFormats.RGBA);
        }

        private static double Percentage(int value, int total)
        {
            if (total == 0)
            {
                return 0;
            }

            return (double)value / total * 100.0;
        }

        private static string CreateInvariant(string format, params object?[] arguments)
        {
            var formatted = string.Format(CultureInfo.InvariantCulture, format, arguments);
            return string.Create(
                formatted.Length,
                formatted,
                static (span, state) => state.AsSpan().CopyTo(span));
        }
    }
}
