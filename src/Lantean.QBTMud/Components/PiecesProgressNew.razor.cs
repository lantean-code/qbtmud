using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;
using System.Globalization;
using System.Text;

namespace Lantean.QBTMud.Components
{
    public partial class PiecesProgressNew : ComponentBase
    {
        private const int MinimumHeatmapColumns = 32;
        private const int MaximumHeatmapColumns = 96;
        private const int HeatmapColumnIncrement = 8;
        private const int MaxHeatmapCellsTarget = 2048;
        private static readonly HeatmapSegment[] EmptyHeatmapSegments = Array.Empty<HeatmapSegment>();

        private bool _showHeatmap;
        private bool _hasViewModelInitialized;
        private bool _shouldRender;
        private int _heatmapColumns = MinimumHeatmapColumns;
        private int _piecesPerCell = 1;
        private string _linearBarStyle = string.Empty;
        private string _linearSummary = string.Empty;
        private string _linearTooltip = string.Empty;
        private string _linearAriaLabel = string.Empty;
        private string _heatmapEmptyText = string.Empty;
        private string _heatmapAriaLabel = string.Empty;
        private IReadOnlyList<IReadOnlyList<HeatmapCellViewModel>> _heatmapRows = Array.Empty<IReadOnlyList<HeatmapCellViewModel>>();
        private IReadOnlyList<LegendItem> _legendItems = Array.Empty<LegendItem>();
        private PieceState[] _renderedPieces = Array.Empty<PieceState>();
        private PieceState[] _pendingPieces = Array.Empty<PieceState>();
        private bool _cachedIsDarkMode;
        private bool _pendingIsDarkMode;
        private string _cachedThemeSignature = string.Empty;
        private string _pendingThemeSignature = string.Empty;
        private string _cachedHash = string.Empty;
        private string _pendingHash = string.Empty;
        private const string AppContext = "AppPiecesProgressNew";

        [Parameter]
        [EditorRequired]
        public string Hash { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IReadOnlyList<PieceState> Pieces { get; set; } = Array.Empty<PieceState>();

        [CascadingParameter(Name = "IsDarkMode")]
        public bool IsDarkMode { get; set; }

        [CascadingParameter]
        public MudTheme Theme { get; set; } = default!;

        [Inject]
        public Lantean.QBTMud.Services.Localization.IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        protected string LinearBarStyle => _linearBarStyle;

        protected string LinearSummary => _linearSummary;

        protected string LinearTooltip => _linearTooltip;

        protected string LinearAriaLabel => _linearAriaLabel;

        protected string HeatmapEmptyText => _heatmapEmptyText;

        protected string HeatmapAriaLabel => _heatmapAriaLabel;

        protected IReadOnlyList<IReadOnlyList<HeatmapCellViewModel>> HeatmapRows => _heatmapRows;

        protected IReadOnlyList<LegendItem> LegendItems => _legendItems;

        protected int HeatmapColumnCount => _heatmapColumns;

        protected string ToggleIcon => _showHeatmap ? Icons.Material.Filled.ExpandLess : Icons.Material.Filled.ExpandMore;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            var themeSignature = CreateThemeSignature();
            var hash = Hash ?? string.Empty;
            var piecesChanged = !_renderedPieces.SequenceEqual(Pieces);
            var hashChanged = !string.Equals(hash, _cachedHash, StringComparison.Ordinal);
            var themeChanged = !string.Equals(themeSignature, _cachedThemeSignature, StringComparison.Ordinal);
            var darkModeChanged = _cachedIsDarkMode != IsDarkMode;

            _shouldRender = !_hasViewModelInitialized || piecesChanged || hashChanged || themeChanged || darkModeChanged;

            if (!_shouldRender)
            {
                return;
            }

            BuildLinearViewModel();

            if (_showHeatmap)
            {
                BuildHeatmapModels();
            }
            else
            {
                _heatmapRows = Array.Empty<IReadOnlyList<HeatmapCellViewModel>>();
                _legendItems = Array.Empty<LegendItem>();
            }

            _pendingPieces = Pieces.ToArray();
            _pendingThemeSignature = themeSignature;
            _pendingIsDarkMode = IsDarkMode;
            _pendingHash = hash;
        }

