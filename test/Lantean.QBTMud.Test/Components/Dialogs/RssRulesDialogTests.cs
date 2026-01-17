using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class RssRulesDialogTests : RazorComponentTestBase<RssRulesDialog>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly RssRulesDialogTestDriver _target;

        public RssRulesDialogTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_dialogWorkflow);

            _target = new RssRulesDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoSelection_WHEN_SaveClicked_THEN_DoesNotCallApi()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());

            var dialog = await _target.RenderDialogAsync();

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "RssRulesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_apiClient).Verify(client => client.SetRssAutoDownloadingRule(It.IsAny<string>(), It.IsAny<AutoDownloadingRule>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoSelection_WHEN_RemoveClicked_THEN_DoesNotCallApi()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());

            var dialog = await _target.RenderDialogAsync();

            var removeButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesRemove");
            await dialog.Component.InvokeAsync(() => removeButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            Mock.Get(_apiClient).Verify(client => client.RemoveRssAutoDownloadingRule(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoSelection_WHEN_CloseClicked_THEN_ResultCanceled()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());

            var dialog = await _target.RenderDialogAsync();

            var closeButton = FindComponentByTestId<MudButton>(dialog.Component, "RssRulesClose");
            await closeButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_AddRuleCanceled_WHEN_Clicked_THEN_NoRuleAdded()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var dialog = await _target.RenderDialogAsync();

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            Action action = () => FindComponentByTestId<MudListItem<string>>(dialog.Component, "RssRule-NewRule");
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_AddRuleExisting_WHEN_Clicked_THEN_RemoveEnabled()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>
            {
                { "RuleA", CreateRule(enabled: true) },
            });
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("RuleA");

            var dialog = await _target.RenderDialogAsync();

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var removeButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesRemove");
            removeButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_UnsavedRule_WHEN_Removed_THEN_DoesNotCallApi()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("NewRule");

            var dialog = await _target.RenderDialogAsync();

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var newRuleItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "RssRule-NewRule");
            await newRuleItem.Find("div").ClickAsync(new MouseEventArgs());

            var removeButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesRemove");
            await removeButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_apiClient).Verify(client => client.RemoveRssAutoDownloadingRule(It.IsAny<string>()), Times.Never);

            Action action = () => FindComponentByTestId<MudListItem<string>>(dialog.Component, "RssRule-NewRule");
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_SavedRule_WHEN_Removed_THEN_CallsApi()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>
            {
                { "RuleA", CreateRule(enabled: false) },
            });
            Mock.Get(_apiClient)
                .Setup(client => client.RemoveRssAutoDownloadingRule("RuleA"))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var ruleItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "RssRule-RuleA");
            await ruleItem.Find("div").ClickAsync(new MouseEventArgs());

            var removeButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesRemove");
            await removeButton.Find("button").ClickAsync(new MouseEventArgs());

            Mock.Get(_apiClient).Verify(client => client.RemoveRssAutoDownloadingRule("RuleA"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_InvalidSelection_WHEN_SelectedValueChanged_THEN_DoesNotCallApi()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());

            var dialog = await _target.RenderDialogAsync();

            var list = FindComponentByTestId<MudList<string>>(dialog.Component, "RssRulesList");
            await dialog.Component.InvokeAsync(() => list.Instance.SelectedValueChanged.InvokeAsync("MissingRule"));

            Mock.Get(_apiClient).Verify(client => client.GetRssMatchingArticles(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RulesWithOptions_WHEN_Selected_THEN_UpdatesFields()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>
            {
                { "RuleDefault", CreateRule(enabled: true, stopped: null, contentLayout: "Default", savePath: "C:/Downloads", affectedFeeds: new[] { "http://feed-a" }) },
                { "RuleAlways", CreateRule(enabled: true, stopped: true, contentLayout: "Original", savePath: "", affectedFeeds: Array.Empty<string>()) },
                { "RuleNever", CreateRule(enabled: true, stopped: false, contentLayout: "Subfolder", savePath: "", affectedFeeds: Array.Empty<string>()) },
                { "RuleNoSubfolder", CreateRule(enabled: true, stopped: null, contentLayout: "NoSubfolder", savePath: "", affectedFeeds: Array.Empty<string>()) },
            });

            var dialog = await _target.RenderDialogAsync();

            var list = FindComponentByTestId<MudList<string>>(dialog.Component, "RssRulesList");
            var addStopped = FindComponentByTestId<MudSelect<string>>(dialog.Component, "RssRulesAddStopped");
            var contentLayout = FindComponentByTestId<MudSelect<string>>(dialog.Component, "RssRulesContentLayout");
            var feeds = FindComponentByTestId<MudList<string>>(dialog.Component, "RssRulesFeeds");
            var saveToField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "RssRulesSaveTo");

            await dialog.Component.InvokeAsync(() => list.Instance.SelectedValueChanged.InvokeAsync("RuleDefault"));
            addStopped.Instance.Value.Should().Be("default");
            contentLayout.Instance.Value.Should().BeNull();
            feeds.Instance.SelectedValues.Should().Contain("FeedA");
            saveToField.Instance.Value.Should().Be("C:/Downloads");

            await dialog.Component.InvokeAsync(() => list.Instance.SelectedValueChanged.InvokeAsync("RuleAlways"));
            addStopped.Instance.Value.Should().Be("always");
            contentLayout.Instance.Value.Should().Be("Original");

            await dialog.Component.InvokeAsync(() => list.Instance.SelectedValueChanged.InvokeAsync("RuleNever"));
            addStopped.Instance.Value.Should().Be("never");
            contentLayout.Instance.Value.Should().Be("Subfolder");

            await dialog.Component.InvokeAsync(() => list.Instance.SelectedValueChanged.InvokeAsync("RuleNoSubfolder"));
            addStopped.Instance.Value.Should().Be("default");
            contentLayout.Instance.Value.Should().Be("NoSubfolder");
        }

        [Fact]
        public async Task GIVEN_SavePathChanged_WHEN_Saved_THEN_UpdatesSavePath()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("RuleA");

            AutoDownloadingRule? capturedRule = null;
            Mock.Get(_apiClient)
                .Setup(client => client.SetRssAutoDownloadingRule("RuleA", It.IsAny<AutoDownloadingRule>()))
                .Callback<string, AutoDownloadingRule>((_, rule) => capturedRule = rule)
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var ruleItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "RssRule-RuleA");
            await ruleItem.Find("div").ClickAsync(new MouseEventArgs());

            var saveToDiff = FindComponentByTestId<FieldSwitch>(dialog.Component, "RssRulesSaveToDifferentDirectory");
            await dialog.Component.InvokeAsync(() => saveToDiff.Instance.ValueChanged.InvokeAsync(true));

            var saveTo = FindComponentByTestId<MudTextField<string>>(dialog.Component, "RssRulesSaveTo");
            await dialog.Component.InvokeAsync(() => saveTo.Instance.ValueChanged.InvokeAsync("C:/Downloads/RuleA"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "RssRulesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            capturedRule.Should().NotBeNull();
            capturedRule!.TorrentParams.SavePath.Should().Be("C:/Downloads/RuleA");
        }

        [Fact]
        public async Task GIVEN_RuleSelected_WHEN_FieldsChangedAndSaved_THEN_UpdatesRule()
        {
            SetupApiClient(new Dictionary<string, AutoDownloadingRule>());
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("RuleA");

            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetRssMatchingArticles("RuleA"))
                .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>>
                {
                    { "FeedA", new List<string> { "ArticleA" } },
                });

            AutoDownloadingRule? capturedRule = null;
            apiClientMock
                .Setup(client => client.SetRssAutoDownloadingRule("RuleA", It.IsAny<AutoDownloadingRule>()))
                .Callback<string, AutoDownloadingRule>((_, rule) => capturedRule = rule)
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RssRulesAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var ruleItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "RssRule-RuleA");
            await ruleItem.Find("div").ClickAsync(new MouseEventArgs());

            var useRegex = FindComponentByTestId<FieldSwitch>(dialog.Component, "RssRulesUseRegex");
            await dialog.Component.InvokeAsync(() => useRegex.Instance.ValueChanged.InvokeAsync(true));

            var mustContain = FindComponentByTestId<MudTextField<string>>(dialog.Component, "RssRulesMustContain");
            await dialog.Component.InvokeAsync(() => mustContain.Instance.ValueChanged.InvokeAsync("MustContain"));

            var mustNotContain = FindComponentByTestId<MudTextField<string>>(dialog.Component, "RssRulesMustNotContain");
            await dialog.Component.InvokeAsync(() => mustNotContain.Instance.ValueChanged.InvokeAsync("MustNotContain"));

            var episodeFilter = FindComponentByTestId<MudTextField<string>>(dialog.Component, "RssRulesEpisodeFilter");
            await dialog.Component.InvokeAsync(() => episodeFilter.Instance.ValueChanged.InvokeAsync("EpisodeFilter"));

            var smartFilter = FindComponentByTestId<FieldSwitch>(dialog.Component, "RssRulesSmartFilter");
            await dialog.Component.InvokeAsync(() => smartFilter.Instance.ValueChanged.InvokeAsync(true));

            var categorySelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "RssRulesCategory");
            await dialog.Component.InvokeAsync(() => categorySelect.Instance.ValueChanged.InvokeAsync("CatA"));

            var tagsField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "RssRulesTags");
            await dialog.Component.InvokeAsync(() => tagsField.Instance.ValueChanged.InvokeAsync("alpha, beta, , gamma"));

            var saveToDiff = FindComponentByTestId<FieldSwitch>(dialog.Component, "RssRulesSaveToDifferentDirectory");
            await dialog.Component.InvokeAsync(() => saveToDiff.Instance.ValueChanged.InvokeAsync(true));
            await dialog.Component.InvokeAsync(() => saveToDiff.Instance.ValueChanged.InvokeAsync(false));

            var ignoreDays = FindComponentByTestId<MudNumericField<int>>(dialog.Component, "RssRulesIgnoreDays");
            await dialog.Component.InvokeAsync(() => ignoreDays.Instance.ValueChanged.InvokeAsync(3));

            var addStopped = FindComponentByTestId<MudSelect<string>>(dialog.Component, "RssRulesAddStopped");
            await dialog.Component.InvokeAsync(() => addStopped.Instance.ValueChanged.InvokeAsync("default"));
            await dialog.Component.InvokeAsync(() => addStopped.Instance.ValueChanged.InvokeAsync("always"));
            await dialog.Component.InvokeAsync(() => addStopped.Instance.ValueChanged.InvokeAsync("never"));

            var contentLayout = FindComponentByTestId<MudSelect<string>>(dialog.Component, "RssRulesContentLayout");
            await dialog.Component.InvokeAsync(() => contentLayout.Instance.ValueChanged.InvokeAsync("Default"));
            await dialog.Component.InvokeAsync(() => contentLayout.Instance.ValueChanged.InvokeAsync("Original"));
            await dialog.Component.InvokeAsync(() => contentLayout.Instance.ValueChanged.InvokeAsync("Subfolder"));
            await dialog.Component.InvokeAsync(() => contentLayout.Instance.ValueChanged.InvokeAsync("NoSubfolder"));

            var feedsList = FindComponentByTestId<MudList<string>>(dialog.Component, "RssRulesFeeds");
            await dialog.Component.InvokeAsync(() => feedsList.Instance.SelectedValuesChanged.InvokeAsync(new List<string> { "FeedA" }));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "RssRulesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            capturedRule.Should().NotBeNull();
            capturedRule!.UseRegex.Should().BeTrue();
            capturedRule.MustContain.Should().Be("MustContain");
            capturedRule.MustNotContain.Should().Be("MustNotContain");
            capturedRule.EpisodeFilter.Should().Be("EpisodeFilter");
            capturedRule.SmartFilter.Should().BeTrue();
            capturedRule.IgnoreDays.Should().Be(3);
            capturedRule.TorrentParams.Category.Should().Be("CatA");
            capturedRule.TorrentParams.Tags.Should().BeEquivalentTo(new[] { "alpha", "beta", "gamma" });
            capturedRule.TorrentParams.SavePath.Should().BeEmpty();
            capturedRule.TorrentParams.Stopped.Should().BeFalse();
            capturedRule.TorrentParams.ContentLayout.Should().Be("NoSubfolder");
            capturedRule.AffectedFeeds.Should().ContainSingle(value => value == "http://feed-a");

            dialog.Component.Markup.Should().Contain("ArticleA");
        }

        private void SetupApiClient(Dictionary<string, AutoDownloadingRule> rules)
        {
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllRssAutoDownloadingRules())
                .ReturnsAsync(rules);
            apiClientMock
                .Setup(client => client.GetRssMatchingArticles(It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>>());
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "CatA", new Category("CatA", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetAllRssItems(false))
                .ReturnsAsync(new Dictionary<string, RssItem>
                {
                    { "FeedA", CreateFeed("FeedA", "http://feed-a") },
                });
        }

        private static AutoDownloadingRule CreateRule(bool? enabled)
        {
            return CreateRule(enabled, null, null, null, null);
        }

        private static AutoDownloadingRule CreateRule(
            bool? enabled,
            bool? stopped,
            string? contentLayout,
            string? savePath,
            IReadOnlyList<string>? affectedFeeds)
        {
            return new AutoDownloadingRule
            {
                Enabled = enabled,
                AffectedFeeds = affectedFeeds ?? Array.Empty<string>(),
                TorrentParams = new TorrentParams
                {
                    Stopped = stopped,
                    ContentLayout = contentLayout,
                    SavePath = savePath ?? string.Empty,
                },
            };
        }

        private static RssItem CreateFeed(string title, string url)
        {
            return new RssItem(null, false, false, null, title, title, url);
        }
    }

    internal sealed class RssRulesDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public RssRulesDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<RssRulesDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var reference = await dialogService.ShowAsync<RssRulesDialog>("Rss Rules");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<RssRulesDialog>();

            return new RssRulesDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class RssRulesDialogRenderContext
    {
        public RssRulesDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<RssRulesDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<RssRulesDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
