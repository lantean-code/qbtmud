using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;

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

        protected string StopCondition { get; set; } = "None";

        protected bool SkipHashCheck { get; set; }

        protected string ContentLayout { get; set; } = "Original";

        protected bool DownloadInSequentialOrder { get; set; }

        protected bool DownloadFirstAndLastPiecesFirst { get; set; }

        protected long DownloadLimit { get; set; }

        protected long UploadLimit { get; set; }

        protected ShareLimitMode SelectedShareLimitMode { get; set; } = ShareLimitMode.Global;

        protected bool RatioLimitEnabled { get; set; }

        protected float RatioLimit { get; set; } = 1.0f;

        protected bool SeedingTimeLimitEnabled { get; set; }

        protected int SeedingTimeLimit { get; set; } = 1440;

        protected bool InactiveSeedingTimeLimitEnabled { get; set; }

        protected int InactiveSeedingTimeLimit { get; set; } = 1440;

        protected ShareLimitAction SelectedShareLimitAction { get; set; } = ShareLimitAction.Default;

        protected bool IsCustomShareLimit => SelectedShareLimitMode == ShareLimitMode.Custom;

        protected override async Task OnInitializedAsync()
        {
            var categories = await ApiClient.GetAllCategories();
            foreach (var (name, value) in categories.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                var option = new CategoryOption(name, value.SavePath, value.DownloadPath);
                _categoryOptions.Add(option);
                _categoryLookup[name] = option;
            }

            var tags = await ApiClient.GetAllTags();
            AvailableTags = tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();

            var preferences = await ApiClient.GetApplicationPreferences();

            TorrentManagementMode = preferences.AutoTmmEnabled;

            _defaultSavePath = preferences.SavePath ?? string.Empty;
            _manualSavePath = _defaultSavePath;
            SavePath = _defaultSavePath;

            _defaultDownloadPath = preferences.TempPath ?? string.Empty;
            _defaultDownloadPathEnabled = preferences.TempPathEnabled;
            _manualDownloadPath = _defaultDownloadPath;
            _manualUseDownloadPath = preferences.TempPathEnabled;
            UseDownloadPath = _manualUseDownloadPath;
            DownloadPath = UseDownloadPath ? _manualDownloadPath : string.Empty;

            StartTorrent = !preferences.AddStoppedEnabled;
            AddToTopOfQueue = preferences.AddToTopOfQueue;
            StopCondition = preferences.TorrentStopCondition;
            ContentLayout = preferences.TorrentContentLayout;

            RatioLimitEnabled = preferences.MaxRatioEnabled;
            RatioLimit = preferences.MaxRatio;
            SeedingTimeLimitEnabled = preferences.MaxSeedingTimeEnabled;
            if (preferences.MaxSeedingTimeEnabled)
            {
                SeedingTimeLimit = preferences.MaxSeedingTime;
            }
            InactiveSeedingTimeLimitEnabled = preferences.MaxInactiveSeedingTimeEnabled;
            if (preferences.MaxInactiveSeedingTimeEnabled)
            {
                InactiveSeedingTimeLimit = preferences.MaxInactiveSeedingTime;
            }
            SelectedShareLimitAction = MapShareLimitAction(preferences.MaxRatioAct);

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

        protected void StopConditionChanged(string value)
        {
            StopCondition = value;
        }

        protected void ContentLayoutChanged(string value)
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
                SelectedShareLimitAction = ShareLimitAction.Default;
            }
        }

        protected void RatioLimitEnabledChanged(bool value)
        {
            RatioLimitEnabled = value;
        }

        protected void RatioLimitChanged(float value)
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

        protected void ShareLimitActionChanged(ShareLimitAction value)
        {
            SelectedShareLimitAction = value;
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
                    options.RatioLimit = Limits.GlobalLimit;
                    options.SeedingTimeLimit = Limits.GlobalLimit;
                    options.InactiveSeedingTimeLimit = Limits.GlobalLimit;
                    options.ShareLimitAction = ShareLimitAction.Default.ToString();
                    break;

                case ShareLimitMode.NoLimit:
                    options.RatioLimit = Limits.NoLimit;
                    options.SeedingTimeLimit = Limits.NoLimit;
                    options.InactiveSeedingTimeLimit = Limits.NoLimit;
                    options.ShareLimitAction = ShareLimitAction.Default.ToString();
                    break;

                case ShareLimitMode.Custom:
                    options.RatioLimit = RatioLimitEnabled ? RatioLimit : Limits.NoLimit;
                    options.SeedingTimeLimit = SeedingTimeLimitEnabled ? SeedingTimeLimit : Limits.NoLimit;
                    options.InactiveSeedingTimeLimit = InactiveSeedingTimeLimitEnabled ? InactiveSeedingTimeLimit : Limits.NoLimit;
                    options.ShareLimitAction = SelectedShareLimitAction.ToString();
                    break;
            }

            return options;
        }

        private void ApplyAutomaticPaths()
        {
            SavePath = ResolveAutomaticSavePath();
            var (enabled, path) = ResolveAutomaticDownloadPath();
            UseDownloadPath = enabled;
            DownloadPath = enabled ? path ?? string.Empty : string.Empty;
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

            if (!string.IsNullOrWhiteSpace(_defaultSavePath) && !string.IsNullOrWhiteSpace(category.Name))
            {
                return Path.Combine(_defaultSavePath, category.Name);
            }

            return _defaultSavePath;
        }

        private (bool Enabled, string? Path) ResolveAutomaticDownloadPath()
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
                return (true, category.DownloadPath.Path);
            }

            return (true, ComposeDefaultDownloadPath(category.Name));
        }

        private string ComposeDefaultDownloadPath(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(_defaultDownloadPath))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return _defaultDownloadPath;
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

        private static ShareLimitAction MapShareLimitAction(int preferenceValue)
        {
            return preferenceValue switch
            {
                0 => ShareLimitAction.Stop,
                1 => ShareLimitAction.Remove,
                2 => ShareLimitAction.RemoveWithContent,
                3 => ShareLimitAction.EnableSuperSeeding,
                _ => ShareLimitAction.Default
            };
        }

        protected enum ShareLimitMode
        {
            Global,
            NoLimit,
            Custom
        }

        protected sealed record CategoryOption(string Name, string? SavePath, DownloadPathOption? DownloadPath);
    }
}