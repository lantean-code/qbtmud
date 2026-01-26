using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;
using System.Globalization;

namespace Lantean.QBTMud.Components
{
    public partial class PiecesProgressSvg : ComponentBase
    {
        private const int LargePieceCountThreshold = 50000;

        private bool _showSvg = false;
        private string _linearBarStyle = string.Empty;
        private string _linearSummary = "Pieces data unavailable";
        private string _linearTooltip = "Pieces data unavailable";
        private string _linearAriaLabel = string.Empty;
        private string _svgEmptyText = "Pieces data unavailable";
        private string _svgHiddenText = "Pieces visualisation is hidden on small screens.";
        private string _svgAriaLabel = string.Empty;
        private string _viewBox = "0 0 0 0";
        private IReadOnlyList<PieceCell> _cells = Array.Empty<PieceCell>();
        private string _svgLoadingText = "Loading pieces...";
        private bool _svgBuilding;
        private bool _buildPending;
        private bool _buildInProgress;
        private IReadOnlyList<PieceState> _lastPiecesReference = Array.Empty<PieceState>();
        private int _lastPiecesSignature;
        private int _lastPiecesCount;

        [Parameter]
        [EditorRequired]
        public string Hash { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IReadOnlyList<PieceState> Pieces { get; set; } = Array.Empty<PieceState>();

        [Parameter]
        public bool PiecesLoading { get; set; }

        [Parameter]
        public bool PiecesFailed { get; set; }

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

        protected string SvgEmptyText => _svgEmptyText;

        protected string SvgLoadingText => _svgLoadingText;

        protected string SvgHiddenText => _svgHiddenText;

        protected string SvgAriaLabel => _svgAriaLabel;

        protected string ToggleIcon => _showSvg ? Icons.Material.Filled.ExpandLess : Icons.Material.Filled.ExpandMore;

        protected IReadOnlyList<PieceCell> Cells => _cells;

        protected int ColumnsForCurrentBreakpoint => DetermineColumnCount();

        private MudColor LinesColor => IsDarkMode ? Theme.PaletteDark.LinesDefault : Theme.PaletteLight.LinesDefault;

        private string StrokeColor => LinesColor.ToString(MudColorOutputFormats.RGBA);

        private string PendingFillColor => "transparent";

        private string PendingStrokeColor => LinesColor.SetAlpha(IsDarkMode ? 0.35 : 0.25).ToString(MudColorOutputFormats.RGBA);

        private string DimmedDownloadedColor => DimColor(IsDarkMode ? Theme.PaletteDark.Success : Theme.PaletteLight.Success);

        private string DimmedDownloadingColor => DimColor(IsDarkMode ? Theme.PaletteDark.Info : Theme.PaletteLight.Info);

        private string DimColor(MudColor color)
        {
            var alpha = IsDarkMode ? 0.45f : 0.55f;
            return color.SetAlpha(alpha).ToString(MudColorOutputFormats.RGBA);
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            var piecesChanged = UpdatePiecesSignature();

            if (piecesChanged || string.IsNullOrWhiteSpace(_linearBarStyle) || PiecesLoading || PiecesFailed)
            {
                BuildProgressSummary();
            }

            if (ShouldBuildSvgCells())
            {
                if (piecesChanged || _cells.Count == 0 || _viewBox == "0 0 0 0" || PiecesLoading || PiecesFailed)
                {
                    QueueBuildSvgCells();
                }
            }
            else
            {
                DeferSvgCells();
            }
        }

        protected async Task ToggleSvg()
        {
            _showSvg = !_showSvg;
            BuildProgressSummary();

            if (_showSvg && (_cells.Count == 0 || _viewBox == "0 0 0 0"))
            {
                QueueBuildSvgCells();
            }

            await InvokeAsync(StateHasChanged);
        }

        protected Task HandleLinearKeyDown(KeyboardEventArgs args)
        {
            if (args.Key is "Enter" or " " or "Space" or "Spacebar")
            {
                return ToggleSvg();
            }

            return Task.CompletedTask;
        }

        private void BuildProgressSummary()
        {
            if (PiecesLoading)
            {
                _linearBarStyle = $"background-color: {PendingColor};";
                _linearSummary = "Loading pieces...";
                _linearTooltip = "Loading pieces data...";
                _linearAriaLabel = $"Loading pieces progress for torrent {Hash}.";
                return;
            }

            if (PiecesFailed || Pieces.Count == 0)
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
                "Pieces progress for torrent {0}: {1:0.#}% complete. {2} downloaded, {3} downloading, {4} pending. Toggle SVG view.",
                Hash,
                percentComplete,
                downloadedCount,
                downloadingCount,
                pendingCount);
        }

