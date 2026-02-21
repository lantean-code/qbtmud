using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class ThemeDetailTests : RazorComponentTestBase<ThemeDetail>
    {
        private readonly IThemeManagerService _themeManagerService;
        private readonly IThemeFontCatalog _themeFontCatalog;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ISnackbar _snackbar;

        public ThemeDetailTests()
        {
            _themeManagerService = Mock.Of<IThemeManagerService>();
            _themeFontCatalog = Mock.Of<IThemeFontCatalog>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();
            _snackbar = Mock.Of<ISnackbar>();

            TestContext.Services.RemoveAll<IThemeManagerService>();
            TestContext.Services.RemoveAll<IThemeFontCatalog>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.RemoveAll<ISnackbar>();

            TestContext.Services.AddSingleton(_themeManagerService);
            TestContext.Services.AddSingleton(_themeFontCatalog);
            TestContext.Services.AddSingleton(_dialogWorkflow);
            TestContext.Services.AddSingleton(_snackbar);

            Mock.Get(_themeManagerService)
                .Setup(service => service.EnsureInitialized())
                .Returns(Task.CompletedTask);

            SetupFontCatalog(new[] { "Nunito Sans", "Open Sans" });
        }

        [Fact]
        public void GIVEN_MissingThemeId_WHEN_Rendered_THEN_ShowsMissingState()
        {
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(new List<ThemeCatalogItem>());

            var target = RenderPage(string.Empty, new List<ThemeCatalogItem>());

            var missing = FindComponentByTestId<MudText>(target, "ThemeDetailMissing");
            GetChildContentText(missing.Instance.ChildContent).Should().Be("Theme not found.");
        }

        [Fact]
        public void GIVEN_UnknownThemeId_WHEN_Rendered_THEN_ShowsMissingState()
        {
            var target = RenderPage("MissingId", new List<ThemeCatalogItem>());

            var missing = FindComponentByTestId<MudText>(target, "ThemeDetailMissing");
            GetChildContentText(missing.Instance.ChildContent).Should().Be("Theme not found.");
        }

        [Fact]
        public async Task GIVEN_BackClicked_WHEN_Invoked_THEN_NavigatesToThemes()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");

            var backButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailBack");
            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/themes");
        }

        [Fact]
        public async Task GIVEN_PreviewClicked_WHEN_Invoked_THEN_ShowsPreviewDialog()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowThemePreviewDialog(It.IsAny<MudTheme>(), true))
                .Returns(Task.CompletedTask);

            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme }, isDarkMode: true);

            var previewButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailPreview");
            await target.InvokeAsync(() => previewButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow)
                .Verify(workflow => workflow.ShowThemePreviewDialog(It.IsAny<MudTheme>(), true), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NameCleared_WHEN_Changed_THEN_ShowsError()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var nameField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailName");
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync(" "));

            nameField.Instance.GetState(x => x.Error).Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FontInvalid_WHEN_Changed_THEN_ShowsError()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            SetupFontCatalog(new[] { "Nunito Sans" });
            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl("Invalid", out It.Ref<string>.IsAny))
                .Returns(false);

            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var fontField = FindComponentByTestId<MudAutocomplete<string>>(target, "ThemeDetailFont");
            await target.InvokeAsync(() => fontField.Instance.ValueChanged.InvokeAsync("Invalid"));

            fontField.Instance.GetState(x => x.Error).Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FontValid_WHEN_Changed_THEN_SaveEnabled()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            SetupFontCatalog(new[] { "Nunito Sans" });

            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var fontField = FindComponentByTestId<MudAutocomplete<string>>(target, "ThemeDetailFont");
            await target.InvokeAsync(() => fontField.Instance.ValueChanged.InvokeAsync("Nunito Sans"));

            var saveButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSave");
            saveButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ResetInvoked_WHEN_Changed_THEN_RevertsName()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var nameField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailName");
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("New Name"));

            var resetButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailReset");
            await target.InvokeAsync(() => resetButton.Instance.OnClick.InvokeAsync());

            nameField.Instance.GetState(x => x.Value).Should().Be("Name");
        }

        [Fact]
        public async Task GIVEN_SaveInvoked_WHEN_Valid_THEN_SavesTheme()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);

            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage("ThemeId", themes);

            var nameField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailName");
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Updated"));

            var descriptionField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailDescription");
            await target.InvokeAsync(() => descriptionField.Instance.ValueChanged.InvokeAsync("Updated Description"));

            var saveButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSave");
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(definition =>
                        definition.Name == "Updated" &&
                        definition.Description == "Updated Description")),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SaveAndApplyInvoked_WHEN_Valid_THEN_SavesAndApplies()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_themeManagerService)
                .Setup(service => service.ApplyTheme("ThemeId"))
                .Returns(Task.CompletedTask);

            var target = RenderPage("ThemeId", themes);

            var nameField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailName");
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Updated"));

            var saveButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSaveApply");
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Once);
            Mock.Get(_themeManagerService).Verify(service => service.ApplyTheme("ThemeId"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReadOnlyTheme_WHEN_SaveActionsInvoked_THEN_DoesNotPersist()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Server)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);

            var target = RenderPage("ThemeId", themes);

            var saveButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSave");
            var saveApplyButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSaveApply");

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => saveApplyButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService)
                .Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Never);
            Mock.Get(_themeManagerService)
                .Verify(service => service.ApplyTheme(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_WhitespaceName_WHEN_SaveApplyInvoked_THEN_DoesNotSave()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);

            var target = RenderPage("ThemeId", themes);

            var nameField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailName");
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync(" "));

            var saveApplyButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSaveApply");
            await target.InvokeAsync(() => saveApplyButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService)
                .Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Never);
            nameField.Instance.GetState(x => x.Error).Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ThemeRemovedAfterSave_WHEN_SaveInvoked_THEN_NavigatesBackToThemes()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback(() => themes.Clear())
                .Returns(Task.CompletedTask);

            var target = RenderPage("ThemeId", themes);
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/theme-edit");

            var nameField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailName");
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Updated"));

            var saveButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSave");
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/themes");
        }

        [Fact]
        public async Task GIVEN_SaveInProgress_WHEN_SaveInvokedAgain_THEN_SecondInvocationReturnsEarly()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            var saveCompletion = new TaskCompletionSource<bool>();

            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(saveCompletion.Task);

            var target = RenderPage("ThemeId", themes);

            var nameField = FindComponentByTestId<MudTextField<string>>(target, "ThemeDetailName");
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Updated"));

            var saveButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSave");
            var firstSaveTask = target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService)
                .Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Once);

            saveCompletion.SetResult(true);
            await firstSaveTask;
        }

        [Fact]
        public async Task GIVEN_ColorChanged_WHEN_PickerInvoked_THEN_SaveEnabled()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            var popoverProvider = TestContext.Render<MudPopoverProvider>();
            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var tabs = target.FindComponent<MudTabs>();
            await target.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(1));

            var colorItem = FindComponentByTestId<ThemeColorItem>(target, "ThemeDetailDark-Primary");
            colorItem.Find("div.theme-color-item__row").Click();
            var colorPicker = popoverProvider.FindComponent<MudColorPicker>();
            await target.InvokeAsync(() => colorPicker.Instance.ValueChanged.InvokeAsync(new MudColor("#123456")));

            var saveButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetailSave");
            saveButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SearchInvoked_WHEN_FontsAvailable_THEN_LoadsPreviewsOnce()
        {
            SetupFontCatalog(new[] { "Nunito Sans", "Open Sans", "Invalid" });
            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl("Invalid", out It.Ref<string>.IsAny))
                .Returns(false);
            var loadGoogleFontInvocation = TestContext.JSInterop.SetupVoid("qbt.loadGoogleFont", _ => true);
            loadGoogleFontInvocation.SetVoidResult();

            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var fontField = FindComponentByTestId<MudAutocomplete<string>>(target, "ThemeDetailFont");
            var searchFunc = fontField.Instance.SearchFunc ?? throw new InvalidOperationException();

            Task<IEnumerable<string>>? searchTask = null;
            await target.InvokeAsync(() =>
            {
                searchTask = searchFunc.Invoke(string.Empty, CancellationToken.None);
            });
            searchTask.Should().NotBeNull();
            await searchTask!;

            Task<IEnumerable<string>>? secondTask = null;
            await target.InvokeAsync(() =>
            {
                secondTask = searchFunc.Invoke(string.Empty, CancellationToken.None);
            });
            secondTask.Should().NotBeNull();
            await secondTask!;

            loadGoogleFontInvocation.Invocations.Should().HaveCount(2);
        }

        [Fact]
        public async Task GIVEN_SearchInvoked_WHEN_FilterProvided_THEN_ReturnsFilteredFonts()
        {
            SetupFontCatalog(new[] { "Nunito Sans", "Open Sans" });
            TestContext.JSInterop.SetupVoid("qbt.loadGoogleFont", _ => true).SetVoidResult();

            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);
            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var fontField = FindComponentByTestId<MudAutocomplete<string>>(target, "ThemeDetailFont");
            var searchFunc = fontField.Instance.SearchFunc ?? throw new InvalidOperationException();

            Task<IEnumerable<string>>? searchTask = null;
            await target.InvokeAsync(() =>
            {
                searchTask = searchFunc.Invoke("Open", CancellationToken.None);
            });

            searchTask.Should().NotBeNull();
            var results = (await searchTask!).ToList();
            results.Should().Equal("Open Sans");
        }

        [Fact]
        public void GIVEN_InvalidThemeFont_WHEN_Rendered_THEN_UsesFallbackFont()
        {
            SetupFontCatalog(new[] { "Nunito Sans" });

            var theme = new ThemeCatalogItem(
                "ThemeId",
                "Name",
                new ThemeDefinition { FontFamily = "Invalid", Theme = new MudTheme() },
                ThemeSource.Local,
                null);
            var target = RenderPage("ThemeId", new List<ThemeCatalogItem> { theme });

            var fontField = FindComponentByTestId<MudAutocomplete<string>>(target, "ThemeDetailFont");
            fontField.Instance.GetState(x => x.Value).Should().Be("Nunito Sans");
        }

        private IRenderedComponent<ThemeDetail> RenderPage(string themeId, List<ThemeCatalogItem> themes, bool isDarkMode = false)
        {
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);

            return TestContext.Render<ThemeDetail>(parameters =>
            {
                parameters.Add(p => p.ThemeId, themeId);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("IsDarkMode", isDarkMode);
            });
        }

        private static ThemeCatalogItem CreateTheme(string id, string name, ThemeSource source)
        {
            return new ThemeCatalogItem(id, name, new ThemeDefinition { Description = "Description", FontFamily = "Nunito Sans", Theme = new MudTheme() }, source, null);
        }

        private void SetupFontCatalog(IEnumerable<string> fonts)
        {
            Mock.Get(_themeFontCatalog)
                .SetupGet(catalog => catalog.SuggestedFonts)
                .Returns(fonts.ToList());

            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl(It.IsAny<string>(), out It.Ref<string>.IsAny))
                .Returns((string fontFamily, out string url) =>
                {
                    if (fonts.Contains(fontFamily, StringComparer.OrdinalIgnoreCase))
                    {
                        url = $"https://fonts/{fontFamily}";
                        return true;
                    }

                    url = string.Empty;
                    return false;
                });
        }
    }
}
