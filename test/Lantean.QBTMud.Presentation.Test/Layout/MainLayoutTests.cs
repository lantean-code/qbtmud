using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using Lantean.QBTMud.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Layout
{
    public sealed class MainLayoutTests : RazorComponentTestBase<MainLayout>
    {
        private readonly IThemeManagerService _themeManagerService;
        private readonly IThemeFontCatalog _themeFontCatalog;
        private readonly TestLocalStorageService _localStorage;
        private ThemeModePreference _currentThemeModePreference = ThemeModePreference.System;

        public MainLayoutTests()
        {
            _themeManagerService = Mock.Of<IThemeManagerService>();
            _themeFontCatalog = Mock.Of<IThemeFontCatalog>();
            _localStorage = new TestLocalStorageService();

            TestContext.Services.RemoveAll<IThemeManagerService>();
            TestContext.Services.RemoveAll<IThemeFontCatalog>();
            TestContext.Services.RemoveAll<ISettingsStorageService>();
            TestContext.Services.RemoveAll<ILocalStorageService>();

            TestContext.Services.AddSingleton(_themeManagerService);
            TestContext.Services.AddSingleton(_themeFontCatalog);
            TestContext.Services.AddSingleton<ISettingsStorageService>(_localStorage);
            TestContext.Services.AddSingleton<ILocalStorageService>(_localStorage);

            Mock.Get(_themeManagerService)
                .Setup(service => service.EnsureInitialized())
                .Returns(Task.CompletedTask);
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeModePreference)
                .Returns(() => _currentThemeModePreference);

            TestContext.JSInterop.SetupVoid("qbt.removeBootstrapTheme", _ => true).SetVoidResult();
        }

        [Fact]
        public void GIVEN_MainLayoutRendered_WHEN_AfterRenderRuns_THEN_InstallsContextMenuPopoverPatchOnce()
        {
            var installPatchInvocation = TestContext.JSInterop.Setup<bool>("qbt.installContextMenuPopoverPatch", _ => true);
            installPatchInvocation.SetResult(true);

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() => installPatchInvocation.Invocations.Should().HaveCount(1));

            target.Render();

            target.WaitForAssertion(() => installPatchInvocation.Invocations.Should().HaveCount(1));
        }

        [Fact]
        public void GIVEN_MainLayoutRendered_WHEN_PatchInstallReturnsFalse_THEN_RetriesOnSubsequentRender()
        {
            var installPatchInvocation = TestContext.JSInterop.Setup<bool>("qbt.installContextMenuPopoverPatch", _ => true);
            installPatchInvocation.SetResult(false);

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() => installPatchInvocation.Invocations.Count.Should().BeGreaterThan(1));
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
        public async Task GIVEN_CurrentFontFamilySet_WHEN_ThemeChanged_THEN_PersistsBootstrapFontFamily()
        {
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentFontFamily)
                .Returns("FontFamily");

            var target = RenderLayout();

            await target.InvokeAsync(() => RaiseThemeChanged(new MudTheme(), "FontFamily", "ThemeId"));

            target.WaitForAssertion(() =>
            {
                var snapshot = _localStorage.Snapshot();
                snapshot.Should().ContainKey("ThemeManager.BootstrapFontFamily");
                snapshot["ThemeManager.BootstrapFontFamily"].Should().Be("FontFamily");
            });
        }

        [Fact]
        public async Task GIVEN_ThemeChanged_WHEN_Rendered_THEN_PersistsBootstrapThemeOnlyToLocalStorage()
        {
            var localStorage = new TestLocalStorageService();
            var settingsStorage = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            settingsStorage
                .Setup(service => service.GetItemAsync<bool?>("MainLayout.DrawerOpen", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<bool?>(null));

            TestContext.Services.RemoveAll<ISettingsStorageService>();
            TestContext.Services.RemoveAll<ILocalStorageService>();
            TestContext.Services.AddSingleton<ISettingsStorageService>(settingsStorage.Object);
            TestContext.Services.AddSingleton<ILocalStorageService>(localStorage);

            var target = RenderLayout();

            await target.InvokeAsync(() => RaiseThemeChanged(new MudTheme(), "FontFamily", "ThemeId"));

            target.WaitForAssertion(() =>
            {
                var snapshot = localStorage.Snapshot();
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Light");
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Dark");
                snapshot.Should().ContainKey("ThemeManager.BootstrapIsDark");
            });

            settingsStorage.Verify(
                service => service.SetItemAsStringAsync(
                    It.Is<string>(key => key.StartsWith("ThemeManager.Bootstrap", StringComparison.Ordinal)),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            settingsStorage.Verify(
                service => service.SetItemAsync(
                    It.Is<string>(key => key.StartsWith("ThemeManager.Bootstrap", StringComparison.Ordinal)),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_ThemeChangedDuringInitialization_WHEN_Rendered_THEN_DoesNotRequeryCurrentTheme()
        {
            var themeManagerMock = Mock.Get(_themeManagerService);
            themeManagerMock
                .Setup(service => service.EnsureInitialized())
                .Returns(() =>
                {
                    RaiseThemeChanged(new MudTheme(), "FontFamily", "ThemeId");
                    return Task.CompletedTask;
                });
            themeManagerMock
                .SetupGet(service => service.CurrentTheme)
                .Throws(new InvalidOperationException("Current theme should not be queried when already applied."));

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() =>
            {
                target.FindComponent<DrawerProbe>().Should().NotBeNull();
                TestContext.JSInterop.Invocations.Should().Contain(inv => inv.Identifier == "qbt.removeBootstrapTheme");
                _localStorage.WriteCount.Should().Be(0);
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapCss.Light");
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapCss.Dark");
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapIsDark");
            });
        }

        [Fact]
        public void GIVEN_CurrentThemeAvailable_WHEN_Rendered_THEN_DoesNotPersistBootstrapThemeOnInitialize()
        {
            var currentTheme = new ThemeCatalogItem(
                id: "ThemeId",
                name: "ThemeName",
                theme: new ThemeDefinition
                {
                    Theme = new MudTheme(),
                },
                source: ThemeSource.Local,
                sourcePath: null);

            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentTheme)
                .Returns(currentTheme);
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentFontFamily)
                .Returns("FontFamily");

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() =>
            {
                TestContext.JSInterop.Invocations.Should().Contain(inv => inv.Identifier == "qbt.removeBootstrapTheme");
                _localStorage.WriteCount.Should().Be(0);
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapCss.Light");
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapCss.Dark");
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapIsDark");
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapFontFamily");
            });
        }

        [Fact]
        public async Task GIVEN_LightThemePreference_WHEN_Rendered_THEN_UsesStoredValues()
        {
            await _localStorage.SetItemAsync("MainLayout.DrawerOpen", true, Xunit.TestContext.Current.CancellationToken);
            _currentThemeModePreference = ThemeModePreference.Light;

            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();

            target.WaitForAssertion(() => probe.Instance.DrawerOpen.Should().BeTrue());
            target.FindComponent<MudThemeProvider>().Instance.GetState(x => x.IsDarkMode).Should().BeFalse();

            Mock.Get(_themeManagerService).Verify(service => service.EnsureInitialized(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DarkThemePreferenceAndDrawerOpenStoredFalse_WHEN_Rendered_THEN_UsesBreakpointDefault()
        {
            await _localStorage.SetItemAsync("MainLayout.DrawerOpen", false, Xunit.TestContext.Current.CancellationToken);
            _currentThemeModePreference = ThemeModePreference.Dark;

            var target = RenderLayout(CreateProbeBody());
            var probe = target.FindComponent<DrawerProbe>();

            target.WaitForAssertion(() => probe.Instance.DrawerOpen.Should().BeFalse());
            target.FindComponent<MudThemeProvider>().Instance.GetState(x => x.IsDarkMode).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_SystemThemeModePreference_WHEN_Rendered_THEN_UsesSystemDarkModePreference()
        {
            _currentThemeModePreference = ThemeModePreference.System;
            var systemDarkModeInvocation = TestContext.JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", _ => true);
            systemDarkModeInvocation.SetResult(true);

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() =>
            {
                target.FindComponent<MudThemeProvider>().Instance.GetState(x => x.IsDarkMode).Should().BeTrue();
                systemDarkModeInvocation.Invocations.Should().HaveCount(1);
            });
        }

        [Fact]
        public async Task GIVEN_SystemThemeModePreferenceAndStoredBootstrapModeMismatch_WHEN_Rendered_THEN_PersistsBootstrapThemeOnInitialize()
        {
            await _localStorage.SetItemAsync("ThemeManager.BootstrapIsDark", false, Xunit.TestContext.Current.CancellationToken);

            _currentThemeModePreference = ThemeModePreference.System;
            var systemDarkModeInvocation = TestContext.JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", _ => true);
            systemDarkModeInvocation.SetResult(true);
            var writesBeforeRender = _localStorage.WriteCount;

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() =>
            {
                target.FindComponent<MudThemeProvider>().Instance.GetState(x => x.IsDarkMode).Should().BeTrue();

                var snapshot = _localStorage.Snapshot();
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Light");
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Dark");
                snapshot.Should().ContainKey("ThemeManager.BootstrapIsDark");
                snapshot["ThemeManager.BootstrapIsDark"].Should().Be(true);
                _localStorage.WriteCount.Should().BeGreaterThan(writesBeforeRender);
            });
        }

        [Fact]
        public async Task GIVEN_SystemThemeModePreferenceAndStoredBootstrapModeMatch_WHEN_Rendered_THEN_DoesNotPersistBootstrapThemeOnInitialize()
        {
            await _localStorage.SetItemAsync("ThemeManager.BootstrapIsDark", true, Xunit.TestContext.Current.CancellationToken);

            _currentThemeModePreference = ThemeModePreference.System;
            var systemDarkModeInvocation = TestContext.JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", _ => true);
            systemDarkModeInvocation.SetResult(true);
            var writesBeforeRender = _localStorage.WriteCount;

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() =>
            {
                target.FindComponent<MudThemeProvider>().Instance.GetState(x => x.IsDarkMode).Should().BeTrue();
                _localStorage.WriteCount.Should().Be(writesBeforeRender);
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapCss.Light");
                _localStorage.Snapshot().Should().NotContainKey("ThemeManager.BootstrapCss.Dark");
            });
        }

        [Fact]
        public void GIVEN_LightThemeModePreference_WHEN_Rendered_THEN_DoesNotQuerySystemDarkModePreference()
        {
            _currentThemeModePreference = ThemeModePreference.Light;

            var systemDarkModeInvocation = TestContext.JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", _ => true);
            systemDarkModeInvocation.SetResult(true);

            var target = RenderLayout(CreateProbeBody());

            target.WaitForAssertion(() =>
            {
                target.FindComponent<MudThemeProvider>().Instance.GetState(x => x.IsDarkMode).Should().BeFalse();
                systemDarkModeInvocation.Invocations.Should().BeEmpty();
            });
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
        public void GIVEN_DefaultRoute_WHEN_Rendered_THEN_PageTitleUsesLocalizedWebUiTitle()
        {
            var target = RenderLayout(CreateProbeBody());
            var pageTitle = target.FindComponent<PageTitle>();

            GetChildContentText(pageTitle.Instance.ChildContent).Should().Be("qBittorrent WebUI");
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
            _currentThemeModePreference = ThemeModePreference.System;
            var target = RenderLayout(CreateProbeBody());
            var themeProvider = target.FindComponent<MudThemeProvider>();

            await target.InvokeAsync(() => themeProvider.Instance.SystemDarkModeChangedAsync(false));

            themeProvider.Instance.GetState(x => x.IsDarkMode).Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DarkThemePreference_WHEN_SystemDarkModeChanged_THEN_DoesNotPersistSystemValue()
        {
            _currentThemeModePreference = ThemeModePreference.Dark;
            var systemDarkModeInvocation = TestContext.JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", _ => true);
            systemDarkModeInvocation.SetResult(true);

            var target = RenderLayout(CreateProbeBody());
            var themeProvider = target.FindComponent<MudThemeProvider>();

            target.WaitForAssertion(() => themeProvider.Instance.GetState(x => x.IsDarkMode).Should().BeTrue());
            var baselineSnapshot = _localStorage.Snapshot();
            var baselineMode = baselineSnapshot.TryGetValue("ThemeManager.BootstrapIsDark", out var value)
                ? value as bool?
                : null;
            baselineMode.Should().BeNull();

            await target.InvokeAsync(() => themeProvider.Instance.SystemDarkModeChangedAsync(false));

            var updatedSnapshot = _localStorage.Snapshot();
            var updatedMode = updatedSnapshot.TryGetValue("ThemeManager.BootstrapIsDark", out var updatedValue)
                ? updatedValue as bool?
                : null;
            updatedMode.Should().BeNull();
            _localStorage.WriteCount.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_ThemeModePreferenceChangedInThemeManager_WHEN_NotificationRaised_THEN_UpdatesProviderWithoutReload()
        {
            _currentThemeModePreference = ThemeModePreference.Dark;
            var systemDarkModeInvocation = TestContext.JSInterop.Setup<bool>("mudThemeProvider.isDarkMode", _ => true);
            systemDarkModeInvocation.SetResult(true);

            var target = RenderLayout(CreateProbeBody());
            var themeProvider = target.FindComponent<MudThemeProvider>();
            target.WaitForAssertion(() => themeProvider.Instance.GetState(x => x.IsDarkMode).Should().BeTrue());

            await target.InvokeAsync(() => RaiseThemeModePreferenceChanged(ThemeModePreference.Light));

            target.WaitForAssertion(() =>
            {
                themeProvider.Instance.GetState(x => x.IsDarkMode).Should().BeFalse();

                var snapshot = _localStorage.Snapshot();
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Light");
                snapshot.Should().ContainKey("ThemeManager.BootstrapCss.Dark");
                snapshot.Should().ContainKey("ThemeManager.BootstrapIsDark");
                snapshot["ThemeManager.BootstrapIsDark"].Should().Be(false);
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

            errorDrawer.Instance.GetState(x => x.Open).Should().BeTrue();
            timerDrawer.Instance.GetState(x => x.Open).Should().BeFalse();
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
            errorDrawer.Instance.GetState(x => x.Open).Should().BeFalse();

            await target.InvokeAsync(() => errorIcon.Instance.OnClick.InvokeAsync());

            errorDrawer.Instance.GetState(x => x.Open).Should().BeTrue();
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
            errorDrawer.Instance.GetState(x => x.Open).Should().BeFalse();
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
        public async Task GIVEN_ErrorDrawerOpenWithoutErrors_WHEN_BreakpointChanges_THEN_ErrorDrawerCloses()
        {
            var target = RenderLayout(CreateProbeBody());
            var errorDrawer = FindErrorDrawer(target);

            await target.InvokeAsync(() => errorDrawer.Instance.OpenChanged.InvokeAsync(true));
            errorDrawer.Instance.GetState(x => x.Open).Should().BeTrue();

            var provider = target.FindComponent<BreakpointOrientationProvider>();
            await target.InvokeAsync(() => provider.Instance.OnBreakpointChanged.InvokeAsync(Breakpoint.Md));

            errorDrawer.Instance.GetState(x => x.Open).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_LoginRouteWithQueryAndFragment_WHEN_Rendered_THEN_HidesMenuButton()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/login?next=%2Fdetails#hash");

            var target = RenderLayout(CreateProbeBody());

            target.FindComponents<MudIconButton>()
                .Any(button => button.Instance.Icon == Icons.Material.Filled.Menu)
                .Should()
                .BeFalse();
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

        private void RaiseThemeModePreferenceChanged(ThemeModePreference themeModePreference)
        {
            _currentThemeModePreference = themeModePreference;
            var args = new ThemeModePreferenceChangedEventArgs(themeModePreference);
            Mock.Get(_themeManagerService).Raise(service => service.ThemeModePreferenceChanged += null!, args);
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
