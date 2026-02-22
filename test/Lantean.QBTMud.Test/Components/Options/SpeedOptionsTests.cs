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
    public sealed class SpeedOptionsTests : RazorComponentTestBase<SpeedOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldDisplayRatesAndScheduler()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindNumeric(target, "UpLimit").Instance.GetState(x => x.Value).Should().Be(50);
            FindNumeric(target, "DlLimit").Instance.GetState(x => x.Value).Should().Be(120);
            FindNumeric(target, "AltUpLimit").Instance.GetState(x => x.Value).Should().Be(10);
            FindNumeric(target, "AltDlLimit").Instance.GetState(x => x.Value).Should().Be(30);

            FindSwitch(target, "SchedulerEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "LimitUtpRate").Instance.Value.Should().BeTrue();
            FindSwitch(target, "LimitTcpOverhead").Instance.Value.Should().BeFalse();
            FindSwitch(target, "LimitLanPeers").Instance.Value.Should().BeTrue();

            FindTimePicker(target, "ScheduleFrom").Instance.Time.Should().Be(TimeSpan.FromHours(1));
            FindTimePicker(target, "ScheduleTo").Instance.Time.Should().Be(TimeSpan.FromHours(5));

            FindSelect<int>(target, "SchedulerDays").Instance.GetState(x => x.Value).Should().Be(1);

            update.UpLimit.Should().BeNull();
            update.SchedulerEnabled.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_UserAdjustments_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await target.InvokeAsync(() => FindNumeric(target, "UpLimit").Instance.ValueChanged.InvokeAsync(75));
            await target.InvokeAsync(() => FindNumeric(target, "DlLimit").Instance.ValueChanged.InvokeAsync(140));
            await target.InvokeAsync(() => FindNumeric(target, "AltUpLimit").Instance.ValueChanged.InvokeAsync(20));
            await target.InvokeAsync(() => FindNumeric(target, "AltDlLimit").Instance.ValueChanged.InvokeAsync(45));

            var schedulerSwitch = FindSwitch(target, "SchedulerEnabled");
            await target.InvokeAsync(() => schedulerSwitch.Instance.ValueChanged.InvokeAsync(false));

            var scheduleFrom = FindTimePicker(target, "ScheduleFrom");
            await target.InvokeAsync(() => scheduleFrom.Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(2.5)));

            var daysSelect = FindSelect<int>(target, "SchedulerDays");
            await target.InvokeAsync(() => daysSelect.Instance.ValueChanged.InvokeAsync(3));

            update.UpLimit.Should().Be(75 * 1024);
            update.DlLimit.Should().Be(140 * 1024);
            update.AltUpLimit.Should().Be(20 * 1024);
            update.AltDlLimit.Should().Be(45 * 1024);
            update.SchedulerEnabled.Should().BeFalse();
            update.ScheduleFromHour.Should().Be(2);
            update.ScheduleFromMin.Should().Be(30);
            update.SchedulerDays.Should().Be(3);

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_TimePickers_WHEN_Adjusted_THEN_ShouldUpdateToFields()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var scheduleFrom = FindTimePicker(target, "ScheduleFrom");
            await target.InvokeAsync(() => scheduleFrom.Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(4)));
            update.ScheduleFromHour.Should().Be(4);
            update.ScheduleFromMin.Should().BeNull();

            var scheduleTo = FindTimePicker(target, "ScheduleTo");
            await target.InvokeAsync(() => scheduleTo.Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(7.5)));
            update.ScheduleToHour.Should().Be(7);
            update.ScheduleToMin.Should().Be(30);
        }

        [Fact]
        public async Task GIVEN_RateLimitSwitches_WHEN_Toggled_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var utpSwitch = FindSwitch(target, "LimitUtpRate");
            await target.InvokeAsync(() => utpSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.LimitUtpRate.Should().BeFalse();

            var overheadSwitch = FindSwitch(target, "LimitTcpOverhead");
            await target.InvokeAsync(() => overheadSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.LimitTcpOverhead.Should().BeTrue();

            var lanSwitch = FindSwitch(target, "LimitLanPeers");
            await target.InvokeAsync(() => lanSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.LimitLanPeers.Should().BeFalse();

            events.Should().HaveCountGreaterThanOrEqualTo(3);
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_UnchangedScheduleTimes_WHEN_Reapplied_THEN_ShouldNotNotify()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await target.InvokeAsync(() => FindTimePicker(target, "ScheduleFrom").Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(1)));
            await target.InvokeAsync(() => FindTimePicker(target, "ScheduleTo").Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(5)));

            events.Should().BeEmpty();
            update.ScheduleFromHour.Should().BeNull();
            update.ScheduleToHour.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NullScheduleTimes_WHEN_Cleared_THEN_ShouldIgnoreChanges()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await target.InvokeAsync(() => FindTimePicker(target, "ScheduleFrom").Instance.TimeChanged.InvokeAsync(null));
            await target.InvokeAsync(() => FindTimePicker(target, "ScheduleTo").Instance.TimeChanged.InvokeAsync(null));

            update.ScheduleFromHour.Should().BeNull();
            update.ScheduleFromMin.Should().BeNull();
            update.ScheduleToHour.Should().BeNull();
            update.ScheduleToMin.Should().BeNull();
            events.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_FromTimeWithOnlyMinuteChange_WHEN_Updated_THEN_ShouldOnlyUpdateMinutes()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await target.InvokeAsync(() => FindTimePicker(target, "ScheduleFrom").Instance.TimeChanged.InvokeAsync(new TimeSpan(1, 30, 0)));

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(value => value.Should().BeSameAs(update));
            update.ScheduleFromHour.Should().BeNull();
            update.ScheduleFromMin.Should().Be(30);
        }

        [Fact]
        public async Task GIVEN_ToTimeWithOnlyHourChange_WHEN_Updated_THEN_ShouldOnlyUpdateHours()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await target.InvokeAsync(() => FindTimePicker(target, "ScheduleTo").Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(7)));

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(value => value.Should().BeSameAs(update));
            update.ScheduleToHour.Should().Be(7);
            update.ScheduleToMin.Should().BeNull();
        }

        [Fact]
        public void GIVEN_ValidationDelegates_WHEN_InvalidValuesProvided_THEN_ShouldReturnValidationMessages()
        {
            TestContext.Render<MudPopoverProvider>();
            var preferences = DeserializePreferences();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var upValidation = FindNumeric(target, "UpLimit").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            upValidation(-1).Should().Be("Global upload rate limit must be greater than 0 or disabled.");
            upValidation(0).Should().BeNull();

            var dlValidation = FindNumeric(target, "DlLimit").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            dlValidation(-1).Should().Be("Global download rate limit must be greater than 0 or disabled.");
            dlValidation(0).Should().BeNull();

            var altUpValidation = FindNumeric(target, "AltUpLimit").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            altUpValidation(-1).Should().Be("Alternative upload rate limit must be greater than 0 or disabled.");
            altUpValidation(0).Should().BeNull();

            var altDlValidation = FindNumeric(target, "AltDlLimit").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            altDlValidation(-1).Should().Be("Alternative download rate limit must be greater than 0 or disabled.");
            altDlValidation(0).Should().BeNull();
        }

        [Fact]
        public void GIVEN_NullPreferences_WHEN_Rendered_THEN_ShouldNotPopulateValuesOrThrow()
        {
            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindNumeric(target, "UpLimit").Instance.GetState(x => x.Value).Should().Be(0);
            FindSwitch(target, "SchedulerEnabled").Instance.Value.Should().BeNull();
            FindSelect<int>(target, "SchedulerDays").Instance.GetState(x => x.Value).Should().Be(0);
        }

        [Fact]
        public void GIVEN_RenderedSchedulerDaysSelect_WHEN_InspectingItems_THEN_AllDayOptionsArePresent()
        {
            TestContext.Render<MudPopoverProvider>();
            var preferences = DeserializePreferences();

            var target = TestContext.Render<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var values = target.FindComponents<MudSelectItem<int>>()
                .Select(item => item.Instance.Value)
                .ToList();

            values.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        private static Preferences DeserializePreferences()
        {
            const string json = """
            {
                "up_limit": 51200,
                "dl_limit": 122880,
                "alt_up_limit": 10240,
                "alt_dl_limit": 30720,
                "bittorrent_protocol": 2,
                "limit_utp_rate": true,
                "limit_tcp_overhead": false,
                "limit_lan_peers": true,
                "scheduler_enabled": true,
                "schedule_from_hour": 1,
                "schedule_from_min": 0,
                "schedule_to_hour": 5,
                "schedule_to_min": 0,
                "scheduler_days": 1
            }
            """;

            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<SpeedOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudTimePicker> FindTimePicker(IRenderedComponent<SpeedOptions> target, string testId)
        {
            return FindComponentByTestId<MudTimePicker>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<SpeedOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }
    }
}
