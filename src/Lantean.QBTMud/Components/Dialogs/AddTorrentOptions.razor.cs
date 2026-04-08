using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class AddTorrentOptions
    {
        private readonly List<CategoryOption> _categoryOptions = new();
        private readonly Dictionary<string, CategoryOption> _categoryLookup = new(StringComparer.Ordinal);
        private string _manualSavePath = string.Empty;
        private bool _manualUseDownloadPath;
        private string _manualDownloadPath = string.Empty;
        private string _defaultSavePath = string.Empty;
        private string _defaultDownloadPath = string.Empty;
        private bool _defaultDownloadPathEnabled;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Parameter]
        public bool ShowCookieOption { get; set; }

        protected bool Expanded { get; set; }

        protected bool TorrentManagementMode { get; set; }

        protected string SavePath { get; set; } = string.Empty;

        protected string DownloadPath { get; set; } = string.Empty;

        protected bool UseDownloadPath { get; set; }

        protected bool DownloadPathDisabled => TorrentManagementMode || !UseDownloadPath;

        protected string? Cookie { get; set; }

        protected string? RenameTorrent { get; set; }

        protected IReadOnlyList<CategoryOption> CategoryOptions => _categoryOptions;

        protected string? Category { get; set; } = string.Empty;

        protected List<string> AvailableTags { get; private set; } = [];

        protected HashSet<string> SelectedTags { get; private set; } = new(StringComparer.Ordinal);

        protected bool StartTorrent { get; set; } = true;

        protected bool AddToTopOfQueue { get; set; } = true;

        protected StopCondition StopCondition { get; set; } = StopCondition.None;

        protected bool SkipHashCheck { get; set; }

        protected TorrentContentLayout ContentLayout { get; set; } = TorrentContentLayout.Original;

        protected bool DownloadInSequentialOrder { get; set; }

        protected bool DownloadFirstAndLastPiecesFirst { get; set; }

        protected int DownloadLimit { get; set; }

        protected int UploadLimit { get; set; }

        protected ShareLimitMode SelectedShareLimitMode { get; set; } = ShareLimitMode.Global;

        protected bool RatioLimitEnabled { get; set; }

        protected double RatioLimit { get; set; } = 1.0;

        protected bool SeedingTimeLimitEnabled { get; set; }

        protected int SeedingTimeLimit { get; set; } = 1440;

        protected bool InactiveSeedingTimeLimitEnabled { get; set; }

        protected int InactiveSeedingTimeLimit { get; set; } = 1440;

        protected bool IsCustomShareLimit => SelectedShareLimitMode == ShareLimitMode.Custom;

        protected override async Task OnInitializedAsync()
        {
            var categoriesResult = await ApiClient.GetAllCategoriesAsync();
            if (categoriesResult.TryGetValue(out var categoryDictionary))
            {
                foreach (var (name, value) in categoryDictionary.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var option = new CategoryOption(name, value.SavePath, value.DownloadPath);
                    _categoryOptions.Add(option);
                    _categoryLookup[name] = option;
                }
            }

            var tagsResult = await ApiClient.GetAllTagsAsync();
            AvailableTags = tagsResult.TryGetValue(out var availableTags)
                ? availableTags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList()
                : [];

            var preferencesResult = await ApiClient.GetApplicationPreferencesAsync();
            if (!preferencesResult.TryGetValue(out var applicationPreferences))
            {
                return;
            }

            TorrentManagementMode = applicationPreferences.AutoTmmEnabled;

            _defaultSavePath = applicationPreferences.SavePath ?? string.Empty;
            _manualSavePath = _defaultSavePath;
            SavePath = _defaultSavePath;

            _defaultDownloadPath = applicationPreferences.TempPath ?? string.Empty;
            _defaultDownloadPathEnabled = applicationPreferences.TempPathEnabled;
            _manualDownloadPath = _defaultDownloadPath;
            _manualUseDownloadPath = applicationPreferences.TempPathEnabled;
            UseDownloadPath = _manualUseDownloadPath;
            DownloadPath = UseDownloadPath ? _manualDownloadPath : string.Empty;

            StartTorrent = !applicationPreferences.AddStoppedEnabled;
            AddToTopOfQueue = applicationPreferences.AddToTopOfQueue;
            StopCondition = applicationPreferences.TorrentStopCondition;
            ContentLayout = applicationPreferences.TorrentContentLayout;

            RatioLimitEnabled = applicationPreferences.MaxRatioEnabled;
            RatioLimit = applicationPreferences.MaxRatio;
            SeedingTimeLimitEnabled = applicationPreferences.MaxSeedingTimeEnabled;
            if (applicationPreferences.MaxSeedingTimeEnabled)
            {
                SeedingTimeLimit = applicationPreferences.MaxSeedingTime;
            }
            InactiveSeedingTimeLimitEnabled = applicationPreferences.MaxInactiveSeedingTimeEnabled;
            if (applicationPreferences.MaxInactiveSeedingTimeEnabled)
            {
                InactiveSeedingTimeLimit = applicationPreferences.MaxInactiveSeedingTime;
            }
            if (TorrentManagementMode)
            {
                ApplyAutomaticPaths();
            }
        }

        protected void SetTorrentManagementMode(bool value)
        {
            if (TorrentManagementMode == value)
            {
                return;
            }

            TorrentManagementMode = value;
            if (TorrentManagementMode)
            {
                ApplyAutomaticPaths();
            }
            else
            {
                RestoreManualPaths();
            }
        }

        protected void SavePathChanged(string value)
        {
            SavePath = value;
            if (!TorrentManagementMode)
            {
                _manualSavePath = value;
            }
        }

        protected void SetUseDownloadPath(bool value)
        {
            if (TorrentManagementMode)
            {
                return;
            }

            _manualUseDownloadPath = value;
            UseDownloadPath = value;

            if (value)
            {
                if (string.IsNullOrWhiteSpace(_manualDownloadPath))
                {
                    _manualDownloadPath = string.IsNullOrWhiteSpace(_defaultDownloadPath) ? string.Empty : _defaultDownloadPath;
                }

                DownloadPath = _manualDownloadPath;
            }
            else
            {
                _manualDownloadPath = DownloadPath;
                DownloadPath = string.Empty;
            }
        }

        protected void DownloadPathChanged(string value)
        {
            DownloadPath = value;
            if (!TorrentManagementMode && UseDownloadPath)
            {
                _manualDownloadPath = value;
            }
        }

        protected void CategoryChanged(string? value)
        {
            Category = string.IsNullOrWhiteSpace(value) ? null : value;
            if (TorrentManagementMode)
            {
                ApplyAutomaticPaths();
            }
        }

        protected void SelectedTagsChanged(IEnumerable<string> tags)
        {
            SelectedTags = tags is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(tags, StringComparer.Ordinal);
        }

        protected void StopConditionChanged(StopCondition value)
        {
            StopCondition = value;
        }

        protected void ContentLayoutChanged(TorrentContentLayout value)
        {
            ContentLayout = value;
        }

        protected void ShareLimitModeChanged(ShareLimitMode mode)
        {
            SelectedShareLimitMode = mode;
            if (mode != ShareLimitMode.Custom)
            {
                RatioLimitEnabled = false;
                SeedingTimeLimitEnabled = false;
                InactiveSeedingTimeLimitEnabled = false;
            }
        }

        protected void RatioLimitEnabledChanged(bool value)
        {
            RatioLimitEnabled = value;
        }

        protected void RatioLimitChanged(double value)
        {
            RatioLimit = value;
        }

        protected void SeedingTimeLimitEnabledChanged(bool value)
        {
            SeedingTimeLimitEnabled = value;
        }

        protected void SeedingTimeLimitChanged(int value)
        {
            SeedingTimeLimit = value;
        }

        protected void InactiveSeedingTimeLimitEnabledChanged(bool value)
        {
            InactiveSeedingTimeLimitEnabled = value;
        }

        protected void InactiveSeedingTimeLimitChanged(int value)
        {
            InactiveSeedingTimeLimit = value;
        }

        public TorrentOptions GetTorrentOptions()
        {
            var options = new TorrentOptions(
                TorrentManagementMode,
                _manualSavePath,
                Cookie,
                RenameTorrent,
                string.IsNullOrWhiteSpace(Category) ? null : Category,
                StartTorrent,
                AddToTopOfQueue,
                StopCondition,
                SkipHashCheck,
                ContentLayout,
                DownloadInSequentialOrder,
                DownloadFirstAndLastPiecesFirst,
                DownloadLimit,
                UploadLimit);

            options.UseDownloadPath = TorrentManagementMode ? null : UseDownloadPath;
            options.DownloadPath = (!TorrentManagementMode && UseDownloadPath) ? DownloadPath : null;
            options.Tags = SelectedTags.Count > 0 ? SelectedTags.ToArray() : null;

            switch (SelectedShareLimitMode)
            {
                case ShareLimitMode.Global:
                    options.RatioLimit = Limits.UseGlobalShareRatioLimit;
                    options.SeedingTimeLimit = Limits.UseGlobalSeedingTimeLimit;
                    options.InactiveSeedingTimeLimit = Limits.UseGlobalInactiveSeedingTimeLimit;
                    break;

                case ShareLimitMode.NoLimit:
                    options.RatioLimit = Limits.NoShareRatioLimit;
                    options.SeedingTimeLimit = Limits.NoSeedingTimeLimit;
                    options.InactiveSeedingTimeLimit = Limits.NoInactiveSeedingTimeLimit;
                    break;

                case ShareLimitMode.Custom:
                    options.RatioLimit = RatioLimitEnabled ? RatioLimit : Limits.NoShareRatioLimit;
                    options.SeedingTimeLimit = SeedingTimeLimitEnabled ? SeedingTimeLimit : Limits.NoSeedingTimeLimit;
                    options.InactiveSeedingTimeLimit = InactiveSeedingTimeLimitEnabled ? InactiveSeedingTimeLimit : Limits.NoInactiveSeedingTimeLimit;
                    break;
            }

            return options;
        }

        private void ApplyAutomaticPaths()
        {
            SavePath = ResolveAutomaticSavePath();
            var (enabled, path) = ResolveAutomaticDownloadPath();
            UseDownloadPath = enabled;
            DownloadPath = enabled ? path : string.Empty;
        }

        private void RestoreManualPaths()
        {
            SavePath = _manualSavePath;
            UseDownloadPath = _manualUseDownloadPath;
            DownloadPath = _manualUseDownloadPath ? _manualDownloadPath : string.Empty;
        }

        private string ResolveAutomaticSavePath()
        {
            var category = GetSelectedCategory();
            if (category is null)
            {
                return _defaultSavePath;
            }

            if (!string.IsNullOrWhiteSpace(category.SavePath))
            {
                return category.SavePath!;
            }

            if (!string.IsNullOrWhiteSpace(_defaultSavePath))
            {
                return Path.Combine(_defaultSavePath, category.Name);
            }

            return _defaultSavePath;
        }

        private (bool Enabled, string Path) ResolveAutomaticDownloadPath()
        {
            var category = GetSelectedCategory();
            if (category is null)
            {
                if (!_defaultDownloadPathEnabled)
                {
                    return (false, string.Empty);
                }

                return (true, _defaultDownloadPath);
            }

            if (category.DownloadPath is null)
            {
                if (!_defaultDownloadPathEnabled)
                {
                    return (false, string.Empty);
                }

                return (true, ComposeDefaultDownloadPath(category.Name));
            }

            if (!category.DownloadPath.Enabled)
            {
                return (false, string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(category.DownloadPath.Path))
            {
                return (true, category.DownloadPath.Path!);
            }

            return (true, ComposeDefaultDownloadPath(category.Name));
        }

        private string ComposeDefaultDownloadPath(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(_defaultDownloadPath))
            {
                return string.Empty;
            }

            return Path.Combine(_defaultDownloadPath, categoryName);
        }

        private CategoryOption? GetSelectedCategory()
        {
            if (string.IsNullOrWhiteSpace(Category))
            {
                return null;
            }

            return _categoryLookup.TryGetValue(Category, out var option) ? option : null;
        }

        protected internal enum ShareLimitMode
        {
            Global,
            NoLimit,
            Custom
        }

        protected sealed record CategoryOption(string Name, string? SavePath, DownloadPathOption? DownloadPath);
    }
}
