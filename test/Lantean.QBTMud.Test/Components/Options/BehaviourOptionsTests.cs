using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class BehaviourOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public BehaviourOptionsTests()
        {
            _target = new ComponentTestContext();
        }

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

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => lastChanged = value));
            });

            var switches = cut.FindComponents<FieldSwitch>();

            switches.Single(s => s.Instance.Label == "Confirm when deleting torrents").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Show external IP in status bar").Instance.Value.Should().BeFalse();
            switches.Single(s => s.Instance.Label == "Log file").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Backup the log after").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Delete backups older than").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Log performance warnings").Instance.Value.Should().BeTrue();

            var pathField = cut.FindComponent<MudTextField<string>>();
            pathField.Instance.Value.Should().Be("/logs");
            pathField.Instance.Disabled.Should().BeFalse();

            var numericFields = cut.FindComponents<MudNumericField<int>>();
            numericFields[0].Instance.Value.Should().Be(4096);
            numericFields[1].Instance.Value.Should().Be(7);

            var ageTypeSelect = cut.FindComponent<MudSelect<int>>();
            ageTypeSelect.Instance.Value.Should().Be(2);

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

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => lastChanged = value));
            });

            var pathField = cut.FindComponent<MudTextField<string>>();
            pathField.Instance.Disabled.Should().BeTrue();

            var numericFields = cut.FindComponents<MudNumericField<int>>();
            numericFields[0].Instance.Disabled.Should().BeTrue();
            numericFields[1].Instance.Disabled.Should().BeTrue();

            var fileLogSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Log file");
            await cut.InvokeAsync(() => fileLogSwitch.Find("input").Change(true));

            updatePreferences.FileLogEnabled.Should().BeTrue();
            lastChanged.Should().Be(updatePreferences);

            pathField.Instance.Disabled.Should().BeFalse();
            numericFields[0].Instance.Disabled.Should().BeFalse();
            numericFields[1].Instance.Disabled.Should().BeFalse();
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

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var pathField = cut.FindComponent<MudTextField<string>>();
            await cut.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("/var/app/logs"));

            var numericFields = cut.FindComponents<MudNumericField<int>>();
            await cut.InvokeAsync(() => numericFields[0].Instance.ValueChanged.InvokeAsync(512));
            await cut.InvokeAsync(() => numericFields[1].Instance.ValueChanged.InvokeAsync(9));

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

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var switches = cut.FindComponents<FieldSwitch>();

            var confirmSwitch = switches.Single(s => s.Instance.Label == "Confirm when deleting torrents");
            await cut.InvokeAsync(() => confirmSwitch.Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.ConfirmTorrentDeletion.Should().BeTrue();

            var externalSwitch = switches.Single(s => s.Instance.Label == "Show external IP in status bar");
            await cut.InvokeAsync(() => externalSwitch.Instance.ValueChanged.InvokeAsync(false));

            updatePreferences.StatusBarExternalIp.Should().BeFalse();

            var performanceSwitch = switches.Single(s => s.Instance.Label == "Log performance warnings");
            await cut.InvokeAsync(() => performanceSwitch.Instance.ValueChanged.InvokeAsync(true));

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

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var switches = cut.FindComponents<FieldSwitch>();

            var logSwitch = switches.Single(s => s.Instance.Label == "Log file");
            await cut.InvokeAsync(() => logSwitch.Instance.ValueChanged.InvokeAsync(true));

            await cut.InvokeAsync(() =>
                switches.Single(s => s.Instance.Label == "Backup the log after").Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.FileLogBackupEnabled.Should().BeTrue();

            await cut.InvokeAsync(() =>
                switches.Single(s => s.Instance.Label == "Delete backups older than").Instance.ValueChanged.InvokeAsync(true));

            updatePreferences.FileLogDeleteOld.Should().BeTrue();

            var ageSelect = cut.FindComponent<MudSelect<int>>();
            await cut.InvokeAsync(() => ageSelect.Instance.ValueChanged.InvokeAsync(2));

            updatePreferences.FileLogAgeType.Should().Be(2);

            var maxSizeField = cut.FindComponents<MudNumericField<int>>().Single(n => n.Instance.Value == 2048);
            await cut.InvokeAsync(() => maxSizeField.Instance.ValueChanged.InvokeAsync(4096));
            updatePreferences.FileLogMaxSize.Should().Be(4096);

            var pathField = cut.FindComponent<MudTextField<string>>();
            pathField.Instance.Disabled.Should().BeFalse();
            var numericFields = cut.FindComponents<MudNumericField<int>>();
            foreach (var numeric in numericFields)
            {
                numeric.Instance.Disabled.Should().BeFalse();
            }

            await cut.InvokeAsync(() => logSwitch.Instance.ValueChanged.InvokeAsync(false));

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

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
