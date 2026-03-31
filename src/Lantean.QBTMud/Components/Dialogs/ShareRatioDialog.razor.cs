using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ShareRatioDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public ShareRatioMax? Value { get; set; }

        [Parameter]
        public ShareRatioMax? CurrentValue { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        protected int ShareRatioType { get; set; }

        protected bool RatioEnabled { get; set; }

        protected float Ratio { get; set; }

        protected bool TotalMinutesEnabled { get; set; }

        protected int TotalMinutes { get; set; }

        protected bool InactiveMinutesEnabled { get; set; }

        protected int InactiveMinutes { get; set; }

        protected ShareLimitAction SelectedShareLimitAction { get; set; } = ShareLimitAction.Default;

        protected bool CustomEnabled => ShareRatioType == 0;

        protected void RatioEnabledChanged(bool value)
        {
            RatioEnabled = value;
        }

        protected void RatioChanged(float value)
        {
            Ratio = value;
        }

        protected void TotalMinutesEnabledChanged(bool value)
        {
            TotalMinutesEnabled = value;
        }

        protected void TotalMinutesChanged(int value)
        {
            TotalMinutes = value;
        }

        protected void InactiveMinutesEnabledChanged(bool value)
        {
            InactiveMinutesEnabled = value;
        }

        protected void InactiveMinutesChanged(int value)
        {
            InactiveMinutes = value;
        }

        protected void ShareLimitActionChanged(ShareLimitAction value)
        {
            SelectedShareLimitAction = value;
        }

        protected override void OnParametersSet()
        {
            RatioEnabled = false;
            TotalMinutesEnabled = false;
            InactiveMinutesEnabled = false;

            var baseline = Value ?? CurrentValue;
            SelectedShareLimitAction = baseline?.ShareLimitAction ?? ShareLimitAction.Default;

            if (baseline is null || baseline.RatioLimit == Limits.UseGlobalShareLimit && baseline.SeedingTimeLimit == Limits.UseGlobalShareLimit && baseline.InactiveSeedingTimeLimit == Limits.UseGlobalShareLimit)
            {
                ShareRatioType = (int)Limits.UseGlobalShareLimit;
                return;
            }

            if (baseline.MaxRatio == Limits.NoShareLimit && baseline.MaxSeedingTime == Limits.NoShareLimit && baseline.MaxInactiveSeedingTime == Limits.NoShareLimit)
            {
                ShareRatioType = (int)Limits.NoShareLimit;
                return;
            }

            ShareRatioType = 0;

            if (baseline.RatioLimit >= 0)
            {
                RatioEnabled = true;
                Ratio = baseline.RatioLimit;
            }
            else
            {
                Ratio = 0;
            }

            if (baseline.SeedingTimeLimit >= 0)
            {
                TotalMinutesEnabled = true;
                TotalMinutes = (int)baseline.SeedingTimeLimit;
            }
            else
            {
                TotalMinutes = 0;
            }

            if (baseline.InactiveSeedingTimeLimit >= 0)
            {
                InactiveMinutesEnabled = true;
                InactiveMinutes = (int)baseline.InactiveSeedingTimeLimit;
            }
            else
            {
                InactiveMinutes = 0;
            }
        }

        protected void ShareRatioTypeChanged(int value)
        {
            ShareRatioType = value;
            if (!CustomEnabled)
            {
                RatioEnabled = false;
                TotalMinutesEnabled = false;
                InactiveMinutesEnabled = false;
                SelectedShareLimitAction = ShareLimitAction.Default;
            }
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            var result = new ShareRatio();
            if (ShareRatioType == (int)Limits.UseGlobalShareLimit)
            {
                result.RatioLimit = result.SeedingTimeLimit = result.InactiveSeedingTimeLimit = Limits.UseGlobalShareLimit;
                result.ShareLimitAction = ShareLimitAction.Default;
            }
            else if (ShareRatioType == (int)Limits.NoShareLimit)
            {
                result.RatioLimit = result.SeedingTimeLimit = result.InactiveSeedingTimeLimit = Limits.NoShareLimit;
                result.ShareLimitAction = ShareLimitAction.Default;
            }
            else
            {
                result.RatioLimit = RatioEnabled ? Ratio : Limits.NoShareLimit;
                result.SeedingTimeLimit = TotalMinutesEnabled ? TotalMinutes : Limits.NoShareLimit;
                result.InactiveSeedingTimeLimit = InactiveMinutesEnabled ? InactiveMinutes : Limits.NoShareLimit;
                result.ShareLimitAction = SelectedShareLimitAction;
            }
            MudDialog.Close(DialogResult.Ok(result));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
