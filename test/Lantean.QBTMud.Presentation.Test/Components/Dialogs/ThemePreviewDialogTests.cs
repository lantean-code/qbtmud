using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components.Dialogs
{
    public sealed class ThemePreviewDialogTests : RazorComponentTestBase<ThemePreviewDialog>
    {
        private readonly Mock<IKeyboardService> _keyboardServiceMock;
        private readonly ThemePreviewDialogTestDriver _target;

        public ThemePreviewDialogTests()
        {
            _keyboardServiceMock = new Mock<IKeyboardService>();
            _keyboardServiceMock.Setup(service => service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>())).Returns(Task.CompletedTask);
            _keyboardServiceMock.Setup(service => service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>())).Returns(Task.CompletedTask);
            _keyboardServiceMock.Setup(service => service.Focus()).Returns(Task.CompletedTask);
            _keyboardServiceMock.Setup(service => service.UnFocus()).Returns(Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardServiceMock.Object);

            _target = new ThemePreviewDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_Parameters_WHEN_Rendered_THEN_UsesPreviewScope()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.Catalogue, false, currentThemeId: "Other");

            var provider = dialog.Component.FindComponent<MudThemeProvider>();

            provider.Instance.Theme.Should().NotBeNull();
            provider.Instance.Theme.PseudoCss.Scope.Should().Be(":root .theme-preview-scope");
        }

        [Fact]
        public async Task GIVEN_DarkModeEnabled_WHEN_Toggled_THEN_IconUpdates()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.Catalogue, true, currentThemeId: "Other");

            var toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.LightMode);

            await dialog.Component.InvokeAsync(() => toggle.Instance.OnClick.InvokeAsync());

            toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.DarkMode);
        }

        [Fact]
        public async Task GIVEN_CatalogueParameters_WHEN_PreviousAndNextUsed_THEN_NavigatesWithinBounds()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.Catalogue, false, currentThemeId: "Other");

            var name = FindComponentByTestId<MudText>(dialog.Component, "ThemePreviewName");
            GetChildContentText(name.Instance.ChildContent).Should().Be("First");

            var previous = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewPrevious");
            var next = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewNext");
            previous.Instance.Disabled.Should().BeTrue();
            next.Instance.Disabled.Should().BeFalse();

            await dialog.Component.InvokeAsync(() => next.Instance.OnClick.InvokeAsync());

            name = FindComponentByTestId<MudText>(dialog.Component, "ThemePreviewName");
            GetChildContentText(name.Instance.ChildContent).Should().Be("Second");
            next = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewNext");
            next.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_CatalogueParameters_WHEN_ApplyClicked_THEN_ReturnsSelectedThemeIdAndCloses()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.Catalogue, false, currentThemeId: "Other");
            var apply = FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewApply");

            await dialog.Component.InvokeAsync(() => apply.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be("First");
        }

        [Fact]
        public async Task GIVEN_DetailsParameters_WHEN_Rendered_THEN_HidesNavigationAndShowsSaveApply()
        {
            var dialog = await _target.RenderDialogAsync(CreateDetailsItems(), "ThemeId", ThemePreviewDialogMode.Details, false, canSaveAndApply: true);

            dialog.Component.FindComponents<MudIconButton>()
                .Any(component => HasTestId(component, "ThemePreviewPrevious"))
                .Should()
                .BeFalse();

            FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewSaveApply").Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_WizardSelectionParameters_WHEN_Rendered_THEN_ShowsUseThemeInsteadOfApply()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.WizardSelection, false, currentSelectionThemeId: "First");

            dialog.Component.FindComponents<MudButton>()
                .Any(component => HasTestId(component, "ThemePreviewApply"))
                .Should()
                .BeFalse();

            FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewUseTheme").Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_WizardSelectionParameters_WHEN_UseThemeClicked_THEN_ReturnsSelectedThemeIdAndCloses()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.WizardSelection, false, currentSelectionThemeId: "First");

            var next = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewNext");
            await dialog.Component.InvokeAsync(() => next.Instance.OnClick.InvokeAsync());

            var useTheme = FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewUseTheme");
            useTheme.Instance.Disabled.Should().BeFalse();
            await dialog.Component.InvokeAsync(() => useTheme.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be("Second");
        }

        [Fact]
        public async Task GIVEN_WizardSelectionParameters_WHEN_CurrentThemeUsed_THEN_ReturnsCurrentSelectedThemeIdAndCloses()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.WizardSelection, false, currentSelectionThemeId: "First");

            var useTheme = FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewUseTheme");
            useTheme.Instance.Disabled.Should().BeFalse();
            await dialog.Component.InvokeAsync(() => useTheme.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be("First");
        }

        [Fact]
        public async Task GIVEN_DetailsParameters_WHEN_SaveApplyClicked_THEN_ReturnsSelectedThemeIdAndCloses()
        {
            var dialog = await _target.RenderDialogAsync(CreateDetailsItems(), "ThemeId", ThemePreviewDialogMode.Details, false, canSaveAndApply: true);
            var saveAndApply = FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewSaveApply");

            await dialog.Component.InvokeAsync(() => saveAndApply.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be("ThemeId");
        }

        [Fact]
        public async Task GIVEN_WizardSelectionParameters_WHEN_ToggledAndNavigated_THEN_UpdatesPreviewState()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueItems(), "First", ThemePreviewDialogMode.WizardSelection, true, currentSelectionThemeId: "First");

            var toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.LightMode);

            await dialog.Component.InvokeAsync(() => toggle.Instance.OnClick.InvokeAsync());

            toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.DarkMode);

            var next = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewNext");
            await dialog.Component.InvokeAsync(() => next.Instance.OnClick.InvokeAsync());

            var name = FindComponentByTestId<MudText>(dialog.Component, "ThemePreviewName");
            GetChildContentText(name.Instance.ChildContent).Should().Be("Second");
        }

        private static IReadOnlyList<ThemePreviewDialogItem> CreateCatalogueItems()
        {
            return
            [
                new ThemePreviewDialogItem("First", "First", "Bundled", new MudTheme()),
                new ThemePreviewDialogItem("Second", "Second", "Repository", new MudTheme())
            ];
        }

        private static IReadOnlyList<ThemePreviewDialogItem> CreateDetailsItems()
        {
            return [new ThemePreviewDialogItem("ThemeId", "Name", "Local Storage", new MudTheme())];
        }
    }

    internal sealed class ThemePreviewDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ThemePreviewDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ThemePreviewDialogRenderContext> RenderDialogAsync(
            IReadOnlyList<ThemePreviewDialogItem> items,
            string selectedThemeId,
            ThemePreviewDialogMode mode,
            bool isDarkMode,
            string? currentThemeId = null,
            string? currentSelectionThemeId = null,
            bool canSaveAndApply = false)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ThemePreviewDialog.Items), items },
                { nameof(ThemePreviewDialog.SelectedThemeId), selectedThemeId },
                { nameof(ThemePreviewDialog.Mode), mode },
                { nameof(ThemePreviewDialog.IsDarkMode), isDarkMode },
                { nameof(ThemePreviewDialog.CurrentThemeId), currentThemeId },
                { nameof(ThemePreviewDialog.CurrentSelectionThemeId), currentSelectionThemeId },
                { nameof(ThemePreviewDialog.CanSaveAndApply), canSaveAndApply }
            };

            var reference = await dialogService.ShowAsync<ThemePreviewDialog>("Theme Preview", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<ThemePreviewDialog>();

            return new ThemePreviewDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class ThemePreviewDialogRenderContext
    {
        public ThemePreviewDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<ThemePreviewDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<ThemePreviewDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
