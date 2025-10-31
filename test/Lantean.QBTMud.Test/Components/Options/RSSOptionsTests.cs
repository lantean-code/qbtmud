using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class RSSOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public RSSOptionsTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var switches = target.FindComponents<FieldSwitch>();
            switches.Single(s => s.Instance.Label == "Enable fetching RSS feeds").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Enable auto downloading of RSS torrents").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Download REPACK/PROPER episodes").Instance.Value.Should().BeFalse();

            var refreshField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Feeds refresh interval");
            refreshField.Instance.Value.Should().Be(30);

            var maxField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum number of articles per feed");
            maxField.Instance.Value.Should().Be(200);

            target.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Filters")
                .Instance.Value.Should().Be("filter-one\nfilter-two");

            update.RssProcessingEnabled.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Settings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var switches = target.FindComponents<FieldSwitch>();
            var processingSwitch = switches.Single(s => s.Instance.Label == "Enable fetching RSS feeds");
            await target.InvokeAsync(() => processingSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.RssProcessingEnabled.Should().BeFalse();

            var refreshField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Feeds refresh interval");
            await target.InvokeAsync(() => refreshField.Instance.ValueChanged.InvokeAsync(45));
            update.RssRefreshInterval.Should().Be(45);

            var maxField = target.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum number of articles per feed");
            await target.InvokeAsync(() => maxField.Instance.ValueChanged.InvokeAsync(250));
            update.RssMaxArticlesPerFeed.Should().Be(250);

            var autoSwitch = switches.Single(s => s.Instance.Label == "Enable auto downloading of RSS torrents");
            await target.InvokeAsync(() => autoSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.RssAutoDownloadingEnabled.Should().BeFalse();

            var repackSwitch = switches.Single(s => s.Instance.Label == "Download REPACK/PROPER episodes");
            await target.InvokeAsync(() => repackSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.RssDownloadRepackProperEpisodes.Should().BeTrue();

            var filtersField = target.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Filters");
            await target.InvokeAsync(() => filtersField.Instance.ValueChanged.InvokeAsync("updated"));
            update.RssSmartEpisodeFilters.Should().Be("updated");

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_EditRulesButton_WHEN_Clicked_THEN_ShouldOpenDialog()
        {
            var workflowMock = _context.AddSingletonMock<IDialogWorkflow>();
            workflowMock
                .Setup(w => w.InvokeRssRulesDialog())
                .Returns(Task.CompletedTask);

            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var button = target.FindAll("button")
                .Single(b => b.TextContent.Contains("Edit auto downloading rules", StringComparison.Ordinal));
            await target.InvokeAsync(() => button.Click());

            workflowMock.Verify(w => w.InvokeRssRulesDialog(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FetchDelay_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<TestableRssOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await target.InvokeAsync(() => target.Instance.InvokeFetchDelayChanged(90));

            target.Instance.FetchDelay.Should().Be(90);
            update.RssFetchDelay.Should().Be(90);
            events.Should().ContainSingle().Which.Should().BeSameAs(update);
        }

        private static Preferences DeserializePreferences()
        {
            const string json = """
            {
                "rss_processing_enabled": true,
                "rss_refresh_interval": 30,
                "rss_fetch_delay": 5,
                "rss_max_articles_per_feed": 200,
                "rss_auto_downloading_enabled": true,
                "rss_download_repack_proper_episodes": false,
                "rss_smart_episode_filters": "filter-one\nfilter-two"
            }
            """;

            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TestableRssOptions : RSSOptions
        {
            public long FetchDelay
            {
                get
                {
                    return RssFetchDelay;
                }
            }

            public Task InvokeFetchDelayChanged(int value)
            {
                return RssFetchDelayChanged(value);
            }
        }
    }
}