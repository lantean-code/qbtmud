using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class DownloadsOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public DownloadsOptionsTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            target.FindComponents<MudSelect<string>>().Single(s => s.Instance.Label == "Torrent content layout").Instance.Value.Should().Be("Original");
            target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Add to top of queue").Instance.Value.Should().BeTrue();
            target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Delete .torrent files afterwards").Instance.Value.Should().BeTrue();
            target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Pre-allocate disk space for all files").Instance.Value.Should().BeTrue();
            target.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "Default Torrent Management Mode").Instance.Value.Should().BeTrue();
            target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/temp").Instance.Disabled.Should().BeFalse();
            target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/export").Instance.Disabled.Should().BeFalse();
            target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Email notification upon download completion").Instance.Value.Should().BeTrue();
            target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Username").Instance.Disabled.Should().BeFalse();
            target.FindAll("tbody tr").Count.Should().Be(2);
            update.ScanDirs.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TogglesAndInputs_WHEN_Changed_THEN_ShouldUpdatePreferencesAndNotify()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addToTopSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Add to top of queue");
            await target.InvokeAsync(() => addToTopSwitch.Instance.ValueChanged.InvokeAsync(false));

            var layoutSelect = target.FindComponents<MudSelect<string>>().Single(s => s.Instance.Label == "Torrent content layout");
            await target.InvokeAsync(() => layoutSelect.Instance.ValueChanged.InvokeAsync("NoSubfolder"));

            var tempSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Keep incomplete torrents in");
            await target.InvokeAsync(() => tempSwitch.Instance.ValueChanged.InvokeAsync(false));

            var tempPathField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/temp");
            await target.InvokeAsync(() => tempPathField.Instance.ValueChanged.InvokeAsync("/tmp-new"));

            var subcategoriesSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Use Subcategories");
            await target.InvokeAsync(() => subcategoriesSwitch.Instance.ValueChanged.InvokeAsync(false));

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
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var addStoppedSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Do not start the download automatically");
            await target.InvokeAsync(() => addStoppedSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AddStoppedEnabled.Should().BeTrue();

            var stopConditionSelect = target.FindComponents<MudSelect<string>>().Single(s => s.Instance.Label == "Torrent stop condition");
            await target.InvokeAsync(() => stopConditionSelect.Instance.ValueChanged.InvokeAsync("FilesChecked"));
            update.TorrentStopCondition.Should().Be("FilesChecked");

            var autoDeleteSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Delete .torrent files afterwards");
            await target.InvokeAsync(() => autoDeleteSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutoDeleteMode.Should().Be(0);

            var preallocateSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Pre-allocate disk space for all files");
            await target.InvokeAsync(() => preallocateSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.PreallocateAll.Should().BeFalse();

            var extensionSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Append .!qB extension to incomplete files");
            await target.InvokeAsync(() => extensionSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.IncompleteFilesExt.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ManagementOptions_WHEN_Toggled_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var autoModeSelect = target.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "Default Torrent Management Mode");
            await target.InvokeAsync(() => autoModeSelect.Instance.ValueChanged.InvokeAsync(false));
            update.AutoTmmEnabled.Should().BeFalse();

            var categorySelect = target.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "When Torrent Category changed");
            await target.InvokeAsync(() => categorySelect.Instance.ValueChanged.InvokeAsync(false));
            update.TorrentChangedTmmEnabled.Should().BeFalse();

            var defaultSaveSelect = target.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "When Default Save Path changed");
            await target.InvokeAsync(() => defaultSaveSelect.Instance.ValueChanged.InvokeAsync(true));
            update.SavePathChangedTmmEnabled.Should().BeTrue();

            var categoryPathSelect = target.FindComponents<MudSelect<bool>>().Single(s => s.Instance.Label == "When Category Save Path changed");
            await target.InvokeAsync(() => categoryPathSelect.Instance.ValueChanged.InvokeAsync(false));
            update.CategoryChangedTmmEnabled.Should().BeFalse();

            var savePathField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/downloads");
            await target.InvokeAsync(() => savePathField.Instance.ValueChanged.InvokeAsync("/downloads/alt"));
            update.SavePath.Should().Be("/downloads/alt");

            var exportSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Copy .torrent files to");
            await target.InvokeAsync(() => exportSwitch.Instance.ValueChanged.InvokeAsync(false));
            exportSwitch.Instance.Value.Should().BeFalse();

            var exportPathField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/export");
            await target.InvokeAsync(() => exportPathField.Instance.ValueChanged.InvokeAsync("/archive"));
            update.ExportDir.Should().Be("/archive");

            var exportFinSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Copy .torrent files for finished downloads to");
            await target.InvokeAsync(() => exportFinSwitch.Instance.ValueChanged.InvokeAsync(false));
            exportFinSwitch.Instance.Value.Should().BeFalse();

            var exportFinField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/export_fin");
            await target.InvokeAsync(() => exportFinField.Instance.ValueChanged.InvokeAsync("/archive_fin"));
            update.ExportDirFin.Should().Be("/archive_fin");
        }

        [Fact]
        public async Task GIVEN_ExcludedFiles_WHEN_Toggled_THEN_ShouldUpdatePreferencesAndInputs()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var exclusionsField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Excluded files names");
            exclusionsField.Instance.Disabled.Should().BeFalse();

            var exclusionsSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Excluded file names");
            await target.InvokeAsync(() => exclusionsSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ExcludedFileNamesEnabled.Should().BeFalse();
            exclusionsField.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => exclusionsField.Instance.ValueChanged.InvokeAsync("*.tmp;*.bak"));
            update.ExcludedFileNames.Should().Be("*.tmp;*.bak");

            await target.InvokeAsync(() => exclusionsSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.ExcludedFileNamesEnabled.Should().BeTrue();
            exclusionsField.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_MailNotifications_WHEN_Adjusted_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var enabledSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Email notification upon download completion");
            await target.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationEnabled.Should().BeFalse();

            var senderField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "From");
            senderField.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationEnabled.Should().BeTrue();

            await target.InvokeAsync(() => senderField.Instance.ValueChanged.InvokeAsync("from@example.com"));
            update.MailNotificationSender.Should().Be("from@example.com");

            var emailField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "To");
            await target.InvokeAsync(() => emailField.Instance.ValueChanged.InvokeAsync("to@example.com"));
            update.MailNotificationEmail.Should().Be("to@example.com");

            var smtpField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "SMTP server");
            await target.InvokeAsync(() => smtpField.Instance.ValueChanged.InvokeAsync("smtp.mail.local"));
            update.MailNotificationSmtp.Should().Be("smtp.mail.local");

            var sslSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "This server requires a secure connection (SSL)");
            await target.InvokeAsync(() => sslSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationSslEnabled.Should().BeFalse();

            await target.InvokeAsync(() => sslSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationSslEnabled.Should().BeTrue();

            var authSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Authentication");
            await target.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationAuthEnabled.Should().BeFalse();

            await target.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationAuthEnabled.Should().BeTrue();

            var usernameField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Username");
            await target.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync("mailer"));
            update.MailNotificationUsername.Should().Be("mailer");

            var passwordField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "Password");
            await target.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync("secret"));
            update.MailNotificationPassword.Should().Be("secret");

            raised.Should().NotBeEmpty();

            await target.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_AutorunSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Run external program on torrent added");
            await target.InvokeAsync(() => addSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutorunOnTorrentAddedEnabled.Should().BeFalse();

            await target.InvokeAsync(() => addSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AutorunOnTorrentAddedEnabled.Should().BeTrue();

            var addProgramField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "External program" && tf.Instance.Value == "/bin/add.sh");
            await target.InvokeAsync(() => addProgramField.Instance.ValueChanged.InvokeAsync("/opt/add.sh"));
            await target.InvokeAsync(() => addProgramField.Instance.ValueChanged.InvokeAsync("/opt/add.sh"));
            update.AutorunOnTorrentAddedProgram.Should().Be("/opt/add.sh");

            var finishSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Run external program on torrent finished");
            await target.InvokeAsync(() => finishSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutorunEnabled.Should().BeFalse();

            await target.InvokeAsync(() => finishSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AutorunEnabled.Should().BeTrue();

            var finishProgramField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Label == "External program" && tf.Instance.Value == "/bin/finish.sh");
            await target.InvokeAsync(() => finishProgramField.Instance.ValueChanged.InvokeAsync("/opt/finish.sh"));
            update.AutorunProgram.Should().Be("/opt/finish.sh");

            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_ScanDirectories_WHEN_Modified_THEN_ShouldUpdateScanDirs()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = _context.RenderComponent<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addedRowSelect = target.FindComponents<MudSelect<string>>()[^1];
            await target.InvokeAsync(() => addedRowSelect.Instance.ValueChanged.InvokeAsync("0"));

            var watchField = target.FindComponents<MudTextField<string>>().Single(tf => tf.Instance.Value == "/watch");
            await target.InvokeAsync(() => watchField.Instance.ValueChanged.InvokeAsync("/watch-renamed"));

            update.ScanDirs.Should().NotBeNull();
            update.ScanDirs!.ContainsKey("/watch-renamed").Should().BeTrue();
            update.ScanDirs.ContainsKey("/watch").Should().BeFalse();

            var newKeyField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == string.Empty);
            await target.InvokeAsync(() => newKeyField.Instance.ValueChanged.InvokeAsync("/new"));

            update.ScanDirs.ContainsKey("/new").Should().BeTrue();

            var removeButton = target.FindComponents<MudIconButton>()
                .First(btn => btn.Instance.Icon == Icons.Material.Outlined.Remove);
            await target.InvokeAsync(() => removeButton.Instance.OnClick.InvokeAsync());

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
            _context.Dispose();
        }
    }
}