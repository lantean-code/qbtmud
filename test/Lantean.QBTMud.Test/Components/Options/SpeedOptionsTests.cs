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
    public sealed class SpeedOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public SpeedOptionsTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldDisplayRatesAndScheduler()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var numericFields = cut.FindComponents<MudNumericField<int>>();
            numericFields[0].Instance.Value.Should().Be(50);
            numericFields[1].Instance.Value.Should().Be(120);
            numericFields[2].Instance.Value.Should().Be(10);
            numericFields[3].Instance.Value.Should().Be(30);

            var switches = cut.FindComponents<FieldSwitch>();
            switches.Single(s => s.Instance.Label == "Schedule the use of alternative rate limits").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Apply rate limit to µTP protocol").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Apply rate limit to transport overhead").Instance.Value.Should().BeFalse();
            switches.Single(s => s.Instance.Label == "Apply rate limit to peers on LAN").Instance.Value.Should().BeTrue();

            var timePickers = cut.FindComponents<MudTimePicker>();
            timePickers[0].Instance.Time.Should().Be(TimeSpan.FromHours(1));
            timePickers[1].Instance.Time.Should().Be(TimeSpan.FromHours(5));

            cut.FindComponent<MudSelect<int>>().Instance.Value.Should().Be(1);

            update.UpLimit.Should().BeNull();
            update.SchedulerEnabled.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_UserAdjustments_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var numericFields = cut.FindComponents<MudNumericField<int>>();
            await cut.InvokeAsync(() => numericFields[0].Instance.ValueChanged.InvokeAsync(75));
            await cut.InvokeAsync(() => numericFields[1].Instance.ValueChanged.InvokeAsync(140));
            await cut.InvokeAsync(() => numericFields[2].Instance.ValueChanged.InvokeAsync(20));
            await cut.InvokeAsync(() => numericFields[3].Instance.ValueChanged.InvokeAsync(45));

            var schedulerSwitch = cut.FindComponents<FieldSwitch>()
                .Single(s => s.Instance.Label == "Schedule the use of alternative rate limits");
            await cut.InvokeAsync(() => schedulerSwitch.Instance.ValueChanged.InvokeAsync(false));

            var scheduleFrom = cut.FindComponents<MudTimePicker>()[0];
            await cut.InvokeAsync(() => scheduleFrom.Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(2.5)));

            var daysSelect = cut.FindComponent<MudSelect<int>>();
            await cut.InvokeAsync(() => daysSelect.Instance.ValueChanged.InvokeAsync(3));

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
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var timePickers = cut.FindComponents<MudTimePicker>();

            await cut.InvokeAsync(() => timePickers[0].Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(4)));
            update.ScheduleFromHour.Should().Be(4);
            update.ScheduleFromMin.Should().BeNull();

            await cut.InvokeAsync(() => timePickers[1].Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(7.5)));
            update.ScheduleToHour.Should().Be(7);
            update.ScheduleToMin.Should().Be(30);
        }

        [Fact]
        public async Task GIVEN_RateLimitSwitches_WHEN_Toggled_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var switches = cut.FindComponents<FieldSwitch>();

            var utpSwitch = switches.Single(s => s.Instance.Label == "Apply rate limit to µTP protocol");
            await cut.InvokeAsync(() => utpSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.LimitUtpRate.Should().BeFalse();

            var overheadSwitch = switches.Single(s => s.Instance.Label == "Apply rate limit to transport overhead");
            await cut.InvokeAsync(() => overheadSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.LimitTcpOverhead.Should().BeTrue();

            var lanSwitch = switches.Single(s => s.Instance.Label == "Apply rate limit to peers on LAN");
            await cut.InvokeAsync(() => lanSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.LimitLanPeers.Should().BeFalse();

            events.Should().HaveCountGreaterThanOrEqualTo(3);
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_UnchangedScheduleTimes_WHEN_Reapplied_THEN_ShouldNotNotify()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var timePickers = cut.FindComponents<MudTimePicker>();

            await cut.InvokeAsync(() => timePickers[0].Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(1)));
            await cut.InvokeAsync(() => timePickers[1].Instance.TimeChanged.InvokeAsync(TimeSpan.FromHours(5)));

            events.Should().BeEmpty();
            update.ScheduleFromHour.Should().BeNull();
            update.ScheduleToHour.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NullScheduleTimes_WHEN_Cleared_THEN_ShouldIgnoreChanges()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var timePickers = cut.FindComponents<MudTimePicker>();

            await cut.InvokeAsync(() => timePickers[0].Instance.TimeChanged.InvokeAsync(null));
            await cut.InvokeAsync(() => timePickers[1].Instance.TimeChanged.InvokeAsync(null));

            update.ScheduleFromHour.Should().BeNull();
            update.ScheduleFromMin.Should().BeNull();
            update.ScheduleToHour.Should().BeNull();
            update.ScheduleToMin.Should().BeNull();
            events.Should().BeEmpty();
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

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
