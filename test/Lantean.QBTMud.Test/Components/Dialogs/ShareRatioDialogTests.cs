using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
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

            var radioGroup = FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType");
            radioGroup.Instance.Value.Should().Be(Limits.GlobalLimit);

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            switches.Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());

            var actionSelect = FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction");
            actionSelect.Instance.Value.Should().Be(ShareLimitAction.Remove);
        }

        [Fact]
        public async Task GIVEN_NoLimitPreset_WHEN_Rendered_THEN_ShouldShowNoLimitOptions()
        {
            var baseline = CreateShareRatioMax(-1, -1, -1, ShareLimitAction.Stop, maxValuesNoLimit: true);

            var dialog = await RenderDialogAsync(value: baseline);

            FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType").Instance.Value.Should().Be(Limits.NoLimit);

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            switches.Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());

            FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction").Instance.Value.Should().Be(ShareLimitAction.Stop);
        }

        [Fact]
        public async Task GIVEN_CustomPreset_WHEN_Rendered_THEN_ShouldPopulateCustomFields()
        {
            var baseline = CreateShareRatioMax(3.5f, 120, 45, ShareLimitAction.Remove);

            var dialog = await RenderDialogAsync(current: baseline);

            FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType").Instance.Value.Should().Be(0);

            FindComponentByTestId<FieldSwitch>(dialog.Component, "RatioEnabled").Instance.Value.Should().BeTrue();
            FindComponentByTestId<FieldSwitch>(dialog.Component, "TotalMinutesEnabled").Instance.Value.Should().BeTrue();
            FindComponentByTestId<FieldSwitch>(dialog.Component, "InactiveMinutesEnabled").Instance.Value.Should().BeTrue();

            FindComponentByTestId<MudNumericField<float>>(dialog.Component, "Ratio").Instance.Value.Should().Be(3.5f);
            FindComponentByTestId<MudNumericField<int>>(dialog.Component, "TotalMinutes").Instance.Value.Should().Be(120);
            FindComponentByTestId<MudNumericField<int>>(dialog.Component, "InactiveMinutes").Instance.Value.Should().Be(45);
            FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction").Instance.Value.Should().Be(ShareLimitAction.Remove);
        }

        [Fact]
        public async Task GIVEN_CustomPreset_WHEN_SelectGlobalPreset_THEN_ShouldResetCustomControls()
        {
            var baseline = CreateShareRatioMax(3.5f, 120, 45, ShareLimitAction.Remove);
            var dialog = await RenderDialogAsync(current: baseline);

            var radioGroup = FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType");
            await dialog.Component.InvokeAsync(() => radioGroup.Instance.ValueChanged.InvokeAsync(Limits.GlobalLimit));

            dialog.Component.FindComponents<FieldSwitch>().Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());
            FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction").Instance.Value.Should().Be(ShareLimitAction.Default);
        }

        [Fact]
        public async Task GIVEN_CustomValues_WHEN_Saved_THEN_ShouldEmitConfiguredShareRatio()
        {
            var dialog = await RenderDialogAsync(value: CreateShareRatioMax(1f, 5, 3, ShareLimitAction.Default));

            var radioGroup = FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType");
            await dialog.Component.InvokeAsync(() => radioGroup.Instance.ValueChanged.InvokeAsync(0));

            var ratioSwitch = FindComponentByTestId<FieldSwitch>(dialog.Component, "RatioEnabled");
            var totalSwitch = FindComponentByTestId<FieldSwitch>(dialog.Component, "TotalMinutesEnabled");
            var inactiveSwitch = FindComponentByTestId<FieldSwitch>(dialog.Component, "InactiveMinutesEnabled");

            await dialog.Component.InvokeAsync(() => ratioSwitch.Instance.ValueChanged.InvokeAsync(true));
            await dialog.Component.InvokeAsync(() => totalSwitch.Instance.ValueChanged.InvokeAsync(true));
            await dialog.Component.InvokeAsync(() => inactiveSwitch.Instance.ValueChanged.InvokeAsync(true));

            var ratioField = FindComponentByTestId<MudNumericField<float>>(dialog.Component, "Ratio");
            await dialog.Component.InvokeAsync(() => ratioField.Instance.ValueChanged.InvokeAsync(4.2f));

            var totalField = FindComponentByTestId<MudNumericField<int>>(dialog.Component, "TotalMinutes");
            await dialog.Component.InvokeAsync(() => totalField.Instance.ValueChanged.InvokeAsync(180));

            var inactiveField = FindComponentByTestId<MudNumericField<int>>(dialog.Component, "InactiveMinutes");
            await dialog.Component.InvokeAsync(() => inactiveField.Instance.ValueChanged.InvokeAsync(60));

            var actionSelect = FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction");
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
            var component = provider.FindComponent<ShareRatioDialog>();

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

        private static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<ShareRatioDialog> component, string testId)
            where TComponent : IComponent
        {
            return component.FindComponents<TComponent>()
                .Single(c => c.FindAll($"[data-test-id='{testId}']").Count > 0);
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
