using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Test.Components.AppSettings
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
        public async Task GIVEN_ThemeModeChanged_WHEN_SelectedValueMatchesCurrent_THEN_DoesNotRaiseCallback()
        {
            var target = RenderTarget();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.System));

            _settings.ThemeModePreference.Should().Be(ThemeModePreference.System);
            _settingsChangedCount.Should().Be(0);
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
        public async Task GIVEN_ThemeRepositoryUrlChanged_WHEN_ValueMatchesCurrent_THEN_DoesNotRaiseCallback()
        {
            _settings.ThemeRepositoryIndexUrl = "https://example.com/index.json";

            var target = RenderTarget();
            var repositoryUrlField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsThemeRepositoryIndexUrl");

            await target.InvokeAsync(() => repositoryUrlField.Instance.ValueChanged.InvokeAsync("https://example.com/index.json"));

            _settings.ThemeRepositoryIndexUrl.Should().Be("https://example.com/index.json");
            _settingsChangedCount.Should().Be(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GIVEN_ThemeRepositoryUrlChanged_WHEN_BlankValue_THEN_RemainsValid(string value)
        {
            var target = RenderTarget();
            var repositoryUrlField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsThemeRepositoryIndexUrl");

            await repositoryUrlField.Find("input").InputAsync(value);
            var rerendered = RenderTarget();

            _settings.ThemeRepositoryIndexUrl.Should().Be(value);
            _settingsChangedCount.Should().Be(1);
            FindComponentByTestId<MudTextField<string>>(rerendered, "AppSettingsThemeRepositoryIndexUrl").Find("input").GetAttribute("aria-invalid").Should().Be("false");
        }

        [Fact]
        public async Task GIVEN_ThemeRepositoryUrlChanged_WHEN_InvalidValue_THEN_ShowsValidationError()
        {
            var target = RenderTarget();
            var repositoryUrlField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsThemeRepositoryIndexUrl");

            await repositoryUrlField.Find("input").InputAsync("ftp://example.com/index.json");
            var rerendered = RenderTarget();

            _settings.ThemeRepositoryIndexUrl.Should().Be("ftp://example.com/index.json");
            _settingsChangedCount.Should().Be(1);
            FindComponentByTestId<MudTextField<string>>(rerendered, "AppSettingsThemeRepositoryIndexUrl").Find("input").GetAttribute("aria-invalid").Should().Be("true");
        }

        [Fact]
        public async Task GIVEN_ThemeRepositoryUrlChanged_WHEN_NonUrlValue_THEN_ShowsValidationError()
        {
            var target = RenderTarget();
            var repositoryUrlField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsThemeRepositoryIndexUrl");

            await repositoryUrlField.Find("input").InputAsync("invalid");
            var rerendered = RenderTarget();

            _settings.ThemeRepositoryIndexUrl.Should().Be("invalid");
            _settingsChangedCount.Should().Be(1);
            FindComponentByTestId<MudTextField<string>>(rerendered, "AppSettingsThemeRepositoryIndexUrl").Find("input").GetAttribute("aria-invalid").Should().Be("true");
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