        protected override bool ShouldRender()
        {
            return _shouldRender;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (_shouldRender)
            {
                _renderedPieces = _pendingPieces;
                _cachedThemeSignature = _pendingThemeSignature;
                _cachedIsDarkMode = _pendingIsDarkMode;
                _cachedHash = _pendingHash;
            }

            _hasViewModelInitialized = true;
            _shouldRender = false;
        }

        protected void ToggleHeatmap()
        {
            _shouldRender = true;
            _showHeatmap = !_showHeatmap;
            if (_showHeatmap)
            {
                BuildHeatmapModels();
            }
            else
            {
                _heatmapRows = Array.Empty<IReadOnlyList<HeatmapCellViewModel>>();
                _legendItems = Array.Empty<LegendItem>();
            }
        }

        protected void HandleLinearKeyDown(KeyboardEventArgs args)
        {
            if (args.Key is "Enter" or " " or "Space" or "Spacebar")
            {
                _shouldRender = true;
                _showHeatmap = !_showHeatmap;
                if (_showHeatmap)
                {
                    BuildHeatmapModels();
                }
                else
                {
                    _heatmapRows = Array.Empty<IReadOnlyList<HeatmapCellViewModel>>();
                    _legendItems = Array.Empty<LegendItem>();
                }
            }
        }

        private void BuildHeatmapModels()
        {
            _piecesPerCell = DeterminePiecesPerCell(Pieces.Count);
            _heatmapColumns = CalculateHeatmapColumns(Pieces.Count, _piecesPerCell);
            BuildHeatmapViewModel();
            BuildLegend();
        }

