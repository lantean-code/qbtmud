using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class ShareRatioDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public ShareRatioMax? Value { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        protected int ShareRatioType { get; set; }

        protected bool RatioEnabled { get; set; }

        protected float Ratio { get; set; }

        protected bool TotalMinutesEnabled { get; set; }

        protected int TotalMinutes { get; set; }

        protected bool InactiveMinutesEnabled { get; set; }

        protected int InactiveMinutes { get; set; }

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

        protected override void OnParametersSet()
        {
            if (Value is null || (Value.RatioLimit == Limits.GlobalLimit && Value.SeedingTimeLimit == Limits.GlobalLimit && Value.InactiveSeedingTimeLimit == Limits.GlobalLimit))
            {
                ShareRatioType = Limits.GlobalLimit;
            }
            else if (Value.MaxRatio == Limits.NoLimit && Value.MaxSeedingTime == Limits.NoLimit && Value.MaxInactiveSeedingTime == Limits.NoLimit)
            {
                ShareRatioType = Limits.NoLimit;
            }
            else
            {
                ShareRatioType = 0;
                if (Value.RatioLimit >= 0)
                {
                    RatioEnabled = true;
                    Ratio = Value.RatioLimit;
                }
                if (Value.SeedingTimeLimit >= 0)
                {
                    TotalMinutesEnabled = true;
                    TotalMinutes = (int)Value.SeedingTimeLimit;
                }
                if (Value.InactiveSeedingTimeLimit >= 0)
                {
                    InactiveMinutesEnabled = true;
                    InactiveMinutes = (int)Value.InactiveSeedingTimeLimit;
                }
            }
        }

        protected void ShareRatioTypeChanged(int value)
        {
            ShareRatioType = value;
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
            }
            else if (ShareRatioType == Limits.NoLimit)
            {
                result.RatioLimit = result.SeedingTimeLimit = result.InactiveSeedingTimeLimit = Limits.NoLimit;
            }
            else
            {
                result.RatioLimit = RatioEnabled ? Ratio : Limits.NoLimit;
                result.SeedingTimeLimit = TotalMinutesEnabled ? TotalMinutes : Limits.NoLimit;
                result.InactiveSeedingTimeLimit = InactiveMinutesEnabled ? InactiveMinutes : Limits.NoLimit;
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