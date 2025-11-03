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
    public sealed class SpeedOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public SpeedOptionsTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldDisplayRatesAndScheduler()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<SpeedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindNumeric(target, "UpLimit").Instance.Value.Should().Be(50);
            FindNumeric(target, "DlLimit").Instance.Value.Should().Be(120);
            FindNumeric(target, "AltUpLimit").Instance.Value.Should().Be(10);
            FindNumeric(target, "AltDlLimit").Instance.Value.Should().Be(30);

            FindSwitch(target, "SchedulerEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "LimitUtpRate").Instance.Value.Should().BeTrue();
            FindSwitch(target, "LimitTcpOverhead").Instance.Value.Should().BeFalse();
            FindSwitch(target, "LimitLanPeers").Instance.Value.Should().BeTrue();

            FindTimePicker(target, "ScheduleFrom").Instance.Time.Should().Be(TimeSpan.FromHours(1));
            FindTimePicker(target, "ScheduleTo").Instance.Time.Should().Be(TimeSpan.FromHours(5));

            FindSelect<int>(target, "SchedulerDays").Instance.Value.Should().Be(1);

            update.UpLimit.Should().BeNull();
            update.SchedulerEnabled.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_UserAdjustments_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<SpeedOptions>(parameters =>
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
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<SpeedOptions>(parameters =>
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
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<SpeedOptions>(parameters =>
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
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<SpeedOptions>(parameters =>
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
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<SpeedOptions>(parameters =>
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
            _context.Dispose();
        }

        private static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<SpeedOptions> target, string testId)
            where TComponent : IComponent
        {
            return target.FindComponents<TComponent>()
                .Single(component => component.FindAll($"[data-test-id='{testId}']").Count > 0);
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<SpeedOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<FieldSwitch> FindSwitch(IRenderedComponent<SpeedOptions> target, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(target, testId);
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