        private void BuildLinearViewModel()
        {
            if (Pieces.Count == 0)
            {
                _linearBarStyle = $"background-color: {PendingColor};";
                _linearSummary = TranslateApp("Pieces data unavailable");
                _linearTooltip = TranslateApp("Pieces data unavailable");
                _linearAriaLabel = TranslateApp("Pieces progress unavailable for torrent %1.", Hash);
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
                : (downloadedCount + (downloadingCount * 0.5)) / Pieces.Count * 100.0;
            var percentCompleteText = percentComplete.ToString("0.#", CultureInfo.InvariantCulture);
            _linearSummary = TranslateApp(
                "%1% complete — %2 downloaded, %3 in progress",
                percentCompleteText,
                downloadedCount,
                downloadingCount);
            _linearTooltip = TranslateApp(
                "Downloaded: %1\nDownloading: %2\nPending: %3",
                downloadedCount,
                downloadingCount,
                pendingCount);
            _linearAriaLabel = TranslateApp(
                "Pieces progress for torrent %1: %2% complete. %3 downloaded, %4 downloading, %5 pending. Toggle heatmap view.",
                Hash,
                percentCompleteText,
                downloadedCount,
                downloadingCount,
                pendingCount);
        }

        private void BuildHeatmapViewModel()
        {
            if (Pieces.Count == 0)
            {
                _heatmapRows = Array.Empty<IReadOnlyList<HeatmapCellViewModel>>();
                _heatmapEmptyText = TranslateApp("Heatmap unavailable without piece data.");
                _heatmapAriaLabel = TranslateApp("Pieces heatmap unavailable for torrent %1.", Hash);
                return;
            }

            var piecesPerCell = Math.Max(1, _piecesPerCell);
            var totalCells = (int)Math.Ceiling((double)Pieces.Count / piecesPerCell);
            var rowsRequired = (int)Math.Ceiling((double)totalCells / _heatmapColumns);

            var rows = new List<IReadOnlyList<HeatmapCellViewModel>>(rowsRequired);
            for (var rowIndex = 0; rowIndex < rowsRequired; rowIndex++)
            {
                var row = new List<HeatmapCellViewModel>(_heatmapColumns);
                for (var columnIndex = 0; columnIndex < _heatmapColumns; columnIndex++)
                {
                    var cellIndex = (rowIndex * _heatmapColumns) + columnIndex;
                    var pieceStartIndex = cellIndex * piecesPerCell;
                    if (pieceStartIndex >= Pieces.Count)
                    {
                        row.Add(HeatmapCellViewModel.Placeholder);
                        continue;
                    }

                    var cellPieces = CollectCellPieces(pieceStartIndex, piecesPerCell);
                    var tooltip = BuildHeatmapTooltip(pieceStartIndex, cellPieces);
                    var segments = BuildHeatmapSegments(cellPieces);
                    var layoutClass = DetermineLayoutClass(segments.Count);
                    row.Add(new HeatmapCellViewModel(layoutClass, segments, tooltip, false));
                }

                rows.Add(row);
            }

            _heatmapRows = rows;
            _heatmapEmptyText = string.Empty;
            _heatmapAriaLabel = TranslateApp("Pieces heatmap for torrent %1.", Hash);
        }

        private IReadOnlyList<PieceState> CollectCellPieces(int startIndex, int piecesPerCell)
        {
            var remaining = Pieces.Count - startIndex;
            var count = Math.Min(piecesPerCell, remaining);
            var cellPieces = new List<PieceState>(count);
            for (var offset = 0; offset < count; offset++)
            {
                cellPieces.Add(Pieces[startIndex + offset]);
            }

            return cellPieces;
        }

        private void BuildLegend()
        {
            _legendItems = new[]
            {
                new LegendItem("pieces-progress-new__legend-swatch--downloaded", TranslateApp("Downloaded")),
                new LegendItem("pieces-progress-new__legend-swatch--downloading", TranslateApp("Downloading")),
                new LegendItem("pieces-progress-new__legend-swatch--pending", TranslateApp("Not downloaded"))
            };
        }

        private string BuildLinearGradient()
        {
            if (Pieces.Count == 0)
            {
                return $"background-color: {PendingColor};";
            }

            var builder = new StringBuilder();
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

        private void AppendGradientSegment(StringBuilder builder, PieceState state, int startIndex, int endIndex, int totalPieces)
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

        private IReadOnlyList<HeatmapSegment> BuildHeatmapSegments(IReadOnlyList<PieceState> states)
        {
            if (states.Count == 0)
            {
                return EmptyHeatmapSegments;
            }

            if (states.Count == 1)
            {
                return new HeatmapSegment[]
                {
                    new HeatmapSegment("pieces-progress-new__cell-segment--whole", GetHeatmapCssClass(states[0]))
                };
            }

            if (states.Count == 2)
            {
                return new HeatmapSegment[]
                {
                    new HeatmapSegment("pieces-progress-new__cell-segment--left", GetHeatmapCssClass(states[0])),
                    new HeatmapSegment("pieces-progress-new__cell-segment--right", GetHeatmapCssClass(states[1]))
                };
            }

            var positions = new[]
            {
                "pieces-progress-new__cell-segment--top-left",
                "pieces-progress-new__cell-segment--top-right",
                "pieces-progress-new__cell-segment--bottom-left",
                "pieces-progress-new__cell-segment--bottom-right"
            };

            var segments = new HeatmapSegment[positions.Length];
            for (var index = 0; index < positions.Length; index++)
            {
                var state = index < states.Count ? states[index] : states[^1];
                segments[index] = new HeatmapSegment(positions[index], GetHeatmapCssClass(state));
            }

            return segments;
        }

        private static string DetermineLayoutClass(int segmentCount)
        {
            return segmentCount switch
            {
                <= 1 => "pieces-progress-new__cell-inner--single",
                2 => "pieces-progress-new__cell-inner--dual",
                _ => string.Empty
            };
        }

        private string BuildHeatmapTooltip(int startIndex, IReadOnlyList<PieceState> states)
        {
            if (states.Count == 0)
            {
                return string.Empty;
            }

            if (states.Count == 1)
            {
                return TranslateApp(
                    "Piece #%1: %2",
                    startIndex + 1,
                    DescribePieceState(states[0]));
            }

            var builder = new StringBuilder();
            for (var index = 0; index < states.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(TranslateApp(
                    "Piece #%1: %2",
                    startIndex + index + 1,
                    DescribePieceState(states[index])));
            }

            return builder.ToString();
        }

        private string DescribePieceState(PieceState state)
        {
            return state switch
            {
                PieceState.Downloaded => TranslateApp("Downloaded"),
                PieceState.Downloading => TranslateApp("Downloading"),
                _ => TranslateApp("Not downloaded")
            };
        }

        private static int DeterminePiecesPerCell(int pieceCount)
        {
            if (pieceCount <= 0)
            {
                return 1;
            }

            if (pieceCount < MaxHeatmapCellsTarget)
            {
                return 1;
            }

            if (pieceCount < MaxHeatmapCellsTarget * 2)
            {
                return 2;
            }

            return 4;
        }

        private static int CalculateHeatmapColumns(int pieceCount, int piecesPerCell)
        {
            if (pieceCount <= 0)
            {
                return MinimumHeatmapColumns;
            }

            var effectivePieces = (int)Math.Ceiling((double)pieceCount / Math.Max(1, piecesPerCell));
            var target = (int)Math.Ceiling(Math.Sqrt(effectivePieces));
            target = Math.Max(target, MinimumHeatmapColumns);
            var rounded = RoundUpToMultiple(target, HeatmapColumnIncrement);
            return Math.Clamp(rounded, MinimumHeatmapColumns, MaximumHeatmapColumns);
        }

        private static int RoundUpToMultiple(int value, int multiple)
        {
            if (multiple <= 0)
            {
                return value;
            }

            var remainder = value % multiple;
            if (remainder == 0)
            {
                return value;
            }

            return value + multiple - remainder;
        }

        private static string GetHeatmapCssClass(PieceState state)
        {
            return state switch
            {
                PieceState.Downloaded => "pieces-progress-new__cell--downloaded",
                PieceState.Downloading => "pieces-progress-new__cell--downloading",
                _ => "pieces-progress-new__cell--pending"
            };
        }

        private string DownloadedColor => ToCssColor(IsDarkMode ? Theme.PaletteDark.Success : Theme.PaletteLight.Success);

        private string DownloadingColor => ToCssColor(IsDarkMode ? Theme.PaletteDark.Info : Theme.PaletteLight.Info);

        private string PendingColor => ToCssColor(IsDarkMode ? Theme.PaletteDark.Surface : Theme.PaletteLight.Surface);

        private string CreateThemeSignature()
        {
            return string.Concat(
                DownloadedColor,
                "|",
                DownloadingColor,
                "|",
                PendingColor);
        }

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

        protected sealed record HeatmapCellViewModel(string LayoutClass, IReadOnlyList<HeatmapSegment> Segments, string Tooltip, bool IsPlaceholder)
        {
            public static HeatmapCellViewModel Placeholder { get; } = new HeatmapCellViewModel("pieces-progress-new__cell-inner--single", EmptyHeatmapSegments, string.Empty, true);

            public bool HasSegments => Segments.Count > 0;
        }

        protected sealed record HeatmapSegment(string PositionClass, string ColorClass);

        protected sealed record LegendItem(string CssClass, string Label);

        private string TranslateApp(string source, params object[] arguments)
        {
            return WebUiLocalizer.Translate(AppContext, source, arguments);
        }
    }
}
