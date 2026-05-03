using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Options;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components.Options
{
    public sealed class BehaviourOptionsTests : RazorComponentTestBase<BehaviourOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldDisplayPreferenceStates()
        {
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.ConfirmTorrentDeletion = true;
                spec.FileLogAge = 7;
                spec.FileLogAgeType = 2;
                spec.FileLogBackupEnabled = true;
                spec.FileLogDeleteOld = true;
                spec.FileLogEnabled = true;
                spec.FileLogMaxSize = 4096;
                spec.FileLogPath = "/logs";
                spec.PerformanceWarning = true;
                spec.StatusBarExternalIp = false;
            });

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
            pathField.Instance.GetState(x => x.Value).Should().Be("/logs");
            pathField.Instance.Disabled.Should().BeFalse();

            FindNumeric(target, "FileLogMaxSize").Instance.GetState(x => x.Value).Should().Be(4096);
            FindNumeric(target, "FileLogAge").Instance.GetState(x => x.Value).Should().Be(7);

            FindSelect<int>(target, "FileLogAgeType").Instance.GetState(x => x.Value).Should().Be(2);

            lastChanged.Should().BeNull();
        }

        [Fact]
        public void GIVEN_LocaleNotInCatalog_WHEN_Rendered_THEN_ShouldResolveLocaleAndDisplayName()
        {
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.Locale = "C";
            });

            var updatePreferences = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var languageSelect = FindSelect<string>(target, "UserInterfaceLanguage");
            languageSelect.Instance.GetState(x => x.Value).Should().Be("en");
            languageSelect.Instance.ToStringFunc.Should().NotBeNull();
            languageSelect.Instance.ToStringFunc!("en").Should().Be("English");
        }

        [Fact]
        public void GIVEN_WhitespaceLocale_WHEN_FormattingDisplayName_THEN_ShouldReturnWhitespace()
        {
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.Locale = "en";
            });

            var updatePreferences = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var languageSelect = FindSelect<string>(target, "UserInterfaceLanguage");
            languageSelect.Instance.ToStringFunc.Should().NotBeNull();
            languageSelect.Instance.ToStringFunc!(" ").Should().Be(" ");
        }

        [Fact]
        public void GIVEN_NullPreferences_WHEN_Rendered_THEN_ShouldKeepDefaults()
        {
            var updatePreferences = new UpdatePreferences();
            UpdatePreferences? lastChanged = null;

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<BehaviourOptions>(parameters =>
            {
                parameters.Add(p => p.UpdatePreferences, updatePreferences);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => lastChanged = value));
            });

            FindSwitch(target, "ConfirmTorrentDeletion").Instance.Value.Should().BeNull();
            FindSwitch(target, "StatusBarExternalIp").Instance.Value.Should().BeNull();
            FindSwitch(target, "FileLogEnabled").Instance.Value.Should().BeNull();
            FindTextField(target, "FileLogPath").Instance.GetState(x => x.Value).Should().BeNull();
            lastChanged.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_FileLogDisabled_WHEN_Toggled_THEN_ShouldEnableInputsAndEmitPreferences()
        {
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.FileLogAge = 3;
                spec.FileLogAgeType = 1;
                spec.FileLogBackupEnabled = false;
                spec.FileLogDeleteOld = false;
                spec.FileLogEnabled = false;
                spec.FileLogMaxSize = 1024;
                spec.FileLogPath = "/logs";
            });

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
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.FileLogAge = 5;
                spec.FileLogAgeType = 0;
                spec.FileLogBackupEnabled = true;
                spec.FileLogDeleteOld = true;
                spec.FileLogEnabled = true;
                spec.FileLogMaxSize = 256;
                spec.FileLogPath = "/logs";
            });

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
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.ConfirmTorrentDeletion = false;
                spec.FileLogEnabled = false;
                spec.PerformanceWarning = false;
                spec.StatusBarExternalIp = true;
            });

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
            var preferences = PreferencesFactory.CreatePreferences(spec =>
            {
                spec.FileLogAge = 10;
                spec.FileLogAgeType = 1;
                spec.FileLogBackupEnabled = false;
                spec.FileLogDeleteOld = false;
                spec.FileLogEnabled = true;
                spec.FileLogMaxSize = 2048;
            });

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
