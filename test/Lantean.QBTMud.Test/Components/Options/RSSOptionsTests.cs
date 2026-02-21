using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moq;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class RSSOptionsTests : RazorComponentTestBase<RSSOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindSwitch(target, "RssProcessingEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "RssAutoDownloadingEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "RssDownloadRepackProperEpisodes").Instance.Value.Should().BeFalse();

            FindNumeric(target, "RssRefreshInterval").Instance.GetState(x => x.Value).Should().Be(30);
            FindNumeric(target, "RssMaxArticlesPerFeed").Instance.GetState(x => x.Value).Should().Be(200);

            FindTextField(target, "RssSmartEpisodeFilters").Instance.GetState(x => x.Value).Should().Be("filter-one\nfilter-two");

            update.RssProcessingEnabled.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Settings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var processingSwitch = FindSwitch(target, "RssProcessingEnabled");
            await target.InvokeAsync(() => processingSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.RssProcessingEnabled.Should().BeFalse();

            var refreshField = FindNumeric(target, "RssRefreshInterval");
            await target.InvokeAsync(() => refreshField.Instance.ValueChanged.InvokeAsync(45));
            update.RssRefreshInterval.Should().Be(45);

            var maxField = FindNumeric(target, "RssMaxArticlesPerFeed");
            await target.InvokeAsync(() => maxField.Instance.ValueChanged.InvokeAsync(250));
            update.RssMaxArticlesPerFeed.Should().Be(250);

            var autoSwitch = FindSwitch(target, "RssAutoDownloadingEnabled");
            await target.InvokeAsync(() => autoSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.RssAutoDownloadingEnabled.Should().BeFalse();

            var repackSwitch = FindSwitch(target, "RssDownloadRepackProperEpisodes");
            await target.InvokeAsync(() => repackSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.RssDownloadRepackProperEpisodes.Should().BeTrue();

            var filtersField = FindTextField(target, "RssSmartEpisodeFilters");
            await target.InvokeAsync(() => filtersField.Instance.ValueChanged.InvokeAsync("updated"));
            update.RssSmartEpisodeFilters.Should().Be("updated");

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_EditRulesButton_WHEN_Clicked_THEN_ShouldOpenDialog()
        {
            var workflowMock = TestContext.AddSingletonMock<IDialogWorkflow>();
            workflowMock
                .Setup(w => w.InvokeRssRulesDialog())
                .Returns(Task.CompletedTask);

            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = TestContext.Render<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var button = FindComponentByTestId<MudButton>(target, "EditRssRules");
            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            workflowMock.Verify(w => w.InvokeRssRulesDialog(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FetchDelay_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            TestContext.Render<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = TestContext.Render<TestableRssOptions>(parameters =>
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

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<RSSOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<RSSOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
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
