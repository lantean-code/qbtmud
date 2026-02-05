using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using System.Linq;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class ThemePreviewDialogTests : RazorComponentTestBase<ThemePreviewDialog>
    {
        private readonly ThemePreviewDialogTestDriver _target;

        public ThemePreviewDialogTests()
        {
            _target = new ThemePreviewDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NullTheme_WHEN_Rendered_THEN_UsesPreviewScope()
        {
            var dialog = await _target.RenderDialogAsync(null, false);

            var provider = dialog.Component.FindComponent<MudThemeProvider>();

            provider.Instance.Theme.Should().NotBeNull();
            provider.Instance.Theme.PseudoCss.Scope.Should().Be(":root .theme-preview-scope");
        }

        [Fact]
        public async Task GIVEN_DarkModeEnabled_WHEN_Toggled_THEN_IconUpdates()
        {
            var dialog = await _target.RenderDialogAsync(new MudTheme(), true);

            var toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.LightMode);

            await dialog.Component.InvokeAsync(() => toggle.Instance.OnClick.InvokeAsync());

            toggle = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewToggleMode");
            toggle.Instance.Icon.Should().Be(Icons.Material.Filled.DarkMode);
        }

        [Fact]
        public async Task GIVEN_CloseClicked_WHEN_Invoked_THEN_DialogCloses()
        {
            var dialog = await _target.RenderDialogAsync(new MudTheme(), false);

            var close = FindComponentByTestId<MudIconButton>(dialog.Component, "ThemePreviewClose");
            await close.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result.Should().NotBeNull();
            result!.Canceled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NavLinkClicked_WHEN_Invoked_THEN_Completes()
        {
            var dialog = await _target.RenderDialogAsync(new MudTheme(), false);

            var navLink = dialog.Component.FindComponents<CustomNavLink>().First();
            navLink.Instance.OnClick.HasDelegate.Should().BeTrue();

            await dialog.Component.InvokeAsync(() => navLink.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
        }
    }

    internal sealed class ThemePreviewDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ThemePreviewDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ThemePreviewDialogRenderContext> RenderDialogAsync(MudTheme? theme, bool isDarkMode)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ThemePreviewDialog.Theme), theme },
                { nameof(ThemePreviewDialog.IsDarkMode), isDarkMode }
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
