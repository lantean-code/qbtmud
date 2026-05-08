using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Microsoft.AspNetCore.Components;
using AppSettingsModel = Lantean.QBTMud.Core.Models.AppSettings;

namespace Lantean.QBTMud.Presentation.Test.Components.AppSettings
{
    public sealed class GeneralAppSettingsTabTests : RazorComponentTestBase<GeneralAppSettingsTab>
    {
        private readonly AppSettingsModel _settings;
        private int _settingsChangedCount;

        public GeneralAppSettingsTabTests()
        {
            _settings = AppSettingsModel.Default.Clone();
        }

        [Fact]
        public void GIVEN_DefaultSettings_WHEN_Rendered_THEN_ShowsDisabledSpeedHistoryToggle()
        {
            var target = RenderTarget();

            FindSwitch(target, "AppSettingsSpeedHistoryEnabled").Instance.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SpeedHistoryChanged_WHEN_Disabled_THEN_UpdatesSettingsAndRaisesCallback()
        {
            _settings.SpeedHistoryEnabled = true;

            var target = RenderTarget();
            var speedHistorySwitch = FindSwitch(target, "AppSettingsSpeedHistoryEnabled");

            await target.InvokeAsync(() => speedHistorySwitch.Instance.ValueChanged.InvokeAsync(false));

            _settings.SpeedHistoryEnabled.Should().BeFalse();
            _settingsChangedCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_SpeedHistoryUnchanged_WHEN_SameValueProvided_THEN_DoesNotRaiseCallback()
        {
            var target = RenderTarget();
            var speedHistorySwitch = FindSwitch(target, "AppSettingsSpeedHistoryEnabled");

            await target.InvokeAsync(() => speedHistorySwitch.Instance.ValueChanged.InvokeAsync(false));

            _settingsChangedCount.Should().Be(0);
        }

        private IRenderedComponent<GeneralAppSettingsTab> RenderTarget()
        {
            return TestContext.Render<GeneralAppSettingsTab>(parameters =>
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
