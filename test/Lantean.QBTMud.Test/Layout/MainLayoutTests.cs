using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Layout;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Layout
{
    public sealed class MainLayoutTests : RazorComponentTestBase<MainLayout>
    {
        private readonly IThemeManagerService _themeManagerService;
        private readonly IThemeFontCatalog _themeFontCatalog;
        private readonly TestLocalStorageService _localStorage;

        public MainLayoutTests()
        {
            _themeManagerService = Mock.Of<IThemeManagerService>();
            _themeFontCatalog = Mock.Of<IThemeFontCatalog>();
            _localStorage = new TestLocalStorageService();

            TestContext.Services.RemoveAll<IThemeManagerService>();
            TestContext.Services.RemoveAll<IThemeFontCatalog>();
            TestContext.Services.RemoveAll<ILocalStorageService>();

            TestContext.Services.AddSingleton(_themeManagerService);
            TestContext.Services.AddSingleton(_themeFontCatalog);
            TestContext.Services.AddSingleton<ILocalStorageService>(_localStorage);

            Mock.Get(_themeManagerService)
                .Setup(service => service.EnsureInitialized())
                .Returns(Task.CompletedTask);

            TestContext.JSInterop.SetupVoid("qbt.removeBootstrapTheme", _ => true).SetVoidResult();
        }

        [Fact]
        public async Task GIVEN_ThemeChangedWithValidFont_WHEN_Rendered_THEN_LoadsFont()
        {
            var url = "Url";
            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl("FontFamily", out url))
                .Returns(true);
            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.BuildFontId("FontFamily"))
                .Returns("FontId");

            TestContext.JSInterop.SetupVoid("qbt.loadGoogleFont", _ => true).SetVoidResult();

            var target = RenderLayout();

            await target.InvokeAsync(() => RaiseThemeChanged(new MudTheme(), "FontFamily", "ThemeId"));

            target.WaitForAssertion(() =>
            {
                TestContext.JSInterop.Invocations.Should().Contain(inv => inv.Identifier == "qbt.loadGoogleFont");
            });
        }

        [Fact]
        public async Task GIVEN_ThemeChangedWithEmptyFont_WHEN_Rendered_THEN_DoesNotLoad()
        {
            var loadGoogleFontInvocation = TestContext.JSInterop.SetupVoid("qbt.loadGoogleFont", _ => true);
            loadGoogleFontInvocation.SetVoidResult();
            var target = RenderLayout();

            await target.InvokeAsync(() => RaiseThemeChanged(new MudTheme(), string.Empty, "ThemeId"));

            target.WaitForAssertion(() =>
            {
                loadGoogleFontInvocation.Invocations.Should().BeEmpty();
            });
        }

        [Fact]
        public async Task GIVEN_ThemeChangedWithInvalidFont_WHEN_Rendered_THEN_DoesNotLoad()
        {
            var url = string.Empty;
            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl("FontFamily", out url))
                .Returns(false);
            var loadGoogleFontInvocation = TestContext.JSInterop.SetupVoid("qbt.loadGoogleFont", _ => true);
            loadGoogleFontInvocation.SetVoidResult();

            var target = RenderLayout();

            await target.InvokeAsync(() => RaiseThemeChanged(new MudTheme(), "FontFamily", "ThemeId"));

            target.WaitForAssertion(() =>
            {
                loadGoogleFontInvocation.Invocations.Should().BeEmpty();
            });
        }

        [Fact]
        public async Task GIVEN_ThemeChanged_WHEN_Rendered_THEN_PersistsBootstrapThemeCss()
        {
            var target = RenderLayout();

            await target.InvokeAsync(() => RaiseThemeChanged(new MudTheme(), "FontFamily", "ThemeId"));

            target.WaitForAssertion(() =>
            {
                var snapshot = _localStorage.Snapshot();
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Light");
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Dark");
                snapshot.Should().ContainKey("ThemeManager.BootstrapIsDark");
                snapshot["ThemeManager.BootstrapCss.Light"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
                snapshot["ThemeManager.BootstrapCss.Dark"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
            });
        }

        [Fact]
        public async Task GIVEN_StoredSettings_WHEN_Rendered_THEN_UsesStoredValues()
        {
            await _localStorage.SetItemAsync("MainLayout.DrawerOpen", true, Xunit.TestContext.Current.CancellationToken);
            await _localStorage.SetItemAsync("MainLayout.IsDarkMode", false, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();

            target.WaitForAssertion(() => probe.Instance.DrawerOpen.Should().BeTrue());
            target.FindComponent<MudThemeProvider>().Instance.IsDarkMode.Should().BeFalse();

            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DrawerOpenStoredFalse_WHEN_Rendered_THEN_UsesBreakpointDefault()
        {
            await _localStorage.SetItemAsync("MainLayout.DrawerOpen", false, Xunit.TestContext.Current.CancellationToken);
            await _localStorage.SetItemAsync("MainLayout.IsDarkMode", true, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();

            target.WaitForAssertion(() => probe.Instance.DrawerOpen.Should().BeFalse());
            target.FindComponent<MudThemeProvider>().Instance.IsDarkMode.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_MenuClicked_WHEN_ToggleDrawerInvoked_THEN_UpdatesStorage()
        {
            await _localStorage.SetItemAsync("MainLayout.DrawerOpen", true, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();
            target.WaitForAssertion(() => probe.Instance.DrawerOpen.Should().BeTrue());

            var menuButton = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.Menu);

            await target.InvokeAsync(() => menuButton.Instance.OnClick.InvokeAsync());

            probe.Instance.DrawerOpen.Should().BeFalse();
            var stored = await _localStorage.GetItemAsync<bool?>("MainLayout.DrawerOpen", Xunit.TestContext.Current.CancellationToken);
            stored.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_LoginRoute_WHEN_Rendered_THEN_HidesMenuButton()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/login");

            var target = RenderLayout(CreateProbeBody());

            target.FindComponents<MudIconButton>()
                .Any(button => button.Instance.Icon == Icons.Material.Filled.Menu)
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task GIVEN_LoginRouteAndSmallBreakpoint_WHEN_Rendered_THEN_UsesFullLocalizedTitle()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/login");

            var target = RenderLayout(CreateProbeBody());
            var provider = target.FindComponent<BreakpointOrientationProvider>();
            await target.InvokeAsync(() => provider.Instance.OnBreakpointChanged.InvokeAsync(Breakpoint.Sm));

            var title = target.FindComponents<MudText>()
                .Single(text => text.Instance.Typo == Typo.h5);

            GetChildContentText(title.Instance.ChildContent).Should().Be("qBittorrent WebUI");
        }

        [Fact]
        public async Task GIVEN_DrawerOpenChangedCallback_WHEN_ValueUnchanged_THEN_StateUnchanged()
        {
            await _localStorage.SetItemAsync("MainLayout.DrawerOpen", true, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();
            target.WaitForAssertion(() => probe.Instance.DrawerOpen.Should().BeTrue());

            await target.InvokeAsync(() => probe.Instance.DrawerOpenChanged.InvokeAsync(probe.Instance.DrawerOpen));

            probe.Instance.DrawerOpen.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TimerDrawerOpenChangedCallback_WHEN_Toggled_THEN_UpdatesState()
        {
            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();

            await target.InvokeAsync(() => probe.Instance.TimerDrawerOpenChanged.InvokeAsync(true));
            probe.Instance.TimerDrawerOpen.Should().BeTrue();

            await target.InvokeAsync(() => probe.Instance.TimerDrawerOpenChanged.InvokeAsync(true));
            probe.Instance.TimerDrawerOpen.Should().BeTrue();

            await target.InvokeAsync(() => probe.Instance.TimerDrawerOpenChanged.InvokeAsync(false));
            probe.Instance.TimerDrawerOpen.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SystemDarkModeChanged_WHEN_Invoked_THEN_UpdatesProvider()
        {
            var target = RenderLayout(CreateProbeBody());
            var themeProvider = target.FindComponent<MudThemeProvider>();

            await target.InvokeAsync(() => themeProvider.Instance.SystemDarkModeChangedAsync(false));

            themeProvider.Instance.IsDarkMode.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_MenuDarkModeChanged_WHEN_Invoked_THEN_PersistsSetting()
        {
            var target = RenderLayout(CreateProbeBody());
            var menu = target.FindComponent<Menu>();

            await target.InvokeAsync(() => menu.Instance.DarkModeChanged.InvokeAsync(false));

            var stored = await _localStorage.GetItemAsync<bool?>("MainLayout.IsDarkMode", Xunit.TestContext.Current.CancellationToken);
            stored.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_MenuDarkModeChanged_WHEN_Invoked_THEN_PersistsBootstrapThemeCss()
        {
            var target = RenderLayout(CreateProbeBody());
            var menu = target.FindComponent<Menu>();

            await target.InvokeAsync(() => menu.Instance.DarkModeChanged.InvokeAsync(false));

            target.WaitForAssertion(() =>
            {
                var snapshot = _localStorage.Snapshot();
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Light");
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Dark");
                snapshot.Should().ContainKey("ThemeManager.BootstrapIsDark");
                snapshot["ThemeManager.BootstrapCss.Light"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
                snapshot["ThemeManager.BootstrapCss.Dark"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
            });
        }

        [Fact]
        public async Task GIVEN_ErrorRaised_WHEN_ParametersSet_THEN_OpensErrorDrawerAndClosesTimer()
        {
            var target = RenderLayout(CreateProbeAndThrowBody());
            var probe = target.FindComponent<DrawerProbe>();

            await target.InvokeAsync(() => probe.Instance.TimerDrawerOpenChanged.InvokeAsync(true));
            probe.Instance.TimerDrawerOpen.Should().BeTrue();

            await target.InvokeAsync(() => target.Find("#throw-button").Click());
            target.Render();

            var errorDrawer = FindErrorDrawer(target);
            var timerDrawer = FindTimerDrawer(target);

            errorDrawer.Instance.Open.Should().BeTrue();
            timerDrawer.Instance.Open.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ErrorDrawerToggled_WHEN_IconClicked_THEN_TogglesState()
        {
            var target = RenderLayout(CreateProbeAndThrowBody());

            await target.InvokeAsync(() => target.Find("#throw-button").Click());
            target.Render();

            var errorIcon = target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Filled.Error);

            await target.InvokeAsync(() => errorIcon.Instance.OnClick.InvokeAsync());

            var errorDrawer = FindErrorDrawer(target);
            errorDrawer.Instance.Open.Should().BeFalse();

            await target.InvokeAsync(() => errorIcon.Instance.OnClick.InvokeAsync());

            errorDrawer.Instance.Open.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ClearErrorsInvoked_WHEN_Executed_THEN_ResetsErrorState()
        {
            var target = RenderLayout(CreateProbeAndThrowBody());

            await target.InvokeAsync(() => target.Find("#throw-button").Click());
            target.Render();

            var errorBoundary = target.FindComponent<EnhancedErrorBoundary>();
            await target.InvokeAsync(() => errorBoundary.Instance.ClearErrors());
            target.Render();

            var errorDrawer = FindErrorDrawer(target);
            errorDrawer.Instance.Open.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_BreakpointChanged_WHEN_SmallScreen_THEN_DrawerCloses()
        {
            await _localStorage.SetItemAsync("MainLayout.DrawerOpen", true, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();
            target.WaitForAssertion(() => probe.Instance.DrawerOpen.Should().BeTrue());

            var provider = target.FindComponent<BreakpointOrientationProvider>();
            await target.InvokeAsync(() => provider.Instance.OnBreakpointChanged.InvokeAsync(Breakpoint.Sm));

            probe.Instance.DrawerOpen.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DisposeCalled_WHEN_Invoked_THEN_Unsubscribes()
        {
            var target = RenderLayout(CreateProbeBody());

            target.Instance.Dispose();
        }

        private IRenderedComponent<MainLayout> RenderLayout(RenderFragment? body = null)
        {
            var layoutBody = body ?? (_ => { });

            return TestContext.Render<MainLayout>(parameters =>
            {
                parameters.Add<RenderFragment>(p => p.Body!, layoutBody);
            });
        }

        private static RenderFragment CreateProbeBody()
        {
            return builder =>
            {
                builder.OpenComponent<DrawerProbe>(0);
                builder.CloseComponent();
            };
        }

        private static RenderFragment CreateProbeAndThrowBody()
        {
            return builder =>
            {
                builder.OpenComponent<DrawerProbe>(0);
                builder.CloseComponent();
                builder.OpenComponent<ThrowOnClick>(1);
                builder.CloseComponent();
            };
        }

        private static IRenderedComponent<MudDrawer> FindErrorDrawer(IRenderedComponent<MainLayout> target)
        {
            return target.FindComponents<MudDrawer>().Single(drawer => drawer.Instance.Class is null);
        }

        private static IRenderedComponent<MudDrawer> FindTimerDrawer(IRenderedComponent<MainLayout> target)
        {
            return target.FindComponents<MudDrawer>().Single(drawer => drawer.Instance.Class == "app-shell__timer-drawer");
        }

        private void RaiseThemeChanged(MudTheme theme, string fontFamily, string? themeId)
        {
            var args = new ThemeChangedEventArgs(theme, fontFamily, themeId);
            Mock.Get(_themeManagerService).Raise(service => service.ThemeChanged += null!, args);
        }

        private sealed class DrawerProbe : ComponentBase
        {
            [CascadingParameter(Name = "DrawerOpen")]
            public bool DrawerOpen { get; set; }

            [CascadingParameter(Name = "DrawerOpenChanged")]
            public EventCallback<bool> DrawerOpenChanged { get; set; }

            [CascadingParameter(Name = "TimerDrawerOpen")]
            public bool TimerDrawerOpen { get; set; }

            [CascadingParameter(Name = "TimerDrawerOpenChanged")]
            public EventCallback<bool> TimerDrawerOpenChanged { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", "drawer-probe");
                builder.CloseElement();
            }
        }

        private sealed class ThrowOnClick : ComponentBase
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "id", "throw-button");
                builder.AddAttribute(2, "type", "button");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
                builder.AddContent(4, "Throw");
                builder.CloseElement();
            }

            private Task OnClick(MouseEventArgs args)
            {
                throw new InvalidOperationException("Boom");
            }
        }
    }
}
