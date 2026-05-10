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
        public async Task GIVEN_Request_WHEN_Rendered_THEN_UsesPreviewScope()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueRequest());

            var provider = dialog.Component.FindComponent<MudThemeProvider>();

            provider.Instance.Theme.Should().NotBeNull();
            provider.Instance.Theme.PseudoCss.Scope.Should().Be(":root .theme-preview-scope");
        }

        [Fact]
        public async Task GIVEN_DarkModeEnabled_WHEN_Toggled_THEN_IconUpdates()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueRequest(isDarkMode: true));

            var toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.LightMode);

            await dialog.Component.InvokeAsync(() => toggle.Instance.OnClick.InvokeAsync());

            toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.DarkMode);
        }

        [Fact]
        public async Task GIVEN_CatalogueRequest_WHEN_PreviousAndNextUsed_THEN_NavigatesWithinBounds()
        {
            var dialog = await _target.RenderDialogAsync(CreateCatalogueRequest());

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
        public async Task GIVEN_CatalogueRequest_WHEN_ApplyClicked_THEN_InvokesCallbackAndCloses()
        {
            var appliedThemeId = string.Empty;
            var request = CreateCatalogueRequest();
            request.ApplyThemeAsync = themeId =>
            {
                appliedThemeId = themeId;
                return Task.FromResult(true);
            };

            var dialog = await _target.RenderDialogAsync(request);
            var apply = FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewApply");

            await dialog.Component.InvokeAsync(() => apply.Instance.OnClick.InvokeAsync());

            appliedThemeId.Should().Be("First");
            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DetailsRequest_WHEN_Rendered_THEN_HidesNavigationAndShowsSaveApply()
        {
            var dialog = await _target.RenderDialogAsync(CreateDetailsRequest());

            dialog.Component.FindComponents<MudIconButton>()
                .Any(component => HasTestId(component, "ThemePreviewPrevious"))
                .Should()
                .BeFalse();

            FindComponentByTestId<MudButton>(dialog.Component, "ThemePreviewSaveApply").Instance.Disabled.Should().BeFalse();
        }

        private static ThemePreviewDialogRequest CreateCatalogueRequest(bool isDarkMode = false)
        {
            var request = new ThemePreviewDialogRequest(
                [
                    new ThemePreviewDialogItem("First", "First", "Bundled", new MudTheme()),
                    new ThemePreviewDialogItem("Second", "Second", "Repository", new MudTheme())
                ],
                "First",
                ThemePreviewDialogMode.Catalogue,
                isDarkMode)
            {
                CurrentThemeId = "Other"
            };

            request.ApplyThemeAsync = _ => Task.FromResult(true);
            return request;
        }

        private static ThemePreviewDialogRequest CreateDetailsRequest()
        {
            return new ThemePreviewDialogRequest(
                [new ThemePreviewDialogItem("ThemeId", "Name", "Local Storage", new MudTheme())],
                "ThemeId",
                ThemePreviewDialogMode.Details,
                false)
            {
                CanSaveAndApply = true,
                SaveAndApplyThemeAsync = () => Task.FromResult(true)
            };
        }
    }

    internal sealed class ThemePreviewDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ThemePreviewDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ThemePreviewDialogRenderContext> RenderDialogAsync(ThemePreviewDialogRequest request)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ThemePreviewDialog.Request), request }
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
