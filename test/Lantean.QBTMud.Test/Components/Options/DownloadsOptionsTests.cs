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
    public sealed class DownloadsOptionsTests : RazorComponentTestBase<DownloadsOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindSelect<string>(target, "TorrentContentLayout").Instance.GetState(x => x.Value).Should().Be("Original");
            FindSwitch(target, "AddToTopOfQueue").Instance.Value.Should().BeTrue();
            FindSwitch(target, "AutoDeleteMode").Instance.Value.Should().BeTrue();
            FindSwitch(target, "PreallocateAll").Instance.Value.Should().BeTrue();
            FindSelect<bool>(target, "AutoTmmEnabled").Instance.GetState(x => x.Value).Should().BeTrue();
            FindPathField(target, "TempPath").Instance.Disabled.Should().BeFalse();
            FindPathField(target, "ExportDir").Instance.Disabled.Should().BeFalse();
            FindSwitch(target, "MailNotificationEnabled").Instance.Value.Should().BeTrue();
            FindTextField(target, "MailNotificationUsername").Instance.Disabled.Should().BeFalse();
            target.FindComponents<MudSimpleTable>().Count.Should().Be(1);
            update.ScanDirs.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TogglesAndInputs_WHEN_Changed_THEN_ShouldUpdatePreferencesAndNotify()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addToTopSwitch = FindSwitch(target, "AddToTopOfQueue");
            await target.InvokeAsync(() => addToTopSwitch.Instance.ValueChanged.InvokeAsync(false));

            var layoutSelect = FindSelect<string>(target, "TorrentContentLayout");
            await target.InvokeAsync(() => layoutSelect.Instance.ValueChanged.InvokeAsync("NoSubfolder"));

            var tempSwitch = FindSwitch(target, "TempPathEnabled");
            await target.InvokeAsync(() => tempSwitch.Instance.ValueChanged.InvokeAsync(false));

            var tempPathField = FindPathField(target, "TempPath");
            await target.InvokeAsync(() => tempPathField.Instance.ValueChanged.InvokeAsync("/tmp-new"));

            var subcategoriesSwitch = FindSwitch(target, "UseSubcategories");
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
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var addStoppedSwitch = FindSwitch(target, "AddStoppedEnabled");
            await target.InvokeAsync(() => addStoppedSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AddStoppedEnabled.Should().BeTrue();

            var stopConditionSelect = FindSelect<string>(target, "TorrentStopCondition");
            await target.InvokeAsync(() => stopConditionSelect.Instance.ValueChanged.InvokeAsync("FilesChecked"));
            update.TorrentStopCondition.Should().Be("FilesChecked");

            var autoDeleteSwitch = FindSwitch(target, "AutoDeleteMode");
            await target.InvokeAsync(() => autoDeleteSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutoDeleteMode.Should().Be(0);

            var preallocateSwitch = FindSwitch(target, "PreallocateAll");
            await target.InvokeAsync(() => preallocateSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.PreallocateAll.Should().BeFalse();

            var extensionSwitch = FindSwitch(target, "IncompleteFilesExt");
            await target.InvokeAsync(() => extensionSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.IncompleteFilesExt.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ManagementOptions_WHEN_Toggled_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var autoModeSelect = FindSelect<bool>(target, "AutoTmmEnabled");
            await target.InvokeAsync(() => autoModeSelect.Instance.ValueChanged.InvokeAsync(false));
            update.AutoTmmEnabled.Should().BeFalse();

            var categorySelect = FindSelect<bool>(target, "TorrentChangedTmmEnabled");
            await target.InvokeAsync(() => categorySelect.Instance.ValueChanged.InvokeAsync(false));
            update.TorrentChangedTmmEnabled.Should().BeFalse();

            var defaultSaveSelect = FindSelect<bool>(target, "SavePathChangedTmmEnabled");
            await target.InvokeAsync(() => defaultSaveSelect.Instance.ValueChanged.InvokeAsync(true));
            update.SavePathChangedTmmEnabled.Should().BeTrue();

            var categoryPathSelect = FindSelect<bool>(target, "CategoryChangedTmmEnabled");
            await target.InvokeAsync(() => categoryPathSelect.Instance.ValueChanged.InvokeAsync(false));
            update.CategoryChangedTmmEnabled.Should().BeFalse();

            var savePathField = FindPathField(target, "SavePath");
            await target.InvokeAsync(() => savePathField.Instance.ValueChanged.InvokeAsync("/downloads/alt"));
            update.SavePath.Should().Be("/downloads/alt");

            var exportSwitch = FindSwitch(target, "ExportDirEnabled");
            await target.InvokeAsync(() => exportSwitch.Instance.ValueChanged.InvokeAsync(false));
            exportSwitch.Instance.Value.Should().BeFalse();

            var exportPathField = FindPathField(target, "ExportDir");
            await target.InvokeAsync(() => exportPathField.Instance.ValueChanged.InvokeAsync("/archive"));
            update.ExportDir.Should().Be("/archive");

            var exportFinSwitch = FindSwitch(target, "ExportDirFinEnabled");
            await target.InvokeAsync(() => exportFinSwitch.Instance.ValueChanged.InvokeAsync(false));
            exportFinSwitch.Instance.Value.Should().BeFalse();

            var exportFinField = FindPathField(target, "ExportDirFin");
            await target.InvokeAsync(() => exportFinField.Instance.ValueChanged.InvokeAsync("/archive_fin"));
            update.ExportDirFin.Should().Be("/archive_fin");
        }

        [Fact]
        public async Task GIVEN_ExcludedFiles_WHEN_Toggled_THEN_ShouldUpdatePreferencesAndInputs()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var exclusionsField = FindTextField(target, "ExcludedFileNames");
            exclusionsField.Instance.Disabled.Should().BeFalse();

            var exclusionsSwitch = FindSwitch(target, "ExcludedFileNamesEnabled");
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
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var enabledSwitch = FindSwitch(target, "MailNotificationEnabled");
            await target.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationEnabled.Should().BeFalse();

            var senderField = FindTextField(target, "MailNotificationSender");
            senderField.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationEnabled.Should().BeTrue();

            await target.InvokeAsync(() => senderField.Instance.ValueChanged.InvokeAsync("from@example.com"));
            update.MailNotificationSender.Should().Be("from@example.com");

            var emailField = FindTextField(target, "MailNotificationEmail");
            await target.InvokeAsync(() => emailField.Instance.ValueChanged.InvokeAsync("to@example.com"));
            update.MailNotificationEmail.Should().Be("to@example.com");

            var smtpField = FindTextField(target, "MailNotificationSmtp");
            await target.InvokeAsync(() => smtpField.Instance.ValueChanged.InvokeAsync("smtp.mail.local"));
            update.MailNotificationSmtp.Should().Be("smtp.mail.local");

            var sslSwitch = FindSwitch(target, "MailNotificationSslEnabled");
            await target.InvokeAsync(() => sslSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationSslEnabled.Should().BeFalse();

            await target.InvokeAsync(() => sslSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationSslEnabled.Should().BeTrue();

            var authSwitch = FindSwitch(target, "MailNotificationAuthEnabled");
            await target.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationAuthEnabled.Should().BeFalse();

            await target.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.MailNotificationAuthEnabled.Should().BeTrue();

            var usernameField = FindTextField(target, "MailNotificationUsername");
            await target.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync("mailer"));
            update.MailNotificationUsername.Should().Be("mailer");

            var passwordField = FindTextField(target, "MailNotificationPassword");
            await target.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync("secret"));
            update.MailNotificationPassword.Should().Be("secret");

            raised.Should().NotBeEmpty();

            await target.InvokeAsync(() => enabledSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.MailNotificationEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_AutorunSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addSwitch = FindSwitch(target, "AutorunOnTorrentAddedEnabled");
            await target.InvokeAsync(() => addSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutorunOnTorrentAddedEnabled.Should().BeFalse();

            await target.InvokeAsync(() => addSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AutorunOnTorrentAddedEnabled.Should().BeTrue();

            var addProgramField = FindTextField(target, "AutorunOnTorrentAddedProgram");
            await target.InvokeAsync(() => addProgramField.Instance.ValueChanged.InvokeAsync("/opt/add.sh"));
            await target.InvokeAsync(() => addProgramField.Instance.ValueChanged.InvokeAsync("/opt/add.sh"));
            update.AutorunOnTorrentAddedProgram.Should().Be("/opt/add.sh");

            var finishSwitch = FindSwitch(target, "AutorunEnabled");
            await target.InvokeAsync(() => finishSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.AutorunEnabled.Should().BeFalse();

            await target.InvokeAsync(() => finishSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.AutorunEnabled.Should().BeTrue();

            var finishProgramField = FindTextField(target, "AutorunProgram");
            await target.InvokeAsync(() => finishProgramField.Instance.ValueChanged.InvokeAsync("/opt/finish.sh"));
            update.AutorunProgram.Should().Be("/opt/finish.sh");

            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_ScanDirectories_WHEN_Modified_THEN_ShouldUpdateScanDirs()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            var target = TestContext.Render<DownloadsOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var addedRowSelect = FindAddedScanDirType(target, 0);
            await target.InvokeAsync(() => addedRowSelect.Instance.ValueChanged.InvokeAsync("0"));

            var existingKeyField = FindExistingScanDirKey(target, 0);
            await target.InvokeAsync(() => existingKeyField.Instance.ValueChanged.InvokeAsync("/watch-renamed"));

            update.ScanDirs.Should().NotBeNull();
            update.ScanDirs!.ContainsKey("/watch-renamed").Should().BeTrue();
            update.ScanDirs.ContainsKey("/watch").Should().BeFalse();

            var newKeyField = FindAddedScanDirKey(target, 0);
            await target.InvokeAsync(() => newKeyField.Instance.ValueChanged.InvokeAsync("/new"));

            update.ScanDirs.ContainsKey("/new").Should().BeTrue();

            var removeButton = FindExistingScanDirRemoveButton(target, 0);
            await target.InvokeAsync(() => removeButton.Instance.OnClick.InvokeAsync());

            update.ScanDirs.ContainsKey("/watch-renamed").Should().BeFalse();
            raised.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<DownloadsOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<DownloadsOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }

        private static IRenderedComponent<PathAutocomplete> FindPathField(IRenderedComponent<DownloadsOptions> target, string testId)
        {
            return FindComponentByTestId<PathAutocomplete>(target, testId);
        }

        private static IRenderedComponent<PathAutocomplete> FindExistingScanDirKey(IRenderedComponent<DownloadsOptions> target, int index)
        {
            return FindPathField(target, $"ScanDirsExisting[{index}].Key");
        }

        private static IRenderedComponent<MudSelect<string>> FindAddedScanDirType(IRenderedComponent<DownloadsOptions> target, int index)
        {
            return FindSelect<string>(target, $"AddedScanDirs[{index}].Type");
        }

        private static IRenderedComponent<PathAutocomplete> FindAddedScanDirKey(IRenderedComponent<DownloadsOptions> target, int index)
        {
            return FindPathField(target, $"AddedScanDirs[{index}].Key");
        }

        private static IRenderedComponent<MudIconButton> FindExistingScanDirRemoveButton(IRenderedComponent<DownloadsOptions> target, int index)
        {
            return FindComponentByTestId<MudIconButton>(target, $"ScanDirsExisting[{index}].Remove");
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
    }
}
