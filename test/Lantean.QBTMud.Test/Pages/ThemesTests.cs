using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Pages
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

            ThemeDefinition? saved = null;
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback<ThemeDefinition>(definition => saved = definition)
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var createButton = FindComponentByTestId<MudIconButton>(target, "ThemesCreate");
            await target.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Name");
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/themes/");
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
        public async Task GIVEN_DuplicateAccepted_WHEN_Invoked_THEN_SavesAndNavigates()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("Duplicate Theme", "Name", "Name Copy"))
                .ReturnsAsync("Name Copy");

            ThemeDefinition? saved = null;
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback<ThemeDefinition>(definition => saved = definition)
                .Returns(Task.CompletedTask);

            var target = RenderPage(themes);

            var duplicateButton = FindComponentByTestId<MudIconButton>(target, "ThemeDuplicate-ThemeId");
            await target.InvokeAsync(() => duplicateButton.Instance.OnClick.InvokeAsync());

            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Name Copy");
            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/themes/");
        }

        [Fact]
        public async Task GIVEN_DuplicateCanceled_WHEN_Invoked_THEN_DoesNotSave()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("Duplicate Theme", "Name", "Name Copy"))
                .ReturnsAsync((string?)null);

            var target = RenderPage(themes);

            var duplicateButton = FindComponentByTestId<MudIconButton>(target, "ThemeDuplicate-ThemeId");
            await target.InvokeAsync(() => duplicateButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RenameCanceled_WHEN_Invoked_THEN_DoesNotSave()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("Rename Theme", "Name", "Name"))
                .ReturnsAsync((string?)null);

            var target = RenderPage(themes);

            var renameButton = FindComponentByTestId<MudIconButton>(target, "ThemeRename-ThemeId");
            await target.InvokeAsync(() => renameButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RenameAccepted_WHEN_Invoked_THEN_SavesTheme()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("Rename Theme", "Name", "Name"))
                .ReturnsAsync("Renamed");

            ThemeDefinition? saved = null;
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback<ThemeDefinition>(definition => saved = definition)
                .Returns(Task.CompletedTask);

            var target = RenderPage(themes);

            var renameButton = FindComponentByTestId<MudIconButton>(target, "ThemeRename-ThemeId");
            await target.InvokeAsync(() => renameButton.Instance.OnClick.InvokeAsync());

            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Renamed");
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

            var reloadButton = FindComponentByTestId<MudIconButton>(target, "ThemesReload");
            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(service => service.ReloadServerThemes(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ExportInvoked_WHEN_Clicked_THEN_TriggersDownload()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Theme/Name", ThemeSource.Local)
            };
            TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true).SetVoidResult();

            var target = RenderPage(themes);

            var exportButton = FindComponentByTestId<MudIconButton>(target, "ThemeExport-ThemeId");
            await target.InvokeAsync(() => exportButton.Instance.OnClick.InvokeAsync());

            var invocation = TestContext.JSInterop.Invocations
                .Where(item => item.Identifier == "qbt.triggerFileDownload")
                .Should()
                .ContainSingle()
                .Subject;
            invocation.Arguments.Count.Should().Be(2);
            invocation.Arguments[1].Should().Be("Theme-Name.json");
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
        public async Task GIVEN_ValidImportJson_WHEN_Imported_THEN_SavesAndNavigates()
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
            ThemeDefinition? saved = null;
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback<ThemeDefinition>(value => saved = value)
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Name");
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
            ThemeDefinition? saved = null;
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback<ThemeDefinition>(value => saved = value)
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Imported Theme");
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
            ThemeDefinition? saved = null;
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback<ThemeDefinition>(value => saved = value)
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem> { existing });

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            saved.Should().NotBeNull();
            saved!.Id.Should().NotBe("ThemeId");
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

            ThemeDefinition? saved = null;
            Mock.Get(_themeManagerService)
                .Setup(service => service.SaveLocalTheme(It.IsAny<ThemeDefinition>()))
                .Callback<ThemeDefinition>(value => saved = value)
                .Returns(Task.CompletedTask);

            var target = RenderPage(new List<ThemeCatalogItem>());

            var upload = target.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
            await target.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(new List<IBrowserFile> { file }));

            saved.Should().NotBeNull();
            saved!.FontFamily.Should().Be("Nunito Sans");
        }

        [Fact]
        public async Task GIVEN_RowClicked_WHEN_OnRowClickInvoked_THEN_NavigatesToDetails()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            var target = RenderPage(themes);
            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();
            var row = target.FindComponents<MudTr>().First();
            var args = new TableRowClickEventArgs<ThemeCatalogItem>(new MouseEventArgs(), row.Instance, themes[0]);

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(args));

            TestContext.Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/themes/ThemeId");
        }

        [Fact]
        public async Task GIVEN_RowClickedWithNullItem_WHEN_OnRowClickInvoked_THEN_DoesNotNavigate()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "Name", ThemeSource.Local)
            };
            var target = RenderPage(themes);
            var table = target.FindComponent<DynamicTable<ThemeCatalogItem>>();
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");

            var row = target.FindComponents<MudTr>().First();
            var args = new TableRowClickEventArgs<ThemeCatalogItem>(new MouseEventArgs(), row.Instance, null);

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(args));

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
        public async Task GIVEN_ExportWithWhitespaceName_WHEN_Invoked_THEN_UsesDefaultFileName()
        {
            var themes = new List<ThemeCatalogItem>
            {
                CreateTheme("ThemeId", "   ", ThemeSource.Local)
            };
            TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true).SetVoidResult();

            var target = RenderPage(themes);

            var exportButton = FindComponentByTestId<MudIconButton>(target, "ThemeExport-ThemeId");
            await target.InvokeAsync(() => exportButton.Instance.OnClick.InvokeAsync());

            var invocation = TestContext.JSInterop.Invocations
                .Where(item => item.Identifier == "qbt.triggerFileDownload")
                .Should()
                .ContainSingle()
                .Subject;
            invocation.Arguments.Count.Should().Be(2);
            invocation.Arguments[1].Should().Be("theme.json");
        }

        [Fact]
        public void GIVEN_ColumnDefinitions_WHEN_Referenced_THEN_ReturnsDefinitions()
        {
            var theme = CreateTheme("ThemeId", "Name", ThemeSource.Local);

            Themes.ColumnsDefinitions.Should().HaveCount(3);
            Themes.ColumnsDefinitions.Select(definition => definition.Header)
                .Should()
                .Equal("Theme", "Source", "Actions");

            Themes.ColumnsDefinitions[0].SortSelector(theme).Should().Be("Name");
            Themes.ColumnsDefinitions[1].SortSelector(theme).Should().Be(ThemeSource.Local.ToString());
            Themes.ColumnsDefinitions[2].SortSelector(theme).Should().Be("Name");
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

        private static ThemeCatalogItem CreateTheme(string id, string name, ThemeSource source)
        {
            return new ThemeCatalogItem(id, name, new ThemeDefinition { FontFamily = "Nunito Sans", Theme = new MudTheme() }, source, null);
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
                _content = System.Text.Encoding.UTF8.GetBytes(content);
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
