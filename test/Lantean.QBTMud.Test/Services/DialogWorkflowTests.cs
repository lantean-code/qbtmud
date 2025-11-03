using AwesomeAssertions;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using MudBlazor;
using MudCategory = Lantean.QBTMud.Models.Category;
using MudTorrent = Lantean.QBTMud.Models.Torrent;
using QbtCategory = Lantean.QBitTorrentClient.Models.Category;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class DialogWorkflowTests
    {
        private readonly Mock<IDialogService> _dialogService;
        private readonly Mock<IApiClient> _apiClient;
        private readonly Mock<ISnackbar> _snackbar;

        private readonly DialogWorkflow _target;

        public DialogWorkflowTests()
        {
            _dialogService = new Mock<IDialogService>(MockBehavior.Strict);
            _apiClient = new Mock<IApiClient>(MockBehavior.Strict);
            _snackbar = new Mock<ISnackbar>();

            _target = new DialogWorkflow(_dialogService.Object, _apiClient.Object, _snackbar.Object);
        }

        [Fact]
        public async Task GIVEN_CategoryCreated_WHEN_InvokeAddCategoryDialog_THEN_ShouldCallApi()
        {
            var reference = CreateReference(DialogResult.Ok(new MudCategory("Name", "SavePath")));
            _dialogService
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Add Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.AddCategory("Name", "SavePath"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeAddCategoryDialog();

            result.Should().Be("Name");
            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_InitialValues_WHEN_InvokeAddCategoryDialog_THEN_ShouldPopulateParameters()
        {
            DialogParameters? captured = null;
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Add Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference);

            var result = await _target.InvokeAddCategoryDialog("Category", "SavePath");

            result.Should().BeNull();
            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(CategoryPropertiesDialog.Category)).Should().BeTrue();
            captured[nameof(CategoryPropertiesDialog.Category)].Should().Be("Category");
            captured.Any(p => p.Key == nameof(CategoryPropertiesDialog.SavePath)).Should().BeTrue();
            captured[nameof(CategoryPropertiesDialog.SavePath)].Should().Be("SavePath");
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeAddCategoryDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Add Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.InvokeAddCategoryDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_FileOptions_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldUploadFilesAndDisposeStreams()
        {
            var streamOne = new TrackingStream();
            var streamTwo = new TrackingStream();
            var fileOne = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileOne.Setup(f => f.Name).Returns("Name");
            fileOne.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamOne);
            var fileTwo = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileTwo.Setup(f => f.Name).Returns("SecondName");
            fileTwo.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamTwo);

            var options = CreateTorrentOptions(false, false);
            options.ShareLimitAction = ShareLimitAction.Remove.ToString();
            var fileOptions = new AddTorrentFileOptions(new[] { fileOne.Object, fileTwo.Object }, options)
            {
                DownloadPath = "DownloadPath",
                InactiveSeedingTimeLimit = 4,
                RatioLimit = 5F,
                SeedingTimeLimit = 6,
                ShareLimitAction = ShareLimitAction.Remove.ToString(),
                UseDownloadPath = true,
                Tags = new[] { "Tags" }
            };

            var reference = CreateReference(DialogResult.Ok(fileOptions));
            _dialogService
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Upload local torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            AddTorrentParams? captured = null;
            _apiClient
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .Callback<AddTorrentParams>(p => captured = p)
                .ReturnsAsync(new AddTorrentResult(1, 1));

            await _target.InvokeAddTorrentFileDialog();

            captured.Should().NotBeNull();
            captured!.Torrents.Should().NotBeNull();
            captured.Torrents!.Count.Should().Be(2);
            captured.Torrents!.ContainsKey("Name").Should().BeTrue();
            captured.Torrents!["Name"].Should().BeSameAs(streamOne);
            captured.Torrents!.ContainsKey("SecondName").Should().BeTrue();
            captured.Torrents!["SecondName"].Should().BeSameAs(streamTwo);
            captured.AutoTorrentManagement.Should().BeFalse();
            captured.Category.Should().Be("Category");
            captured.DownloadLimit.Should().Be(2);
            captured.UploadLimit.Should().Be(3);
            captured.DownloadPath.Should().Be("DownloadPath");
            captured.UseDownloadPath.Should().BeTrue();
            captured.SavePath.Should().Be("SavePath");
            captured.Tags!.Single().Should().Be("Tags");
            captured.RatioLimit.Should().Be(5F);
            captured.SeedingTimeLimit.Should().Be(6);
            captured.InactiveSeedingTimeLimit.Should().Be(4);
            captured.ShareLimitAction.Should().Be(ShareLimitAction.Remove);
            captured.ContentLayout.Should().Be(TorrentContentLayout.Original);
            captured.StopCondition.Should().Be(StopCondition.MetadataReceived);
            captured.Stopped.Should().BeTrue();
            captured.AddToTopOfQueue.Should().BeTrue();

            streamOne.DisposeAsyncCalled.Should().BeTrue();
            streamTwo.DisposeAsyncCalled.Should().BeTrue();
            fileOne.VerifyAll();
            fileTwo.VerifyAll();

            var snackbarCall = _snackbar.Invocations.Single(i => i.Method.Name == nameof(ISnackbar.Add));
            snackbarCall.Arguments[0].Should().Be("Added torrent(s) and failed to add torrent(s).");
            snackbarCall.Arguments[1].Should().Be(Severity.Warning);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldNotUpload()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Upload local torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeAddTorrentFileDialog();

            _apiClient.Verify(a => a.AddTorrent(It.IsAny<AddTorrentParams>()), Times.Never);
            _snackbar.Invocations.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_LinkOptions_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldAddTorrent()
        {
            DialogParameters? capturedParameters = null;
            var options = CreateTorrentOptions(true, true);
            options.ShareLimitAction = ShareLimitAction.Remove.ToString();
            var linkOptions = new AddTorrentLinkOptions("http://one\nhttp://two", options)
            {
                DownloadPath = "DownloadPath",
                InactiveSeedingTimeLimit = 4,
                RatioLimit = 5F,
                SeedingTimeLimit = 6,
                ShareLimitAction = ShareLimitAction.Remove.ToString(),
                UseDownloadPath = true,
                Tags = new[] { "Tags" }
            };

            var reference = CreateReference(DialogResult.Ok(linkOptions));
            _dialogService
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => capturedParameters = parameters)
                .ReturnsAsync(reference);

            AddTorrentParams? captured = null;
            _apiClient
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .Callback<AddTorrentParams>(p => captured = p)
                .ReturnsAsync(new AddTorrentResult(1, 0, 0, new[] { "Hash" }));

            await _target.InvokeAddTorrentLinkDialog();

            capturedParameters.Should().NotBeNull();
            capturedParameters!.Any(p => p.Key == nameof(AddTorrentLinkDialog.Url)).Should().BeTrue();
            capturedParameters[nameof(AddTorrentLinkDialog.Url)].Should().BeNull();

            captured.Should().NotBeNull();
            captured!.Urls!.Should().BeEquivalentTo("http://one", "http://two");
            captured.Torrents.Should().BeNull();
            captured.AutoTorrentManagement.Should().BeTrue();
            captured.SavePath.Should().BeNull();
            captured.DownloadPath.Should().Be("DownloadPath");
            captured.UseDownloadPath.Should().BeTrue();
            captured.Tags!.Single().Should().Be("Tags");
            captured.RatioLimit.Should().Be(5F);
            captured.SeedingTimeLimit.Should().Be(6);
            captured.InactiveSeedingTimeLimit.Should().Be(4);
            captured.ShareLimitAction.Should().Be(ShareLimitAction.Remove);
            captured.StopCondition.Should().Be(StopCondition.MetadataReceived);
            captured.Stopped.Should().BeFalse();

            var snackbarCall = _snackbar.Invocations.Single(i => i.Method.Name == nameof(ISnackbar.Add));
            snackbarCall.Arguments[0].Should().Be("Added 1 torrent.");
            snackbarCall.Arguments[1].Should().Be(Severity.Success);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldNotAddTorrent()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeAddTorrentLinkDialog();

            _apiClient.Verify(a => a.AddTorrent(It.IsAny<AddTorrentParams>()), Times.Never);
            _snackbar.Invocations.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_AddTorrentFails_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldReportFailure()
        {
            var options = CreateTorrentOptions(true, true);
            var linkOptions = new AddTorrentLinkOptions("http://one", options);
            var reference = CreateReference(DialogResult.Ok(linkOptions));
            _dialogService
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(0, 1, 0, new[] { "Hash" }));

            await _target.InvokeAddTorrentLinkDialog();

            var snackbarCall = _snackbar.Invocations.Single(i => i.Method.Name == nameof(ISnackbar.Add));
            snackbarCall.Arguments[0].Should().Be("Failed to add 1 torrent.");
            snackbarCall.Arguments[1].Should().Be(Severity.Error);
        }

        [Fact]
        public async Task GIVEN_DeleteWithoutConfirmation_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldDelete()
        {
            _apiClient
                .Setup(a => a.DeleteTorrents(null, false, It.Is<string[]>(hashes => hashes.Length == 1 && hashes[0] == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeDeleteTorrentDialog(false, "Hash");

            result.Should().BeTrue();
            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldReturnFalse()
        {
            var result = await _target.InvokeDeleteTorrentDialog(true);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationDeclined_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldNotDelete()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<DeleteDialog>("Remove torrent?", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.InvokeDeleteTorrentDialog(true, "Hash");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationAccepted_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldDeleteWithFilesOption()
        {
            DialogParameters? captured = null;
            var reference = CreateReference(DialogResult.Ok(true));
            _dialogService
                .Setup(s => s.ShowAsync<DeleteDialog>("Remove torrent?", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.DeleteTorrents(null, true, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeDeleteTorrentDialog(true, "Hash");

            result.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(DeleteDialog.Count)).Should().BeTrue();
            captured[nameof(DeleteDialog.Count)].Should().Be(1);
            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_ForceRecheckAsync_THEN_ShouldNotInvokeApi()
        {
            await _target.ForceRecheckAsync(Array.Empty<string>(), false);

            _apiClient.Verify(a => a.RecheckTorrents(It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RecheckWithoutConfirmation_WHEN_ForceRecheckAsync_THEN_ShouldCallApi()
        {
            _apiClient
                .Setup(a => a.RecheckTorrents(null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.ForceRecheckAsync(new[] { "Hash" }, false);

            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_RecheckConfirmationDeclined_WHEN_ForceRecheckAsync_THEN_ShouldNotCallApi()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<ConfirmDialog>("Force recheck", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);

            await _target.ForceRecheckAsync(new[] { "Hash" }, true);

            _apiClient.Verify(a => a.RecheckTorrents(It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RecheckConfirmationAccepted_WHEN_ForceRecheckAsync_THEN_ShouldCallApi()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            _dialogService
                .Setup(s => s.ShowAsync<ConfirmDialog>("Force recheck", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.RecheckTorrents(null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.ForceRecheckAsync(new[] { "Hash" }, true);

            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_DialogConfirmed_WHEN_InvokeDownloadRateDialog_THEN_ShouldUpdateRate()
        {
            var reference = CreateReference(DialogResult.Ok(3L));
            _dialogService
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Download Rate", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.SetTorrentDownloadLimit(3072, null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeDownloadRateDialog(2048, new[] { "Hash" });

            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeDownloadRateDialog_THEN_ShouldNotUpdateRate()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Download Rate", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeDownloadRateDialog(2048, new[] { "Hash" });

            _apiClient.Verify(a => a.SetTorrentDownloadLimit(It.IsAny<long>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DialogConfirmed_WHEN_InvokeUploadRateDialog_THEN_ShouldUpdateRate()
        {
            var reference = CreateReference(DialogResult.Ok(4L));
            _dialogService
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Upload Rate", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.SetTorrentUploadLimit(4096, null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeUploadRateDialog(1024, new[] { "Hash" });

            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeUploadRateDialog_THEN_ShouldNotUpdateRate()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Upload Rate", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeUploadRateDialog(1024, new[] { "Hash" });

            _apiClient.Verify(a => a.SetTorrentUploadLimit(It.IsAny<long>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_CategoryFound_WHEN_InvokeEditCategoryDialog_THEN_ShouldCallApi()
        {
            DialogParameters? captured = null;
            var categories = new Dictionary<string, QbtCategory>
            {
                { "Category", new QbtCategory("Category", "SavePath", new DownloadPathOption(true, "DownloadPath")) }
            };
            _apiClient
                .Setup(a => a.GetAllCategories())
                .ReturnsAsync(categories);
            var reference = CreateReference(DialogResult.Ok(new MudCategory("Name", "SavePath")));
            _dialogService
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Edit Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.EditCategory("Name", "SavePath"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeEditCategoryDialog("Category");

            result.Should().Be("Name");
            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(CategoryPropertiesDialog.Category)).Should().BeTrue();
            captured[nameof(CategoryPropertiesDialog.Category)].Should().Be("Category");
            captured.Any(p => p.Key == nameof(CategoryPropertiesDialog.SavePath)).Should().BeTrue();
            captured[nameof(CategoryPropertiesDialog.SavePath)].Should().Be("SavePath");
            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeEditCategoryDialog_THEN_ShouldReturnNull()
        {
            var categories = new Dictionary<string, QbtCategory>();
            _apiClient
                .Setup(a => a.GetAllCategories())
                .ReturnsAsync(categories);
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Edit Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.InvokeEditCategoryDialog("Category");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_InvokeRenameFilesDialog_THEN_ShouldForwardParameters()
        {
            DialogParameters? captured = null;
            var reference = new Mock<IDialogReference>();
            _dialogService
                .Setup(s => s.ShowAsync<RenameFilesDialog>("Rename Files", It.IsAny<DialogParameters>(), DialogWorkflow.FullScreenDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference.Object);

            await _target.InvokeRenameFilesDialog("Hash");

            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(RenameFilesDialog.Hash)).Should().BeTrue();
            captured[nameof(RenameFilesDialog.Hash)].Should().Be("Hash");
        }

        [Fact]
        public async Task GIVEN_InvokeRssRulesDialog_WHEN_Executed_THEN_ShouldOpenDialog()
        {
            var reference = new Mock<IDialogReference>();
            _dialogService
                .Setup(s => s.ShowAsync<RssRulesDialog>("Edit Rss Auto Downloading Rules", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference.Object)
                .Verifiable();

            await _target.InvokeRssRulesDialog();

            _dialogService.Verify();
        }

        [Fact]
        public async Task GIVEN_NoTorrents_WHEN_InvokeShareRatioDialog_THEN_ShouldReturnImmediately()
        {
            await _target.InvokeShareRatioDialog(Enumerable.Empty<MudTorrent>());

            _dialogService.Invocations.Should().BeEmpty();
            _apiClient.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_TorrentsWithDistinctShareRatios_WHEN_InvokeShareRatioDialogConfirmed_THEN_ShouldUpdateShareLimits()
        {
            var torrents = new[]
            {
                CreateTorrent("Hash", 2F, 3, 4F, ShareLimitAction.Stop),
                CreateTorrent("SecondHash", 3F, 3, 4F, ShareLimitAction.Remove)
            };

            DialogParameters? captured = null;
            var reference = CreateReference(DialogResult.Ok(new ShareRatio
            {
                RatioLimit = 5F,
                SeedingTimeLimit = 6F,
                InactiveSeedingTimeLimit = 7F,
                ShareLimitAction = ShareLimitAction.Remove
            }));
            _dialogService
                .Setup(s => s.ShowAsync<ShareRatioDialog>("Share ratio", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.SetTorrentShareLimit(5F, 6F, 7F, ShareLimitAction.Remove, null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash", "SecondHash" }))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeShareRatioDialog(torrents);

            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(ShareRatioDialog.Value)).Should().BeTrue();
            captured[nameof(ShareRatioDialog.Value)].Should().BeNull();
            captured.Any(p => p.Key == nameof(ShareRatioDialog.CurrentValue)).Should().BeTrue();
            captured[nameof(ShareRatioDialog.CurrentValue)].Should().BeAssignableTo<ShareRatioMax>();
            var currentValue = (ShareRatioMax)captured[nameof(ShareRatioDialog.CurrentValue)]!;
            currentValue.RatioLimit.Should().Be(2F);
            currentValue.SeedingTimeLimit.Should().Be(3);
            currentValue.InactiveSeedingTimeLimit.Should().Be(4F);
            currentValue.ShareLimitAction.Should().Be(ShareLimitAction.Stop);
            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_TorrentsWithMatchingShareRatios_WHEN_InvokeShareRatioDialogConfirmed_THEN_ShouldProvideExistingValue()
        {
            var torrents = new[]
            {
                CreateTorrent("Hash", 2F, 3, 4F, ShareLimitAction.Stop),
                CreateTorrent("SecondHash", 2F, 3, 4F, ShareLimitAction.Stop)
            };

            DialogParameters? captured = null;
            var reference = CreateReference(DialogResult.Ok(new ShareRatio
            {
                RatioLimit = 5F,
                SeedingTimeLimit = 6F,
                InactiveSeedingTimeLimit = 7F,
                ShareLimitAction = ShareLimitAction.Remove
            }));
            _dialogService
                .Setup(s => s.ShowAsync<ShareRatioDialog>("Share ratio", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.SetTorrentShareLimit(5F, 6F, 7F, ShareLimitAction.Remove, null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash", "SecondHash" }))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeShareRatioDialog(torrents);

            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(ShareRatioDialog.Value)).Should().BeTrue();
            captured[nameof(ShareRatioDialog.Value)].Should().BeAssignableTo<ShareRatioMax>();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeShareRatioDialog_THEN_ShouldNotUpdateLimits()
        {
            var torrents = new[] { CreateTorrent("Hash", 2F, 3, 4F, ShareLimitAction.Stop) };
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<ShareRatioDialog>("Share ratio", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeShareRatioDialog(torrents);

            _apiClient.Verify(a => a.SetTorrentShareLimit(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<ShareLimitAction?>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ValueReturned_WHEN_InvokeStringFieldDialog_THEN_ShouldInvokeSuccess()
        {
            var reference = CreateReference(DialogResult.Ok("Value"));
            _dialogService
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.InvokeStringFieldDialog("Title", "Label", "Value", value =>
            {
                invoked = value == "Value";
                return Task.CompletedTask;
            });

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ValueMissing_WHEN_InvokeStringFieldDialog_THEN_ShouldNotInvokeSuccess()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.InvokeStringFieldDialog("Title", "Label", "Value", _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            invoked.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_PeersAdded_WHEN_ShowAddPeersDialog_THEN_ShouldReturnPeers()
        {
            var peers = new HashSet<PeerId> { new PeerId("Host", 1) };
            var reference = CreateReference(DialogResult.Ok(peers));
            _dialogService
                .Setup(s => s.ShowAsync<AddPeerDialog>("Add Peer", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddPeersDialog();

            result.Should().BeEquivalentTo(peers);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowAddPeersDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<AddPeerDialog>("Add Peer", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddPeersDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TagsAdded_WHEN_ShowAddTagsDialog_THEN_ShouldReturnTags()
        {
            var tags = new HashSet<string> { "Tag" };
            var reference = CreateReference(DialogResult.Ok(tags));
            _dialogService
                .Setup(s => s.ShowAsync<AddTagDialog>("Add Tags", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTagsDialog();

            result.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowAddTagsDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<AddTagDialog>("Add Tags", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTagsDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TrackersAdded_WHEN_ShowAddTrackersDialog_THEN_ShouldReturnTrackers()
        {
            var trackers = new HashSet<string> { "Tracker" };
            var reference = CreateReference(DialogResult.Ok(trackers));
            _dialogService
                .Setup(s => s.ShowAsync<AddTrackerDialog>("Add Tracker", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTrackersDialog();

            result.Should().BeEquivalentTo(trackers);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowAddTrackersDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<AddTrackerDialog>("Add Tracker", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTrackersDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ColumnOptionsReturned_WHEN_ShowColumnsOptionsDialog_THEN_ShouldReturnValues()
        {
            var columns = new[] { new ColumnDefinition<string>("Header", value => value) };
            var selected = new HashSet<string> { "Header" };
            var widths = new Dictionary<string, int?> { { "Header", 10 } };
            var order = new Dictionary<string, int> { { "Header", 0 } };
            var reference = CreateReference(DialogResult.Ok((selected, widths, order)));
            _dialogService
                .Setup(s => s.ShowAsync<ColumnOptionsDialog<string>>("Column Options", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowColumnsOptionsDialog(columns.ToList(), selected, widths, order);

            result.SelectedColumns.Should().BeEquivalentTo(selected);
            result.ColumnWidths.Should().BeEquivalentTo(widths);
            result.ColumnOrder.Should().BeEquivalentTo(order);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowColumnsOptionsDialog_THEN_ShouldReturnDefault()
        {
            var columns = new[] { new ColumnDefinition<string>("Header", value => value) };
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<ColumnOptionsDialog<string>>("Column Options", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowColumnsOptionsDialog(columns.ToList(), new HashSet<string>(), new Dictionary<string, int?>(), new Dictionary<string, int>());

            result.SelectedColumns.Should().BeNull();
            result.ColumnWidths.Should().BeNull();
            result.ColumnOrder.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_DialogConfirmed_WHEN_ShowConfirmDialog_THEN_ShouldReturnTrue()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            _dialogService
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowConfirmDialog("Title", "Content");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowConfirmDialog_THEN_ShouldReturnFalse()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowConfirmDialog("Title", "Content");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationAccepted_WHEN_ShowConfirmDialogWithTask_THEN_ShouldInvokeCallback()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            _dialogService
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.ShowConfirmDialog("Title", "Content", () =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ConfirmationRejected_WHEN_ShowConfirmDialogWithTask_THEN_ShouldNotInvokeCallback()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.ShowConfirmDialog("Title", "Content", () =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            invoked.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationAccepted_WHEN_ShowConfirmDialogWithAction_THEN_ShouldInvokeCallback()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            _dialogService
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.ShowConfirmDialog("Title", "Content", () => invoked = true);

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FiltersReturned_WHEN_ShowFilterOptionsDialog_THEN_ShouldReturnFilters()
        {
            var filters = new List<PropertyFilterDefinition<FilterSample>>
            {
                new PropertyFilterDefinition<FilterSample>(nameof(FilterSample.Value), "Equals", "Value")
            };
            var reference = CreateReference(DialogResult.Ok(filters));
            _dialogService
                .Setup(s => s.ShowAsync<FilterOptionsDialog<FilterSample>>("Filters", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowFilterOptionsDialog(filters);

            result.Should().BeEquivalentTo(filters);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowFilterOptionsDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<FilterOptionsDialog<FilterSample>>("Filters", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowFilterOptionsDialog<FilterSample>(null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ValueReturned_WHEN_ShowStringFieldDialog_THEN_ShouldReturnValue()
        {
            DialogParameters? captured = null;
            var reference = CreateReference(DialogResult.Ok("Value"));
            _dialogService
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference);

            var result = await _target.ShowStringFieldDialog("Title", "Label", "Value");

            result.Should().Be("Value");
            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(StringFieldDialog.Label)).Should().BeTrue();
            captured[nameof(StringFieldDialog.Label)].Should().Be("Label");
            captured.Any(p => p.Key == nameof(StringFieldDialog.Value)).Should().BeTrue();
            captured[nameof(StringFieldDialog.Value)].Should().Be("Value");
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowStringFieldDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowStringFieldDialog("Title", "Label", "Value");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Data_WHEN_ShowSubMenu_THEN_ShouldForwardParameters()
        {
            DialogParameters? captured = null;
            var reference = new Mock<IDialogReference>();
            var parent = new UIAction("Name", "Parent", null, Color.Primary, "Href");
            var hashes = new[] { "Hash" };
            var torrents = new Dictionary<string, MudTorrent> { { "Hash", CreateTorrent("Hash", 0F, 0, 0F, ShareLimitAction.Default) } };
            _dialogService
                .Setup(s => s.ShowAsync<SubMenuDialog>("Parent", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .Callback<string, DialogParameters, DialogOptions>((_, parameters, _) => captured = parameters)
                .ReturnsAsync(reference.Object);

            await _target.ShowSubMenu(hashes, parent, torrents, null);

            captured.Should().NotBeNull();
            captured!.Any(p => p.Key == nameof(SubMenuDialog.ParentAction)).Should().BeTrue();
            captured[nameof(SubMenuDialog.ParentAction)].Should().BeSameAs(parent);
            captured.Any(p => p.Key == nameof(SubMenuDialog.Hashes)).Should().BeTrue();
            ((IEnumerable<string>)captured[nameof(SubMenuDialog.Hashes)]!).Should().BeEquivalentTo(hashes);
            captured.Any(p => p.Key == nameof(SubMenuDialog.Torrents)).Should().BeTrue();
            captured[nameof(SubMenuDialog.Torrents)].Should().BeSameAs(torrents);
        }

        [Fact]
        public async Task GIVEN_PluginChanges_WHEN_ShowSearchPluginsDialog_THEN_ShouldReturnTrue()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            _dialogService
                .Setup(s => s.ShowAsync<SearchPluginsDialog>("Search plugins", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowSearchPluginsDialog();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowSearchPluginsDialog_THEN_ShouldReturnFalse()
        {
            var reference = CreateReference(DialogResult.Cancel());
            _dialogService
                .Setup(s => s.ShowAsync<SearchPluginsDialog>("Search plugins", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowSearchPluginsDialog();

            result.Should().BeFalse();
        }

        private static IDialogReference CreateReference(DialogResult result)
        {
            var reference = new Mock<IDialogReference>();
            reference.Setup(r => r.Result).ReturnsAsync(result);
            return reference.Object;
        }

        private static TorrentOptions CreateTorrentOptions(bool torrentManagementMode, bool startTorrent)
        {
            var options = new TorrentOptions(
                torrentManagementMode,
                "SavePath",
                "Cookie",
                "RenameTorrent",
                "Category",
                startTorrent,
                true,
                StopCondition.MetadataReceived.ToString(),
                false,
                "Original",
                true,
                true,
                2,
                3);
            options.DownloadPath = "DownloadPath";
            options.InactiveSeedingTimeLimit = 4;
            options.RatioLimit = 5F;
            options.SeedingTimeLimit = 6;
            options.ShareLimitAction = ShareLimitAction.Remove.ToString();
            options.UseDownloadPath = true;
            options.Tags = new[] { "Tags" };
            return options;
        }

        private static MudTorrent CreateTorrent(string hash, float ratioLimit, int seedingTimeLimit, float inactiveSeedingTimeLimit, ShareLimitAction shareLimitAction)
        {
            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 1,
                category: string.Empty,
                completed: 0,
                completionOn: 0,
                contentPath: string.Empty,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: string.Empty,
                infoHashV2: string.Empty,
                lastActivity: 0,
                magnetUri: string.Empty,
                maxRatio: ratioLimit + 1,
                maxSeedingTime: seedingTimeLimit + 1,
                name: hash,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit,
                savePath: string.Empty,
                seedingTime: 0,
                seedingTimeLimit,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state: string.Empty,
                superSeeding: false,
                tags: Array.Empty<string>(),
                timeActive: 0,
                totalSize: 0,
                tracker: string.Empty,
                trackersCount: 0,
                hasTrackerError: false,
                hasTrackerWarning: false,
                hasOtherAnnounceError: false,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed: 0,
                reannounce: 0,
                inactiveSeedingTimeLimit,
                maxInactiveSeedingTime: inactiveSeedingTimeLimit + 1,
                popularity: 0,
                downloadPath: string.Empty,
                rootPath: string.Empty,
                isPrivate: false,
                shareLimitAction,
                comment: string.Empty);
        }

        private sealed class TrackingStream : MemoryStream
        {
            public bool DisposeAsyncCalled { get; private set; }

            public override ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                return base.DisposeAsync();
            }
        }

        private sealed class FilterSample
        {
            public string Value { get; set; } = "Value";
        }
    }
}
