using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Moq;
using MudBlazor;
using System.Globalization;
using System.Text.Json;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class AddTorrentOptionsTests : RazorComponentTestBase<AddTorrentOptions>
    {
        private readonly AddTorrentOptionsTestDriver _target;

        public AddTorrentOptionsTests()
        {
            _target = new AddTorrentOptionsTestDriver(TestContext);
        }

        [Fact]
        public void GIVEN_ShowCookieOptionTrue_WHEN_Rendered_THEN_CookieFieldVisible()
        {
            UseApiClientMock();

            var component = _target.RenderComponent(showCookieOption: true);
            ExpandOptions(component);

            component.FindComponents<MudTextField<string>>()
                .Any(field => HasTestId(field, "Cookie"))
                .Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ShowCookieOptionFalse_WHEN_Rendered_THEN_CookieFieldHidden()
        {
            UseApiClientMock();

            var component = _target.RenderComponent(showCookieOption: false);
            ExpandOptions(component);

            component.FindComponents<MudTextField<string>>()
                .Any(field => HasTestId(field, "Cookie"))
                .Should().BeFalse();
            FindSelect<string>(component, "Tags").Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_PreferencesLoaded_WHEN_Rendered_THEN_StateInitialized()
        {
            var categories = new Dictionary<string, ClientModels.Category>
            {
                { "beta", CreateCategory("beta", "SavePath", null) },
                { "Alpha", CreateCategory("Alpha", "SavePath", null) },
            };
            var tags = new[] { "beta", "Alpha" };
            var preferences = CreatePreferences(
                autoTmmEnabled: false,
                savePath: "SavePath",
                tempPath: "TempPath",
                tempPathEnabled: true,
                addStoppedEnabled: true,
                addToTopOfQueue: false,
                stopCondition: "StopCondition",
                contentLayout: "ContentLayout",
                maxRatioEnabled: true,
                maxRatio: 1.5f,
                maxSeedingTimeEnabled: true,
                maxSeedingTime: 60,
                maxInactiveSeedingTimeEnabled: true,
                maxInactiveSeedingTime: 120,
                maxRatioAct: 2);

            UseApiClientMock(categories: categories, tags: tags, preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            FindSelect<bool>(component, "TorrentManagementMode").Instance.Value.Should().BeFalse();
            GetSavePath(component).Should().Be("SavePath");
            GetUseDownloadPath(component).Should().BeTrue();
            GetDownloadPath(component).Should().Be("TempPath");
            FindFieldSwitch(component, "StartTorrent").Instance.Value.Should().BeFalse();
            FindFieldSwitch(component, "AddToTopOfQueue").Instance.Value.Should().BeFalse();
            FindSelect<string>(component, "StopCondition").Instance.Value.Should().Be("StopCondition");
            FindSelect<string>(component, "ContentLayout").Instance.Value.Should().Be("ContentLayout");
            FindFieldSwitch(component, "RatioLimitEnabled").Instance.Value.Should().BeTrue();
            FindNumericField<float>(component, "RatioLimit").Instance.Value.Should().Be(1.5f);
            FindFieldSwitch(component, "SeedingTimeLimitEnabled").Instance.Value.Should().BeTrue();
            FindNumericField<int>(component, "SeedingTimeLimit").Instance.Value.Should().Be(60);
            FindFieldSwitch(component, "InactiveSeedingTimeLimitEnabled").Instance.Value.Should().BeTrue();
            FindNumericField<int>(component, "InactiveSeedingTimeLimit").Instance.Value.Should().Be(120);
            FindSelect<string>(component, "Tags").Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_PreferencesWithNullPaths_WHEN_Rendered_THEN_DefaultPathsEmpty()
        {
            var preferences = CreatePreferences(
                autoTmmEnabled: false,
                savePath: null,
                tempPath: null,
                tempPathEnabled: true);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            GetSavePath(component).Should().BeEmpty();
            GetUseDownloadPath(component).Should().BeTrue();
            GetDownloadPath(component).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_TorrentManagementEnabled_WHEN_Rendered_THEN_AutomaticPathsApplied()
        {
            var preferences = CreatePreferences(
                autoTmmEnabled: true,
                savePath: "SavePath",
                tempPath: "TempPath",
                tempPathEnabled: true);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            FindSelect<bool>(component, "TorrentManagementMode").Instance.Value.Should().BeTrue();
            GetSavePath(component).Should().Be("SavePath");
            GetUseDownloadPath(component).Should().BeTrue();
            GetDownloadPath(component).Should().Be("TempPath");

            var modeSelect = FindSelect<bool>(component, "TorrentManagementMode");
            await component.InvokeAsync(() => modeSelect.Instance.ValueChanged.InvokeAsync(true));
            GetSavePath(component).Should().Be("SavePath");

            await SetFieldSwitchValue(component, "UseIncompleteSavePath", false);
            GetUseDownloadPath(component).Should().BeTrue();

            var options = component.Instance.GetTorrentOptions();
            options.UseDownloadPath.Should().BeNull();
            options.DownloadPath.Should().BeNull();
            options.Tags.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_DefaultDownloadPathEmpty_WHEN_ReEnablingDownloadPath_THEN_UsesEmptyDefault()
        {
            var preferences = CreatePreferences(
                autoTmmEnabled: false,
                tempPath: string.Empty,
                tempPathEnabled: true);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetFieldSwitchValue(component, "UseIncompleteSavePath", false);
            await SetFieldSwitchValue(component, "UseIncompleteSavePath", true);

            GetDownloadPath(component).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_AutomaticModeAndCategories_WHEN_CategoryChanged_THEN_PathsResolved()
        {
            var categories = new Dictionary<string, ClientModels.Category>
            {
                { "CategorySave", CreateCategory("CategorySave", "CategorySavePath", null) },
                { "CategoryCombine", CreateCategory("CategoryCombine", null, null) },
                { "CategoryDownloadDisabled", CreateCategory("CategoryDownloadDisabled", null, new ClientModels.DownloadPathOption(false, "DownloadPath")) },
                { "CategoryDownloadValue", CreateCategory("CategoryDownloadValue", null, new ClientModels.DownloadPathOption(true, "DownloadPathValue")) },
                { "CategoryDownloadEmpty", CreateCategory("CategoryDownloadEmpty", null, new ClientModels.DownloadPathOption(true, string.Empty)) },
            };
            var preferences = CreatePreferences(
                autoTmmEnabled: true,
                savePath: "SavePath",
                tempPath: "DownloadPath",
                tempPathEnabled: true);

            UseApiClientMock(categories: categories, preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetSelectValue(component, "Category", "CategorySave");
            GetSavePath(component).Should().Be("CategorySavePath");
            GetUseDownloadPath(component).Should().BeTrue();
            GetDownloadPath(component).Should().Be(Path.Combine("DownloadPath", "CategorySave"));

            await SetSelectValue(component, "Category", "CategoryCombine");
            GetSavePath(component).Should().Be(Path.Combine("SavePath", "CategoryCombine"));
            GetDownloadPath(component).Should().Be(Path.Combine("DownloadPath", "CategoryCombine"));

            await SetSelectValue(component, "Category", "CategoryDownloadDisabled");
            GetUseDownloadPath(component).Should().BeFalse();
            GetDownloadPath(component).Should().BeEmpty();

            await SetSelectValue(component, "Category", "CategoryDownloadValue");
            GetUseDownloadPath(component).Should().BeTrue();
            GetDownloadPath(component).Should().Be("DownloadPathValue");

            await SetSelectValue(component, "Category", "CategoryDownloadEmpty");
            GetUseDownloadPath(component).Should().BeTrue();
            GetDownloadPath(component).Should().Be(Path.Combine("DownloadPath", "CategoryDownloadEmpty"));
        }

        [Fact]
        public async Task GIVEN_DefaultDownloadPathDisabled_WHEN_CategoryChanged_THEN_DownloadPathDisabled()
        {
            var categories = new Dictionary<string, ClientModels.Category>
            {
                { "Category", CreateCategory("Category", null, null) },
            };
            var preferences = CreatePreferences(
                autoTmmEnabled: true,
                savePath: "SavePath",
                tempPath: "TempPath",
                tempPathEnabled: false);

            UseApiClientMock(categories: categories, preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetSelectValue(component, "Category", "Category");
            GetUseDownloadPath(component).Should().BeFalse();
            GetDownloadPath(component).Should().BeEmpty();

            await SetSelectValue(component, "Category", string.Empty);
            GetUseDownloadPath(component).Should().BeFalse();
            GetDownloadPath(component).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_MissingCategory_WHEN_AutomaticModeCategoryChanged_THEN_DefaultPathsUsed()
        {
            var preferences = CreatePreferences(
                autoTmmEnabled: true,
                savePath: "SavePath",
                tempPath: "TempPath",
                tempPathEnabled: true);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetSelectValue(component, "Category", "Missing");

            GetSavePath(component).Should().Be("SavePath");
            GetDownloadPath(component).Should().Be("TempPath");
        }

        [Fact]
        public async Task GIVEN_DefaultSavePathEmpty_WHEN_AutomaticModeCategoryChanged_THEN_SavePathEmpty()
        {
            var categories = new Dictionary<string, ClientModels.Category>
            {
                { "Category", CreateCategory("Category", null, null) },
            };
            var preferences = CreatePreferences(
                autoTmmEnabled: true,
                savePath: string.Empty,
                tempPath: string.Empty,
                tempPathEnabled: true);

            UseApiClientMock(categories: categories, preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetSelectValue(component, "Category", "Category");

            GetSavePath(component).Should().BeEmpty();
            GetDownloadPath(component).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ManualMode_WHEN_DownloadPathToggled_THEN_ManualPathsUpdated()
        {
            var preferences = CreatePreferences(
                autoTmmEnabled: false,
                savePath: "SavePath",
                tempPath: "TempPath",
                tempPathEnabled: true);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetTextFieldValue(component, "SaveFilesLocation", "ManualSavePath");
            GetSavePath(component).Should().Be("ManualSavePath");

            await SetTextFieldValue(component, "IncompleteSavePath", string.Empty);
            await SetFieldSwitchValue(component, "UseIncompleteSavePath", false);
            GetUseDownloadPath(component).Should().BeFalse();
            GetDownloadPath(component).Should().BeEmpty();

            await SetFieldSwitchValue(component, "UseIncompleteSavePath", true);
            GetUseDownloadPath(component).Should().BeTrue();
            GetDownloadPath(component).Should().Be("TempPath");
        }

        [Fact]
        public async Task GIVEN_ManualModeWithDownloadPathDisabled_WHEN_DownloadPathChanged_THEN_ManualPathNotUpdated()
        {
            var preferences = CreatePreferences(
                autoTmmEnabled: false,
                tempPath: "TempPath",
                tempPathEnabled: true);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetTextFieldValue(component, "IncompleteSavePath", "ManualDownloadPath");
            await SetFieldSwitchValue(component, "UseIncompleteSavePath", false);

            await SetTextFieldValue(component, "IncompleteSavePath", "UpdatedPath");
            await SetFieldSwitchValue(component, "UseIncompleteSavePath", true);

            GetDownloadPath(component).Should().Be("ManualDownloadPath");
        }

        [Fact]
        public async Task GIVEN_TorrentManagementModeToggled_WHEN_SetTorrentManagementModeInvoked_THEN_RestoresManualPaths()
        {
            var categories = new Dictionary<string, ClientModels.Category>
            {
                { "Category", CreateCategory("Category", "CategorySavePath", new ClientModels.DownloadPathOption(true, "DownloadPathValue")) },
            };
            var preferences = CreatePreferences(
                autoTmmEnabled: false,
                savePath: "SavePath",
                tempPath: "TempPath",
                tempPathEnabled: true);

            UseApiClientMock(categories: categories, preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetTextFieldValue(component, "SaveFilesLocation", "ManualSavePath");
            await SetTextFieldValue(component, "IncompleteSavePath", "ManualDownloadPath");

            await SetSelectValue(component, "TorrentManagementMode", true);
            await SetSelectValue(component, "Category", "Category");
            await SetTextFieldValue(component, "SaveFilesLocation", "AutoPath");

            await SetSelectValue(component, "TorrentManagementMode", false);
            GetSavePath(component).Should().Be("ManualSavePath");
            GetDownloadPath(component).Should().Be("ManualDownloadPath");
        }

        [Fact]
        public async Task GIVEN_CategoryAndTagsSelected_WHEN_GetTorrentOptionsInvoked_THEN_SelectionsCaptured()
        {
            var preferences = CreatePreferences(
                autoTmmEnabled: false,
                tempPath: "TempPath",
                tempPathEnabled: true);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetSelectValue(component, "Category", "Category");
            await SetSelectedTags(component, new[] { "Tag" });

            var options = component.Instance.GetTorrentOptions();
            options.Category.Should().Be("Category");
            options.Tags.Should().BeEquivalentTo(new[] { "Tag" });

            await SetSelectValue(component, "Category", string.Empty);
            await ClearSelectedTags(component);

            var clearedOptions = component.Instance.GetTorrentOptions();
            clearedOptions.Category.Should().BeNull();
            clearedOptions.Tags.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ShareLimitModes_WHEN_Changed_THEN_GetTorrentOptionsMatches()
        {
            UseApiClientMock();

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetSelectValue(component, "ShareLimitMode", AddTorrentOptions.ShareLimitMode.Custom);
            FindFieldSwitch(component, "RatioLimitEnabled").Instance.Disabled.Should().BeFalse();

            await SetFieldSwitchValue(component, "RatioLimitEnabled", true);
            await SetNumericValue(component, "RatioLimit", 2.5f);
            await SetFieldSwitchValue(component, "SeedingTimeLimitEnabled", true);
            await SetNumericValue(component, "SeedingTimeLimit", 60);
            await SetFieldSwitchValue(component, "InactiveSeedingTimeLimitEnabled", true);
            await SetNumericValue(component, "InactiveSeedingTimeLimit", 30);
            await SetSelectValue(component, "StopCondition", "FilesChecked");
            await SetSelectValue(component, "ContentLayout", "Subfolder");

            var customOptions = component.Instance.GetTorrentOptions();
            customOptions.RatioLimit.Should().Be(2.5f);
            customOptions.SeedingTimeLimit.Should().Be(60);
            customOptions.InactiveSeedingTimeLimit.Should().Be(30);
            customOptions.StopCondition.Should().Be("FilesChecked");
            customOptions.ContentLayout.Should().Be("Subfolder");

            await SetSelectValue(component, "ShareLimitMode", AddTorrentOptions.ShareLimitMode.Global);
            FindFieldSwitch(component, "RatioLimitEnabled").Instance.Value.Should().BeFalse();
            FindFieldSwitch(component, "SeedingTimeLimitEnabled").Instance.Value.Should().BeFalse();
            FindFieldSwitch(component, "InactiveSeedingTimeLimitEnabled").Instance.Value.Should().BeFalse();

            var globalOptions = component.Instance.GetTorrentOptions();
            globalOptions.RatioLimit.Should().Be(Limits.GlobalLimit);
            globalOptions.SeedingTimeLimit.Should().Be(Limits.GlobalLimit);
            globalOptions.InactiveSeedingTimeLimit.Should().Be(Limits.GlobalLimit);

            await SetSelectValue(component, "ShareLimitMode", AddTorrentOptions.ShareLimitMode.NoLimit);

            var noLimitOptions = component.Instance.GetTorrentOptions();
            noLimitOptions.RatioLimit.Should().Be(Limits.NoLimit);
            noLimitOptions.SeedingTimeLimit.Should().Be(Limits.NoLimit);
            noLimitOptions.InactiveSeedingTimeLimit.Should().Be(Limits.NoLimit);
        }

        [Fact]
        public async Task GIVEN_CustomShareLimitWithDefaults_WHEN_GetTorrentOptionsInvoked_THEN_NoLimitApplied()
        {
            var preferences = CreatePreferences(maxRatioAct: 9);

            UseApiClientMock(preferences: preferences);

            var component = _target.RenderComponent();
            ExpandOptions(component);

            await SetSelectValue(component, "ShareLimitMode", AddTorrentOptions.ShareLimitMode.Custom);

            var options = component.Instance.GetTorrentOptions();
            options.RatioLimit.Should().Be(Limits.NoLimit);
            options.SeedingTimeLimit.Should().Be(Limits.NoLimit);
            options.InactiveSeedingTimeLimit.Should().Be(Limits.NoLimit);
        }

        [Fact]
        public async Task GIVEN_CategoryAndTagsProvided_WHEN_SelectMenusOpened_THEN_ItemsRendered()
        {
            var categories = new Dictionary<string, ClientModels.Category>
            {
                { "CategoryValue", CreateCategory("CategoryValue", "SavePath", null) },
            };
            var tags = new[] { "TagValue" };

            UseApiClientMock(categories: categories, tags: tags);
            TestContext.Render<MudPopoverProvider>();

            var component = _target.RenderComponent();
            ExpandOptions(component);

            var categorySelect = FindSelect<string>(component, "Category");
            await component.InvokeAsync(() => categorySelect.Instance.OpenMenu());

            component.WaitForState(() => component.FindComponents<MudSelectItem<string>>().Any(item => item.Instance.Value?.ToString() == "CategoryValue"));

            var tagsSelect = FindSelect<string>(component, "Tags");
            await component.InvokeAsync(() => tagsSelect.Instance.OpenMenu());

            component.WaitForState(() => component.FindComponents<MudSelectItem<string>>().Any(item => item.Instance.Value?.ToString() == "TagValue"));
        }

        private static void ExpandOptions(IRenderedComponent<AddTorrentOptions> component)
        {
            var toggle = FindComponentByTestId<MudSwitch<bool>>(component, "AdditionalOptions");
            toggle.Find("input").Change(true);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<AddTorrentOptions> component, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(component, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<AddTorrentOptions> component, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(component, testId);
        }

        private static IRenderedComponent<PathAutocomplete>? FindPathAutocomplete(IRenderedComponent<AddTorrentOptions> component, string testId)
        {
            return component.FindComponents<PathAutocomplete>()
                .SingleOrDefault(field => HasTestId(field, testId));
        }

        private static IRenderedComponent<MudNumericField<T>> FindNumericField<T>(IRenderedComponent<AddTorrentOptions> component, string testId)
        {
            return FindComponentByTestId<MudNumericField<T>>(component, testId);
        }

        private static IRenderedComponent<FieldSwitch> FindFieldSwitch(IRenderedComponent<AddTorrentOptions> component, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(component, testId);
        }

        private static string? GetSavePath(IRenderedComponent<AddTorrentOptions> component)
        {
            return FindPathAutocomplete(component, "SaveFilesLocation")?.Instance.Value;
        }

        private static string? GetDownloadPath(IRenderedComponent<AddTorrentOptions> component)
        {
            return FindPathAutocomplete(component, "IncompleteSavePath")?.Instance.Value;
        }

        private static bool GetUseDownloadPath(IRenderedComponent<AddTorrentOptions> component)
        {
            return FindFieldSwitch(component, "UseIncompleteSavePath").Instance.Value == true;
        }

        private static async Task SetSelectValue<T>(IRenderedComponent<AddTorrentOptions> component, string testId, T value)
        {
            var select = FindSelect<T>(component, testId);
            await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetTextFieldValue(IRenderedComponent<AddTorrentOptions> component, string testId, string value)
        {
            var pathField = FindPathAutocomplete(component, testId);
            if (pathField is not null)
            {
                await component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync(value));
                return;
            }

            var field = FindTextField(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetNumericValue<T>(IRenderedComponent<AddTorrentOptions> component, string testId, T value)
        {
            var field = FindNumericField<T>(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetFieldSwitchValue(IRenderedComponent<AddTorrentOptions> component, string testId, bool value)
        {
            var field = FindFieldSwitch(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetSelectedTags(IRenderedComponent<AddTorrentOptions> component, IEnumerable<string> values)
        {
            var select = FindSelect<string>(component, "Tags");
            await component.InvokeAsync(() => select.Instance.SelectedValuesChanged.InvokeAsync(values));
        }

        private static async Task ClearSelectedTags(IRenderedComponent<AddTorrentOptions> component)
        {
            var select = FindSelect<string>(component, "Tags");
            await component.InvokeAsync(() => select.Instance.SelectedValuesChanged.InvokeAsync(default(IEnumerable<string>)!));
        }

        private Mock<IApiClient> UseApiClientMock(
            IReadOnlyDictionary<string, ClientModels.Category>? categories = null,
            IEnumerable<string>? tags = null,
            ClientModels.Preferences? preferences = null)
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetAllCategories()).ReturnsAsync(categories ?? new Dictionary<string, ClientModels.Category>());
            apiClientMock.Setup(c => c.GetAllTags()).ReturnsAsync(tags?.ToArray() ?? Array.Empty<string>());
            apiClientMock.Setup(c => c.GetApplicationPreferences()).ReturnsAsync(preferences ?? CreatePreferences());
            return apiClientMock;
        }

        private static ClientModels.Category CreateCategory(string name, string? savePath, ClientModels.DownloadPathOption? downloadPath)
        {
            return new ClientModels.Category(name, savePath, downloadPath);
        }

        private static ClientModels.Preferences CreatePreferences(
            bool autoTmmEnabled = false,
            string? savePath = "SavePath",
            string? tempPath = "TempPath",
            bool tempPathEnabled = false,
            bool addStoppedEnabled = false,
            bool addToTopOfQueue = true,
            string stopCondition = "StopCondition",
            string contentLayout = "ContentLayout",
            bool maxRatioEnabled = false,
            float maxRatio = 1.0f,
            bool maxSeedingTimeEnabled = false,
            int maxSeedingTime = 0,
            bool maxInactiveSeedingTimeEnabled = false,
            int maxInactiveSeedingTime = 0,
            int maxRatioAct = 0)
        {
            var savePathValue = savePath is null ? "null" : $"\"{savePath}\"";
            var tempPathValue = tempPath is null ? "null" : $"\"{tempPath}\"";
            var json = $"{{\"auto_tmm_enabled\":{autoTmmEnabled.ToString().ToLowerInvariant()},\"save_path\":{savePathValue},\"temp_path\":{tempPathValue},\"temp_path_enabled\":{tempPathEnabled.ToString().ToLowerInvariant()},\"add_stopped_enabled\":{addStoppedEnabled.ToString().ToLowerInvariant()},\"add_to_top_of_queue\":{addToTopOfQueue.ToString().ToLowerInvariant()},\"torrent_stop_condition\":\"{stopCondition}\",\"torrent_content_layout\":\"{contentLayout}\",\"max_ratio_enabled\":{maxRatioEnabled.ToString().ToLowerInvariant()},\"max_ratio\":{maxRatio.ToString(CultureInfo.InvariantCulture)},\"max_seeding_time_enabled\":{maxSeedingTimeEnabled.ToString().ToLowerInvariant()},\"max_seeding_time\":{maxSeedingTime},\"max_inactive_seeding_time_enabled\":{maxInactiveSeedingTimeEnabled.ToString().ToLowerInvariant()},\"max_inactive_seeding_time\":{maxInactiveSeedingTime},\"max_ratio_act\":{maxRatioAct}}}";
            return JsonSerializer.Deserialize<ClientModels.Preferences>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
    }

    internal sealed class AddTorrentOptionsTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public AddTorrentOptionsTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public IRenderedComponent<AddTorrentOptions> RenderComponent(bool showCookieOption = false)
        {
            return _testContext.Render<AddTorrentOptions>(parameters =>
            {
                parameters.Add(p => p.ShowCookieOption, showCookieOption);
            });
        }
    }
}
