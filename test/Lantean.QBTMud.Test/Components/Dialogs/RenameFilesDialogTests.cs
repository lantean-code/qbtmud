using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using System.Net;
using System.Text.Json;
using MudPriority = Lantean.QBTMud.Models.Priority;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class RenameFilesDialogTests : RazorComponentTestBase<RenameFilesDialogTestHarness>
    {
        private const string PreferencesKey = "RenameFilesDialog.MultiRenamePreferences";
        private readonly RenameFilesDialogTestDriver _target;

        public RenameFilesDialogTests()
        {
            _target = new RenameFilesDialogTestDriver(TestContext);
        }

        [Fact]
        public void GIVEN_NoHash_WHEN_Rendered_THEN_DefaultStateAndFilesEmpty()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = _target.RenderComponent();

            dialog.Instance.GetFilesSnapshot().Should().BeEmpty();
            dialog.Instance.GetSelectedItems().Should().BeEmpty();
            dialog.Instance.SearchValue.Should().Be(string.Empty);
            dialog.Instance.UseRegexValue.Should().BeFalse();
            dialog.Instance.MatchAllOccurrencesValue.Should().BeFalse();
            dialog.Instance.CaseSensitiveValue.Should().BeFalse();
            dialog.Instance.ReplacementValue.Should().Be(string.Empty);
            dialog.Instance.AppliesToValueState.Should().Be(AppliesTo.FilenameExtension);
            dialog.Instance.IncludeFilesValue.Should().BeTrue();
            dialog.Instance.IncludeFoldersValue.Should().BeFalse();
            dialog.Instance.FileEnumerationStartValue.Should().Be(0);
            dialog.Instance.ReplaceAllValue.Should().BeFalse();
            dialog.Instance.RememberMultiRenameSettingsValue.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ColumnsDefinitions_WHEN_Accessed_THEN_DefaultsAvailable()
        {
            var columns = RenameFilesDialog.ColumnsDefinitions;

            columns.Should().HaveCount(2);
            columns.Select(c => c.Header).Should().Contain(new[] { "Name", "Replacement" });
        }

        [Fact]
        public async Task GIVEN_ReplaceAllEnabled_WHEN_Rendered_THEN_SubmitLabelShowsReplaceAll()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeReplaceAllChanged(true));
            dialog.Component.Render();

            var buttons = dialog.Component.FindAll("button");
            buttons.Any(button => button.TextContent.Contains("Replace all")).Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NestedRow_WHEN_Rendered_THEN_NameColumnUsesIndentedMargin()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("root", "root", -1, true, 0),
                CreateContentItem("root/child.txt", "child.txt", 1, false, 1),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");

            dialog.Component.WaitForAssertion(() =>
            {
                dialog.Component.Markup.Should().Contain("margin-left: 30px");
            });
        }

        [Fact]
        public async Task GIVEN_RememberedPreferencesAndHierarchy_WHEN_Rendered_THEN_PopulatesStateAndFlattensTree()
        {
            var preferencesJson = "{\"rememberPreferences\":true,\"search\":\"SearchValue\",\"useRegex\":true,\"matchAllOccurrences\":true,\"caseSensitive\":true,\"replace\":\"ReplaceValue\",\"appliesTo\":2,\"includeFiles\":false,\"includeFolders\":true,\"fileEnumerationStart\":5,\"replaceAll\":true}";
            await TestContext.LocalStorage.SetItemAsStringAsync(PreferencesKey, preferencesJson);

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

            var dialog = _target.RenderComponent("Hash");

            dialog.WaitForAssertion(() =>
            {
                dialog.Instance.GetFilesSnapshot().Count.Should().Be(4);
            });

            dialog.Instance.SearchValue.Should().Be("SearchValue");
            dialog.Instance.UseRegexValue.Should().BeTrue();
            dialog.Instance.MatchAllOccurrencesValue.Should().BeTrue();
            dialog.Instance.CaseSensitiveValue.Should().BeTrue();
            dialog.Instance.ReplacementValue.Should().Be("ReplaceValue");
            dialog.Instance.AppliesToValueState.Should().Be(AppliesTo.Extension);
            dialog.Instance.IncludeFilesValue.Should().BeFalse();
            dialog.Instance.IncludeFoldersValue.Should().BeTrue();
            dialog.Instance.FileEnumerationStartValue.Should().Be(5);
            dialog.Instance.ReplaceAllValue.Should().BeTrue();

            var names = dialog.Instance.GetFilesSnapshot().Select(f => f.Name).ToList();
            names.Should().Contain("root");
            names.Should().Contain("root/file.txt");
            names.Should().Contain("root/nested");
            names.Should().Contain("root/nested/file2.txt");
            names.Should().NotContain("root/empty");

            dialog.Instance.GetColumnsSnapshot().Should().HaveCount(2);
        }

        [Fact]
        public async Task GIVEN_FolderRow_WHEN_Rendered_THEN_NameColumnRenders()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("folder", "folder", -1, true, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = await _target.RenderDialogAsync("Hash");

            dialog.Component.WaitForAssertion(() =>
            {
                dialog.Component.Markup.Should().Contain("folder");
            });

            dialog.Component.FindAll(".mud-icon-root").Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_SortAndSelectionChanges_WHEN_Applied_THEN_UpdatesState()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("b.txt", "b.txt", 2, false, 0),
                CreateContentItem("a.txt", "a.txt", 1, false, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = _target.RenderComponent("Hash");

            dialog.WaitForAssertion(() =>
            {
                dialog.Instance.GetFilesSnapshot().Count.Should().Be(2);
            });

            dialog.Instance.InvokeSortColumnChanged("Name");
            dialog.Instance.InvokeSortDirectionChanged(SortDirection.Ascending);

            var files = dialog.Instance.GetFilesSnapshot().ToList();
            files[0].Name.Should().Be("a.txt");
            files[1].Name.Should().Be("b.txt");

            var selectedItems = new HashSet<FileRow> { CreateFileRow("a.txt", "a.txt", false, 0, string.Empty, false) };
            dialog.Instance.InvokeSelectedItemsChanged(selectedItems);
            dialog.Instance.GetSelectedItems().Should().BeEquivalentTo(selectedItems);
        }

        [Fact]
        public async Task GIVEN_RememberPreferencesEnabled_WHEN_OptionsChanged_THEN_PreferencesPersisted()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = _target.RenderComponent();

            dialog.Instance.InvokeSearchChanged("SearchValue");
            await dialog.Instance.InvokeRememberMultiRenameSettingsChanged(true);
            await dialog.Instance.InvokeUseRegexChanged(true);
            await dialog.Instance.InvokeMatchAllOccurrencesChanged(true);
            await dialog.Instance.InvokeCaseSensitiveChanged(true);
            await dialog.Instance.InvokeReplacementChanged("ReplacementValue");
            await dialog.Instance.InvokeAppliesToChanged(AppliesTo.Extension);
            await dialog.Instance.InvokeIncludeFilesChanged(false);
            await dialog.Instance.InvokeIncludeFoldersChanged(true);
            await dialog.Instance.InvokeFileEnumerationStartChanged(3);
            await dialog.Instance.InvokeReplaceAllChanged(true);

            dialog.Instance.SearchValue.Should().Be("SearchValue");
            dialog.Instance.UseRegexValue.Should().BeTrue();
            dialog.Instance.MatchAllOccurrencesValue.Should().BeTrue();
            dialog.Instance.CaseSensitiveValue.Should().BeTrue();
            dialog.Instance.ReplacementValue.Should().Be("ReplacementValue");
            dialog.Instance.AppliesToValueState.Should().Be(AppliesTo.Extension);
            dialog.Instance.IncludeFilesValue.Should().BeFalse();
            dialog.Instance.IncludeFoldersValue.Should().BeTrue();
            dialog.Instance.FileEnumerationStartValue.Should().Be(3);
            dialog.Instance.ReplaceAllValue.Should().BeTrue();
            dialog.Instance.RememberMultiRenameSettingsValue.Should().BeTrue();

            var json = await TestContext.LocalStorage.GetItemAsStringAsync(PreferencesKey);
            json.Should().NotBeNull();

            using var document = JsonDocument.Parse(json!);
            document.RootElement.GetProperty("rememberPreferences").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("useRegex").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("matchAllOccurrences").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("caseSensitive").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("replace").GetString().Should().Be("ReplacementValue");
            document.RootElement.GetProperty("appliesTo").GetInt32().Should().Be((int)AppliesTo.Extension);
            document.RootElement.GetProperty("includeFiles").GetBoolean().Should().BeFalse();
            document.RootElement.GetProperty("includeFolders").GetBoolean().Should().BeTrue();
            document.RootElement.GetProperty("fileEnumerationStart").GetInt32().Should().Be(3);
            document.RootElement.GetProperty("replaceAll").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RememberPreferencesDisabled_WHEN_OptionChanged_THEN_PreferencesCleared()
        {
            await TestContext.LocalStorage.SetItemAsStringAsync(PreferencesKey, "{\"rememberPreferences\":false}");
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = _target.RenderComponent();

            await dialog.Instance.InvokeRememberMultiRenameSettingsChanged(false);
            await dialog.Instance.InvokeUseRegexChanged(true);

            var json = await TestContext.LocalStorage.GetItemAsStringAsync(PreferencesKey);
            json.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Dialog_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeCancel());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NullHash_WHEN_Submitted_THEN_ResultOk()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = await _target.RenderDialogAsync(null);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmitAsync());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NullHash_WHEN_DoRenameInvoked_THEN_NoApiCalls()
        {
            TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);

            var dialog = _target.RenderComponent();

            await dialog.Instance.InvokeDoRenameAsync();

            dialog.Instance.GetFilesSnapshot().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ReplaceAllSelected_WHEN_Submitted_THEN_RenamesFilesAndFolders()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dialog = await _target.RenderDialogAsync("Hash");

            var file = CreateFileRow("oldfile.txt", "oldfile.txt", false, 0, string.Empty, false);
            var folder = CreateFileRow("oldfolder", "oldfolder", true, 0, "oldfolder/", false);
            dialog.Component.Instance.SetSelectedItems(new[] { file, folder });

            dialog.Component.Instance.InvokeSearchChanged("old");
            await dialog.Component.Instance.InvokeReplacementChanged("new");
            await dialog.Component.Instance.InvokeIncludeFilesChanged(true);
            await dialog.Component.Instance.InvokeIncludeFoldersChanged(true);
            await dialog.Component.Instance.InvokeReplaceAllChanged(true);

            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                dialog.Component.Instance.GetSelectedItems(),
                dialog.Component.Instance.SearchValue,
                dialog.Component.Instance.UseRegexValue,
                dialog.Component.Instance.ReplacementValue,
                dialog.Component.Instance.MatchAllOccurrencesValue,
                dialog.Component.Instance.CaseSensitiveValue,
                dialog.Component.Instance.AppliesToValueState,
                dialog.Component.Instance.IncludeFilesValue,
                dialog.Component.Instance.IncludeFoldersValue,
                dialog.Component.Instance.ReplaceAllValue,
                dialog.Component.Instance.FileEnumerationStartValue);

            var renamedFile = renamedFiles.Single(r => !r.IsFolder);
            var renamedFolder = renamedFiles.Single(r => r.IsFolder);

            var filePaths = GetReplaceAllPaths(renamedFile);
            var folderPaths = GetReplaceAllPaths(renamedFolder);

            apiClientMock.Setup(c => c.RenameFile("Hash", filePaths.OldPath, filePaths.NewPath)).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.RenameFolder("Hash", folderPaths.OldPath, folderPaths.NewPath)).Returns(Task.CompletedTask);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmitAsync());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_SingleFileSelected_WHEN_Submitted_THEN_RenamesFile()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dialog = await _target.RenderDialogAsync("Hash");

            var file = CreateFileRow("oldfile.txt", "oldfile.txt", false, 0, string.Empty, false);
            dialog.Component.Instance.SetSelectedItems(new[] { file });

            dialog.Component.Instance.InvokeSearchChanged("old");
            await dialog.Component.Instance.InvokeReplacementChanged("new");
            await dialog.Component.Instance.InvokeIncludeFilesChanged(true);
            await dialog.Component.Instance.InvokeReplaceAllChanged(false);

            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                dialog.Component.Instance.GetSelectedItems(),
                dialog.Component.Instance.SearchValue,
                dialog.Component.Instance.UseRegexValue,
                dialog.Component.Instance.ReplacementValue,
                dialog.Component.Instance.MatchAllOccurrencesValue,
                dialog.Component.Instance.CaseSensitiveValue,
                dialog.Component.Instance.AppliesToValueState,
                dialog.Component.Instance.IncludeFilesValue,
                dialog.Component.Instance.IncludeFoldersValue,
                dialog.Component.Instance.ReplaceAllValue,
                dialog.Component.Instance.FileEnumerationStartValue);

            var renamedFile = renamedFiles.Single();
            var filePaths = GetReplaceAllPaths(renamedFile);

            apiClientMock.Setup(c => c.RenameFile("Hash", filePaths.OldPath, filePaths.NewPath)).Returns(Task.CompletedTask);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmitAsync());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_SingleFolderSelected_WHEN_Submitted_THEN_RenamesFolder()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dialog = await _target.RenderDialogAsync("Hash");

            var folder = CreateFileRow("oldfolder", "oldfolder", true, 0, "oldfolder/", false);
            dialog.Component.Instance.SetSelectedItems(new[] { folder });

            dialog.Component.Instance.InvokeSearchChanged("old");
            await dialog.Component.Instance.InvokeReplacementChanged("new");
            await dialog.Component.Instance.InvokeIncludeFoldersChanged(true);
            await dialog.Component.Instance.InvokeReplaceAllChanged(false);

            var renamedFiles = FileNameMatcher.GetRenamedFiles(
                dialog.Component.Instance.GetSelectedItems(),
                dialog.Component.Instance.SearchValue,
                dialog.Component.Instance.UseRegexValue,
                dialog.Component.Instance.ReplacementValue,
                dialog.Component.Instance.MatchAllOccurrencesValue,
                dialog.Component.Instance.CaseSensitiveValue,
                dialog.Component.Instance.AppliesToValueState,
                dialog.Component.Instance.IncludeFilesValue,
                dialog.Component.Instance.IncludeFoldersValue,
                dialog.Component.Instance.ReplaceAllValue,
                dialog.Component.Instance.FileEnumerationStartValue);

            var renamedFolder = renamedFiles.Single();
            var folderPaths = GetReplaceAllPaths(renamedFolder);

            apiClientMock.Setup(c => c.RenameFolder("Hash", folderPaths.OldPath, folderPaths.NewPath)).Returns(Task.CompletedTask);

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmitAsync());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_NoRenamedItems_WHEN_DoRenameInvoked_THEN_DoesNothing()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dialog = _target.RenderComponent("Hash");

            dialog.WaitForAssertion(() =>
            {
                apiClientMock.Verify(c => c.GetTorrentContents("Hash", It.IsAny<int[]>()), Times.Once);
            });

            await dialog.Instance.InvokeDoRenameAsync();

            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_RenamedFolder_WHEN_DoRenameInvoked_THEN_RenamesFolder()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("folder", "folder", -1, true, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = _target.RenderComponent("Hash");

            dialog.WaitForAssertion(() =>
            {
                dialog.Instance.GetFilesSnapshot().Count.Should().Be(1);
            });

            var folder = CreateFileRow("folder", "folder", true, 0, "folder", true);
            folder.NewName = "folder-new";
            dialog.Instance.SetSelectedItems(new[] { folder });

            dialog.Instance.InvokeSearchChanged("folder");
            await dialog.Instance.InvokeReplacementChanged("folder-new");
            await dialog.Instance.InvokeIncludeFoldersChanged(true);

            apiClientMock.Setup(c => c.RenameFile("Hash", "folder", "folder-new")).Returns(Task.CompletedTask);

            await dialog.Instance.InvokeDoRenameAsync();

            folder.Renamed.Should().BeTrue();
            folder.ErrorMessage.Should().BeNull();

            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_RenameWithErrors_WHEN_DoRenameInvoked_THEN_UpdatesResults()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("oldfile.txt", "oldfile.txt", 1, false, 0),
                CreateContentItem("olderror.txt", "olderror.txt", 2, false, 0),
                CreateContentItem("oldfolder", "oldfolder", 3, true, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = _target.RenderComponent("Hash");

            var fileSuccess = CreateFileRow("oldfile.txt", "oldfile.txt", false, 0, string.Empty, true);
            var fileError = CreateFileRow("olderror.txt", "olderror.txt", false, 0, string.Empty, true);
            var folderError = CreateFileRow("oldfolder", "oldfolder", true, 0, string.Empty, true);
            dialog.Instance.SetSelectedItems(new[] { fileSuccess, fileError, folderError });

            dialog.Instance.InvokeSearchChanged("old");
            await dialog.Instance.InvokeReplacementChanged("new");
            await dialog.Instance.InvokeIncludeFilesChanged(true);
            await dialog.Instance.InvokeIncludeFoldersChanged(true);

            FileNameMatcher.GetRenamedFiles(
                dialog.Instance.GetSelectedItems(),
                dialog.Instance.SearchValue,
                dialog.Instance.UseRegexValue,
                dialog.Instance.ReplacementValue,
                dialog.Instance.MatchAllOccurrencesValue,
                dialog.Instance.CaseSensitiveValue,
                dialog.Instance.AppliesToValueState,
                dialog.Instance.IncludeFilesValue,
                dialog.Instance.IncludeFoldersValue,
                dialog.Instance.ReplaceAllValue,
                dialog.Instance.FileEnumerationStartValue);

            var successPaths = GetRenameItemPaths(fileSuccess);
            var errorPaths = GetRenameItemPaths(fileError);
            var folderErrorPaths = GetRenameItemPaths(folderError);

            apiClientMock.Setup(c => c.RenameFile("Hash", successPaths.OldPath, successPaths.NewPath)).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.RenameFile("Hash", errorPaths.OldPath, errorPaths.NewPath)).ThrowsAsync(new HttpRequestException("Problem", null, HttpStatusCode.BadRequest));
            apiClientMock.Setup(c => c.RenameFile("Hash", folderErrorPaths.OldPath, folderErrorPaths.NewPath)).ThrowsAsync(new HttpRequestException(string.Empty, null, HttpStatusCode.BadRequest));

            await dialog.Instance.InvokeDoRenameAsync();

            fileSuccess.Renamed.Should().BeTrue();
            fileSuccess.ErrorMessage.Should().BeNull();
            fileError.Renamed.Should().BeFalse();
            fileError.ErrorMessage.Should().Be("Problem");
            folderError.Renamed.Should().BeFalse();
            folderError.ErrorMessage.Should().Be("Error with request: BadRequest.");

            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_NoRenameChanges_WHEN_DoRenameInvoked_THEN_SetsRenamedFalse()
        {
            var apiClientMock = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetTorrentContents("Hash", It.IsAny<int[]>())).ReturnsAsync(Array.Empty<FileData>());

            var dataManagerMock = TestContext.AddSingletonMock<ITorrentDataManager>(MockBehavior.Strict);
            var contentItems = new[]
            {
                CreateContentItem("same.txt", "same.txt", 1, false, 0),
            };
            dataManagerMock
                .Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>()))
                .Returns(CreateContentMap(contentItems));

            var dialog = _target.RenderComponent("Hash");

            var file = CreateFileRow("same.txt", "same.txt", false, 0, string.Empty, true);
            dialog.Instance.SetSelectedItems(new[] { file });

            dialog.Instance.InvokeSearchChanged("same");
            await dialog.Instance.InvokeReplacementChanged("same");
            await dialog.Instance.InvokeIncludeFilesChanged(true);

            await dialog.Instance.InvokeDoRenameAsync();

            file.Renamed.Should().BeFalse();
            file.ErrorMessage.Should().BeNull();

            apiClientMock.VerifyAll();
        }

        private static ContentItem CreateContentItem(string name, string displayName, int index, bool isFolder, int level)
        {
            return new ContentItem(name, displayName, index, MudPriority.Normal, 0, 0, 0, isFolder, level, 0);
        }

        private static Dictionary<string, ContentItem> CreateContentMap(IEnumerable<ContentItem> items)
        {
            return items.ToDictionary(item => item.Name, StringComparer.Ordinal);
        }

        private static FileRow CreateFileRow(string name, string originalName, bool isFolder, int level, string path, bool renamed)
        {
            return new FileRow
            {
                Name = name,
                OriginalName = originalName,
                NewName = originalName,
                IsFolder = isFolder,
                Level = level,
                Path = path,
                Renamed = renamed,
            };
        }

        private static (string OldPath, string NewPath) GetReplaceAllPaths(FileRow row)
        {
            var oldPath = row.Path + row.OriginalName;
            var newPath = row.Path + row.NewName;
            return (oldPath, newPath);
        }

        private static (string OldPath, string NewPath) GetRenameItemPaths(FileRow row)
        {
            var parentPath = Path.GetDirectoryName(row.Name);
            var oldPath = string.IsNullOrEmpty(parentPath)
                ? row.OriginalName
                : Path.Combine(parentPath, row.OriginalName);
            var newPath = string.IsNullOrEmpty(parentPath)
                ? row.NewName ?? string.Empty
                : Path.Combine(parentPath, row.NewName ?? string.Empty);
            return (oldPath, newPath);
        }
    }

    public sealed class RenameFilesDialogTestHarness : RenameFilesDialog
    {
        public IReadOnlyList<FileRow> GetFilesSnapshot()
        {
            return Files.ToList();
        }

        public IReadOnlyList<ColumnDefinition<FileRow>> GetColumnsSnapshot()
        {
            return Columns.ToList();
        }

        public HashSet<FileRow> GetSelectedItems()
        {
            return SelectedItems;
        }

        public string SearchValue
        {
            get
            {
                return Search;
            }
        }

        public bool UseRegexValue
        {
            get
            {
                return UseRegex;
            }
        }

        public bool MatchAllOccurrencesValue
        {
            get
            {
                return MatchAllOccurrences;
            }
        }

        public bool CaseSensitiveValue
        {
            get
            {
                return CaseSensitive;
            }
        }

        public string ReplacementValue
        {
            get
            {
                return Replacement;
            }
        }

        public AppliesTo AppliesToValueState
        {
            get
            {
                return base.AppliesToValue;
            }
        }

        public bool IncludeFilesValue
        {
            get
            {
                return IncludeFiles;
            }
        }

        public bool IncludeFoldersValue
        {
            get
            {
                return IncludeFolders;
            }
        }

        public int FileEnumerationStartValue
        {
            get
            {
                return FileEnumerationStart;
            }
        }

        public bool ReplaceAllValue
        {
            get
            {
                return ReplaceAll;
            }
        }

        public bool RememberMultiRenameSettingsValue
        {
            get
            {
                return RememberMultiRenameSettings;
            }
        }

        public void SetSelectedItems(IEnumerable<FileRow> items)
        {
            SelectedItems = new HashSet<FileRow>(items);
        }

        public void InvokeSearchChanged(string value)
        {
            SearchChanged(value);
        }

        public Task InvokeUseRegexChanged(bool value)
        {
            return UseRegexChanged(value);
        }

        public Task InvokeMatchAllOccurrencesChanged(bool value)
        {
            return MatchAllOccurrencesChanged(value);
        }

        public Task InvokeCaseSensitiveChanged(bool value)
        {
            return CaseSensitiveChanged(value);
        }

        public Task InvokeReplacementChanged(string value)
        {
            return ReplacementChanged(value);
        }

        public Task InvokeAppliesToChanged(AppliesTo value)
        {
            return AppliesToChanged(value);
        }

        public Task InvokeIncludeFilesChanged(bool value)
        {
            return IncludeFilesChanged(value);
        }

        public Task InvokeIncludeFoldersChanged(bool value)
        {
            return IncludeFoldersChanged(value);
        }

        public Task InvokeFileEnumerationStartChanged(int value)
        {
            return FileEnumerationStartChanged(value);
        }

        public Task InvokeReplaceAllChanged(bool value)
        {
            return ReplaceAllChanged(value);
        }

        public Task InvokeRememberMultiRenameSettingsChanged(bool value)
        {
            return RememberMultiRenameSettingsChanged(value);
        }

        public void InvokeSortColumnChanged(string value)
        {
            SortColumnChanged(value);
        }

        public void InvokeSortDirectionChanged(SortDirection value)
        {
            SortDirectionChanged(value);
        }

        public void InvokeSelectedItemsChanged(HashSet<FileRow> items)
        {
            SelectedItemsChanged(items);
        }

        public Task InvokeSubmitAsync()
        {
            return Submit();
        }

        public Task InvokeDoRenameAsync()
        {
            return DoRename();
        }

        public void InvokeCancel()
        {
            Cancel();
        }
    }

    internal sealed class RenameFilesDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public RenameFilesDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public IRenderedComponent<RenameFilesDialogTestHarness> RenderComponent(string? hash = null)
        {
            if (hash is null)
            {
                return _testContext.Render<RenameFilesDialogTestHarness>();
            }

            return _testContext.Render<RenameFilesDialogTestHarness>(parameters =>
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

            var reference = await dialogService.ShowAsync<RenameFilesDialogTestHarness>("Rename Files", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<RenameFilesDialogTestHarness>();

            return new DialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class DialogRenderContext
    {
        public DialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<RenameFilesDialogTestHarness> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<RenameFilesDialogTestHarness> Component { get; }

        public IDialogReference Reference { get; }
    }
}
