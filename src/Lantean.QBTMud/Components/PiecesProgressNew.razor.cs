using System;
using System.Globalization;
using System.Text;
using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Components
{
    public partial class PiecesProgressNew : ComponentBase
    {
        private const int HeatmapColumns = 32;

        private bool _showHeatmap;
        private string _linearBarStyle = string.Empty;
        private string _linearSummary = "Pieces data unavailable";
        private string _linearTooltip = "Pieces data unavailable";
        private string _linearAriaLabel = string.Empty;
        private string _heatmapEmptyText = "Pieces data unavailable";
        private string _heatmapAriaLabel = string.Empty;
        private IReadOnlyList<IReadOnlyList<HeatmapCellViewModel>> _heatmapRows = Array.Empty<IReadOnlyList<HeatmapCellViewModel>>();
        private IReadOnlyList<LegendItem> _legendItems = Array.Empty<LegendItem>();

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

        protected string LinearBarStyle => _linearBarStyle;

        protected string LinearSummary => _linearSummary;

        protected string LinearTooltip => _linearTooltip;

        protected string LinearAriaLabel => _linearAriaLabel;

        protected string HeatmapEmptyText => _heatmapEmptyText;

        protected string HeatmapAriaLabel => _heatmapAriaLabel;

        protected IReadOnlyList<IReadOnlyList<HeatmapCellViewModel>> HeatmapRows => _heatmapRows;

        protected IReadOnlyList<LegendItem> LegendItems => _legendItems;

        protected string ToggleIcon => _showHeatmap ? Icons.Material.Filled.ExpandLess : Icons.Material.Filled.ExpandMore;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            BuildLinearViewModel();
            BuildHeatmapViewModel();
            BuildLegend();
        }

        protected void ToggleHeatmap()
        {
            _showHeatmap = !_showHeatmap;
        }

        protected void HandleLinearKeyDown(KeyboardEventArgs args)
        {
            if (args.Key is "Enter" or " " or "Space" or "Spacebar")
            {
                _showHeatmap = !_showHeatmap;
            }
        }

        private void BuildLinearViewModel()
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
                "Pieces progress for torrent {0}: {1:0.#}% complete. {2} downloaded, {3} downloading, {4} pending. Toggle heatmap view.",
                Hash,
                percentComplete,
                downloadedCount,
                downloadingCount,
                pendingCount);
        }

        private void BuildHeatmapViewModel()
        {
            if (Pieces.Count == 0)
            {
                _heatmapRows = Array.Empty<IReadOnlyList<HeatmapCellViewModel>>();
                _heatmapEmptyText = "Heatmap unavailable without piece data.";
                _heatmapAriaLabel = $"Pieces heatmap unavailable for torrent {Hash}.";
                return;
            }

            var rows = new List<IReadOnlyList<HeatmapCellViewModel>>();
            for (var index = 0; index < Pieces.Count; index += HeatmapColumns)
            {
                var row = new List<HeatmapCellViewModel>(HeatmapColumns);
                for (var offset = 0; offset < HeatmapColumns; offset++)
                {
                    var pieceIndex = index + offset;
                    if (pieceIndex < Pieces.Count)
                    {
                        var state = Pieces[pieceIndex];
                        var tooltip = BuildHeatmapTooltip(pieceIndex, state);
                        row.Add(new HeatmapCellViewModel(GetHeatmapCssClass(state), tooltip, false));
                    }
                    else
                    {
                        row.Add(new HeatmapCellViewModel("pieces-progress-new__cell--empty", string.Empty, true));
                    }
                }

                rows.Add(row);
            }

            _heatmapRows = rows;
            _heatmapEmptyText = string.Empty;
            _heatmapAriaLabel = $"Pieces heatmap for torrent {Hash}.";
        }

        private void BuildLegend()
        {
            _legendItems = new[]
            {
                new LegendItem("pieces-progress-new__legend-swatch--downloaded", "Downloaded"),
                new LegendItem("pieces-progress-new__legend-swatch--downloading", "Downloading"),
                new LegendItem("pieces-progress-new__legend-swatch--pending", "Not downloaded")
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

        private static string BuildHeatmapTooltip(int index, PieceState state)
        {
            var stateDescription = state switch
            {
                PieceState.Downloaded => "Downloaded",
                PieceState.Downloading => "Downloading",
                _ => "Not downloaded"
            };
            return CreateInvariant(
                "Piece #{0}: {1}",
                index + 1,
                stateDescription);
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

        protected sealed record HeatmapCellViewModel(string CssClass, string Tooltip, bool IsPlaceholder);

        protected sealed record LegendItem(string CssClass, string Label);

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