        private void QueueBuildSvgCells()
        {
            _buildPending = true;
            _ = BuildSvgCellsAsync();
        }

        private async Task BuildSvgCellsAsync()
        {
            if (_buildInProgress)
            {
                return;
            }

            _buildInProgress = true;

            try
            {
                while (_buildPending)
                {
                    _buildPending = false;
                    var shouldShowSpinner = Pieces.Count >= LargePieceCountThreshold && !PiecesLoading && !PiecesFailed;
                    _svgBuilding = shouldShowSpinner;
                    if (shouldShowSpinner)
                    {
                        await InvokeAsync(StateHasChanged);
                        await Task.Yield();
                        await Task.Delay(50);
                    }

                    BuildSvgCellsCore();

                    _svgBuilding = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
            finally
            {
                _buildInProgress = false;
            }
        }

        private void BuildSvgCellsCore()
        {
            if (PiecesLoading)
            {
                _svgEmptyText = _svgLoadingText;
                _svgAriaLabel = $"Loading pieces SVG for torrent {Hash}.";
                _viewBox = "0 0 0 0";
                _cells = Array.Empty<PieceCell>();
                return;
            }

            if (PiecesFailed || Pieces.Count == 0)
            {
                _svgEmptyText = "Pieces data unavailable.";
                _svgAriaLabel = $"Pieces SVG unavailable for torrent {Hash}.";
                _viewBox = "0 0 0 0";
                _cells = Array.Empty<PieceCell>();
                return;
            }

            var columns = DetermineColumnCount();
            if (columns == 0)
            {
                _svgHiddenText = "Pieces SVG hidden on small screens.";
                _svgAriaLabel = $"Pieces SVG hidden for torrent {Hash}.";
                _viewBox = "0 0 0 0";
                _cells = Array.Empty<PieceCell>();
                return;
            }

            var rows = (int)Math.Ceiling((double)Pieces.Count / columns);
            var cellWidth = 1.0;
            var cellHeight = 1.0;
            var gap = 0.08;
            var drawWidth = cellWidth - gap;
            var drawHeight = cellHeight - gap;
            var offset = gap / 2.0;

            var cells = new List<PieceCell>(Pieces.Count);
            for (var index = 0; index < Pieces.Count; index++)
            {
                var column = index % columns;
                var row = index / columns;
                var x = (column * cellWidth) + offset;
                var y = (row * cellHeight) + offset;
                var tooltip = CreateInvariant("Piece #{0}: {1}", index + 1, DescribePieceState(Pieces[index]));
                cells.Add(new PieceCell(
                    x,
                    y,
                    drawWidth,
                    drawHeight,
                    GetSvgCssClass(Pieces[index]),
                    tooltip));
            }

            _cells = cells;
            _viewBox = $"0 0 {columns} {Math.Max(1, rows)}";
            _svgEmptyText = string.Empty;
            _svgAriaLabel = CreateInvariant(
                "Pieces SVG for torrent {0}. Rendering {1} pieces with {2} columns.",
                Hash,
                Pieces.Count,
                columns);
        }

        private int DetermineColumnCount()
        {
            if (CurrentBreakpoint <= Breakpoint.Xs)
            {
                return 0;
            }

            if (CurrentBreakpoint == Breakpoint.Sm)
            {
                return 32;
            }

            if (CurrentBreakpoint <= Breakpoint.Md)
            {
                return 64;
            }

            if (CurrentBreakpoint <= Breakpoint.Lg)
            {
                return 96;
            }

            return 128;
        }

        private string BuildLinearGradient()
        {
            if (Pieces.Count == 0)
            {
                return $"background-color: {PendingColor};";
            }

            var segments = BuildSegments();
            var builder = new System.Text.StringBuilder();
            builder.Append("background-color: ").Append(PendingColor).Append(';');
            builder.Append("background-image: linear-gradient(to right");

            for (var index = 0; index < segments.Count; index++)
            {
                var (color, start, end) = segments[index];
                if (index == 0)
                {
                    builder.Append(", ")
                        .Append(color)
                        .Append(" 0%");
                }
                else
                {
                    builder.Append(", ")
                        .Append(color)
                        .Append(' ')
                        .Append(start.ToString("0.#####", CultureInfo.InvariantCulture))
                        .Append('%');
                }

                builder.Append(", ")
                    .Append(color)
                    .Append(' ')
                    .Append(end.ToString("0.#####", CultureInfo.InvariantCulture))
                    .Append('%');

                if (index + 1 < segments.Count)
                {
                    var nextColor = segments[index + 1].Color;
                    builder.Append(", ")
                        .Append(nextColor)
                        .Append(' ')
                        .Append(end.ToString("0.#####", CultureInfo.InvariantCulture))
                        .Append('%');
                }
            }

            builder.Append(");");

            if (_showSvg)
            {
                builder.Append(" filter: saturate(0.6) brightness(0.9);");
            }

            return builder.ToString();
        }

        private List<Segment> BuildSegments()
        {
            var segments = new List<Segment>();
            if (Pieces.Count == 0)
            {
                return segments;
            }

            var totalPieces = Pieces.Count;
            var segmentStart = 0;
            var currentState = Pieces[0];
            for (var index = 1; index < totalPieces; index++)
            {
                if (Pieces[index] != currentState)
                {
                    segments.Add(CreateSegment(currentState, segmentStart, index, totalPieces));
                    segmentStart = index;
                    currentState = Pieces[index];
                }
            }

            segments.Add(CreateSegment(currentState, segmentStart, totalPieces, totalPieces));
            return segments;
        }

        private Segment CreateSegment(PieceState state, int startIndex, int endIndex, int totalPieces)
        {
            var color = state switch
            {
                PieceState.Downloaded => _showSvg ? DimmedDownloadedColor : DownloadedColor,
                PieceState.Downloading => _showSvg ? DimmedDownloadingColor : DownloadingColor,
                _ => PendingColor
            };

            var startPercent = Percentage(startIndex, totalPieces);
            var endPercent = Percentage(endIndex, totalPieces);
            return new Segment(color, startPercent, endPercent);
        }

        private static string DescribePieceState(PieceState state)
        {
            return state switch
            {
                PieceState.Downloaded => "Downloaded",
                PieceState.Downloading => "Downloading",
                _ => "Not downloaded"
            };
        }

        private static string GetSvgCssClass(PieceState state)
        {
            return state switch
            {
                PieceState.Downloaded => "pieces-progress-svg__rect--downloaded",
                PieceState.Downloading => "pieces-progress-svg__rect--downloading",
                _ => "pieces-progress-svg__rect--pending"
            };
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

        private bool UpdatePiecesSignature()
        {
            if (ReferenceEquals(Pieces, _lastPiecesReference))
            {
                return false;
            }

            var signatureBuilder = new HashCode();
            signatureBuilder.Add(Pieces.Count);
            for (var index = 0; index < Pieces.Count; index++)
            {
                signatureBuilder.Add((int)Pieces[index]);
            }

            var signature = signatureBuilder.ToHashCode();
            var unchanged = _lastPiecesCount == Pieces.Count && _lastPiecesSignature == signature;

            _lastPiecesReference = Pieces;
            _lastPiecesSignature = signature;
            _lastPiecesCount = Pieces.Count;

            return !unchanged;
        }

        private bool ShouldBuildSvgCells()
        {
            return _showSvg;
        }

        private void DeferSvgCells()
        {
            _viewBox = "0 0 0 0";
            _cells = Array.Empty<PieceCell>();
            _svgEmptyText = string.Empty;
            _svgAriaLabel = CreateInvariant("Pieces SVG deferred for torrent {0}. Expand to render.", Hash);
            _buildPending = false;
            _svgBuilding = false;
        }

        protected sealed record PieceCell(double X, double Y, double Width, double Height, string CssClass, string Tooltip);

        private sealed record Segment(string Color, double Start, double End);
    }
}
