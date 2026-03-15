using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Test.Components.AppSettingsTabs
{
    public sealed class VisualAppSettingsTabTests : RazorComponentTestBase<VisualAppSettingsTab>
    {
        private readonly AppSettingsModel _settings;
        private int _settingsChangedCount;

        public VisualAppSettingsTabTests()
        {
            _settings = AppSettingsModel.Default.Clone();
        }

        [Fact]
        public async Task GIVEN_ThemeModeChanged_WHEN_DarkSelected_THEN_UpdatesSettingsAndRaisesCallback()
        {
            var target = RenderTarget();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.Dark));

            _settings.ThemeModePreference.Should().Be(ThemeModePreference.Dark);
            _settingsChangedCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_ThemeModeChanged_WHEN_LightSelected_THEN_UpdatesSettingsAndRaisesCallback()
        {
            var target = RenderTarget();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.Light));

            _settings.ThemeModePreference.Should().Be(ThemeModePreference.Light);
            _settingsChangedCount.Should().Be(1);
        }

        [Fact]
        public void GIVEN_RenderedControl_WHEN_ThemeModeSelectLoaded_THEN_UsesHelperText()
        {
            var target = RenderTarget();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");

            themeModeSelect.Instance.HelperText.Should().Be("Choose whether qbtmud follows the system appearance or uses a fixed mode.");
        }

        [Fact]
        public async Task GIVEN_ThemeRepositoryUrlChanged_WHEN_ValidHttpsValue_THEN_UpdatesSettingsAndRaisesCallback()
        {
            var target = RenderTarget();
            var repositoryUrlField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsThemeRepositoryIndexUrl");

            await target.InvokeAsync(() => repositoryUrlField.Instance.ValueChanged.InvokeAsync("https://example.com/index.json"));

            _settings.ThemeRepositoryIndexUrl.Should().Be("https://example.com/index.json");
            _settingsChangedCount.Should().Be(1);
            repositoryUrlField.Instance.GetState(x => x.Error).Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ThemeRepositoryUrlChanged_WHEN_InvalidValue_THEN_ShowsValidationError()
        {
            var target = RenderTarget();
            var repositoryUrlField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsThemeRepositoryIndexUrl");

            await target.InvokeAsync(() => repositoryUrlField.Instance.ValueChanged.InvokeAsync("ftp://example.com/index.json"));

            repositoryUrlField.Instance.GetState(x => x.Error).Should().BeTrue();
        }

        private IRenderedComponent<VisualAppSettingsTab> RenderTarget()
        {
            return TestContext.Render<VisualAppSettingsTab>(parameters =>
            {
                parameters.Add(component => component.Settings, _settings);
                parameters.Add(component => component.SettingsChanged, EventCallback.Factory.Create(this, OnSettingsChanged));
            });
        }

        private void OnSettingsChanged()
        {
            _settingsChangedCount++;
        }
    }
}
