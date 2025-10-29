using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class RSSOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public RSSOptionsTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var switches = cut.FindComponents<FieldSwitch>();
            switches.Single(s => s.Instance.Label == "Enable fetching RSS feeds").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Enable auto downloading of RSS torrents").Instance.Value.Should().BeTrue();
            switches.Single(s => s.Instance.Label == "Download REPACK/PROPER episodes").Instance.Value.Should().BeFalse();

            var refreshField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Feeds refresh interval");
            refreshField.Instance.Value.Should().Be(30);

            var maxField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum number of articles per feed");
            maxField.Instance.Value.Should().Be(200);

            cut.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Filters")
                .Instance.Value.Should().Be("filter-one\nfilter-two");

            update.RssProcessingEnabled.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Settings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var switches = cut.FindComponents<FieldSwitch>();
            var processingSwitch = switches.Single(s => s.Instance.Label == "Enable fetching RSS feeds");
            await cut.InvokeAsync(() => processingSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.RssProcessingEnabled.Should().BeFalse();

            var refreshField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Feeds refresh interval");
            await cut.InvokeAsync(() => refreshField.Instance.ValueChanged.InvokeAsync(45));
            update.RssRefreshInterval.Should().Be(45);

            var maxField = cut.FindComponents<MudNumericField<int>>()
                .Single(f => f.Instance.Label == "Maximum number of articles per feed");
            await cut.InvokeAsync(() => maxField.Instance.ValueChanged.InvokeAsync(250));
            update.RssMaxArticlesPerFeed.Should().Be(250);

            var autoSwitch = switches.Single(s => s.Instance.Label == "Enable auto downloading of RSS torrents");
            await cut.InvokeAsync(() => autoSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.RssAutoDownloadingEnabled.Should().BeFalse();

            var repackSwitch = switches.Single(s => s.Instance.Label == "Download REPACK/PROPER episodes");
            await cut.InvokeAsync(() => repackSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.RssDownloadRepackProperEpisodes.Should().BeTrue();

            var filtersField = cut.FindComponents<MudTextField<string>>()
                .Single(tf => tf.Instance.Label == "Filters");
            await cut.InvokeAsync(() => filtersField.Instance.ValueChanged.InvokeAsync("updated"));
            update.RssSmartEpisodeFilters.Should().Be("updated");

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_EditRulesButton_WHEN_Clicked_THEN_ShouldOpenDialog()
        {
            var dialogMock = _target.AddSingletonMock<IDialogService>();
            dialogMock
                .Setup(s => s.ShowAsync<RssRulesDialog>(It.IsAny<string>(), It.IsAny<DialogOptions>()))
                .ReturnsAsync(Mock.Of<IDialogReference>());

            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<RSSOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var button = cut.FindAll("button")
                .Single(b => b.TextContent.Contains("Edit auto downloading rules", StringComparison.Ordinal));
            await cut.InvokeAsync(() => button.Click());

            dialogMock.Verify(s => s.ShowAsync<RssRulesDialog>("Edit Rss Auto Downloading Rules", DialogHelper.FullScreenDialogOptions), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FetchDelay_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var cut = _target.RenderComponent<TestableRssOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await cut.InvokeAsync(() => cut.Instance.InvokeFetchDelayChanged(90));

            cut.Instance.FetchDelay.Should().Be(90);
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
            _target.Dispose();
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
