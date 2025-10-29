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
    public sealed class DownloadsOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public DownloadsOptionsTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            cut.FindComponents<MudSelect<string>>().Single(s => s.Instance.Label == "Torrent content layout").Instance.Value.Should().Be("Original");
            cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Add to top of queue").Instance.Value.Should().BeTrue();
            cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Delete .torrent files afterwards").Instance.Value.Should().BeTrue();
            cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Pre-allocate disk space for all files").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "Default Torrent Management Mode").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/temp").Instance.Disabled.Should().BeFalse();
            cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/export").Instance.Disabled.Should().BeFalse();
            cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Email notification upon download completion").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Username").Instance.Disabled.Should().BeFalse();
            cut.FindAll("tbody tr").Count.Should().Be(2);
            update.ScanDirs.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TogglesAndInputs_WHEN_Changed_THEN_ShouldUpdatePreferencesAndNotify()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addToTopSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Add to top of queue");
            await cut.InvokeAsync(() => addToTopSwitch.Instance.ValueChanged.InvokeAsync(false));

            var layoutSelect = cut.FindComponents<MudSelect<string>>().Single(s => s.Instance.Label == "Torrent content layout");
            await cut.InvokeAsync(() => layoutSelect.Instance.ValueChanged.InvokeAsync("NoSubfolder"));

            var tempSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Keep incomplete torrents in");
            await cut.InvokeAsync(() => tempSwitch.Instance.ValueChanged.InvokeAsync(false));

            var tempPathField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/temp");
            await cut.InvokeAsync(() => tempPathField.Instance.ValueChanged.InvokeAsync("/tmp-new"));

            var subcategoriesSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Use Subcategories");
            await cut.InvokeAsync(() => subcategoriesSwitch.Instance.ValueChanged.InvokeAsync(false));

            update.AddToTopOfQueue.Should().BeFalse();
            update.TorrentContentLayout.Should().Be("NoSubfolder");
            update.TempPathEnabled.Should().BeFalse();
            update.TempPath.Should().Be("/tmp-new");
            update.UseSubcategories.Should().BeFalse();

            raised.Should().NotBeEmpty();
            raised.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_AddTorrentSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var addStoppedSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Do not start the download automatically");
            await cut.InvokeAsync(() => addStoppedSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AddStoppedEnabled.Should().BeTrue();

            var stopConditionSelect = cut.FindComponents<MudSelect<string>>().Single(s => s.Instance.Label == "Torrent stop condition");
            await cut.InvokeAsync(() => stopConditionSelect.Instance.ValueChanged.InvokeAsync("FilesChecked"));
            update.TorrentStopCondition.Should().Be("FilesChecked");

            var autoDeleteSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Delete .torrent files afterwards");
            await cut.InvokeAsync(() => autoDeleteSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutoDeleteMode.Should().Be(0);

            var preallocateSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Pre-allocate disk space for all files");
            await cut.InvokeAsync(() => preallocateSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.PreallocateAll.Should().BeFalse();

            var extensionSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Append .!qB extension to incomplete files");
            await cut.InvokeAsync(() => extensionSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.IncompleteFilesExt.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ManagementOptions_WHEN_Toggled_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var autoModeSelect = cut.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "Default Torrent Management Mode");
            await cut.InvokeAsync(() => autoModeSelect.Instance.ValueChanged.InvokeAsync(false));
            update.AutoTmmEnabled.Should().BeFalse();

            var categorySelect = cut.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "When Torrent Category changed");
            await cut.InvokeAsync(() => categorySelect.Instance.ValueChanged.InvokeAsync(false));
            update.TorrentChangedTmmEnabled.Should().BeFalse();

            var defaultSaveSelect = cut.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "When Default Save Path changed");
            await cut.InvokeAsync(() => defaultSaveSelect.Instance.ValueChanged.InvokeAsync(true));
            update.SavePathChangedTmmEnabled.Should().BeTrue();

            var categoryPathSelect = cut.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "When Category Save Path changed");
            await cut.InvokeAsync(() => categoryPathSelect.Instance.ValueChanged.InvokeAsync(false));
            update.CategoryChangedTmmEnabled.Should().BeFalse();

            var savePathField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/downloads");
            await cut.InvokeAsync(() => savePathField.Instance.ValueChanged.InvokeAsync("/downloads/alt"));
            update.SavePath.Should().Be("/downloads/alt");

            var exportSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Copy .torrent files to");
            await cut.InvokeAsync(() => exportSwitch.Instance.ValueChanged.InvokeAsync(false));
            exportSwitch.Instance.Value.Should().BeFalse();

            var exportPathField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/export");
            await cut.InvokeAsync(() => exportPathField.Instance.ValueChanged.InvokeAsync("/archive"));
            update.ExportDir.Should().Be("/archive");

            var exportFinSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Copy .torrent files for finished downloads to");
            await cut.InvokeAsync(() => exportFinSwitch.Instance.ValueChanged.InvokeAsync(false));
            exportFinSwitch.Instance.Value.Should().BeFalse();

            var exportFinField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/export_fin");
            await cut.InvokeAsync(() => exportFinField.Instance.ValueChanged.InvokeAsync("/archive_fin"));
            update.ExportDirFin.Should().Be("/archive_fin");
        }

        [Fact]
        public async Task GIVEN_ExcludedFiles_WHEN_Toggled_THEN_ShouldUpdatePreferencesAndInputs()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var exclusionsField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Excluded files names");
            exclusionsField.Instance.Disabled.Should().BeFalse();

            var exclusionsSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Excluded file names");
            await cut.InvokeAsync(() => exclusionsSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ExcludedFileNamesEnabled.Should().BeFalse();
            exclusionsField.Instance.Disabled.Should().BeTrue();

            await cut.InvokeAsync(() => exclusionsField.Instance.ValueChanged.InvokeAsync("*.tmp;*.bak"));
            update.ExcludedFileNames.Should().Be("*.tmp;*.bak");

            await cut.InvokeAsync(() => exclusionsSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.ExcludedFileNamesEnabled.Should().BeTrue();
            exclusionsField.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_MailNotifications_WHEN_Adjusted_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var enabledSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Email notification upon download completion");
            await cut.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationEnabled.Should().BeFalse();

            var senderField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "From");
            senderField.Instance.Disabled.Should().BeTrue();

            await cut.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationEnabled.Should().BeTrue();

            await cut.InvokeAsync(() => senderField.Instance.ValueChanged.InvokeAsync("from@example.com"));
            update.MailNotificationSender.Should().Be("from@example.com");

            var emailField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "To");
            await cut.InvokeAsync(() => emailField.Instance.ValueChanged.InvokeAsync("to@example.com"));
            update.MailNotificationEmail.Should().Be("to@example.com");

            var smtpField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "SMTP server");
            await cut.InvokeAsync(() => smtpField.Instance.ValueChanged.InvokeAsync("smtp.mail.local"));
            update.MailNotificationSmtp.Should().Be("smtp.mail.local");

            var sslSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "This server requires a secure connection (SSL)");
            await cut.InvokeAsync(() => sslSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationSslEnabled.Should().BeFalse();

            await cut.InvokeAsync(() => sslSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationSslEnabled.Should().BeTrue();

            var authSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Authentication");
            await cut.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationAuthEnabled.Should().BeFalse();

            await cut.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationAuthEnabled.Should().BeTrue();

            var usernameField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Username");
            await cut.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync("mailer"));
            update.MailNotificationUsername.Should().Be("mailer");

            var passwordField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Password");
            await cut.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync("secret"));
            update.MailNotificationPassword.Should().Be("secret");

            raised.Should().NotBeEmpty();

            await cut.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_AutorunSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Run external program on torrent added");
            await cut.InvokeAsync(() => addSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutorunOnTorrentAddedEnabled.Should().BeFalse();

            await cut.InvokeAsync(() => addSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AutorunOnTorrentAddedEnabled.Should().BeTrue();

            var addProgramField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "External program" && tf.Instance.Value == "/bin/add.sh");
            await cut.InvokeAsync(() => addProgramField.Instance.ValueChanged.InvokeAsync("/opt/add.sh"));
            await cut.InvokeAsync(() => addProgramField.Instance.ValueChanged.InvokeAsync("/opt/add.sh"));
            update.AutorunOnTorrentAddedProgram.Should().Be("/opt/add.sh");

            var finishSwitch = cut.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Run external program on torrent finished");
            await cut.InvokeAsync(() => finishSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutorunEnabled.Should().BeFalse();

            await cut.InvokeAsync(() => finishSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AutorunEnabled.Should().BeTrue();

            var finishProgramField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "External program" && tf.Instance.Value == "/bin/finish.sh");
            await cut.InvokeAsync(() => finishProgramField.Instance.ValueChanged.InvokeAsync("/opt/finish.sh"));
            update.AutorunProgram.Should().Be("/opt/finish.sh");

            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_ScanDirectories_WHEN_Modified_THEN_ShouldUpdateScanDirs()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addedRowSelect = cut.FindComponents<MudSelect<string>>().Last();
            await cut.InvokeAsync(() => addedRowSelect.Instance.ValueChanged.InvokeAsync("0"));

            var watchField = cut.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/watch");
            await cut.InvokeAsync(() => watchField.Instance.ValueChanged.InvokeAsync("/watch-renamed"));

            update.ScanDirs.Should().NotBeNull();
            update.ScanDirs!.ContainsKey("/watch-renamed").Should().BeTrue();
            update.ScanDirs.ContainsKey("/watch").Should().BeFalse();

            var newKeyField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == string.Empty);
            await cut.InvokeAsync(() => newKeyField.Instance.ValueChanged.InvokeAsync("/new"));

            update.ScanDirs.ContainsKey("/new").Should().BeTrue();

            var removeButton = cut.FindComponents<MudIconButton>()
                .First(btn => btn.Instance.Icon == Icons.Material.Outlined.Remove);
            await cut.InvokeAsync(() => removeButton.Instance.OnClick.InvokeAsync());

            update.ScanDirs.ContainsKey("/watch-renamed").Should().BeFalse();
            raised.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        private static Preferences DeserializePreferences()
        {
            const string json = """
            {
                "torrent_content_layout": "Original",
                "add_to_top_of_queue": true,
                "add_stopped_enabled": false,
                "torrent_stop_condition": "None",
                "auto_delete_mode": 1,
                "preallocate_all": true,
                "incomplete_files_ext": false,
                "auto_tmm_enabled": true,
                "torrent_changed_tmm_enabled": true,
                "save_path_changed_tmm_enabled": false,
                "category_changed_tmm_enabled": true,
                "use_subcategories": true,
                "save_path": "/downloads",
                "temp_path_enabled": true,
                "temp_path": "/temp",
                "export_dir": "/export",
                "export_dir_fin": "/export_fin",
                "scan_dirs": { "/watch": 1 },
                "excluded_file_names_enabled": true,
                "excluded_file_names": "*.tmp",
                "mail_notification_enabled": true,
                "mail_notification_sender": "noreply@example.com",
                "mail_notification_email": "user@example.com",
                "mail_notification_smtp": "smtp.example.com",
                "mail_notification_ssl_enabled": true,
                "mail_notification_auth_enabled": true,
                "mail_notification_username": "user",
                "mail_notification_password": "pass",
                "autorun_on_torrent_added_enabled": true,
                "autorun_on_torrent_added_program": "/bin/add.sh",
                "autorun_enabled": true,
                "autorun_program": "/bin/finish.sh"
            }
            """;

            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}


