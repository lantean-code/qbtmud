using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using CategoryModel = Lantean.QBTMud.Models.Category;
using TorrentModel = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class TorrentActionsTests : RazorComponentTestBase<TorrentActions>
    {
        [Fact]
        public async Task GIVEN_MenuItems_WHEN_StartStopForceStartInvoked_THEN_ApiCalledAndSnackbarsShown()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock.Setup(c => c.StartTorrents(null, It.IsAny<string[]>())).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.StopTorrents(null, It.IsAny<string[]>())).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.SetForceStart(true, null, It.IsAny<string[]>())).Returns(Task.CompletedTask);

            var target = RenderMenuItems(Hashes("Hash"), Torrents(CreateTorrent("Hash", state: "stoppedDL")), Tags("Tag"), Categories("Category"));

            var start = FindMenuItem(target, "Start");
            var stop = FindMenuItem(target, "Stop");
            var forceStart = FindMenuItem(target, "Force start");

            await target.InvokeAsync(() => start.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => stop.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => forceStart.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.StartTorrents(null, It.Is<string[]>(a => a.SequenceEqual(Hashes("Hash")))), Times.Once);
            apiClientMock.Verify(c => c.StopTorrents(null, It.Is<string[]>(a => a.SequenceEqual(Hashes("Hash")))), Times.Once);
            apiClientMock.Verify(c => c.SetForceStart(true, null, It.Is<string[]>(a => a.SequenceEqual(Hashes("Hash")))), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(m => string.Equals(m, "Torrent started.", StringComparison.Ordinal)), Severity.Normal, null, null), Times.Once);
            snackbarMock.Verify(s => s.Add(It.Is<string>(m => m.Contains("stopped", StringComparison.OrdinalIgnoreCase)), Severity.Normal, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveAndRenameActions_WHEN_Invoked_THEN_DialogsAndNavigationExecuted()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.SetTorrentLocation("SavePath", null, Hashes("Hash"))).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.SetTorrentName("Name", "Hash")).Returns(Task.CompletedTask);
            TestContext.UseSnackbarMock();
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            dialogWorkflowMock.Setup(d => d.InvokeDeleteTorrentDialog(false, Hashes("Hash"))).ReturnsAsync(true);
            dialogWorkflowMock.Setup(d => d.InvokeStringFieldDialog("Set Location", "Location", "SavePath", It.IsAny<Func<string, Task>>())).Returns<string, string, string?, Func<string, Task>>((_, _, _, cb) => cb("SavePath"));
            dialogWorkflowMock.Setup(d => d.InvokeStringFieldDialog("Rename", "Name", "Name", It.IsAny<Func<string, Task>>())).Returns<string, string, string?, Func<string, Task>>((_, _, _, cb) => cb("Name"));
            dialogWorkflowMock.Setup(d => d.InvokeRenameFilesDialog("Hash")).Returns(Task.CompletedTask);

            var navigation = TestContext.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("http://localhost/other");

            var target = RenderMenuItems(Hashes("Hash"), Torrents(CreateTorrent("Hash")), Tags("Tag"), Categories("Category"));

            var remove = FindMenuItem(target, "Remove");
            var setLocation = FindMenuItem(target, "Set location");
            var rename = FindMenuItem(target, "Rename");
            var renameFiles = FindMenuItem(target, "Rename files");

            await target.InvokeAsync(() => setLocation.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => rename.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => renameFiles.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => remove.Instance.OnClick.InvokeAsync());

            navigation.Uri.Should().Be(navigation.BaseUri);
            dialogWorkflowMock.Verify(d => d.InvokeDeleteTorrentDialog(false, Hashes("Hash")), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AutoManagementAndLimits_WHEN_Clicked_THEN_CallsIssued()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var torrents = Torrents(
                CreateTorrent("One", automaticTorrentManagement: true, downloadLimit: 111, uploadLimit: 222),
                CreateTorrent("Two", automaticTorrentManagement: true, downloadLimit: 111, uploadLimit: 222));
            apiClientMock.Setup(c => c.SetAutomaticTorrentManagement(false, null, It.IsAny<string[]>())).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.SetAutomaticTorrentManagement(true, null, It.IsAny<string[]>())).Returns(Task.CompletedTask);
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            dialogWorkflowMock.Setup(d => d.InvokeDownloadRateDialog(111, Hashes("One", "Two"))).Returns(Task.CompletedTask);
            dialogWorkflowMock.Setup(d => d.InvokeUploadRateDialog(222, Hashes("One", "Two"))).Returns(Task.CompletedTask);
            dialogWorkflowMock.Setup(d => d.InvokeShareRatioDialog(It.Is<IReadOnlyList<TorrentModel>>(list => list.Count == 2))).Returns(Task.CompletedTask);
            TestContext.UseSnackbarMock();

            var target = RenderMenuItems(Hashes("One", "Two"), torrents, Tags("Tag"), Categories("Category"));

            var autoTmm = FindMenuItem(target, "Automatic Torrent Management");
            var limitDownload = FindMenuItem(target, "Limit download rate");
            var limitUpload = FindMenuItem(target, "Limit upload rate");
            var limitShareRatio = FindMenuItem(target, "Limit share ratio");

            await target.InvokeAsync(() => autoTmm.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => limitDownload.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => limitUpload.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => limitShareRatio.Instance.OnClick.InvokeAsync());

            torrents["One"].AutomaticTorrentManagement.Should().BeFalse();
            torrents["Two"].AutomaticTorrentManagement.Should().BeFalse();
            apiClientMock.Verify(c => c.SetAutomaticTorrentManagement(false, null, It.Is<string[]>(a => a.SequenceEqual(new[] { "One", "Two" }))), Times.Once);
            apiClientMock.Verify(c => c.SetAutomaticTorrentManagement(true, null, It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AutoTmmToggle_WHEN_ApiFails_THEN_StateRestored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.SetAutomaticTorrentManagement(false, null, Hashes("One", "Two"))).ThrowsAsync(new InvalidOperationException());
            TestContext.UseSnackbarMock();

            var torrents = Torrents(
                CreateTorrent("One", automaticTorrentManagement: true),
                CreateTorrent("Two", automaticTorrentManagement: true));

            var target = RenderMenuItems(Hashes("One", "Two"), torrents, Tags("Tag"), Categories("Category"));

            var autoTmm = FindMenuItem(target, "Automatic Torrent Management");

            await Assert.ThrowsAsync<InvalidOperationException>(() => target.InvokeAsync(() => autoTmm.Instance.OnClick.InvokeAsync()));

            torrents["One"].AutomaticTorrentManagement.Should().BeTrue();
            torrents["Two"].AutomaticTorrentManagement.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_SequentialAndForceActions_WHEN_Invoked_THEN_ApiCalled()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.ToggleSequentialDownload(null, It.IsAny<string[]>())).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.SetFirstLastPiecePriority(null, It.IsAny<string[]>())).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.ReannounceTorrents(null, It.IsAny<string[]>())).Returns(Task.CompletedTask);
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            dialogWorkflowMock.Setup(d => d.ForceRecheckAsync(Hashes("Alpha", "Beta"), false)).Returns(Task.CompletedTask);
            TestContext.UseSnackbarMock();

            var torrents = Torrents(
                CreateTorrent("Alpha"),
                CreateTorrent("Beta"));

            var target = RenderMenuItems(Hashes("Alpha", "Beta"), torrents, Tags("Tag"), Categories("Category"));

            var sequential = FindMenuItem(target, "Download in sequential order");
            var firstLast = FindMenuItem(target, "Download first and last pieces first");
            var recheck = FindMenuItem(target, "Force recheck");
            var reannounce = FindMenuItem(target, "Force reannounce");

            await target.InvokeAsync(() => sequential.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => firstLast.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => recheck.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => reannounce.Instance.OnClick.InvokeAsync());

            torrents["Alpha"].SequentialDownload.Should().BeTrue();
            torrents["Beta"].SequentialDownload.Should().BeTrue();
            torrents["Alpha"].FirstLastPiecePriority.Should().BeTrue();
            torrents["Beta"].FirstLastPiecePriority.Should().BeTrue();
            apiClientMock.Verify(c => c.ToggleSequentialDownload(null, Hashes("Alpha", "Beta")), Times.Once);
            apiClientMock.Verify(c => c.SetFirstLastPiecePriority(null, Hashes("Alpha", "Beta")), Times.Once);
            apiClientMock.Verify(c => c.ReannounceTorrents(null, Hashes("Alpha", "Beta")), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SequentialToggle_WHEN_ApiFails_THEN_StateRestored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.ToggleSequentialDownload(null, Hashes("Hash"))).ThrowsAsync(new InvalidOperationException());
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var torrents = Torrents(CreateTorrent("Hash", sequentialDownload: true));

            var target = RenderMenuItems(Hashes("Hash"), torrents, Tags("Tag"), Categories("Category"));

            var sequential = FindMenuItem(target, "Download in sequential order");

            await Assert.ThrowsAsync<InvalidOperationException>(() => target.InvokeAsync(() => sequential.Instance.OnClick.InvokeAsync()));

            torrents["Hash"].SequentialDownload.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FirstLastPiecePriorityToggle_WHEN_ApiFails_THEN_StateRestored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.SetFirstLastPiecePriority(null, Hashes("Hash"))).ThrowsAsync(new InvalidOperationException());
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var torrents = Torrents(CreateTorrent("Hash", firstLastPiecePriority: true));

            var target = RenderMenuItems(Hashes("Hash"), torrents, Tags("Tag"), Categories("Category"));

            var firstLast = FindMenuItem(target, "Download first and last pieces first");

            await Assert.ThrowsAsync<InvalidOperationException>(() => target.InvokeAsync(() => firstLast.Instance.OnClick.InvokeAsync()));

            torrents["Hash"].FirstLastPiecePriority.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_CopyAndExportActions_WHEN_Invoked_THEN_ClipboardAndExportCalled()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetExportUrl("Hash")).ReturnsAsync("http://export/torrent");
            TestContext.JSInterop.SetupVoid("qbt.copyTextToClipboard", _ => true).SetVoidResult();
            TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true).SetVoidResult();
            TestContext.UseSnackbarMock();

            var torrent = CreateTorrent("Hash", magnetUri: "Magnet", name: "Name");
            var torrents = Torrents(torrent);

            var copyTarget = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Children);
                parameters.Add(p => p.ParentAction, new UIAction("copy", "Copy", Icons.Material.Filled.FolderCopy, Color.Info, string.Empty));
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, torrents);
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var copyMagnet = copyTarget.FindComponents<MudListItem<string>>().Single(item => item.Markup.Contains("Magnet link", StringComparison.Ordinal));
            await copyTarget.InvokeAsync(() => copyMagnet.Instance.OnClick.InvokeAsync());

            var exportTarget = RenderMenuItems(Hashes("Hash"), torrents, Tags("Tag"), Categories("Category"));
            var export = FindMenuItem(exportTarget, "Export");
            await exportTarget.InvokeAsync(() => export.Instance.OnClick.InvokeAsync());

            TestContext.JSInterop.Invocations.Should().ContainSingle(inv => inv.Identifier == "qbt.copyTextToClipboard");
            TestContext.JSInterop.Invocations.Should().ContainSingle(inv => inv.Identifier == "qbt.triggerFileDownload");
        }

        [Fact]
        public async Task GIVEN_CopyChildren_WHEN_AllCopyOptionsInvoked_THEN_AllValuesCopied()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();
            TestContext.JSInterop.SetupVoid("qbt.copyTextToClipboard", _ => true).SetVoidResult();

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Children);
                parameters.Add(p => p.ParentAction, new UIAction("copy", "Copy", Icons.Material.Filled.FolderCopy, Color.Info, string.Empty));
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash", name: "Name", magnetUri: "Magnet", savePath: "SavePath")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var actionsMenu = TestContext.Render<MudMenu>();
            target.Instance.ActionsMenu = actionsMenu.Instance;

            var items = target.FindComponents<MudListItem<string>>();
            foreach (var item in items)
            {
                await target.InvokeAsync(() => item.Instance.OnClick.InvokeAsync());
            }

            TestContext.JSInterop.Invocations.Count(invocation => invocation.Identifier == "qbt.copyTextToClipboard").Should().Be(items.Count);
        }

        [Fact]
        public async Task GIVEN_TagAndCategoryChildren_WHEN_Toggled_THEN_ApiUpdatesState()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.RemoveTorrentTags(It.Is<IEnumerable<string>>(t => t.Single() == "Tag"), null, "Hash")).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.AddTorrentTags(It.Is<IEnumerable<string>>(t => t.Single() == "Tag"), null, "Other")).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.SetTorrentCategory(string.Empty, null, "Hash")).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.SetTorrentCategory("Category", null, "New")).Returns(Task.CompletedTask);
            TestContext.UseSnackbarMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();

            var removeTagTarget = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Children);
                parameters.Add(p => p.ParentAction, new UIAction("tags", "Tags", Icons.Material.Filled.Label, Color.Info, string.Empty));
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash", tags: new[] { "Tag" }, category: "Category")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var tagChild = removeTagTarget.FindComponents<MudListItem<string>>().First();
            await removeTagTarget.InvokeAsync(() => tagChild.Instance.OnClick.InvokeAsync());

            var addTagTarget = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Children);
                parameters.Add(p => p.ParentAction, new UIAction("tags", "Tags", Icons.Material.Filled.Label, Color.Info, string.Empty));
                parameters.Add(p => p.Hashes, Hashes("Other"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Other", tags: Array.Empty<string>(), category: string.Empty)));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var tagAddChild = addTagTarget.FindComponents<MudListItem<string>>().First();
            await addTagTarget.InvokeAsync(() => tagAddChild.Instance.OnClick.InvokeAsync());

            var categoryRemoveTarget = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Children);
                parameters.Add(p => p.ParentAction, new UIAction("category", "Category", Icons.Material.Filled.List, Color.Info, string.Empty));
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash", tags: new[] { "Tag" }, category: "Category")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var categoryRemoveChild = categoryRemoveTarget.FindComponents<MudListItem<string>>().First();
            await categoryRemoveTarget.InvokeAsync(() => categoryRemoveChild.Instance.OnClick.InvokeAsync());

            var categoryAddTarget = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Children);
                parameters.Add(p => p.ParentAction, new UIAction("category", "Category", Icons.Material.Filled.List, Color.Info, string.Empty));
                parameters.Add(p => p.Hashes, Hashes("New"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("New", tags: Array.Empty<string>(), category: string.Empty)));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var categoryAddChild = categoryAddTarget.FindComponents<MudListItem<string>>().First();
            await categoryAddTarget.InvokeAsync(() => categoryAddChild.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.RemoveTorrentTags(It.Is<IEnumerable<string>>(t => t.Single() == "Tag"), null, "Hash"), Times.Once);
            apiClientMock.Verify(c => c.AddTorrentTags(It.Is<IEnumerable<string>>(t => t.Single() == "Tag"), null, "Other"), Times.Once);
            apiClientMock.Verify(c => c.SetTorrentCategory(string.Empty, null, "Hash"), Times.Once);
            apiClientMock.Verify(c => c.SetTorrentCategory("Category", null, "New"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SuperSeedingChildren_WHEN_Clicked_THEN_TogglesState()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.SetSuperSeeding(false, null, Hashes("Done"))).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.SetSuperSeeding(true, null, Hashes("Other"))).Returns(Task.CompletedTask);
            TestContext.UseSnackbarMock();

            var torrents = Torrents(
                CreateTorrent("Done", progress: 1f, superSeeding: true),
                CreateTorrent("Other", progress: 1f, superSeeding: false));

            var target = RenderMenuItems(Hashes("Done", "Other"), torrents, Tags("Tag"), Categories("Category"));

            var superSeeding = FindMenuItem(target, "Super seeding mode");
            await target.InvokeAsync(() => superSeeding.Instance.OnClick.InvokeAsync());

            torrents["Done"].SuperSeeding.Should().BeFalse();
            torrents["Other"].SuperSeeding.Should().BeTrue();
            apiClientMock.Verify(c => c.SetSuperSeeding(false, null, Hashes("Done")), Times.Once);
            apiClientMock.Verify(c => c.SetSuperSeeding(true, null, Hashes("Other")), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SuperSeedingToggle_WHEN_ApiFails_THEN_StateRestored()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.SetSuperSeeding(false, null, Hashes("Done"))).ThrowsAsync(new InvalidOperationException());
            TestContext.UseSnackbarMock();

            var torrents = Torrents(
                CreateTorrent("Done", progress: 1f, superSeeding: true),
                CreateTorrent("Other", progress: 1f, superSeeding: false));

            var target = RenderMenuItems(Hashes("Done", "Other"), torrents, Tags("Tag"), Categories("Category"));

            var superSeeding = FindMenuItem(target, "Super seeding mode");

            await Assert.ThrowsAsync<InvalidOperationException>(() => target.InvokeAsync(() => superSeeding.Instance.OnClick.InvokeAsync()));

            torrents["Done"].SuperSeeding.Should().BeTrue();
            torrents["Other"].SuperSeeding.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_QueueChildren_WHEN_Clicked_THEN_PrioritiesAdjusted()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.MaxTorrentPriority(null, Hashes("Hash"))).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.IncreaseTorrentPriority(null, Hashes("Hash"))).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.DecreaseTorrentPriority(null, Hashes("Hash"))).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.MinTorrentPriority(null, Hashes("Hash"))).Returns(Task.CompletedTask);
            TestContext.UseSnackbarMock();

            var torrents = Torrents(CreateTorrent("Hash"));

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Children);
                parameters.Add(p => p.ParentAction, new UIAction("queue", "Queue", Icons.Material.Filled.Queue, Color.Info, string.Empty));
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, torrents);
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var items = target.FindComponents<MudListItem<string>>();
            await target.InvokeAsync(() => items[0].Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => items[1].Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => items[2].Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => items[3].Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.MaxTorrentPriority(null, Hashes("Hash")), Times.Once);
            apiClientMock.Verify(c => c.IncreaseTorrentPriority(null, Hashes("Hash")), Times.Once);
            apiClientMock.Verify(c => c.DecreaseTorrentPriority(null, Hashes("Hash")), Times.Once);
            apiClientMock.Verify(c => c.MinTorrentPriority(null, Hashes("Hash")), Times.Once);
        }

        [Fact]
        public void GIVEN_NoHashes_WHEN_Rendered_THEN_TagApplicationSkipped()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var target = RenderMenuItems(Array.Empty<string>(), Torrents(), Tags("Tag"), Categories("Category"));
            target.FindComponents<MudMenuItem>().Should().NotBeEmpty();
        }

        [Fact]
        public void GIVEN_NoTags_WHEN_Rendered_THEN_TagChildrenOmitted()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var target = RenderMenuItems(Hashes("Hash"), Torrents(CreateTorrent("Hash")), Tags(Array.Empty<string>()), Categories("Category"));
            target.FindComponents<MudMenuItem>().Should().NotBeEmpty();
        }

        [Fact]
        public void GIVEN_MissingTorrents_WHEN_Rendered_THEN_TagAndCategoryActionsSkipped()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var target = RenderMenuItems(Hashes("One", "Two"), new Dictionary<string, TorrentModel>(), Tags("Tag"), Categories("Category"));

            target.FindComponents<MudMenuItem>().Any(item => item.Markup.Contains("Tags", StringComparison.Ordinal)).Should().BeFalse();
            target.FindComponents<MudMenuItem>().Any(item => item.Markup.Contains("Category", StringComparison.Ordinal)).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ToolbarRenderTypes_WHEN_Rendered_THEN_ActionsAppear()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var toolbar = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Toolbar);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            toolbar.FindComponent<MudToolBar>().Should().NotBeNull();
            toolbar.FindComponents<MudIconButton>().Should().NotBeEmpty();

            var mixed = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.MixedToolbar);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            mixed.FindComponent<MudToolBar>().Should().NotBeNull();
            mixed.FindComponents<MudIconButton>().Should().NotBeEmpty();
        }

        [Fact]
        public void GIVEN_InitialIconsOnly_WHEN_Rendered_THEN_ShowsIconsAndMenu()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.InitialIconsOnly);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            target.FindComponents<MudIconButton>().Count.Should().BeGreaterThan(4);
            target.FindComponent<MudMenu>().Should().NotBeNull();
        }

        [Fact]
        public void GIVEN_ToolbarContents_WHEN_Rendered_THEN_NoToolbarWrapper()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.ToolbarContents);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            target.FindComponents<MudToolBar>().Should().BeEmpty();
            target.FindComponents<MudIconButton>().Should().NotBeEmpty();
        }

        [Fact]
        public void GIVEN_MixedToolbarContents_WHEN_Rendered_THEN_NoToolbarWrapper()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.MixedToolbarContents);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            target.FindComponents<MudToolBar>().Should().BeEmpty();
            target.FindComponents<MudMenu>().Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_MenuWithoutActivator_WHEN_OverlayChanges_THEN_MenuCloses()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock();

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.MenuWithoutActivator);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.PrimaryHash, "Hash");
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var overlay = target.FindComponent<MudOverlay>();
            await target.InvokeAsync(() => overlay.Instance.VisibleChanged.InvokeAsync(true));
            await target.InvokeAsync(() => overlay.Instance.VisibleChanged.InvokeAsync(false));
        }

        [Fact]
        public async Task GIVEN_MenuRenderType_WHEN_DeleteShortcutPressed_THEN_DeleteInvokedAndShortcutUnregistered()
        {
            var apiClientMock = TestContext.UseApiClientMock();
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            dialogWorkflowMock.Setup(d => d.InvokeDeleteTorrentDialog(false, Hashes("Hash"))).ReturnsAsync(true);
            var keyboardMock = TestContext.AddSingletonMock<IKeyboardService>(MockBehavior.Strict);
            Func<KeyboardEvent, Task>? deleteHandler = null;
            keyboardMock.Setup(k => k.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Delete"), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Returns<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    deleteHandler = handler;
                    return Task.CompletedTask;
                });
            keyboardMock.Setup(k => k.UnregisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Delete"))).Returns(Task.CompletedTask);
            TestContext.UseSnackbarMock();
            var navigation = TestContext.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("http://localhost/elsewhere");

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.Menu);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.PrimaryHash, "Hash");
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
            });

            var menu = target.FindComponent<MudMenu>();
            await target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs()));
            deleteHandler.Should().NotBeNull();

            await target.InvokeAsync(() => deleteHandler!(new KeyboardEvent("Delete")));
            await target.InvokeAsync(() => menu.Instance.CloseMenuAsync());

            dialogWorkflowMock.Verify(d => d.InvokeDeleteTorrentDialog(false, Hashes("Hash")), Times.Once);
            keyboardMock.Verify(k => k.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Delete"), It.IsAny<Func<KeyboardEvent, Task>>()), Times.Once);
            keyboardMock.Verify(k => k.UnregisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Delete")), Times.Once);
            await target.InvokeAsync(() => deleteHandler!(new KeyboardEvent("Delete")));
            target.Render(parameters => parameters.Add(p => p.RenderType, RenderType.MixedToolbar));
            await target.InvokeAsync(() => deleteHandler!(new KeyboardEvent("Delete")));
            navigation.Uri.Should().Be(navigation.BaseUri);
        }

        [Fact]
        public async Task GIVEN_MudDialogProvided_WHEN_ActionInvoked_THEN_DialogCloses()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.StartTorrents(null, Hashes("Hash"))).Returns(Task.CompletedTask);
            var dialogMock = new Mock<IMudDialogInstance>(MockBehavior.Strict);
            dialogMock.Setup(d => d.Close());
            TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.AddSingletonMock<IDialogWorkflow>();

            var target = TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.MenuItems);
                parameters.Add(p => p.Hashes, Hashes("Hash"));
                parameters.Add(p => p.Torrents, Torrents(CreateTorrent("Hash")));
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.Tags, Tags("Tag"));
                parameters.Add(p => p.Categories, Categories("Category"));
                parameters.Add(p => p.MudDialog, dialogMock.Object);
            });

            var start = FindMenuItem(target, "Start");
            await target.InvokeAsync(() => start.Instance.OnClick.InvokeAsync());

            dialogMock.Verify(d => d.Close(), Times.Once);
        }

        private IRenderedComponent<TorrentActions> RenderMenuItems(IEnumerable<string> hashes, Dictionary<string, TorrentModel> torrents, HashSet<string> tags, Dictionary<string, CategoryModel> categories, Preferences? preferences = null)
        {
            return TestContext.Render<TorrentActions>(parameters =>
            {
                parameters.Add(p => p.RenderType, RenderType.MenuItems);
                parameters.Add(p => p.Hashes, hashes);
                parameters.Add(p => p.Torrents, torrents);
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.Tags, tags);
                parameters.Add(p => p.Categories, categories);
            });
        }

        private static IRenderedComponent<MudMenuItem> FindMenuItem(IRenderedComponent<TorrentActions> target, string text)
        {
            var components = target.FindComponents<MudMenuItem>();
            var match = components.FirstOrDefault(item => item.Markup.Contains($">{text}<", StringComparison.Ordinal))
                ?? components.FirstOrDefault(item => item.Markup.Contains(text, StringComparison.Ordinal));
            if (match is null)
            {
                var available = string.Join(" | ", components.Select(c => c.Markup));
                throw new InvalidOperationException($"Menu item '{text}' not found. Available: {available}");
            }

            return match;
        }

        private static Dictionary<string, TorrentModel> Torrents(params TorrentModel[] torrents)
        {
            return torrents.ToDictionary(t => t.Hash, t => t);
        }

        private static HashSet<string> Tags(params string[] tags)
        {
            return new HashSet<string>(tags);
        }

        private static Dictionary<string, CategoryModel> Categories(params string[] categories)
        {
            return categories.ToDictionary(c => c, c => new CategoryModel(c, c));
        }

        private static string[] Hashes(params string[] hashes)
        {
            return hashes;
        }

        private static TorrentModel CreateTorrent(
            string hash,
            float progress = 0.5f,
            bool sequentialDownload = false,
            bool firstLastPiecePriority = false,
            bool forceStart = false,
            bool superSeeding = false,
            bool automaticTorrentManagement = true,
            string state = "stoppedDL",
            string category = "Category",
            IEnumerable<string>? tags = null,
            long downloadLimit = -1,
            long uploadLimit = -1,
            string savePath = "SavePath",
            string name = "Name",
            string magnetUri = "Magnet")
        {
            return new TorrentModel(
                hash,
                0,
                0,
                automaticTorrentManagement,
                0,
                category,
                0,
                0,
                "ContentPath",
                downloadLimit,
                0,
                0,
                0,
                0,
                firstLastPiecePriority,
                forceStart,
                "InfoHashV1",
                "InfoHashV2",
                0,
                magnetUri,
                0,
                0,
                name,
                0,
                0,
                0,
                0,
                0,
                progress,
                0,
                0,
                savePath,
                0,
                0,
                0,
                sequentialDownload,
                0,
                state,
                superSeeding,
                tags ?? Array.Empty<string>(),
                0,
                0,
                "Tracker",
                0,
                false,
                false,
                false,
                uploadLimit,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                "DownloadPath",
                "RootPath",
                false,
                ShareLimitAction.Default,
                "Comment");
        }
    }
}
