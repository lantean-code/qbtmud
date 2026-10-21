using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient.Models;
using Category = Lantean.QBTMud.Core.Models.Category;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class CategoryPropertiesDialog
    {
        private string _savePath = string.Empty;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public QBittorrentPreferences? Preferences { get; set; }

        [Parameter]
        public string? Category { get; set; }

        [Parameter]
        public string? SavePath { get; set; }

        [Parameter]
        public bool SupportsCategoryOptions { get; set; }

        [Parameter]
        public QBittorrent.ApiClient.Models.Category? OriginalCategory { get; set; }

        protected int DownloadPathMode { get; set; }

        protected string? DownloadPath { get; set; }

        protected int RatioMode { get; set; } = -2;

        protected int SeedingTimeMode { get; set; } = -2;

        protected int InactiveSeedingTimeMode { get; set; } = -2;

        protected double RatioLimit { get; set; } = -2;

        protected int SeedingTimeLimit { get; set; } = -2;

        protected int InactiveSeedingTimeLimit { get; set; } = -2;

        protected ShareLimitsMode ShareLimitsMode { get; set; } = ShareLimitsMode.Default;

        protected ShareLimitAction ShareLimitAction { get; set; } = ShareLimitAction.Default;

        protected override Task OnInitializedAsync()
        {
            _savePath = Preferences?.SavePath ?? string.Empty;
            SavePath ??= _savePath;
            DownloadPathMode = OriginalCategory?.DownloadPath is null ? 0 : OriginalCategory.DownloadPath.Enabled ? 2 : 1;
            DownloadPath = OriginalCategory?.DownloadPath?.Path;
            RatioLimit = OriginalCategory?.RatioLimit ?? -2;
            SeedingTimeLimit = OriginalCategory?.SeedingTimeLimit ?? -2;
            InactiveSeedingTimeLimit = OriginalCategory?.InactiveSeedingTimeLimit ?? -2;
            RatioMode = RatioLimit < 0 ? (int)RatioLimit : 0;
            SeedingTimeMode = SeedingTimeLimit < 0 ? SeedingTimeLimit : 0;
            InactiveSeedingTimeMode = InactiveSeedingTimeLimit < 0 ? InactiveSeedingTimeLimit : 0;
            RatioLimit = Math.Max(0, RatioLimit);
            SeedingTimeLimit = Math.Max(0, SeedingTimeLimit);
            InactiveSeedingTimeLimit = Math.Max(0, InactiveSeedingTimeLimit);
            ShareLimitsMode = OriginalCategory?.ShareLimitsMode ?? ShareLimitsMode.Default;
            ShareLimitAction = OriginalCategory?.ShareLimitAction ?? ShareLimitAction.Default;

            return Task.CompletedTask;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            if (Category is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(SavePath))
            {
                SavePath = _savePath;
            }

            var category = new Category(Category, SavePath);
            if (SupportsCategoryOptions)
            {
                category.Options = new TorrentCategoryOptions
                {
                    SavePath = SavePath,
                    InheritDownloadPath = DownloadPathMode == 0,
                    DownloadPath = DownloadPathMode == 0 ? null : new DownloadPathOption(DownloadPathMode == 2, DownloadPath),
                    RatioLimit = RatioMode == 0 ? RatioLimit : RatioMode,
                    SeedingTimeLimit = SeedingTimeMode == 0 ? SeedingTimeLimit : SeedingTimeMode,
                    InactiveSeedingTimeLimit = InactiveSeedingTimeMode == 0 ? InactiveSeedingTimeLimit : InactiveSeedingTimeMode,
                    ShareLimitsMode = ShareLimitsMode,
                    ShareLimitAction = ShareLimitAction
                };
            }

            MudDialog.Close(DialogResult.Ok(category));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
