using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

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

            if (baseline is null || baseline.RatioLimit == Limits.GlobalLimit && baseline.SeedingTimeLimit == Limits.GlobalLimit && baseline.InactiveSeedingTimeLimit == Limits.GlobalLimit)
            {
                ShareRatioType = Limits.GlobalLimit;
                return;
            }

            if (baseline.MaxRatio == Limits.NoLimit && baseline.MaxSeedingTime == Limits.NoLimit && baseline.MaxInactiveSeedingTime == Limits.NoLimit)
            {
                ShareRatioType = Limits.NoLimit;
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
            if (ShareRatioType == Limits.GlobalLimit)
            {
                result.RatioLimit = result.SeedingTimeLimit = result.InactiveSeedingTimeLimit = Limits.GlobalLimit;
                result.ShareLimitAction = ShareLimitAction.Default;
            }
            else if (ShareRatioType == Limits.NoLimit)
            {
                result.RatioLimit = result.SeedingTimeLimit = result.InactiveSeedingTimeLimit = Limits.NoLimit;
                result.ShareLimitAction = ShareLimitAction.Default;
            }
            else
            {
                result.RatioLimit = RatioEnabled ? Ratio : Limits.NoLimit;
                result.SeedingTimeLimit = TotalMinutesEnabled ? TotalMinutes : Limits.NoLimit;
                result.InactiveSeedingTimeLimit = InactiveMinutesEnabled ? InactiveMinutes : Limits.NoLimit;
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