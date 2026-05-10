using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Pages
{
    public sealed class ThemesTests : RazorComponentTestBase<Themes>
    {
        private readonly IThemeManagerService _themeManagerService;
        private readonly IThemeFontCatalog _themeFontCatalog;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ISnackbar _snackbar;

        public ThemesTests()
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
            Mock.Get(_themeManagerService)
                .Setup(service => service.EnsureRepositoryThemesLoaded())
                .Returns(Task.CompletedTask);
            Mock.Get(_themeManagerService)
                .Setup(service => service.ReloadServerThemes())
                .Returns(Task.CompletedTask);
            Mock.Get(_themeManagerService)
                .Setup(service => service.GetLocalThemeStorageTypeAsync())
                .ReturnsAsync(StorageType.LocalStorage);
        }

        [Fact]
        public void GIVEN_ThemesProvided_WHEN_Rendered_THEN_ShowsTableItems()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };

            var target = RenderPage(themes);

            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();

            table.Instance.Items.Should().ContainSingle(item => item.Id == "ThemeId");
        }

        [Fact]
        public void GIVEN_ThemeWithSourcePath_WHEN_Rendered_THEN_PreservesSourcePath()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Server, "SourcePath")
            };

            var target = RenderPage(themes);
            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();

            table.Instance.Items.Should().NotBeNull();
            table.Instance.Items!.Single().SourcePath.Should().Be("SourcePath");
        }

        [Fact]
        public void GIVEN_CurrentThemeApplied_WHEN_Rendered_THEN_SelectsAppliedThemeInTable()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeA", "Theme A", ThemeSource.Local),
                CreateTheme("ThemeB", "Theme B", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns("ThemeB");

            var target = RenderPage(themes);

            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();

            table.Instance.SelectedItem.Should().NotBeNull();
            table.Instance.SelectedItem!.Id.Should().Be("ThemeB");
            table.Instance.HighlightSelectedItem.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_CurrentThemeAppliedLocal_WHEN_Rendered_THEN_DoesNotRenderAppliedChip()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns("ThemeId");

            var target = RenderPage(themes);

            var chips = target.FindComponents<MudChip<string>>();

            chips.Should().HaveCount(1);
            chips[0].Instance.Color.Should().Be(Color.Default);
            GetChildContentText(chips[0].Instance.ChildContent).Should().Be("Local Storage");
        }

        [Fact]
        public void GIVEN_ReadOnlyTheme_WHEN_Rendered_THEN_RendersSingleSourceChip()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Server)
            };

            var target = RenderPage(themes);

            var chips = target.FindComponents<MudChip<string>>();

            chips.Should().HaveCount(1);
            chips[0].Instance.Color.Should().Be(Color.Info);
        }

        [Fact]
        public void GIVEN_LocalThemeStoredInClientData_WHEN_Rendered_THEN_ShowsClientDataLabel()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .Setup(service => service.GetLocalThemeStorageTypeAsync())
                .ReturnsAsync(StorageType.ClientData);

            var target = RenderPage(themes);

            var chips = target.FindComponents<MudChip<string>>();

            chips.Should().HaveCount(1);
            GetChildContentText(chips[0].Instance.ChildContent).Should().Be("Client Data");
        }

        [Fact]
        public void GIVEN_InitialRepositoryIssues_WHEN_Rendered_THEN_ShowsWarning()
        {
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.LastReloadHadRepositoryIssues)
                .Returns(true);
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add("Unable to load theme repository. Showing bundled and local themes only.", Severity.Warning, null, null));

            RenderPage(new List<ThemeCatalogItem>());

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add("Unable to load theme repository. Showing bundled and local themes only.", Severity.Warning, null, null), Times.Once);
        }

        [Fact]
        public void GIVEN_NoCurrentTheme_WHEN_Rendered_THEN_DoesNotSelectThemeInTable()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeA", "Theme A", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns((string?)null);

            var target = RenderPage(themes);

            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();

            table.Instance.SelectedItem.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_BackClicked_WHEN_DrawerClosed_THEN_NavigatesHome()
        {
            var target = RenderPage(new List<ThemeCatalogItem>(), drawerOpen: false);
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");

            var backButton = FindComponentByTestId<MudIconButton>(target, "ThemesBack");
            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_CreateCanceled_WHEN_Invoked_THEN_DoesNotSave()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("New Theme", "Name", null))
                .ReturnsAsync((string?)null);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var createButton = FindComponentByTestId<MudIconButton>(target, "ThemesCreate");
            await target.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_CreateWithNoCurrentTheme_WHEN_Invoked_THEN_SavesAndNavigates()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("New Theme", "Name", null))
                .ReturnsAsync("Name");
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentTheme)
                .Returns((ThemeCatalogItem?)null);
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var createButton = FindComponentByTestId<MudIconButton>(target, "ThemesCreate");
            await target.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(definition =>
                        definition.Name == "Name"
                        && definition.Description == string.Empty)),
                Times.Once);
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/themes/");
        }

        [Fact]
        public async Task GIVEN_CreateWithCurrentTheme_WHEN_Invoked_THEN_ClonesCurrentTheme()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("New Theme", "Name", null))
                .ReturnsAsync("Name");
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentTheme)
                .Returns(
                    new ThemeCatalogItem(
                        "CurrentId",
                        "Current",
                        new ThemeDefinition
                        {
                            Id = "CurrentId",
                            Name = "Current",
                            Description = "Description",
                            FontFamily = "Open Sans",
                            Theme = new MudTheme()
                        },
                        ThemeSource.Local,
                        null));
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var createButton = FindComponentByTestId<MudIconButton>(target, "ThemesCreate");
            await target.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(definition =>
                        definition.Name == "Name"
                        && definition.Description == string.Empty
                        && definition.FontFamily == "Open Sans")),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeNotApplied_WHEN_ApplyInvoked_THEN_AppliesTheme()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns("Other");
            Mock.Get(_themeManagerService)
                .Setup(service => service.ApplyTheme("ThemeId"))
                .Returns(Task.CompletedTask);

            var target = RenderPage(themes);

            var applyButton = FindComponentByTestId<MudIconButton>(target, "ThemeApply-ThemeId");
            await target.InvokeAsync(() => applyButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.ApplyTheme("ThemeId"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeApplied_WHEN_ApplyInvoked_THEN_SkipsApply()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns("ThemeId");

            var target = RenderPage(themes);

            var applyButton = FindComponentByTestId<MudIconButton>(target, "ThemeApply-ThemeId");
            await target.InvokeAsync(() => applyButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.ApplyTheme(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_PreviewClicked_WHEN_Invoked_THEN_ShowsCataloguePreviewDialog()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local),
                CreateTheme("ThemeIdTwo", "Name Two", ThemeSource.Repository)
            };
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowThemePreviewDialog(It.IsAny<ThemePreviewDialogRequest>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(themes);

            var previewButton = FindComponentByTestId<MudIconButton>(target, "ThemePreview-ThemeId");
            await target.InvokeAsync(() => previewButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowThemePreviewDialog(
                    It.Is<ThemePreviewDialogRequest>(request =>
                        request.Mode == ThemePreviewDialogMode.Catalogue
                        && request.SelectedThemeId == "ThemeId"
                        && request.Items.Count == 2)),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DeleteCanceled_WHEN_Invoked_THEN_DoesNotDelete()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog("Delete theme?", "Delete 'Name'?"))
                .ReturnsAsync(false);

            var target = RenderPage(themes);

            var deleteButton = FindComponentByTestId<MudIconButton>(target, "ThemeDelete-ThemeId");
            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.DeleteLocalTheme(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DeleteConfirmed_WHEN_Invoked_THEN_DeletesTheme()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog("Delete theme?", "Delete 'Name'?"))
                .ReturnsAsync(true);

            Mock.Get(_themeManagerService)
                .Setup(service => service.DeleteLocalTheme("ThemeId"))
                .Returns(Task.CompletedTask);

            var target = RenderPage(themes);

            var deleteButton = FindComponentByTestId<MudIconButton>(target, "ThemeDelete-ThemeId");
            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.DeleteLocalTheme("ThemeId"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReloadInvoked_WHEN_Clicked_THEN_ReloadsThemes()
        {
            Mock.Get(_themeManagerService)
                .Setup(service => service.ReloadServerThemes())
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());
            _themeManagerService.ClearInvocations();

            var reloadButton = FindComponentByTestId<MudIconButton>(target, "ThemesReload");
            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.ReloadServerThemes(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReloadInProgress_WHEN_ClickedAgain_THEN_SkipsSecondReload()
        {
            var reloadTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_themeManagerService)
                .Setup(service => service.ReloadServerThemes())
                .Returns(reloadTaskSource.Task);

            var target = RenderPage(new List<ThemeCatalogItem>());
            _themeManagerService.ClearInvocations();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "ThemesReload");

            var firstReloadTask = target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_themeManagerService).Verify(service => service.ReloadServerThemes(), Times.Once);
            });

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.ReloadServerThemes(), Times.Once);

            reloadTaskSource.SetResult();
            await firstReloadTask;
        }

        [Fact]
        public async Task GIVEN_ReloadWithRepositoryIssues_WHEN_Clicked_THEN_ShowsWarning()
        {
            Mock.Get(_themeManagerService)
                .Setup(service => service.ReloadServerThemes())
                .Returns(Task.CompletedTask);
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.LastReloadHadRepositoryIssues)
                .Returns(true);
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add("Unable to load theme repository. Showing bundled and local themes only.", Severity.Warning, null, null));

            var target = RenderPage(new List<ThemeCatalogItem>());
            _snackbar.ClearInvocations();

            var reloadButton = FindComponentByTestId<MudIconButton>(target, "ThemesReload");
            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add("Unable to load theme repository. Showing bundled and local themes only.", Severity.Warning, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_InvalidImportJson_WHEN_Imported_THEN_ShowsError()
        {
            var file = new TestBrowserFile("theme.json", "not-json");
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(It.Is<string>(message => message.StartsWith("Unable to import theme:", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null));

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.Is<string>(message => message.StartsWith("Unable to import theme:", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ImportNullJson_WHEN_Imported_THEN_ShowsInvalidJsonMessage()
        {
            var file = new TestBrowserFile("theme.json", "null");
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(It.Is<string>(message => message.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null));

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_snackbar).Verify(snackbar => snackbar.Add(It.Is<string>(message => message.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ImportWithoutFiles_WHEN_Imported_THEN_SkipsSave()
        {
            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile>()));

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ImportInProgress_WHEN_ImportedAgain_THEN_SkipsSecondImport()
        {
            var definition = new ThemeDefinition
            {
                Id = "ThemeId",
                Name = "Name",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var file = new TestBrowserFile("theme.json", json);

            SetupFontCatalogValid();
            var saveTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(saveTaskSource.Task);

            var target = RenderPage(new List<ThemeCatalogItem>());
            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();

            var firstImportTask = target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            target.WaitForAssertion(() =>
            {
                Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Once);
            });

            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Once);

            saveTaskSource.SetResult();
            await firstImportTask;
        }

        [Fact]
        public async Task GIVEN_ValidImportJson_WHEN_Imported_THEN_SavesAndNavigates()
        {
            var definition = new ThemeDefinition
            {
                Id = "ThemeId",
                Name = "Name",
                Description = " Description ",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var file = new TestBrowserFile("theme.json", json);

            SetupFontCatalogValid();
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(value =>
                        value.Name == "Name"
                        && value.Description == "Description")),
                Times.Once);
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/themes/");
        }

        [Fact]
        public async Task GIVEN_ImportThrowsIOException_WHEN_Imported_THEN_ShowsError()
        {
            var file = new FailingBrowserFile("theme.json");
            Mock.Get(_snackbar)
                .Setup(snackbar => snackbar.Add(It.Is<string>(message => message.Contains("Unable to import theme", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null));

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_snackbar)
                .Verify(snackbar => snackbar.Add(It.Is<string>(message => message.Contains("Unable to import theme", StringComparison.OrdinalIgnoreCase)), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ImportMissingName_WHEN_Imported_THEN_UsesFallbackName()
        {
            var definition = new ThemeDefinition
            {
                Id = "ThemeId",
                Name = " ",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var file = new TestBrowserFile(" .json", json);

            SetupFontCatalogValid();
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(value => value.Name == "Imported Theme")),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ImportDuplicateId_WHEN_Imported_THEN_AssignsNewId()
        {
            var existing = CreateTheme("ThemeId", "Existing", ThemeSource.Local);
            var definition = new ThemeDefinition
            {
                Id = "ThemeId",
                Name = "Name",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var file = new TestBrowserFile("theme.json", json);

            SetupFontCatalogValid();
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem> { existing });

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(value => value.Id != "ThemeId")),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ImportInvalidFont_WHEN_Imported_THEN_FallsBackToNunitoSans()
        {
            var definition = new ThemeDefinition
            {
                Id = "ThemeId",
                Name = "Name",
                FontFamily = "Invalid",
                Theme = new MudTheme()
            };
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var file = new TestBrowserFile("theme.json", json);

            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl("Invalid", out It.Ref<string>.IsAny))
                .Returns(false);
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(value => value.FontFamily == "Nunito Sans")),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ImportWithoutIdThemeAndFont_WHEN_Imported_THEN_NormalizesDefaults()
        {
            var definition = new ThemeDefinition
            {
                Id = " ",
                Name = "Name",
                Description = " ",
                FontFamily = " ",
                Theme = new MudTheme()
            };
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var file = new TestBrowserFile("theme.json", json);

            SetupFontCatalogValid();
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(
                    It.Is<ThemeDefinition>(value =>
                        !string.IsNullOrWhiteSpace(value.Id)
                        && value.Description == string.Empty
                        && value.FontFamily == "Nunito Sans")),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ImportWithNullTheme_WHEN_Imported_THEN_ThrowsNullReferenceException()
        {
            var definition = new ThemeDefinition
            {
                Id = "ThemeId",
                Name = "Name",
                FontFamily = "Nunito Sans",
                Theme = null!
            };
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var file = new TestBrowserFile("theme.json", json);

            SetupFontCatalogValid();
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            Func<Task> action = async () =>
            {
                await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));
            };

            await action.Should().ThrowAsync<NullReferenceException>();
        }

        [Fact]
        public async Task GIVEN_DetailsClicked_WHEN_Invoked_THEN_NavigatesToDetails()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            var target = RenderPage(themes);
            var detailsButton = FindComponentByTestId<MudIconButton>(target, "ThemeDetails-ThemeId");

            await target.InvokeAsync(() => detailsButton.Instance.OnClick.InvokeAsync());

            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/themes/ThemeId");
        }

        [Fact]
        public async Task GIVEN_RowClicked_WHEN_OnRowClickNotConfigured_THEN_DoesNotNavigate()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            var target = RenderPage(themes);
            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");

            table.Instance.OnRowClick.HasDelegate.Should().BeFalse();

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            navigationManager.Uri.Should().Be("http://localhost/other");
        }

        [Fact]
        public void GIVEN_RowClassFunc_WHEN_ThemeApplied_THEN_ReturnsAppliedClass()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns("ThemeId");

            var target = RenderPage(themes);
            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();

            var result = table.Instance.RowClassFunc?.Invoke(themes[0], 0);

            result.Should().Be("theme-row--applied");
        }

        [Fact]
        public async Task GIVEN_ApplyInProgress_WHEN_ClickedAgain_THEN_SkipsSecondApply()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.CurrentThemeId)
                .Returns("Other");
            var applyTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_themeManagerService)
                .Setup(service => service.ApplyTheme("ThemeId"))
                .Returns(applyTaskSource.Task);

            var target = RenderPage(themes);
            var applyButton = FindComponentByTestId<MudIconButton>(target, "ThemeApply-ThemeId");

            var firstApplyTask = target.InvokeAsync(() => applyButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_themeManagerService).Verify(service => service.ApplyTheme("ThemeId"), Times.Once);
            });

            await target.InvokeAsync(() => applyButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.ApplyTheme("ThemeId"), Times.Once);

            applyTaskSource.SetResult();
            await firstApplyTask;
        }

        [Fact]
        public async Task GIVEN_DeleteReadOnlyTheme_WHEN_Clicked_THEN_SkipsDelete()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Server)
            };

            var target = RenderPage(themes);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "ThemeDelete-ThemeId");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Mock.Get(_themeManagerService).Verify(service => service.DeleteLocalTheme(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DeleteRepositoryTheme_WHEN_Clicked_THEN_SkipsDelete()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Repository)
            };

            var target = RenderPage(themes);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "ThemeDelete-ThemeId");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Mock.Get(_themeManagerService).Verify(service => service.DeleteLocalTheme(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DeleteInProgress_WHEN_ClickedAgain_THEN_SkipsSecondDelete()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            var confirmTaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog("Delete theme?", "Delete 'Name'?"))
                .Returns(confirmTaskSource.Task);

            var target = RenderPage(themes);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "ThemeDelete-ThemeId");

            var firstDeleteTask = target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowConfirmDialog("Delete theme?", "Delete 'Name'?"), Times.Once);
            });

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowConfirmDialog("Delete theme?", "Delete 'Name'?"), Times.Once);

            confirmTaskSource.SetResult(false);
            await firstDeleteTask;
        }

        [Fact]
        public void GIVEN_ColumnDefinitions_WHEN_Referenced_THEN_ReturnsDefinitions()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);

            Themes.ColumnsDefinitions.Should().HaveCount(5);
            Themes.ColumnsDefinitions.Select(definition => definition.Header)
                .Should()
                .Equal("Theme", "Description", "Source", "Colors", "Actions");

            Themes.ColumnsDefinitions[0].SortSelector(theme).Should().Be("Name");
            Themes.ColumnsDefinitions[1].SortSelector(theme).Should().Be("Description");
            Themes.ColumnsDefinitions[2].SortSelector(theme).Should().Be(ThemeSource.Local.ToString());
            Themes.ColumnsDefinitions[3].SortSelector(theme).Should().Be("Name");
            Themes.ColumnsDefinitions[4].SortSelector(theme).Should().Be("Name");
        }

        private IRenderedComponent<Themes> RenderPage(List<ThemeCatalogItem> themes, bool drawerOpen = true)
        {
            Mock.Get(_themeManagerService)
                .SetupGet(service => service.Themes)
                .Returns(themes);

            return TestContext.Render<Themes>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
            });
        }

        private static ThemeCatalogItem CreateTheme(string id, string name, ThemeSource source, string? sourcePath = null)
        {
            return new ThemeCatalogItem(id, name, new ThemeDefinition { Description = "Description", FontFamily = "Nunito Sans", Theme = new MudTheme() }, source, sourcePath);
        }

        private void SetupFontCatalogValid()
        {
            var url = "Url";
            Mock.Get(_themeFontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl(It.IsAny<string>(), out url))
                .Returns(true);
        }

        private sealed class TestBrowserFile : IBrowserFile
        {
            private readonly byte[] _content;

            public TestBrowserFile(string name, string content)
            {
                Name = name;
                _content = Encoding.UTF8.GetBytes(content);
            }

            public string Name { get; }

            public DateTimeOffset LastModified => new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            public long Size => _content.Length;

            public string ContentType => "application/json";

            public Stream OpenReadStream(long maxAllowedSize, CancellationToken cancellationToken = default)
            {
                return new MemoryStream(_content);
            }
        }

        private sealed class FailingBrowserFile : IBrowserFile
        {
            public FailingBrowserFile(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public DateTimeOffset LastModified => new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            public long Size => 1;

            public string ContentType => "application/json";

            public Stream OpenReadStream(long maxAllowedSize, CancellationToken cancellationToken = default)
            {
                throw new IOException("Failure");
            }
        }
    }
}
