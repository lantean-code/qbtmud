using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using System.Text.Json;
using MudPriority = Lantean.QBTMud.Models.Priority;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class RenameFilesDialogTests : RazorComponentTestBase<RenameFilesDialog>
    {
        private const string PreferencesKey = "RenameFilesDialog.MultiRenamePreferences";
        private readonly RenameFilesDialogTestDriver _target;

        public RenameFilesDialogTests()
        {
            _target = new RenameFilesDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoHash_WHEN_Rendered_THEN_DefaultStateAndFilesEmpty()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);
            var component = dialog.Component;

            FindSwitch(component, "RenameFilesRemember").Instance.Value.Should().BeFalse();
            FindTextField(component, "RenameFilesSearch").Instance.GetState(x => x.Value).Should().Be(string.Empty);
            FindSwitch(component, "RenameFilesUseRegex").Instance.Value.Should().BeFalse();
            FindSwitch(component, "RenameFilesMatchAll").Instance.Value.Should().BeFalse();
            FindSwitch(component, "RenameFilesCaseSensitive").Instance.Value.Should().BeFalse();
            FindTextField(component, "RenameFilesReplacement").Instance.GetState(x => x.Value).Should().Be(string.Empty);
            FindSelect<AppliesTo>(component, "RenameFilesAppliesTo").Instance.GetState(x => x.Value).Should().Be(AppliesTo.FilenameExtension);
            FindSwitch(component, "RenameFilesIncludeFiles").Instance.Value.Should().BeTrue();
            FindSwitch(component, "RenameFilesIncludeFolders").Instance.Value.Should().BeFalse();
            FindNumericField(component, "RenameFilesEnumerate").Instance.GetState(x => x.Value).Should().Be(0);
            FindSelect<bool>(component, "RenameFilesReplaceType").Instance.GetState(x => x.Value).Should().BeFalse();

            var table = FindTable(component);
            table.Instance.Items.Should().NotBeNull();
            table.Instance.Items!.Should().BeEmpty();

            GetChildContentText(FindButton(component, "RenameFilesSubmit").Instance.ChildContent).Should().Be("Replace");
        }

        [Fact]
        public async Task GIVEN_ColumnsDefinitions_WHEN_Rendered_THEN_DefaultsAvailable()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);
            var table = FindTable(dialog.Component);
            var columns = table.Instance.ColumnDefinitions.ToList();

            columns.Should().HaveCount(2);
            columns.Select(c => c.Header).Should().Contain(new[] { "Original", "Renamed" });
        }

        [Fact]
        public async Task GIVEN_RememberedPreferencesAndHierarchy_WHEN_Rendered_THEN_PopulatesStateAndFlattensTree()
        {
            var preferencesJson = "{\"rememberPreferences\":true,\"search\":\"Search\",\"useRegex\":true,\"matchAllOccurrences\":true,\"caseSensitive\":true,\"replace\":\"Replacement\",\"appliesTo\":2,\"includeFiles\":false,\"includeFolders\":true,\"fileEnumerationStart\":5,\"replaceAll\":true}";
            await TestContext.LocalStorage.SetItemAsStringAsync(PreferencesKey, preferencesJson, Xunit.TestContext.Current.CancellationToken);

            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("root", "root", -1, true, 0),
                CreateContentItem("root/empty", "empty", -2, true, 1),
                CreateContentItem("root/file.txt", "file.txt", 1, false, 1),
                CreateContentItem("root/nested", "nested", -3, true, 1),
                CreateContentItem("root/nested/file2.txt", "file2.txt", 2, false, 2),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");
            var component = dialog.Component;
            var table = FindTable(component);

            component.WaitForAssertion(() =>
            {
                table.Instance.Items.Should().NotBeNull();
                table.Instance.Items!.Count().Should().Be(4);
            });

            FindTextField(component, "RenameFilesSearch").Instance.GetState(x => x.Value).Should().Be("Search");
            FindSwitch(component, "RenameFilesUseRegex").Instance.Value.Should().BeTrue();
            FindSwitch(component, "RenameFilesMatchAll").Instance.Value.Should().BeTrue();
            FindSwitch(component, "RenameFilesCaseSensitive").Instance.Value.Should().BeTrue();
            FindTextField(component, "RenameFilesReplacement").Instance.GetState(x => x.Value).Should().Be("Replacement");
            FindSelect<AppliesTo>(component, "RenameFilesAppliesTo").Instance.GetState(x => x.Value).Should().Be(AppliesTo.Extension);
            FindSwitch(component, "RenameFilesIncludeFiles").Instance.Value.Should().BeFalse();
            FindSwitch(component, "RenameFilesIncludeFolders").Instance.Value.Should().BeTrue();
            FindNumericField(component, "RenameFilesEnumerate").Instance.GetState(x => x.Value).Should().Be(5);
            FindSelect<bool>(component, "RenameFilesReplaceType").Instance.GetState(x => x.Value).Should().BeTrue();

            var names = table.Instance.Items!.Select(item => item.Name).ToList();
            names.Should().Contain(new[] { "root", "root/file.txt", "root/nested", "root/nested/file2.txt" });
            names.Should().NotContain("root/empty");

            component.FindComponents<MudIcon>().Any(icon => icon.Instance.Icon == Icons.Material.Filled.Folder).Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RememberPreferencesEnabled_WHEN_OptionsChanged_THEN_PreferencesPersisted()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);
            var component = dialog.Component;

            await SetTextFieldValue(component, "RenameFilesSearch", "Search");
            await SetSwitchValue(component, "RenameFilesRemember", true);
            await SetSwitchValue(component, "RenameFilesUseRegex", true);
            await SetSwitchValue(component, "RenameFilesMatchAll", true);
            await SetSwitchValue(component, "RenameFilesCaseSensitive", true);
            await SetTextFieldValue(component, "RenameFilesReplacement", "Replacement");
            await SetSelectValue(component, "RenameFilesAppliesTo", AppliesTo.Extension);
            await SetSwitchValue(component, "RenameFilesIncludeFiles", false);
            await SetSwitchValue(component, "RenameFilesIncludeFolders", true);
            await SetNumericValue(component, "RenameFilesEnumerate", 3);
            await SetSelectValue(component, "RenameFilesReplaceType", true);

            FindTextField(component, "RenameFilesSearch").Instance.GetState(x => x.Value).Should().Be("Search");
            FindSwitch(component, "RenameFilesRemember").Instance.Value.Should().BeTrue();

            var json = await TestContext.LocalStorage.GetItemAsStringAsync(PreferencesKey, Xunit.TestContext.Current.CancellationToken);
            json.Should().NotBeNull();

            using var document = JsonDocument.Parse(json!);
            document.RootElement.GetProperty("rememberPreferences").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("useRegex").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("matchAllOccurrences").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("caseSensitive").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("replace").GetString().Should().Be("Replacement");
            document.RootElement.GetProperty("appliesTo").GetInt32().Should().Be((int)AppliesTo.Extension);
            document.RootElement.GetProperty("includeFiles").GetBoolean().Should().BeFalse();
            document.RootElement.GetProperty("includeFolders").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("fileEnumerationStart").GetInt32().Should().Be(3);
            document.RootElement.GetProperty("replaceAll").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RememberPreferencesDisabled_WHEN_OptionChanged_THEN_PreferencesCleared()
        {
            await TestContext.LocalStorage.SetItemAsStringAsync(PreferencesKey, "{\"rememberPreferences\":true}", Xunit.TestContext.Current.CancellationToken);
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);
            var component = dialog.Component;

            await SetSwitchValue(component, "RenameFilesRemember", false);
            await SetSwitchValue(component, "RenameFilesUseRegex", true);

            var json = await TestContext.LocalStorage.GetItemAsStringAsync(PreferencesKey, Xunit.TestContext.Current.CancellationToken);
            json.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_InvalidSortColumn_WHEN_FilesQueried_THEN_UsesNameSort()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("Beta.txt", "Beta.txt", 1, false, 0),
                CreateContentItem("Alpha.txt", "Alpha.txt", 2, false, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");
            var component = dialog.Component;
            var table = FindTable(component);

            component.WaitForAssertion(() =>
            {
                table.Instance.Items.Should().NotBeNull();
                table.Instance.Items!.Count().Should().Be(2);
            });

            await component.InvokeAsync(() => table.Instance.SortColumnChanged.InvokeAsync(string.Empty));
            component.Render();

            var names = table.Instance.Items!.Select(item => item.Name).ToList();
            names.Should().ContainInOrder("Alpha.txt", "Beta.txt");
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "RenameFilesClose");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NullHash_WHEN_Submitted_THEN_ResultOk()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "RenameFilesSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NoMatches_WHEN_Submitted_THEN_DoesNotRename()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("Name.txt", "Name.txt", 1, false, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");

            await ClickRowAsync(dialog.Component, "Name.txt", false);

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "RenameFilesSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            apiClientMock.Verify(c => c.RenameFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            apiClientMock.Verify(c => c.RenameFolder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ReplaceAllSelected_WHEN_Submitted_THEN_RenamesFilesAndFolders()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("NameFile.txt", "NameFile.txt", 1, false, 0),
                CreateContentItem("NameFolder/", "NameFolder", -1, true, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");

            await ClickRowAsync(dialog.Component, "NameFile.txt", false);
            await ClickRowAsync(dialog.Component, "NameFolder/", true);
            await SetTextFieldValue(dialog.Component, "RenameFilesSearch", "Name");
            await SetTextFieldValue(dialog.Component, "RenameFilesReplacement", "Replacement");
            await SetSwitchValue(dialog.Component, "RenameFilesIncludeFolders", true);
            await SetSelectValue(dialog.Component, "RenameFilesReplaceType", true);

            var selectedRows = new List<FileRow>
            {
                CreateFileRow(contentItems[0]),
                CreateFileRow(contentItems[1]),
            };

            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                selectedRows,
                "Name",
                false,
                "Replacement",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                true,
                true,
                0);

            var renamedFile = renamedFiles.Single(r => !r.IsFolder);
            var renamedFolder = renamedFiles.Single(r => r.IsFolder);

            var filePaths = GetReplaceAllPaths(renamedFile);
            var folderPaths = GetReplaceAllPaths(renamedFolder);

            apiClientMock.Setup(c => c.RenameFile("Hash", filePaths.OldPath, filePaths.NewPath)).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.RenameFolder("Hash", folderPaths.OldPath, folderPaths.NewPath)).Returns(Task.CompletedTask);

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "RenameFilesSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_SingleFileSelected_WHEN_Submitted_THEN_RenamesFile()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("NameFile.txt", "NameFile.txt", 1, false, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");

            await ClickRowAsync(dialog.Component, "NameFile.txt", false);
            await SetTextFieldValue(dialog.Component, "RenameFilesSearch", "Name");
            await SetTextFieldValue(dialog.Component, "RenameFilesReplacement", "Replacement");
            await SetSwitchValue(dialog.Component, "RenameFilesIncludeFiles", true);
            await SetSelectValue(dialog.Component, "RenameFilesReplaceType", false);

            var selectedRows = new List<FileRow>
            {
                CreateFileRow(contentItems[0]),
            };

            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                selectedRows,
                "Name",
                false,
                "Replacement",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                0);

            var renamedFile = renamedFiles.Single();
            var filePaths = GetReplaceAllPaths(renamedFile);

            apiClientMock.Setup(c => c.RenameFile("Hash", filePaths.OldPath, filePaths.NewPath)).Returns(Task.CompletedTask);

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "RenameFilesSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_SingleFolderSelected_WHEN_Submitted_THEN_RenamesFolder()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("NameFolder/", "NameFolder", -1, true, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");

            await ClickRowAsync(dialog.Component, "NameFolder/", false);
            await SetTextFieldValue(dialog.Component, "RenameFilesSearch", "Name");
            await SetTextFieldValue(dialog.Component, "RenameFilesReplacement", "Replacement");
            await SetSwitchValue(dialog.Component, "RenameFilesIncludeFolders", true);
            await SetSelectValue(dialog.Component, "RenameFilesReplaceType", false);

            var selectedRows = new List<FileRow>
            {
                CreateFileRow(contentItems[0]),
            };

            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                selectedRows,
                "Name",
                false,
                "Replacement",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                true,
                false,
                0);

            var renamedFolder = renamedFiles.Single();
            var folderPaths = GetReplaceAllPaths(renamedFolder);

            apiClientMock.Setup(c => c.RenameFolder("Hash", folderPaths.OldPath, folderPaths.NewPath)).Returns(Task.CompletedTask);

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "RenameFilesSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            apiClientMock.VerifyAll();
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<RenameFilesDialog> component, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(component, testId);
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumericField(IRenderedComponent<RenameFilesDialog> component, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(component, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<RenameFilesDialog> component, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(component, testId);
        }

        private static IRenderedComponent<DynamicTable<FileRow>> FindTable(IRenderedComponent<RenameFilesDialog> component)
        {
            return FindComponentByTestId<DynamicTable<FileRow>>(component, "RenameFilesTable");
        }

        private static async Task SetTextFieldValue(IRenderedComponent<RenameFilesDialog> component, string testId, string value)
        {
            var field = FindTextField(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetNumericValue(IRenderedComponent<RenameFilesDialog> component, string testId, int value)
        {
            var field = FindNumericField(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetSelectValue<T>(IRenderedComponent<RenameFilesDialog> component, string testId, T value)
        {
            var select = FindSelect<T>(component, testId);
            await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetSwitchValue(IRenderedComponent<RenameFilesDialog> component, string testId, bool value)
        {
            var field = FindSwitch(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task ClickRowAsync(IRenderedComponent<RenameFilesDialog> component, string name, bool ctrlKey)
        {
            var rowKey = name.Replace('/', '_');
            var element = component.Find($"[data-test-id='Row-RenameFiles-{rowKey}']");
            await element.ClickAsync(new MouseEventArgs { CtrlKey = ctrlKey });
        }

        private static ContentItem CreateContentItem(string name, string displayName, int index, bool isFolder, int level)
        {
            return new ContentItem(name, displayName, index, MudPriority.Normal, 0, 0, 0, isFolder, level, 0);
        }

        private static Dictionary<string, ContentItem> CreateContentMap(IEnumerable<ContentItem> items)
        {
            return items.ToDictionary(item => item.Name, StringComparer.Ordinal);
        }

        private static FileRow CreateFileRow(ContentItem item)
        {
            var fileRow = new FileRow
            {
                IsFolder = item.IsFolder,
                Level = item.Level,
                NewName = item.DisplayName,
                OriginalName = item.DisplayName,
                Name = item.Name,
                Path = item.Path,
            };

            return fileRow;
        }

        private static (string OldPath, string NewPath) GetReplaceAllPaths(FileRow row)
        {
            var oldPath = row.Path + row.OriginalName;
            var newPath = row.Path + row.NewName;
            return (oldPath, newPath);
        }
    }

    internal sealed class RenameFilesDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public RenameFilesDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public IRenderedComponent<RenameFilesDialog> RenderComponent(string? hash = null)
        {
            if (hash is null)
            {
                return _testContext.Render<RenameFilesDialog>();
            }

            return _testContext.Render<RenameFilesDialog>(parameters =>
            {
                parameters.Add(p => p.Hash, hash);
            });
        }

        public async Task<DialogRenderContext> RenderDialogAsync(string? hash)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();
            if (hash is not null)
            {
                parameters.Add(nameof(RenameFilesDialog.Hash), hash);
            }

            var reference = await dialogService.ShowAsync<RenameFilesDialog>("Rename Files", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<RenameFilesDialog>();

            return new DialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class DialogRenderContext
    {
        public DialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<RenameFilesDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<RenameFilesDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
