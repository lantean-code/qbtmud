using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class BehaviourOptionsTests : RazorComponentTestBase<BehaviourOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldDisplayPreferenceStates()
        {
            var preferences = DeserializePreferences("""
            {
                "confirm_torrent_deletion": true,
                "status_bar_external_ip": false,
                "file_log_enabled": true,
                "file_log_path": "/logs",
                "file_log_backup_enabled": true,
                "file_log_max_size": 4096,
                "file_log_delete_old": true,
                "file_log_age": 7,
                "file_log_age_type": 2,
                "performance_warning": true
            }
            """);

            var updatePreferences = new UpdatePreferences();
            UpdatePreferences? lastChanged = null;

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => lastChanged = value));
            });

            FindSwitch(target, "ConfirmTorrentDeletion").Instance.Value.Should().BeTrue();
            FindSwitch(target, "StatusBarExternalIp").Instance.Value.Should().BeFalse();
            FindSwitch(target, "FileLogEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "FileLogBackupEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "FileLogDeleteOld").Instance.Value.Should().BeTrue();
            FindSwitch(target, "PerformanceWarning").Instance.Value.Should().BeTrue();

            var pathField = FindTextField(target, "FileLogPath");
            pathField.Instance.Value.Should().Be("/logs");
            pathField.Instance.Disabled.Should().BeFalse();

            FindNumeric(target, "FileLogMaxSize").Instance.Value.Should().Be(4096);
            FindNumeric(target, "FileLogAge").Instance.Value.Should().Be(7);

            FindSelect<int>(target, "FileLogAgeType").Instance.Value.Should().Be(2);

            lastChanged.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_FileLogDisabled_WHEN_Toggled_THEN_ShouldEnableInputsAndEmitPreferences()
        {
            var preferences = DeserializePreferences("""
            {
                "file_log_enabled": false,
                "file_log_path": "/logs",
                "file_log_backup_enabled": false,
                "file_log_delete_old": false,
                "file_log_age": 3,
                "file_log_age_type": 1,
                "file_log_max_size": 1024
            }
            """);

            var updatePreferences = new UpdatePreferences();
            UpdatePreferences? lastChanged = null;

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => lastChanged = value));
            });

            var pathField = FindTextField(target, "FileLogPath");
            pathField.Instance.Disabled.Should().BeTrue();

            var maxSizeField = FindNumeric(target, "FileLogMaxSize");
            var ageField = FindNumeric(target, "FileLogAge");
            maxSizeField.Instance.Disabled.Should().BeTrue();
            ageField.Instance.Disabled.Should().BeTrue();

            var fileLogSwitch = FindSwitch(target, "FileLogEnabled");
            await target.InvokeAsync(() => fileLogSwitch.Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.FileLogEnabled.Should().BeTrue();
            lastChanged.Should().Be(updatePreferences);

            pathField.Instance.Disabled.Should().BeFalse();
            maxSizeField.Instance.Disabled.Should().BeFalse();
            ageField.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_FileLogInputs_WHEN_Modified_THEN_ShouldUpdatePreferencesAndNotify()
        {
            var preferences = DeserializePreferences("""
            {
                "file_log_enabled": true,
                "file_log_path": "/logs",
                "file_log_backup_enabled": true,
                "file_log_delete_old": true,
                "file_log_age": 5,
                "file_log_age_type": 0,
                "file_log_max_size": 256
            }
            """);

            var updatePreferences = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var pathField = FindTextField(target, "FileLogPath");
            await target.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("/var/app/logs"));

            var maxSizeField = FindNumeric(target, "FileLogMaxSize");
            var ageField = FindNumeric(target, "FileLogAge");
            await target.InvokeAsync(() => maxSizeField.Instance.ValueChanged.InvokeAsync(512));
            await target.InvokeAsync(() => ageField.Instance.ValueChanged.InvokeAsync(9));

            updatePreferences.FileLogPath.Should().Be("/var/app/logs");
            updatePreferences.FileLogMaxSize.Should().Be(512);
            updatePreferences.FileLogAge.Should().Be(9);

            raised.Count.Should().Be(3);
            foreach (var item in raised)
            {
                item.Should().BeSameAs(updatePreferences);
            }
        }

        [Fact]
        public async Task GIVEN_PrimarySwitches_WHEN_Toggled_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences("""
            {
                "confirm_torrent_deletion": false,
                "status_bar_external_ip": true,
                "performance_warning": false,
                "file_log_enabled": false
            }
            """);

            var updatePreferences = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var confirmSwitch = FindSwitch(target, "ConfirmTorrentDeletion");
            await target.InvokeAsync(() => confirmSwitch.Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.ConfirmTorrentDeletion.Should().BeTrue();

            var externalSwitch = FindSwitch(target, "StatusBarExternalIp");
            await target.InvokeAsync(() => externalSwitch.Instance.ValueChanged.InvokeAsync(false));

            updatePreferences.StatusBarExternalIp.Should().BeFalse();

            var performanceSwitch = FindSwitch(target, "PerformanceWarning");
            await target.InvokeAsync(() => performanceSwitch.Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.PerformanceWarning.Should().BeTrue();

            raised.Should().HaveCount(3);
            raised.Should().AllSatisfy(value => value.Should().BeSameAs(updatePreferences));
        }

        [Fact]
        public async Task GIVEN_FileLogToggles_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences("""
            {
                "file_log_enabled": true,
                "file_log_backup_enabled": false,
                "file_log_delete_old": false,
                "file_log_age": 10,
                "file_log_age_type": 1,
                "file_log_max_size": 2048
            }
            """);

            var updatePreferences = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var logSwitch = FindSwitch(target, "FileLogEnabled");
            await target.InvokeAsync(() => logSwitch.Instance.ValueChanged.InvokeAsync(true));

            await target.InvokeAsync(() =>
                FindSwitch(target, "FileLogBackupEnabled").Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.FileLogBackupEnabled.Should().BeTrue();

            await target.InvokeAsync(() =>
                FindSwitch(target, "FileLogDeleteOld").Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.FileLogDeleteOld.Should().BeTrue();

            var ageSelect = FindSelect<int>(target, "FileLogAgeType");
            await target.InvokeAsync(() => ageSelect.Instance.ValueChanged.InvokeAsync(2));

            updatePreferences.FileLogAgeType.Should().Be(2);

            var maxSizeField = FindNumeric(target, "FileLogMaxSize");
            await target.InvokeAsync(() => maxSizeField.Instance.ValueChanged.InvokeAsync(4096));
            updatePreferences.FileLogMaxSize.Should().Be(4096);

            var pathField = FindTextField(target, "FileLogPath");
            pathField.Instance.Disabled.Should().BeFalse();
            var ageField = FindNumeric(target, "FileLogAge");
            var numericFields = new[] { maxSizeField, ageField };
            foreach (var numeric in numericFields)
            {
                numeric.Instance.Disabled.Should().BeFalse();
            }

            await target.InvokeAsync(() => logSwitch.Instance.ValueChanged.InvokeAsync(false));

            updatePreferences.FileLogEnabled.Should().BeFalse();

            pathField.Instance.Disabled.Should().BeTrue();
            foreach (var numeric in numericFields)
            {
                numeric.Instance.Disabled.Should().BeTrue();
            }

            raised.Should().HaveCount(6);
            raised.Should().AllSatisfy(value => value.Should().BeSameAs(updatePreferences));
        }

        private static Preferences DeserializePreferences(string json)
        {
            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<BehaviourOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<BehaviourOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<BehaviourOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }
    }
}
