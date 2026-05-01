using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components.Dialogs
{
    public sealed class ShareRatioDialogTests : RazorComponentTestBase<ShareRatioDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly ShareRatioDialogTestDriver _target;

        public ShareRatioDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new ShareRatioDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_GlobalPreset_WHEN_Rendered_THEN_ShouldShowGlobalOptions()
        {
            var baseline = CreateShareRatioMax(Limits.UseGlobalShareRatioLimit, Limits.UseGlobalSeedingTimeLimit, Limits.UseGlobalInactiveSeedingTimeLimit, ShareLimitAction.Remove);

            var dialog = await _target.RenderDialogAsync(value: baseline);

            var radioGroup = FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType");
            radioGroup.Instance.Value.Should().Be(Limits.UseGlobalSeedingTimeLimit);

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            switches.Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());

            var actionSelect = FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction");
            actionSelect.Instance.GetState(x => x.Value).Should().Be(ShareLimitAction.Remove);
        }

        [Fact]
        public async Task GIVEN_NoLimitPreset_WHEN_Rendered_THEN_ShouldShowNoLimitOptions()
        {
            var baseline = CreateShareRatioMax(Limits.NoShareRatioLimit, Limits.NoSeedingTimeLimit, Limits.NoInactiveSeedingTimeLimit, ShareLimitAction.Stop, maxValuesNoLimit: true);

            var dialog = await _target.RenderDialogAsync(value: baseline);

            FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType").Instance.Value.Should().Be(Limits.NoSeedingTimeLimit);

            var switches = dialog.Component.FindComponents<FieldSwitch>();
            switches.Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());

            FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction").Instance.GetState(x => x.Value).Should().Be(ShareLimitAction.Stop);
        }

        [Fact]
        public async Task GIVEN_CustomPreset_WHEN_Rendered_THEN_ShouldPopulateCustomFields()
        {
            var baseline = CreateShareRatioMax(3.5f, 120, 45, ShareLimitAction.Remove);

            var dialog = await _target.RenderDialogAsync(current: baseline);

            FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType").Instance.Value.Should().Be(0);

            FindComponentByTestId<FieldSwitch>(dialog.Component, "RatioEnabled").Instance.Value.Should().BeTrue();
            FindComponentByTestId<FieldSwitch>(dialog.Component, "TotalMinutesEnabled").Instance.Value.Should().BeTrue();
            FindComponentByTestId<FieldSwitch>(dialog.Component, "InactiveMinutesEnabled").Instance.Value.Should().BeTrue();

            FindComponentByTestId<MudNumericField<float>>(dialog.Component, "Ratio").Instance.GetState(x => x.Value).Should().Be(3.5f);
            FindComponentByTestId<MudNumericField<int>>(dialog.Component, "TotalMinutes").Instance.GetState(x => x.Value).Should().Be(120);
            FindComponentByTestId<MudNumericField<int>>(dialog.Component, "InactiveMinutes").Instance.GetState(x => x.Value).Should().Be(45);
            FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction").Instance.GetState(x => x.Value).Should().Be(ShareLimitAction.Remove);
        }

        [Fact]
        public async Task GIVEN_CustomPresetWithNegativeLimits_WHEN_Rendered_THEN_ShouldUseZeroDefaults()
        {
            var baseline = CreateShareRatioMax(-1, -1, -1, ShareLimitAction.Stop);

            var dialog = await _target.RenderDialogAsync(value: baseline, label: "Label");

            dialog.Component.Instance.Label.Should().Be("Label");

            FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType").Instance.Value.Should().Be(0);

            FindComponentByTestId<FieldSwitch>(dialog.Component, "RatioEnabled").Instance.Value.Should().BeFalse();
            FindComponentByTestId<FieldSwitch>(dialog.Component, "TotalMinutesEnabled").Instance.Value.Should().BeFalse();
            FindComponentByTestId<FieldSwitch>(dialog.Component, "InactiveMinutesEnabled").Instance.Value.Should().BeFalse();

            FindComponentByTestId<MudNumericField<float>>(dialog.Component, "Ratio").Instance.GetState(x => x.Value).Should().Be(0);
            FindComponentByTestId<MudNumericField<int>>(dialog.Component, "TotalMinutes").Instance.GetState(x => x.Value).Should().Be(0);
            FindComponentByTestId<MudNumericField<int>>(dialog.Component, "InactiveMinutes").Instance.GetState(x => x.Value).Should().Be(0);
            FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction").Instance.GetState(x => x.Value).Should().Be(ShareLimitAction.Stop);
        }

        [Fact]
        public async Task GIVEN_CustomPresetWithDisabledFields_WHEN_Saved_THEN_ShouldEmitNoLimitValues()
        {
            var baseline = CreateShareRatioMax(-1, -1, -1, ShareLimitAction.Stop);

            var dialog = await _target.RenderDialogAsync(value: baseline);

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ShareRatioSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var shareLimit = (ShareLimit)result.Data!;

            shareLimit.RatioLimit.Should().Be(Limits.NoShareRatioLimit);
            shareLimit.SeedingTimeLimit.Should().Be(Limits.NoSeedingTimeLimit);
            shareLimit.InactiveSeedingTimeLimit.Should().Be(Limits.NoInactiveSeedingTimeLimit);
            shareLimit.ShareLimitAction.Should().Be(ShareLimitAction.Stop);
        }

        [Fact]
        public async Task GIVEN_ActionSelect_WHEN_MenuOpened_THEN_AllActionsAreAvailable()
        {
            TestContext.Render<MudPopoverProvider>();

            var dialog = await _target.RenderDialogAsync(value: CreateShareRatioMax(1f, 5, 3, ShareLimitAction.Default));
            var actionSelect = FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction");

            await dialog.Component.InvokeAsync(() => actionSelect.Instance.OpenMenu());

            dialog.Provider.WaitForAssertion(() =>
            {
                var values = dialog.Provider.FindComponents<MudSelectItem<ShareLimitAction>>()
                    .Select(item => item.Instance.Value)
                    .ToList();

                values.Should().Contain(ShareLimitAction.RemoveWithContent);
                values.Should().Contain(ShareLimitAction.EnableSuperSeeding);
            });
        }

        [Fact]
        public async Task GIVEN_CustomPreset_WHEN_SelectGlobalPreset_THEN_ShouldResetCustomControls()
        {
            var baseline = CreateShareRatioMax(3.5f, 120, 45, ShareLimitAction.Remove);
            var dialog = await _target.RenderDialogAsync(current: baseline);

            var radioGroup = FindComponentByTestId<MudRadioGroup<int>>(dialog.Component, "ShareRatioType");
            await dialog.Component.InvokeAsync(() => radioGroup.Instance.ValueChanged.InvokeAsync(Limits.UseGlobalSeedingTimeLimit));

            dialog.Component.FindComponents<FieldSwitch>().Should().AllSatisfy(s => s.Instance.Value.Should().BeFalse());
            FindComponentByTestId<MudSelect<ShareLimitAction>>(dialog.Component, "SelectedShareLimitAction").Instance.GetState(x => x.Value).Should().Be(ShareLimitAction.Default);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "ShareRatioCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_GlobalPreset_WHEN_Saved_THEN_ShouldEmitGlobalShareRatio()
        {
            var baseline = CreateShareRatioMax(Limits.UseGlobalShareRatioLimit, Limits.UseGlobalSeedingTimeLimit, Limits.UseGlobalInactiveSeedingTimeLimit, ShareLimitAction.Remove);

            var dialog = await _target.RenderDialogAsync(value: baseline);

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ShareRatioSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var shareLimit = (ShareLimit)result.Data!;

            shareLimit.RatioLimit.Should().Be(Limits.UseGlobalShareRatioLimit);
            shareLimit.SeedingTimeLimit.Should().Be(Limits.UseGlobalSeedingTimeLimit);
            shareLimit.InactiveSeedingTimeLimit.Should().Be(Limits.UseGlobalInactiveSeedingTimeLimit);
            shareLimit.ShareLimitAction.Should().Be(ShareLimitAction.Default);
        }

        [Fact]
        public async Task GIVEN_NoLimitPreset_WHEN_Saved_THEN_ShouldEmitNoLimitShareRatio()
        {
            var baseline = CreateShareRatioMax(Limits.NoShareRatioLimit, 0, 0, ShareLimitAction.Stop, maxValuesNoLimit: true);

            var dialog = await _target.RenderDialogAsync(value: baseline);

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ShareRatioSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var shareLimit = (ShareLimit)result.Data!;

            shareLimit.RatioLimit.Should().Be(Limits.NoShareRatioLimit);
            shareLimit.SeedingTimeLimit.Should().Be(Limits.NoSeedingTimeLimit);
            shareLimit.InactiveSeedingTimeLimit.Should().Be(Limits.NoInactiveSeedingTimeLimit);
            shareLimit.ShareLimitAction.Should().Be(ShareLimitAction.Default);
        }

        [Fact]
        public async Task GIVEN_CustomValues_WHEN_Saved_THEN_ShouldEmitConfiguredShareRatio()
        {
            var dialog = await _target.RenderDialogAsync(value: CreateShareRatioMax(1f, 5, 3, ShareLimitAction.Default));

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

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ShareRatioSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
            result.Data.Should().BeAssignableTo<ShareLimit>();
            var shareLimit = (ShareLimit)result.Data!;

            shareLimit.RatioLimit.Should().Be(4.2f);
            shareLimit.SeedingTimeLimit.Should().Be(180);
            shareLimit.InactiveSeedingTimeLimit.Should().Be(60);
            shareLimit.ShareLimitAction.Should().Be(ShareLimitAction.Remove);
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultOk()
        {
            Func<KeyboardEvent, Task>? submitHandler = null;
            var keyboardMock = Mock.Get(_keyboardService);
            keyboardMock
                .Setup(service => service.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Enter" && !e.CtrlKey), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    submitHandler = handler;
                })
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(value: CreateShareRatioMax(1f, 5, 3, ShareLimitAction.Default));

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var shareLimit = (ShareLimit)result.Data!;

            shareLimit.RatioLimit.Should().Be(1f);
            shareLimit.SeedingTimeLimit.Should().Be(5);
            shareLimit.InactiveSeedingTimeLimit.Should().Be(3);
            shareLimit.ShareLimitAction.Should().Be(ShareLimitAction.Default);
        }

        private static ShareLimitMax CreateShareRatioMax(float ratio, int seedingTimeLimit, int inactiveSeedingTimeLimit, ShareLimitAction action, bool maxValuesNoLimit = false)
        {
            return new ShareLimitMax
            {
                RatioLimit = ratio,
                SeedingTimeLimit = seedingTimeLimit,
                InactiveSeedingTimeLimit = inactiveSeedingTimeLimit,
                ShareLimitAction = action,
                MaxRatio = maxValuesNoLimit ? Limits.NoShareRatioLimit : ratio + 1,
                MaxSeedingTime = maxValuesNoLimit ? Limits.NoSeedingTimeLimit : seedingTimeLimit + 1,
                MaxInactiveSeedingTime = maxValuesNoLimit ? Limits.NoInactiveSeedingTimeLimit : inactiveSeedingTimeLimit + 1,
            };
        }
    }

    internal sealed class ShareRatioDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ShareRatioDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ShareRatioDialogRenderContext> RenderDialogAsync(ShareLimitMax? value = null, ShareLimitMax? current = null, bool disabled = false, string? label = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ShareRatioDialog.Value), value },
                { nameof(ShareRatioDialog.CurrentValue), current },
                { nameof(ShareRatioDialog.Disabled), disabled },
            };

            if (label is not null)
            {
                parameters.Add(nameof(ShareRatioDialog.Label), label);
            }

            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
            };

            var reference = await dialogService.ShowAsync<ShareRatioDialog>("Torrent Upload/Download Ratio Limiting", parameters, options);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<ShareRatioDialog>();

            return new ShareRatioDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class ShareRatioDialogRenderContext
    {
        public ShareRatioDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<ShareRatioDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<ShareRatioDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
