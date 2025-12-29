using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class Speed
    {
        private static readonly IReadOnlyList<(SpeedPeriod Period, string Label)> PeriodOptions = new List<(SpeedPeriod Period, string Label)>
        {
            (SpeedPeriod.Min1, "1m"),
            (SpeedPeriod.Min5, "5m"),
            (SpeedPeriod.Min30, "30m"),
            (SpeedPeriod.Hour3, "3h"),
            (SpeedPeriod.Hour6, "6h"),
            (SpeedPeriod.Hour12, "12h"),
            (SpeedPeriod.Hour24, "24h")
        };

        private static readonly string[] Palette = ["#86c43f", "#3299ff"];

        private static readonly Dictionary<SpeedPeriod, TimeSpan> BucketSizes = new()
        {
            { SpeedPeriod.Min1, TimeSpan.FromSeconds(2) },
            { SpeedPeriod.Min5, TimeSpan.FromSeconds(5) },
            { SpeedPeriod.Min30, TimeSpan.FromSeconds(15) },
            { SpeedPeriod.Hour3, TimeSpan.FromMinutes(1) },
            { SpeedPeriod.Hour6, TimeSpan.FromMinutes(2) },
            { SpeedPeriod.Hour12, TimeSpan.FromMinutes(4) },
            { SpeedPeriod.Hour24, TimeSpan.FromMinutes(8) }
        };

        private static readonly SpeedSeriesBuilder SeriesBuilder = new();

        private static readonly Dictionary<SpeedPeriod, TimeSpan> LabelSpacings = new()
        {
            { SpeedPeriod.Min1, TimeSpan.FromSeconds(10) },
            { SpeedPeriod.Min5, TimeSpan.FromSeconds(30) },
            { SpeedPeriod.Min30, TimeSpan.FromMinutes(5) },
            { SpeedPeriod.Hour3, TimeSpan.FromMinutes(15) },
            { SpeedPeriod.Hour6, TimeSpan.FromMinutes(30) },
            { SpeedPeriod.Hour12, TimeSpan.FromHours(1) },
            { SpeedPeriod.Hour24, TimeSpan.FromHours(2) }
        };

        private static readonly UnitScale BytesPerSecond = new(1, "B/s");
        private static readonly UnitScale KibiBytesPerSecond = new(1024, "KiB/s");
        private static readonly UnitScale MebiBytesPerSecond = new(1024 * 1024, "MiB/s");
        private static readonly UnitScale GibiBytesPerSecond = new(1024 * 1024 * 1024, "GiB/s");

        private SpeedPeriod _selectedPeriod = SpeedPeriod.Min5;
        private bool _includeDownload = true;
        private bool _includeUpload = true;
        private List<TimeSeriesChartSeries> _series = new();
        private ChartOptions _chartOptions = new();
        private AxisChartOptions _axisOptions = new();
        private TimeSpan _timeLabelSpacing = TimeSpan.FromMinutes(5);
        private string _lastUpdatedText = "n/a";
        private UnitScale _currentUnit = MebiBytesPerSecond;

        [Inject]
        protected ISpeedHistoryService SpeedHistoryService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter]
        public MainData? MainData { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await SpeedHistoryService.InitializeAsync();
            await LoadSeriesAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            await LoadSeriesAsync();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        private async Task OnPeriodChanged(SpeedPeriod period)
        {
            if (_selectedPeriod == period)
            {
                return;
            }

            _selectedPeriod = period;
            await LoadSeriesAsync();
        }

        private async Task ToggleDownload(bool enabled)
        {
            if (_includeDownload == enabled)
            {
                return;
            }

            _includeDownload = enabled;
            await LoadSeriesAsync();
        }

        private async Task ToggleUpload(bool enabled)
        {
            if (_includeUpload == enabled)
            {
                return;
            }

            _includeUpload = enabled;
            await LoadSeriesAsync();
        }

        private async Task LoadSeriesAsync()
        {
            await SpeedHistoryService.InitializeAsync();

            var downloadSamples = SpeedHistoryService.GetSeries(_selectedPeriod, SpeedDirection.Download);
            var uploadSamples = SpeedHistoryService.GetSeries(_selectedPeriod, SpeedDirection.Upload);

            var paletteInfo = BuildChartData(downloadSamples, uploadSamples);

            _chartOptions = new ChartOptions
            {
                ChartPalette = BuildPalette(paletteInfo.DownloadSegmentCount, paletteInfo.UploadSegmentCount, paletteInfo.HasBounds),
                ShowLegend = false,
                XAxisLines = true,
                YAxisToStringFunc = value => FormatBytesPerSecond(value, _currentUnit),
                MaxNumYAxisTicks = 6,
                LineStrokeWidth = 2
            };

            _axisOptions = new AxisChartOptions
            {
                MatchBoundsToSize = true
            };

            _lastUpdatedText = SpeedHistoryService.LastUpdatedUtc?.ToLocalTime().ToString("G") ?? "n/a";
        }

        private (int DownloadSegmentCount, int UploadSegmentCount, bool HasBounds) BuildChartData(IReadOnlyList<SpeedPoint> downloadSamples, IReadOnlyList<SpeedPoint> uploadSamples)
        {
            var windowEnd = SpeedHistoryService.LastUpdatedUtc ?? DateTime.UtcNow;
            var windowStart = windowEnd - GetPeriodDuration(_selectedPeriod);
            var bucketSize = BucketSizes.TryGetValue(_selectedPeriod, out var size) ? size : TimeSpan.FromMinutes(1);
            _timeLabelSpacing = LabelSpacings.TryGetValue(_selectedPeriod, out var spacing) ? spacing : TimeSpan.FromMinutes(1);

            var downloadSegments = SeriesBuilder.BuildSegments(downloadSamples, windowStart, windowEnd, bucketSize);
            var uploadSegments = SeriesBuilder.BuildSegments(uploadSamples, windowStart, windowEnd, bucketSize);
            _currentUnit = SelectUnit(downloadSegments.SelectMany(s => s).ToList(), uploadSegments.SelectMany(s => s).ToList());

            var seriesList = new List<TimeSeriesChartSeries>();
            if (_includeDownload)
            {
                foreach (var segment in downloadSegments)
                {
                    seriesList.Add(new TimeSeriesChartSeries
                    {
                        Name = "Download",
                        Data = segment,
                        LineDisplayType = LineDisplayType.Line
                    });
                }
            }

            if (_includeUpload)
            {
                foreach (var segment in uploadSegments)
                {
                    seriesList.Add(new TimeSeriesChartSeries
                    {
                        Name = "Upload",
                        Data = segment,
                        LineDisplayType = LineDisplayType.Line
                    });
                }
            }

            var hasBounds = false;
            if (seriesList.Count > 0)
            {
                seriesList.Add(new TimeSeriesChartSeries
                {
                    Name = string.Empty,
                    Data = BuildBounds(windowStart, windowEnd),
                    LineDisplayType = LineDisplayType.Line,
                    ShowDataMarkers = false,
                    StrokeOpacity = 0,
                    FillOpacity = 0
                });
                hasBounds = true;
            }

            _series = seriesList;
            return (_includeDownload ? downloadSegments.Count : 0, _includeUpload ? uploadSegments.Count : 0, hasBounds);
        }

        private static List<TimeSeriesChartSeries.TimeValue> BuildBounds(DateTime windowStart, DateTime windowEnd)
        {
            return new List<TimeSeriesChartSeries.TimeValue>
            {
                new(windowStart, 0),
                new(windowEnd, 0)
            };
        }

        private static string[] BuildPalette(int downloadSegmentCount, int uploadSegmentCount, bool hasBounds)
        {
            var palette = new List<string>();

            for (var i = 0; i < downloadSegmentCount; i++)
            {
                palette.Add(Palette[0]);
            }

            for (var i = 0; i < uploadSegmentCount; i++)
            {
                palette.Add(Palette[1]);
            }

            if (hasBounds)
            {
                palette.Add("transparent");
            }

            return palette.ToArray();
        }

        private static UnitScale SelectUnit(IReadOnlyList<TimeSeriesChartSeries.TimeValue> downloadSeries, IReadOnlyList<TimeSeriesChartSeries.TimeValue> uploadSeries)
        {
            var maxValue = Math.Max(
                downloadSeries.Count > 0 ? downloadSeries.Max(p => p.Value) : 0,
                uploadSeries.Count > 0 ? uploadSeries.Max(p => p.Value) : 0);

            if (maxValue >= GibiBytesPerSecond.Factor)
            {
                return GibiBytesPerSecond;
            }

            if (maxValue >= MebiBytesPerSecond.Factor)
            {
                return MebiBytesPerSecond;
            }

            if (maxValue >= KibiBytesPerSecond.Factor)
            {
                return KibiBytesPerSecond;
            }

            return BytesPerSecond;
        }

        private static string FormatBytesPerSecond(double value, UnitScale unit)
        {
            var scaled = value / unit.Factor;
            var format = scaled >= 100 ? "0" : "0.0";
            return $"{scaled.ToString(format)} {unit.Label}";
        }

        private sealed record UnitScale(double Factor, string Label);

        private static TimeSpan GetPeriodDuration(SpeedPeriod period)
        {
            return period switch
            {
                SpeedPeriod.Min1 => TimeSpan.FromMinutes(1),
                SpeedPeriod.Min5 => TimeSpan.FromMinutes(5),
                SpeedPeriod.Min30 => TimeSpan.FromMinutes(30),
                SpeedPeriod.Hour3 => TimeSpan.FromHours(3),
                SpeedPeriod.Hour6 => TimeSpan.FromHours(6),
                SpeedPeriod.Hour12 => TimeSpan.FromHours(12),
                SpeedPeriod.Hour24 => TimeSpan.FromHours(24),
                _ => TimeSpan.FromMinutes(5)
            };
        }
    }
}
