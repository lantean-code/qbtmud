using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class ShareRatioDialogTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public ShareRatioDialogTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public async Task GIVEN_GlobalPreset_WHEN_Rendered_THEN_ShouldShowGlobalOptions()
        {
            var baseline = CreateShareRatioMax(Limits.GlobalLimit, Limits.GlobalLimit, Limits.GlobalLimit, ShareLimitAction.Remove);

            var dialog = await RenderDialogAsync(value: baseline);

            var radioGroup = dialog.Component.FindComponent<MudRadioGroup<int>>();
            radioGroup.Instance.Value.Should().Be(Limits.GlobalLimit);

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            switches.Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());

            var actionSelect = dialog.Component.FindComponent<MudSelect<ShareLimitAction>>();
            actionSelect.Instance.Value.Should().Be(ShareLimitAction.Remove);
        }

        [Fact]
        public async Task GIVEN_NoLimitPreset_WHEN_Rendered_THEN_ShouldShowNoLimitOptions()
        {
            var baseline = CreateShareRatioMax(-1, -1, -1, ShareLimitAction.Stop, maxValuesNoLimit: true);

            var dialog = await RenderDialogAsync(value: baseline);

            dialog.Component.FindComponent<MudRadioGroup<int>>().Instance.Value.Should().Be(Limits.NoLimit);

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            switches.Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());

            dialog.Component.FindComponent<MudSelect<ShareLimitAction>>().Instance.Value.Should().Be(ShareLimitAction.Stop);
        }

        [Fact]
        public async Task GIVEN_CustomPreset_WHEN_Rendered_THEN_ShouldPopulateCustomFields()
        {
            var baseline = CreateShareRatioMax(3.5f, 120, 45, ShareLimitAction.Remove);

            var dialog = await RenderDialogAsync(current: baseline);

            dialog.Component.FindComponent<MudRadioGroup<int>>().Instance.Value.Should().Be(0);

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            switches.Single(s => s.Instance.Label == "Ratio").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Total minutes").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Inactive minutes").Instance.Value.Should().BeTrue();

            dialog.Component.FindComponents<MudNumericField<float>>().Single().Instance.Value.Should().Be(3.5f);
            dialog.Component.FindComponents<MudNumericField<int>>().Single(f => f.Instance.Label == "Total minutes").Instance.Value.Should().Be(120);
            dialog.Component.FindComponents<MudNumericField<int>>().Single(f => f.Instance.Label == "Inactive minutes").Instance.Value.Should().Be(45);
            dialog.Component.FindComponent<MudSelect<ShareLimitAction>>().Instance.Value.Should().Be(ShareLimitAction.Remove);
        }

        [Fact]
        public async Task GIVEN_CustomPreset_WHEN_SelectGlobalPreset_THEN_ShouldResetCustomControls()
        {
            var baseline = CreateShareRatioMax(3.5f, 120, 45, ShareLimitAction.Remove);
            var dialog = await RenderDialogAsync(current: baseline);

            var radioGroup = dialog.Component.FindComponent<MudRadioGroup<int>>();
            await dialog.Component.InvokeAsync(() => radioGroup.Instance.ValueChanged.InvokeAsync(Limits.GlobalLimit));

            dialog.Component.FindComponents<FieldSwitch>().Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());
            dialog.Component.FindComponent<MudSelect<ShareLimitAction>>().Instance.Value.Should().Be(ShareLimitAction.Default);
        }

        [Fact]
        public async Task GIVEN_CustomValues_WHEN_Saved_THEN_ShouldEmitConfiguredShareRatio()
        {
            var dialog = await RenderDialogAsync(value: CreateShareRatioMax(1f, 5, 3, ShareLimitAction.Default));

            var radioGroup = dialog.Component.FindComponent<MudRadioGroup<int>>();
            await dialog.Component.InvokeAsync(() => radioGroup.Instance.ValueChanged.InvokeAsync(0));

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            var ratioSwitch = switches.Single(s => s.Instance.Label == "Ratio");
            var totalSwitch = switches.Single(s => s.Instance.Label == "Total minutes");
            var inactiveSwitch = switches.Single(s => s.Instance.Label == "Inactive minutes");

            await dialog.Component.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(true));
            await dialog.Component.InvokeAsync(() => totalSwitch.Instance.ValueChanged.InvokeAsync(true));
            await dialog.Component.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(true));

            var ratioField = dialog.Component.FindComponents<MudNumericField<float>>().Single();
            await dialog.Component.InvokeAsync(() => ratioField.Instance.ValueChanged.InvokeAsync(4.2f));

            var totalField = dialog.Component.FindComponents<MudNumericField<int>>().Single(f => f.Instance.Label == "Total minutes");
            await dialog.Component.InvokeAsync(() => totalField.Instance.ValueChanged.InvokeAsync(180));

            var inactiveField = dialog.Component.FindComponents<MudNumericField<int>>().Single(f => f.Instance.Label == "Inactive minutes");
            await dialog.Component.InvokeAsync(() => inactiveField.Instance.ValueChanged.InvokeAsync(60));

            var actionSelect = dialog.Component.FindComponent<MudSelect<ShareLimitAction>>();
            await dialog.Component.InvokeAsync(() => actionSelect.Instance.ValueChanged.InvokeAsync(ShareLimitAction.Remove));

            var saveButton = dialog.Dialog.FindAll("button").Single(b => b.TextContent.Trim() == "Save");
            await dialog.Component.InvokeAsync(() => saveButton.Click());

            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
            result.Data.Should().BeAssignableTo<ShareRatio>();
            var shareRatio = (ShareRatio)result.Data!;

            shareRatio.RatioLimit.Should().Be(4.2f);
            shareRatio.SeedingTimeLimit.Should().Be(180f);
            shareRatio.InactiveSeedingTimeLimit.Should().Be(60f);
            shareRatio.ShareLimitAction.Should().Be(ShareLimitAction.Remove);
        }

        private async Task<DialogRenderContext> RenderDialogAsync(ShareRatioMax? value = null, ShareRatioMax? current = null, bool disabled = false)
        {
            var provider = _context.RenderComponent<MudDialogProvider>();
            var dialogService = _context.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ShareRatioDialog.Value), value },
                { nameof(ShareRatioDialog.CurrentValue), current },
                { nameof(ShareRatioDialog.Disabled), disabled },
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
            };

            var reference = await dialogService.ShowAsync<ShareRatioDialog>("Share ratio", parameters, options);

            var dialog = provider.FindComponent<MudDialog>();
            var component = dialog.FindComponent<ShareRatioDialog>();

            return new DialogRenderContext(provider, dialog, component, reference);
        }

        private static ShareRatioMax CreateShareRatioMax(float ratio, int seedingTime, float inactive, ShareLimitAction action, bool maxValuesNoLimit = false)
        {
            return new ShareRatioMax
            {
                RatioLimit = ratio,
                SeedingTimeLimit = seedingTime,
                InactiveSeedingTimeLimit = inactive,
                ShareLimitAction = action,
                MaxRatio = maxValuesNoLimit ? Limits.NoLimit : ratio + 1,
                MaxSeedingTime = maxValuesNoLimit ? Limits.NoLimit : seedingTime + 1,
                MaxInactiveSeedingTime = maxValuesNoLimit ? Limits.NoLimit : inactive + 1,
            };
        }

        private sealed record DialogRenderContext(
            IRenderedComponent<MudDialogProvider> Provider,
            IRenderedComponent<MudDialog> Dialog,
            IRenderedComponent<ShareRatioDialog> Component,
            IDialogReference Reference);

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
